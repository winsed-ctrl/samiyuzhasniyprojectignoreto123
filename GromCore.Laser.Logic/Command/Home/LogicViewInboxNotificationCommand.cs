namespace GromCore.Laser.Logic.Command.Home
{
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicViewInboxNotificationCommand : Command
    {
        public int NotificationIndex;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            NotificationIndex = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            Notification? alliance = homeMode.Avatar.AllianceId > 0? LogicServerListener.Instance.GetAllianceMail(homeMode.Avatar.AllianceId)?.ElementAtOrDefault(NotificationIndex) : null;
            if(alliance != null)
            {
                alliance.IsViewed = true;
                return 0;
            }
            if (NotificationIndex >= 5000) return 0;
            if (NotificationIndex > homeMode.Home.NotificationFactory.GetIndex()) return -1;
            Notification? notif;
            try
            {
                 notif = homeMode.Home.NotificationFactory.NotificationList[NotificationIndex];
            }
            catch
            {
                return -1;
            }
            if (notif == null || notif.IsViewed) return -1;
            LogicGiveDeliveryItemsCommand delivery = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            notif.IsViewed = true;
            switch (notif.Id)
            {
                case 63:
                case 70:
                    int truetype = GatchaDrop.GetGatchaDropByShopItem(notif.gemOffer.Type);
                    GatchaDrop drop = new GatchaDrop(truetype);
                    int data = notif.gemOffer.BrawlerData;
                    if (notif.gemOffer.BrawlerData1 > 0) data = GlobalId.CreateGlobalId(notif.gemOffer.BrawlerData1, notif.gemOffer.BrawlerData2);
                    if (truetype != 4) drop.DataGlobalId = data;
                    else drop.CardGlobalId = data;
                    int fixedData = notif.gemOffer.ExtraData;
                    if (GlobalId.GetClassId(fixedData) != 29 && truetype == 9)
                        fixedData = GlobalId.CreateGlobalId(29, fixedData);
                    if(GlobalId.GetClassId(fixedData) != 52 && truetype == 11)
                        fixedData = GlobalId.CreateGlobalId(52, fixedData);
                    if (truetype != 1) drop.SkinGlobalId = fixedData;
                    drop.Count = notif.gemOffer.Count;
                    unit.AddDrop(drop);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(homeMode);
                    homeMode.GameListener.SendCommand(delivery);
                    break;
                case 89: // GemRewardNotification
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = notif.DonationCount;
                    unit.AddDrop(reward);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(homeMode);
                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = delivery;
                    homeMode.GameListener.SendMessage(message);
                    break;
                case 94: // SkinRewardNotification
                    GatchaDrop reward1 = new GatchaDrop(9);
                    reward1.Count = 1;
                    reward1.SkinGlobalId = GlobalId.CreateGlobalId(29, notif.SkinID);
                    unit.AddDrop(reward1);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(homeMode);
                    AvailableServerCommandMessage message1 = new AvailableServerCommandMessage();
                    message1.Command = delivery;
                    homeMode.GameListener.SendMessage(message1);
                    break;
                case 72: // VanityRewardNotifif
                    GatchaDrop reward12 = new GatchaDrop(11);
                    reward12.Count = 1;
                    reward12.DataGlobalId = notif.VanityRewardGlobalId;
                    unit.AddDrop(reward12);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(homeMode);
                    AvailableServerCommandMessage message12 = new AvailableServerCommandMessage();
                    message12.Command = delivery;
                    homeMode.GameListener.SendMessage(message12);
                    break;
                case 80: // VanityRewardNotifif
                    GatchaDrop reward123 = new GatchaDrop(12);
                    reward123.Count = 1;
                    reward123.DataGlobalId = notif.VanityRewardGlobalId;
                    unit.AddDrop(reward123);
                    delivery.DeliveryUnits.Add(unit);
                    delivery.Execute(homeMode);
                    AvailableServerCommandMessage message123 = new AvailableServerCommandMessage();
                    message123.Command = delivery;
                    homeMode.GameListener.SendMessage(message123);
                    break;
                case 79:
                    foreach (int a in homeMode.Home.NotificationFactory.NotificationList[NotificationIndex].StarpointsAwarded)
                    {
                        homeMode.Avatar.AddBlings(a);
                    }
                    break;
            }
            
            return 0;
        }

        public override int GetCommandType()
        {
            return 528;
        }
    }
}
