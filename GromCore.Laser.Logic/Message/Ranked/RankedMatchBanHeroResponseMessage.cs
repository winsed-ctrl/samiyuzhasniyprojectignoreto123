using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Message.Club;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchBanHeroResponseMessage : GameMessage
    {
        public int BannedCharacter;
        public override void Encode()
        {
            /*
             *   ByteStreamHelper::encodeLogicLong(a1 + 8, a1 + 144);
  WriteVint(a1 + 8, *(_DWORD *)(a1 + 152));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 160));
  WriteBoolean(a1 + 8, *(unsigned __int8 *)(a1 + 168));
  WriteVint(a1 + 8, *(_DWORD *)(a1 + 172));
            */
            Stream.WriteLogicLong(0, 1);
            Stream.WriteVInt(0); 
            // reason
            // 0 - skip
            // 1 - return
            Stream.WriteDataReference(BannedCharacter);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(1); // state
        }

        public override int GetMessageType()
        {
            return 22152;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
