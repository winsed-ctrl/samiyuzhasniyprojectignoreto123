using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home.Structures;

namespace GromCore.Laser.Logic.Message.Home
{
    public class ChangePlayerMapNameResponseMessage : GameMessage
    {
        public int Res;
        public string Name;
        public long Id;
        public override void Encode()
        {
            Stream.WriteVInt(Res);
            ByteStreamHelper.EncodeLogicLong(Stream, Id);
            Stream.WriteString(Name);
        }

        public override int GetMessageType()
        {
            return 22106;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
