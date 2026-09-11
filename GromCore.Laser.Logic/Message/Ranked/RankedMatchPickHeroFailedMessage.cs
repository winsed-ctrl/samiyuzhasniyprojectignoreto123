using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchPickHeroFailedMessage : GameMessage
    {

        public override void Encode()
        {
            /*
             *   WriteVint(a1 + 8, *(_DWORD *)(a1 + 144));
  ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 152));
  WriteBoolean(a1 + 8, *(unsigned __int8 *)(a1 + 160));
  return ByteStreamHelper::writeDataReference(a1 + 8, *(_QWORD *)(a1 + 168));
            */
            Stream.WriteVInt(1);
            ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(16, 0));
            Stream.WriteBoolean(false);
            ByteStreamHelper.WriteDataReference(Stream, GlobalId.CreateGlobalId(16, 0));
        }

        public override int GetMessageType()
        {
            return 22155;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
