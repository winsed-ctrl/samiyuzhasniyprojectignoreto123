namespace GromCore.Laser.Logic.Message.Battle
{
    public class PlayAgainStatusMessage : GameMessage
    {
        public long AccountId;

        public override void Encode()
        {
            Stream.WriteInt(2);

            Stream.WriteVInt(1);
            Stream.WriteInt(1);
            Stream.WriteInt(1);

            Stream.WriteVInt(0);
            //Stream.WriteInt(1);
            //Stream.WriteInt(1);

            Stream.WriteInt(9);
            Stream.WriteInt(9);
        }
        public override int GetMessageType()
        {
            return 24777;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
