using Newtonsoft.Json;
using GromCore.Laser.Logic.Avatar;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Battle;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Data.Helper;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Logic.Home.Items;
using GromCore.Laser.Logic.Home.Structures;
using GromCore.Laser.Logic.Listener;
using GromCore.Laser.Logic.Message;
using GromCore.Laser.Logic.Message.Home;
using GromCore.Laser.Logic.Message.Ranked;
using GromCore.Laser.Logic.Time;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GromCore.Laser.Logic.Ranked
{
    public struct TickAction
    {
        public bool ActionWill;
        public int AtWhenTick;
    }
    public class RankedMatch
    {
        public long PROJECTGROM_RANKED_MATCH_ID;
        public List<RankedMatchPlayer> MatchPlayers;
        public Timer TempTimer;
        public bool Done;
        public bool Over;
        public GameTime Ticks;
        public bool MatchWithBans = true;
        private bool AllPickedUp;
        private int AllPickedUpLastTick;
        private bool AllBan;
        private int AllBanLastTick;
        private int PostAllPickupTick;
        
        public TickAction AllPlayersBannedAction;
        public TickAction PickupAction;
        public bool PickUpStarted;
        public int PickUpLastTick;
        public List<int> QueueModel;
        public int Round;
        public RankedMatchPlayer Turn;
        private int PrefferedLocation;
        private int PrefferedTeam;
        public int GameRounds;
        public int TrueTeamWins;
        public int TrueRedWins;
        public bool Solo;
        public bool BanDone;
        public bool MatchOver => TrueRedWins >= 2 || TrueTeamWins >= 2;
        
        private static readonly Random _random = new Random();

        public RankedMatch()
        {
            Round = 0;
            QueueModel = new();
            PrefferedTeam = _random.Next(2);
            PROJECTGROM_RANKED_MATCH_ID = 0;
            Ticks = new();
            MatchPlayers = new();
        }

        public void Start() => TempTimer = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);
        public void Stop()
        {
            TempTimer?.Dispose();
            TempTimer = null;
        }

        public void AddMatchPlayer(RankedMatchPlayer plr)=>MatchPlayers.Add(plr);
        public static int CalculatePlayerIndex(int playerPosition, int teamIndex)
        {
            if (playerPosition == 0 && teamIndex == 1)
            {
                return 3;
            }

            if (teamIndex == 0)
            {
                return playerPosition % 3;
            }
            else if (teamIndex == 1)
            {
                return (playerPosition % 3) + 3;
            }
            return -1;
        }
        public void Update(object stateInfo) => Tick();

        public void Tick()
        {
            Ticks.IncreaseTick();
            if (Done) return;
            HandleTerminate();
            if (GetTicks() == 10)
            {
                PrefferedLocation = CalculateRandomMap();
                QueueModel = new List<int> { 0, 1, 2, 3, 4, 5 };

                Send(new RankedMatchStartedMessage
                {
                    Players = MatchPlayers,
                    SelectedMap = GlobalId.GetInstanceId(PrefferedLocation),
                    Team = PrefferedTeam,
                    PROJECTGROM_RANKED_MATCH_ID = PROJECTGROM_RANKED_MATCH_ID
                });
            }
            if (GetTicks() == 20 * 9)
            {
                if (MatchWithBans)
                {
                    foreach (RankedMatchPlayer p in MatchPlayers)
                        p.BanState = 1;
                    Send(new RankedMatchBanStartedMessage());

                    AllBan = false;
                    AllBanLastTick = GetTicks();
                }
                else
                {
                    if (QueueModel.Count > 0 && Round < QueueModel.Count)
                    {
                        Turn = MatchPlayers.Find(p => p.PlayerInternalIndex == QueueModel[Round]);
                    }
                    
                    if (Turn != null)
                    {
                        int nextIndex = (Round + 1 < QueueModel.Count) ? QueueModel[Round + 1] : -1;
                        Send(new RankedMatchPickStartedMessage { Player = Turn, Next = nextIndex });
                        Turn.PickState = 0;
                        PickUpStarted = true;
                        PickUpLastTick = GetTicks();
                    }
                }
            }

            if (MatchPlayers.All(p => p.Picked) && !AllPickedUp)
            {
                Send(new RankedMatchFinalPreparationStartedMessage());
                AllPickedUp = true;
                AllPickedUpLastTick = GetTicks();
            }

            if (MatchPlayers.All(p => p.Banned) && !AllBan)
            {
                Dictionary<long, int> bannedPersons = new();
                foreach (RankedMatchPlayer p in MatchPlayers)
                {
                    bannedPersons.Add(p.AccountId, p.BannedCharacter);
                }
                Send(new RankedMatchBanEndedMessage { BannedBrawlers = bannedPersons });
                
                foreach (RankedMatchPlayer p in MatchPlayers)
                {
                    p.Character = 0;
                    p.BanState = 0;
                }
                
                if (QueueModel.Count > 0 && Round < QueueModel.Count)
                {
                    Turn = MatchPlayers.Find(p => p.PlayerInternalIndex == QueueModel[Round]);
                }
                
                if (Turn != null)
                {
                    int nextIndex = (Round + 1 < QueueModel.Count) ? QueueModel[Round + 1] : -1;
                    Send(new RankedMatchPickStartedMessage { Player = Turn, Next = nextIndex });
                    Turn.PickState = 0;
                    PickUpStarted = true;
                    PickUpLastTick = GetTicks();
                }
                
                AllBan = true;
                BanDone = true;
            }

            if (PickUpStarted && PickUpLastTick + (20 * 16) < GetTicks() && Turn != null)
            {
                Send(new RankedMatchTerminatedMessage { Name = Turn.DisplayData.Name, Reason = 2 });
                Turn.Home.RankedBan = DateTime.Now.AddMinutes(30);
                Done = true;
            }

            if (AllPickedUp && AllPickedUpLastTick + (20 * 10) < GetTicks())
            {
                LogicServerListener.Instance.StartMatchBattle(MatchPlayers, null, PrefferedLocation, (int)PROJECTGROM_RANKED_MATCH_ID);
                Done = true;
            }

            if (!BanDone && AllBan && AllBanLastTick + (20 * 15) <= GetTicks())
            {
                Dictionary<long, int> bannedPersons = new();
                foreach (RankedMatchPlayer p in MatchPlayers)
                {
                    bannedPersons.Add(p.AccountId, p.BannedCharacter);
                }
                Send(new RankedMatchBanEndedMessage { BannedBrawlers = bannedPersons });
                
                foreach (RankedMatchPlayer p in MatchPlayers)
                {
                    p.Character = 0;
                    p.BanState = 0;
                }
                
                if (QueueModel.Count > 0 && Round < QueueModel.Count)
                {
                    Turn = MatchPlayers.Find(p => p.PlayerInternalIndex == QueueModel[Round]);
                }
                
                if (Turn != null)
                {
                    int nextIndex = (Round + 1 < QueueModel.Count) ? QueueModel[Round + 1] : -1;
                    Send(new RankedMatchPickStartedMessage { Player = Turn, Next = nextIndex });
                    Turn.PickState = 0;
                    PickUpStarted = true;
                    PickUpLastTick = GetTicks();
                }
            }

            TickTimers();
        }

        public void NextTurn()
        {
            Round++;

            if (Round >= QueueModel.Count)
                return;

            Turn = MatchPlayers.Find(p => p.PlayerInternalIndex == QueueModel[Round]);
            if (Turn != null)
            {
                PickUpStarted = true;
                Turn.PickState = 0;
                int nextIndex = (Round + 1 < QueueModel.Count) ? QueueModel[Round + 1] : -1;
                Send(new RankedMatchPickStartedMessage { Player = Turn, Next = nextIndex });
                PickUpLastTick = GetTicks();
            }
        }

        public void TickTimers()
        {
            if (PostAllPickupTick > 0) PostAllPickupTick--;
        }

        public void TerminateError(string name)
        {
            Send(new RankedMatchTerminatedMessage { Name = name });
            Done = true;
        }
        public int RestartRound()
        {
            if (TrueRedWins >= 2 || TrueTeamWins >= 2) return -1;
            LogicServerListener.Instance.StartMatchBattle(MatchPlayers, null, PrefferedLocation, (int)PROJECTGROM_RANKED_MATCH_ID, GameRounds);
            return 0;
        }
        
        public void StopMatch()
        {
            Over = true;
            Stop();
        }

        public void HandleTerminate()
        {
            foreach (RankedMatchPlayer p in MatchPlayers)
                if (!LogicServerListener.Instance.IsPlayerOnline(p.AccountId))
                {
                    Send(new RankedMatchTerminatedMessage { Name = p.DisplayData.Name , Reason = 2});
                    p.Home.RankedBan = DateTime.Now.AddMinutes(30);
                    Done = true;
                }
        }

        public void Send(GameMessage message)
        {
            foreach (RankedMatchPlayer conn in MatchPlayers)
                conn.Home.HomeMode.GameListener.SendMessage(message);
        }

        public int CalculateRandomMap()
        {
            List<string> Names = new();
            foreach (RankedLocationsData data in DataTables.Get(DataType.RankedE).GetDatas())
            {
                var locationData = DataTables.Get(DataType.Location).GetData<LocationData>(data.Name);
                if (locationData != null && !GeneralStaticLogic.BlockedEventsForRanked.Contains(locationData.GameModeVariation))
                {
                    Names.Add(data.Name);
                }
            }
            
            if (Names.Count == 0)
            {
                throw new InvalidOperationException("No available ranked maps found");
            }
            
            string random = Names[_random.Next(Names.Count)];
            var selectedLocation = DataTables.Get(DataType.Location).GetData<LocationData>(random);
            if (selectedLocation == null)
            {
                throw new InvalidOperationException($"Location data not found: {random}");
            }
            
            return selectedLocation.GetGlobalId();
        }

        public RankedMatchPlayer GetPlayer(long id) => MatchPlayers.Find(p => p.AccountId == id);
        public int GetTicks() => Ticks.GetTick();
    }
    public class RankedMatchPlayer
    {
        public long AccountId;
        public PlayerDisplayData DisplayData;
        public int Character;
        public int BanState;
        public int PickState;
        public bool Picked;
        public bool Banned;
        public Hero HeroData;
        public int PlayerIndex;
        public int PlayerInternalIndex;
        public int TeamIndex;
        public int BannedCharacter;
        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;
        public RankedMatchPlayer()
        {
            DisplayData = new PlayerDisplayData();
            AccountId = -1;
        }
        public RankedMatchPlayer(ClientAvatar a, ClientHome h) : this()
        {
            Home = h;
            Avatar = a;
        }
        public static RankedMatchPlayer CreateRankedPlayer(ClientAvatar avatar, ClientHome home, int teamIndex, int playerIndex)
        {
            RankedMatchPlayer player = new(avatar, home);
            player.TeamIndex = teamIndex;
            player.PlayerInternalIndex = playerIndex;
            player.PlayerIndex = playerIndex >= 3 ? RankedMatch.CalculatePlayerIndex(playerIndex, teamIndex) : playerIndex;
            player.DisplayData = new(home.ThumbnailId, home.NameColorId, avatar.Name, home.HasPremiumPass, home.HasPremiumPass);
            player.AccountId = avatar.AccountId;
            return player;
        }
        public void ChangeHeroData(int character, int state)
        {
            // 0 - cannot ban
            // 1 - can ban
            // 2 - cannot pickup
            // 3 - can pickup
            if (state >= 0 && state <= 1) BanState = state;
            if (state >= 2 && state <= 3)
                switch (state)
                {
                    case 2:
                        PickState = 0;
                        break;
                    case 3:
                        PickState = 1;
                        break;
                }


        }
        public void SetBanQueue(int i) => BanState = i;
        public void SetPickUpQueue(int q) => PickState = q;
        public void Encode(ByteStream Stream)
        {
            ByteStreamHelper.EncodeLogicLong(Stream, AccountId);
            Stream.WriteBoolean(true);
            DisplayData.Encode(Stream);

            Stream.WriteVInt(BanState); // state?
            Stream.WriteVInt(PickState); // PickUp
            Stream.WriteVInt(PlayerIndex); // index 0-2
            ByteStreamHelper.WriteDataReference(Stream, Character); // picked up brawler
            if (Character == 0)
            {
                Stream.WriteVInt(11); // PlayerLVL
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                ByteStreamHelper.WriteDataReference(Stream, 0);
                Stream.WriteBoolean(false); // picked up
                Stream.WriteVInt(1); // gear lvl (not used);
                ByteStreamHelper.WriteDataReference(Stream, 0); // gear
                Stream.WriteVInt(1); // gear lvl (not used);
                ByteStreamHelper.WriteDataReference(Stream, 0); // gear
                Stream.WriteVInt(0);
            }
            else
            {
                Hero hero = Avatar.GetHero(Character);
                int _heroLVL = 1;
                int _starpowerId = 0;
                int _gadgetId = 0;
                int _hyperCharge = 0;
                int _gear1GlobalId = 0;
                int _gear2GlobalId = 0;
                if (hero != null)
                {
                    _heroLVL = hero.PowerLevel;
                    _starpowerId =  GlobalId.CreateGlobalId(23, hero.SelectedStarPowerId);
                    _gadgetId = GlobalId.CreateGlobalId(23, hero.SelectedGadgetId);
                    _hyperCharge = GlobalId.CreateGlobalId(23, hero.SelectedOverChargeId);
                    _gear1GlobalId = GlobalId.CreateGlobalId(62, hero.SelectedGearId1);
                    _gear2GlobalId = GlobalId.CreateGlobalId(62, hero.SelectedGearId2);
                }
                Stream.WriteVInt(_heroLVL); // PlayerLVL
                ByteStreamHelper.WriteDataReference(Stream, 0); // Unknown
                ByteStreamHelper.WriteDataReference(Stream, _starpowerId); // brawler starpower
                ByteStreamHelper.WriteDataReference(Stream, _gadgetId); // brawler gadget
                ByteStreamHelper.WriteDataReference(Stream, _hyperCharge); // hypercharge
                Stream.WriteBoolean(Picked); // picked up
                Stream.WriteVInt(1); // gear lvl (not used);
                ByteStreamHelper.WriteDataReference(Stream, _gear1GlobalId); // gear
                Stream.WriteVInt(1); // gear lvl (not used);
                ByteStreamHelper.WriteDataReference(Stream, _gear2GlobalId); // gear
                Stream.WriteVInt(0); // Hero count (not used)
            }
        }
    }
}

// made by stealdev \\