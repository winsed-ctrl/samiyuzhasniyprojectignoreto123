namespace GromCore.Laser.Logic.Message.Home
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;

    public class LogicOffersChangedMessage : GameMessage
    {
        public LogicOffersChangedMessage() : base()
        {
            ;
        }

        public List<OfferBundle> OfferBundles;

        public override void Encode()
        {
            Stream.WriteVInt(211);
            //Stream.WriteVInt(211);
            try{
            Stream.WriteVInt(OfferBundles.Count); // Shop offers at 0x78e0c4
            foreach (OfferBundle offerBundle in OfferBundles)
            {
                offerBundle.Encode(Stream);
            }}
            catch{
                ;
            }
        }

        public override int GetMessageType()
        {
            return 24111;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
