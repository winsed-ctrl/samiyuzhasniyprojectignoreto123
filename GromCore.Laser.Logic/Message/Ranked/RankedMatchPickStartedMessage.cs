using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Ranked;
using System.IO;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchPickStartedMessage : GameMessage
    {
        public RankedMatchPlayer Player;
        public int Next;

        public override void Encode()
        {
            
            Stream.WriteVInt(15);
            Stream.WriteBoolean(true);
            Player.Encode(Stream);

            Stream.WriteVInt(0);
            Stream.WriteVInt(Next); // next who ban

            ByteStreamHelper.EncodeLogicLong(Stream, Player.AccountId);
        }

        public override int GetMessageType()
        {
            return 22154;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
