namespace GromCore.Laser.Logic.Message.Battle
{
    using System.Buffers;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.Util;

    public class SpectateFailedMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteInt(1);

        }

        public override int GetMessageType()
        {
            return 24105;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
