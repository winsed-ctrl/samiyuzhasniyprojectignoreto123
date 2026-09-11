namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Titan.DataStream;

    public class LogicBrawlPassSeasonChangedCommand : Command
    {
        public BaseNotification Notification;

        public override void Encode(ByteStream stream)
        {
            if (stream.WriteBoolean(Notification != null))
            {
                stream.WriteVInt(Notification.GetNotificationType());
                Notification.Encode(stream);
            }

            stream.WriteVInt(0);
            base.Encode(stream);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
        }

        public override int GetCommandType()
        {
            return 206;
        }
    }
}
