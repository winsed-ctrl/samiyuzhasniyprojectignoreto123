namespace GromCore.Laser.Logic.Message.Friends
{
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Friends;
    using GromCore.Laser.Logic.Team;
    using GromCore.Laser.Logic.Util;

    public class FriendOnlineStatusEntryMessage : GameMessage
    {
        public long AvatarId;
        public int PlayerStatus;
        public TeamEntry AllianceTeamEntry;
        public int donotdisturb;
        public bool sft;
        public int selectedGameMode;
        public override void Encode()
        {
            Stream.WriteLong(AvatarId);
            if (Stream.WriteBoolean(PlayerStatus >= 0))
            {
                Stream.WriteLong(AvatarId);
                Stream.WriteVInt(PlayerStatus);
                Stream.WriteVInt(0);
                Stream.WriteBoolean(donotdisturb == 1 ? true : false); // не беспокоить
                if (Stream.WriteBoolean(AllianceTeamEntry != null))// SocialTeamEntry::encode
                { 
                    Stream.WriteVInt(AllianceTeamEntry.Type); // team type
                    
                    Stream.WriteVInt(AllianceTeamEntry.Type != 1 ? 3 : GamePlayUtil.GetPlayerCountWithGameModeVariation(GameModeUtil.GetGameModeVariation(DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(AllianceTeamEntry.LocationId).GameModeVariation))); // mesta
                    Stream.WriteLong(AllianceTeamEntry.Id); // teamid

                    Stream.WriteVInt(0); 
                    Stream.WriteVInt(0);
                    Stream.WriteVInt(0);
                    Stream.WriteVInt(0);
                    Stream.WriteVInt(0);

                    Stream.WriteBoolean(false);
                    Stream.WriteBoolean(false);
                    Stream.WriteBoolean(false);
                    Stream.WriteVInt(AllianceTeamEntry.Members.Count);
                    foreach (TeamMember mem in AllianceTeamEntry.Members)
                    {
                        Stream.WriteLong(mem.AccountId); // id
                        Stream.WriteVInt(mem.HeroTrophies); // trophies
                        Stream.WriteVInt(0);
                        Stream.WriteVInt(0);
                        mem.DisplayData.Encode(Stream);
                    }
                    Stream.WriteDataReference(AllianceTeamEntry.LocationId);
                    Stream.WriteVInt(AllianceTeamEntry.EventSlot); //gamemode
                    Stream.WriteBoolean(sft);
                } 
                Stream.WriteVInt(selectedGameMode); // Selected gamemode
                Stream.WriteBoolean(sft); // в поиске команды
            }
        }

        public override int GetMessageType()
        {
            return 24555;
        }

        public override int GetServiceNodeType()
        {
            return 3;
        }
    }
}
