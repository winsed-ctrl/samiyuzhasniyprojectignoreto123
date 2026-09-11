namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicEditBattlePassCommand1 : Command
    {
        public bool unk1;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            unk1 = stream.ReadBoolean();
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 567;
        }
    }
}
