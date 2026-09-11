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
    public class HomeBattleReplayViewedMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteVInt(1); // state?
            Stream.WriteLong(0, 0); // replay id
        }

        public override int GetMessageType()
        {
            return 24117;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}