namespace GromCore.Laser.Logic.Message.Battle
{
    public class PlayAgainMessage : GameMessage
    {
        public long AccountId;

        public override int GetMessageType()
        {
            return 14177;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
