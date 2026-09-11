using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home.Structures;

namespace GromCore.Laser.Logic.Message.Home
{
    public class CreatePlayerMapResponseMessage : GameMessage
    {
        public PlayerMap map;
        public override void Encode()
        {
            Stream.WriteVInt(0);
            if (Stream.WriteBoolean(map != null))
            {
                map.Encode(Stream);
            }
        }

        public override int GetMessageType()
        {
            return 22100;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
