namespace GromCore.Laser.Logic.Message.Club
{
    public class RequestJoinAllianceMessage : GameMessage
    {
        public long AllianceId;
        public string Desc;
        public override void Decode()
        {
            base.Decode();
            AllianceId = Stream.ReadLong();
            Desc = Stream.ReadString();
        }
        public override int GetMessageType()
        {
            return 14317;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
