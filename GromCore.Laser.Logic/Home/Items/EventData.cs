namespace GromCore.Laser.Logic.Home.Items
{
    using Masuda.Net.Models;
    using Newtonsoft.Json.Converters;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Titan.Util;
    using System.Buffers.Text;
    using System.Diagnostics.SymbolStore;
    using System.IO;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Text;
    public class ChallengeGemOffer
    {
        public int RewardGemOfferType { get; set; }
        public int RewardGemOfferCount { get; set; }
        public int RewardGemOfferData1 { get; set; }
        public int RewardGemOfferData2 { get; set; }
        public int RewardGemOfferExtra { get; set; }
    }
    public class EventData
    {
        public int Slot;
        public int LocationId;
        public DateTime EndTime;
        public DateTime StartTime;
        public DateTime TimeToNext;
        public LocationData Location => DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(LocationId);
        public BattlePlayerMap BattlePlayerMap;
        public HashSet<int> modifi = new();
        public string Title;
        public string SubTitle;
        public int ChallengeLosePerStep;
        public int ChallengeType;
        public int GemOfferType;
        public int GemOfferCount;
        public int GemOfferData1;
        public int GemOfferData2;
        public int GemOfferExtra;
        public List<int> ExtraLives = new();
        public string FileHash;
        public string FileName;
        public List<ChallengeGemOffer> cgemofferlist = new();
        public bool IsChallengeSlot => Slot >= 20 && Slot <= 24;
        public readonly int ChampieWins = 3;

        public void Encode(ByteStream stream, bool encodeTimeToNext = false, int playerLoses = 0, int playerWins = 0)
        {
            if (Slot == 12 || Slot == 13)
            {
                stream.WriteVInt(0);
                stream.WriteVInt(Slot);
                stream.WriteVInt(1);
                stream.WriteVInt(0);
                stream.WriteVInt(72292);
                stream.WriteVInt(10);
                ByteStreamHelper.WriteDataReference(stream, LogicServerListener.Instance.GetIntForBMap());
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
                if (stream.WriteBoolean(Slot == 13)) ByteStreamHelper.WriteBattlePlayerMap(stream, LogicServerListener.Instance.GetWinnerMap() != null 
                    ? new(LogicServerListener.Instance.GetWinnerMap())
                    : new(new PlayerMap()
                      {
                          MapEnvironmentData = 1,
                          MapName = "",
                          GMV = 6,
                          MapData = ZLibHelper.DecompressBase64Map("VA4AAHicM+AyMwAhPQrA4NBsSFA1morB4exRzaOah5tmwlkRCzCkhs2jmke8Zt8Bs3lU8+DSPFoMjWoexJpHk+eI1WyIl0tLm+mhGQC2/4qu"),
                          MapId = RandomNumberGenerator.GetInt32(0, 2147483647),
                          AccountId = 0,
                          AvatarName = "dnull be here!!"
                      })); 
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
                return;
            }
            if (modifi == null) modifi = new();
            if(Slot == 35)
                stream.WriteVInt(1488);
            else
                stream.WriteVInt(-1);
            stream.WriteVInt(Slot);
            stream.WriteVInt(0);


            int secondsToNext = 0;
            if (encodeTimeToNext && Slot != 34 && Slot != 14 && Slot != 15) secondsToNext = (int)(TimeToNext - DateTime.Now).TotalSeconds;
            if (Slot == 35)
                stream.WriteVInt((int)(StartTime - DateTime.Now).TotalSeconds);
            else
                stream.WriteVInt(secondsToNext > 0 ? secondsToNext : 0);
            stream.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds);
            stream.WriteVInt(0);

            if (Slot == 12 || Slot == 13 || Slot == 35 || Slot == 18 || Slot == 34 || Slot == 14 || Slot == 15)
                ByteStreamHelper.WriteDataReference(stream, null);
            else
                ByteStreamHelper.WriteDataReference(stream, Location);
            stream.WriteVInt(-1); // GameModeVaridation
            stream.WriteVInt(2);
            stream.WriteString(null);
            stream.WriteVInt(0);
            stream.WriteVInt(playerLoses);
            stream.WriteVInt(ChallengeLosePerStep);
            stream.WriteVInt(modifi.Count); // modifier
            if (modifi.Count > 0)
            {
                foreach (int i in modifi)
                {
                    stream.WriteVInt(i);
                }
            }
            stream.WriteVInt(playerWins);
            stream.WriteVInt(ChallengeType);
            ByteStreamHelper.WriteBattlePlayerMap(stream, BattlePlayerMap);
            stream.WriteVInt(0);
            stream.WriteBoolean(Slot == 14 || Slot == 15); // ranked
            if(Slot == 14)
            {
                stream.WriteVInt(GeneralStaticLogic.RankedSeason);
                stream.WriteString(GeneralStaticLogic.RankedSeasonTID);
                stream.WriteVInt(0);
                stream.WriteVInt(0);

                stream.WriteByte((byte)GeneralStaticLogic.QuestForRanked.Count);
                foreach(GeneralStaticLogic.LogicRewardConfig cfg in GeneralStaticLogic.QuestForRanked)
                {
                    stream.WriteBoolean(true); // LogicRewardConfig
                    stream.WriteVInt((byte)cfg.QuestType); // Quest Type
                    stream.WriteVInt((byte)cfg.NeededRank); // Rank
                    stream.WriteBoolean(true); // LogicRewardConfig
                    cfg.GemOffer.Encode(stream);
                }

                stream.WriteVInt(1); // Quests Count
                stream.WriteVInt(-1);
                stream.WriteVInt(-1);

                stream.WriteVInt(19);// Road Count
                for(int j = 1; j < 20; j++)
                {
                    stream.WriteVInt(j);
                    stream.WriteVInt(500);
                }
            }

            if(Slot == 15)
            {
                stream.WriteVInt(GeneralStaticLogic.RankedSeason);
                stream.WriteString(GeneralStaticLogic.RankedSeasonTID);
                stream.WriteVInt(0);
                stream.WriteVInt(0);

                stream.WriteByte((byte)GeneralStaticLogic.QuestForRanked.Count);
                foreach (GeneralStaticLogic.LogicRewardConfig cfg in GeneralStaticLogic.QuestForRanked)
                {
                    stream.WriteBoolean(true); // LogicRewardConfig
                    stream.WriteVInt((byte)cfg.QuestType); // Quest Type
                    stream.WriteVInt((byte)cfg.NeededRank); // Rank
                    stream.WriteBoolean(true); // LogicRewardConfig
                    cfg.GemOffer.Encode(stream);
                }

                stream.WriteVInt(0); // Quests Count

                stream.WriteVInt(19);// Road Count
                for (int j = 1; j < 20; j++)
                {
                    stream.WriteVInt(j);
                    stream.WriteVInt(500);
                }
            }
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            if (stream.WriteBoolean(!String.IsNullOrEmpty(Title)))
                stream.ChronosTextEntry(Title, 0);
            if(Slot != 18)
            {
                if (stream.WriteBoolean(!String.IsNullOrEmpty(SubTitle)))
                    stream.ChronosTextEntry(SubTitle, 0);
            }
            else
            {
                stream.WriteBoolean(true);
                stream.ChronosTextEntry("+1 за уничтожение/-1 за смерть", 0);
            }
                stream.WriteBoolean(false);
            if (stream.WriteBoolean(GemOfferType > 0))
            {
                stream.WriteVInt(GemOfferType);
                stream.WriteVInt(GemOfferCount);
                stream.WriteDataReference(GemOfferData1, GemOfferData2);
                stream.WriteVInt(GemOfferExtra);
            }
            stream.WriteVInt(ExtraLives.Count);
            foreach (int live in ExtraLives)
                stream.WriteVInt(0);
            if (stream.WriteBoolean(!String.IsNullOrEmpty(FileHash)))
                stream.ChronosFileEntry(FileHash, FileName);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            if(Slot == 34)
            {
                stream.WriteVInt(GeneralStaticLogic.EventListForChaos.Count);
                foreach (int e in GeneralStaticLogic.EventListForChaos)
                {
                    stream.WriteDataReference(15, e);
                }

                stream.WriteVInt(GeneralStaticLogic.BrawlerListForChaos.Count);
                foreach (int e in GeneralStaticLogic.BrawlerListForChaos)
                {
                    stream.WriteDataReference(16, e);
                }
            }
            else
            {
                if(Slot == 35)
                {
                    stream.WriteVInt(GeneralStaticLogic.EventListForSecret.Count);
                    foreach (int e in GeneralStaticLogic.EventListForSecret)
                    {
                        stream.WriteDataReference(15, e);
                    }
                }
                else
                    stream.WriteVInt(0);
                stream.WriteVInt(0);
            }
            if (Slot == 35)
            {
                stream.WriteVInt(modifi.Count);
                foreach (int i in modifi)
                    stream.WriteVInt(i);
            }
            else
                stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteBoolean(Slot == 35);
            if(Slot == 35)
            {
                stream.WriteVInt(3);
                stream.WriteVInt(1);
                stream.WriteVInt(3);
                stream.WriteVInt(10);
                stream.WriteVInt(7);
                stream.WriteVInt(5);
            }
        }
    }
}