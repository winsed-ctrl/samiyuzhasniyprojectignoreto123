namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicSelectStarPowerCommand : Command
    {
        public int CardInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            CardInstanceId=stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            // if (GeneralStaticLogic.LockedUnquies.Contains(CardInstanceId))
            // {
            //     LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
            //     logicAddNotificationCommand.Notification = new FloaterTextNotification("This gadget is unfinished.\nPlease try again later.");
            //     AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
            //     availableServerCommandMessage.Command = logicAddNotificationCommand;
            //     homeMode.GameListener.SendMessage(availableServerCommandMessage);
            //     return 0;
            // }

            CharacterData hero;
            CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(29, CardInstanceId));
            if (card == null) return -1;
            Hero playerHero = homeMode.Avatar.GetHeroForCard(card);
            if (card.MetaType == 4) playerHero.ChampieSelectedStarPowerId = card.GetInstanceId();
            else playerHero.ChampieSelectedGadgetId = card.GetInstanceId();
            if (!homeMode.Avatar.SPGS.Contains(card.GetGlobalId())) return 0;

            string m = card.Name.Replace("_2", "");
            m = m.Replace("_3", "");
            CardData card1 = DataTables.Get(DataType.Card).GetData<CardData>(m);
            CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_2");
            CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_3");
            hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
            Hero h = homeMode.Avatar.GetHero(hero.GetGlobalId());

            homeMode.Avatar.SelectedSPGS.Remove(card1.GetGlobalId());
            if(card2 != null)homeMode.Avatar.SelectedSPGS.Remove(card2.GetGlobalId());
            if (card3 != null)
            {
                homeMode.Avatar.SelectedSPGS.Remove(card3.GetGlobalId());
            }
            if (card.MetaType == 4) playerHero.SelectedStarPowerId = card.GetInstanceId();
            else playerHero.SelectedGadgetId = card.GetInstanceId();
            
            homeMode.Avatar.SelectedSPGS.Add(card.GetGlobalId());
            homeMode.CharacterChanged.Invoke(0);
            return 0;
        }

        public override int GetCommandType()
        {
            return 529;
        }
    }
}
