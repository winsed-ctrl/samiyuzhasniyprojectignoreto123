using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanStartedMessage : GameMessage
    {
        public int Time;
        public int QueueIndexer;
        public override void Encode()
        {
            Stream.WriteVInt(15); // time
            Stream.WriteVInt(QueueIndexer); // queue index
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 22151;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
