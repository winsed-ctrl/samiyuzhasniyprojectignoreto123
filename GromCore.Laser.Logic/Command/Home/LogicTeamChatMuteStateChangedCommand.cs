namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Titan.DataStream;

    public class LogicTeamChatMuteStateChangedComamnd : Command
    {
        public bool ChatMutestate;

        public override void Encode(ByteStream stream)
        {
            stream.WriteBoolean(ChatMutestate);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            homeMode.Avatar.ChatMutestate = ChatMutestate ? 1 : 0;
            return 0;
        }

        public override int GetCommandType()
        {
            return 221;
        }
    }
}
