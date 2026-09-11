namespace GromCore.Laser.Logic.Command.Home
{
    using System.Reflection.Metadata;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSetSupportCreatorCommand : Command
    {
        public string Code;

        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(1);
            stream.WriteString(Code);
            stream.WriteVInt(1);
            base.Encode(stream);

        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Home.CreatorCode = Code;
            return 0;
        }

        public override int GetCommandType()
        {
            return 215;
        }
    }
}
