namespace GromCore.Laser.Logic.Home.Structures
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;
    using GromCore.Laser.Logic.Home.Structures;
    using System.Runtime.CompilerServices;

    [JsonObject(MemberSerialization.OptIn)]
    public class PlayerMap
    {

        [JsonProperty] public long MapId;
        [JsonProperty] public string MapName;
        [JsonProperty] public int GMV;
        [JsonProperty] public int MapEnvironmentData;
        [JsonProperty] public byte[] MapData;

        [JsonProperty] public long AccountId;
        [JsonProperty] public string AvatarName;
        [JsonProperty] public int State = 1;
        [JsonProperty] public Dictionary<long, int> Votes = new();

        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.EncodeLogicLong(stream,MapId);
            stream.WriteString(MapName);
            stream.WriteVInt(GMV);
            stream.WriteDataReference(54, MapEnvironmentData);
            if (MapData != null) stream.WriteBytes(MapData, MapData.Length);
            else stream.WriteBytes(null, 1);
            ByteStreamHelper.EncodeLogicLong(stream,AccountId);
            stream.WriteString(AvatarName);
            stream.WriteVInt(State);//state 
            /*
             * 1 - waiting for publish
             * 2 - publishing
             * 3 - approved
             * 4 - published
             * 5 - winner
             * 6 - dont approved
             * 7 - blocked
             */
            
            stream.WriteLong(0);//update time since epoch
            stream.WriteVInt(Votes.Count); // всего поигравшиш
            stream.WriteVInt(Votes.Values.Count(p => p == 1));//likes
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }
    }
}
