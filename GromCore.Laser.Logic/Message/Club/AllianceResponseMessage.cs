namespace GromCore.Laser.Logic.Message.Club
{
    public class AllianceResponseMessage : GameMessage
    {
        public int ResponseType;
        public DateTime choto;
        public void SetResponse(int i) => ResponseType = i;
        public override void Encode()
        {
            Stream.WriteVInt(ResponseType);
            if (ResponseType == 112 && choto != null) Stream.WriteVInt((int)(choto - DateTime.Now).TotalSeconds);
        }

        public override int GetMessageType()
        {
            return 24333;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
