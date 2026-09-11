using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Message.Club;
using GromCore.Laser.Logic.Notification;
using GromCore.Laser.Logic.Stream.Entry;
using GromCore.Laser.Titan.DataStream;
using System.Net.Mail;
using System.Runtime;

namespace GromCore.Laser.Logic.Home.Items
{
    public class BattleLogEntry
    {
        public int IternalId;
        public DateTime CreationDate;
        public int Type;
        public int Result;
        public int Ticks;
        public bool BattleWithoutTrophies;
        public int Location;
        public int ResultType;
        public List<BattleLogPlayerEntry> PlayerEntries;
        public void Encode(ByteStream Stream)
        {
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(-(int)(CreationDate - DateTime.Now).TotalSeconds);

            Stream.WriteVInt(Type);
            Stream.WriteVInt(Result);
            Stream.WriteVInt(Ticks);

            Stream.WriteBoolean(BattleWithoutTrophies);
            ByteStreamHelper.WriteDataReference(Stream, Location);
            Stream.WriteVInt(ResultType);
            Stream.WriteVInt(-2);

            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);
            Stream.WriteInt(0);

            Stream.WriteVInt(0);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(0); 
            Stream.WriteVInt(PlayerEntries.Count); // player hero
            foreach (BattleLogPlayerEntry entry in PlayerEntries)
                entry.Encode(Stream);

            Stream.WriteVInt(100);
            Stream.WriteBoolean(false); // silowaya gonka
            Stream.WriteVInt(-1); // Challenge type
            Stream.WriteBoolean(true);
            Stream.WriteVInt(IternalId);
            if (Stream.WriteBoolean(false)) // Player Battlle Map
            {
                ByteStreamHelper.EncodeLogicLong(Stream, 1);
                Stream.WriteVInt(1);
                Stream.WriteInt(-1);
                ByteStreamHelper.EncodeLogicLong(Stream, 1);
            }

            if (Stream.WriteBoolean(false))
            {
                Stream.WriteVInt(9999);
                Stream.WriteVInt(9999);
                Stream.WriteBoolean(true);
                Stream.WriteVInt(9999);
                Stream.WriteVInt(9999);
            }
            int tmp = 0;
            Stream.WriteVInt(tmp);
            if (tmp > 0) Stream.WriteVInt(0);
        }
    }
}