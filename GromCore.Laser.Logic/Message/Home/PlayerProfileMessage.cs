namespace GromCore.Laser.Logic.Message.Home
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;

    public class PlayerProfileMessage : GameMessage
    {
        public Profile Profile;
        public AllianceHeader AllianceHeader;
        public AllianceRole AllianceRole;

        public PlayerProfileMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Profile.Encode(Stream);

            if (Stream.WriteBoolean(AllianceHeader != null))
            {
                AllianceHeader.Encode(Stream);
            }

            if (AllianceRole > AllianceRole.None)
                ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(25, (int)AllianceRole));
            else
                Stream.WriteVInt(0);

            Stream.WriteVInt(0);// не используетс
        }

        public override int GetMessageType()
        {
            return 24113;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
