using System.Diagnostics;
using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Home
{
    public class SetSupportedCreatorResponceMessage : GameMessage
    {
        public string creator;
        public override void Encode()
        {
            Stream.WriteVInt(0);
            Stream.WriteString(creator);
        }

        public override int GetMessageType()
        {
            return 28686;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
