using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Message.Club
{
    public class AlliancePremadeChatMessage : GameMessage
    {
        public int EmojiGlobalId;
        public int DefEmojiSlot;

        public override void Decode()
        {
            Stream.ReadVInt();
            DefEmojiSlot = Stream.ReadVInt(); // def emoji type
            EmojiGlobalId = Stream.ReadVInt(); // emoji global id(not work for def)

        }
        public override void Encode()
        {
            // Stream.WriteVInt(ResponseType);
        }

        public override int GetMessageType()
        {
            return 14469;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}