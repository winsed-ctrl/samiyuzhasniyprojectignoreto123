using Newtonsoft.Json;
using GromCore.Laser.Logic;
using GromCore.Laser.Logic.Avatar;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Battle;
using GromCore.Laser.Logic.Battle.Structures;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Data.Helper;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Logic.Home.Items;
using GromCore.Laser.Logic.Listener;
using GromCore.Laser.Logic.Message;
using GromCore.Laser.Logic.Message.Battle;
using GromCore.Laser.Logic.Message.Home;
using GromCore.Laser.Logic.Ranked;
using GromCore.Laser.Logic.Time;
using GromCore.Laser.Logic.Util;
using GromCore.Laser.Server.Networking;
using GromCore.Laser.Server.Networking.Session;
using GromCore.Laser.Server.Settings;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GromCore.Laser.Server.Logic.Game;
public class RankedMatchRegulator
{

    private static long _counter;
    public static ConcurrentDictionary<long, RankedMatch> Matchs;

    public static void Init()
    {
        Matchs = new ConcurrentDictionary<long, RankedMatch>();
        _counter = 0;

        new Thread(Update).Start();
    }

    public static void Update()
    {
        while (true)
        {
            foreach (RankedMatch battle in Matchs.Values.ToArray())
            {
                if (battle.Over)
                {
                    long batid = battle.PROJECTGROM_RANKED_MATCH_ID;
                    Matchs.Remove(battle.PROJECTGROM_RANKED_MATCH_ID, out _);
                    Logger.LogPrint($"Match end! ID: {batid}");
                }
            }
            Thread.Sleep(1000);
        }
    }

    public static long Add(RankedMatch m)
    {
        if (Matchs == null)
        {
            Matchs = new ConcurrentDictionary<long, RankedMatch>();
            _counter = 0;
        }

        long id = ++_counter;
        Matchs[id] = m;
        Logger.LogPrint($"Match started! ID: {id}");
        return id;
    }

    public static RankedMatch Get(long id)
    {
        if (!Matchs.ContainsKey(id)) return null;
        return Matchs[id];
    }
    public static void StartMatchBattle(List<MatchmakingEntry> entries, EventData edata, int Location = -1, int id = 0, int round = 0)
    {
        UDPGateway gateway = UPD.UdpServers[UPD.GetRandomPort()];
        BattleMode battle = new BattleMode(Location);
        battle.Id = Battles.Add(battle);
        //if (edata.modifi.Count > 0)
        //    foreach (int i in edata.modifi)
        //        battle.EventModifiers.Add(i);

        Random rand = new();
        int players = battle.GetPlayersCountWithGameModeVariation();
        List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();
        Dictionary<long, List<MatchmakingEntry>> teamEntries = new Dictionary<long, List<MatchmakingEntry>>();
        MatchmakingEntry[] entriesWithTeam = entries.FindAll(entry => entry.PlayerTeamId > 0).ToArray();
        for (int i = 0; i < entriesWithTeam.Length; i++)
        {
            MatchmakingEntry entry = entriesWithTeam[i];
            long teamId = entry.PlayerTeamId;
            if (!teamEntries.ContainsKey(teamId)) teamEntries.Add(teamId, new List<MatchmakingEntry>());
            teamEntries[teamId].Add(entry);
            entries.Remove(entry);
        }

        long[] ids = teamEntries.Keys.ToArray();
        for (int i = 0; i < ids.Length; i++)
        {
            List<MatchmakingEntry> teamedEntries = teamEntries[ids[i]];
            for (int j = 0; j < teamedEntries.Count; j++)
            {
                sortedEntries.Add(teamedEntries[j]);
                teamedEntries[j].PrefferedTeam = i;
            }
        }

        for (int i = 0; i < entries.Count; i++)
        {
            sortedEntries.Add(entries[i]);
        }

        for (int i = 0; i < sortedEntries.Count; i++)
        {
            UDPSocket socket = gateway.CreateSocket();
            sortedEntries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
            socket.TCPConnection = sortedEntries[i].Connection;
            socket.Battle = battle;
            sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

            int teamIndex = i % 2;
            if (sortedEntries[i].PrefferedTeam != -1)
            {
                teamIndex = sortedEntries[i].PrefferedTeam;
            }

            if (battle.GetTeamPlayersCount(teamIndex) >= 3)
            {
                if (teamIndex == 1) teamIndex = 0;
                else teamIndex = 1;
            }
            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex, edata, sortedEntries[i].CharacterId);
            player.TeamId = sortedEntries[i].PlayerTeamId;
            sortedEntries[i].Player = player;
           // player.RankedCharacterId = sortedEntries[i].CharacterId;
            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
        }
        battle.AddGameObjects();

        for (int i = 0; i < sortedEntries.Count; i++)
        {
            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = sortedEntries[i].Player.TeamIndex;
            startLoading.OwnIndex = sortedEntries[i].Player.PlayerIndex;
            startLoading.GameMode = battle.GetGameModeVariation();
            startLoading.Round = round;
            sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
            startLoading.Players.AddRange(battle.GetPlayers());
            sortedEntries[i].Connection.Send(startLoading);
            battle.Dummy = startLoading;
            battle.BattleWithTrophies = true;
        }
        battle.IsRanked = true;
        battle.RankedId = id;
        battle.Round = round;
        battle.Start();
    }

}


// made by stealdev :ppp \\