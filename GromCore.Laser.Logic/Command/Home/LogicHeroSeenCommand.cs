namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Titan.DataStream;

    public class LogicHeroSeenCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            ByteStreamHelper.ReadDataReference(stream);
            stream.ReadInt();
        }
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }
        public override int GetCommandType()
        {
            return 522;
        }
    }
}
