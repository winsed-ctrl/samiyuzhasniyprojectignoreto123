namespace GromCore.Laser.Logic.Message.Home
{
    using Newtonsoft.Json.Linq;
    using System.Text;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Util;
    using GromCore.Laser.Titan.Debug;

    public class SubminPlayerMapResponseMessage : GameMessage
    {
        public int MapId;
        public int ErrorCode;
        public override void Encode()
        {
            Stream.WriteVInt(ErrorCode);
            ByteStreamHelper.EncodeLogicLong(Stream, MapId);
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 22104;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
