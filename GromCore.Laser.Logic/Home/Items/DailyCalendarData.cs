using GromCore.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using GromCore.Laser.Titan.DataStream;
using Newtonsoft.Json;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Home.Items;
using System.Globalization;
using GromCore.Laser.Logic;

namespace DailyCalendarData
{
    [JsonObject(MemberSerialization.OptOut)]

    public class DailyCalendarGemOffer
    {
        public int CalendarId;
        public int Day;
        public int Count;

        public ShopItem ShopItemId1;
        public int ShopItemCount1;
        public Dictionary<int, int> ShopItemDataReference1;
        public int ShopItemExtraData1;


        public ShopItem ShopItemId2;
        public int ShopItemCount2;
        public Dictionary<int, int> ShopItemDataReference2;
        public int ShopItemExtraData2;

    }
    public class DailyCalendarData
    {
        public List<DailyCalendarGemOffer> dailyCalendarGemOffers = GeneralStaticLogic.calendarData;
        public int DailyRewardsType;
        public int DailyRewatdsRoadCount = 1;

        public int DailyRewardsDay = 1;
        public int DailyRewardsClaimedDay;
        public int DailyRewardTimer;
        public int DailyRewardTimerForNextReward;

        public void Encode(ByteStream stream, HomeMode player)
        {
            stream.WriteVInt(0);
            stream.WriteVInt(2);
            stream.WriteVInt(0); // day
            stream.WriteVInt(1);

            foreach (DailyCalendarGemOffer offer in dailyCalendarGemOffers)
            {
                if (offer.Day != 0) continue;
                stream.WriteVInt((int)offer.ShopItemId1);
                stream.WriteVInt(offer.ShopItemCount1);
                stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference1[0], offer.ShopItemDataReference1[1]));
                stream.WriteVInt(offer.ShopItemExtraData1);
                stream.WriteBoolean(false);
            }



            stream.WriteVInt(1);
            stream.WriteVInt(1); // Count

            foreach (DailyCalendarGemOffer offer in dailyCalendarGemOffers)
            {
                if (offer.Day != 1) continue;
                stream.WriteVInt((int)offer.ShopItemId1);
                stream.WriteVInt(offer.ShopItemCount1);
                stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference1[0], offer.ShopItemDataReference1[1]));
                stream.WriteVInt(offer.ShopItemExtraData1);
                stream.WriteBoolean(false);
            }

            stream.WriteVInt(DailyRewatdsRoadCount); // Calendars Count

            stream.WriteVInt(1);
            
            stream.WriteVInt(dailyCalendarGemOffers.Count(o => o.CalendarId == 1)); // Days

            foreach (DailyCalendarGemOffer offer in dailyCalendarGemOffers)
            {
                if (DailyRewatdsRoadCount == 2 && offer.CalendarId != 1) continue;
                if (offer.Day == 1 || offer.Day == 0) continue;
                stream.WriteVInt(offer.Day); // Day
                stream.WriteVInt(offer.Count); // CountRewards

                stream.WriteVInt((int)offer.ShopItemId1);
                stream.WriteVInt(offer.ShopItemCount1);
                stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference1[0], offer.ShopItemDataReference1[1]));
                stream.WriteVInt(offer.ShopItemExtraData1);
                stream.WriteBoolean(false);

                if(offer.Count == 2)
                {
                    stream.WriteVInt((int)offer.ShopItemId2);
                    stream.WriteVInt(offer.ShopItemCount2);
                    stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference2[0], offer.ShopItemDataReference2[1]));
                    stream.WriteVInt(offer.ShopItemExtraData2);
                    stream.WriteBoolean(false);
                }
            }

            if(DailyRewatdsRoadCount == 2)
            {
                stream.WriteVInt(2);

                stream.WriteVInt(dailyCalendarGemOffers.Count(o => o.CalendarId == 2)); // Days

                foreach (DailyCalendarGemOffer offer in dailyCalendarGemOffers)
                {
                    if (DailyRewatdsRoadCount == 2 && offer.CalendarId != 1) continue;
                    if (offer.Day == 1 || offer.Day == 0) continue;
                    stream.WriteVInt(offer.Day); // Day
                    stream.WriteVInt(offer.Count); // CountRewards

                    stream.WriteVInt((int)offer.ShopItemId1);
                    stream.WriteVInt(offer.ShopItemCount1);
                    stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference1[0], offer.ShopItemDataReference1[1]));
                    stream.WriteVInt(offer.ShopItemExtraData1);
                    stream.WriteBoolean(false);

                    if (offer.Count == 2)
                    {
                        stream.WriteVInt((int)offer.ShopItemId2);
                        stream.WriteVInt(offer.ShopItemCount2);
                        stream.WriteDataReference(GlobalId.CreateGlobalId(offer.ShopItemDataReference2[0], offer.ShopItemDataReference2[1]));
                        stream.WriteVInt(offer.ShopItemExtraData2);
                        stream.WriteBoolean(false);
                    }
                }
            }

            stream.WriteVInt(2); // Day
            stream.WriteVInt(0); // CountdownTimer
            stream.WriteVInt(-1); // Timer
            stream.WriteVInt(1); // collected day
            stream.WriteVInt(1); // Road (if Poco Road - 1, if Brock Road - 2);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }

        public DailyCalendarGemOffer GetRewardData(int Day)
        {
           
            foreach (DailyCalendarGemOffer gem in dailyCalendarGemOffers)
            {
                if (gem.Day == Day) return gem;
            }
            return null;
        }
    }
}
