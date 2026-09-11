using System.IO;
using GromCore.Laser.Logic.Notification;
using GromCore.Laser.Titan.DataStream;

namespace GromCore.Laser.Logic.Message.Home
{
    public class NotificationMessage : GameMessage
    {
        public BaseNotification Notification;

        public override int GetMessageType()
        {
            return 20801;
        }
        public override void Encode()
        {
            Stream.WriteVInt(Notification.GetNotificationType());
            Notification.Encode(Stream);
        }
        public override int GetServiceNodeType()
        {
            return 1;
        }
    }
}
