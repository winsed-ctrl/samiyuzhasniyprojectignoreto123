using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Logic.Stream.Entry;
using GromCore.Laser.Logic.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace GromCore.Laser.Logic.Message.Club
{
    public class BattleLogMessage : GameMessage
    {

        public HomeMode homeMode;

        public BattleLogMessage() : base()
        {
            ;
        }
        public override void Encode()
        {
            homeMode.Home.BattleLogs.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 23458;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}