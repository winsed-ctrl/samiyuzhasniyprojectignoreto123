namespace GromCore.Laser.Server.Logic.Game
{
    using GromCore.Laser.Logic;
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Message.Battle;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Ranked;
    using GromCore.Laser.Logic.Team;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Networking.Session;
    using GromCore.Laser.Server.Settings;
    using System.Collections.Concurrent;

    public static class Matchmaking
    {
        private static ConcurrentDictionary<int, MatchmakingSlot> Slots;
        private static Timer _updateTimer;
        private static readonly object _initLock = new();
        private static volatile bool _initialized;

        public static void Init()
        {
            lock (_initLock)
            {
                if (_initialized) return;

                Slots = new ConcurrentDictionary<int, MatchmakingSlot>();

                var events = Events.GetEvents();
                if (events == null || events.Length == 0) return;

                foreach (EventData e in events)
                {
                    if (e == null) continue;

                    int mode = GameModeUtil.GetGameModeVariation(e.Location.GameModeVariation);
                    var slot = new MatchmakingSlot(e, GamePlayUtil.GetPlayerCountWithGameModeVariation(mode))
                    {
                        Slot = e.Slot
                    };
                    Slots[e.Slot] = slot;
                }

                _updateTimer = new Timer(_ => UpdateAllSlots(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));

                for (int i = MatchmakingSlot.BotBrawlersList.Count - 1; i >= 0; i--)
                {
                    int brawlerId = MatchmakingSlot.BotBrawlersList[i];
                    if (GeneralStaticLogic.LockedBrawlers.Contains(brawlerId))
                    {
                        MatchmakingSlot.BotBrawlersList.RemoveAt(i);
                    }
                }

                _initialized = true;
            }
        }

        private static void UpdateAllSlots()
        {
            if (Slots == null) return;

            foreach (var slot in Slots.Values)
            {
                try
                {
                    slot.Update();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Slot {slot.Slot} update error: {ex.Message}");
                }
            }
        }

        public static void ReloadSlot(int slotId, EventData newEvent)
        {
            if (Slots == null || newEvent == null) return;

            int mode = GameModeUtil.GetGameModeVariation(newEvent.Location.GameModeVariation);
            int playersRequired = mode == 30 ? 1 : GamePlayUtil.GetPlayerCountWithGameModeVariation(mode);

            if (Slots.TryGetValue(slotId, out var existing))
            {
                existing.Reconfigure(newEvent, playersRequired);
            }
            else
            {
                var newSlot = new MatchmakingSlot(newEvent, playersRequired) { Slot = slotId };
                Slots[slotId] = newSlot;
            }
        }

        public static void RequestMatchmake(Connection connection, int slot, long team = -1)
        {
            if (Slots == null || !Slots.TryGetValue(slot, out var matchSlot))
            {
                Console.WriteLine($"Unknown slot: {slot}");
                return;
            }

            connection.MatchmakeSlot = slot;
            var entry = new MatchmakingEntry(connection) { PlayerTeamId = team };
            connection.MatchmakingEntry = entry;
            matchSlot.Add(entry);
        }

        public static void RequestRankedMatchmake(Connection connection, bool solo, int prefferedTeam = -1)
        {
            int slot = solo ? 14 : 15;
            if (Slots == null || !Slots.TryGetValue(slot, out var matchSlot)) return;

            connection.MatchmakeSlot = slot;
            var entry = new MatchmakingEntry(connection);
            if (prefferedTeam != -1) entry.PrefferedTeam = prefferedTeam;
            connection.MatchmakingEntry = entry;
            matchSlot.Add(entry);
        }

        public static void CancelMatchmake(Connection connection)
        {
            if (Slots == null) return;

            int slot = connection.MatchmakeSlot;
            if (!Slots.TryGetValue(slot, out var matchSlot)) return;

            connection.MatchmakeSlot = -1;
            matchSlot.Remove(connection.MatchmakingEntry);

            if (connection.MatchmakingEntry?.PlayerTeamId > 0)
            {
                TeamEntry team = Teams.Get(connection.MatchmakingEntry.PlayerTeamId);
                if (team == null) return;

                foreach (TeamMember member in team.Members)
                {
                    member.IsReady = false;
                    Session session = Sessions.GetSession(member.AccountId);
                    if (session != null)
                    {
                        session.Connection.MatchmakingEntry.PlayerTeamId = -1;
                        CancelMatchmake(session.Connection);
                        session.Connection.Send(new MatchMakingCancelledMessage());
                    }
                }
                team.TeamUpdated();
            }
        }
    }

    public class MatchmakingSlot
    {
        private readonly ConcurrentQueue<MatchmakingEntry> _requestQueue = new();
        private readonly ConcurrentQueue<MatchmakingEntry> _removeQueue = new();
        private readonly List<MatchmakingEntry> _queue = new();
        private readonly object _queueLock = new();

        private int _playersRequired;
        private EventData _eventData;
        private int _selectedPort = -1;

        public int SecondsLeft;
        private int _turns;
        public bool MoveTimer;
        public int Slot;

        public bool IsRanked => Slot == 14 || Slot == 15;
        public const int SEARCH_TIMEOUT = 10;

        public static List<int> BotBrawlersList = new()
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            20, 21, 43, 44, 45, 46, 47, 48
        };
        public static List<int> botBrawlers = BotBrawlersList;
        public MatchmakingSlot(EventData eventData, int playersRequired)
        {
            _eventData = eventData;
            int mode = GameModeUtil.GetGameModeVariation(eventData.Location.GameModeVariation);

            _playersRequired = mode == 30 ? 1 : playersRequired;
            MoveTimer = true;

            if (mode == 3 && eventData.modifi.Contains(15))
            {
                _playersRequired = 2;
                MoveTimer = false;
            }

            if (IsRanked) _playersRequired = 2;
            SecondsLeft = SEARCH_TIMEOUT;
        }

        public void Reconfigure(EventData eventData, int playersRequired)
        {
            lock (_queueLock)
            {
                _eventData = eventData;
                int mode = GameModeUtil.GetGameModeVariation(eventData.Location.GameModeVariation);

                _playersRequired = mode == 30 ? 1 : playersRequired;
                MoveTimer = true;

                if (mode == 3 && eventData.modifi.Contains(15))
                {
                    _playersRequired = 2;
                    MoveTimer = false;
                }

                if (IsRanked) _playersRequired = 2;
            }
        }

        public void Add(MatchmakingEntry entry)
        {
            _requestQueue.Enqueue(entry);
        }

        public void Remove(MatchmakingEntry entry)
        {
            if (entry != null) _removeQueue.Enqueue(entry);
        }

        public void Update()
        {
            if (Configuration.Instance.IsLocalServer) _playersRequired = 1;
            if (IsRanked) _playersRequired = 2;
            if (Sessions.Maintenance) return;

            lock (_queueLock)
            {
                ProcessQueues();

                if (_queue.Count == 0)
                {
                    _turns = 0;
                    SecondsLeft = SEARCH_TIMEOUT;
                    return;
                }

                int settedEvent = -1;
                if (_eventData.Slot == 34)
                {
                    settedEvent = GlobalId.CreateGlobalId(15,
                        GeneralStaticLogic.EventListForChaos[Random.Shared.Next(GeneralStaticLogic.EventListForChaos.Count)]);
                }

                if (_queue.Count >= _playersRequired)
                {
                    StartMatch(settedEvent);
                }
                else if (SecondsLeft <= 0 && MoveTimer && !IsRanked)
                {
                    StartMatchWithBots(settedEvent);
                }

                UpdateTimer();
                SendStatusToPlayers();
            }
        }

        private void ProcessQueues()
        {
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (!_queue[i].Connection.IsOpen)
                {
                    _queue.RemoveAt(i);
                }
            }

            while (_removeQueue.TryDequeue(out var entry))
            {
                _queue.Remove(entry);
            }

            while (_requestQueue.TryDequeue(out var entry))
            {
                if (_queue.All(x => x.Connection != entry.Connection))
                {
                    bool wasEmpty = _queue.Count == 0;
                    _queue.Add(entry);
                    if (wasEmpty)
                    {
                        SecondsLeft = SEARCH_TIMEOUT;
                        _turns = 0;
                    }
                }
            }
        }

        private void StartMatch(int settedEvent)
        {
            _selectedPort = UPD.GetRandomPort();
            var entries = _queue.Take(_playersRequired).ToList();

            foreach (var entry in entries)
            {
                if (entry.Connection?.Home?.HomeMode?.Avatar != null)
                {
                    entry.Connection.Home.HomeMode.Avatar.BattlePort = _selectedPort;
                }
            }

            if (IsRanked)
                StartRankedMatch(entries);
            else
                StartGame(entries, _eventData, settedEvent);

            _queue.RemoveRange(0, _playersRequired);
        }

        private void StartMatchWithBots(int settedEvent)
        {
            _selectedPort = UPD.GetRandomPort();
            var entries = _queue.ToList();
            _queue.Clear();

            foreach (var entry in entries)
            {
                if (entry.Connection?.Home?.HomeMode?.Avatar != null)
                {
                    entry.Connection.Home.HomeMode.Avatar.BattlePort = _selectedPort;
                    if (_eventData.Slot == 34)
                    {
                        entry.Connection.Home.ChaosTempHero = GlobalId.CreateGlobalId(16,
                            GeneralStaticLogic.BrawlerListForChaos[Random.Shared.Next(GeneralStaticLogic.BrawlerListForChaos.Count)]);
                    }
                }
            }

            StartGame(entries, _eventData, settedEvent);
            SecondsLeft = SEARCH_TIMEOUT;
        }

        private void UpdateTimer()
        {
            if (_queue.Count > 0)
            {
                _turns++;
                if (_turns >= 4)
                {
                    _turns = 0;
                    SecondsLeft--;
                }
            }
        }

        private void SendStatusToPlayers()
        {
            if (_queue.Count == 0) return;

            var message = new MatchMakingStatusMessage
            {
                Seconds = MoveTimer ? SecondsLeft : -1,
                Found = _queue.Count,
                Max = _playersRequired,
                ShowTips = true
            };

            foreach (var entry in _queue)
            {
                try
                {
                    entry.Connection.Messaging.Send(message);
                }
                catch { }
            }
        }

        public void StartRankedMatch(List<MatchmakingEntry> entries)
        {
            var ranked = new RankedMatch();
            ranked.PROJECTGROM_RANKED_MATCH_ID = RankedMatchRegulator.Add(ranked);

            var sortedEntries = SortEntriesByTeam(entries);

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                int teamIndex = sortedEntries[i].PrefferedTeam != -1 ? sortedEntries[i].PrefferedTeam : (i % 2);
                sortedEntries[i].Connection.Avatar.RanledId = ranked.PROJECTGROM_RANKED_MATCH_ID;
                ranked.AddMatchPlayer(RankedMatchPlayer.CreateRankedPlayer(
                    sortedEntries[i].Connection.Avatar,
                    sortedEntries[i].Connection.Home,
                    teamIndex, i));
            }

            ranked.Solo = Slot == 14;
            ranked.Start();
        }

        public void StartGame(List<MatchmakingEntry> entries, EventData edata, int Location = -1)
        {
            UDPGateway gateway = UPD.UdpServers[_selectedPort];
            int location = _eventData.LocationId;

            if (edata.Slot == 18)
                location = GeneralStaticLogic.EventListForSecret[Random.Shared.Next(GeneralStaticLogic.EventListForSecret.Count)];
            if (Location != -1)
                location = Location;

            var battle = new BattleMode(location)
            {
                Diff = 1,
                IsSecret = edata.Slot == 18
            };

            battle.Id = Battles.Add(battle);

            if (_eventData.modifi.Count > 0)
            {
                foreach (int i in _eventData.modifi)
                    battle.EventModifiers.Add(i);
            }

            if (edata.Slot == 34) battle.EventModifiers.Add(28);

            int players = battle.GetPlayersCountWithGameModeVariation();
            if (_eventData.modifi.Contains(15) && battle.m_gameModeVariation == 3) players = 2;

            var sortedEntries = ProcessGameEntries(entries, battle, gateway, edata, players);

            if (battle.m_players.Count > 0 && battle.m_players[0].IsAdmin)
            {
                for (int i = 0; i < 120; i++) battle.m_time.IncreaseTick();
            }

            battle.StoryMode?.Init();
            battle.AddGameObjects();

            SendStartMessages(sortedEntries, battle, edata);
            battle.Start();
        }

        private List<MatchmakingEntry> SortEntriesByTeam(List<MatchmakingEntry> entries)
        {
            var sorted = new List<MatchmakingEntry>();
            var teamEntries = new Dictionary<long, List<MatchmakingEntry>>();

            foreach (var entry in entries.Where(e => e.PlayerTeamId > 0))
            {
                if (!teamEntries.ContainsKey(entry.PlayerTeamId))
                    teamEntries[entry.PlayerTeamId] = new List<MatchmakingEntry>();
                teamEntries[entry.PlayerTeamId].Add(entry);
            }

            int teamIndex = 0;
            foreach (var teamList in teamEntries.Values)
            {
                foreach (var entry in teamList)
                {
                    entry.PrefferedTeam = teamIndex;
                    sorted.Add(entry);
                }
                teamIndex++;
            }

            foreach (var entry in entries.Where(e => e.PlayerTeamId <= 0))
            {
                sorted.Add(entry);
            }

            return sorted;
        }

        private List<MatchmakingEntry> ProcessGameEntries(List<MatchmakingEntry> entries, BattleMode battle,
            UDPGateway gateway, EventData edata, int players)
        {
            int gameMode = battle.GetGameModeVariation();

            if (gameMode == 7)
                return ProcessSoloEntries(entries, battle, gateway, edata);
            else if (GameModeUtil.HasTwoTeams(gameMode))
                return ProcessTwoTeamEntries(entries, battle, gateway, edata, players);
            else if (GameModeUtil.HasFiveTeams(gameMode))
                return ProcessFiveTeamEntries(entries, battle, gateway, edata);
            else
                return ProcessDefaultEntries(entries, battle, gateway, edata, players);
        }

        private List<MatchmakingEntry> ProcessSoloEntries(List<MatchmakingEntry> entries, BattleMode battle,
            UDPGateway gateway, EventData edata)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var socket = gateway.CreateSocket();
                entries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
                socket.TCPConnection = entries[i].Connection;
                socket.TCPConnection.Avatar.BattlePort = gateway.GetGateWayPort();
                socket.Battle = battle;
                entries[i].Connection.UdpSessionId = socket.SessionId;

                var player = BattlePlayer.Create(entries[i].Connection.Home, entries[i].Connection.Avatar, i, 0, edata);
                player.TeamId = entries[i].PlayerTeamId;
                entries[i].Player = player;
                battle.AddPlayer(player, entries[i].Connection.UdpSessionId);
            }
            return entries;
        }

        private List<MatchmakingEntry> ProcessTwoTeamEntries(List<MatchmakingEntry> entries, BattleMode battle,
            UDPGateway gateway, EventData edata, int players)
        {
            var sortedEntries = SortEntriesByTeam(entries);
            var teamCounter = new int[2];

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var socket = gateway.CreateSocket();
                sortedEntries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
                socket.TCPConnection = sortedEntries[i].Connection;
                socket.Battle = battle;
                sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                int teamIndex = sortedEntries[i].PrefferedTeam != -1 ? sortedEntries[i].PrefferedTeam : (i % 2);
                teamIndex = Math.Clamp(teamIndex, 0, 1);

                if (teamCounter[teamIndex] >= 3)
                {
                    int other = 1 - teamIndex;
                    if (teamCounter[other] < 3) teamIndex = other;
                }

                teamCounter[teamIndex]++;

                var player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar, i, teamIndex, edata);
                player.TeamId = sortedEntries[i].PlayerTeamId;
                sortedEntries[i].Player = player;
                battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
            }

            AddBotsForTwoTeams(battle, sortedEntries.Count, players);
            return sortedEntries;
        }

        private void AddBotsForTwoTeams(BattleMode battle, int realPlayers, int totalPlayers)
        {
            var team1Chars = new HashSet<int>();
            var team2Chars = new HashSet<int>();

            for (int i = realPlayers; i < totalPlayers; i++)
            {
                int botChar = GetUniqueBotCharacter(i % 2 == 0 ? team1Chars : team2Chars);
                int teamIndex = i % 2;

                if (battle.GetTeamPlayersCount(teamIndex) >= 3)
                    teamIndex = 1 - teamIndex;

                if (teamIndex == 0) team1Chars.Add(botChar);
                else team2Chars.Add(botChar);

                var bot = BattlePlayer.CreateBotInfo((i - realPlayers + 1).ToString(), i, teamIndex, botChar);
                battle.AddPlayer(bot, -1);
            }
        }

        private List<MatchmakingEntry> ProcessFiveTeamEntries(List<MatchmakingEntry> entries, BattleMode battle,
            UDPGateway gateway, EventData edata)
        {
            var sortedEntries = SortEntriesByTeam(entries);
            int teamIndex = 0;

            foreach (var entry in sortedEntries.Where(e => e.PrefferedTeam != -1))
            {
                teamIndex = Math.Max(teamIndex, entry.PrefferedTeam + 1);
            }

            for (int i = 0; i < sortedEntries.Count; i++)
            {
                var socket = gateway.CreateSocket();
                sortedEntries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
                socket.TCPConnection = sortedEntries[i].Connection;
                socket.Battle = battle;
                sortedEntries[i].Connection.UdpSessionId = socket.SessionId;

                int targetTeam = sortedEntries[i].PrefferedTeam != -1
                    ? sortedEntries[i].PrefferedTeam
                    : GetMinTeam(battle, 5, 2);

                var player = BattlePlayer.Create(sortedEntries[i].Connection.Home, sortedEntries[i].Connection.Avatar,
                    battle.m_players.Count, targetTeam, edata);
                player.TeamId = sortedEntries[i].PlayerTeamId;
                sortedEntries[i].Player = player;
                battle.AddPlayer(player, sortedEntries[i].Connection.UdpSessionId);
            }

            for (int i = battle.m_players.Count; i < 10; i++)
            {
                int botChar = 16000000 + BotBrawlersList[Random.Shared.Next(BotBrawlersList.Count)];
                int targetTeam = GetMinTeam(battle, 5, 2);
                var bot = BattlePlayer.CreateBotInfo((i - entries.Count + 1).ToString(), battle.m_players.Count, targetTeam, botChar);
                battle.AddPlayer(bot, -1);
            }

            return sortedEntries;
        }

        private List<MatchmakingEntry> ProcessDefaultEntries(List<MatchmakingEntry> entries, BattleMode battle,
            UDPGateway gateway, EventData edata, int players)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var socket = gateway.CreateSocket();
                entries[i].Connection.Avatar.BattlePort = gateway.GetGateWayPort();
                socket.TCPConnection = entries[i].Connection;
                socket.Battle = battle;
                entries[i].Connection.UdpSessionId = socket.SessionId;

                var player = BattlePlayer.Create(entries[i].Connection.Home, entries[i].Connection.Avatar, i, i, edata);
                player.TeamId = entries[i].PlayerTeamId;
                entries[i].Player = player;
                battle.AddPlayer(player, entries[i].Connection.UdpSessionId);
            }

            for (int i = entries.Count; i < players; i++)
            {
                int botChar = 16000000 + BotBrawlersList[Random.Shared.Next(BotBrawlersList.Count)];
                var data = DataTables.Get(16).GetDataByGlobalId<CharacterData>(botChar);
                var bot = BattlePlayer.CreateBotInfo(data.ItemName.ToUpper(), i, i, botChar);
                battle.AddPlayer(bot, -1);
            }

            return entries;
        }

        private int GetMinTeam(BattleMode battle, int teamCount, int maxPerTeam)
        {
            int minTeam = 0;
            int minCount = int.MaxValue;

            for (int t = 0; t < teamCount; t++)
            {
                int count = battle.GetTeamPlayersCount(t);
                if (count < minCount && count < maxPerTeam)
                {
                    minCount = count;
                    minTeam = t;
                }
            }

            return minTeam;
        }

        private int GetUniqueBotCharacter(HashSet<int> usedChars)
        {
            int botChar;
            int tries = 0;

            do
            {
                botChar = 16000000 + BotBrawlersList[Random.Shared.Next(BotBrawlersList.Count)];
                tries++;
            } while (usedChars.Contains(botChar) && tries < 100);

            return botChar;
        }

        private void SendStartMessages(List<MatchmakingEntry> entries, BattleMode battle, EventData edata)
        {
            foreach (var entry in entries)
            {
                var startLoading = new StartLoadingMessage
                {
                    LocationId = battle.Location.GetGlobalId(),
                    TeamIndex = entry.Player.TeamIndex,
                    OwnIndex = entry.Player.PlayerIndex,
                    GameMode = battle.GetGameModeVariation()
                };
                startLoading.Modifiers = battle.EventModifiers;
                entry.Connection.Avatar.UdpSessionId = entry.Connection.UdpSessionId;
                startLoading.Players.AddRange(battle.GetPlayers());
                entry.Connection.Send(startLoading);

                battle.Dummy = startLoading;
                battle.BattleWithTrophies = true;
            }

            battle.IsChampionship = edata.IsChallengeSlot;
            battle.IsChaos = edata.Slot == 34;

            if (edata.Slot == 34) battle.BattleWithTrophies = false;
            if (edata.IsChallengeSlot) battle.ChampieData = edata;
        }
    }

    public class MatchmakingEntry
    {
        public readonly Connection Connection;
        public BattlePlayer Player;
        public long PlayerTeamId;
        public int CharacterId = 16000001;
        public int PrefferedTeam = -1;

        public MatchmakingEntry(Connection connection)
        {
            Connection = connection;
        }
    }
}
