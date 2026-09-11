using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GromCore.Laser.Logic.Battle.Structures;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Home.Items;
using GromCore.Laser.Titan.DataStream;

namespace GromCore.Laser.Logic
{
    public static class GeneralStaticLogic
    {
        public class LogicRewardConfig
        {
            public int QuestType;
            public int NeededRank;
            public GemOffer GemOffer;
            public LogicRewardConfig(int a1, int a2, GemOffer a3)
            {
                QuestType = a1; NeededRank = a2; GemOffer = a3;
            }
        }
        public static List<int> LockedBrawlers = new List<int>  // instance id!!!
        {
            13
            // DataTables.GetCharacterByName("ClusterBombDude").GetInstanceId(),
            // DataTables.GetCharacterByName("BowDude").GetInstanceId(),
            // DataTables.GetCharacterByName("Duplicator").GetInstanceId(),
            // DataTables.GetCharacterByName("FishTank").GetInstanceId(),
            // DataTables.GetCharacterByName("Reviver").GetInstanceId(),
            // DataTables.GetCharacterByName("Cooker").GetInstanceId(),
            // DataTables.GetCharacterByName("Cocooner").GetInstanceId(),
            // DataTables.GetCharacterByName("Attacher").GetInstanceId(),
            // DataTables.GetCharacterByName("Twins").GetInstanceId()
            // DataTables.GetCharacterByName("Leaper").GetInstanceId(),
            // DataTables.GetCharacterByName("Duelist").GetInstanceId(),
            // DataTables.GetCharacterByName("Puppeteer").GetInstanceId(),
            // DataTables.GetCharacterByName("Splitter").GetInstanceId(),
            // DataTables.GetCharacterByName("DoorMan").GetInstanceId(),
            // DataTables.GetCharacterByName("Flea").GetInstanceId(),
            // DataTables.GetCharacterByName("Knight").GetInstanceId(),
            // DataTables.GetCharacterByName("StickyBomb").GetInstanceId(),
            // DataTables.GetCharacterByName("Flea").GetInstanceId(),
            // DataTables.GetCharacterByName("Angelo").GetInstanceId(),
            // DataTables.GetCharacterByName("Geisha").GetInstanceId(),
            // DataTables.GetCharacterByName("Cocooner").GetInstanceId()
        };
        public static List<int> UnlockedGadgets = new List<int> { };
        public static List<int> LockedUnquies = new List<int> {  };
        public static List<int> AllowedUnquies = new List<int> {  };
        public static List<int> AllowedGadgets = new List<int> {  };
        public static List<int> AllowedSPGs = new List<int> { 73 };
        public static List<int> AllowedOvercharges = new List<int> {  };
        private static Dictionary<int, string> LockedForChronosBrawlers = new Dictionary<int, string> { };// instance id && DateTime in str ("2045-03-01 12:30:00")
        private static Dictionary<int, string> LockedForChronosOvercharges = new Dictionary<int, string> { };// instance id && DateTime in str("2045-03-01 12:30:00")
        private static Dictionary<int, string> LockedForChronosSkins = new Dictionary<int, string> { };// instance id && DateTime in str( "2045-03-01 12:30:00")
        public static readonly List<string> BlockedWords = new List<string>
        {
            "tg",
            "ddos",
            "http",
            "https",
            "telegram",
            "��������",//git with russian words problem, i guess uk this.
            "��",
            "tege",
            "telega",
            "����",
            "����",
            "ddos",
            "dd0s",
            "ddoz",
            "dd0z",
            "dudoz",
            "PiranhaDdos",
            "tme",
            "t.me",
            "t.me/",
            "@coolbrawlcheat",
            "@coolbrawl",
            "����",
            "���"
        };

        // public static readonly string BrawlPassTimer = "2025-09-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
        // public static readonly string SeasonTimer = "2025-09-06 15:00:00"; // DateTime in str( "2045-03-01 12:30:00")
        // public static readonly bool TwoDropsEvent;
        // public static readonly int ShopSkinsSetId = -1;
        // public static readonly int ThemeId;
        // public static readonly string ShopSkinsSetTimer = "2045-03-01 12:30:00"; // DateTime in str( "2045-03-01 12:30:00")
        public static readonly bool ProfanityEnabled = false;
        public static readonly bool IsCustom = false;
        // Theme used for new accounts and for accounts without a personal
        // preference. The Telegram bot can change this value at runtime.
        public static int DefaultThemeId = 88;
        public static int MaxMutesForPlayer = 15 ;
        public static int MaxBansForPlayer = 10 ;
        public const double GEMS_PER_COIN = 0.1;
        public const double GEMS_PER_BLING = 31.25;
        public static List<int> BlockedObtainTypes = new List<int> { 1, 7, 4, 6, 3 };
        public static bool IsMaintence = false;
        public static DateTime MaintenceEndTimer = DateTime.Parse("2025-07-26 19:40");
        public static volatile bool CoinsForWinEvent = false;
        public static volatile bool BonusTrophiesForWinEvent = false;
        public static volatile bool StarrDropForWinEvent = false;
        public static int MaxMastery = 24800;
        public static bool DisableGadgets = false;
        public static bool EnableMessageDebbuging = true;
        public static bool AcceptDebugMessage = false;
        public static bool UseOldPathFinder = false;// i prefer use true bcs aislop shit dont work
        public static int RankedSeason = 0;
        public static string RankedSeasonTID = "TID_BRAWL_PASS_SEASON_4";
        public static List<LogicRewardConfig> QuestForRanked = new List<LogicRewardConfig>
        {
            {new(4, 30, new(25,1, 0,63-3)) },
            {new(2, 7, new(25,1, 0,64-3)) },
            {new(1, 60, new(26,1,0,277-3)) },
        };

        public static List<int> rare ;
        public static List<int> super_rare ;
        public static List<int> epic;
        public static List<int> mythic;
        public static List<int> legendary;
        public static List<int> ultra_legendary;


        public static readonly List<DailyCalendarData.DailyCalendarGemOffer> calendarData = new()
        {
            new() { CalendarId = 0, Day = 0, Count = 1, ShopItemCount1 = 1, ShopItemId1 = ShopItem.Skin, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }, ShopItemExtraData1 = 29 },
            new() { CalendarId = 0, Day = 1, Count = 1, ShopItemCount1 = 99999, ShopItemId1 = ShopItem.Coin, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }, ShopItemExtraData1 = 5 },

            new() { CalendarId = 1, Day = 2, Count = 2, ShopItemCount1 = 1, ShopItemId1 = ShopItem.GuaranteedHero, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 16 }, { 1, 18 } }, ShopItemExtraData1 = 0, ShopItemCount2 = 1, ShopItemId2 = ShopItem.GuaranteedHero, ShopItemDataReference2 = new Dictionary<int, int> { { 0, 16 }, { 1, 20 } }, ShopItemExtraData2 = 0 },
            new() { CalendarId = 1, Day = 3, Count = 1, ShopItemCount1 = 2, ShopItemId1 = ShopItem.Skin, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }, ShopItemExtraData1 = 52, ShopItemCount2 = 1, ShopItemId2 = ShopItem.Skin, ShopItemDataReference2 = new Dictionary<int, int> { { 0, 16 }, { 1, 20 } }, ShopItemExtraData2 = 29 },

            new() { CalendarId = 2, Day = 2, Count = 1, ShopItemCount1 = 1, ShopItemId1 = ShopItem.Coin, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }, ShopItemExtraData1 = 5 },
            new() { CalendarId = 2, Day = 3, Count = 1, ShopItemCount1 = 1, ShopItemId1 = ShopItem.Coin, ShopItemDataReference1 = new Dictionary<int, int> { { 0, 0 }, { 1, 0 } }, ShopItemExtraData1 = 5 },
        };
        public static readonly string CustomPreset = "grom"; 
        public static void InitPreset()
        {
            if (CustomPreset == "grom")
            {
                rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
                super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
                epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 60, 68 };
                mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66 };
                legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76 };
                ultra_legendary = new List<int> { };
            }
        }
        public static List<int> EventListForSecret = new()
        {
            697-3,
            673-3,
            667-3,
            660-3,
            27-3,
            8-3,
            5-3
        };
        public static List<int> EventListForChaos = new()
        {
            17-3,
            18-3,
            19-3,
            16-3,
            46-3,
            48-3
        }; 
        public static List<int> BrawlerListForChaos = new()
        {
            0,
            5-3,
            27-3,
            37-3,
            46-3,
            42-3
        };
        public static List<string> BlockedEventsForRanked = new()
        {
            "Knockout",
            "KingOfHill"
        };

        //public static DateTime calendarTimer = DateTime.Parse("2025-10-00 12:00");
    }
}
