using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using GromCore.Laser.Logic.Avatar.Structures;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Ranked;

namespace GromCore.Laser.Logic.Message.Ranked
{
    public class RankedMatchStartedMessage : GameMessage
    {
        public bool Stub;
        public int Modificator;
        public int SelectedMap;
        public int Hero;
        public int Team;
        public List<RankedMatchPlayer> Players;
        public long PROJECTGROM_RANKED_MATCH_ID;
        public override void Encode()
        {
            Stream.WriteBoolean(true);
            ByteStreamHelper.EncodeLogicLong(Stream, PROJECTGROM_RANKED_MATCH_ID);
            Stream.WriteDataReference(15, SelectedMap);


            Stream.WriteVInt(Players.Count);
            foreach (RankedMatchPlayer p in Players)
                Stream.WriteVInt(p.TeamIndex);

            Stream.WriteVInt(Modificator);
            Stream.WriteVInt(1);

            if (Stub)
            {
                Stream.WriteVInt(1);
                {
                    Stream.WriteLogicLong(0, 1);
                    Stream.WriteBoolean(true);
                    Stream.WriteString("Shelly");
                    Stream.WriteVInt(5000);
                    Stream.WriteVInt(GlobalId.CreateGlobalId(28, 0));
                    Stream.WriteVInt(GlobalId.CreateGlobalId(43, 0));
                    Stream.WriteVInt(-1);

                    Stream.WriteVInt(1); // PlayerLVL
                    Stream.WriteVInt(1); // Trophies
                    Stream.WriteVInt(1); // PlayerLVL
                    ByteStreamHelper.WriteDataReference(Stream, 0);

                    Stream.WriteVInt(0); // PlayerLVL
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteBoolean(true);
                    Stream.WriteVInt(999);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteVInt(1);
                    ByteStreamHelper.WriteDataReference(Stream, 0);
                    Stream.WriteVInt(0);
                }
            }
            else
            {
                Stream.WriteVInt(Players.Count);
                foreach (RankedMatchPlayer p in Players)
                    p.Encode(Stream);
            }

            Stream.WriteVInt(Team);
        }

        public override int GetMessageType()
        {
            return 22150;
        }

        public override int GetServiceNodeType()
        {
            return 9;
        }
    }
}
