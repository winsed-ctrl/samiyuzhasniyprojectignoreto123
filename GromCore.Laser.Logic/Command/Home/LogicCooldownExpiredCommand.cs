namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;

    public class LogicCooldownExpiredCommand : Command
    {
        int index;
        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(2);
            stream.WriteVInt(0); // dataref
        }
        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 216;
        }
    }
}
