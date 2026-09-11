using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Ranked
{
    
    public class RankedMatchBanEndedMessage : GameMessage
    {
        public Dictionary<long, int> BannedBrawlers;
        public override void Encode()
        {
            Stream.WriteVInt(BannedBrawlers.Count); // count 
            foreach(var kvp  in BannedBrawlers)
            {
                Stream.WriteLong(kvp.Key);
                Stream.WriteDataReference(kvp.Value);
            }
        }

        public override int GetMessageType()
        {
            return 22153;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
