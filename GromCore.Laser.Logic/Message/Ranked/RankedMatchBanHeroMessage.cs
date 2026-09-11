using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Message.Club;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanHeroMessage : GameMessage
    {
        public int BrawlerId;
        public int PickType;
        public override void Decode()
        {
            Stream.ReadVInt();
            BrawlerId = Stream.ReadVInt();
            PickType = Stream.ReadVInt();
        }

        public override int GetMessageType()
        {
            return 12152;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
