namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;

    public class LogicPurchaseChallengeLivesCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            return -1;
            if (!homeMode.Avatar.UseDiamonds(19)) return -1;

            return 0;
        }

        public override int GetCommandType()
        {
            return 540;
        }
    }
}
