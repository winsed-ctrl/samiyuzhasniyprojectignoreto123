using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Listener;

namespace GromCore.Laser.Logic.Message.Club
{
    public class RespondToAllianceJoinRequestMessage : GameMessage
    {
        public long AccountId;
        public bool Accept;

        public override void Decode()
        {
            AccountId = Stream.ReadLong();
            Accept = Stream.ReadBoolean();
            if (LogicServerListener.Instance.IsDev()) Console.WriteLine($"Respond received: to {AccountId}, Accepted?: {Accept}");
        }

        public override int GetMessageType()
        {
            return 14321;
        }

        public override int GetServiceNodeType()
        {
            return 11;
        }
    }
}
