namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicOpenRandomCommand : Command
    {
        public int Unk;
        public int RewardType;
        public int RewardAmount;
        public int Unk2;
        public int Unk3;

        public override int Execute(HomeMode homeMode)
        {
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop drop = new GatchaDrop(homeMode.Home.StarrDrop.Data.Type);

            if (homeMode.Home.StarrDrop.Data.Type != 4) drop.DataGlobalId = homeMode.Home.StarrDrop.Data.DataGlobalID;
            else drop.CardGlobalId = homeMode.Home.StarrDrop.Data.DataGlobalID;
            if (homeMode.Home.StarrDrop.Data.Type != 1) drop.SkinGlobalId = homeMode.Home.StarrDrop.Data.SkinGlobalID;
            drop.Count = homeMode.Home.StarrDrop.Data.Ammount;
            unit.AddDrop(drop);
            command.StarrDropExecute = true;
            command.DeliveryUnits.Add(unit);

           

            LogicRefreshRandomRewardsCommand LogicRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
            LogicRandomRewardsCommand.HomeModeCopy = homeMode;
            LogicRandomRewardsCommand.VisualRarity = (int)homeMode.Home.StarrDrop.Rarity;
            LogicRandomRewardsCommand.Execute(homeMode);
            homeMode.GameListener.SendCommand(LogicRandomRewardsCommand);


            command.Execute(homeMode);
            homeMode.GameListener.SendCommand(command);
            if (homeMode.Home.StarrDrop.Data.Type == 9) NewCommand(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(homeMode.Home.StarrDrop.Data.SkinGlobalID));
            void NewCommand(SkinData skinData)
            {
                if (skinData == null) return;
                DeliveryUnit unit = new DeliveryUnit(100);
                GatchaDrop reward = new GatchaDrop(9);
                foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
                {

                    if (emoteData.Skin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }

                }
                foreach (PlayerThumbnailData playerThumbnailData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
                {
                    if (playerThumbnailData.CatalogPreRequirementSkin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(playerThumbnailData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }

                }
                foreach (SprayData sprayData in DataTables.Get(DataType.Spray).GetDatas())
                {
                    if (sprayData.Skin == skinData.Name)
                    {
                        GatchaDrop reward1 = new GatchaDrop(11);
                        reward1.DataGlobalId = DataTables.Get(DataType.Spray).GetData<SprayData>(sprayData.Name).GetGlobalId();
                        reward1.Count = 1;
                        unit.AddDrop(reward1);
                    }
                }
                command.DeliveryUnits.Add(unit);
                if (command.DeliveryUnits.Count <= 0) return;
                if (unit.Drops.Count != 0)
                {
                    command.Execute(homeMode);

                    AvailableServerCommandMessage message = new AvailableServerCommandMessage();
                    message.Command = command;
                    homeMode.GameListener.SendMessage(message);
                }
            }
            if (homeMode.Home.DropsCount > 0) homeMode.Home.DropsCount--;
            if (homeMode.Home.DropsCount <= 0)
            {
                LogicRefreshRandomRewardsCommand DisableRender = new();
                DisableRender.HomeModeCopy = homeMode;
                DisableRender.StopRender = true;
                DisableRender.Execute(homeMode);

                AvailableServerCommandMessage disableRender = new();
                disableRender.Command = DisableRender;
                homeMode.GameListener.SendMessage(disableRender);
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 571;
        }
    }
}
