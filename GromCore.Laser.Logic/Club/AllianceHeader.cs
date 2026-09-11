namespace GromCore.Laser.Logic.Club
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;
    using System.Diagnostics.Metrics;

    public class AllianceHeader
    {
        private long Id;

        private string Name;
        private int Badge;
        private int PlayersCount;
        private int Trophies;
        private int RequiredTrophies;
        private int Type;
        private string Country;

        public AllianceHeader(long id, string name, int badge, int playersCount, int trophies, int requiredTrophies, int type, string country)
        {
            Id = id;
            Name = name;
            Badge = badge;
            PlayersCount = playersCount;
            Trophies = trophies;
            RequiredTrophies = requiredTrophies;
            Country = country;
            Type = type;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(Id);
            stream.WriteString(Name);
            ByteStreamHelper.WriteDataReference(stream, Badge);
            stream.WriteVInt(Type); // Type
            stream.WriteVInt(PlayersCount);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(RequiredTrophies); // trophies required
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteString(Country);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);//v53
        }
    }
}
