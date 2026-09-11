namespace GromCore.Laser.Logic.Message.Club
{
    public class SetTeamChatMutedMessage : GameMessage
    {
        public bool state;

        public override void Decode()
        {
            state = Stream.ReadBoolean();
        }

        public override int GetMessageType()
        {
            return 14778;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
