namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicGatchaCommand : Command
    {
        public int BoxIndex;

        public LogicGatchaCommand() : base()
        {
            ;
        }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            BoxIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 500;
        }
    }
}
