using Newtonsoft.Json.Bson;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Stream
{
    public class KudoStatus(List<Dictionary<int, int>> kvp)
    {
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(kvp.Count);
            foreach(var i in kvp)
            {
                foreach(KeyValuePair<int,int> KvP in i)
                {
                    stream.WriteByte((byte)KvP.Key);
                    stream.WriteByte((byte)KvP.Key);
                }
            }
        }

        public void Decode(ByteStream stream) // where it needed? idk
        {
            int integer = stream.ReadVInt();
            for(int i  = 0; i < integer; i++)
            {
                int v1 = stream.ReadByte();
                int v2 = stream.ReadByte();
            }
        }
    }
}
