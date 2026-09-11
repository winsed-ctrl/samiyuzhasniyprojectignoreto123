namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Titan.DataStream;

    public class LogicAddNotificationCommand : Command
    {
        public BaseNotification Notification;
        public Notification Notif;
        public override void Encode(ByteStream stream)
        {
            if(Notification != null)
            {
                if (stream.WriteBoolean(Notification != null))
                {
                    stream.WriteVInt(Notification.GetNotificationType());
                    Notification.Encode(stream);
                }

                stream.WriteVInt(0);
                base.Encode(stream);
                return;
            }
            else
            {
                if (stream.WriteBoolean(Notif != null))
                {
                    Notif.Encode(stream);
                }

                stream.WriteVInt(0);
                base.Encode(stream);
            }
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
