namespace GromCore.Laser.Logic.Home.Gatcha
{
    using GromCore.Laser.Titan.DataStream;

    public class DeliveryUnit
    {
        public int Type;
        public List<GatchaDrop> Drops;

        public DeliveryUnit(int type)
        {
            Type = type;
            Drops = new List<GatchaDrop>();
        }

        public void AddDrop(GatchaDrop drop)
        {
            if (drop != null)
            {
                Drops.Add(drop);
            }
        }

        public GatchaDrop[] GetDrops()
        {
            return Drops.ToArray();
        }

        public void Encode(ByteStream stream)
        {
            GatchaDrop[] dropsSnapshot = Drops.ToArray();
            stream.WriteVInt(Type);
            stream.WriteVInt(dropsSnapshot.Length);
            foreach (GatchaDrop drop in dropsSnapshot)
            {
                drop.Encode(stream);
            }
        }
    }
}
