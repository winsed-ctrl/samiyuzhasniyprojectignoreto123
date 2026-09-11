namespace GromCore.Laser.Logic.Notification
{
    using GromCore.Laser.Titan.DataStream;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class FreeTextNotification : BaseNotification
    {
        public override void Encode(ByteStream stream)
        {
            base.Encode(stream);
            stream.WriteVInt(1);
        }

        public override int GetNotificationType()
        {
            return 81;
        }
    }
}
