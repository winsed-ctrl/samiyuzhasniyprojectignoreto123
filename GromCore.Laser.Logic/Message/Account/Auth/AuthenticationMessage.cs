using Microsoft.VisualBasic;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Titan.DataStream;

namespace GromCore.Laser.Logic.Message.Account.Auth
{
    public class AuthenticationMessage : GameMessage
    {
        public AuthenticationMessage() : base()
        {
            AccountId = 0;
        }

        public long AccountId;
        public string PassToken;
        public int ClientMajor;
        public int ClientMinor;
        public int ClientBuild;
        public string ResourceSha;
        public string? Device;
        public int DeviceIMEI;
        public bool IsAdvertisingEnabled;
        public bool IsAndroid;
        public string? OSVersion;
        public string? PreferredDeviceLanguage;
        public int PreferredLanguage;
        public string ClientVersion;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            PassToken = Stream.ReadString();
            ClientMajor = Stream.ReadInt();
            ClientMinor = Stream.ReadInt();
            ClientBuild = Stream.ReadInt();
            ResourceSha = Stream.ReadString();
            
            Device = Stream.ReadString(1024);
            PreferredLanguage = ByteStreamHelper.ReadDataReference(Stream);
            PreferredDeviceLanguage = Stream.ReadString(1024);
            OSVersion = Stream.ReadString(1024);
            IsAndroid = Stream.ReadBoolean();
            Stream.ReadStringReference(1024);
            Stream.ReadStringReference(1024);
            IsAdvertisingEnabled = Stream.ReadBoolean();
            Stream.ReadString(1024);
            DeviceIMEI = Stream.ReadInt();
            Stream.ReadVInt();
            ClientVersion = Stream.ReadStringReference(1024); // Client Version( why? )

        }

        public override int GetMessageType()
        {
            return 10101;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
