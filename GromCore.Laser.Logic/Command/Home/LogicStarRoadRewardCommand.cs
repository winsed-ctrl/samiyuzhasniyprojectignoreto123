namespace GromCore.Laser.Logic.Command.Home
{
    using Masuda.Net.Models;
    using GromCore.Laser.Logic.Command.Avatar;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicStarRoadRewardCommand : Command
    {
        List<int> rare = new List<int> {};
        List<int> super_rare = new List<int> {};
        List<int> epic = new List<int> {};
        List<int> mythic = new List<int> {};
        List<int> legendary = new List<int> {};
        List<int> ultra_legendary = new List<int> {};


        private int BrawlerID;

        public override void Decode(ByteStream stream)
        {
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            stream.ReadVInt();
            BrawlerID = stream.ReadVInt();
            //BrawlerID = stream.ReadVInt();

        }

        public override int Execute(HomeMode homeMode)
        {
            rare = homeMode.Home.GetBrawlersByRarity("rare");
            super_rare = homeMode.Home.GetBrawlersByRarity("super_rare");
            epic = homeMode.Home.GetBrawlersByRarity("epic");
            mythic = homeMode.Home.GetBrawlersByRarity("mythic");
            legendary = homeMode.Home.GetBrawlersByRarity("legendary");
            ultra_legendary = homeMode.Home.GetBrawlersByRarity("ultra_legendary");

            if (rare.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 160;
            else if (super_rare.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 430;
            else if (epic.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 925;
            else if (mythic.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 1900;
            else if (legendary.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 3800;
            else if (ultra_legendary.Contains(BrawlerID))
                homeMode.Home.RecruitTokens = homeMode.Home.RecruitTokens -= 5500;
            else
                homeMode.Home.RecruitTokens = 0;

            if (homeMode.Home.RecruitTokens < 0)
            {
                homeMode.Home.RecruitTokens = 0;
            }

            if (!homeMode.Avatar.HasHero(GlobalId.CreateGlobalId(16, BrawlerID)))
            {
                homeMode.Avatar.UnlockHero(GlobalId.CreateGlobalId(16, BrawlerID));
            }

            LogicStarroadRefreshCommand command = new LogicStarroadRefreshCommand();
            command.Execute(homeMode);
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command;
            homeMode.GameListener.SendMessage(serverCommandMessage);

            return 0;

        }

        public override int GetCommandType()
        {
            return 562;
        }
    }
}
