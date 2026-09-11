namespace GromCore.Laser.Logic.Message.Home
{
    public class ServerErrorMessage: GameMessage
    {
        public int id = 43;
        public override int GetMessageType()
        {
            return 24115;
        }
        public override void Encode()
        {
            Stream.WriteInt(id);//49 50
        }
        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
