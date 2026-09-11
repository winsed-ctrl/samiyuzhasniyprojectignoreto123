namespace GromCore.Laser.Logic.Message.Account
{
    public class BindGoogleServiceAccountMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteBoolean(true);
            Stream.WriteString("AASDASDASDISAFNUASNFASU");
            Stream.WriteString("AASDASDASDISAFNUASNFASU");
        }

        public override void Decode()
        {
            //Ping = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 14262;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
