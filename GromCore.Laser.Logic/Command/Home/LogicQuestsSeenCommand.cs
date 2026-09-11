namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Quest;

    public class LogicQuestsSeenCommand : Command
    {
        public override int Execute(HomeMode homeMode)
        {
            foreach (Quest quest in homeMode.Home.Quests.QuestList.ToArray())
            {
                quest.Seen = true;
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 533;
        }
    }
}
