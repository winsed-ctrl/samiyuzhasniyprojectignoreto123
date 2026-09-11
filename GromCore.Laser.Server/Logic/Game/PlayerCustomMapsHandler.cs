using Newtonsoft.Json;
using GromCore.Laser.Logic.Battle.Structures;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Home.Structures;
using GromCore.Laser.Logic.Util;
using GromCore.Laser.Server.Database;
using GromCore.Laser.Server.Database.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Telegram.Bots.Requests.Stickers;

namespace GromCore.Laser.Server.Logic.Game
{
    public static class PlayerCustomMapsHandler
    {
        private const string SaveFilePath = "battle_player_map.json";
        private static readonly object _lock = new();

        private static List<PlayerMap> _candidates = new();
        private static PlayerMap _winner;

        private static bool _initialized = false;

        public static IReadOnlyList<PlayerMap> Candidates => _candidates;
        public static PlayerMap Winner => _winner;
        public static string GameMode = "Showdown";
        public static int GetRandomMapWithGameModeVariation(string var)
        {
            List<int> its = new();
            foreach (LocationData i in DataTables.Get(DataType.Location).GetDatas())
            {
                if (i.GameModeVariation == var) its.Add(i.GetGlobalId());
            }
            return its[new Random().Next(its.Count)];
        }

        public static void EnsureInitialized()
        {
            if (!_initialized)
            {
                lock (_lock)
                {
                    if (!_initialized)
                    {
                        LoadFromFile();
                        _initialized = true;
                    }
                }
            }
        }
        public static List<PlayerMap> GetMaps() => _candidates;
        private static PlayerMap FindMap(long accountId, long mapId)
        {
            return _candidates.FirstOrDefault(m => m.AccountId == accountId && m.MapId == mapId);
        }
        public static void AddMap(PlayerMap add)
        {
            if (add == null) return;
            EnsureInitialized();

            lock (_lock)
            {
                _candidates.Add(add);
                add.State = 3; 
                SaveToFile();
                UpdateAccountMapState(add.AccountId, add.MapId, 3);
            }
        }
        public static List<PlayerMap> GetTodayCandidats() => _candidates.FindAll(p => p.GMV == GameModeUtil.GetGameModeVariation(GameMode));
        public static void AddVote(PlayerMap map, int vote, long accid)
        {
            map.Votes.Add(accid, vote);
            //if (map.Likes > 9) SetApproved(accountId, mapId);
        }
        public static void SetWaitingForPublish(long accountId, long mapId) => SetMapState(accountId, mapId, 1);
        public static void SetPublishing(long accountId, long mapId) => SetMapState(accountId, mapId, 2);
        public static void SetApproved(long accountId, long mapId) => SetMapState(accountId, mapId, 3);
        public static void SetPublished(long accountId, long mapId) => SetMapState(accountId, mapId, 4);
        public static void SetWinnerState(long accountId, long mapId) => SetMapState(accountId, mapId, 5);
        public static void SetNotApproved(long accountId, long mapId) => SetMapState(accountId, mapId, 6);
        public static void SetBlocked(long accountId, long mapId) => SetMapState(accountId, mapId, 7);

        private static void SetMapState(long accountId, long mapId, int newState)
        {
            EnsureInitialized();

            lock (_lock)
            {
                var map = FindMap(accountId, mapId);
                if (map == null)
                {
                    Console.WriteLine($"map not found: Account={accountId}, Map={mapId}");
                    return;
                }

                map.State = newState;
                SaveToFile();
                UpdateAccountMapState(accountId, mapId, newState);
            }
        }
        public static void SetMapState(int index, int newState)
        {
            EnsureInitialized();

            lock (_lock)
            {
                var map = _candidates[index];
                if (map == null)
                {
                    Console.WriteLine($"map not found");
                    return;
                }

                map.State = newState;
                SaveToFile();
                UpdateAccountMapState(map.AccountId, map.MapId, newState);
            }
        }

        public static void RemoveMap(long accountId, long mapId, string reason)
        {
            EnsureInitialized();

            lock (_lock)
            {
                var map = FindMap(accountId, mapId);
                if (reason == "block") SetBlocked(accountId, mapId);
                else SetNotApproved(accountId, mapId);
                if (map != null && _candidates.Remove(map))
                {
                    SaveToFile();
                }
            }
        }

        public static void SetWinner(long accountId, long mapId)
        {
            EnsureInitialized();

            lock (_lock)
            {
                var map = FindMap(accountId, mapId);
                if (map != null)
                {
                    _winner = map;
                    SaveToFile();
                }
            }
        }
        private static void UpdateAccountMapState(long accountId, long mapId, int state)
        {
            try
            {
                Account acc = Accounts.Load(accountId);
                if (acc?.Home?.PlayerMaps != null)
                {
                    var accountMap = acc.Home.PlayerMaps.FirstOrDefault(m => m.MapId == mapId);
                    if (accountMap != null)
                    {
                        accountMap.State = state;
                        Accounts.Save(acc);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to update account {accountId} map {mapId}: {e}");
            }
        }

        private static void SaveToFile()
        {
            try
            {
                var data = new
                {
                    Candidates = _candidates,
                    Winner = _winner
                };

                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed to save: {ex}");
            }
        }

        private static void LoadFromFile()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    _candidates = new List<PlayerMap>();
                    _winner = null;
                    LoadFromFile();
                }

                string json = File.ReadAllText(SaveFilePath);
                var data = JsonConvert.DeserializeObject<HandlerSaveData>(json);

                _candidates = data?.Candidates ?? new List<PlayerMap>();
                _winner = data?.Winner;

                Console.WriteLine($"loaded {_candidates.Count} candidates and winner: {_winner?.MapId}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed to load: {e}");
                _candidates = new List<PlayerMap>();
                _winner = null;
            }
        }

        private class HandlerSaveData
        {
            public List<PlayerMap> Candidates { get; set; }
            public PlayerMap Winner { get; set; }
        }
    }
}