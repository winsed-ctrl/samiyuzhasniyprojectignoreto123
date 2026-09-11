namespace GromCore.Laser.Logic.Message.Home
{
    using GromCore.Laser.Logic.Command;

    public class EndClientTurnMessage : GameMessage
    {
        public int Tick;
        public int Checksum;
        public int type;

        public List<Command> Commands;

        public EndClientTurnMessage() : base()
        {
            Commands = new List<Command>();
        }

        public override void Decode()
        {
            Stream.ReadBoolean();
            Tick = Stream.ReadVInt();
            Checksum = Stream.ReadVInt();

            int count = Stream.ReadVInt();
            for (int i = 0; i < count; i++)
            {
                type = Stream.ReadVInt();
                Command command = CommandManager.DecodeCommand(Stream, type);
                if (command == null) return;

                Commands.Add(command);
            }
        }

        public override int GetMessageType()
        {
            return 14102;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
