namespace GromCore.Laser.Logic.Friends
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Titan.DataStream;

    public class Friend
    {
        public long AccountId;
        public int Trophies;
        public PlayerDisplayData DisplayData;

        public int FriendState;
        public int FriendReason;

        [JsonIgnore] public ClientAvatar Avatar => LogicServerListener.Instance.GetAvatar(AccountId);

        public void Encode(ByteStream stream)
        {
            stream.WriteLong(AccountId);

            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);
            stream.WriteString(null);

            if(AccountId>0)  stream.WriteInt(Avatar.Trophies); 
            else stream.WriteInt(0);
            stream.WriteInt(FriendState);
            stream.WriteInt(FriendReason);
            stream.WriteInt(0);//FriendReasonDetails???
            stream.WriteInt(0);

            stream.WriteBoolean(false); // Alliance entry

            /*
             *     (*(void (__fastcall **)(__int64, _QWORD))(*(_QWORD *)a2 + 168LL))(a2, *(_QWORD *)(a1 + 88));
    (*(void (__fastcall **)(__int64, _QWORD))(*(_QWORD *)a2 + 136LL))(a2, *(unsigned int *)(a1 + 96));
    (*(void (__fastcall **)(__int64, _QWORD))(*(_QWORD *)a2 + 56LL))(a2, *(_QWORD *)(a1 + 104));
    (*(void (__fastcall **)(__int64, _QWORD))(*(_QWORD *)a2 + 136LL))(a2, *(unsigned int *)(a1 + 112));
    (*(void (__fastcall **)(__int64, _QWORD))(*(_QWORD *)a2 + 136LL))(a2, *(unsigned int *)(a1 + 116));
            
            stream.WriteLong(0);
            stream.WriteVInt(0);
            stream.WriteString("");
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            */

            stream.WriteString(null);
            if(AccountId>0) stream.WriteInt(LogicServerListener.Instance.IsPlayerOnline(AccountId) ? 0 : (int)(DateTime.UtcNow - Avatar.LastOnline).TotalSeconds); // Last online time
            else stream.WriteInt(0);
            stream.WriteInt(0);//RankedRankData::GetRankedRankDataByRank
            if (stream.WriteBoolean(DisplayData != null))
            {
                DisplayData.Encode(stream);
            }
            stream.WriteInt(0);
            stream.WriteInt(0);
            
        }
    }
}
