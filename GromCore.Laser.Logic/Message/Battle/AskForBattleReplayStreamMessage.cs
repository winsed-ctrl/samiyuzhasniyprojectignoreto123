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
    public class AskForBattleReplayStreamMessage : GameMessage
    {
        public override void Decode()
        {

        }

        public override int GetMessageType()
        {
            return 14406;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}