using Newtonsoft.Json;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Stream.Entry;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Home.Items
{
    public class LogicRecruitRoad
    {
        [JsonProperty] public List<LogicBrawlerRecruit> UnlockList = new List<LogicBrawlerRecruit> { };
        [JsonProperty] public LogicBrawlerRecruit NowRecruit = new LogicBrawlerRecruit { GlobalId = GlobalId.CreateGlobalId(16, 1), GemsPrice = 1900, TokenPrice = 1999 };
        [JsonProperty] public Dictionary<int, int> Sale = new();
        [JsonProperty] public bool FirstGenerate;
        [JsonProperty] public int? PreferredBrawlerGlobalId;
        [JsonProperty] public int CurrentPatternIndex = 0;

        public void Encode(ByteStream stream, ClientHome Home)
        {
            stream.WriteBoolean(true);
            stream.WriteVInt(0);
            if (false) // int ^^ > 0
                ByteStreamHelper.WriteDataReference(stream, 0);
            stream.WriteVInt(0);
            if (false) // int ^^ > 0
                ByteStreamHelper.WriteDataReference(stream, 0);
            ByteStreamHelper.WriteDataReference(stream, 0);

            stream.WriteBoolean(NowRecruit != null);
            if (NowRecruit != null) NowRecruit.Encode(stream, Home);
            stream.WriteVInt(UnlockList.Count);
            if (UnlockList.Count > 0)
                foreach (LogicBrawlerRecruit _l in UnlockList)
                    _l.Encode(stream, Home);
            stream.WriteVInt(Sale.Count);
            if (Sale.Count > 0)
                foreach (KeyValuePair<int, int> kvp in Sale)
                    stream.WriteLogicLong(kvp.Key, kvp.Value);
            stream.WriteVInt(0);
        }
        public void CommandEncode(ByteStream stream, ClientHome Home)
        {
            stream.WriteBoolean(NowRecruit != null);
            if(NowRecruit != null) NowRecruit.Encode(stream, Home);
            stream.WriteVInt(UnlockList.Count);
            if (UnlockList.Count > 0)
                foreach (LogicBrawlerRecruit _l in UnlockList)
                    _l.Encode(stream, Home);
            stream.WriteVInt(0);
            //if (Sale.Count > 0)
            //    foreach (KeyValuePair<int, int> kvp in Sale)
            //        stream.WriteLogicLong(kvp.Key, kvp.Value);
            stream.WriteVInt(0);
        }
        public static int GetGemPriceForRarity(int RecruitBrawler)
        {
            if (GeneralStaticLogic.rare.Contains(RecruitBrawler))
                return 29;
            else if (GeneralStaticLogic.super_rare.Contains(RecruitBrawler))
                return 79;
            else if (GeneralStaticLogic.epic.Contains(RecruitBrawler))
                return 169;
            else if (GeneralStaticLogic.mythic.Contains(RecruitBrawler))
                return 349;
            else if (GeneralStaticLogic.legendary.Contains(RecruitBrawler))
                return 699;
            else if (GeneralStaticLogic.ultra_legendary.Contains(RecruitBrawler))
                return 999;
            else
                return 99999;
        }
        public static int GetTokenPriceForRarity(int RecruitBrawler)
        {
            if (GeneralStaticLogic.rare.Contains(RecruitBrawler))
                return 160;
            else if (GeneralStaticLogic.super_rare.Contains(RecruitBrawler))
                return 430;
            else if (GeneralStaticLogic.epic.Contains(RecruitBrawler))
                return 925;
            else if (GeneralStaticLogic.mythic.Contains(RecruitBrawler))
                return 1900;
            else if (GeneralStaticLogic.legendary.Contains(RecruitBrawler))
                return 3800;
            else if (GeneralStaticLogic.ultra_legendary.Contains(RecruitBrawler))
                return 5500;
            else
                return 99999;
        }
        public void SetSaleForRarity(int rarity, int sale)
        {
            if (Sale.Keys.Any(r => r == rarity))
            {

            }

        }

    }
}
