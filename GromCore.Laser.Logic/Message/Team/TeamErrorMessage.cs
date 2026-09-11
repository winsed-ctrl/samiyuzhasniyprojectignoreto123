namespace GromCore.Laser.Logic.Message.Team
{
    using GromCore.Laser.Logic.Helper;

    public class TeamErrorMessage : GameMessage
    {
        public int ErrorCode;

        public override void Encode()
        {
            Stream.WriteVInt(ErrorCode);
            Stream.WriteVInt(ErrorCode);
        }

        public override int GetMessageType()
        {
            return 24129;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
