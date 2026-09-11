using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Message.Account
{
    public class AvatarNameChangeFailedMessage : GameMessage
    {
        public int Reason;

        public override void Encode()
        {
            Stream.WriteInt(Reason);
        }

        public override int GetMessageType()
        {
            return 20205;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
