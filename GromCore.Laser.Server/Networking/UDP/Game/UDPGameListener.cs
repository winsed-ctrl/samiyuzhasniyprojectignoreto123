namespace GromCore.Laser.Server.Networking.UDP.Game
{
    using GromCore.Laser.Logic.Command;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message;

    public class UDPGameListener : LogicGameListener
    {
        private UDPSocket Socket;
        private Connection TCPConnection;

        public UDPGameListener(UDPSocket socket, Connection connection)
        {
            Socket = socket;
            TCPConnection = connection;
        }

        public override void SendMessage(GameMessage message)
        {
            Socket.SendMessage(message);
        }

        public override void SendTCPMessage(GameMessage message)
        {
            try
            {
                TCPConnection.Send(message);
            } catch (Exception ex) { }
        }
        public override void SendCommand(Command command)
        {
           // throw new NotImplementedException();
        }
    }
}
