namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSetPlayerThumbnailCommand : Command
    {
        public int ThumbnailInstanceId;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            ThumbnailInstanceId = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            PlayerThumbnailData icon = DataTables.Get(DataType.PlayerThumbnail).GetDataWithId<PlayerThumbnailData>(ThumbnailInstanceId);
            if (icon == null) return -1;
            if (icon.Name != "base1" && icon.RequiredTotalTrophies == 0 && icon.RequiredHero == null && !homeMode.Home.UnlockedThumbnails.Contains(GlobalId.CreateGlobalId(28, ThumbnailInstanceId))) return -1;
            if (ThumbnailInstanceId < 0) return -1;
            if (ThumbnailInstanceId > DataTables.Get(DataType.PlayerThumbnail).Count) return 2;

            homeMode.Home.ThumbnailId = GlobalId.CreateGlobalId(28, ThumbnailInstanceId);

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
            return 505;
        }
    }
}
