namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSetDoNotDisturb : Command
    {
        public int DoNotDistrub;

        public override void Encode(ByteStream stream)
        {

            stream.WriteVInt(DoNotDistrub);
            stream.WriteVInt(0);
            base.Encode(stream);

        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Avatar.DoNotDisturb = DoNotDistrub;
            return 0;
        }

        public override int GetCommandType()
        {
            return 213;
        }
    }
}
