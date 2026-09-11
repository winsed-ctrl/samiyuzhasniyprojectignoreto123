using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Titan.DataStream;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Home.Items
{
    public class BattleLogPlayerEntry
    {
        public PlayerDisplayData Data;
        public int Brawler;
        public int BrawlerPowerLVL;
        public bool IsStarPlayer;
        public long AccountId;
        public int Index;
        public int Trophies;
        public BattleLogPlayerEntry() { }
        public void Encode(ByteStream Stream)
        {
            Stream.WriteVInt(1); 
            Stream.WriteLong(AccountId); 
            Stream.WriteVInt(Index);
            Stream.WriteBoolean(IsStarPlayer);  
            Stream.WriteVInt(1); 
            {
                ByteStreamHelper.WriteDataReference(Stream, Brawler); 
                Stream.WriteVInt(Trophies); 
                Stream.WriteVInt(-5);
                Stream.WriteVInt(BrawlerPowerLVL); 
            }
            Stream.WriteVInt(1); 
            Data.Encode(Stream);
            if (Stream.WriteBoolean(false))
                Stream.WriteLong(1);
        }
    }
}
