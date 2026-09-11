namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSetPlayerNameColorCommand : Command
    {
        public int NameColorInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            NameColorInstanceId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (NameColorInstanceId < 0) return -1;
            if (NameColorInstanceId > DataTables.Get(DataType.NameColor).Count) return -1;

            homeMode.Home.NameColorId = GlobalId.CreateGlobalId(43, NameColorInstanceId);
            
            if (homeMode.Avatar.Friends.Count > 0)
            {
                foreach (var friend in homeMode.Avatar.Friends)
                {
                    if (friend.Avatar.Friends.Find(x => x.AccountId == homeMode.Avatar.AccountId) == null) continue;
                    friend.Avatar.Friends.Find(x => x.AccountId == homeMode.Avatar.AccountId).DisplayData = new Logic.Avatar.Structures.PlayerDisplayData(homeMode.Home.ThumbnailId, homeMode.Home.NameColorId, homeMode.Avatar.Name, homeMode.Home.HasPremiumPass, homeMode.Home.HasPremiumPassPlus);
                }
            }
            return 0;
        }

        public override int GetCommandType()
        {
            return 527;
        }
    }
}
