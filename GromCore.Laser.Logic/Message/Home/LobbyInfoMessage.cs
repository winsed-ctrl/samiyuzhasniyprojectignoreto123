using GromCore.Laser.Logic.Listener;

namespace GromCore.Laser.Logic.Message.Home
{
    public class LobbyInfoMessage : GameMessage
    {
        public LobbyInfoMessage()
        {
        }

        public override void Encode()
        {
            Stream.WriteVInt(4480);

            int onlinePlayers = LogicServerListener.Instance?.GetOnlinePlayersCount() ?? 0;
            Stream.WriteString(
                "<c8b5cff>ULTRA</c><cff4fd8>BRAWL</c>" +
                $"\n<c53ff8a>● Онлайн: {onlinePlayers}</c>" +
                "\n<c52a8ff>Telegram: @UltraBrawl</c>");
            
            Stream.WriteVInt(0);
        }

        public override int GetMessageType()
        {
            return 23457;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
