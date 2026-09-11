namespace GromCore.Laser.Logic.Message.Club
{
    using global::GromCore.Laser.Logic.Avatar;
    using global::GromCore.Laser.Logic.Club;
    using global::GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;

    public class SearchAllianceMessage : GameMessage
    {
        public string AllianceName;

        public override void Decode()
        {
            base.Decode();
            AllianceName = Stream.ReadString();
        }
        public override int GetMessageType()
        {
            return 14324;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
