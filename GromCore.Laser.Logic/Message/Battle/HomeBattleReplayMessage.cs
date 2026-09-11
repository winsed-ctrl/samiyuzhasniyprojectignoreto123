using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GromCore.Laser.Logic.Message.Club
{
    public class HomeBattleReplayMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteInt(0); //idk
            Stream.WriteLogicLong(0,1); // idk too
        }

        public override int GetMessageType()
        {
            return 14114;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}