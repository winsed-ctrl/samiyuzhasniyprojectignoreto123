namespace GromCore.Laser.Logic.Message.Ranking
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using System.Numerics;
    using System.Reflection.Emit;
    using System.Reflection.Metadata;

    public class LeaderboardMessage : GameMessage
    {
        public int LeaderboardType { get; set; }

        public List<KeyValuePair<ClientHome, ClientAvatar>> Avatars;
        public Dictionary<ClientHome, ClientAvatar> Brawlers;
        public List<Alliance> AllianceList;
        public long OwnAvatarId;
        public string Region { get; set; }
        public int HeroDataId;
        public int idk;
        public string AvatarRegion;
        
        public LeaderboardMessage() : base()
        {
            Avatars = new List<KeyValuePair<ClientHome, ClientAvatar>>();
            AllianceList = new List<Alliance>();
            LeaderboardType = 1;
        }

        public override void Encode()
        {
            int playerIndex = 0;
            Stream.WriteVInt(LeaderboardType);
            Stream.WriteVInt(idk);
            ByteStreamHelper.WriteDataReference(Stream, HeroDataId);
            Stream.WriteString(Region);
            if (LeaderboardType == 1)
            {
                Stream.WriteVInt(Avatars.Count);
                foreach (var pair in Avatars)
                {
                    var home = pair.Key;
                    var avatar = pair.Value;
                    if (avatar.AccountId == OwnAvatarId)
                    {
                        playerIndex = Avatars.IndexOf(pair) + 1;
                    }

                    Stream.WriteVLong(avatar.AccountId);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(avatar.Trophies);
                    //Stream.WriteVInt(avatar.Trophies);

                    Stream.WriteBoolean(true);
                    Stream.WriteString(avatar.AllianceName);
                    Stream.WriteString(avatar.Name ?? "���� �������");
                    Stream.WriteVInt(100);
                    int icon = home.ThumbnailId;
                    var icondata = DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(icon);
                    if (icondata == null || icondata.Disabled || icondata.LockedForChronos) icon = GlobalId.CreateGlobalId(28, 0);
                    int name = home.NameColorId;
                    var namedata = DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(name);
                    if (namedata == null) name = GlobalId.CreateGlobalId(43, 0);
                    Stream.WriteVInt(icon);
                    Stream.WriteVInt(name);
                    Stream.WriteVInt(0);
                    Stream.WriteBoolean(false);
                }
            }
            if (LeaderboardType == 4)
            {
                Stream.WriteVInt(Avatars.Count);
                foreach (var pair in Avatars)
                {
                    var home = pair.Key;
                    var avatar = pair.Value;
                    if (avatar.AccountId == OwnAvatarId)
                    {
                        playerIndex = Avatars.IndexOf(pair) + 1;
                    }

                    Stream.WriteVLong(avatar.AccountId);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(home.RankedSoloRank);
                    //Stream.WriteVInt(avatar.Trophies);

                    Stream.WriteBoolean(true);
                    Stream.WriteString(avatar.AllianceName);
                    Stream.WriteString(avatar.Name ?? "���� �������");
                    Stream.WriteVInt(100);
                    int icon = home.ThumbnailId;
                    var icondata = DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(icon);
                    if (icondata == null || icondata.Disabled || icondata.LockedForChronos) icon = GlobalId.CreateGlobalId(28, 0);
                    int name = home.NameColorId;
                    var namedata = DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(name);
                    if (namedata == null) name = GlobalId.CreateGlobalId(43, 0);
                    Stream.WriteVInt(icon);
                    Stream.WriteVInt(name);
                    Stream.WriteVInt(0);
                    Stream.WriteBoolean(false);
                }
            }
            if (LeaderboardType == 5)
            {
                Stream.WriteVInt(Avatars.Count);
                foreach (var pair in Avatars)
                {
                    var home = pair.Key;
                    var avatar = pair.Value;
                    if (avatar.AccountId == OwnAvatarId)
                    {
                        playerIndex = Avatars.IndexOf(pair) + 1;
                    }

                    Stream.WriteVLong(avatar.AccountId);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(home.RankedTrioRank);
                    //Stream.WriteVInt(avatar.Trophies);

                    Stream.WriteBoolean(true);
                    Stream.WriteString(avatar.AllianceName);
                    Stream.WriteString(avatar.Name ?? "���� �������");
                    Stream.WriteVInt(100);
                    int icon = home.ThumbnailId;
                    var icondata = DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(icon);
                    if (icondata == null || icondata.Disabled || icondata.LockedForChronos) icon = GlobalId.CreateGlobalId(28, 0);
                    int name = home.NameColorId;
                    var namedata = DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(name);
                    if (namedata == null) name = GlobalId.CreateGlobalId(43, 0);
                    Stream.WriteVInt(icon);
                    Stream.WriteVInt(name);
                    Stream.WriteVInt(0);
                    Stream.WriteBoolean(false);
                }
            }

            else if (LeaderboardType == 0)
            {
                Stream.WriteVInt(Avatars.Count);
                foreach (var pair in Avatars)
                {
                    var home = pair.Key;
                    var avatar = pair.Value;
                    if (avatar.AccountId == OwnAvatarId)
                    {
                        playerIndex = Avatars.IndexOf(pair) + 1;
                    }

                    Stream.WriteVLong(avatar.AccountId);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(avatar.GetHero(HeroDataId).Trophies);

                    Stream.WriteBoolean(true);
                    Stream.WriteString(avatar.AllianceName);
                    Stream.WriteString(avatar.Name ?? "���� �������");
                    Stream.WriteVInt(100);
                    int icon = home.ThumbnailId;
                    var icondata = DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(icon);
                    if (icondata == null || icondata.Disabled || icondata.LockedForChronos) icon = GlobalId.CreateGlobalId(28, 0);
                    int name = home.NameColorId;
                    var namedata = DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(name);
                    if (namedata == null) name = GlobalId.CreateGlobalId(43, 0);
                    Stream.WriteVInt(icon);
                    Stream.WriteVInt(name);
                    Stream.WriteVInt(0);
                    Stream.WriteBoolean(false);
                }
            }
            else if (LeaderboardType == 2)
            {
                Stream.WriteVInt(AllianceList.Count);
                foreach (var alliance in AllianceList)
                {
                    Stream.WriteVLong(alliance.Id);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(alliance.Trophies);

                    Stream.WriteVInt(2);

                    Stream.WriteString(alliance.Name);
                    Stream.WriteVInt(alliance.Members.Count);
                    ByteStreamHelper.WriteDataReference(Stream, alliance.AllianceBadgeId);
                }
            }
            else if (LeaderboardType == 3)
            {
                Stream.WriteVInt(Avatars.Count);
                foreach (var pair in Avatars)
                {
                    var home = pair.Key;
                    var avatar = pair.Value;
                    if (avatar.AccountId == OwnAvatarId)
                    {
                        playerIndex = Avatars.IndexOf(pair) + 1;
                    }

                    Stream.WriteVLong(avatar.AccountId);

                    Stream.WriteVInt(1);
                    Stream.WriteVInt(22);

                    Stream.WriteVInt(3);

                    Stream.WriteString(avatar.AllianceName); // Club name

                    Stream.WriteString(avatar.Name ?? "Brawler");
                    Stream.WriteVInt(100);
                    int icon = home.ThumbnailId;
                    var icondata = DataTables.Get(DataType.PlayerThumbnail).GetDataByGlobalId<PlayerThumbnailData>(icon);
                    if (icondata == null || icondata.Disabled || icondata.LockedForChronos) icon = GlobalId.CreateGlobalId(28, 0);
                    int name = home.NameColorId;
                    var namedata = DataTables.Get(DataType.NameColor).GetDataByGlobalId<NameColorData>(name);
                    if (namedata == null) name = GlobalId.CreateGlobalId(43, 0);
                    Stream.WriteVInt(icon);
                    Stream.WriteVInt(name);
                    Stream.WriteVInt(0);
                    Stream.WriteVInt(0);
                }
            }
            Stream.WriteVInt(0);
            Stream.WriteVInt(playerIndex);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteString(AvatarRegion ?? "EN");
        }

        public override int GetMessageType()
        {
            return 24403;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
