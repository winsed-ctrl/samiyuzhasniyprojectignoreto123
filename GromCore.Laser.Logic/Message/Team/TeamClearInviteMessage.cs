namespace GromCore.Laser.Logic.Message.Team
{
    public class TeamClearInviteMessage : GameMessage
    {
        public long InviteId;

        public override void Decode()
        {
            InviteId = Stream.ReadLong();
        }

        public override int GetMessageType()
        {
            return 14367;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
