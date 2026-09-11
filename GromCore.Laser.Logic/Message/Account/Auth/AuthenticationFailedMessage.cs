namespace GromCore.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationFailedMessage : GameMessage
    {
        public int ErrorCode;
        public string FingerprintSha;
        public string Message;
        public string UpdateUrl;
        public DateTime dateTime;
        public string ContentUrl;
        public override void Encode()
        {
            Stream.WriteInt(ErrorCode);
            Stream.WriteString(FingerprintSha);
            Stream.WriteString(null); // Redirect
            Stream.WriteString(ContentUrl); // content url
            Stream.WriteString(UpdateUrl); // update url
            Stream.WriteString(Message);
            
            Stream.WriteInt((int)(dateTime-DateTime.Now).TotalSeconds); // Till Maintenance ends
            Stream.WriteBoolean(false); // Show contact support for ban
            Stream.WriteCompressedString(FingerprintSha);
            Stream.WriteInt(0); // Content URI List
            Stream.WriteInt(0); // KunlunAppStore
            Stream.WriteInt(3); // Maintenance Type
            Stream.WriteString(string.Empty); // Helpshit FaqID
            Stream.WriteInt(0); // Tier
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteStringReference(string.Empty);
            Stream.WriteVInt(0);
            Stream.WriteStringReference(string.Empty);
            Stream.WriteBoolean(false);
        }

        public override int GetMessageType()
        {
            return 20103;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
