namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Titan.DataStream;

    public class LogicGiveDeliveryItemsCommand : Command
    {
        public readonly List<DeliveryUnit> DeliveryUnits;
        public int RewardTrackType { get; set; }
        public int RewardForRank { get; set; }
        public int BrawlPassSeason { get; set; }

        public bool BrawlPassExecute { get; set; }

        public bool StarrDropExecute { get; set; }

        public LogicGiveDeliveryItemsCommand() : base()
        {
            DeliveryUnits = new List<DeliveryUnit>();
        }

        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(0);

            // Исправлено: используем оригинальный список напрямую
            DeliveryUnit[] unitsSnapshot = DeliveryUnits.ToArray();
            stream.WriteVInt(unitsSnapshot.Length);
            foreach (DeliveryUnit unit in unitsSnapshot)
            {
                unit.Encode(stream);
            }

            if (StarrDropExecute)
            {
                stream.WriteByte(1);
                stream.WriteVInt(200);
                stream.WriteVInt(200);
                stream.WriteVInt(5);
                stream.WriteVInt(93);
                stream.WriteVInt(206);
                stream.WriteVInt(456);
                stream.WriteVInt(1001);
                stream.WriteVInt(2264);

                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(5);
                stream.WriteByte(2);
                stream.WriteDataReference(0, 0);
                stream.WriteVInt(10596);
                stream.WriteVInt(10596);
            }

            stream.WriteBoolean(false);
            stream.WriteVInt(RewardTrackType);
            stream.WriteVInt(RewardForRank);
            stream.WriteVInt(BrawlPassSeason);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            // ИСПРАВЛЕНО: создаём копию списка перед итерацией
            // Это предотвращает ошибку "Collection was modified; enumeration operation may not execute"
            List<DeliveryUnit> unitsCopy = DeliveryUnits.ToList();
            
            foreach (DeliveryUnit unit in unitsCopy)
            {
                List<GatchaDrop> dropsCopy = unit.GetDrops().ToList();
                foreach (GatchaDrop drop in dropsCopy)
                {
                    drop.DoDrop(homeMode);
                }
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 203;
        }
    }
}
