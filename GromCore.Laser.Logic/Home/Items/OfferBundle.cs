namespace GromCore.Laser.Logic.Home.Items
{
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;

    public class OfferBundle
    {
        public List<Offer> Items;
        public int Currency;
        public int Cost;
        public bool IsDailyDeals;
        public bool Purchased;
        public DateTime StartTime;
        public DateTime EndTime;
        public int OldCost;
        public string Title;
        public string BackgroundExportName;
        public string Claim;
        public int State;

        public int IsTID;
        public bool IsTrue;
        public int OfferType; // 1 - daily offer 2 - jackpot
        public bool OneTimeOffer;
        public bool LoadOnStartup;
        public bool Processed;
        public int TypeBenefit;
        public int Benefit;

        public int ShopPanelLayoutClass;
        public int ShopPanelLayoutType;

        public int ShopStyleSetClass;
        public int ShopStyleSetType;
        public int MultiCount;

        public bool specialOffer = false;


        public OfferBundle()
        {
            Items = new List<Offer>();
            State = 0;
        }

        public void Encode(ByteStream Stream)
        {
            Stream.WriteVInt(Items.Count);  // RewardCount
            foreach (Offer gemOffer in Items)
            {
                gemOffer.Encode(Stream);
            }

            Stream.WriteVInt(Currency); // currency
            Stream.WriteVInt(Cost); // cost
            Stream.WriteVInt((int)(EndTime - DateTime.Now).TotalSeconds); // Seconds left
            Stream.WriteVInt(State); // State
            Stream.WriteVInt(0); // ??
            Stream.WriteBoolean(Purchased); // already bought

            Stream.WriteVInt(0); // ??
            Stream.WriteVInt(0); // ??

            Stream.WriteBoolean(false); // is daily deals
            Stream.WriteVInt(OldCost); // Old cost???
            Stream.WriteString(Title); // Name
            Stream.WriteVInt(IsTID); // is tid
            Stream.WriteBoolean(LoadOnStartup); // LoadOnStartup
            Stream.WriteString(BackgroundExportName ?? "offer_generic");  // background
            Stream.WriteVInt(-1);
            Stream.WriteBoolean(Processed); // processed
            Stream.WriteVInt(TypeBenefit); // type benefit
            Stream.WriteVInt(Benefit); // benefit
            Stream.WriteString("");
            Stream.WriteBoolean(OneTimeOffer); // one-time-offer text 
            Stream.WriteBoolean(false);

            if (ShopPanelLayoutClass == 0 && ShopPanelLayoutType == 0)
            {
                Stream.WriteDataReference(0); // shop panel layouts 
            }
            else
            {
                Stream.WriteDataReference(ShopPanelLayoutClass, ShopPanelLayoutType); // shop panel layouts 
            }

            if (ShopStyleSetClass == 0 && ShopStyleSetType == 0)
            {
                Stream.WriteDataReference(0); // shop panel layouts 
            }
            else
            {
                Stream.WriteDataReference(ShopStyleSetClass, ShopStyleSetType); // shop panel layouts 
            }

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(OfferType);
            if (ShopPanelLayoutType < 9)
            {
                Stream.WriteVInt(-1);
                Stream.WriteVInt(Cost);
                Stream.WriteBoolean(false);
                Stream.WriteBoolean(false);
                Stream.WriteVInt(0);
                Stream.WriteVInt(0);
                Stream.WriteBoolean(false);
                Stream.WriteVInt(0);
                Stream.WriteBoolean(false);
                Stream.WriteVInt(5);
                Stream.WriteVInt(-1);
                Stream.WriteVInt(0);
            }
            else {
                Stream.WriteVInt(16); // self.writeVInt(16)
                Stream.WriteVInt(77); // self.writeVInt(77)
                Stream.WriteBoolean(false); // self.writeBoolean(False)
                Stream.WriteBoolean(false); // self.writeBoolean(False)
                Stream.WriteVInt(2); // self.writeVInt(2)
                if (IsTID > 0) Stream.WriteVInt((int)(DateTime.Now.Date.AddDays(IsTID).AddHours(23) - DateTime.Now).TotalSeconds); // self.writeVInt(0) #�������� ����� (�����)
                else {
                    if ((int)(StartTime - DateTime.Now).TotalSeconds > 0) Stream.WriteVInt((int)(StartTime - DateTime.Now).TotalSeconds);
                    else Stream.WriteVInt(0);
                } // self.writeVInt(0) #�������� ����� (�����)
                Stream.WriteBoolean(false); // self.writeBoolean(False) #������� 
                Stream.WriteVInt(OfferType - 10); // self.writeVInt(0) #Day Gift
                Stream.WriteBoolean(ShopPanelLayoutType==333); // self.writeVInt(0) #Mode(0- Default, 1 - Gift)
                Stream.WriteVInt(0); // self.writeVInt(0) #Purchased(Gift offer)
                Stream.WriteVInt(-1); // self.writeVInt(-1)
                Stream.WriteVInt(0); // self.writeVInt(0)
            }
        }
    }
}
