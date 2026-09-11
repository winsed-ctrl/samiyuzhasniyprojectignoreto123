using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home.Structures;

namespace GromCore.Laser.Logic.Message.Home
{
    public class DebugCommandMessage : GameMessage
    {

        public override int GetMessageType()
        {
            return 11736;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
