namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Quest;

    public class LogicPurchaseTokenDoublersCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            if (homeMode.Avatar.UseDiamonds(40))
            {
                homeMode.Home.TokenDoublers += 1000;
                return 0;
            }
            return -1;
        }

        public override int GetCommandType()
        {
            return 509;
        }
    }
}
