namespace GromCore.Laser.Server.Logic.Game
{
    using Masuda.Net.Models;
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Message.Battle;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Message.Team;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Logic.Team;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Networking.Session;
    using System.Reflection.Metadata.Ecma335;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Xml.Linq;

    public static class Teams
    {
        private static ConcurrentDictionary<long, TeamEntry> Entries;
        private static long TeamIdCounter;
        private static Timer _updateTimer;
        private static Timer _resetTimer;
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public static void Init()
        {
            Entries = new ConcurrentDictionary<long, TeamEntry>();
            TeamIdCounter = 0;

            _updateTimer = new Timer(_ =>
            {
                RunTimerSafely("update", () =>
                {
                    foreach (TeamEntry team in Entries.Values.ToArray())
                    {
                        if (team == null) continue;

                        try
                        {
                            team.Invites.RemoveAll(invite => invite.InviteTimer <= DateTime.Now);
                            team.TeamUpdated();

                            TeamMember[] members = team.Members.ToArray();
                            if (members.Length > 0 && members.All(e => e == null || e.State == 0 || e.State == 6))
                            {
                                foreach (TeamMember member in members)
                                {
                                    if (member?.homeMode?.Avatar != null)
                                        member.homeMode.Avatar.TeamId = 0;
                                }
                                Remove(team.Id);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Print($"Teams: failed to update team {team.Id}: {ex}");
                        }
                    }
                });
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            _resetTimer = new Timer(_ =>
            {
                RunTimerSafely("reset", () =>
                {
                    foreach (TeamEntry team in Entries.Values.ToArray())
                    {
                        foreach (TeamMember member in team.Members.ToArray())
                        {
                            if (member == null) continue;
                            var session = Sessions.GetSession(member.AccountId);
                            session?.Connection?.Send(new TeamLeftMessage());
                        }
                    }
                    Entries.Clear();
                });
            }, null, TimeSpan.FromHours(1), TimeSpan.FromHours(1));
        }

        private static void RunTimerSafely(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Print($"Teams: {operation} timer failed: {ex}");
            }
        }

        public static void Stop()
        {
            _cancellationTokenSource.Cancel();
            _updateTimer?.Dispose();
            _resetTimer?.Dispose();
        }
        public static int Count
        {
            get
            {
                return Entries.Count;
            }
        }

        public static TeamEntry Create()
        {
            TeamEntry entry = new TeamEntry();
            entry.Id = Interlocked.Increment(ref TeamIdCounter);
            Entries[entry.Id] = entry;
            return entry;
        }

        public static void Remove(long id)
        {
            Entries.TryRemove(id, out _);
        }

        public static void StartGame(TeamEntry team)
        {
            UDPGateway gateway = UPD.UdpServers[UPD.GetRandomPort()];

            try
            {
                if (team.Type == 0)
                {

                    foreach (TeamMember member in team.Members)
                    {
                        if(Sessions.GetSession(member.AccountId) == null) continue;
                        if(Sessions.GetSession(member.AccountId).Connection == null) continue;
                        Connection connection = Sessions.GetSession(member.AccountId).Connection;
                        if (!team.Members.All(member => member.homeMode.Home.ChallengeWins == team.Members.FirstOrDefault()?.homeMode.Home.ChallengeWins)
                            && (team.EventSlot == 20 || team.EventSlot == 21 || team.EventSlot == 22 || team.EventSlot == 23 || team.EventSlot == 24))
                        {
                            connection.Send(new TeamErrorMessage()
                            {
                                ErrorCode = 120
                            });
                            return;
                        }
                        if (team.EventSlot == 34)
                        {
                            connection.Send(new TeamErrorMessage()
                            {
                                ErrorCode = 125
                            });
                            return;
                        }
                        connection.Send(new TeamGameStartingMessage()
                        {
                            LocationId = team.LocationId
                        });
                        Matchmaking.RequestMatchmake(connection, team.EventSlot, team.Id);
                        member.IsReady = false;
                    }
                }
                if (team.Type == 1)
                {

                    string[] supportedModes = { "GemGrab", "Showdown", "Bounty", "Heist", "DuoShowdown", "KingOfHill", "ProtectKing", "CTF" };
                    string gmv = DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(team.LocationId).GameModeVariation;
                    /*
                    if (!supportedModes.Contains(gmv))
                    {
                        LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                        logicAddNotificationCommand.Notification = new FloaterTextNotification("Not avaible gamemode.");
                        AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                        availableServerCommandMessage.Command = logicAddNotificationCommand;
                        foreach (TeamMember member in team.Members)
                        {
                            Sessions.GetSession(member.AccountId).Connection.Send(availableServerCommandMessage);
                        }
                        return;
                    }*/
                    BattleMode battle = new BattleMode(team.LocationId);
                    battle.BattleWithTrophies = false;
                    battle.Id = Battles.Add(battle);
                    if (team.BattlePlayerMap != null) battle.SetPlayerMap(team.BattlePlayerMap);
                    battle.SetEventModifiers(team.CustomModifiers);
                    List<MatchmakingEntry> entries = new List<MatchmakingEntry>();
                    List<MatchmakingEntry> sortedEntries = new List<MatchmakingEntry>();
                    foreach (TeamMember member in team.Members)
                    {
                        Connection connection = Sessions.GetSession(member.AccountId).Connection;
                        MatchmakingEntry entry = new MatchmakingEntry(connection);
                        entry.PrefferedTeam = member.TeamIndex;
                        entries.Add(entry);
                        member.IsReady = false;
                    }

                    if (GameModeUtil.HasTwoTeams(battle.GetGameModeVariation())) {

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

                            int teamIndex = sortedEntries[i].PrefferedTeam;
                            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                            player.TeamId = sortedEntries[i].PlayerTeamId;
                            sortedEntries[i].Player = player;
                            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                        }
                        List<int> team1BotsCharacters = new List<int>();
                        List<int> team2BotsCharacters = new List<int>();
                        for (int i = battle.GetTeamPlayersCount(0); i < battle.GetPlayersCountWithGameModeVariation() / 2; i++)
                        {
                            if (team.DisabledBots.Contains(i)) continue;
                            
                            bool isBotValid = false;

                            int botCharacter = -1;
                            while (!isBotValid)
                            {
                                botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();
                                isBotValid = !team1BotsCharacters.Contains(botCharacter);
                                if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                            }

                            team1BotsCharacters.Add(botCharacter);
                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo((i - entries.Count + 2).ToString(), battle.GetPlayers().Count(), 0, botCharacter);
                            battle.AddPlayer(bot, -1);
                        }
                        for (int i = battle.GetPlayersCountWithGameModeVariation() / 2 + battle.GetTeamPlayersCount(1); i < battle.GetPlayersCountWithGameModeVariation(); i++)
                        {
                            if (team.DisabledBots.Contains(i)) continue;
                            bool isBotValid = false;
                            
                            int botCharacter = -1;
                            while (!isBotValid)
                            {
                                botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();
                                isBotValid = !team2BotsCharacters.Contains(botCharacter);
                                if (!isBotValid) isBotValid = !GameModeUtil.HasTwoTeams(battle.GetGameModeVariation());
                            }
                            team2BotsCharacters.Add(botCharacter);
                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo((i - entries.Count + 2).ToString(), battle.GetPlayers().Count(), 1, botCharacter);
                            battle.AddPlayer(bot, -1);
                        }
                    }
                    else
                    {
                        sortedEntries = entries;
                        for (int i = 0; i < entries.Count; i++)
                        {
                            
                            UDPSocket socket = gateway.CreateSocket();
                            sortedEntries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
                            socket.TCPConnection = sortedEntries[i].Connection;
                            socket.Battle = battle;
                            sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                            int teamIndex = i;
                            
                            BattlePlayer player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex);
                            player.TeamId = sortedEntries[i].PlayerTeamId;
                            sortedEntries[i].Player = player;
                            battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
                        }
                        int botSlotIndex = entries.Count; 
                        int addedBots = 0;
                        int targetBotCount = battle.GetPlayersCountWithGameModeVariation() - entries.Count;

                        for (int i = entries.Count; i < battle.GetPlayersCountWithGameModeVariation() && addedBots < targetBotCount; i++)
                        {
                            if (team.DisabledBots.Contains(i))
                            {
                                continue;
                            }

                            int botCharacter = 16000000 + MatchmakingSlot.botBrawlers.OrderBy(f => Guid.NewGuid()).First();
                            CharacterData data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botCharacter);
                            BattlePlayer bot = BattlePlayer.CreateBotInfo(data.ItemName.ToUpper(), botSlotIndex, botSlotIndex, botCharacter);
                            battle.AddPlayer(bot, -1);

                            botSlotIndex++;
                            addedBots++;
                        }
                    }

                    for (int i = 0; i < sortedEntries.Count; i++)
                    {
                        StartLoadingMessage startLoading = new StartLoadingMessage();
                        startLoading.LocationId = battle.Location.GetGlobalId();
                        startLoading.TeamIndex = sortedEntries[i].Player.TeamIndex;
                        startLoading.OwnIndex = sortedEntries[i].Player.PlayerIndex;
                        startLoading.GameMode = battle.GetGameModeVariation();
                        startLoading.SetPlayerMap(battle.BattlePlayerMap);
                        startLoading.Modifiers = battle.EventModifiers;
                        if (sortedEntries.Count <= 1) { startLoading.LeaveButton = true; }
                        sortedEntries[i].Connection.Avatar.UdpSessionId = sortedEntries[i].Connection.UdpSessionId;
                        startLoading.Players.AddRange(battle.GetPlayers());
                        sortedEntries[i].Connection.Send(startLoading);
                        battle.Dummy = startLoading;
                        battle.BattleWithTrophies = false;
                    }
                    battle.AddGameObjects();
                    battle.Start();

                }
            }
            catch (Exception)
            {
                ;
            }
        }

        public static TeamEntry Get(long id)
        {
            return Entries.TryGetValue(id, out TeamEntry entry) ? entry : null;
        }
    }
}
