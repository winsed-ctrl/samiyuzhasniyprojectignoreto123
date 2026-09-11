using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Titan.DataStream;

namespace GromCore.Laser.Logic.Message.Home
{
    public class ChangePlayerMapNameMessage : GameMessage
    {
        public string Name;
        public long Id;
        public override void Decode()
        {
            Id = ByteStreamHelper.DecodeLogicLong(Stream);
            Name = Stream.ReadString();
        }

        public override int GetMessageType()
        {
            return 12106;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
