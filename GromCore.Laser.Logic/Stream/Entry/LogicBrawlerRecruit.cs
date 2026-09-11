using Newtonsoft.Json;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Stream.Entry
{
    public class LogicBrawlerRecruit
    {
        [JsonProperty] public int GlobalId;
        [JsonProperty] public int GemsPrice;
        [JsonProperty] public int TokenPrice;
        [JsonProperty] public int Index;
        [JsonProperty] public List<int> OtherUnlockVariations = new();
        public void Encode(ByteStream stream, ClientHome _home, List<LogicBrawlerRecruit> _nextUnlock = null)
        {
            ByteStreamHelper.WriteDataReference(stream, GlobalId);
            stream.WriteVInt(TokenPrice);
            stream.WriteVInt(GemsPrice);
            stream.WriteVInt(0);
            stream.WriteVInt(_home.RecruitTokens);
            stream.WriteVInt(Index);
            Console.WriteLine(Index);
            if (OtherUnlockVariations != null && OtherUnlockVariations.Count > 0)
            {
                stream.WriteVInt(OtherUnlockVariations.Count);
                foreach (int _o in OtherUnlockVariations)
                    ByteStreamHelper.WriteDataReference(stream, _o);
            }
            else stream.WriteVInt(0);
        }
    }
}
