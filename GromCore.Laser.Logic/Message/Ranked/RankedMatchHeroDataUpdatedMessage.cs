using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Ranked;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchHeroDataUpdatedMessage : GameMessage
    {
        public RankedMatchPlayer Player;
        public override void Encode()
        {
            Stream.WriteBoolean(true);
            Player.Encode(Stream);
        }

        public override int GetMessageType()
        {
            return 22157;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
