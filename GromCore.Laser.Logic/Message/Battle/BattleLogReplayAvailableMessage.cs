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
    public class BattleLogReplayAvailableMessage : GameMessage
    {
        public override void Encode()
        {

        }

        public override int GetMessageType()
        {
            return 23459;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}