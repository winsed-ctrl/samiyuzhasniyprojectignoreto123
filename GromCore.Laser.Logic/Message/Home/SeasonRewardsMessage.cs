using GromCore.Laser.Logic.Home.Items;

namespace GromCore.Laser.Logic.Message.Home
{ 
    public class SeasonRewardsMessage : GameMessage
    {
        public List<EventData> eventData;
        public int Type;
        public override void Encode()
        {

            int[] start = { 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400 };
            int[] end = { 599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, -1 };
            int[] reward1 = { 50, 60, 80, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240 };
            int[] reset = { 549, 599, 649, 699, 749, 799, 849, 899, 924, 949, 974, 1024, 1049, 1074, 1099, 1124, 1149, 1174 };

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);

            switch (Type)
            {
                case 6:
                    List<EventData> challengeEvents = new List<EventData>();
 
                    if (eventData != null)
                    {
                        foreach (var ev in eventData)
                        {
                            if (ev != null && ev.Slot >= 20 && ev.Slot <= 24)
                            {
                                challengeEvents.Add(ev);
                            }
                        }
                    }
                    int t = 0;
                    foreach (var challengeEvent in challengeEvents)
                        if (challengeEvent.cgemofferlist != null)
                            t += challengeEvent.cgemofferlist.Count;

                    Stream.WriteVInt(t);
                    int index = 1; 
                    foreach (var challengeEvent in challengeEvents)
                    {
                        if (challengeEvent.cgemofferlist != null)
                        {
                            for (int i = 0; i < challengeEvent.cgemofferlist.Count; i++)
                            {
                                var reward = challengeEvent.cgemofferlist[i];

                                Stream.WriteBoolean(true); 
                                Stream.WriteVInt(index); 
                                Stream.WriteVInt(index);
                                Stream.WriteVInt(index); 
                                Stream.WriteBoolean(false);

                                Stream.WriteVInt(reward.RewardGemOfferType);
                                Stream.WriteVInt(reward.RewardGemOfferCount);
                                Stream.WriteDataReference(reward.RewardGemOfferData1, reward.RewardGemOfferData2);
                                Stream.WriteVInt(reward.RewardGemOfferExtra);

                                Stream.WriteBoolean(false);
                                Stream.WriteBoolean(false);
                                Stream.WriteBoolean(false);
                                index++;
                            }
                        }
                    }

                    if (challengeEvents.Count == 0)
                        Console.WriteLine("No challenge events found for slots 20-24");
                    break;
                case 1:
                    Stream.WriteVInt(start.Length); // stage

                    for (int x = 0; x < start.Length; x++)
                    {
                        Stream.WriteBoolean(true);
                        Stream.WriteVInt(start[x]);
                        Stream.WriteVInt(end[x]);
                        Stream.WriteVInt(reset[x]);
                        Stream.WriteBoolean(false);
                        Stream.WriteVInt(49);
                        Stream.WriteVInt(reward1[x]);
                        Stream.WriteDataReference(0); 
                        Stream.WriteVInt(0); 
                        Stream.WriteBoolean(false);
                        Stream.WriteBoolean(false);
                        Stream.WriteBoolean(false);
                    }
                    break;
                default:
                    Stream.WriteVInt(0);
                    break;
            }

            Stream.WriteVInt(Type); // type
           
        }

        public override int GetMessageType()
        {
            return 22033;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
