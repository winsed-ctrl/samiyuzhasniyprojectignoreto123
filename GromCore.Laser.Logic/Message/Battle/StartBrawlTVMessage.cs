namespace GromCore.Laser.Logic.Message.Battle
{
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.Util;

    public class StartBrawlTVMessage : GameMessage
    {
        public override void Encode()
        {
            Stream.WriteString(null);
        }

        public override int GetMessageType()
        {
            return 14700;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
