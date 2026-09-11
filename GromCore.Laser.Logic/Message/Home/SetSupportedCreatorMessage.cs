using GromCore.Laser.Logic.Helper;

namespace GromCore.Laser.Logic.Message.Home
{
    public class SetSupportedCreatorMessage : GameMessage
    {
        public string Code;
        public override void Decode()
        {
            Code = Stream.ReadString();
        }

        public override int GetMessageType()
        {
            return 18686;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
