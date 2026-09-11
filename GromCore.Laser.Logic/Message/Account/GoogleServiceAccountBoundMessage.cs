namespace GromCore.Laser.Logic.Message.Account
{
    public class GoogleServiceAccountBoundMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteInt(1);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
        }

        public override void Decode()
        {
            //Ping = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 25165;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
