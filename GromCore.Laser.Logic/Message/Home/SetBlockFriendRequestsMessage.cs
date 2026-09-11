namespace GromCore.Laser.Logic.Message.Club
{
    public class SetBlockFriendRequestsMessage : GameMessage
    {
        public bool state;

        public override void Decode()
        {
            state = Stream.ReadBoolean();
        }

        public override int GetMessageType()
        {
            return 10576;
        }

        public override int GetServiceNodeType()
        {
            return 8; 
        }
    }
}
