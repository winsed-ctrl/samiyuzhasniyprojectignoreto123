using System.Diagnostics;
using System.Text;
using GromCore.Laser.Titan.Library;

namespace GromCore.Laser.Logic.Message.Security
{
    public class CryptoErrorMessage : GameMessage
    {
        public override void Decode()
        {
            // Console.WriteLine($"CRYPTO ERROR: {Stream.ReadVInt()}")
            ;
        }

        public override int GetMessageType()
        {
            return 10099;
        }

        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
