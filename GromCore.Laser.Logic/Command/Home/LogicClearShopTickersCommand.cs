namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;

    public class LogicClearShopTickersCommand : Command
    {
        int index;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            // Console.WriteLine(stream.ReadVInt());
            // Console.WriteLine(stream.ReadVInt());
            // Console.WriteLine(stream.ReadVInt());
            // Console.WriteLine(stream.ReadVInt());
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
        }
        public override int Execute(HomeMode homeMode)
        {
            OfferBundle[] bundles = homeMode.Home.OfferBundles.ToArray();
            //OfferBundle bundle = homeMode.Home.OfferBundles[index];
            foreach (OfferBundle bundle in bundles)
            {
            bundle.State = 2;
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 515;
        }
    }
}
