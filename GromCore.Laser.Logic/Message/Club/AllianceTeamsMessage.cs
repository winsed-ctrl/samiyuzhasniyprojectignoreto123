namespace GromCore.Laser.Logic.Message.Club
{
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Stream.Entry;
    using System.IO;

    public class AllianceTeamsMessage : GameMessage
    {

        public override void Encode()
        {
            Stream.WriteBoolean(true);
            Stream.WriteVInt(1);

            Stream.WriteVInt(0); // team type

            Stream.WriteVInt(1); // mesta
            Stream.WriteLong(0); // teamid

            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);

            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteBoolean(false);
            Stream.WriteVInt(1);

            Stream.WriteLong(0); // id
            Stream.WriteVInt(999); // trophies
            Stream.WriteVInt(0);
            Stream.WriteVInt(0);
            new PlayerDisplayData(GlobalId.CreateGlobalId(28, 0), GlobalId.CreateGlobalId(43, 1), "Player", true, true).Encode(Stream);
            Stream.WriteDataReference(0);
            Stream.WriteVInt(0); //gamemode
            Stream.WriteBoolean(false);
        }

        public override int GetMessageType()
        {
            return 24364;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
