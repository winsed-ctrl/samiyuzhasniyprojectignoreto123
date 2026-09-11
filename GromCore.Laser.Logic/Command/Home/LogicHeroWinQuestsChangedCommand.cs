namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Titan.DataStream;
    using System.Security.Cryptography.X509Certificates;

    public class LogicHeroWinQuestsChangedCommand : Command
    {
        public Quests Quests { get; set; }
        public int usedrerolls;
        public int seas;

        public override void Encode(ByteStream stream)
        {
            if (stream.WriteBoolean(Quests != null))
            {
                Quests.Encode(stream,seas);
                stream.WriteVInt(0);//next reroll update
                stream.WriteVInt(0);//used rerolls
                stream.WriteVInt(0);
            }

            stream.WriteVInt(1);
            stream.WriteVInt(1);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 220;
        }
    }
}
