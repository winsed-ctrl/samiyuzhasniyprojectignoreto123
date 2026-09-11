namespace GromCore.Laser.Logic.Listener
{
    using GromCore.Laser.Logic.Message;

    public abstract class LogicGameListener
    {
        public int HandledInputs;

        public abstract void SendMessage(GameMessage message);
        public abstract void SendTCPMessage(GameMessage message);
        public abstract void SendCommand(Command.Command command);
    }
}
