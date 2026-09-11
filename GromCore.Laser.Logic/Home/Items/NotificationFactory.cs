namespace GromCore.Laser.Logic.Home.Items
{
    using System.IO;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;

    public class NotificationFactory
    {
        [JsonProperty("notification_list")]
        public List<Notification> NotificationList;

        public NotificationFactory()
        {
            NotificationList = new List<Notification>();
        }

        public void Add(Notification notification)
        {
            notification.Index = GetIndex();
            NotificationList.Add(notification);
        }

        public int GetIndex()
        {
            return NotificationList.Count;
        }

        public void Encode(ByteStream stream, List<Notification> AllianceNotifications = null)
        {
            if(AllianceNotifications == null)
            {
                stream.WriteVInt(NotificationList.Count);
                foreach (Notification notification in NotificationList)
                {
                    notification.Encode(stream);
                }
            }
            else
            {
                stream.WriteVInt(NotificationList.Count + AllianceNotifications.Count);
                foreach (Notification notification in NotificationList)
                {
                    notification.Encode(stream);
                }
                foreach (Notification notification in AllianceNotifications)
                {
                    notification.Encode(stream);
                }
            }
        }
    }
}
