namespace GromCore.Laser.Logic.Team
{
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Logic.Home;
    public class TeamMember
    {
        public bool IsOwner;
        public long AccountId;
        public int CharacterId;
        public int SkinId;
        public int HeroTrophies;
        public int HeroHighestTrophies;
        public int HeroLevel;
        public int State;
        public bool IsReady;
        public int SelectedStarPowerId;
        public int SelectedGadget;
        public int SelectedGear1;
        public int SelectedGear2;
        public int SelectedOvercharge;
        public PlayerDisplayData DisplayData;
        public HomeMode homeMode;
        public int TeamIndex;
        public int TeamType;
        public void Encode(ByteStream stream)
        {
            stream.WriteBoolean(IsOwner);
            stream.WriteLong(AccountId);

            ByteStreamHelper.WriteDataReference(stream, CharacterId);
            ByteStreamHelper.WriteDataReference(stream, SkinId);

            stream.WriteVInt(0);
            stream.WriteVInt(HeroTrophies);//tropies
            stream.WriteVInt(HeroHighestTrophies);
            stream.WriteVInt(HeroLevel);//power
            stream.WriteVInt(State);

            stream.WriteBoolean(IsReady);

            stream.WriteVInt(TeamIndex); // team
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk
            stream.WriteVInt(0); // unk

            DisplayData.Encode(stream);

            ByteStreamHelper.WriteDataReference(stream, null); 
            ByteStreamHelper.WriteDataReference(stream, null); 
            ByteStreamHelper.WriteDataReference(stream, null); 
            ByteStreamHelper.WriteDataReference(stream, null); 
            ByteStreamHelper.WriteDataReference(stream, null);

            stream.WriteVInt(0);
        }
    }
}
