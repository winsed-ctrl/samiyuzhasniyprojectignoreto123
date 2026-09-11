namespace GromCore.Laser.Server.Logic
{
    using GromCore.Laser.Logic.Command;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Server.Networking;

    public class HomeGameListener : LogicGameListener
    {
        private Connection Connection;

        public HomeGameListener(Connection connection)
        {
            Connection = connection;
        }

        public override void SendMessage(GameMessage message) => Connection.Send(message);

        public override void SendTCPMessage(GameMessage message) => Connection.Send(message);
        public override void SendCommand(Command command)
        {
            AvailableServerCommandMessage message = new()
            {
                Command = command
            };
            SendMessage(message);
        }
    }
}
