namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicSelectCharacterCommand : Command
    {
        public int CharacterInstanceId;
        public int Index;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            CharacterInstanceId = stream.ReadVInt();
            Index=stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (GeneralStaticLogic.LockedBrawlers.Contains(CharacterInstanceId) && !homeMode.Avatar.IsDebugAccount)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification("Извини, данный бравлер недоступен. Попробуй в следующий раз!");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                homeMode.GameListener.SendMessage(availableServerCommandMessage);
                return 0;
            }
            
            int globalId = GlobalId.CreateGlobalId(16, CharacterInstanceId);
            var d = DataTables.Get(16).GetDataByGlobalId<CharacterData>(globalId);
            if (d != null && !d.Disabled && homeMode.Avatar.HasHero(globalId))
            {
                homeMode.Home.CharacterIds[Index] = globalId;
                homeMode.CharacterChanged.Invoke(globalId);
                Hero hero = homeMode.Avatar.GetHero(globalId);
                return 0;
            }


            return -1;
        }

        public override int GetCommandType()
        {
            return 525;
        }
    }
}
