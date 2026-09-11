namespace GromCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Util;
    using GromCore.Laser.Titan.Debug;

    public class DeletePlayerMapMessage : GameMessage
    {
        public int MapId;
        public override void Decode()
        {
            MapId=(int) ByteStreamHelper.DecodeLogicLong(Stream);
        }

        public override int GetMessageType()
        {
            return 12101;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
