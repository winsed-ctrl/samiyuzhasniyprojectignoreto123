namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Logic.Util;

    public class LogicPurchaseBrawlPassProgressCommand : Command
    {
        public int Unknown { get; set; }

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Unknown = stream.ReadVInt();
        }

        public override int Execute(HomeMode HomeMode)
        {
            if (HomeMode.Avatar.UseDiamonds(30))
            {
                for (int x = 3939; x < 3999 + 2; x++)
                {
                    MilestoneData milestoneData = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId((int)DataType.Milestone, x));
                    if (milestoneData.ProgressStart <= HomeMode.Home.BrawlPassTokens && (milestoneData.ProgressStart + milestoneData.Progress) > HomeMode.Home.BrawlPassTokens)
                    {
                        HomeMode.Home.BrawlPassTokens = milestoneData.ProgressStart + milestoneData.Progress;
                        return 0;
                    }
                }
            }
            return -1;
        }

        public override int GetCommandType()
        {
            return 536;
        }
    }
}
