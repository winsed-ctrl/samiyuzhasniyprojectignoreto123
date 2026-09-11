namespace GromCore.Laser.Server.Networking
{
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Message;
    using GromCore.Laser.Logic.Message.Battle;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Server.Message;
    using GromCore.Laser.Logic.Battle.Input;
    using System.Net;
    using System.Runtime.CompilerServices;

    public sealed class UDPSocket
    {
        public readonly long SessionId;
        private EndPoint _endPoint;

        public BattleMode Battle;
        public bool IsConnected => _endPoint != null;

        public Connection TCPConnection;
        public bool IsSpectator;

        public UDPGateway Gateway;
        public int Port;
        public int GatewayPort { get; set; }

        private const int HEADER_SIZE = 20;

        public UDPSocket(long sessionId)
        {
            SessionId = sessionId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetEndPoint(EndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public void SendMessage(GameMessage message)
        {
            var endpoint = _endPoint;
            var gateway = Gateway;
            var tcp = TCPConnection;

            if (endpoint == null || gateway == null || tcp == null) return;

            tcp.BattlePort = GatewayPort;

            if (message.GetEncodingLength() == 0) 
                message.Encode();

            int messageLength = message.GetEncodingLength();
            ByteStream stream = new(HEADER_SIZE + messageLength);
            stream.WriteLong(SessionId);
            stream.WriteShort(0);
            stream.WriteVInt(message.GetMessageType());
            stream.WriteVInt(messageLength);
            stream.WriteBytesWithoutLength(message.GetMessageBytes(), messageLength);

            gateway.SendTo(stream.GetByteArray(), 0, stream.GetOffset(), endpoint);
        }

        public void ProcessReceive(ByteStream stream)
        {
            try
            {
                int type = stream.ReadVInt();
                int length = stream.ReadVInt();

                if (length < 0 || length > 16 * 1024) return;
                if (!stream.CanRead(length)) return;

                byte[] data = stream.ReadBytes(length, 16 * 1024);

                GameMessage message = MessageFactory.Instance.CreateMessageByType(type);
                if (message == null) return;

                message.GetByteStream().SetByteArray(data, length);
                message.Decode();

                HandleMessage(message);
            }
            catch (Exception ex)
            {
                Logger.LogPrint($"UDPSocket ProcessReceive error: {ex.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandleMessage(GameMessage message)
        {
            if (message.GetMessageType() != 10555) return;

            var tcp = TCPConnection;
            var battle = Battle;

            if (tcp?.MessageManager != null)
            {
                tcp.MessageManager.LastKeepAlive = DateTime.UtcNow;
            }

            if (battle == null) return;

            var clientInputMessage = (ClientInputMessage)message;

            while (clientInputMessage.Inputs.TryDequeue(out ClientInput clientInput))
            {
                if (!IsSpectator)
                {
                    battle.AddClientInput(clientInput, SessionId);
                }
                else
                {
                    battle.HandleSpectatorInput(clientInput, SessionId);
                }
            }
        }
    }
}
