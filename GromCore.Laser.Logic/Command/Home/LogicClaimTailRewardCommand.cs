namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicClaimTailRewardCommand : Command
    {
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);

            stream.ReadVInt();
            stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (!LogicServerListener.Instance.IsDev())
            {
                if (homeMode.Home.BrawlPassTokens < 1600) return -1;
                homeMode.Home.BrawlPassTokens -= 1600;
            }
            homeMode.Home.DropsCount += 1;
            LogicGiveDeliveryItemsCommand command = new();
            DeliveryUnit unit = new DeliveryUnit(100);
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);

            homeMode.GameListener.SendCommand(command);

            homeMode.Home.StarrDrop.GenerateDrop(homeMode);
            LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new();
            logicRefreshRandomRewardsCommand.Execute(homeMode);
            homeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
            return 0;
        }

        public override int GetCommandType()
        {
            return 535;
        }
    }
}
