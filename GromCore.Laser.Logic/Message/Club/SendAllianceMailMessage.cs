namespace GromCore.Laser.Logic.Message.Club
{
    using global::GromCore.Laser.Logic.Avatar;
    using global::GromCore.Laser.Logic.Club;
    using global::GromCore.Laser.Logic.Helper;

    public class SendAllianceMailMessage : GameMessage
    {
        public int a1;
        public string a2;

        public override void Decode()
        {
            base.Decode();
            a1 = Stream.ReadInt();
            a2 = Stream.ReadString();
        }
        public override int GetMessageType()
        {
            return 14330;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
