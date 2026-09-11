using System.Linq;

namespace GromCore.Laser.Logic.Home
{
    using Masuda.Net.Models;
    using Newtonsoft.Json;
    using System.IO;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Command.Avatar;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Club;
    using GromCore.Laser.Logic.Message.Friends;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Logic.Stream.Entry;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;
    using GromCore.Laser.Logic.Avatar;  // ← ДОБАВИТЬ ЭТУ СТРОКУ
    using GromCore.Laser.Logic;
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Numerics;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Security.Cryptography;

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientHome
    {
        public const int DAILYOFFERS_COUNT = 6;

        public static readonly int[] GoldPacksPrice = new int[]
        {
            20, 50, 140, 280
        };

        public static readonly int[] GoldPacksAmount = new int[]
        {
            150, 400, 1200, 2600
        };

        [JsonProperty] public long HomeId;
        [JsonProperty] public int ThumbnailId;
        [JsonProperty] public int NameColorId;
        [JsonProperty] public int[] CharacterIds;
        [JsonProperty] public int FavouriteCharacter;
        public int CharacterId => CharacterIds[0];
        [JsonProperty] public int TrophiesReward;
        [JsonProperty] public int FrogsReward;
        [JsonProperty] public int TokenReward;
        [JsonProperty] public int StarTokenReward;
        [JsonProperty] public int BlingsReward;
        [JsonProperty] public BigInteger BrawlPassProgress;
        [JsonProperty] public BigInteger PremiumPassProgress;
        [JsonProperty] public BigInteger BrawlPassPlusProgress;
        [JsonProperty] public int BrawlPassTokens;
        [JsonProperty] public bool HasPremiumPass = false;
        [JsonProperty] public bool HasPremiumPassPlus = false;
        [JsonProperty] public List<int> UnlockedEmotes;
        [JsonProperty] public List<int> UnlockedThumbnails;
        [JsonProperty] public List<int> UnlockedTituls;
        [JsonProperty] public List<int> UnlockedSprays;
        [JsonProperty] public NotificationFactory NotificationFactory;
        [JsonProperty] public List<int> UnlockedSkins;
        [JsonProperty] public int TrophyRoadProgress;
        [JsonProperty] public int EventId;
        [JsonProperty] public List<PlayerMap> PlayerMaps = new List<PlayerMap>();
        [JsonProperty] public BattleCard DefaultBattleCard;
        // -1 means "not selected"; theme ID 0 is a valid game theme.
        [JsonProperty] public int PreferredThemeId = -1;
        [JsonProperty] public int RewardType;
        [JsonProperty] public int RewardCount;
        [JsonProperty] public int StarrDropRarity;
        [JsonProperty] public int TokenDoublers;
        [JsonProperty] public int MioparkCompensation;
        [JsonProperty] public int MioparkCompensationSet;
        [JsonProperty] public string MioparkCompensationCode;

        [JsonProperty] public int TokenRefresh;
        [JsonProperty] public int RecruitTokens;
        [JsonProperty] public int FameTokens;
        [JsonProperty] public int PreAddFameTokens;
        [JsonProperty] public int RecruitBrawler;
        [JsonProperty] public int RecruitBrawlerCard;
        [JsonProperty] public int RecruitGemsCost;
        [JsonProperty] public int RecruitCost;
        [JsonProperty] public int ChromaticCoins; // after v52 - shit bcs not uset yet
        [JsonProperty] public int NewRecruitBrawler;
        [JsonProperty] public List<int> BrawlersRoad;
        [JsonProperty] public bool BrawlersRoadGenerate;
        [JsonProperty] public bool IsRustoreInstall;
        [JsonProperty] public bool IsAppleInstall;
        [JsonProperty] public string CreatorCode;
        [JsonProperty] public List<string> CreatorCodes;
        [JsonProperty] public List<string> PaidOffers;
        [JsonProperty] public int StarPointsGained;

        [JsonProperty] public int OldMioparkComp;


        // ignore SerializeObject lore
        [JsonIgnore] public StarrDrop StarrDrop;
        [JsonIgnore] public EventData[] Events;
        [JsonIgnore] public List<OfferBundle> OfferBundles;
        [JsonProperty] public Quests Quests;
        [JsonProperty] public BattleLogs BattleLogs;
        [JsonIgnore] public DailyCalendarData.DailyCalendarData DailyCalendarData;
        [JsonProperty] public Dictionary<int, int> PlayerSelectedEmotes = new Dictionary<int, int>();
        [JsonProperty] public Dictionary<int, int> PlayerSelectedSpray = new Dictionary<int, int>();
        public PlayerThumbnailData Thumbnail => DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(ThumbnailId);
        public NameColorData NameColor => DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(NameColorId);

        public HomeMode HomeMode;

        [JsonProperty] public DateTime LastVisitHomeTime;
        [JsonProperty] public List<string> OffersClaimed;

        [JsonProperty] public string Day;
        [JsonProperty] public string Device;
        [JsonProperty] public string IpAddress;
        [JsonProperty] public int ActivityDays;
        [JsonProperty] public int DailyGift;
        [JsonProperty] public int DebugInt;

        [JsonProperty] public int BpSeason;
        [JsonProperty] public string BrawlPassTimer; // DateTime in str( "2045-03-01 12:30:00")
        [JsonProperty] public string SeasonTimer; // DateTime in str( "2045-03-01 12:30:00")
        [JsonProperty] public bool TwoDropsEvent;
        [JsonProperty] public int ShopSkinsSetId;
        [JsonProperty] public int ThemeId;
        [JsonProperty] public string ShopSkinsSetTimer; // DateTime in str( "2045-03-01 12:30:00")

        [JsonProperty] public bool LvLUpOffers;

        [JsonProperty] public int SomeRefresh;
        [JsonProperty] public int EventTokensRefresh;
        [JsonProperty] public bool AngeloCompensate;

        [JsonProperty] public bool ExecuteLobbyDrop;
        [JsonProperty] public int DropsCount;
        [JsonProperty] public int ChallengeID;
        [JsonProperty] public int ChallengeLoses;
        [JsonProperty] public int ChallengeWins;
        [JsonIgnore] public int ShowLivesPopup;
        [JsonIgnore] public int ChaosTempHero;
        [JsonIgnore] public List<GemOffer> PendingReward;
        [JsonProperty] public int RankedSoloRank;
        [JsonProperty] public int RankedSoloMaxRank;
        [JsonProperty] public int RankedTrioRank;
        [JsonProperty] public int RankedTrioMaxRank;
        [JsonProperty] public int RankedSoloProgress;
        [JsonProperty] public int RankedTrioProgress;
        [JsonProperty] public int RankedSoloMaxProgress;
        [JsonProperty] public int RankedTrioMaxProgress;
        [JsonProperty] public int NotifClear;
        [JsonProperty] public DateTime RankedBan; 

        [JsonProperty] public int AccountCreateStamp;

        [JsonProperty] public int EventTokensClear;
        [JsonProperty] public int EventTokensComp;
        [JsonProperty] public int CreatorsRefresh;
        [JsonProperty] public int CreatorsGemsRemove;
        [JsonProperty] public List<int> EventOfferData;

        [JsonProperty] public List<int> KeysBuyed;

        [JsonProperty] public List<int> EventDailyGift;
        [JsonProperty] public List<int> EventDailyOffer1;
        [JsonProperty] public List<int> EventDailyOffer2;
        [JsonProperty] public List<int> EventDailyOffer3;
        [JsonProperty] public List<int> EventDailyOffer4;
        [JsonProperty] public List<int> EventDailyMegaOffer;
        [JsonProperty] public List<int> EventDailyOfferSkin1;
        [JsonProperty] public List<int> EventDailyOfferSkin2;
        [JsonProperty] public LogicRecruitRoad RecruitRoad;
        [JsonProperty] public int UsedRerolls;
        [JsonProperty] public int PiggyBankTickets;
        public ClientHome()
        {
            ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            NameColorId = GlobalId.CreateGlobalId(43, 0);
            CharacterIds = new int[] { GlobalId.CreateGlobalId(16, 0), GlobalId.CreateGlobalId(16, 1), GlobalId.CreateGlobalId(16, 2) };
            FavouriteCharacter = GlobalId.CreateGlobalId(16, 0);
            PlayerSelectedEmotes.Add(4, 28);
            PlayerSelectedEmotes.Add(5, 84 - 3);
            PlayerSelectedSpray.Add(7, 7 - 3);
            PlayerSelectedSpray.Add(8, 8 - 3);
            PlayerSelectedSpray.Add(9, 18 - 3);
            PlayerSelectedSpray.Add(10, 3);
            NotificationFactory = new NotificationFactory();

            OfferBundles = new List<OfferBundle>();
            UnlockedSkins = new List<int>();
            BrawlersRoad = new List<int>();
            KeysBuyed = new List<int>();
            EventOfferData = new List<int>();

            EventDailyGift = new List<int>();
            EventDailyOffer1 = new List<int>();
            EventDailyOffer2 = new List<int>();
            EventDailyOffer3 = new List<int>();
            EventDailyOffer4 = new List<int>();
            EventDailyMegaOffer = new List<int>();
            EventDailyOfferSkin1 = new List<int>();
            EventDailyOfferSkin2 = new List<int>();

            UnlockedEmotes = new List<int>();
            UnlockedTituls = new List<int>();
            UnlockedThumbnails = new List<int>();
            UnlockedSprays = new List<int>();
            LastVisitHomeTime = DateTime.UnixEpoch;
            OffersClaimed = new List<string>();
            CreatorCodes = new List<string>();
            PaidOffers = new List<string>();
            TrophyRoadProgress = 1;
            TokenDoublers = 0;
            BrawlPassProgress = 1;
            PremiumPassProgress = 1;
            EventId = 1;
            UnlockedEmotes = new List<int>();
            DefaultBattleCard = new BattleCard();
            StarrDrop = new StarrDrop(HomeMode);
            DailyCalendarData = null;// new DailyCalendarData.DailyCalendarData();
            PreferredThemeId = GeneralStaticLogic.DefaultThemeId;
            BattleLogs = new();
            PendingReward = new();
            RankedSoloRank = 1;
            RankedSoloMaxRank = 1;
            RankedTrioRank = 1;
            RankedTrioMaxRank = 1;
            RecruitRoad = new();
            UsedRerolls = 5;
            PiggyBankTickets = 15;
        }

        /// <summary>
        /// Repairs legacy accounts where a short skin id or a malformed global id
        /// was persisted. Invalid rows are dropped instead of crashing HomeState
        /// encoding on the next login.
        /// </summary>
        public void NormalizeUnlockedSkinIds()
        {
            UnlockedSkins ??= new List<int>();
            var table = DataTables.Get(DataType.Skin);
            if (table == null) return;

            var normalized = new HashSet<int>();
            foreach (int storedId in UnlockedSkins)
            {
                int instanceId = GlobalId.GetInstanceId(storedId);
                if (instanceId < 0 || instanceId >= table.Count) continue;
                SkinData skinData = table.GetData<SkinData>(instanceId);
                if (skinData != null) normalized.Add(skinData.GetGlobalId());
            }
            UnlockedSkins = normalized.ToList();
        }



public class CustomOffer
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int Cost { get; set; }
    public int OldCost { get; set; }
    public int Currency { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string BackgroundName { get; set; }
    public bool IsActive { get; set; }
    public List<CustomOfferReward> Rewards { get; set; }
}

public class CustomOfferReward
{
    public string Type { get; set; }
    public int Count { get; set; }
    public int DataId { get; set; }
}


        public void SendChangedHomeCommand()
        {
            LogicDayChangedCommand changed = new();
            changed.Events = Events;
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = changed;
            HomeMode.GameListener.SendMessage(message);
        }

        public void SetGeneralLogicData()
        {
            SeasonTimer = "2026-12-06 15:00:00";
            BrawlPassTimer = SeasonTimer;
            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark")
            {
                int season = 88;
                if (TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 9, 6, 12, 0, 0)) > 0)
                {
                    season = 40;
                    BrawlPassTimer = "2026-09-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-09-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 9, 6, 12, 0, 0), new DateTime(2026, 10, 18, 12, 0, 0)) > 0)
                {
                    season = 41;
                    BrawlPassTimer = "2026-10-18 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-10-18 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 10, 18, 12, 0, 0), new DateTime(2026, 11, 15, 15, 0, 0)) > 0)
                {
                    season = 42;
                    BrawlPassTimer = "2026-11-15 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-11-15 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 11, 15, 15, 0, 0), new DateTime(2026, 1, 10, 15, 0, 0)) > 0)
                {
                    season = 88;
                    BrawlPassTimer = "2026-01-10 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-01-10 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else
                {
                    season = 45;
                    BrawlPassTimer = "2026-03-01 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-03-01 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                if (season != BpSeason)
                {
                    if (BpSeason != 0)
                    {
                        Quests.QuestList.RemoveAll(q => q.QuestType != 0);
                        Quests.AddRandomQuests(HomeMode.Avatar.Heroes, HasPremiumPass);
                    }
                    BpSeason = season;
                    BrawlPassProgress = 0;
                    PremiumPassProgress = 0;
                    BrawlPassPlusProgress = 0;
                    HasPremiumPass = false;
                    HasPremiumPassPlus = false;
                    BrawlPassTokens = 0;
                    Day = "0";
                }
                ShopSkinsSetId = -1;
                if (TimerMath(new DateTime(2026, 1, 24, 11, 0, 0), new DateTime(2026, 1, 31, 11, 0, 0)) > 0) ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                else if (TimerMath(new DateTime(2026, 1, 31, 11, 0, 0), new DateTime(2026, 2, 7, 11, 0, 0)) > 0) ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                ShopSkinsSetTimer = "2045-03-01 12:30:00"; // DateTime in str( "2045-03-01 12:30:00")
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            {
                int season = 88;
                if (TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 9, 27, 15, 0, 0)) > 0)
                {
                    season = 21;
                    Console.WriteLine("111111111111111111");
                    BrawlPassTimer = "2026-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 9, 27, 15, 0, 0), new DateTime(2026, 10, 25, 15, 0, 0)) > 0)
                {
                    season = 22;
                    Console.WriteLine("2154346537657");
                    BrawlPassTimer = "2026-10-25 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-10-25 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 10, 25, 15, 0, 0), new DateTime(2026, 11, 15, 15, 0, 0)) > 0)
                {
                    season = 23;
                    BrawlPassTimer = "2026-11-15 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-11-15 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 11, 15, 15, 0, 0), new DateTime(2026, 12, 6, 15, 0, 0)) > 0)
                {
                    season = 24;
                    BrawlPassTimer = "2026-12-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-12-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 11, 15, 15, 0, 0), new DateTime(2026, 1, 10, 15, 0, 0)) > 0)
                {
                    season = 88;
                    BrawlPassTimer = "2026-01-10 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-01-10 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else
                {
                    season = 25;
                    BrawlPassTimer = "2026-03-21 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-02-28 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                
                if (season != BpSeason)
                {
                    if (BpSeason != 0)
                    {
                        Quests.QuestList.RemoveAll(q => q.QuestType != 0);
                        Quests.AddRandomQuests(HomeMode.Avatar.Heroes, HasPremiumPass);
                    }

                    BpSeason = season;
                    BrawlPassProgress = 0;
                    PremiumPassProgress = 0;
                    BrawlPassPlusProgress = 0;
                    HasPremiumPass = false;
                    HasPremiumPassPlus = false;
                    BrawlPassTokens = 0;
                    Day = "0";
                }
                ShopSkinsSetId = -1;
                if (TimerMath(new DateTime(2026, 1, 24, 11, 0, 0), new DateTime(2026, 1, 31, 11, 0, 0)) > 0) ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                else if (TimerMath(new DateTime(2026, 1, 31, 11, 0, 0), new DateTime(2026, 2, 7, 11, 0, 0)) > 0) ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                ShopSkinsSetTimer = "2045-03-01 12:30:00"; // DateTime in str( "2045-03-01 12:30:00")
            }
            // else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            // {
            //     int season = 0;
            //     season = 21;
            //     BrawlPassTimer = "2035-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
            //     SeasonTimer = "2035-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
            //     if (season != BpSeason)
            //     {
            //         if (BpSeason != 0)
            //         {
            //             Quests.QuestList.RemoveAll(q => q.QuestType != 0);
            //             Quests.AddRandomQuests(HomeMode.Avatar.Heroes, HasPremiumPass);
            //         }
            //         BpSeason = season;
            //         BrawlPassProgress = 0;
            //         PremiumPassProgress = 0;
            //         BrawlPassPlusProgress = 0;
            //         HasPremiumPass = false;
            //         HasPremiumPassPlus = false;
            //         BrawlPassTokens = 0;
            //         Day = "0";
            //     }
            //     ShopSkinsSetId = -1;
            //     // if (TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 8, 27, 12, 0, 0)) > 0) ThemeId = 88;
            //     // else if (TimerMath(new DateTime(2026, 8, 27, 12, 0, 0), new DateTime(2026, 9, 27, 12, 0, 0)) > 0) ThemeId = 88;
            //     // else 
            //     ThemeId = 88;
            //     ShopSkinsSetTimer = "2045-03-01 12:30:00"; // DateTime in str( "2045-03-01 12:30:00")
            // }
            else if (!GeneralStaticLogic.IsCustom)
            {
                int season = 0;
                if (TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 9, 27, 15, 0, 0)) > 0)
                {
                    season = 21;
                    Console.WriteLine("111111111111111111");
                    BrawlPassTimer = "2026-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-09-27 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                else if (TimerMath(new DateTime(2026, 9, 27, 15, 0, 0), new DateTime(2026, 10, 18, 15, 0, 0)) > 0)
                {
                    season = 22;
                    Console.WriteLine("2154346537657");
                    BrawlPassTimer = "2026-10-25 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                    SeasonTimer = "2026-10-25 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
                }
                if (season != BpSeason)
                {
                    if (BpSeason != 0)
                    {
                        Quests.QuestList.RemoveAll(q => q.QuestType != 0);
                        Quests.AddRandomQuests(HomeMode.Avatar.Heroes, HasPremiumPass);
                    }
                    BpSeason = season;
                    BrawlPassProgress = 0;
                    PremiumPassProgress = 0;
                    BrawlPassPlusProgress = 0;
                    HasPremiumPass = false;
                    HasPremiumPassPlus = false;
                    BrawlPassTokens = 0;
                    Day = "0";
                }
                ShopSkinsSetId = -1;
                // if (TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 8, 27, 12, 0, 0)) > 0) ThemeId = 88;
                // else if (TimerMath(new DateTime(2026, 8, 27, 12, 0, 0), new DateTime(2026, 9, 27, 12, 0, 0)) > 0) ThemeId = 88;
                // else 
                ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
                ShopSkinsSetTimer = "2045-03-01 12:30:00"; // DateTime in str( "2045-03-01 12:30:00")
            }
        }

        public List<int> GetBrawlersByRarity(string rarity){
            List<int> rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
            List<int> super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
            List<int> epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 60, 68 };
            List<int> mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66 };
            List<int> legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76 };
            List<int> ultra_legendary = new List<int> { };


            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark")
            {
                rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
                super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
                epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 60, 68, 79, 82, 86, 89, 96 };
                mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66, 78, 81, 83, 84, 87, 90, 91, 92, 93 };
                legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76, 80, 85 };
                ultra_legendary = new List<int> { };

                if (TimerMath(new DateTime(2026, 8, 6, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0)
                {
                    mythic.Add(95);
                }
                if (TimerMath(new DateTime(2026, 8, 20, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0)
                {
                    ultra_legendary.Add(94);
                }
                if (TimerMath(new DateTime(2026, 10, 18, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(97);
                if (TimerMath(new DateTime(2026, 10, 4, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(98);
                if (TimerMath(new DateTime(2026, 12, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) legendary.Add(99);
                if (TimerMath(new DateTime(2026, 1, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(101);
                if (TimerMath(new DateTime(2026, 12, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(100);
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) legendary.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) epic.Add(77);
                if (TimerMath(new DateTime(2026, 11, 8, 11, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) epic.Add(79);
                if (TimerMath(new DateTime(2026, 11, 29, 10, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(78);
                if (TimerMath(new DateTime(2026, 1, 28, 10, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) legendary.Add(80);
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "retro")
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) legendary.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) epic.Add(77);
            }
            else
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) mythic.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) legendary.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) epic.Add(77);
            }

            if (rarity == "rare") return rare;
            else if (rarity == "super_rare") return super_rare;
            else if (rarity == "epic") return epic;
            else if (rarity == "mythic") return mythic;
            else if (rarity == "legendary") return legendary;
            else if (rarity == "ultra_legendary") return ultra_legendary;
            return rare;
        }

        public List<int> GetBrawlers(){
            List<int> all_brawlers_list = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24, 7, 9, 18, 19, 22, 25, 27, 34, 61, 4, 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 11, 17, 21, 35, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 60, 62, 66, 68, 5, 12, 23, 28, 40, 52, 63, 76, 38, 70 };

            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark")
            {
                all_brawlers_list = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24, 7, 9, 18, 19, 22, 25, 27, 34, 61, 4, 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 11, 17, 21, 35, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 60, 62, 66, 68, 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 89, 90, 91, 92, 93, 96 };
                if (TimerMath(new DateTime(2026, 8, 6, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(95);
                if (TimerMath(new DateTime(2026, 8, 20, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(94);
                if (TimerMath(new DateTime(2026, 10, 18, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(97);
                if (TimerMath(new DateTime(2026, 10, 4, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(98);
                if (TimerMath(new DateTime(2026, 12, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(99);
                if (TimerMath(new DateTime(2026, 1, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(101);
                if (TimerMath(new DateTime(2026, 12, 31, 9, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(100);
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(77);
                if (TimerMath(new DateTime(2026, 11, 8, 11, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(79);
                if (TimerMath(new DateTime(2026, 11, 29, 10, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(78);
                if (TimerMath(new DateTime(2026, 1, 28, 10, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(80);
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "retro")
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(77);
            }
            else
            {
                if (TimerMath(new DateTime(2026, 9, 27, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(75);
                if (TimerMath(new DateTime(2026, 9, 3, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(76);
                if (TimerMath(new DateTime(2026, 10, 11, 12, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0) all_brawlers_list.Add(77);
            }
            return all_brawlers_list;
        }

        public void InitRecruitRoad()
        {
            LogicRecruitRoad r = RecruitRoad;
            r.UnlockList = new List<LogicBrawlerRecruit>();
            r.NowRecruit = null;

            var levelToRarity = new Dictionary<int, string>
    {
        { 1, "rare" },
        { 2, "super_rare" },
        { 3, "epic" },
        { 4, "mythic" },
        { 5, "legendary" }
    };

            var internalToCard = new Dictionary<string, string>
            {
                ["rare"] = "rare",
                ["super_rare"] = "super_rare",
                ["epic"] = "epic",
                ["mythic"] = "mega_epic",
                ["legendary"] = "legendary"
            };

            var allBrawlers = new Dictionary<string, List<int>>();
            foreach (var rarity in levelToRarity.Values)
                allBrawlers[rarity] = new List<int>();

            var sources = new (string rarity, IEnumerable<int> list)[]
            {
        ("rare", GeneralStaticLogic.rare),
        ("super_rare", GeneralStaticLogic.super_rare),
        ("epic", GeneralStaticLogic.epic),
        ("mythic", GeneralStaticLogic.mythic),
        ("legendary", GeneralStaticLogic.legendary)
            };

            foreach (var (rarity, list) in sources)
            {
                string expected = internalToCard[rarity];
                foreach (int id in list)
                {
                    var gid = GlobalId.CreateGlobalId(16, id);
                    if (HomeMode.Avatar.HasHero(gid)) continue;

                    var cd = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(gid);
                    if (cd == null || cd.Disabled || cd.LockedForChronos) continue;

                    var card = DataTables.Get(DataType.Card).GetData<CardData>(cd.Name + "_unlock");
                    if (card?.Rarity != expected) continue;

                    allBrawlers[rarity].Add(id);
                }
            }

            var basePattern = new[] { 1, 1, 2, 1, 2, 3, 2, 2, 3, 4, 2, 2, 5 };
            var groupsByRarity = new Dictionary<string, List<List<int>>>();
            var random = new Random();

            foreach (var rarity in allBrawlers.Keys)
            {
                var ids = allBrawlers[rarity];
                var groups = new List<List<int>>();

                while (ids.Count > 0)
                {
                    int variationCount = Math.Min(ids.Count - 1, random.Next(1, 3));
                    int take = 1 + variationCount;
                    var group = ids.Take(take).ToList();
                    groups.Add(group);
                    ids = ids.Skip(take).ToList();
                }

                groupsByRarity[rarity] = groups;
            }

            var allGroups = new List<(int level, List<int> group)>();
            for (int i = r.CurrentPatternIndex; i < basePattern.Length; i++)
            {
                int level = basePattern[i];
                if (!levelToRarity.TryGetValue(level, out string rarity)) continue;
                if (groupsByRarity[rarity].Count == 0) continue;

                allGroups.Add((level, groupsByRarity[rarity][0]));
                groupsByRarity[rarity].RemoveAt(0);
            }
            var order = new[] { "rare", "super_rare", "epic", "mythic", "legendary" };
            foreach (var rarity in order)
            {
                foreach (var group in groupsByRarity[rarity])
                {
                    allGroups.Add((levelToRarity.First(x => x.Value == rarity).Key, group));
                }
            }

            int globalIndex = 0;
            foreach (var (level, group) in allGroups)
            {
                if (group.Count == 0) continue;

                int main = group[0];
                var others = group.Skip(1).ToList();

                if (globalIndex == 0 && r.PreferredBrawlerGlobalId.HasValue)
                {
                    int preferredId = GlobalId.GetInstanceId(r.PreferredBrawlerGlobalId.Value);
                    if (group.Contains(preferredId))
                    {
                        main = preferredId;
                        others = group.Where(id => id != preferredId).ToList();
                    }
                }

                var mainRecruit = new LogicBrawlerRecruit
                {
                    GlobalId = GlobalId.CreateGlobalId(16, main),
                    GemsPrice = LogicRecruitRoad.GetGemPriceForRarity(main),
                    TokenPrice = LogicRecruitRoad.GetTokenPriceForRarity(main),
                    Index = globalIndex,
                    OtherUnlockVariations = others.Select(id => GlobalId.CreateGlobalId(16, id)).ToList()
                };

                if (globalIndex == 0)
                    r.NowRecruit = mainRecruit;
                else
                    r.UnlockList.Add(mainRecruit);

                foreach (int other in others)
                {
                    r.UnlockList.Add(new LogicBrawlerRecruit
                    {
                        GlobalId = GlobalId.CreateGlobalId(16, other),
                        GemsPrice = LogicRecruitRoad.GetGemPriceForRarity(other),
                        TokenPrice = LogicRecruitRoad.GetTokenPriceForRarity(other),
                        Index = globalIndex,
                        OtherUnlockVariations = new List<int>()
                    });
                }

                globalIndex++;
            }
            r.CurrentPatternIndex = Math.Min(r.CurrentPatternIndex + 1, basePattern.Length);
        }

        public void RefreshCurrentSlot(int newNowRecruitGlobalId)
        {
            if (RecruitRoad == null || RecruitRoad.NowRecruit == null)
                return;

            var road = RecruitRoad;
            int currentIndex = road.NowRecruit.Index;

            var newNowRecruit = road.UnlockList
                .FirstOrDefault(r => r.GlobalId == newNowRecruitGlobalId && r.Index == currentIndex);

            if (newNowRecruit == null)
                return;

            var oldNow = road.NowRecruit;
            road.NowRecruit = newNowRecruit;
            road.UnlockList.Remove(newNowRecruit);

            oldNow.OtherUnlockVariations = new List<int>();
            road.UnlockList.Add(oldNow);

            var sameSlotBrawlers = road.UnlockList
                .Where(r => r.Index == currentIndex)
                .Select(r => r.GlobalId)
                .ToList();

            road.NowRecruit.OtherUnlockVariations = sameSlotBrawlers;
        }
        public void HomeVisited()
        {
            if (NotifClear != 994399){
                NotificationFactory = new NotificationFactory();
                NotifClear = 994399;
            }

            try{ if(HomeMode.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(HomeMode.Avatar.TeamId); }
            catch {}

            List<int> _SPGS = new();
            bool ntrSPGS = false;
            foreach (int unquie in HomeMode.Avatar.SPGS){
                // Console.WriteLine("unquie = " + unquie);
                if (!GeneralStaticLogic.AllowedUnquies.Contains(unquie-23000000)){
                    // HomeMode.Avatar.SPGS.Remove(unquie)
                    CardData spg = DataTables.Get(23).GetData<CardData>(unquie-23000000);
                    HomeMode.Avatar.Gold += spg.DirectPurchasePrice;
                    ntrSPGS = true;
                }
                else{
                    _SPGS.Add(unquie);
                }
            }

            if (ntrSPGS){
                HomeMode.Avatar.SPGS = _SPGS;
            }

            foreach (Hero h in HomeMode.Avatar.Heroes){
                // Console.WriteLine("h.SelectedStarPowerId = " + h.SelectedStarPowerId);
                // Console.WriteLine("h.SelectedGadgetId = " + h.SelectedGadgetId);
                // Console.WriteLine("h.SelectedOverChargeId = " + h.SelectedOverChargeId);

                if (!HomeMode.Avatar.SPGS.Contains(h.SelectedStarPowerId+23000000)) h.SelectedStarPowerId = 23000000;
                if (!HomeMode.Avatar.SPGS.Contains(h.SelectedGadgetId+23000000)) h.SelectedGadgetId = 23000000;
                if (!HomeMode.Avatar.SPGS.Contains(h.SelectedOverChargeId+23000000)) h.SelectedOverChargeId = 23000000;
            };

            if (KeysBuyed.Count == 0){
                KeysBuyed.Add(0);
                KeysBuyed.Add(0);
                KeysBuyed.Add(0);
                KeysBuyed.Add(0);
            }
            if (KeysBuyed.Count == 3){
                KeysBuyed.Add(0);
            }
            Quests ??= new Quests();
            if (CreatorsRefresh != 1){
                CreatorsRefresh = 1;
                CreatorCode = "";
                CreatorCodes = new List<string>();
            }
            if (AccountCreateStamp == 0){
                AccountCreateStamp = 1;
            }
            //Console.WriteLine("DDDDD");
            if (DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) TwoDropsEvent = true;
            else TwoDropsEvent = false;
            //Console.WriteLine("DDDDD1");
            // if (LogicServerListener.Instance.IsDev()) TwoDropsEvent = true; 
            // if (RecruitRoad == null) RecruitRoad = new();
            // InitRecruitRoad();
            //Console.WriteLine("DDDDD12");
            var temp = new List<Quest.Quest>(Quests.QuestList);
            //Console.WriteLine("DDDDD2");
            foreach(var q in Quests.QuestList)
            {
                if (q.Time <= 0 && q.QuestType == 0) temp.Remove(q);
            }
            Quests.QuestList = temp;
            // if(!Quests.QuestList.Any(q => q.Id == 52005)) Quests.AddGemOfferQuset(1, new(1, 100, 0, 0), 10, null, null, TimerMath(new DateTime(2000, 8, 6, 12, 0, 0), new DateTime(2026, 11, 20, 12, 0, 0)), 52005,true);
            if (NotificationFactory.NotificationList.Find(n => n.SpecialId == 1001) == null)
            {
                Notification notif = new Notification
                {
                    Id = 81,
                    SpecialId = 1001,
                    MessageEntry = "New TEST Ranked Season started! Earn 100 blings per rank\n<cff0000>N<cff0000>O<cff0000>T<cff0000>I<cff0000>C<cff0000>E</c> After test, you will loss your rank, and highest rank will be shown after test."
                };
                NotificationFactory.Add(notif);
                LogicAddNotificationCommand acm2 = new()
                {
                    Notif = notif
                };
                HomeMode.GameListener.SendCommand(acm2);
            }
            
            if (!UnlockedTituls.Contains(DefaultBattleCard.Title)) DefaultBattleCard.Title = 0;
            PlayerThumbnailData icon = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(DefaultBattleCard.Thumbnail1);
            if (icon == null) DefaultBattleCard.Thumbnail1 = 0;
            if (icon.Name != "base1" && icon.RequiredTotalTrophies == 0 && icon.RequiredHero == null && !HomeMode.Home.UnlockedThumbnails.Contains(DefaultBattleCard.Thumbnail1)) DefaultBattleCard.Thumbnail1 = 0;
            if (!String.IsNullOrEmpty(icon.RequiredHero) && !HomeMode
                .Avatar
                .HasHero(
                DataTables.Get(16)
                .GetData<CharacterData>(
                    icon.RequiredHero)
                .GetGlobalId())) DefaultBattleCard.Thumbnail1 = 0;

            PlayerThumbnailData icon1 = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(DefaultBattleCard.Thumbnail2);
            if (icon1 == null) DefaultBattleCard.Thumbnail2 = 0;
            if (icon1.Name != "base1" && icon1.RequiredTotalTrophies == 0 && icon1.RequiredHero == null && !HomeMode.Home.UnlockedThumbnails.Contains(DefaultBattleCard.Thumbnail2)) DefaultBattleCard.Thumbnail2 = 0;
            if (!String.IsNullOrEmpty(icon1.RequiredHero) && !HomeMode
                .Avatar
                .HasHero(
                DataTables.Get(16)
                .GetData<CharacterData>(
                    icon1.RequiredHero)
                .GetGlobalId())) DefaultBattleCard.Thumbnail2 = 0;

            EmoteData emoteDat123123a = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(DefaultBattleCard.Emote);
            if (emoteDat123123a == null && emoteDat123123a.Rarity != "DEFAULT" && !HomeMode.Home.UnlockedEmotes.Contains(DefaultBattleCard.Emote)) DefaultBattleCard.Emote = 0;
            if (!String.IsNullOrEmpty(emoteDat123123a.Character) && !HomeMode
                    .Avatar
                    .HasHero(
                    DataTables.Get(16)
                    .GetData<CharacterData>(
                        emoteDat123123a.Character)
                    .GetGlobalId())) DefaultBattleCard.Emote = 0;
            if (DropsCount >= 1)
            {
                LogicGiveDeliveryItemsCommand command = new();
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop drop = new(27);
                unit.Drops.Add(drop);
                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);

                HomeMode.GameListener.SendCommand(command);

                HomeMode.Home.StarrDrop.GenerateDrop(HomeMode);
                LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new();
                logicRefreshRandomRewardsCommand.Execute(HomeMode);
                HomeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
            }
            //Console.WriteLine("DDDDD21");
            SetGeneralLogicData();
            if (HomeMode.Avatar.MaxWinstreak < HomeMode.Avatar.WinStreak) HomeMode.Avatar.MaxWinstreak = HomeMode.Avatar.WinStreak;
            if (Events?.Any(e => e != null && e.Slot == 34) == true)
            {
                //Quests.AddRandomQuests(null,1);
                LogicHeroWinQuestsChangedCommand cmd = new();
                cmd.Quests = Quests;

                AvailableServerCommandMessage message = new();
                message.Command = cmd;
                HomeMode.GameListener.SendMessage(message);
            }
            if (false)
            {
                for (int i = 0; i < 100; i++) // dont set big numbers, CPU WILL BE 100% BCS ITS TOO HARD
                {
                    CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(i);
                    if (characterData == null) continue;
                    MasteryVanityData masteryVanityData = DataTables.Get(DataType.MasteryVanity).GetData<MasteryVanityData>(string.Concat("Mastery", characterData.Name));
                    CardData cardData = DataTables.Get(DataType.Card).GetData<CardData>(characterData.Name + "_unlock");
                    if (masteryVanityData == null) continue;
                    TitlesData titlesData = DataTables.Get(DataType.Titul).GetData<TitlesData>(masteryVanityData.RewardTitles);
                    PlayerThumbnailData playerThumbnailData = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(masteryVanityData.RewardPlayerIcons);
                    EmoteData emoteData = DataTables.Get(DataType.Emote).GetData<EmoteData>(masteryVanityData.RewardEmotes);
                    Hero her = HomeMode.Avatar.GetHero(characterData.GetGlobalId());
                    if (her == null) continue;
                    if (her.ClaimedMasteryLVL > 8 && !UnlockedEmotes.Contains(emoteData.GetGlobalId()) && NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == emoteData.GetGlobalId()) == null)
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = emoteData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        HomeMode.GameListener.SendCommand(acm2);
                    }
                    if (her.ClaimedMasteryLVL > 9 && !UnlockedThumbnails.Contains(playerThumbnailData.GetGlobalId()) && NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == playerThumbnailData.GetGlobalId()) == null)
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = playerThumbnailData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        HomeMode.GameListener.SendCommand(acm2);
                    }
                    if (her.ClaimedMasteryLVL > 10 && !UnlockedTituls.Contains(titlesData.GetGlobalId()) && NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == titlesData.GetGlobalId()) == null)
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = titlesData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        HomeMode.GameListener.SendCommand(acm2);
                    }

                }
            }
            //Console.WriteLine("DDDDD22");
            foreach (int skin in UnlockedSkins.ToList())
            {
                try{
                SkinData sdata = DataTables.Get(29).GetDataByGlobalId<SkinData>(skin);
                if (sdata == null) continue;
                if (sdata.Name.EndsWith("Default")) continue;
                foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                {

                    if (
                        emoteData.Skin == sdata.Name && 
                    !UnlockedEmotes.Contains(emoteData.GetGlobalId()) && 
                    NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == emoteData.GetGlobalId()) == null
                    )
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = emoteData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
                        acm5.Command = acm2;
                        HomeMode.GameListener.SendTCPMessage(acm5);
                    }

                }
                foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                {
                    if (playerThumbnailData.CatalogPreRequirementSkin == sdata.Name && !UnlockedThumbnails.Contains(playerThumbnailData.GetGlobalId()) && NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == playerThumbnailData.GetGlobalId()) == null)
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = playerThumbnailData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
                        acm5.Command = acm2;
                        HomeMode.GameListener.SendTCPMessage(acm5);
                    }
                }

                foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                {
                    if (sprayData.Skin == sdata.Name && !UnlockedSprays.Contains(sprayData.GetGlobalId()) && NotificationFactory.NotificationList.Find(n => n.VanityRewardGlobalId == sprayData.GetGlobalId()) == null)
                    {
                        Notification notif = new Notification
                        {
                            Id = 72,
                            VanityRewardGlobalId = sprayData.GetGlobalId(),
                        };
                        NotificationFactory.Add(notif);
                        LogicAddNotificationCommand acm2 = new()
                        {
                            Notif = notif
                        };
                        AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
                        acm5.Command = acm2;
                        HomeMode.GameListener.SendTCPMessage(acm5);
                    }
                }}
                catch{
                    continue;
                }
            }
            //Console.WriteLine("DDDDD23");
            
            // if (CreatorsGemsRemove < 7){
            //     CreatorsGemsRemove = 10;
            //     if (HomeMode.Avatar.Diamonds > 10000){
            //         CreatorsGemsRemove = HomeMode.Avatar.Diamonds;
            //         HomeMode.Avatar.Diamonds = 0;
            //     }
            // }
            // if (OffersClaimed.Contains("event_giftshop_item_5") && !UnlockedTituls.Contains(76000104)){
            //     UnlockedTituls.Add(76000104);
            // }
            // int _DebugInt = 88423699;
            // List<int> __all_brawlers_list = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24, 7, 9, 18, 19, 22, 25, 27, 34, 61, 4, 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 11, 17, 21, 35, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 60, 62, 66, 68, 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 89, 90, 91, 92, 93, 94, 95, 96 };
            // List<int> _all_brawlers_list = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24, 7, 9, 18, 19, 22, 25, 27, 34, 61, 4, 14, 15, 16, 20, 26, 29, 30, 36, 43 };
            // if (DebugInt != _DebugInt && HomeMode.Avatar.IsDev){
            //     HomeMode.Avatar.AddStarPoints(8347523);
            //     HomeMode.Avatar.Gold = 9999999;
            //     HomeMode.Avatar.PowerPoints = 9999999;
            //     HomeMode.Avatar.Diamonds = 9999999;
            //     DebugInt = _DebugInt;
            //     Random r = new Random();
            //     foreach (int x in _all_brawlers_list){
            //         HomeMode.Avatar.UnlockHero(16000000 + x);
            //     }
            //    foreach (Hero h in HomeMode.Avatar.Heroes){
            //         int x = r.Next(1500);
            //         h.Trophies = x;
            //         h.HighestTrophies = x;
            //     }
            // }

            // if (HomeMode.Avatar.IsDev) Day = "0";

            //Console.WriteLine("DDDDD2x");

            int currentChallengeID = 3;
            if (ChallengeID != currentChallengeID)
            {
                ChallengeLoses = 0;
                ChallengeWins = 0;
                ShowLivesPopup = 0;
                ChallengeID = currentChallengeID;
            }

            int currentEventTokensClear = 3;
            if (EventTokensClear != currentEventTokensClear)
            {
                // EventTokensClear = currentEventTokensClear;
                // EventTokensComp += (HomeMode.Avatar.StarPoints / 100)+1;
                HomeMode.Avatar.StarPoints = 0;
                // Day = "0";
            }

            if ((GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark") && MioparkCompensationSet != 1){
                MioparkCompensation = HomeMode.Avatar.HighestTrophies;
                MioparkCompensationSet = 1;
                MioparkCompensationCode = "NONE!";
            }


            //wwqetqrtehsfmil89o8();
            var _Heroes = new List<Hero> { };
            bool NeedToReplaceHeroes = false;
            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom") {foreach (Hero h in HomeMode.Avatar.Heroes){
                if (!((h.CharacterId == 16000080 && !(PaidOffers.Contains("offer_draco") || TimerMath(new DateTime(2026, 1, 28, 10, 0, 0), new DateTime(2035, 3, 12, 12, 0, 0)) > 0)) ||
                    (h.CharacterId > 16000080))){
                    _Heroes.Add(h);
                    NeedToReplaceHeroes = true;
                }
            }}
            //Console.WriteLine("DDDDD24");
            if (NeedToReplaceHeroes)
            {
                HomeMode.Avatar.Heroes = _Heroes;
                for (int x = 0; x < 3; x++)
                {
                    CharacterIds[x] = 16000000;
                }
                AngeloCompensate = true;
                // AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                // loginFailed.ErrorCode = 1;
                // loginFailed.Message = "Rejoin.";
                // HomeMode.GameListener.SendMessage(loginFailed);
                // return;
            }

            List<int> banned_spgs = new List<int> { 637 };

            foreach (Hero h in HomeMode.Avatar.Heroes)
            {
                if (banned_spgs.Contains(h.SelectedStarPowerId)) h.SelectedStarPowerId = 0;
            }
            if (!BrawlersRoadGenerate)
            {
                Random random = new Random();

                List<int> rare = GetBrawlersByRarity("rare");
                List<int> super_rare = GetBrawlersByRarity("super_rare");
                List<int> epic = GetBrawlersByRarity("epic");
                List<int> mythic = GetBrawlersByRarity("mythic");
                List<int> legendary = GetBrawlersByRarity("legendary");
                List<int> ultra_legendary = GetBrawlersByRarity("ultra_legendary");

                BrawlersRoad = new List<int> { 8, 3, 7, 10, 24, 9, 14, 6, 4, 13, 30, 1, 2 };

                super_rare.Remove(7);
                super_rare.Remove(9);
                super_rare.Remove(4);
                epic.Remove(14);
                epic.Remove(30);

                while (super_rare.Count != 0 || epic.Count != 0 || mythic.Count != 0 || legendary.Count != 0)
                {
                    List<int> rares_pattern = new List<int> { 2, 2, 3, 2, 2, 3, 4, 2, 2, 5 };
                    for (int x = 0; x < 10; x++)
                    {

                        if (rares_pattern[x] == 2)
                        {
                            if (super_rare.Count != 0)
                            {
                                int brawler = super_rare[random.Next(super_rare.Count)];
                                BrawlersRoad.Add(brawler);
                                super_rare.Remove(brawler);
                            }
                        }
                        else if (rares_pattern[x] == 3)
                        {
                            if (epic.Count != 0)
                            {
                                int brawler = epic[random.Next(epic.Count)];
                                BrawlersRoad.Add(brawler);
                                epic.Remove(brawler);
                            }
                        }
                        else if (rares_pattern[x] == 4)
                        {
                            if (mythic.Count != 0)
                            {
                                int brawler = mythic[random.Next(mythic.Count)];
                                BrawlersRoad.Add(brawler);
                                mythic.Remove(brawler);
                            }
                        }
                        else if (rares_pattern[x] == 5)
                        {
                            if (legendary.Count != 0)
                            {
                                int brawler = legendary[random.Next(legendary.Count)];
                                BrawlersRoad.Add(brawler);
                                legendary.Remove(brawler);
                            }
                        }
                    }
                }
                BrawlersRoadGenerate = true;
            }

            List<int> all_brawlers_list = GetBrawlers();

            foreach (int brawler in all_brawlers_list)
            {
                if (!BrawlersRoad.Contains(brawler)) BrawlersRoad.Add(brawler);
            }

            List<int> _BrawlersRoad = new List<int>{};
            bool ReplaceBrawlersRoad = false;
            foreach (int brawler in BrawlersRoad)
            {
                if (all_brawlers_list.Contains(brawler)) _BrawlersRoad.Add(brawler);
                else ReplaceBrawlersRoad = true;
            }
            if (ReplaceBrawlersRoad) BrawlersRoad = _BrawlersRoad;

            ////Console.WriteLine("DDDDD");


            DayUpdate();
            ChangeOffers();
            HomeMode.GameListener.SendTCPMessage(new LobbyInfoMessage());
            // string FloaterTextNotificationText = "";

            // if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark") FloaterTextNotificationText = FloaterTextNotificationText + "                   MioparkBrawl 2 Beta!" + "\n";
            // else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom") FloaterTextNotificationText = FloaterTextNotificationText + "                   ProjectGrom!" + "\n";
            // else FloaterTextNotificationText = FloaterTextNotificationText + "SERVER" + "\n";

            // if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom" && (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2026, 10, 25, 12, 0, 0)) > 0))
            // {
            //     FloaterTextNotificationText = FloaterTextNotificationText + "                     !??" + "\n";
            // }

            // if (GeneralStaticLogic.IsCustom && (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2026, 11, 1, 12, 0, 0)) > 0))
            // {
            //     if (HomeMode.Avatar.EventTokenCap < 200) FloaterTextNotificationText = FloaterTextNotificationText + $"\n                       : 'Brawloween'!\n                          : {HomeMode.Avatar.EventTokenCap}/200??";
            //     else FloaterTextNotificationText = FloaterTextNotificationText + $"\n                       : 'Brawloween'!\n                     !??";
            // }

            // LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
            // logicAddNotificationCommand.Notification = new FloaterTextNotification(FloaterTextNotificationText);
            // AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
            // availableServerCommandMessage.Command = logicAddNotificationCommand;
            // HomeMode.GameListener.SendMessage(availableServerCommandMessage);
            // MatchMakingCancelledMessage cancelledMessage = new();
            // HomeMode.GameListener.SendMessage(cancelledMessage);
            //Console.WriteLine("DDDDD3");
        }
        

        public void Tick()
        {
          //  if (LogicServerListener.Instance.IsDev()) HomeMode.GameListener.SendMessage(new LobbyInfoMessage { player = HomeMode});
            LastVisitHomeTime = DateTime.UtcNow;
            TokenReward = 0;
            TrophiesReward = 0;
            StarTokenReward = 0;
            FrogsReward = 0;
            BlingsReward = 0;
            ShowLivesPopup = 0;
            //HomeMode.GameListener.SendCommand(new LogicCooldownExpiredCommand());
            RankedBan = DateTime.Parse("2000-10-04 12:00:00");
            if (HomeMode.Avatar != null)
                HomeMode.Avatar.AllianceName = LogicServerListener.Instance.GetAllianceName(HomeMode.Avatar.AllianceId);
            if (RankedSoloRank == 0) RankedSoloRank = 1;
            if (RankedSoloMaxRank == 0) RankedSoloMaxRank = 1;
            if (RankedTrioRank == 0) RankedTrioRank = 1;
            if (RankedTrioMaxRank == 0) RankedTrioMaxRank = 1;
            if (PendingReward.Count > 0)
            {
                List<GemOffer> copy = PendingReward;
                foreach (GemOffer relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV in copy)
                {
                    if (relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV == null || relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.SettedUp) continue;
                    LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
                    int real = GatchaDrop.GetGatchaDropByShopItem(relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.Type);
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop drop = new GatchaDrop(real);
                    int tmp = relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.BrawlerData;
                    if (relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.BrawlerData == 0 && relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.BrawlerData1 > 0)
                        tmp = GlobalId.CreateGlobalId(relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.BrawlerData1, relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.BrawlerData2);
                    if (real != 4) drop.DataGlobalId = tmp;
                    else drop.CardGlobalId = tmp;
                    if (real != 1) drop.SkinGlobalId = tmp;
                    drop.Count = relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.Count;
                    unit.AddDrop(drop);
                    command.DeliveryUnits.Add(unit);
                    command.Execute(HomeMode);
                    HomeMode.GameListener.SendCommand(command);
                    relgemofferstealdevKrutoi100ProtcentovReaaaaalGemOfferpenisroyaldeV.SettedUp = true;
                }
            }
            if ((int)(RankedBan - DateTime.Now).TotalSeconds == 0)
            {
                HomeMode.GameListener.SendCommand(new LogicCooldownExpiredCommand());
            }
        }

public void GenerateDailyOffers()
{
    Console.WriteLine("[DEBUG] GenerateDailyOffers started");
    
    try
    {
        // ========== БЕСПЛАТНАЯ АКЦИЯ: СКИН 52 + 300 ГЕМОВ ==========
        Console.WriteLine("[DEBUG] Generating free offer: Skin 52 + 300 gems...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2030, 1, 1, 0, 0, 0),
            0, 0, 0,
            "free_release_skin52_300gems", "РЕЛИЗ", "offer_bgr_legendary",
            0, 0, false,
            false, false, 1,
            5, 0, 0, 0, 0, false,
            300, 0, 0, ShopItem.Gems,
            1, 0, 52, ShopItem.Skin
        );

        // ========== БЕСПЛАТНАЯ АКЦИЯ: СКИН 263 + 250 ГЕМОВ ==========
        Console.WriteLine("[DEBUG] Generating free offer: Skin 263 + 250 gems...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2036, 12, 31, 23, 59, 59),
            0, 0, 0,
            "free_skin263_250gems", "ПОДАРОК", "offer_bgr_legendary",
            0, 0, false,
            false, false, 1,
            5, 0, 0, 0, 0, false,
            250, 0, 0, ShopItem.Gems,
            1, 0, 263, ShopItem.Skin
        );
        
        // ========== БЕСПЛАТНЫЙ УЛЬТРА ХАОС ДРОП ==========
        Console.WriteLine("[DEBUG] Generating Free UltraChaosDrop...");
        GenerateOffer(
            new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
            0, 0, 0,
            "FreeUltraChaosDrop", "Подарок!", "offer_bgr",
            0, 0, false,
            false, false, 3,
            100, 0, 0, 0, 0, false,
            1, 0, 0, ShopItem.UltraChaosDrop
        );
        
        // ========== БЕСКОНЕЧНАЯ АКЦИЯ: ХАОС ДРОП ЗА 299 ГЕМОВ ==========
        Console.WriteLine("[DEBUG] Generating ChaosDrop offer for 299 gems...");
        GenerateOffer(
            new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
            299, 0, 0,
            "ChaosDropOffer", "@UltraBrawl", "offer_bgr",
            0, 0, false,
            false, false, 3,
            100, 0, 0, 0, 0, false,
            1, 0, 0, ShopItem.UltraChaosDrop
        );
        
        // ========== МИРОВЫЕ ЧЕМПИОНАТЫ - СКИН 615 ==========
        Console.WriteLine("[DEBUG] Generating World Championship skin 615...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2030, 12, 31, 23, 59, 59),
            899, 0, 0,
            "world_championship_615", "МИРОВЫЕ ЧЕМПИОНАТЫ", "offer_bgr_wf2023",
            0, 0, true,
            false, false, 0,
            0, 0, 0, 0, 0, false,
            1, 0, 615, ShopItem.Skin
        );
        
        // ========== МИРОВЫЕ ЧЕМПИОНАТЫ - СКИН 706 ==========
        Console.WriteLine("[DEBUG] Generating World Championship skin 706...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2030, 12, 31, 23, 59, 59),
            899, 0, 0,
            "world_championship_706", "МИРОВЫЕ ЧЕМПИОНАТЫ", "offer_bgr_wf2023",
            0, 0, true,
            false, false, 0,
            0, 0, 0, 0, 0, false,
            1, 0, 706, ShopItem.Skin
        );
        
        // ========== МИРОВЫЕ ЧЕМПИОНАТЫ - СКИН 742 ==========
        Console.WriteLine("[DEBUG] Generating World Championship skin 742...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2030, 12, 31, 23, 59, 59),
            899, 0, 0,
            "world_championship_742", "МИРОВЫЕ ЧЕМПИОНАТЫ", "offer_bgr_wf2023",
            0, 0, true,
            false, false, 0,
            0, 0, 0, 0, 0, false,
            1, 0, 742, ShopItem.Skin
        );
        
        // ========== ДУЭЛЯНТ КОЛЬТ ЗА 4999 КРИСТАЛЛОВ (217) ==========
        Console.WriteLine("[DEBUG] Generating Duelist Colt skin 217 for 4999 gems...");
        GenerateOffer(
            new DateTime(2025, 1, 1, 0, 0, 0), new DateTime(2030, 12, 31, 23, 59, 59),
            4999, 0, 0,
            "duelist_colt_217", "ДУЭЛЯНТ КОЛЬТ", "offer_bgr_legendary",
            0, 0, true,
            false, false, 0,
            0, 0, 0, 0, 0, false,
            1, 0, 217, ShopItem.Skin
        );
        
        // АКЦИИ С КЛЮЧАМИ
        Console.WriteLine("[DEBUG] Generating Key offers...");
        
        // KeyResource - 599 гемов
        if (KeysBuyed[0] == 0)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeyResource", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                100, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyResource
            );
        }
        else if (KeysBuyed[0] == 1)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 249, 0,
                "KeyResource", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                80, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyResource
            );
        }
        else
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeyResource", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 0,
                0, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyResource
            );
        }
        
        // KeySkins - 599 гемов
        if (KeysBuyed[2] == 0)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeySkins", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                100, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeySkins
            );
        }
        else if (KeysBuyed[2] == 1)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 249, 0,
                "KeySkins", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                80, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeySkins
            );
        }
        else
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeySkins", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 0,
                0, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeySkins
            );
        }
        
        // KeyBrawlers - 299 гемов
        if (KeysBuyed[1] == 0)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                299, 0, 0,
                "KeyBrawlers", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                100, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBrawlers
            );
        }
        else if (KeysBuyed[1] == 1)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                299, 499, 0,
                "KeyBrawlers", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                80, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBrawlers
            );
        }
        else
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                299, 0, 0,
                "KeyBrawlers", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 0,
                0, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBrawlers
            );
        }
        
        // KeyPass (KeyBuffies) - 599 гемов
        if (KeysBuyed[3] == 0)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeyPass", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                100, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBuffies
            );
        }
        else if (KeysBuyed[3] == 1)
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 499, 0,
                "KeyPass", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 3,
                80, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBuffies
            );
        }
        else
        {
            GenerateOffer( //
                new DateTime(2026, 4, 17, 12, 0, 0), new DateTime(2036, 4, 17, 12, 0, 0),
                599, 0, 0,
                "KeyPass", "@UltraBrawl", "offer_bgr",
                0, 0, false,
                false, false, 0,
                0, 0, 0, 0, 0, false,
                1, 0, 0, ShopItem.KeyBuffies
            );
        }
        
        // ========== ДОБАВЛЕНИЕ КАСТОМНЫХ АКЦИЙ ИЗ ФАЙЛА ==========
        Console.WriteLine("[DEBUG] Adding custom offers from file...");
        
        try
        {
            string offersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_offers.json");
            if (File.Exists(offersFile))
            {
                string json = File.ReadAllText(offersFile);
                var customOffers = JsonConvert.DeserializeObject<List<CustomOffer>>(json);
                
                if (customOffers != null)
                {
                    var now = DateTime.UtcNow;
                    int addedCount = 0;
                    
                    foreach (var offer in customOffers)
                    {
                        // Проверяем активна ли акция
                        if (!offer.IsActive) continue;
                        if (offer.StartTime > now || offer.EndTime < now) continue;

                        string customOfferClaim = $"custom_offer_{offer.Id}";

                        // Покупка хранится в аккаунте, поэтому после пересоздания магазина
                        // (в том числе после боя) одноразовая акция не должна появляться снова.
                        if (OffersClaimed.Contains(customOfferClaim)) continue;
                        
                        // Проверяем, не добавлена ли уже
                        if (OfferBundles.Any(b => b.Claim == customOfferClaim)) continue;
                        
                        // Все награды одной акции должны находиться в одном наборе.
                        // Раньше здесь создавалась отдельная карточка на каждую награду
                        // с одинаковым claim. После получения первой карточки общий claim
                        // считался закрытым, и остальные награды исчезали из магазина.
                        var customBundle = new OfferBundle
                        {
                            IsDailyDeals = false,
                            IsTrue = true,
                            StartTime = offer.StartTime,
                            EndTime = offer.EndTime,
                            Cost = offer.Cost,
                            OldCost = offer.OldCost,
                            Currency = offer.Currency,
                            Claim = customOfferClaim,
                            Title = offer.Title,
                            BackgroundExportName = offer.BackgroundName ?? "offer_bgr_legendary",
                            IsTID = 0,
                            OfferType = 0,
                            OneTimeOffer = true,
                            LoadOnStartup = false,
                            Processed = false,
                            TypeBenefit = 0,
                            Benefit = 0,
                            ShopPanelLayoutClass = 0,
                            ShopPanelLayoutType = 0,
                            ShopStyleSetClass = 0,
                            ShopStyleSetType = 0,
                            specialOffer = false,
                            MultiCount = 1,
                            Purchased = false
                        };

                        foreach (var reward in offer.Rewards)
                        {
                            ShopItem itemType = GetShopItemTypeFromString(reward.Type);
                            int brawlerId = 0;
                            int extra = 0;
                            
                            if (reward.Type == "brawler")
                            {
                                brawlerId = GlobalId.GetInstanceId(reward.DataId);
                            }
                            else if (reward.Type == "skin")
                            {
                                int skinInstanceId = GlobalId.GetInstanceId(reward.DataId);
                                SkinData skinData = skinInstanceId >= 0
                                    ? DataTables.Get(DataType.Skin).GetData<SkinData>(skinInstanceId)
                                    : null;
                                SkinConfData skinConf = skinData?.GetConf();
                                CharacterData characterData = skinConf == null
                                    ? null
                                    : DataTables.GetCharacterByName(skinConf.Character);

                                if (skinData == null || characterData == null)
                                {
                                    Console.WriteLine($"[WARN] Custom offer {offer.Id}: invalid skin ID {reward.DataId}");
                                    continue;
                                }

                                brawlerId = characterData.GetInstanceId();
                                extra = skinInstanceId;
                            }
                            else if (reward.Type == "emote" || reward.Type == "spray" ||
                                     reward.Type == "title" || reward.Type == "icon")
                            {
                                extra = GlobalId.GetInstanceId(reward.DataId);
                            }
                            if (reward.Type == "vip")
                            {
                                extra = reward.DataId;
                                brawlerId = reward.Count;
                            }
                            
                            int itemGlobalId = 16000000 + brawlerId;
                            customBundle.Items.Add(new Offer(itemType, reward.Count, itemGlobalId, extra));
                        }

                        if (customBundle.Items.Count > 0)
                        {
                            OfferBundles.Add(customBundle);
                            addedCount++;
                        }
                    }
                    
                    if (addedCount > 0)
                        Console.WriteLine($"[DEBUG] Added {addedCount} custom offers from file");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to load custom offers: {ex.Message}");
        }
        
        Console.WriteLine("[DEBUG] GenerateDailyOffers completed");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] GenerateOffer crashed: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

private ShopItem GetShopItemTypeFromString(string type)
{
    switch (type.ToLower())
    {
        case "gems": return ShopItem.Gems;
        case "coins": return ShopItem.Coin;
        case "pp": return ShopItem.PowerPoint;
        case "bling": return ShopItem.Bling;
        case "sp": return ShopItem.StarPoints;
        case "recruit": return ShopItem.RecruitToken;
        case "skin": return ShopItem.Skin;
        case "brawler": return ShopItem.GuaranteedHero;
        case "emote": return ShopItem.Emote;
        case "spray": return ShopItem.Spray;
        case "title": return ShopItem.PlayerTitle;
        case "icon": return ShopItem.PlayerThumbnail;
        case "chaos": return ShopItem.ChaosDrop;
        case "ultra": return ShopItem.UltraChaosDrop;
        default: return ShopItem.Coin;
    }
}

public void RefreshOffers()
{
    OfferBundles.Clear();
    if (LogicServerListener.Instance.IsDev()) return;
    
    Console.WriteLine("[DEBUG] RefreshOffers started");
    GenerateDailyOffers();
    Console.WriteLine($"[DEBUG] RefreshOffers completed, OfferBundles count={OfferBundles.Count}");
    
    // ВСЕ ОСТАЛЬНЫЕ АКЦИИ ЗАКОММЕНТИРОВАНЫ
    /*
    if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom") 
        RefreshOffersGrom();
    else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark") 
        RefreshOffersCustom();
    else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "retro") 
        RefreshOffersRetro();
    else 
        RefreshOffersCommon();
    */
}
        public ShopItem GetEventShopItemByID(int id)
        {
            switch (id)
            {
                case 0:
                    return ShopItem.Candy;
                case 1:
                    return ShopItem.Gems;
                case 2:
                    return ShopItem.Coin;
                case 3:
                    return ShopItem.PowerPoint;
                case 4:
                    return ShopItem.Bling;
                case 5:
                    return ShopItem.CoinDoubler;
                case 6:
                    return ShopItem.PlayerThumbnail;
                case 7:
                    return ShopItem.Emote;
                case 8:
                    return ShopItem.Skin;
                case 9:
                    return ShopItem.GuaranteedHeroWithLevel;
                default:
                    return ShopItem.Coin;
            }
        }

        public void RefreshOffersRetro()
        {
            GenerateOffer(
                new DateTime(2026, 10, 21, 11, 0, 0), new DateTime(2035, 11, 1, 11, 0, 0),
                0, 0, 0,
                "release_gifts_4", "TEST", "offer_bgr_outlaw",
                0, 0, false,
                false, false, 0,
                0, 0, 0, 0, 0, false,
                5000, 999, 0, ShopItem.Coin
            );
        }

        public void RefreshEventOffers_GiftShop(){
            if (EventOfferData.Count != 5){
                EventOfferData.Add(0);
                EventOfferData.Add(0);
                EventOfferData.Add(0);
                EventOfferData.Add(0);
                EventOfferData.Add(0);
            }
            if (!HomeMode.Avatar.HasHero(16000000 + 39) || OffersClaimed.Contains("event_giftshop_1")){
                GenerateOffer( //
                    new DateTime(2026, 12, 1, 14, 0, 0), new DateTime(2026, 12, 14, 9, 0, 0),
                    0, 0, 0,
                    "event_giftshop_1", "ПОДАРОК", "offer_bgr_giftshop",
                    0, 11, false,
                    false, false, 0,
                    0, 69, 9, 70, 0, false,
                    7, 39, 0, ShopItem.GuaranteedHeroWithLevel
                );
            }
            else{
                GenerateOffer( //
                    new DateTime(2026, 12, 1, 14, 0, 0), new DateTime(2026, 12, 14, 9, 0, 0),
                    0, 0, 0,
                    "event_giftshop_1", "ПОДАРОК", "offer_bgr_legendary",
                    0, 11, false,
                    false, false, 0,
                    0, 69, 9, 70, 0, false,
                    1, 39, 311, ShopItem.Emote
                );
            }
            GenerateOffer( //
                new DateTime(2026, 12, 1, 14, 0, 0), new DateTime(2026, 12, 14, 9, 0, 0),
                10, 0, 3,
                "event_giftshop_gatcha_1", $"МАГАЗИН ПОДАРКОВ {EventOfferData[0]}/5", "offer_bgr_legendary",
                0, 12, false,
                false, false, 0,
                0, 69, 9, 70, 0, false,
                1, 0, 0, ShopItem.EventGatcha,
                1000, 0, 0, ShopItem.Coin
            );
        }

        // CoinDoubler = 9,
        // Coin = 1,
        // Gems = 16,
        // GuaranteedBox = 2,
        // GuaranteedHero = 3,
        // Skin = 4,
        // Item = 5,
        // GuaranteedHeroWithLevel = 30,
        // Spray = 35,
        // PowerPoint = 41,
        // Bling = 45,
        // RecruitToken = 38,
        // OverchargeStarrDrop = 47,
        // BrawlerUpgradeFromLVLToLVL = 48,
        // RarityStarrDrop = 49,
        // RandomStarrDrop = 50,
        // Emote = 19,
        // EmoteBundle = 20,
        // RandomEmotes = 21,
        // RandomEmotesForBrawler = 22,
        // RandomEmoteOfRarity = 23,
        // RandomEmotesPackOfRarity = 27,
        // PlayerThumbnail = 25,

        //     public void GenerateOffer(
        // DateTime OfferStart,
        // DateTime OfferEnd,
        // int Cost,
        // int OldCost,
        // int Currency,
        // string Claim,
        // string Title,
        // string BGR,
        // int IsTID,
        // int DailyOfferType,
        // bool OneTimeOffer,
        // bool LoadOnStartup,
        // bool Processed,
        // int TypeBenefit,
        // int Benefit,int panelClass,int panelType,int styleClass,int styleType,bool Special,

        // int Count,int BrawlerID,int Extra,ShopItem Item,

        // int Count2 = -1,int BrawlerID2 = -1,int Extra2 = -1,ShopItem Item2 = ShopItem.Coin,

        // int Count3 = -1,int BrawlerID3 = -1,int Extra3 = -1,ShopItem Item3 = ShopItem.Coin,

        // int Count4 = -1,int BrawlerID4 = -1,int Extra4 = -1,ShopItem Item4 = ShopItem.Coin
        // )

        public void DayUpdate()
        {
            LastVisitHomeTime = DateTime.Now;
            string Today = LastVisitHomeTime.ToString("d");
            if (Today != Day)
            {
                ActivityDays++;
                LvLUpOffers = false;
                Day = Today;
                HomeMode.Avatar.Wins = 0;
                if (DateTime.Now.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) TwoDropsEvent = true;
                else TwoDropsEvent = false;
                if (LogicServerListener.Instance.IsDev()) TwoDropsEvent = true;
               // RefreshDailyOffers();
                if (Quests == null)
                {
                    Quests = new Quests();
                }
                if(TrophyRoadProgress >= 11){
                    Quests.AddRandomQuests(HomeMode.Avatar.Heroes, HasPremiumPass);
                }
            }
        }

        public List<int> GetUnlockedEventPins()
        {
            List<int> EventPins = new List<int> { 592, 808, 809, 810, 811, 812 };
            List<int> UnlockedEventPins = new List<int> { };
            foreach (int x in EventPins)
            {
                if (!UnlockedEmotes.Contains(x))
                {
                    UnlockedEventPins.Add(x);
                }
            }
            return UnlockedEventPins;
        }

        public List<int> GetUnlockedEventThumbnails()
        {
            List<int> EventThumbnails = new List<int> { 309, 310, 311, 312, 313 };
            List<int> UnlockedEventThumbnails = new List<int> { };
            foreach (int x in EventThumbnails)
            {
                if (!UnlockedThumbnails.Contains(x))
                {
                    UnlockedEventThumbnails.Add(x);
                }
            }
            return UnlockedEventThumbnails;
        }

        public List<int> GetUnlockedEventSkins()
        {
            List<int> EventSkins = new List<int> { 591, 387, 412, 415, 557, 216, 417, 587, 722, 727 };
            List<int> EventSkinsBrawlers = new List<int> { 3, 21, 31, 47, 23, 14, 45, 48, 0, 23 };
            List<int> UnlockedEventSkins = new List<int> { };
            for (int x = 0; x < EventSkins.Count; x++)
            {
                if (!UnlockedSkins.Contains(EventSkins[x]) && HomeMode.HasHeroUnlocked(16000000 + EventSkinsBrawlers[x]))
                {
                    UnlockedEventSkins.Add(EventSkins[x]);
                }
            }
            return UnlockedEventSkins;
        }



        public void RefreshDailyOffers()
        {
            HomeMode.Avatar.EventTokenCap = 0;
            Random random = new Random();
            if (ActivityDays == 1 && HomeMode.Avatar.Trophies == 0)
            {
                DailyGift = 0;
            }
            else if (random.Next(30) == 0)
            {
                DailyGift = random.Next(1, 4);
            }
            else
            {
                DailyGift = random.Next(4, 10);
            }

            List<int> brawlers_list = BrawlersRoad;
            List<int> road_list = new List<int> { };

            // Console.WriteLine("-----------");

            // foreach (int brawler in brawlers_list)
            // {
            //     if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
            //     {
            //         road_list.Add(brawler);
            //         Console.WriteLine($"-----{brawler}----");
            //     }
            // }

            // Console.WriteLine("-----------");

            EventDailyGift = new List<int>();
            EventDailyOffer1 = new List<int>();
            EventDailyOffer2 = new List<int>();
            EventDailyOffer3 = new List<int>();
            EventDailyOffer4 = new List<int>();
            EventDailyMegaOffer = new List<int>();
            EventDailyOfferSkin1 = new List<int>();
            EventDailyOfferSkin2 = new List<int>();

            int EventDailyGift_Random = random.Next(6);
            EventDailyGift.Add(EventDailyGift_Random);
            List<int> UnlockedEventPins = GetUnlockedEventPins();
            List<int> UnlockedEventThumbnails = GetUnlockedEventThumbnails();
            List<int> UnlockedEventSkins = GetUnlockedEventSkins();

            // 0 - конфеты
            // 1 - гемы
            // 2 - монеты
            // 3 - очки силы
            // 4 - блинги
            // 5 - удвоители опыта
            // 6 - иконка игрока
            // 7 - пин
            // 8 - боец

            switch (EventDailyGift_Random)
            {
                case 0:
                    EventDailyGift.Add(random.Next(1, 10) * 5);
                    break;
                case 1:
                    EventDailyGift.Add(random.Next(1, 10));
                    break;
                case 2:
                    EventDailyGift.Add(random.Next(1, 20) * 10);
                    break;
                case 3:
                    EventDailyGift.Add(random.Next(1, 10) * 10);
                    break;
                case 4:
                    EventDailyGift.Add(random.Next(10, 50) * 5);
                    break;
                case 5:
                    EventDailyGift.Add(random.Next(100, 500));
                    break;
            }

            int EventDailyOffer1_ItemsCount = 2;
            int EventDailyOffer1_Price = 0;
            if (UnlockedEventPins.Count > 0 && random.Next(3) == 0)
            {
                int pin1 = UnlockedEventPins[random.Next(UnlockedEventPins.Count)];
                EventDailyOffer1.Add(7);
                EventDailyOffer1.Add(pin1);
                EventDailyOffer1_ItemsCount--;
                EventDailyOffer1_Price += 100;
                UnlockedEventPins.Remove(pin1);
            }
            else if (UnlockedEventThumbnails.Count > 0 && random.Next(3) == 0)
            {
                int thumbnail1 = UnlockedEventThumbnails[random.Next(UnlockedEventThumbnails.Count)];
                EventDailyOffer1.Add(6);
                EventDailyOffer1.Add(thumbnail1);
                EventDailyOffer1_ItemsCount--;
                EventDailyOffer1_Price += 100;
                UnlockedEventThumbnails.Remove(thumbnail1);
            }
            else if (road_list.Count != 0 && random.Next(6) == 0)
            {
                List<int> rare = GetBrawlersByRarity("rare");
                List<int> super_rare = GetBrawlersByRarity("super_rare");
                List<int> epic = GetBrawlersByRarity("epic");
                List<int> mythic = GetBrawlersByRarity("mythic");
                List<int> legendary = GetBrawlersByRarity("legendary");
                List<int> ultra_legendary = GetBrawlersByRarity("ultra_legendary");

                int RecruitBrawler = road_list[random.Next(road_list.Count)];
                road_list.Remove(RecruitBrawler);
                EventDailyOffer1.Add(9);
                EventDailyOffer1.Add(RecruitBrawler);
                int bprice = 0;
                if (rare.Contains(RecruitBrawler))
                    bprice = 200;
                else if (super_rare.Contains(RecruitBrawler))
                    bprice = 350;
                else if (epic.Contains(RecruitBrawler))
                    bprice = 600;
                else if (mythic.Contains(RecruitBrawler))
                    bprice = 900;
                else if (legendary.Contains(RecruitBrawler))
                    bprice = 1500;
                else if (ultra_legendary.Contains(RecruitBrawler))
                    bprice = 2500;
                EventDailyOffer1_ItemsCount--;
                EventDailyOffer1_Price += bprice;
            }
            for (int x = 0; x < EventDailyOffer1_ItemsCount; x++)
            {
                int EventDailyOffer1_Random = random.Next(1, 6);
                int count = 0;
                EventDailyOffer1.Add(EventDailyOffer1_Random);
                switch (EventDailyOffer1_Random)
                {
                    case 1:
                        count = random.Next(1, 11);
                        EventDailyOffer1_Price += count * 20;
                        EventDailyOffer1.Add(count * 3);
                        break;
                    case 2:
                        count = random.Next(5, 11);
                        EventDailyOffer1_Price += count * 3;
                        EventDailyOffer1.Add(count * 50);
                        break;
                    case 3:
                        count = random.Next(5, 11);
                        EventDailyOffer1_Price += count * 3;
                        EventDailyOffer1.Add(count * 25);
                        break;
                    case 4:
                        count = random.Next(5, 11);
                        EventDailyOffer1_Price += count * 4;
                        EventDailyOffer1.Add(count * 200);
                        break;
                    case 5:
                        count = random.Next(5, 11);
                        EventDailyOffer1_Price += count * 2;
                        EventDailyOffer1.Add(count * 250);
                        break;
                }
            }
            EventDailyOffer1.Add(EventDailyOffer1_Price);




            int EventDailyOffer2_ItemsCount = 3;
            int EventDailyOffer2_Price = 0;
            if (UnlockedEventPins.Count > 0 && random.Next(3) == 0)
            {
                int pin1 = UnlockedEventPins[random.Next(UnlockedEventPins.Count)];
                EventDailyOffer2.Add(7);
                EventDailyOffer2.Add(pin1);
                EventDailyOffer2_ItemsCount--;
                EventDailyOffer2_Price += 100;
                UnlockedEventPins.Remove(pin1);
            }
            else if (UnlockedEventThumbnails.Count > 0 && random.Next(3) == 0)
            {
                int thumbnail1 = UnlockedEventThumbnails[random.Next(UnlockedEventThumbnails.Count)];
                EventDailyOffer2.Add(6);
                EventDailyOffer2.Add(thumbnail1);
                EventDailyOffer2_ItemsCount--;
                EventDailyOffer2_Price += 100;
                UnlockedEventThumbnails.Remove(thumbnail1);
            }
            else if (road_list.Count != 0 && random.Next(6) == 0)
            {
                List<int> rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
                List<int> super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
                List<int> epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 60, 68, 79, 82, 86, 89, 96 };
                List<int> mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66, 78, 81, 83, 84, 87, 90, 91, 92, 93, 95, 98, 97 };
                List<int> legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76, 80, 85 };
                List<int> ultra_legendary = new List<int> { 94 };

                int RecruitBrawler = road_list[random.Next(road_list.Count)];
                road_list.Remove(RecruitBrawler);
                EventDailyOffer2.Add(9);
                EventDailyOffer2.Add(RecruitBrawler);
                int bprice = 0;
                if (rare.Contains(RecruitBrawler))
                    bprice = 200;
                else if (super_rare.Contains(RecruitBrawler))
                    bprice = 350;
                else if (epic.Contains(RecruitBrawler))
                    bprice = 600;
                else if (mythic.Contains(RecruitBrawler))
                    bprice = 900;
                else if (legendary.Contains(RecruitBrawler))
                    bprice = 1500;
                else if (ultra_legendary.Contains(RecruitBrawler))
                    bprice = 2500;
                EventDailyOffer2_ItemsCount--;
                EventDailyOffer2_Price += bprice;
            }
            for (int x = 0; x < EventDailyOffer2_ItemsCount; x++)
            {
                int EventDailyOffer2_Random = random.Next(1, 6);
                int count = 0;
                EventDailyOffer2.Add(EventDailyOffer2_Random);
                switch (EventDailyOffer2_Random)
                {
                    case 1:
                        count = random.Next(1, 11);
                        EventDailyOffer2_Price += count * 20;
                        EventDailyOffer2.Add(count * 3);
                        break;
                    case 2:
                        count = random.Next(5, 11);
                        EventDailyOffer2_Price += count * 3;
                        EventDailyOffer2.Add(count * 50);
                        break;
                    case 3:
                        count = random.Next(5, 11);
                        EventDailyOffer2_Price += count * 3;
                        EventDailyOffer2.Add(count * 25);
                        break;
                    case 4:
                        count = random.Next(5, 11);
                        EventDailyOffer2_Price += count * 4;
                        EventDailyOffer2.Add(count * 200);
                        break;
                    case 5:
                        count = random.Next(5, 11);
                        EventDailyOffer2_Price += count * 2;
                        EventDailyOffer2.Add(count * 250);
                        break;
                }
            }
            EventDailyOffer2.Add(EventDailyOffer2_Price);


            int EventDailyOffer3_ItemsCount = 3;
            int EventDailyOffer3_Price = 0;
            if (UnlockedEventPins.Count > 0 && random.Next(3) == 0)
            {
                int pin1 = UnlockedEventPins[random.Next(UnlockedEventPins.Count)];
                EventDailyOffer3.Add(7);
                EventDailyOffer3.Add(pin1);
                EventDailyOffer3_ItemsCount--;
                EventDailyOffer3_Price += 100;
                UnlockedEventPins.Remove(pin1);
            }
            else if (UnlockedEventThumbnails.Count > 0 && random.Next(3) == 0)
            {
                int thumbnail1 = UnlockedEventThumbnails[random.Next(UnlockedEventThumbnails.Count)];
                EventDailyOffer3.Add(6);
                EventDailyOffer3.Add(thumbnail1);
                EventDailyOffer3_ItemsCount--;
                EventDailyOffer3_Price += 100;
                UnlockedEventThumbnails.Remove(thumbnail1);
            }
            else if (road_list.Count != 0 && random.Next(1) == 0)
            {
                List<int> rare = GetBrawlersByRarity("rare");
                List<int> super_rare = GetBrawlersByRarity("super_rare");
                List<int> epic = GetBrawlersByRarity("epic");
                List<int> mythic = GetBrawlersByRarity("mythic");
                List<int> legendary = GetBrawlersByRarity("legendary");
                List<int> ultra_legendary = GetBrawlersByRarity("ultra_legendary");

                int RecruitBrawler = road_list[random.Next(road_list.Count)];
                road_list.Remove(RecruitBrawler);
                EventDailyOffer3.Add(9);
                EventDailyOffer3.Add(RecruitBrawler);
                int bprice = 0;
                if (rare.Contains(RecruitBrawler))
                    bprice = 200;
                else if (super_rare.Contains(RecruitBrawler))
                    bprice = 350;
                else if (epic.Contains(RecruitBrawler))
                    bprice = 600;
                else if (mythic.Contains(RecruitBrawler))
                    bprice = 900;
                else if (legendary.Contains(RecruitBrawler))
                    bprice = 1500;
                else if (ultra_legendary.Contains(RecruitBrawler))
                    bprice = 2500;
                EventDailyOffer3_ItemsCount--;
                EventDailyOffer3_Price += bprice;
            }
            for (int x = 0; x < EventDailyOffer3_ItemsCount; x++)
            {
                int EventDailyOffer3_Random = random.Next(1, 6);
                int count = 0;
                EventDailyOffer3.Add(EventDailyOffer3_Random);
                switch (EventDailyOffer3_Random)
                {
                    case 1:
                        count = random.Next(1, 11);
                        EventDailyOffer3_Price += count * 20;
                        EventDailyOffer3.Add(count * 3);
                        break;
                    case 2:
                        count = random.Next(5, 11);
                        EventDailyOffer3_Price += count * 3;
                        EventDailyOffer3.Add(count * 50);
                        break;
                    case 3:
                        count = random.Next(5, 11);
                        EventDailyOffer3_Price += count * 3;
                        EventDailyOffer3.Add(count * 25);
                        break;
                    case 4:
                        count = random.Next(5, 11);
                        EventDailyOffer3_Price += count * 4;
                        EventDailyOffer3.Add(count * 200);
                        break;
                    case 5:
                        count = random.Next(5, 11);
                        EventDailyOffer3_Price += count * 2;
                        EventDailyOffer3.Add(count * 250);
                        break;
                }
            }
            EventDailyOffer3.Add(EventDailyOffer3_Price);





            int EventDailyOffer4_ItemsCount = 4;
            int EventDailyOffer4_Price = 0;
            if (UnlockedEventPins.Count > 0 && random.Next(3) == 0)
            {
                int pin4 = UnlockedEventPins[random.Next(UnlockedEventPins.Count)];
                EventDailyOffer4.Add(7);
                EventDailyOffer4.Add(pin4);
                EventDailyOffer4_ItemsCount--;
                EventDailyOffer4_Price += 100;
                UnlockedEventPins.Remove(pin4);
            }
            else if (UnlockedEventThumbnails.Count > 0 && random.Next(3) == 0)
            {
                int thumbnail4 = UnlockedEventThumbnails[random.Next(UnlockedEventThumbnails.Count)];
                EventDailyOffer4.Add(6);
                EventDailyOffer4.Add(thumbnail4);
                EventDailyOffer4_ItemsCount--;
                EventDailyOffer4_Price += 100;
                UnlockedEventThumbnails.Remove(thumbnail4);
            }
            else if (road_list.Count != 0 && random.Next(1) == 110)
            {
                List<int> rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
                List<int> super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
                List<int> epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 60, 68, 79, 82, 86, 89, 96 };
                List<int> mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66, 78, 81, 83, 84, 87, 90, 91, 92, 93, 95, 98, 97 };
                List<int> legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76, 80, 85 };
                List<int> ultra_legendary = new List<int> { 94 };

                int RecruitBrawler = road_list[random.Next(road_list.Count)];
                road_list.Remove(RecruitBrawler);
                EventDailyOffer4.Add(9);
                EventDailyOffer4.Add(RecruitBrawler);
                int bprice = 0;
                if (rare.Contains(RecruitBrawler))
                    bprice = 200;
                else if (super_rare.Contains(RecruitBrawler))
                    bprice = 350;
                else if (epic.Contains(RecruitBrawler))
                    bprice = 600;
                else if (mythic.Contains(RecruitBrawler))
                    bprice = 900;
                else if (legendary.Contains(RecruitBrawler))
                    bprice = 1500;
                else if (ultra_legendary.Contains(RecruitBrawler))
                    bprice = 2500;
                EventDailyOffer4_ItemsCount--;
                EventDailyOffer4_Price += bprice;
            }
            for (int x = 0; x < EventDailyOffer4_ItemsCount; x++)
            {
                int EventDailyOffer4_Random = random.Next(1, 6);
                int count = 0;
                EventDailyOffer4.Add(EventDailyOffer4_Random);
                switch (EventDailyOffer4_Random)
                {
                    case 1:
                        count = random.Next(1, 11);
                        EventDailyOffer4_Price += count * 20;
                        EventDailyOffer4.Add(count * 3);
                        break;
                    case 2:
                        count = random.Next(5, 11);
                        EventDailyOffer4_Price += count * 3;
                        EventDailyOffer4.Add(count * 50);
                        break;
                    case 3:
                        count = random.Next(5, 11);
                        EventDailyOffer4_Price += count * 3;
                        EventDailyOffer4.Add(count * 25);
                        break;
                    case 4:
                        count = random.Next(5, 11);
                        EventDailyOffer4_Price += count * 4;
                        EventDailyOffer4.Add(count * 200);
                        break;
                    case 5:
                        count = random.Next(5, 11);
                        EventDailyOffer4_Price += count * 2;
                        EventDailyOffer4.Add(count * 250);
                        break;
                }
            }
            EventDailyOffer4.Add(EventDailyOffer4_Price);



            if (UnlockedEventSkins.Count == 0)
            {
                EventDailyOfferSkin1.Add(2);
                EventDailyOfferSkin1.Add(100);
                EventDailyOfferSkin1.Add(0);
            }
            else
            {
                List<int> rareSkins = new List<int> { 591 };
                List<int> superrareSkins = new List<int> { 387, 412, 415, 557 };
                List<int> epicSkins = new List<int> { 216, 417, 587 };
                List<int> mythicSkins = new List<int> { 722, 727 };
                int eventskin = UnlockedEventSkins[random.Next(UnlockedEventSkins.Count)];
                int eventskinprice = 0;
                if (rareSkins.Contains(eventskin)) eventskinprice = 150;
                if (superrareSkins.Contains(eventskin)) eventskinprice = 350;
                if (epicSkins.Contains(eventskin)) eventskinprice = 550;
                if (mythicSkins.Contains(eventskin)) eventskinprice = 800;
                EventDailyOfferSkin1.Add(8);
                EventDailyOfferSkin1.Add(eventskin);
                EventDailyOfferSkin1.Add(eventskinprice);
                UnlockedEventSkins.Remove(eventskin);
            }

            if (UnlockedEventSkins.Count == 0)
            {
                EventDailyOfferSkin2.Add(2);
                EventDailyOfferSkin2.Add(100);
                EventDailyOfferSkin2.Add(0);
            }
            else
            {
                List<int> rareSkins = new List<int> { 591 };
                List<int> superrareSkins = new List<int> { 387, 412, 415, 557 };
                List<int> epicSkins = new List<int> { 216, 417, 587 };
                List<int> mythicSkins = new List<int> { 722, 727 };
                int eventskin = UnlockedEventSkins[random.Next(UnlockedEventSkins.Count)];
                int eventskinprice = 0;
                if (rareSkins.Contains(eventskin)) eventskinprice = 150;
                if (superrareSkins.Contains(eventskin)) eventskinprice = 350;
                if (epicSkins.Contains(eventskin)) eventskinprice = 550;
                if (mythicSkins.Contains(eventskin)) eventskinprice = 800;
                EventDailyOfferSkin2.Add(8);
                EventDailyOfferSkin2.Add(eventskin);
                EventDailyOfferSkin2.Add(eventskinprice);
                UnlockedEventSkins.Remove(eventskin);
            }

        }
        public void PurchaseOfferWithCatalog(int DataGlobalId, int Currency)
        {
            // bool BuySkin(int count)
            // {
            //     int availableBlings = HomeMode.Avatar.Blings;

            //     if (availableBlings >= count)
            //     {
            //         HomeMode.Avatar.Blings -= count;
            //         return true;
            //     }
            //     else
            //     {
            //         int gemsNeeded = (int)Math.Ceiling((count - availableBlings) / GeneralStaticLogic.GEMS_PER_BLING);
            //         HomeMode.Avatar.Blings = 0;
            //         if (!HomeMode.Avatar.UseDiamonds(gemsNeeded)) return false;
            //         return true;
            //     }
            // }

            // bool BuyByCoins(int count)
            // {
            //     if (count > HomeMode.Avatar.Gold)
            //     {
            //         HomeMode.Avatar.Gold = 0;
            //         int deficit = count - HomeMode.Avatar.Gold;
            //         if (!HomeMode.Avatar.UseDiamonds((int)Math.Ceiling(deficit * GeneralStaticLogic.GEMS_PER_COIN)))
            //         {
            //             return false;
            //         }
            //         return true;
            //     }
            //     HomeMode.Avatar.Gold -= count;
            //     return true;
            // }
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            if (DataGlobalId > 29000000 && DataGlobalId < 30000000)
            {
                SkinData skinData = DataTables.Get(DataType.Skin).GetDataWithId<SkinData>(DataGlobalId);
                if (skinData.Disabled || GeneralStaticLogic.BlockedObtainTypes.Contains(skinData.ObtainType)) return;
                // // if (Currency != 1) return;
                if (Currency == 0) // Coins 
                {
                    if (!HomeMode.Avatar.UseGold(skinData.PriceCoins)) return;
                    if (skinData.PriceCoins == 0) return;
                    if (skinData.PriceCoins == null) return;
                }
                else if (Currency == 1) // Gems
                {
                    if (!HomeMode.Avatar.UseDiamonds(skinData.PriceGems)) return;
                    if (skinData.PriceGems == 0) return;
                    if (skinData.PriceGems == null) return;
                }
                else if (Currency == 2) // Blings
                {
                    if (!HomeMode.Avatar.UseBlings(skinData.PriceBling)) return;
                    if (skinData.PriceBling == 0) return;
                    if (skinData.PriceBling == null) return;
                }
                else return;

                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(9);
                reward.SkinGlobalId = DataGlobalId;
                reward.Count = 1;
                unit.AddDrop(reward);

                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);
                NewCommand(skinData);
                SkinConfData conf = skinData.GetConf();
                Hero? hero = HomeMode.Avatar.GetHero(DataTables.GetCharacterByName(conf.Character).GetGlobalId());
                LogicSelectSkinCommand set = new();
                // set.SetSkinIdByServer(skinData.GetInstanceId());
                // if (hero != null && UnlockedSkins.Contains(skinData.GetGlobalId())) HomeMode.GameListener.SendMessage(new AvailableServerCommandMessage { Command = set }); set.Execute(HomeMode);



            }
            else if (DataGlobalId > 52000000 && DataGlobalId < 53000000)
            {
                EmoteData skinData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(DataGlobalId);
                if (!skinData.Name.EndsWith("team") && (skinData.Disabled || skinData.GiveOnSkinUnlock || skinData.PriceGems <= 0 || skinData.PriceBling <= 0)) return;
                if (Currency == 1) // Gems
                {
                    if (!HomeMode.Avatar.UseDiamonds(skinData.PriceGems)) return;
                    if (skinData.PriceGems == 0) return;
                    if (skinData.PriceGems == null) return;
                }
                else if (Currency == 2) // Blings
                {
                    if (!HomeMode.Avatar.UseBlings(skinData.PriceBling)) return;
                    if (skinData.PriceBling == 0) return;
                    if (skinData.PriceBling == null) return;
                }
                else return;

                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(11);
                reward.DataGlobalId = DataGlobalId;
                reward.Count = 1;
                unit.AddDrop(reward);
                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);
                if (skinData.EmoteType == "SAD" && skinData.Character != null)
                {
                    Hero hero = HomeMode.Avatar.GetHero(DataTables.GetCharacterByName(skinData.Character).GetGlobalId());
                    if (hero != null) hero.SelectedEmotes[2] = skinData.GetInstanceId();
                }
                if (skinData.EmoteType == "ANGRY" && skinData.Character != null)
                {
                    Hero hero = HomeMode.Avatar.GetHero(DataTables.GetCharacterByName(skinData.Character).GetGlobalId());
                    if (hero != null) hero.SelectedEmotes[3] = skinData.GetInstanceId();
                }
            }
            else if (DataGlobalId > 28000000 && DataGlobalId < 29000000)
            {
                PlayerThumbnailData skinData = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(DataGlobalId);
                if (!skinData.Name.StartsWith("player_icon_esports") && (skinData.Disabled || skinData.GiveOnSkinUnlock || skinData.PriceGems <= 0 || skinData.PriceBling <= 0 || skinData.LockedForChronos
                    || skinData.HideInCatalogWhenNotOwned)) return;
                // if (Currency != 1) return;
                Console.WriteLine("skinData.PriceGems = " + skinData.PriceGems);
                Console.WriteLine("skinData.PriceBling = " + skinData.PriceBling);
                if (Currency == 1) // Gems
                {
                    if (!HomeMode.Avatar.UseDiamonds(skinData.PriceGems)) return;
                    if (skinData.PriceGems == 0) return;
                    if (skinData.PriceGems == null) return;
                }
                else if (Currency == 2) // Blings
                {
                    return;
                    if (!HomeMode.Avatar.UseBlings(skinData.PriceBling)) return;
                    if (skinData.PriceBling == 0) return;
                    if (skinData.PriceBling == null) return;
                }
                else return;

                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(11);
                reward.DataGlobalId = DataGlobalId;
                reward.Count = 1;
                unit.AddDrop(reward);



                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);

            }
            else if (DataGlobalId > 68000000 && DataGlobalId < 69000000)
            {
                SprayData skinData = DataTables.Get(DataType.Spray).GetDataWithId<SprayData>(DataGlobalId);
                // if (Currency != 1) return;
                if (Currency == 1) // Gems
                {
                    if (!HomeMode.Avatar.UseDiamonds(skinData.PriceGems)) return;
                    if (skinData.PriceGems == 0) return;
                    if (skinData.PriceGems == null) return;
                }
                else if (Currency == 2) // Blings
                {
                    if (!HomeMode.Avatar.UseBlings(skinData.PriceBling)) return;
                    if (skinData.PriceBling == 0) return;
                    if (skinData.PriceBling == null) return;
                }
                else return;

                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(11);
                reward.DataGlobalId = DataGlobalId;
                reward.Count = 1;
                unit.AddDrop(reward);



                command.DeliveryUnits.Add(unit);
                command.Execute(HomeMode);
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);
                if (skinData.Character != null)
                {
                    Hero hero = HomeMode.Avatar.GetHero(DataTables.GetCharacterByName(skinData.Character).GetGlobalId());
                    if (hero != null && hero.SelectedSpray == -1) hero.SelectedSpray = skinData.GetInstanceId();
                }
            }

            void NewCommand(SkinData skinData)
            {
                if (skinData == null) return;
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(9);
                foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                {

                    if (emoteData.Skin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }

                }
                foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                {
                    if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }
                }

                foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                {
                    if (sprayData.Skin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.Spray).GetData<SprayData>(sprayData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }
                }
                command.DeliveryUnits.Add(unit);
                if (command.DeliveryUnits.Count >= 0)
                {
                    command.Execute(HomeMode);

                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    HomeMode.GameListener.SendMessage(message);
                }


            }
        }


public bool ForcePurchaseOfferByClaim(string claim, string playerTag, HomeMode targetHomeMode)
{
    try
    {
        if (targetHomeMode == null || targetHomeMode.Home == null)
        {
            Console.WriteLine($"[ERROR] Игрок с тегом {playerTag} не найден или не в сети");
            return false;
        }
        
        var targetHome = targetHomeMode.Home;
        
        // Ищем оффер по Claim в целевой домашней странице
        var bundle = targetHome.OfferBundles.FirstOrDefault(b => b.Claim == claim);
        
        if (bundle == null)
        {
            Console.WriteLine($"[ERROR] Оффер с claim '{claim}' не найден у игрока {playerTag}");
            return false;
        }
        
        // Если уже куплен, всё равно выдаём (админская команда)
        if (!bundle.Purchased)
        {
            bundle.Purchased = true;
            if (bundle.Claim != "debug" && bundle.Claim != "brawler_switch" && bundle.Claim != "brawler_switch_clear") 
                targetHome.OffersClaimed.Add(bundle.Claim);
        }
        
        Console.WriteLine($"[INFO] Выдача оффера '{claim}' игроку {playerTag}");
        Console.WriteLine($"[INFO] Название: {bundle.Title}");
        
        // Выдаём награды
        LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        int rewardsGiven = 0;
        
        foreach (Offer offer in bundle.Items)
        {
            // Обработка разных типов наград
            if (offer.Type == ShopItem.Gems)
            {
                targetHomeMode.Avatar.Diamonds += offer.Count;
                Console.WriteLine($"[INFO] Выдано {offer.Count} гемов");
                rewardsGiven++;
            }
            else if (offer.Type == ShopItem.Coin)
            {
                targetHomeMode.Avatar.Gold += offer.Count;
                Console.WriteLine($"[INFO] Выдано {offer.Count} монет");
                rewardsGiven++;
            }
            else if (offer.Type == ShopItem.PowerPoint)
            {
                targetHomeMode.Avatar.PowerPoints += offer.Count;
                Console.WriteLine($"[INFO] Выдано {offer.Count} очков силы");
                rewardsGiven++;
            }
            else if (offer.Type == ShopItem.Bling)
            {
                targetHomeMode.Avatar.Blings += offer.Count;
                Console.WriteLine($"[INFO] Выдано {offer.Count} блингов");
                rewardsGiven++;
            }
            else if (offer.Type == ShopItem.Emote)
            {
                int emoteGlobalId = GlobalId.CreateGlobalId(52, offer.SkinDataId);
                if (!targetHome.UnlockedEmotes.Contains(emoteGlobalId))
                {
                    targetHome.UnlockedEmotes.Add(emoteGlobalId);
                    Console.WriteLine($"[INFO] Выдан пин {emoteGlobalId} (ID: {offer.SkinDataId})");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = emoteGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else
                {
                    Console.WriteLine($"[INFO] Пин {emoteGlobalId} уже есть у игрока");
                }
            }
            else if (offer.Type == ShopItem.PlayerTitle)
            {
                int titleGlobalId = GlobalId.CreateGlobalId(76, offer.SkinDataId);
                if (!targetHome.UnlockedTituls.Contains(titleGlobalId))
                {
                    targetHome.UnlockedTituls.Add(titleGlobalId);
                    Console.WriteLine($"[INFO] Выдан титул {titleGlobalId} (ID: {offer.SkinDataId})");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = titleGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else
                {
                    Console.WriteLine($"[INFO] Титул {titleGlobalId} уже есть у игрока");
                }
            }
            else if (offer.Type == ShopItem.Spray)
            {
                int sprayGlobalId = GlobalId.CreateGlobalId(68, offer.SkinDataId);
                if (!targetHome.UnlockedSprays.Contains(sprayGlobalId))
                {
                    targetHome.UnlockedSprays.Add(sprayGlobalId);
                    Console.WriteLine($"[INFO] Выдан спрей {sprayGlobalId} (ID: {offer.SkinDataId})");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = sprayGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
            }
            else if (offer.Type == ShopItem.PlayerThumbnail)
            {
                int thumbnailGlobalId = GlobalId.CreateGlobalId(28, offer.SkinDataId);
                if (!targetHome.UnlockedThumbnails.Contains(thumbnailGlobalId))
                {
                    targetHome.UnlockedThumbnails.Add(thumbnailGlobalId);
                    Console.WriteLine($"[INFO] Выдана иконка {thumbnailGlobalId} (ID: {offer.SkinDataId})");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = thumbnailGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
            }
            else if (offer.Type == ShopItem.Skin)
            {
                int skinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                if (!targetHome.UnlockedSkins.Contains(skinGlobalId))
                {
                    targetHome.UnlockedSkins.Add(skinGlobalId);
                    Console.WriteLine($"[INFO] Выдан скин {skinGlobalId} (ID: {offer.SkinDataId})");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = skinGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    
                    // Выдача связанных эмоций и спреев
                    SkinData skinData = DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(skinGlobalId);
                    if (skinData != null)
                    {
                        foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                        {
                            if (emoteData.Skin == skinData.Name && !targetHome.UnlockedEmotes.Contains(emoteData.GetGlobalId()))
                            {
                                targetHome.UnlockedEmotes.Add(emoteData.GetGlobalId());
                                GatchaDrop reward1 = new GatchaDrop(11);
                                reward1.DataGlobalId = emoteData.GetGlobalId();
                                reward1.Count = 1;
                                unit.AddDrop(reward1);
                            }
                        }
                        
                        foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                        {
                            if (sprayData.Skin == skinData.Name && !targetHome.UnlockedSprays.Contains(sprayData.GetGlobalId()))
                            {
                                targetHome.UnlockedSprays.Add(sprayData.GetGlobalId());
                                GatchaDrop reward1 = new GatchaDrop(11);
                                reward1.DataGlobalId = sprayData.GetGlobalId();
                                reward1.Count = 1;
                                unit.AddDrop(reward1);
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"[INFO] Скин {skinGlobalId} уже есть у игрока");
                }
            }
            else if (offer.Type == ShopItem.SkinAndHero)
            {
                int skinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                int heroGlobalId = GlobalId.CreateGlobalId(16, offer.ItemDataId);
                
                if (!targetHome.UnlockedSkins.Contains(skinGlobalId))
                {
                    targetHome.UnlockedSkins.Add(skinGlobalId);
                    Console.WriteLine($"[INFO] Выдан скин {skinGlobalId}");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = skinGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                
                if (!targetHomeMode.Avatar.HasHero(heroGlobalId))
                {
                    targetHomeMode.Avatar.UnlockHero(heroGlobalId);
                    Console.WriteLine($"[INFO] Выдан герой {heroGlobalId}");
                    rewardsGiven++;
                }
            }
            else if (offer.Type == ShopItem.GuaranteedHero || offer.Type == ShopItem.GuaranteedHeroWithLevel)
            {
                int heroGlobalId = GlobalId.CreateGlobalId(16, offer.ItemDataId);
                if (!targetHomeMode.Avatar.HasHero(heroGlobalId))
                {
                    targetHomeMode.Avatar.UnlockHero(heroGlobalId);
                    Console.WriteLine($"[INFO] Выдан герой {heroGlobalId}");
                    rewardsGiven++;
                    
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(1);
                    reward.DataGlobalId = heroGlobalId;
                    reward.Count = 1;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else
                {
                    Console.WriteLine($"[INFO] Герой {heroGlobalId} уже есть у игрока");
                }
            }
            else if (offer.Type == ShopItem.RecruitToken)
            {
                targetHome.RecruitTokens += offer.Count;
                Console.WriteLine($"[INFO] Выдано {offer.Count} рекруит токенов");
                rewardsGiven++;
            }
            else if (offer.Type == ShopItem.KeyResource || offer.Type == ShopItem.KeyBrawlers || 
                     offer.Type == ShopItem.KeySkins || offer.Type == ShopItem.KeyBuffies)
            {
                for (int x = 0; x < offer.Count; x++)
                {
                    int unitType = 15;
                    if (offer.Type == ShopItem.KeyBrawlers) unitType = 17;
                    else if (offer.Type == ShopItem.KeySkins) unitType = 16;
                    else if (offer.Type == ShopItem.KeyBuffies) unitType = 20;
                    
                    DeliveryUnit unit = new DeliveryUnit(unitType);
                    targetHomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                    Console.WriteLine($"[INFO] Выдан ключ типа {offer.Type}");
                    rewardsGiven++;
                }
            }
            else if (offer.Type == ShopItem.ChaosDrop)
            {
                for (int x = 0; x < offer.Count; x++)
                {
                    DeliveryUnit unit = new DeliveryUnit(18);
                    targetHomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                    Console.WriteLine($"[INFO] Выдан хаосдроп");
                    rewardsGiven++;
                }
            }
            else if (offer.Type == ShopItem.UltraChaosDrop)
            {
                for (int x = 0; x < offer.Count; x++)
                {
                    DeliveryUnit unit = new DeliveryUnit(19);
                    targetHomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                    Console.WriteLine($"[INFO] Выдан ультра-хаосдроп");
                    rewardsGiven++;
                }
            }
            else if (offer.Type == ShopItem.EventGatcha)
            {
                for (int x = 0; x < offer.Count; x++)
                {
                    DeliveryUnit unit = new DeliveryUnit(14);
                    targetHomeMode.SimulateGatcha(unit);
                    command.DeliveryUnits.Add(unit);
                    Console.WriteLine($"[INFO] Выдан ивентовый гатча");
                    rewardsGiven++;
                }
            }
            else
            {
                Console.WriteLine($"[WARN] Неподдерживаемый тип награды: {offer.Type}");
            }
        }
        
        // Отправляем команду выдачи наград
        if (command.DeliveryUnits.Count > 0)
        {
            command.Execute(targetHomeMode);
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            targetHomeMode.GameListener.SendMessage(message);
        }
        
        // Обновляем офферы
        targetHome.ChangeOffers();
        
        Console.WriteLine($"[INFO] Оффер '{claim}' успешно выдан игроку {playerTag}. Выдано наград: {rewardsGiven}");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Ошибка при выдаче оффера '{claim}' игроку {playerTag}: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        return false;
    }
}




        // public async void PurchaseOffer(int index)
        // {
        //     if (index < 0 || index >= OfferBundles.Count) return;

        //     OfferBundle bundle = OfferBundles[index];
        //     if (bundle.Purchased) return;

        //     if (bundle.Currency == 0)
        //     {
        //         HomeMode.Avatar.UseDiamonds(bundle.Cost);
        //     }
        //     else if (bundle.Currency == 1)
        //     {
        //         HomeMode.Avatar.UseGold(bundle.Cost);
        //     }
        //     else if (bundle.Currency == 6)
        //     {
        //         HomeMode.Avatar.UseBlings(bundle.Cost);
        //     }

        //     bundle.Purchased = true;

        //     if (bundle.Claim == "debug" || bundle.Claim == "brawler_switch" || bundle.Claim == "brawler_switch_clear")
        //     {
        //         ;
        //     }
        //     else
        //     {
        //         OffersClaimed.Add(bundle.Claim);
        //     }

        //     Random rand = new Random();
        //     foreach (Offer offer in bundle.Items)
        //         if (offer.Type == ShopItem.RandomEmotes)
        //         {

        //             List<int> Emotes_All = new List<int>();
        //             foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas())
        //             {
        //                 if (!emote.Disabled && emote.Skin == null && emote.Character != null && !UnlockedEmotes.Contains(emote.GetGlobalId()))
        //                 {
        //                     CharacterData brawler = DataTables.Get(DataType.Character).GetData<CharacterData>(emote.Character);

        //                     if (!HomeMode.Avatar.HasHero(brawler.GetGlobalId()))
        //                     {
        //                         Emotes_All.Add(emote.GetInstanceId());
        //                     }
        //                 }
        //             }
        //             List<int> Emotes_Locked = Emotes_All.Except(UnlockedEmotes).OrderBy(x => Guid.NewGuid()).Take(3).ToList();

        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);



        //             foreach (int x in Emotes_Locked)
        //             {
        //                 GatchaDrop reward = new GatchaDrop(11);
        //                 reward.Count = 1;
        //                 reward.DataGlobalId = GlobalId.CreateGlobalId(52, x);
        //                 unit.AddDrop(reward);
        //                 UnlockedEmotes.Add(GlobalId.CreateGlobalId(52, x));
        //             }
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.RandomEmotesForBrawler)
        //         {
        //             CharacterData brawler = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(offer.ItemDataId);
        //             List<int> Emotes_All = new List<int>();
        //             foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas())
        //             {
        //                 if (!emote.Disabled && emote.Skin == null && emote.Character == brawler.Name && emote.Rarity != "COLLECTORS" && !UnlockedEmotes.Contains(emote.GetGlobalId()))
        //                 {
        //                     Emotes_All.Add(emote.GetInstanceId());
        //                 }
        //             }
        //             List<int> Emotes_Locked = Emotes_All.Except(UnlockedEmotes).OrderBy(x => Guid.NewGuid()).Take(Emotes_All.Count).ToList();

        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);



        //             foreach (int x in Emotes_Locked)
        //             {
        //                 GatchaDrop reward = new GatchaDrop(11);
        //                 reward.Count = 1;
        //                 reward.DataGlobalId = GlobalId.CreateGlobalId(52, x);
        //                 unit.AddDrop(reward);
        //                 UnlockedEmotes.Add(GlobalId.CreateGlobalId(52, x));
        //             }
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.RandomEmoteOfRarity)
        //         {
        //             List<int> Emotes_All = new List<int>();
        //             string EmoteRarity = "DEFAULT";
        //             switch (offer.SkinDataId)
        //             {
        //                 case 0:
        //                     EmoteRarity = "COMMON";
        //                     break;
        //                 case 1:
        //                     EmoteRarity = "RARE";
        //                     break;
        //                 case 2:
        //                     EmoteRarity = "EPIC";
        //                     break;

        //             }

        //             foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas())
        //             {
        //                 if (!emote.Disabled && emote.Skin == null && emote.Character != null && emote.Rarity == EmoteRarity && emote.Rarity !="DEFAULT")
        //                 {
        //                     Emotes_All.Add(emote.GetInstanceId());
        //                 }
        //             }
        //             List<int> Emotes_Locked = Emotes_All.Except(UnlockedEmotes).OrderBy(x => Guid.NewGuid()).Take(1).ToList();

        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);



        //             foreach (int x in Emotes_Locked)
        //             {
        //                 GatchaDrop reward = new GatchaDrop(11);
        //                 reward.Count = 1;
        //                 reward.DataGlobalId = GlobalId.CreateGlobalId(52, x);
        //                 unit.AddDrop(reward);
        //                 UnlockedEmotes.Add(GlobalId.CreateGlobalId(52, x));
        //             }
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.RarityStarrDrop)
        //         {

        //             if (offer.SkinDataId != -1)
        //             {
        //                 StarrDrop.GenerateRarityDrop(HomeMode, offer.SkinDataId);
        //                 LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();

        //                 logicRefreshRandomRewardsCommand.Rarity = offer.SkinDataId;
        //                 logicRefreshRandomRewardsCommand.Количество = 1;
        //                 logicRefreshRandomRewardsCommand.Execute(HomeMode);

        //                 AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
        //                 message25.Command = logicRefreshRandomRewardsCommand;
        //                 HomeMode.GameListener.SendMessage(message25);

        //                 LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
        //                 DeliveryUnit unit = new DeliveryUnit(100);

        //                 GatchaDrop drop = new GatchaDrop(HomeMode.Home.StarrDrop.data.Type);

        //                 if (HomeMode.Home.StarrDrop.data.Type != 4) drop.DataGlobalId = HomeMode.Home.StarrDrop.data.DataGlobalID;
        //                 else drop.CardGlobalId = HomeMode.Home.StarrDrop.data.DataGlobalID;
        //                 if (HomeMode.Home.StarrDrop.data.Type != 1) drop.SkinGlobalId = HomeMode.Home.StarrDrop.data.SkinGlobalID;
        //                 drop.Count = HomeMode.Home.StarrDrop.data.Ammount;
        //                 unit.AddDrop(drop);

        //                 command123.StarrDropExecute = true;
        //                 command123.DeliveryUnits.Add(unit);
        //                 command123.Execute(HomeMode);
        //                 AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //                 message.Command = command123;
        //                 HomeMode.GameListener.SendMessage(message);

        //                 LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand1 = new LogicRefreshRandomRewardsCommand();

        //                 logicRefreshRandomRewardsCommand1.Disable = true;
        //                 logicRefreshRandomRewardsCommand1.Execute(HomeMode);

        //                 AvailableServerCommandMessage message251 = new AvailableServerCommandMessage();
        //                 message251.Command = logicRefreshRandomRewardsCommand1;
        //                 HomeMode.GameListener.SendMessage(message251);
        //             }
        //             else
        //             {
        //                 StarrDrop.GenerateDrop(HomeMode);
        //                 LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();

        //                 logicRefreshRandomRewardsCommand.Rarity = 0;
        //                 logicRefreshRandomRewardsCommand.Количество = 4;
        //                 logicRefreshRandomRewardsCommand.Execute(HomeMode);

        //                 AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
        //                 message25.Command = logicRefreshRandomRewardsCommand;
        //                 HomeMode.GameListener.SendMessage(message25);

        //                 LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
        //                 DeliveryUnit unit = new DeliveryUnit(100);

        //                 GatchaDrop drop = new GatchaDrop(HomeMode.Home.StarrDrop.data.Type);

        //                 if(HomeMode.Home.StarrDrop.data.Type != 4)drop.DataGlobalId = HomeMode.Home.StarrDrop.data.DataGlobalID;
        //                 else drop.CardGlobalId = HomeMode.Home.StarrDrop.data.DataGlobalID;
        //                 if (HomeMode.Home.StarrDrop.data.Type != 1) drop.SkinGlobalId = HomeMode.Home.StarrDrop.data.SkinGlobalID;
        //                 drop.Count = HomeMode.Home.StarrDrop.data.Ammount;
        //                 unit.AddDrop(drop);

        //                 command123.StarrDropExecute = true;
        //                 command123.DeliveryUnits.Add(unit);
        //                 command123.Execute(HomeMode);
        //                 AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //                 message.Command = command123;
        //                 HomeMode.GameListener.SendMessage(message);

        //                 LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand1 = new LogicRefreshRandomRewardsCommand();

        //                 logicRefreshRandomRewardsCommand1.Disable = true;
        //                 logicRefreshRandomRewardsCommand1.Execute(HomeMode);

        //                 AvailableServerCommandMessage message251 = new AvailableServerCommandMessage();
        //                 message251.Command = logicRefreshRandomRewardsCommand1;
        //                 HomeMode.GameListener.SendMessage(message251);
        //             }
        //             Console.WriteLine(StarrDrop.data.Type);
        //             Console.WriteLine(StarrDrop.data.Ammount);
        //             Console.WriteLine(StarrDrop.data.SkinGlobalID);
        //             Console.WriteLine(StarrDrop.data.DataGlobalID);
        //         }
        //         else if (offer.Type == ShopItem.Coin)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(7);
        //             reward.Count = offer.Count;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.Gems)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(8);
        //             reward.Count = offer.Count;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.Bling)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(4);
        //             reward.Count = 1;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.PowerPoint)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(24);
        //             reward.Count = offer.Count;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.GuaranteedHero)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             if (bundle.Claim == "brawler_switch"){
        //                 NewRecruitBrawler = offer.ItemDataId - 16000000;
        //                 LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
        //                 commands.Execute(HomeMode);
        //                 AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
        //                 serverCommandMessage.Command = commands;
        //                 HomeMode.GameListener.SendMessage(serverCommandMessage);
        //                 DeliveryUnit unit = new DeliveryUnit(100);
        //                 GatchaDrop reward = new GatchaDrop(8);
        //                 reward.Count = 0;
        //                 unit.AddDrop(reward);
        //                 command.DeliveryUnits.Add(unit);
        //             }
        //             else if (bundle.Claim == "brawler_switch_clear"){
        //                 NewRecruitBrawler = 0;
        //                 LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
        //                 commands.Execute(HomeMode);
        //                 AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
        //                 serverCommandMessage.Command = commands;
        //                 HomeMode.GameListener.SendMessage(serverCommandMessage);
        //                 DeliveryUnit unit = new DeliveryUnit(100);
        //                 GatchaDrop reward = new GatchaDrop(8);
        //                 reward.Count = 0;
        //                 unit.AddDrop(reward);
        //                 command.DeliveryUnits.Add(unit);
        //             }
        //             else {
        //                 DeliveryUnit unit = new DeliveryUnit(100);
        //                 GatchaDrop reward = new GatchaDrop(1);
        //                 reward.Count = offer.Count;
        //                 reward.DataGlobalId = offer.ItemDataId;
        //                 unit.AddDrop(reward);
        //                 command.DeliveryUnits.Add(unit);
        //             }
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.PlayerThumbnail)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(11);
        //             reward.DataGlobalId = GlobalId.CreateGlobalId(28, offer.SkinDataId);
        //             reward.Count = offer.Count;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.Skin)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(9);
        //             reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
        //             reward.Count = 1;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //             NewCommand(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(offer.SkinDataId), command);
        //         }
        //         else if (offer.Type == ShopItem.Emote)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);
        //             GatchaDrop reward = new GatchaDrop(11);
        //             reward.DataGlobalId = GlobalId.CreateGlobalId(52, offer.SkinDataId);
        //             reward.Count = 1;
        //             unit.AddDrop(reward);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else if (offer.Type == ShopItem.RecruitToken)
        //         {
        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
        //             DeliveryUnit unit = new DeliveryUnit(100);

        //             GatchaDrop drop = new GatchaDrop(22);
        //             drop.Count = offer.Count;
        //             unit.AddDrop(drop);
        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);

        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);
        //         }
        //         else
        //         {
        //             LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
        //             logicAddNotificationCommand.Notification = new FloaterTextNotification("ShopItem not initialized\nPlease contact the developers with this problem.");
        //             AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
        //             availableServerCommandMessage.Command = logicAddNotificationCommand;
        //             HomeMode.GameListener.SendMessage(availableServerCommandMessage);

        //             LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();

        //             DeliveryUnit unit = new DeliveryUnit(100);

        //             command.DeliveryUnits.Add(unit);
        //             command.Execute(HomeMode);
        //             AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //             message.Command = command;
        //             HomeMode.GameListener.SendMessage(message);

        //         }

        //         LogicStarroadRefreshCommand wqfwpjfpqjwpfjo5 = new();
        //         wqfwpjfpqjwpfjo5.Execute(HomeMode);
        //         AvailableServerCommandMessage skofskdfoskdfskfopwqjou = new()
        //         {
        //             Command = wqfwpjfpqjwpfjo5
        //         };
        //         HomeMode.GameListener.SendMessage(skofskdfoskdfskfopwqjou);
        //         RefreshOffers();
        //         LogicOffersChangedMessage refreshoffers = new LogicOffersChangedMessage();
        //         refreshoffers.OfferBundles = OfferBundles;
        //         HomeMode.GameListener.SendMessage(refreshoffers);

        //     void NewCommand(SkinData skinData, LogicGiveDeliveryItemsCommand command)
        //     {
        //         if (skinData == null) return;
        //         DeliveryUnit unit = new DeliveryUnit(100);
        //         GatchaDrop reward = new GatchaDrop(9);
        //         foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
        //         {

        //             if (emoteData.Skin == skinData.Name)
        //             {
        //                 GatchaDrop reward1 = new GatchaDrop(11);
        //                 reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
        //                 reward1.Count = 1;
        //                 unit.AddDrop(reward1);
        //             }

        //         }
        //         foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
        //         {
        //             if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
        //             {
        //                 GatchaDrop reward1 = new GatchaDrop(11);
        //                 reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
        //                 reward1.Count = 1;
        //                 unit.AddDrop(reward1);
        //             }

        //         }
        //         command.DeliveryUnits.Add(unit);
        //         command.Execute(HomeMode);

        //         AvailableServerCommandMessage message = new AvailableServerCommandMessage();
        //         message.Command = command;
        //         HomeMode.GameListener.SendMessage(message);

        //     }
        // } 

        public void PurchaseOffer(int index)
        {
            if (index < 0 || index >= OfferBundles.Count) return;

            var event_wins = new List<int>{
                0, 0,1, 5, 10, 20, 30,
                50, 75, 100, 125, 150,
                180, 210, 240, 270, 300
            };

            OfferBundle bundle = OfferBundles[index]; // offer_event_bad_paid_1

            // Custom offers created through the Telegram admin bot are one-time offers.
            // Check the persisted claim as well as the transient Purchased flag so a shop
            // refresh or a stale client request cannot purchase the same offer again.
            if (bundle.Claim?.StartsWith("custom_offer_", StringComparison.Ordinal) == true &&
                OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
                return;
            }

            if (bundle.Purchased ||
                (bundle.Claim == "dailycallendar_1" && ActivityDays < 1) ||
                (bundle.Claim == "dailycallendar_2" && ActivityDays < 2) ||
                (bundle.Claim == "dailycallendar_3" && ActivityDays < 3) ||
                (bundle.Claim == "dailycallendar_4" && ActivityDays < 4) ||
                (bundle.Claim == "dailycallendar_5" && ActivityDays < 5) ||
                (bundle.Claim == "dailycallendar_6" && ActivityDays < 6) ||
                (bundle.Claim == "dailycallendar_7" && ActivityDays < 7) ||
                (bundle.Claim == "dailycallendar_8" && ActivityDays < 8) ||
                (bundle.Claim == "dailycallendar_9" && ActivityDays < 9) ||
                (bundle.Claim == "dailycallendar_10" && ActivityDays < 10)

            ) return;

            if (
                (bundle.Claim == "offer_event_bad_paid_2" && HomeMode.Avatar.EventWins < event_wins[2]) ||
                (bundle.Claim == "offer_event_bad_paid_3" && HomeMode.Avatar.EventWins < event_wins[3]) ||
                (bundle.Claim == "offer_event_bad_paid_4" && HomeMode.Avatar.EventWins < event_wins[4]) ||
                (bundle.Claim == "offer_event_bad_paid_5" && HomeMode.Avatar.EventWins < event_wins[5]) ||
                (bundle.Claim == "offer_event_bad_paid_6" && HomeMode.Avatar.EventWins < event_wins[6]) ||
                (bundle.Claim == "offer_event_bad_paid_7" && HomeMode.Avatar.EventWins < event_wins[7]) ||
                (bundle.Claim == "offer_event_bad_paid_8" && HomeMode.Avatar.EventWins < event_wins[8]) ||
                (bundle.Claim == "offer_event_bad_paid_9" && HomeMode.Avatar.EventWins < event_wins[9]) ||
                (bundle.Claim == "offer_event_bad_paid_10" && HomeMode.Avatar.EventWins < event_wins[10]) ||
                (bundle.Claim == "offer_event_bad_paid_11" && HomeMode.Avatar.EventWins < event_wins[11]) ||
                (bundle.Claim == "offer_event_bad_paid_12" && HomeMode.Avatar.EventWins < event_wins[12]) ||
                (bundle.Claim == "offer_event_bad_paid_13" && HomeMode.Avatar.EventWins < event_wins[13]) ||
                (bundle.Claim == "offer_event_bad_paid_14" && HomeMode.Avatar.EventWins < event_wins[14]) ||
                (bundle.Claim == "offer_event_bad_paid_15" && HomeMode.Avatar.EventWins < event_wins[15]) ||
                (bundle.Claim == "offer_event_bad_paid_16" && HomeMode.Avatar.EventWins < event_wins[16]) ||
                (bundle.Claim == "offer_event_bad_free_2" && HomeMode.Avatar.EventWins < event_wins[2]) ||
                (bundle.Claim == "offer_event_bad_free_3" && HomeMode.Avatar.EventWins < event_wins[3]) ||
                (bundle.Claim == "offer_event_bad_free_4" && HomeMode.Avatar.EventWins < event_wins[4]) ||
                (bundle.Claim == "offer_event_bad_free_5" && HomeMode.Avatar.EventWins < event_wins[5]) ||
                (bundle.Claim == "offer_event_bad_free_6" && HomeMode.Avatar.EventWins < event_wins[6]) ||
                (bundle.Claim == "offer_event_bad_free_7" && HomeMode.Avatar.EventWins < event_wins[7]) ||
                (bundle.Claim == "offer_event_bad_free_8" && HomeMode.Avatar.EventWins < event_wins[8]) ||
                (bundle.Claim == "offer_event_bad_free_9" && HomeMode.Avatar.EventWins < event_wins[9]) ||
                (bundle.Claim == "offer_event_bad_free_10" && HomeMode.Avatar.EventWins < event_wins[10]) ||
                (bundle.Claim == "offer_event_bad_free_11" && HomeMode.Avatar.EventWins < event_wins[11]) ||
                (bundle.Claim == "offer_event_bad_free_12" && HomeMode.Avatar.EventWins < event_wins[12]) ||
                (bundle.Claim == "offer_event_bad_free_13" && HomeMode.Avatar.EventWins < event_wins[13]) ||
                (bundle.Claim == "offer_event_bad_free_14" && HomeMode.Avatar.EventWins < event_wins[14]) ||
                (bundle.Claim == "offer_event_bad_free_15" && HomeMode.Avatar.EventWins < event_wins[15]) ||
                (bundle.Claim == "offer_event_bad_free_16" && HomeMode.Avatar.EventWins < event_wins[16]))
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "Для получения награды нужно больше побед!";
                HomeMode.GameListener.SendMessage(loginFailed);
                return;
            }

            if (bundle.Currency == 0)
            {
                if (!HomeMode.Avatar.UseDiamonds(bundle.Cost)) return;
            }
            else if (bundle.Currency == 1)
            {
                if (!HomeMode.Avatar.UseGold(bundle.Cost)) return;
            }
            else if (bundle.Currency == 3)
            {
                if (!HomeMode.Avatar.UseStarPoints(bundle.Cost)) return;
            }

            if (bundle.StartTime > DateTime.Now) return;

            bundle.Purchased = true;

            if (bundle.Claim == "debug_dayupdate")
            {
                Day = "0";
                DayUpdate();
            }
            else if (bundle.Claim == "debug_out")
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 1;
                loginFailed.Message = "Успешно!";
                HomeMode.GameListener.SendMessage(loginFailed);
                return;
            }
            else if (bundle.Claim == "brawler_switch")
            {
                ;
            }
            else if (bundle.Claim == "brawler_switch_clear")
            {
                ;
            }
            else if (bundle.Claim == "EventTokensComp")
            {
                EventTokensComp = 0;
            }
            else if (bundle.Claim == "KeyResource") KeysBuyed[0] = KeysBuyed[0] + 1;
            else if (bundle.Claim == "KeyBrawlers") KeysBuyed[1] = KeysBuyed[1] + 1;
            else if (bundle.Claim == "KeySkins") KeysBuyed[2] = KeysBuyed[2] + 1;
            else if (bundle.Claim == "KeyPass") KeysBuyed[3] = KeysBuyed[3] + 1;
            else if (bundle.Claim == "event_giftshop_gatcha_1"){
                EventOfferData[0] = EventOfferData[0] + 1;
                if (EventOfferData[0] == 5) OffersClaimed.Add(bundle.Claim);
            } 
            else if (bundle.Claim == "event_giftshop_gatcha_2"){
                EventOfferData[1] = EventOfferData[1] + 1;
                if (EventOfferData[1] == 5) OffersClaimed.Add(bundle.Claim);
            } 
            else if (bundle.Claim == "event_giftshop_gatcha_3"){
                EventOfferData[2] = EventOfferData[2] + 1;
                if (EventOfferData[2] == 5) OffersClaimed.Add(bundle.Claim);
            } 
            else if (bundle.Claim == "event_giftshop_gatcha_4"){
                EventOfferData[3] = EventOfferData[3] + 1;
                if (EventOfferData[3] == 5) OffersClaimed.Add(bundle.Claim);
            } 
            else if (bundle.Claim == "event_giftshop_gatcha_5"){
                EventOfferData[4] = EventOfferData[4] + 1;
                if (EventOfferData[4] == 5) OffersClaimed.Add(bundle.Claim);
            } 
            else
            {
                if (bundle.Claim != "debug") OffersClaimed.Add(bundle.Claim);
            }

            bool isStarrDrop = false;


            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            Random rand = new Random();
            foreach (Offer offer in bundle.Items)
            {

                if (offer.Type == ShopItem.EventGatcha)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(14);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.ChaosDrop)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(18);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.UltraChaosDrop)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(19);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.KeyResource)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(15);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.KeyBuffies)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(20);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.KeySkins)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(16);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.KeyBrawlers)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(17);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.EventTokens)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(12);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.HeroPower)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(6);
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.BigBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(12);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.MegaBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(11);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.GigaBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(13);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.ZombieBox)
                {
                    for (int x = 0; x < offer.Count; x++)
                    {
                        DeliveryUnit unit = new DeliveryUnit(11);
                        HomeMode.SimulateGatcha(unit);
                        if (x + 1 != offer.Count)
                        {
                            command.Execute(HomeMode);
                        }
                        command.DeliveryUnits.Add(unit);
                    }
                }
                else if (offer.Type == ShopItem.Skin || offer.Type == ShopItem.SkinAndHero)
                {

                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    reward.SkinGlobalId = GlobalId.CreateGlobalId(29, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                    SkinData skinData = DataTables.Get(DataType.Skin).GetDataWithId<SkinData>(GlobalId.CreateGlobalId(29, offer.SkinDataId));
                    NewCommand(skinData);
                }
                else if (offer.Type == ShopItem.Gems)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Item)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(4);
                    reward.Count = offer.Count;
                    reward.DataGlobalId = offer.ItemDataId;
                    reward.CardGlobalId = offer.SkinDataId;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Bling)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(25);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.BrawlPassBundle)
                {
                    HasPremiumPass = true;
                }
                else if (offer.Type == ShopItem.BrawlPassPlusBundle)
                {
                    HasPremiumPass = true;
                    HasPremiumPassPlus = true;
                }
                else if (offer.Type == ShopItem.Emote)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(52, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
               else if (offer.Type == ShopItem.PlayerTitle)
{
    int titleFullId = GlobalId.CreateGlobalId(76, offer.SkinDataId);
    
    // Добавляем в список разблокированных титулов
    if (!UnlockedTituls.Contains(titleFullId))
    {
        UnlockedTituls.Add(titleFullId);
        Console.WriteLine($"[TITLE] Добавлен титул {titleFullId} (Instance: {offer.SkinDataId}) игроку {HomeMode.Avatar.Name}");
    }
    
    // Создаём визуальную награду (для отображения в окне получения)
    DeliveryUnit unit = new DeliveryUnit(100);
    GatchaDrop reward = new GatchaDrop(11);
    reward.DataGlobalId = titleFullId;
    reward.Count = offer.Count;
    unit.AddDrop(reward);
    command.DeliveryUnits.Add(unit);
}
                else if (offer.Type == ShopItem.Spray)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(68, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.PlayerThumbnail)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(11);
                    reward.DataGlobalId = GlobalId.CreateGlobalId(28, offer.SkinDataId);
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.PowerPoint)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(24);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Coin)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(7);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.Candy)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(12);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.RandomStarrDrop)
                {
                    LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
                    logicRefreshRandomRewardsCommand.Execute(HomeMode);

                    LogicGiveDeliveryItemsCommand command123 = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);





                    command123.StarrDropExecute = true;
                    command123.DeliveryUnits.Add(unit);
                    command123.Execute(HomeMode);

                    AvailableServerCommandMessage message1 = new AvailableServerCommandMessage();
                    message1.Command = command123;
                    HomeMode.GameListener.SendMessage(message1);

                    AvailableServerCommandMessage message25 = new AvailableServerCommandMessage();
                    message25.Command = logicRefreshRandomRewardsCommand;
                    HomeMode.GameListener.SendMessage(message25);


                }
                else if (offer.Type == ShopItem.CoinDoubler)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(2);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.RecruitToken)
                {
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(22);
                    reward.Count = offer.Count;
                    unit.AddDrop(reward);
                    command.DeliveryUnits.Add(unit);
                }
                else if (offer.Type == ShopItem.GuaranteedHero)
                {
                    //Console.WriteLine();
                    if (bundle.Claim == "brawler_switch")
                    {
                        NewRecruitBrawler = offer.ItemDataId - 16000000;
                        LogicBrawlerRecruitRoadChangedCommand commands = new LogicBrawlerRecruitRoadChangedCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(8);
                        reward.Count = 0;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                    }
                    else if (bundle.Claim == "brawler_switch_clear")
                    {
                        NewRecruitBrawler = 0;
                        LogicBrawlerRecruitRoadChangedCommand commands = new LogicBrawlerRecruitRoadChangedCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(8);
                        reward.Count = 0;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                    }
                    else
                    {
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(1);
                        reward.Count = offer.Count;
                        reward.DataGlobalId = offer.ItemDataId;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                        LogicBrawlerRecruitRoadChangedCommand commands = new LogicBrawlerRecruitRoadChangedCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                    }
                }
                else if (offer.Type == ShopItem.GuaranteedHeroWithLevel)
                {
                    if (bundle.Claim == "brawler_switch")
                    {
                        NewRecruitBrawler = offer.ItemDataId - 16000000;
                        LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(8);
                        reward.Count = 0;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                    }
                    else if (bundle.Claim == "brawler_switch_clear")
                    {
                        NewRecruitBrawler = 0;
                        LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(8);
                        reward.Count = 0;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                    }
                    else
                    {
                        DeliveryUnit unit = new DeliveryUnit(100);
                        GatchaDrop reward = new GatchaDrop(1);
                        reward.Count = offer.Count;
                        reward.DataGlobalId = offer.ItemDataId;
                        unit.AddDrop(reward);
                        command.DeliveryUnits.Add(unit);
                        LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
                        commands.Execute(HomeMode);
                        AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                        serverCommandMessage.Command = commands;
                        HomeMode.GameListener.SendMessage(serverCommandMessage);
                    }
                }

                else if (offer.Type == ShopItem.RarityStarrDrop) // real frop oewprwierop
                {

                    if (offer.SkinDataId != -1)
                    {
                        LogicGiveDeliveryItemsCommand logicka = new();
                        DeliveryUnit unit = new DeliveryUnit(100);
                        logicka.DeliveryUnits.Add(unit);
                        logicka.Execute(HomeMode);
                        HomeMode.GameListener.SendCommand(logicka);

                        HomeMode.Home.StarrDrop.GenerateRarityDrop(HomeMode, (StarrDropRarity)offer.SkinDataId);
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new();
                        logicRefreshRandomRewardsCommand.Execute(HomeMode);
                        HomeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
                    }
                    else
                    {
                        DropsCount += offer.Count;
                        LogicGiveDeliveryItemsCommand logicka = new();
                        DeliveryUnit unit = new DeliveryUnit(100);
                        logicka.DeliveryUnits.Add(unit);
                        logicka.Execute(HomeMode);
                        HomeMode.GameListener.SendCommand(logicka);

                        HomeMode.Home.StarrDrop.GenerateDrop(HomeMode);
                        LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new();
                        logicRefreshRandomRewardsCommand.Execute(HomeMode);
                        HomeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
                    }
                }
                //bundle.Claim == "brawler_switch"
                else
                {
                    // todo...
                }


                void NewCommand(SkinData skinData)
                {
                    if (skinData == null) return;
                    DeliveryUnit unit = new DeliveryUnit(100);
                    GatchaDrop reward = new GatchaDrop(9);
                    foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                    {

                        if (emoteData.Skin == skinData.Name)
                        {
                            GatchaDrop reward1 = new GatchaDrop(11);
                            reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                            reward1.Count = 1;
                            unit.AddDrop(reward1);
                        }

                    }
                    foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                    {
                        if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                        {
                            GatchaDrop reward1 = new GatchaDrop(11);
                            reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                            reward1.Count = 1;
                            unit.AddDrop(reward1);
                        }

                    }
                    foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                    {
                        if (sprayData.Skin == skinData.Name)
                        {
                            GatchaDrop reward1 = new GatchaDrop(11);
                            reward1.DataGlobalId = DataTables.Get(DataType.Spray).GetData<SprayData>(sprayData.Name).GetGlobalId();
                            reward1.Count = 1;
                            unit.AddDrop(reward1);
                        }
                    }
                    if (unit.Drops.Count != 0)
                    {
                        // The complete command is executed and sent once after all
                        // offer items have been added. Sending this mutable command
                        // here raced with the asynchronous encoder and duplicated drops.
                        command.DeliveryUnits.Add(unit);
                    }
                }
            }
            command.Execute(HomeMode);

            if (!isStarrDrop)
            {
                AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                message.Command = command;
                HomeMode.GameListener.SendMessage(message);
                ChangeOffers();
            }
        }



    public void ChangeOffers()
{
    RefreshOffers();
    LogicOffersChangedMessage refreshoffers = new LogicOffersChangedMessage();
    refreshoffers.OfferBundles = OfferBundles;
    HomeMode.GameListener.SendMessage(refreshoffers);
}


        private void RotateShopContent(DateTime time, bool isNewAcc)
        {
            //OfferBundles.RemoveAll(bundle => true);
            //if (OfferBundles.Select(bundle => bundle.IsDailyDeals).ToArray().Length > 6)
            //{
            //    OfferBundles.RemoveAll(bundle => bundle.IsDailyDeals);
            //}
            //OfferBundles.RemoveAll(offer => offer.EndTime <= time);

            //if (isNewAcc || DateTime.UtcNow >= DateTime.UtcNow.Date.AddHours(8)) // Daily deals refresh at 08:00 AM UTC
            //{
            //    if (LastVisitHomeTime < DateTime.UtcNow.Date.AddHours(8)||true)
            //    {
            //        UpdateDailyOfferBundles();
            //    }
            //}
            if (true || isNewAcc)
            {
                OfferBundle uab = new OfferBundle();
                uab.Title = "👴要全英雄";
                uab.EndTime = DateTime.UtcNow.Date.AddDays(18141); // tomorrow at 8:00 utc (11:00 MSK)
                uab.Cost = 0;
                Offer offer = new Offer(ShopItem.BrawlBox, 1);
                uab.Items.Add(offer);
                //OfferBundles.Add(uab);
            }

        }


        private static void NormalizeOfferItem(ShopItem item, ref int brawlerId, ref int extra)
        {
            // Offer.Encode expects instance IDs in SkinDataId and a character data
            // reference in ItemDataId. Admin offers can contain full global IDs, so
            // normalize both representations before checking ownership or encoding.
            brawlerId = GlobalId.GetInstanceId(brawlerId);
            extra = GlobalId.GetInstanceId(extra);

            if (item != ShopItem.Skin && item != ShopItem.SkinAndHero)
            {
                return;
            }

            SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(extra);
            SkinConfData skinConf = skinData?.GetConf();
            CharacterData characterData = skinConf == null
                ? null
                : DataTables.GetCharacterByName(skinConf.Character);

            if (characterData != null)
            {
                brawlerId = characterData.GetInstanceId();
            }
        }

        public void GenerateOffer(
    DateTime OfferStart,
    DateTime OfferEnd,
    int Cost,
    int OldCost,
    int Currency,
    string Claim,
    string Title,
    string BGR,
    int IsTID,
    int DailyOfferType,
    bool OneTimeOffer,
    bool LoadOnStartup,
    bool Processed,
    int TypeBenefit,
    int Benefit,

    int panelClass,
    int panelType,

    int styleClass,
    int styleType,
    bool Special,

    int Count,
    int BrawlerID,
    int Extra,
    ShopItem Item,

    int Count2 = -1,
    int BrawlerID2 = -1,
    int Extra2 = -1,
    ShopItem Item2 = ShopItem.Coin,

    int Count3 = -1,
    int BrawlerID3 = -1,
    int Extra3 = -1,
    ShopItem Item3 = ShopItem.Coin,

    int Count4 = -1,
    int BrawlerID4 = -1,
    int Extra4 = -1,
    ShopItem Item4 = ShopItem.Coin
    )
        {

            NormalizeOfferItem(Item, ref BrawlerID, ref Extra);
            if (Count2 != -1) NormalizeOfferItem(Item2, ref BrawlerID2, ref Extra2);
            if (Count3 != -1) NormalizeOfferItem(Item3, ref BrawlerID3, ref Extra3);
            if (Count4 != -1) NormalizeOfferItem(Item4, ref BrawlerID4, ref Extra4);

            // This release gift is commonly edited by replacing only its skin ID.
            // Persist the purchase against the actual skin, otherwise an account
            // that claimed Star Shelly sees every replacement as already claimed.
            if (Claim == "free_release_skin52_300gems")
            {
                int offerSkinId = Item == ShopItem.Skin ? Extra
                    : Item2 == ShopItem.Skin ? Extra2
                    : Item3 == ShopItem.Skin ? Extra3
                    : Item4 == ShopItem.Skin ? Extra4
                    : -1;

                if (offerSkinId >= 0)
                {
                    Claim = $"free_release_skin{offerSkinId}_300gems";
                }
            }


            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.StartTime = OfferStart;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;
            bundle.IsTID = IsTID;
            bundle.OfferType = DailyOfferType;
            bundle.OneTimeOffer = OneTimeOffer;
            bundle.LoadOnStartup = LoadOnStartup;
            bundle.Processed = Processed;
            bundle.TypeBenefit = TypeBenefit;
            bundle.Benefit = Benefit;
            bundle.ShopPanelLayoutClass = panelClass;
            bundle.ShopPanelLayoutType = panelType;
            bundle.ShopStyleSetClass = styleClass;
            bundle.ShopStyleSetType = styleType;
            bundle.specialOffer = Special;
            bundle.MultiCount = 1;
            List<int> road_list = new List<int> { };
            foreach (int brawler in HomeMode.Home.BrawlersRoad)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
                {
                    road_list.Add(brawler);
                }
            }



            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (bundle.Currency == 1000 && PaidOffers.Contains(bundle.Claim))
            {
                bundle.Currency = 0;
                bundle.Cost = 0;
                bundle.Title = "ЗАБРАТЬ АКЦИЮ";
                bundle.Purchased = false;
            }
            if (bundle.Claim.StartsWith("month")){
                bundle.Purchased = false;
            }
            if (OffersClaimed.Contains(bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.GuaranteedHero || Item == ShopItem.GuaranteedHeroWithLevel && HomeMode.Avatar.HasHero(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Skin && UnlockedSkins.Contains(29000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.SkinAndHero && (UnlockedSkins.Contains(29000000 + Extra)))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Item && HomeMode.Avatar.SPGS.Contains(23000000 + Extra))
            {
                bundle.Purchased = true;
            }

            if (Item == ShopItem.PlayerThumbnail && UnlockedThumbnails.Contains(28000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Emote && UnlockedEmotes.Contains(52000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Spray && UnlockedSprays.Contains(68000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Spray && UnlockedSprays.Contains(68000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.RecruitToken && road_list.Count == 0)
            {
                bundle.Purchased = true;
            }


            if (bundle.Purchased && DailyOfferType == 0 && panelClass == 0) return;


            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);

            if (Count2 != -1)
            {
                Offer offer2 = new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2);
                bundle.Items.Add(offer2);
            }
            if (Count3 != -1)
            {
                Offer offer3 = new Offer(Item3, Count3, (16000000 + BrawlerID3), Extra3);
                bundle.Items.Add(offer3);
            }
            if (Count4 != -1)
            {
                Offer offer4 = new Offer(Item4, Count4, (16000000 + BrawlerID4), Extra4);
                bundle.Items.Add(offer4);
            }

            OfferBundles.Add(bundle);
        }


        public void GenerateMultiOffer(
    DateTime OfferStart,
    DateTime OfferEnd,
    int Cost,
    int OldCost,
    int Currency,
    string Claim,
    string Title,
    string BGR,
    int IsTID,
    int DailyOfferType,
    bool OneTimeOffer,
    bool LoadOnStartup,
    bool Processed,
    int TypeBenefit,
    int Benefit,

    int panelClass,
    int panelType,

    int styleClass,
    int styleType,
    bool Special,
    int MultiCount,

    int Count,
    int BrawlerID,
    int Extra,
    ShopItem Item,

    int Count2 = -1,
    int BrawlerID2 = -1,
    int Extra2 = -1,
    ShopItem Item2 = ShopItem.Coin,

    int Count3 = -1,
    int BrawlerID3 = -1,
    int Extra3 = -1,
    ShopItem Item3 = ShopItem.Coin,

    int Count4 = -1,
    int BrawlerID4 = -1,
    int Extra4 = -1,
    ShopItem Item4 = ShopItem.Coin
    )
        {


            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.StartTime = OfferStart;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;
            bundle.IsTID = IsTID;
            bundle.OfferType = DailyOfferType;
            bundle.OneTimeOffer = OneTimeOffer;
            bundle.LoadOnStartup = LoadOnStartup;
            bundle.Processed = Processed;
            bundle.TypeBenefit = TypeBenefit;
            bundle.Benefit = Benefit;
            bundle.ShopPanelLayoutClass = panelClass;
            bundle.ShopPanelLayoutType = panelType;
            bundle.ShopStyleSetClass = styleClass;
            bundle.ShopStyleSetType = styleType;
            bundle.specialOffer = Special;
            bundle.MultiCount = MultiCount;
            List<int> road_list = new List<int> { };
            foreach (int brawler in HomeMode.Home.BrawlersRoad)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
                {
                    road_list.Add(brawler);
                }
            }



            if (TimerMath(OfferStart, OfferEnd) == -1)
            {
                bundle.Purchased = true;
            }
            if (bundle.Currency == 1000 && PaidOffers.Contains(bundle.Claim))
            {
                bundle.Currency = 0;
                bundle.Cost = 0;
                bundle.Title = "ЗАБРАТЬ АКЦИЮ";
                bundle.Purchased = false;
            }
            if (bundle.Claim.StartsWith("month")){
                bundle.Purchased = false;
            }
            if (bundle.MultiCount <= OffersClaimed.Count(x => x == bundle.Claim))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.GuaranteedHero || Item == ShopItem.GuaranteedHeroWithLevel && HomeMode.Avatar.HasHero(16000000 + BrawlerID))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Skin && UnlockedSkins.Contains(29000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Item && HomeMode.Avatar.SPGS.Contains(23000000 + Extra))
            {
                bundle.Purchased = true;
            }

            if (Item == ShopItem.PlayerThumbnail && UnlockedThumbnails.Contains(28000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Emote && UnlockedEmotes.Contains(52000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Spray && UnlockedSprays.Contains(68000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.Spray && UnlockedSprays.Contains(68000000 + Extra))
            {
                bundle.Purchased = true;
            }
            if (Item == ShopItem.RecruitToken && road_list.Count == 0)
            {
                bundle.Purchased = true;
            }

            bundle.Title = $"{bundle.Title} ({OffersClaimed.Count(x => x == bundle.Claim)+1}/{bundle.MultiCount})";


            if (bundle.Purchased && DailyOfferType == 0 && panelClass == 0) return;


            Offer offer = new Offer(Item, Count, (16000000 + BrawlerID), Extra);
            bundle.Items.Add(offer);

            if (Count2 != -1)
            {
                Offer offer2 = new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2);
                bundle.Items.Add(offer2);
            }
            if (Count3 != -1)
            {
                Offer offer3 = new Offer(Item3, Count3, (16000000 + BrawlerID3), Extra3);
                bundle.Items.Add(offer3);
            }
            if (Count4 != -1)
            {
                Offer offer4 = new Offer(Item4, Count4, (16000000 + BrawlerID4), Extra4);
                bundle.Items.Add(offer4);
            }

            OfferBundles.Add(bundle);
        }

        public void Encode(ByteStream stream)
        {//64 VInt 32 Boolean 28 String 16 String 36 Int

            // Older or partially initialized accounts may deserialize missing
            // collections as null. Encoding must never disconnect the player
            // because optional profile data is absent.
            CharacterIds ??= new int[]
            {
                GlobalId.CreateGlobalId(16, 0),
                GlobalId.CreateGlobalId(16, 1),
                GlobalId.CreateGlobalId(16, 2)
            };
            if (CharacterIds.Length == 0)
            {
                CharacterIds = new int[] { GlobalId.CreateGlobalId(16, 0) };
            }
            NormalizeUnlockedSkinIds();
            OfferBundles ??= new List<OfferBundle>();
            UnlockedEmotes ??= new List<int>();
            UnlockedTituls ??= new List<int>();
            UnlockedThumbnails ??= new List<int>();
            UnlockedSprays ??= new List<int>();
            PlayerSelectedEmotes ??= new Dictionary<int, int>();
            PlayerSelectedSpray ??= new Dictionary<int, int>();
            CreatorCode ??= string.Empty;


            stream.WriteVInt(2000000);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(HomeMode.Avatar.Trophies); // Trophies
            stream.WriteVInt(HomeMode.Avatar.HighestTrophies); // Highest Trophies
            stream.WriteVInt(HomeMode.Avatar.HighestTrophies);
            if(LogicServerListener.Instance.IsDev()) TrophyRoadProgress = 200;
            stream.WriteVInt(TrophyRoadProgress);
            stream.WriteVInt(50000); // Experience
            ByteStreamHelper.WriteDataReference(stream, ThumbnailId);
            ByteStreamHelper.WriteDataReference(stream, NameColorId);


            stream.WriteVInt(27);//played gamemodes(dont shouw battle hint)
            for (int i = 0; i < 27; i++) stream.WriteVInt(i);
            int skins = 0;
            foreach (Hero hero in HomeMode.Avatar.Heroes)
            {
                if (hero.SelectedSkinId != 0) skins++;
            }
            stream.WriteVInt(skins); // Selected Skins
            foreach (Hero hero in HomeMode.Avatar.Heroes)
            {
                if (hero.SelectedSkinId != 0) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(29, hero.SelectedSkinId));
            }
            stream.WriteVInt(0); // Randomizer Skin Selected

            stream.WriteVInt(0); // Current Random Skin

            stream.WriteVInt(UnlockedSkins.Count); // Played game modes
            foreach (int s in UnlockedSkins)
            {
                ByteStreamHelper.WriteDataReference(stream, s);
            }

            stream.WriteVInt(0); // Unlocked Skin Purchase Option

            stream.WriteVInt(0); // New Item State

            stream.WriteVInt(0);
            stream.WriteVInt(0);//highest trophies
            stream.WriteVInt(0);
            stream.WriteVInt(2);//control mode
            stream.WriteBoolean(true);//battle hints
            stream.WriteVInt(TokenDoublers);//token doubler
            stream.WriteVInt(0);//maybe starr drop timer ? #v50 --risporce(bsds)
            stream.WriteVInt((int)(DateTime.Parse(SeasonTimer) - DateTime.Now).TotalSeconds);//trophy league timer --risporce(bsds)
            stream.WriteVInt(0);//power play timer --risporce(bsds)
            stream.WriteVInt((int)(DateTime.Parse(BrawlPassTimer) - DateTime.Now).TotalSeconds);//Brawl pass season time --risporce(bsds)

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteBoolean(true); // Token Doubler Enabled
            stream.WriteVInt(2);  // Token Doubler New Tag State
            stream.WriteVInt(2);  // Event Tickets New Tag State
            stream.WriteVInt(2);  // Coin Packs New Tag State
            stream.WriteVInt(HomeMode.Avatar.NameChangeCost);  // Change Name Cost
            //if(HomeMode.Avatar.NameChangeTimer != )
            stream.WriteVInt(0);  // Timer For the Next Name Change

            stream.WriteVInt(OfferBundles.Count); // Shop offers array
            foreach (OfferBundle offerBundle in OfferBundles)
            {
                offerBundle.Encode(stream);
            }

            stream.WriteVInt(200); // BattleTokens
            stream.WriteVInt(-1); // Time to new tokens

            stream.WriteVInt(0);

            stream.WriteVInt(1);
            stream.WriteVInt(30);

            stream.WriteByte(1);
            ByteStreamHelper.WriteDataReference(stream, CharacterIds[0]);
            //ByteStreamHelper.WriteDataReference(stream, CharacterIds[1]);
            //ByteStreamHelper.WriteDataReference(stream, CharacterIds[2]);

            stream.WriteString(HomeMode.Avatar.Region);
            stream.WriteString(CreatorCode);
            stream.WriteVInt(9);
            stream.WriteVInt(2147483647); stream.WriteVInt(28);  // Have already watched starrdrop stupid animation
            stream.WriteVInt(TokenReward); stream.WriteVInt(3);
            stream.WriteVInt(TrophiesReward); stream.WriteVInt(4);
            stream.WriteLogicLong(HomeMode.Avatar.WinStreak, 37);
            stream.WriteLogicLong(HomeMode.Avatar.DoNotDisturb, 7); // invite block
            stream.WriteLogicLong(HomeMode.Avatar.ChatMutestate, 17); // invite block
            stream.WriteLogicLong(StarPointsGained, 8); // frogs
            stream.WriteLogicLong(BlingsReward, 32);
            stream.WriteLogicLong(ShowLivesPopup, 19);
            // stream.WriteLogicLong(HomeMode.Avatar.FriendRequestBlock, 6); // friend request block

            //stream.WriteLogicLong(1, 17); // chat block
            //stream.WriteLogicLong(0, 15); // chat block
            //stream.WriteLogicLong(1, 9); // chat block
            //stream.WriteLogicLong(1, 22); // chat block
            //stream.WriteLogicLong(1, 24); // chat block
            //stream.WriteLogicLong(1, 25); // chat block
            //stream.WriteLong(2, 1);  // Unknown
            //stream.WriteLong(3, 0);  // Tokens Gained
            //stream.WriteLong(4, 0);  // Trophies Gained
            //stream.WriteLong(6, 0);  // Demo Account
            //stream.WriteLong(7, 0);  // Invites Blocked

            //stream.WriteLong(8, 0);  // Star Points Gained
            //stream.WriteLong(9, 1);  // Show Star Points
            //stream.WriteLong(10, 0);  // Power Play Trophies Gained
            //stream.WriteLong(12, 1);  // Unknown
            //stream.WriteLong(14, 0);  // Coins Gained
            //stream.WriteLong(15, 0);  // AgeScreen | 3 = underage (disable social media); | 1 = age popup
            //stream.WriteLong(16, 1);
            //stream.WriteLong(17, 0);  // Team Chat Muted
            //stream.WriteLong(18, 1);  // Esport Button
            //stream.WriteLong(19, 0);  // Champion Ship Lives Buy Popup
            //stream.WriteLong(20, 0);  // Gems Gained
            //stream.WriteLong(21, 0);  // Looking For Team State
            //stream.WriteLong(22, 1);
            //stream.WriteLong(23, 0);  // Club Trophies Gained
            //stream.WriteLong(24, 1);  // Have already watched club league stupid animation
            // 26 - Club League Point
            // 27 - PowerPoints Gained
            // 30 - FUCKING FROGS
            // 32 - blings gained
            if(LogicServerListener.Instance.IsDev()) BpSeason = 21;
            TokenReward = 0;
            TrophiesReward = 0;
            FrogsReward = 0;
            BlingsReward = 0;
            ShowLivesPopup = 0;
            if ((int)(RankedBan - DateTime.Now).TotalSeconds > 0)
            {
                stream.WriteVInt(1);
                stream.WriteVInt((int)(RankedBan - DateTime.Now).TotalSeconds);
                stream.WriteVInt(2);
                stream.WriteVInt(0);
            }
            else
            {
                stream.WriteVInt(0);
            }

            stream.WriteVInt(1); // count brawl pass seasons
            for (int i = 0; i < 1; i++)
            {
                if (LogicServerListener.Instance.IsDev())
                {
                    stream.WriteVInt(BpSeason); // season
                    stream.WriteVInt(99999999);
                    stream.WriteBoolean(false);

                    stream.WriteVInt(131);
                    stream.WriteBoolean(false);

                    if (stream.WriteBoolean(true)) // Track 9
                    {
                        stream.WriteLongLong128(PremiumPassProgress);
                    }
                    if (stream.WriteBoolean(true)) // Track 10
                    {
                        stream.WriteLongLong128(BrawlPassProgress);
                    }
                    //v53
                    stream.WriteBoolean(false); // BrawlPassPlus
                    if (stream.WriteBoolean(true)) // Track ?
                    {
                        stream.WriteLongLong128(BrawlPassPlusProgress);
                    }
                }
                else
                {
                    stream.WriteVInt(BpSeason); // season
                    stream.WriteVInt(BrawlPassTokens);
                    stream.WriteBoolean(HasPremiumPass);

                    stream.WriteVInt(131);
                    stream.WriteBoolean(false);

                    if (stream.WriteBoolean(true)) // Track 9
                    {
                        stream.WriteLongLong128(PremiumPassProgress);
                    }
                    if (stream.WriteBoolean(true)) // Track 10
                    {
                        stream.WriteLongLong128(BrawlPassProgress);
                    }
                    //v53
                    stream.WriteBoolean(HasPremiumPassPlus); // BrawlPassPlus
                    if (stream.WriteBoolean(true)) // Track ?
                    {
                        stream.WriteLongLong128(BrawlPassPlusProgress);
                    }
                }
            }
            stream.WriteVInt(0);

            if (Quests != null)
            {
                stream.WriteBoolean(true);
                Quests.Encode(stream, HomeMode.Home.BpSeason);
            }
            else
            {
                stream.WriteBoolean(true);
                stream.WriteVInt(0);
            }

            stream.WriteVInt(9999999);//next reroll update
            stream.WriteVInt(UsedRerolls);//used rerolls
            stream.WriteVInt(0);

            stream.WriteBoolean(true);

            stream.WriteVInt(UnlockedEmotes.Count + UnlockedTituls.Count + UnlockedThumbnails.Count + UnlockedSprays.Count); // Played game modes
            foreach (int Emote in UnlockedEmotes)
            {
                ByteStreamHelper.WriteDataReference(stream, Emote);
                int emoteInstanceId = GlobalId.GetInstanceId(Emote);
                bool isFound = false;
                int foundSlot = 0;
                var globalEntry = PlayerSelectedEmotes.FirstOrDefault(x => x.Value == emoteInstanceId);
                if (!globalEntry.Equals(default(KeyValuePair<int, int>)))
                {
                    isFound = true;
                    foundSlot = globalEntry.Key;
                }
                if (!isFound)
                {
                    EmoteData? emoteData = DataTables.Get(52).GetDataByGlobalId<EmoteData>(Emote);
                    if (!String.IsNullOrEmpty(emoteData?.Character))
                    {
                        CharacterData? characterData = DataTables.Get(16).GetData<CharacterData>(emoteData.Character);
                        if (characterData != null)
                        {
                            Hero hero = HomeMode.Avatar.GetHero(characterData.GetGlobalId());
                            if (hero != null && hero.SelectedEmotes != null)
                            {
                                var heroEntry = hero.SelectedEmotes.FirstOrDefault(x => x.Value == emoteInstanceId);
                                if (!heroEntry.Equals(default(KeyValuePair<int, int>)))
                                {
                                    isFound = true;
                                    foundSlot = heroEntry.Key;
                                }
                            }
                        }
                    }
                }
                stream.WriteVInt(isFound ? 1 : 0);
                if (isFound)
                {
                    stream.WriteVInt(foundSlot);
                    stream.WriteVInt(3);
                }
            }
            foreach (int Thumbnail in UnlockedThumbnails)
            {
                ByteStreamHelper.WriteDataReference(stream, Thumbnail);
                stream.WriteVInt(0);
            }
            foreach (int Title in UnlockedTituls)
            {
                ByteStreamHelper.WriteDataReference(stream, Title);
                stream.WriteVInt(0);
            }
            foreach (int Spray in UnlockedSprays)
            {
                ByteStreamHelper.WriteDataReference(stream, Spray);
                int sprayInstanceId = GlobalId.GetInstanceId(Spray);
                bool isFound = false;
                int foundSlot = 0;
                SprayData? emoteData = DataTables.Get(68).GetDataByGlobalId<SprayData>(Spray);

                if (emoteData != null)
                {
                    CharacterData? characterData = DataTables.Get(16).GetData<CharacterData>(emoteData.Character);
                    if (characterData != null)
                    {
                        Hero hero = HomeMode.Avatar.GetHero(characterData.GetGlobalId());
                        if (hero != null)
                        {
                            if (hero.SelectedSpray == sprayInstanceId)
                            {
                                isFound = true;
                                foundSlot = 6;
                            }
                        }
                    }
                }


                if (!isFound)
                {
                    var globalEntry = PlayerSelectedSpray.FirstOrDefault(x => x.Value == sprayInstanceId);
                    if (!globalEntry.Equals(default(KeyValuePair<int, int>)))
                    {
                        isFound = true;
                        foundSlot = globalEntry.Key;
                    }
                }
                stream.WriteVInt(isFound ? 1 : 0);
                if (isFound)
                {
                    stream.WriteVInt(foundSlot);
                    stream.WriteVInt(3);
                }
            }

            if(stream.WriteBoolean(true))
            {
                stream.WriteVInt(GeneralStaticLogic.RankedSeason); // Season
                stream.WriteVInt(RankedSoloRank); // Rank Solo League
                stream.WriteVInt(RankedSoloProgress); // Total team league progress
                stream.WriteVInt(RankedTrioRank); // Rank Team League
                stream.WriteVInt(RankedTrioProgress); // Total team league progress
                stream.WriteVInt(RankedSoloMaxRank); // High Rank Solo League
                stream.WriteVInt(RankedSoloMaxProgress); // max solo progress
                stream.WriteVInt(RankedTrioMaxRank); // High Rank Team League
                stream.WriteVInt(RankedTrioMaxProgress);  // max team progress
                stream.WriteVInt(0); // чет для лидербордов
                stream.WriteVInt(1);
                stream.WriteVInt(1); // reward count???
                if (stream.WriteBoolean(true)) //LogicPlayerRewardData::encode
                {
                    if (stream.WriteBoolean(true))
                    {
                        stream.WriteVInt(1);
                        stream.WriteVInt(1);
                    }
                    if (stream.WriteBoolean(true))
                        new GemOffer(25, 1, 0, 63 - 3).Encode(stream);
                }
                stream.WriteVInt(3);
                stream.WriteBoolean(true);
            }
            stream.WriteInt(0);
            stream.WriteVInt(502052);
            ByteStreamHelper.WriteDataReference(stream, FavouriteCharacter);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(2023189);


            stream.WriteVInt(35); // event slot id
            stream.WriteVInt(1);
            stream.WriteVInt(2);
            stream.WriteVInt(3);
            stream.WriteVInt(4);
            stream.WriteVInt(5);
            stream.WriteVInt(6);
            stream.WriteVInt(7);
            stream.WriteVInt(8);
            stream.WriteVInt(9);
            stream.WriteVInt(10);
            stream.WriteVInt(11);
            stream.WriteVInt(12);
            stream.WriteVInt(13);
            stream.WriteVInt(14);
            stream.WriteVInt(15);
            stream.WriteVInt(16);
            stream.WriteVInt(17);
            stream.WriteVInt(18);
            stream.WriteVInt(19);
            stream.WriteVInt(20);
            stream.WriteVInt(21);
            stream.WriteVInt(22);
            stream.WriteVInt(23);
            stream.WriteVInt(24);
            stream.WriteVInt(25);
            stream.WriteVInt(26);
            stream.WriteVInt(27);
            stream.WriteVInt(28);
            stream.WriteVInt(29);
            stream.WriteVInt(30);
            stream.WriteVInt(31);
            stream.WriteVInt(32);
            stream.WriteVInt(33);
            stream.WriteVInt(34);
            stream.WriteVInt(35);

            EventData[] encodableEvents = Events?.Where(e => e != null).ToArray() ?? Array.Empty<EventData>();
            stream.WriteVInt(encodableEvents.Length);
            foreach (EventData e in encodableEvents)
            {
                e.Encode(stream, false, ChallengeLoses, ChallengeWins);
            }

            /*
              stream.WriteVInt(-1);
              stream.WriteVInt(14); //eventid
              stream.WriteVInt(1);
              stream.WriteVInt(0);
              stream.WriteVInt(72292);
              stream.WriteVInt(10);
              stream.WriteDataReference(0, 0); // map id
              stream.WriteVInt(-1);
              stream.WriteVInt(2); //state
              stream.WriteString("");
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false); // MapMaker map structure array
              stream.WriteVInt(0);
              stream.WriteBoolean(true); // Power League array entry
                                         // Power League Data Array Start //


              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteVInt(-1);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteVInt(-1);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false);

              stream.WriteVInt(-1);
              stream.WriteVInt(15); //eventid
              stream.WriteVInt(1);
              stream.WriteVInt(0);
              stream.WriteVInt(72292);
              stream.WriteVInt(10);
              stream.WriteDataReference(0, 0); // map id
              stream.WriteVInt(-1);
              stream.WriteVInt(2); //state
              stream.WriteString("");
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false); // MapMaker map structure array
              stream.WriteVInt(0);
              stream.WriteBoolean(true); // Power League array entry
                                         // Power League Data Array Start //



              // Power League Data Array End //
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteVInt(-1);
              stream.WriteBoolean(false);
              stream.WriteBoolean(false);
              stream.WriteVInt(-1);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteVInt(0);
              stream.WriteBoolean(false);

               *             stream.WriteVInt(0);
                          stream.WriteVInt(13);
                          stream.WriteVInt(1);
                          stream.WriteVInt(0);
                          stream.WriteVInt(72292);
                          stream.WriteVInt(10);
                          stream.WriteDataReference(15, 454); // map id
                          stream.WriteVInt(-1);
                          stream.WriteVInt(2);
                          stream.WriteString("");
                          stream.WriteVInt(0);
                          stream.WriteVInt(0);
                          stream.WriteVInt(0);
                          stream.WriteVInt(1);
                          stream.WriteVInt(-1);
                          stream.WriteVInt(0);
                          stream.WriteVInt(0);
                          stream.WriteBoolean(false); // array
                          stream.WriteBoolean(true); // MapMaker map structure array
                          ByteStreamHelper.WriteBattlePlayerMap(stream,new(PlayerMaps.FirstOrDefault()));
                          stream.WriteVInt(0);
                          stream.WriteBoolean(false); //sub_6BA960
                      stream.WriteVInt(0);
                          stream.WriteVInt(0);
                          stream.WriteBoolean(false); //String
                          stream.WriteBoolean(false); //String

                          stream.WriteBoolean(false); 
                          stream.WriteVInt(-1);
                          stream.WriteBoolean(false); 
                          stream.WriteBoolean(false); 
                      stream.WriteVInt(-1);
                          stream.WriteVInt(1);
                          stream.WriteVInt(1);
                          stream.WriteDataReference(0, 0);
                          stream.WriteVInt(1); //
                          stream.WriteVInt(0);
                          stream.WriteVInt(0);
                          stream.WriteBoolean(false);
            */
           

            stream.WriteVInt(encodableEvents.Length);
            foreach (EventData e in encodableEvents)
            {
                e.Encode(stream, true, 0, 0);
            }



            ByteStreamHelper.WriteIntList(stream, new List<int> { 20, 35, 75, 140, 290, 480, 800, 1250, 1875, 2800 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 30, 80, 170, 360 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 300, 880, 2040, 4680 });

            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            {
                stream.WriteVInt(ReleaseEntry.LogicReleaseEntryGrom.Length / 2);  // IntValueEntry
                for (int i = 0; i < ReleaseEntry.LogicReleaseEntryGrom.Length / 2; i++)
                {
                    stream.WriteDataReference(ReleaseEntry.LogicReleaseEntryGrom[i * 2] / 1000000, ReleaseEntry.LogicReleaseEntryGrom[i * 2]);
                    stream.WriteInt(ReleaseEntry.LogicReleaseEntryGrom[i * 2 + 1]);
                    stream.WriteInt(0);
                    stream.WriteInt(0);
                    stream.WriteBoolean(false);
                }
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark")
            {
                stream.WriteVInt(ReleaseEntry.LogicReleaseEntryMiopark.Length / 2);  // IntValueEntry
                for (int i = 0; i < ReleaseEntry.LogicReleaseEntryMiopark.Length / 2; i++)
                {
                    stream.WriteDataReference(ReleaseEntry.LogicReleaseEntryMiopark[i * 2] / 1000000, ReleaseEntry.LogicReleaseEntryMiopark[i * 2]);
                    stream.WriteInt(ReleaseEntry.LogicReleaseEntryMiopark[i * 2 + 1]);
                    stream.WriteInt(0);
                    stream.WriteInt(0);
                    stream.WriteBoolean(false);
                }
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "retro")
            {
                stream.WriteVInt(ReleaseEntry.LogicReleaseEntryRetro.Length / 2);  // IntValueEntry
                for (int i = 0; i < ReleaseEntry.LogicReleaseEntryRetro.Length / 2; i++)
                {
                    stream.WriteDataReference(ReleaseEntry.LogicReleaseEntryRetro[i * 2] / 1000000, ReleaseEntry.LogicReleaseEntryRetro[i * 2]);
                    stream.WriteInt(ReleaseEntry.LogicReleaseEntryRetro[i * 2 + 1]);
                    stream.WriteInt(0);
                    stream.WriteInt(0);
                    stream.WriteBoolean(false);
                }
            }
            else
            {
                stream.WriteVInt(ReleaseEntry.LogicReleaseEntryDefault.Length / 2);  // IntValueEntry
                for (int i = 0; i < ReleaseEntry.LogicReleaseEntryDefault.Length / 2; i++)
                {
                    stream.WriteDataReference(ReleaseEntry.LogicReleaseEntryDefault[i * 2] / 1000000, ReleaseEntry.LogicReleaseEntryDefault[i * 2]);
                    stream.WriteInt(ReleaseEntry.LogicReleaseEntryDefault[i * 2 + 1]);
                    stream.WriteInt(0);
                    stream.WriteInt(0);
                    stream.WriteBoolean(false);
                }
            }

            bool ClassicEvent = (TimerMath(new DateTime(2005, 9, 27, 12, 0, 0), new DateTime(2026, 10, 11, 12, 0, 0)) > 0);
            if (LogicServerListener.Instance.IsDev())
                ThemeId = PreferredThemeId >= 0 ? PreferredThemeId : GeneralStaticLogic.DefaultThemeId;
            List<int> logicConfData = new List<int>
                        {
                        1,41000000+ThemeId,
                        10027,0,
                        10029,20,
                        10018,1,
                        23000136,1,
                        29, ShopSkinsSetId,
                        48, 99999,
                        79, 99999,
                        80, 99999,
                        65,2,
                        66,0,
                        47,41381,
                        50,1,
                        1100, 500,
                        1101, 500,
                        1003, 1,
                        36,0,
                        74,1,
                        78,1,
                        17,4,
                        100046,1,
                        87,1,
                        63,1,
                        14, ClassicEvent ? 1 : 0,
                        113, TwoDropsEvent ? 1 : 0,
                        37,0, // disable quest, but FUCKING DISABLE ALL BP 
                        5,0, // disable quest, but FUCKING DISABLE ALL BP 
                        133,0

                        };
            if (!HomeMode.Avatar.IsDebugAccount)
            {
                foreach (int brawler in GeneralStaticLogic.LockedBrawlers)
                {
                    logicConfData.Add(GlobalId.CreateGlobalId(16, brawler));
                    logicConfData.Add(1);
                }
            }

            int[] LogicConfData = logicConfData.ToArray();
            stream.WriteVInt(LogicConfData.Length / 2);  // IntValueEntry
            for (int i = 0; i < LogicConfData.Length / 2; i++)
            {
                stream.WriteVInt(LogicConfData[i * 2 + 1]);
                stream.WriteVInt(LogicConfData[i * 2]);
            }


            stream.WriteVInt(4); //Custom Event

            stream.WriteVInt(3);
            stream.WriteVInt(3);
            stream.WriteVInt(3);
            stream.WriteVInt(3);

            stream.WriteVInt(14);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(739760);

            stream.WriteVInt(29); // skin theme timer
            stream.WriteVInt(10);
            stream.WriteVInt(79);
            stream.WriteVInt((int)(DateTime.Parse(ShopSkinsSetTimer) - DateTime.Now).TotalSeconds);

            stream.WriteVInt(29); // хз чет второе
            stream.WriteVInt(10);
            stream.WriteVInt(0);
            stream.WriteVInt(1330340);

            stream.WriteVInt(0); // Custom event

            if (StarPointsGained > 0)
            {
                StarPointsGained = 0;
            }
            // stream.WriteBoolean(true); // ChronosTextEntry
            // stream.WriteString("ПОЛУЧИ МИФИЧЕСКИЙ СКИН И МНОЖЕСТВО НАГРАД!"); // Subtitle
            // stream.WriteVInt(0); // IsTid
            // stream.WriteVInt(1);
            // stream.WriteDataReference(69, 10); // Attached customize
            // stream.WriteVInt(1);
            // stream.WriteString("offer_bgr_onceupon"); // Background
            // stream.WriteBoolean(true); // ChronosTextEntry
            // stream.WriteString("РЫЦАРЬ ДЖЕКИ"); // Title
            // stream.WriteVInt(0); // IsTid
            // stream.WriteVInt(1);
            // stream.WriteVInt(1);
            // stream.WriteVInt(1);
            // stream.WriteString(null);
            // stream.WriteDataReference(29, 908); // Showed skin (-1 for nothing);

            if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "grom")
            {
                stream.WriteVInt(4); // Customize chain offers count
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ЗАХОДИ В ИГРУ 10 ДНЕЙ И ПОЛУЧАЙ НАГРАДЫ"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 11); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_cartoon"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ДОБРО ПОЖАЛОВАТЬ"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 751); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧИ ДРАКО, И МНОЖЕСТВО ДРУГИХ РЕСУРСОВ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 12); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_phoenix"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("НОВЫЙ ПЕРСОНАЖ"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 771); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧИ НОВЫЙ СКИН И ДОПОЛНИТЕЛЬНЫЕ РЕСУРСЫ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 9); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_lny23"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("НОВЫЙ СКИН"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 965); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧАЙ ПОДАРКИ КАЖДЫЙ ДЕНЬ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 10); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_retro2023"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОДАРКИ РЕТРОПОЛИСА"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 873); // Showed skin (-1 for nothing);
            }
            else if (GeneralStaticLogic.IsCustom && GeneralStaticLogic.CustomPreset == "miopark")
            {
                stream.WriteVInt(4); // Customize chain offers count

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧИ ДРАКО, И МНОЖЕСТВО ДРУГИХ РЕСУРСОВ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 12); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_phoenix"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("НОВЫЙ ПЕРСОНАЖ"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 771); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ЗАХОДИ В ИГРУ 10 ДНЕЙ И ПОЛУЧАЙ НАГРАДЫ"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 11); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_enchanted"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ДОБРО ПОЖАЛОВАТЬ"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 860); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧИ НОВЫЙ СКИН И ДОПОЛНИТЕЛЬНЫЕ РЕСУРСЫ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 9); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_lny23"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("НОВЫЙ СКИН"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 975); // Showed skin (-1 for nothing);

                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОЛУЧАЙ ПОДАРКИ КАЖДЫЙ ДЕНЬ!"); // Subtitle
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteDataReference(69, 10); // Attached customize
                stream.WriteVInt(1);
                stream.WriteString("offer_bgr_lny23"); // Background
                stream.WriteBoolean(true); // ChronosTextEntry
                stream.WriteString("ПОДАРКИ СТИМПАНКА"); // Title
                stream.WriteVInt(0); // IsTid
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteVInt(1);
                stream.WriteString(null);
                stream.WriteDataReference(29, 974); // Showed skin (-1 for nothing);
            }
            else
            {
                stream.WriteVInt(0);
            }

            stream.WriteVInt(0);

            ByteStreamHelper.WriteIntList(stream, new List<int> { 1, 2 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 1, -64 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 1, 4 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 0, 29, 79, 169, 349, 699 });
            ByteStreamHelper.WriteIntList(stream, new List<int> { 0, 160, 450, 500, 1250, 2500 });

            stream.WriteLong(HomeId);  // PlayerID

            NotificationFactory.Encode(stream, LogicServerListener.Instance.GetAllianceMail(HomeMode.Avatar.AllianceId));
            // stream.WriteVInt(0);
            /*
            stream.WriteVInt(1);
            stream.WriteVInt(44);
            stream.WriteInt(0);
            stream.WriteBoolean(false);
            stream.WriteInt(0);
            stream.WriteString(String.Empty);
            stream.WriteVInt(0);*/
            //stream.ChronosTextEntry("popa", 1);
            //stream.ChronosTextEntry("popa", 1);
            //stream.ChronosTextEntry("popa", 1);
            //stream.ChronosFileEntry("pop_up_1920x1235_welcome.png", "6bb3b752a80107a14671c7bdebe0a1b662448d0c");
            //stream.WriteString("brawlstars://shop");
            //stream.WriteVInt(0);
            //stream.WriteBoolean(true);
            //stream.WriteBoolean(true);

            stream.WriteVInt(-1);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);//+108
            stream.WriteVInt(0);//+124

            if (DailyCalendarData != null)
            {
                stream.WriteBoolean(true); // Daily Login Calendar
                DailyCalendarData.Encode(stream, HomeMode);
            }
            else
                stream.WriteBoolean(false); // Daily Login Calendar
            stream.WriteVInt(HomeMode.Avatar.Heroes.Count);//Gears
            foreach (Hero hero in HomeMode.Avatar.Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterId);
                List<GearData> gearDatas = new List<GearData>();
                foreach (GearData gearData in DataTables.Get(DataType.Gear).GetDatas())
                {
                    if (gearData.Rarity == "RareGear" || gearData.ExtraHerosAvailableTo.Contains(DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(hero.CharacterId).Name)) gearDatas.Add(gearData);
                }
                stream.WriteVInt(hero.OwnedGears.Count);
                foreach (int gearId in hero.OwnedGears)
                {
                    ByteStreamHelper.WriteDataReference(stream, 62000000 + gearId);
                }
                stream.WriteVInt(2);
                stream.WriteDataReference(62000000 + hero.SelectedGearId1);
                stream.WriteDataReference(62000000 + hero.SelectedGearId2);
            }

            stream.WriteBoolean(true);// starroad

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);


            //stream.WriteBoolean(true);

            List<int> rare = GetBrawlersByRarity("rare");
            List<int> super_rare = GetBrawlersByRarity("super_rare");
            List<int> epic = GetBrawlersByRarity("epic");
            List<int> mythic = GetBrawlersByRarity("mythic");
            List<int> legendary = GetBrawlersByRarity("legendary");
            List<int> ultra_legendary = GetBrawlersByRarity("ultra_legendary");
            List<int> brawlers_list = BrawlersRoad;
            List<int> road_list = new List<int> { };

            if (HomeMode.Home.NewRecruitBrawler != 0)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + HomeMode.Home.NewRecruitBrawler))
                {
                    road_list.Add(HomeMode.Home.NewRecruitBrawler);
                }
            }
            foreach (int brawler in brawlers_list)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
                {
                    road_list.Add(brawler);
                }
            }
            if (road_list.Count != 0) RecruitBrawler = road_list[0];
            else RecruitBrawler = -1;


            stream.WriteBoolean(RecruitBrawler != -1);

            if (RecruitBrawler != -1)
            {

                stream.WriteDataReference(16, RecruitBrawler); // brawler id

                if (rare.Contains(RecruitBrawler))
                    stream.WriteVInt(160);
                else if (super_rare.Contains(RecruitBrawler))
                    stream.WriteVInt(430);
                else if (epic.Contains(RecruitBrawler))
                    stream.WriteVInt(925);
                else if (mythic.Contains(RecruitBrawler))
                    stream.WriteVInt(1900);
                else if (legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(3800);
                else if (ultra_legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(5500);
                else
                    stream.WriteVInt(0);


                if (rare.Contains(RecruitBrawler))
                    stream.WriteVInt(29);
                else if (super_rare.Contains(RecruitBrawler))
                    stream.WriteVInt(79);
                else if (epic.Contains(RecruitBrawler))
                    stream.WriteVInt(169);
                else if (mythic.Contains(RecruitBrawler))
                    stream.WriteVInt(349);
                else if (legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(699);
                else if (ultra_legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(999);
                else
                    stream.WriteVInt(0);


                stream.WriteVInt(0);
                stream.WriteVInt(RecruitTokens);
                stream.WriteVInt(0);
                stream.WriteVInt(0);


                int allbrawlers = road_list.Count - 1;
                int srb_index = 0;
                stream.WriteVInt(allbrawlers);
                foreach (int x in road_list)
                {
                    if (srb_index == 0)
                    {
                        srb_index++;
                        continue;
                    }

                    stream.WriteDataReference(16, x);

                    if (rare.Contains(x))
                        stream.WriteVInt(160);
                    else if (super_rare.Contains(x))
                        stream.WriteVInt(430);
                    else if (epic.Contains(x))
                        stream.WriteVInt(925);
                    else if (mythic.Contains(x))
                        stream.WriteVInt(1900);
                    else if (legendary.Contains(x))
                        stream.WriteVInt(3800);
                    else if (ultra_legendary.Contains(x))
                        stream.WriteVInt(5500);
                    else
                    {
                        // Console.WriteLine("CHLEEEEEEEEEEEEEEEEEEEEEEENNNNNNN = " + x);
                        // Console.WriteLine("CHLEEEEEEEEEEEEEEEEEEEEEEENNNNNNN");
                        // Console.WriteLine("CHLEEEEEEEEEEEEEEEEEEEEEEENNNNNNN");
                        // Console.WriteLine("CHLEEEEEEEEEEEEEEEEEEEEEEENNNNNNN");
                        // Console.WriteLine("CHLEEEEEEEEEEEEEEEEEEEEEEENNNNNNN");
                        stream.WriteVInt(0);
                    }


                    if (rare.Contains(x))
                        stream.WriteVInt(29);
                    else if (super_rare.Contains(x))
                        stream.WriteVInt(79);
                    else if (epic.Contains(x))
                        stream.WriteVInt(169);
                    else if (mythic.Contains(x))
                        stream.WriteVInt(349);
                    else if (legendary.Contains(x))
                        stream.WriteVInt(699);
                    else if (ultra_legendary.Contains(x))
                        stream.WriteVInt(999);
                    else
                        stream.WriteVInt(0);


                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(srb_index); //
                    srb_index++;
                    stream.WriteVInt(0);
                }


                stream.WriteVInt(0);

                stream.WriteVInt(0);
            }
            else
            {
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
            }

            stream.WriteVInt(97);
            for (int i = 0; i < 97; i++)
            {
                Hero hero = HomeMode.Avatar.GetHero(GlobalId.CreateGlobalId(16, i));
                stream.WriteVInt(hero != null ? hero.MasteryPoints : 0);
                stream.WriteVInt(hero != null ? hero.ClaimedMasteryLVL : 0);
                stream.WriteDataReference(16, i);
            }

            DefaultBattleCard.Encode(stream);

            stream.WriteVInt(0); //sub_D5C83C

            StarrDrop.Encode(stream, HomeMode);

            stream.WriteBoolean(true);//v53
            stream.WriteVInt(1488);
            stream.WriteVInt(30);
            stream.WriteVInt(PiggyBankTickets); // tickets

        }
        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            {
                DateTime timer_now = DateTime.Now;
                if (timer_now > timer_start)
                {
                    if (timer_now < timer_end)
                    {
                        int time_sec = (int)(timer_end - timer_now).TotalSeconds;
                        return time_sec;
                    }
                    else
                    {
                        return -1;
                    }
                }
                else
                {
                    return -1;
                }
            }
        }
    }
}

