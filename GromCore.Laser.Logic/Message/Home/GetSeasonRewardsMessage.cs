namespace GromCore.Laser.Logic.Message.Home
{
    public class GetSeasonRewardsMessage : GameMessage
    {
        public int Type;
        public override void Decode()
        {
            Type = Stream.ReadVInt();
            Console.WriteLine(Type);
        }

        public override void Encode()
        {
            Stream.WriteVInt(Type);
        }
        public override int GetMessageType()
        {
            return 12938;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
