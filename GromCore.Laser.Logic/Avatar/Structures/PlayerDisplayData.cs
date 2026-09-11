namespace GromCore.Laser.Logic.Avatar.Structures
{
    using GromCore.Laser.Titan.DataStream;

    public class PlayerDisplayData
    {
        public int ThumbnailId;
        public int NameColorId;
        public string Name;
        public int HighNameColorId;
        public PlayerDisplayData()
        {
            ;
        }

        public PlayerDisplayData(int thumbnail, int namecolor, string name, bool hasPremiumPass, bool hasPremiumPassPlus)
        {
            ThumbnailId = thumbnail;
            NameColorId = namecolor;
            Name = name;
            if (hasPremiumPass || hasPremiumPassPlus)
            {
                HighNameColorId = namecolor;
            }
            else HighNameColorId = -1;
        }

        public void Encode(ByteStream stream, bool encodeWithoutName = false)
        {
            stream.WriteString(encodeWithoutName ? "" : Name);
            stream.WriteVInt(5000);
            stream.WriteVInt(ThumbnailId);
            stream.WriteVInt(NameColorId);
            stream.WriteVInt(HighNameColorId);

        }
    }
}
