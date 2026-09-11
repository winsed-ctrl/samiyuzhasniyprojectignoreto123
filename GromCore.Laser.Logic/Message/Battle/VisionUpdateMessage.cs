namespace GromCore.Laser.Logic.Message.Battle
{
    using GromCore.Laser.Titan.DataStream;
    using System.Security.Permissions;

    public class VisionUpdateMessage : GameMessage
    {
        public int Tick;
        public int HandledInputs;
        public int Viewers;
        public BitStream VisionBitStream;
        public bool IsBrawlTV;
        public VisionUpdateMessage() : base()
        {
            ;
        }

        public override void Encode()
        {
            Stream.WriteVInt(Tick);
            Stream.WriteVInt(HandledInputs);
            Stream.WriteVInt(0); // unknown
            Stream.WriteVInt(Viewers);
            Stream.WriteBoolean(IsBrawlTV);

            Stream.WriteBytes(VisionBitStream.GetByteArray(), VisionBitStream.GetLength());
        }

        public override int GetMessageType()
        {
            return 24109;
        }

        public override int GetServiceNodeType()
        {
            return 4;
        }
    }
}
