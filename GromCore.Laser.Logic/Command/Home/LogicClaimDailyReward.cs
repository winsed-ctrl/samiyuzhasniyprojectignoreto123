namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Command.Avatar;
    using System.Reflection.Metadata;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using DailyCalendarData;

    public class LogicSetDailyRewardType : Command
    {
        int Type;
        int Day;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            Day = stream.ReadVInt();
            Type = stream.ReadVInt(); // road
            //Console.WriteLine(stream.ReadVInt());
        }

        public override int Execute(HomeMode homeMode)
        {
            DailyCalendarGemOffer gem = null;
            if(Type == 0 && Day == 1) gem = homeMode.Home.DailyCalendarData.GetRewardData(Day);
            else gem = homeMode.Home.DailyCalendarData.GetRewardData(Day);
            
            LogicGiveDeliveryItemsCommand command = new();
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop drop = null;
            if (Type == 0)
            {
                drop = new GatchaDrop(GatchaDrop.GetGatchaDropByShopItem((int)gem.ShopItemId1));
                drop.DataGlobalId = GlobalId.CreateGlobalId(gem.ShopItemDataReference1[0], gem.ShopItemDataReference1[1]);
                if ((int)gem.ShopItemId1 != 1) drop.SkinGlobalId = gem.ShopItemExtraData1;
                drop.Count = gem.ShopItemCount1;
            }
            else
            {
                drop = new GatchaDrop(GatchaDrop.GetGatchaDropByShopItem((int)gem.ShopItemId2));
                drop.DataGlobalId = GlobalId.CreateGlobalId(gem.ShopItemDataReference2[0], gem.ShopItemDataReference2[1]);
                if ((int)gem.ShopItemId1 != 1) drop.SkinGlobalId = gem.ShopItemExtraData2;
                drop.Count = gem.ShopItemCount2;
            }

                unit.AddDrop(drop);
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);
            AvailableServerCommandMessage message = new()
            {
                Command = command
            };
            homeMode.GameListener.SendMessage(message);
            homeMode.Home.DailyCalendarData.DailyRewardsType = Type+1;
            homeMode.Home.DailyCalendarData.DailyRewardsClaimedDay = +1;
            homeMode.Home.DailyCalendarData.DailyRewardsDay = +1;
            return 0;
        }

        public override int GetCommandType()
        {
            return 550;
        }
    }
}
