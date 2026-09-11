using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Ranking
{
    public class GetLeaderboardMessage : GameMessage
    {
        public bool IsRegional { get; set; }
        public int LeaderboardType { get; set; }
        public int HeroDataId;
        public override void Decode()
        {
            base.Decode();
            
            IsRegional = Stream.ReadBoolean();
            LeaderboardType = Stream.ReadVInt();
            HeroDataId = ByteStreamHelper.ReadDataReference(Stream);
            //Console.WriteLine(Stream.ReadVInt());
        }

        public override int GetMessageType()
        {
            return 14403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
