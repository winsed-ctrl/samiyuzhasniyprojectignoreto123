namespace GromCore.Laser.Logic.Stream.Entry
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Titan.DataStream;
    using System.Runtime.Serialization;

    public class AllianceStreamEntry
    {
        public long Id;
        public long AuthorId;
        public string AuthorName;
        public string Message;
        public AllianceRole AuthorRole;
        public DateTime SendTime;
        public long PlayerId;
        public string PlayerName;
        public int Type;
        public int Event;
        public int MessageData;
        public int EmoteId;
        public string WhoAccepted;
        public PlayerDisplayData StreamDisplay;
        public int Action;

        public AllianceStreamEntry()
        {
            SendTime = DateTime.UtcNow;
        }

        public void Encode(ByteStream stream)
        {
            if (StreamDisplay == null) StreamDisplay = new PlayerDisplayData(GlobalId.CreateGlobalId(28, 0), GlobalId.CreateGlobalId(43, 1), "Player", true, true);
            stream.WriteVInt(Type);
            stream.WriteVLong(Id);
            stream.WriteVLong(AuthorId);
            stream.WriteString(AuthorName);
            stream.WriteVInt((int)AuthorRole);
            stream.WriteVInt((int)(DateTime.UtcNow - SendTime).TotalSeconds);
            stream.WriteVInt(0);
            switch (Type)
            {
                case 4:
                    stream.WriteVInt(Event);
                    stream.WriteVInt(1);
                    stream.WriteVLong(PlayerId);
                    stream.WriteString(PlayerName);
                    break;
                case 8:
                    stream.WriteDataReference(40, MessageData);
                    stream.WriteBoolean(false);
                    stream.WriteString(null);
                    stream.WriteVInt(0);
                    stream.WriteVInt(EmoteId);
                    break;
                case 3: // da
                    stream.WriteString(Message);
                    stream.WriteString(WhoAccepted);
                    stream.WriteVInt(Action);
                    StreamDisplay.Encode(stream);
                    break;
                default:
                    stream.WriteString(Message);
                    stream.WriteVInt(0);
                    break;

            }
        }
    }
}
