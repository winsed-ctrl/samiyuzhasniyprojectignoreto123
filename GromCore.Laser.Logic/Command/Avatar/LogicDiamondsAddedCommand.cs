namespace GromCore.Laser.Logic.Command.Avatar
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicDiamondsAddedCommand : Command
    {
        public int Amount;

        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            //homeMode.Avatar.AddDiamonds(Amount);
            return 0;
        }

        public override int GetCommandType()
        {
            return 202;
        }
    }
}
