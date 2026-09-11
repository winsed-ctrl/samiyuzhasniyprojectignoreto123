namespace GromCore.Laser.Logic.Message.Club
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;

    public class MyAllianceMessage : GameMessage
    {
        public int OnlineMembers;
        public AllianceRole Role;
        public AllianceHeader AllianceHeader { get; set; }

        public override void Encode()
        {
            Stream.WriteVInt(OnlineMembers);
            if (Stream.WriteBoolean(AllianceHeader != null))
            {
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(25, (int)Role));
                AllianceHeader.Encode(Stream);
            }
            Stream.WriteBoolean(true); // piggy
            Stream.WriteVInt(1488);
            Stream.WriteVInt(1488); // idk
            Stream.WriteVInt(1488); // piggy wins
            Stream.WriteBoolean(true);
        }

        public override int GetMessageType()
        {
            return 24399;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
