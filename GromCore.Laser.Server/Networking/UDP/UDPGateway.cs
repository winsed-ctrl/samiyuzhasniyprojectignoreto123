namespace GromCore.Laser.Server.Networking
{
    using GromCore.Laser.Server.Networking.UDP.Game;
    using GromCore.Laser.Server.Settings;
    using GromCore.Laser.Titan.DataStream;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    public static class UPD
    {
        public static ConcurrentDictionary<int, UDPGateway> UdpServers = new();

        public static List<int> AllowedPorts = Configuration.Instance.UdpPorts;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRandomPort()
        {
            int minPort = -1;
            int minCount = int.MaxValue;

            foreach (var kvp in UdpServers)
            {
                int count = kvp.Value.GetCurrentPlayerCount();
                if (count < minCount)
                {
                    minCount = count;
                    minPort = kvp.Key;
                }
            }

            return minPort;
        }

        public static void SetPorts(List<int> ports)
        {
            AllowedPorts.Clear();
            AllowedPorts = ports;
        }
    }

    public sealed class UDPGateway : IDisposable
    {
        private Socket _socket;
        private readonly ConcurrentDictionary<long, UDPSocket> _sockets = new();
        private readonly CancellationTokenSource _cts = new();
        private long _sessionCounter;

        // Rate limiting
        private const int MAX_PACKETS_PER_SECOND = 100;
        private const int MAX_SESSIONS_PER_IP = 5;
        private const int BLACKLIST_DURATION_MIN = 5;
        private const int PACKET_MIN_LENGTH = 10;
        private const int BUFFER_SIZE = 1500;
        private const int RECEIVE_PARALLELISM = 4; 

        private readonly ConcurrentDictionary<string, int> _packetCounters = new();
        private readonly ConcurrentDictionary<string, int> _sessionCounters = new();
        private static readonly ConcurrentDictionary<string, long> _blacklist = new();
        private Timer _resetTimer;

        private int _port;
        private string _ip;
        private bool _disposed;

        public UDPGateway()
        {
            _resetTimer = new Timer(ResetCounters, null, 1000, 1000);
        }

        private void ResetCounters(object state)
        {
            _packetCounters.Clear();
            CleanBlacklist();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CleanBlacklist()
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            foreach (var ip in _blacklist.Keys)
            {
                if (_blacklist.TryGetValue(ip, out var unblockTicks) && nowTicks >= unblockTicks)
                {
                    _blacklist.TryRemove(ip, out _);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsIpBlocked(string ip)
        {
            if (_blacklist.TryGetValue(ip, out var unblockTicks))
            {
                if (DateTime.UtcNow.Ticks < unblockTicks) return true;
                _blacklist.TryRemove(ip, out _);
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BlockIp(string ip)
        {
            _blacklist[ip] = DateTime.UtcNow.AddMinutes(BLACKLIST_DURATION_MIN).Ticks;
            Logger.LogPrint($"IP blocked: {ip} for {BLACKLIST_DURATION_MIN} minutes");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateSessionIp(string ip)
        {
            _sessionCounters.AddOrUpdate(ip, 1, (_, count) => count + 1);
        }

        public void RemoveSocket(string ip)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                _sessionCounters.AddOrUpdate(ip, 0, (_, count) => Math.Max(0, count - 1));
            }
        }

        public void RemoveSocket(long sessionId)
        {
            _sockets.TryRemove(sessionId, out _);
        }

        public void Init(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.ReceiveBufferSize = 1024 * 1024; // 1MB
            _socket.SendBufferSize = 1024 * 1024;    // 1MB

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const int SIO_UDP_CONNRESET = -1744830452;
                _socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }

            _socket.Bind(new IPEndPoint(IPAddress.Parse(host), port));

            _ip = host;
            _port = port;

            for (int i = 0; i < RECEIVE_PARALLELISM; i++)
            {
                _ = ReceiveLoopAsync(_cts.Token);
            }

            Logger.LogPrint($"UDP Server started at {host}:{port} with {RECEIVE_PARALLELISM} receivers");
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _socket.ReceiveFromAsync(
                            new ArraySegment<byte>(buffer, 0, BUFFER_SIZE),
                            SocketFlags.None,
                            new IPEndPoint(IPAddress.Any, 0));

                        ProcessPacket(buffer, result.ReceivedBytes, result.RemoteEndPoint);
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        Logger.LogPrint($"Receive exception: {e.GetType().Name} - {e.Message}");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsGarbagePacket(byte[] buffer, int length)
        {
            int nonZero = 0;
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] != 0)
                {
                    if (++nonZero > 5) return false;
                }
            }
            return true;
        }

        private void ProcessPacket(byte[] buffer, int receivedBytes, EndPoint remoteEndPoint)
        {
            if (receivedBytes < PACKET_MIN_LENGTH) return;
            if (IsGarbagePacket(buffer, receivedBytes)) return;

            var ipEndPoint = (IPEndPoint)remoteEndPoint;
            string remoteIp = ipEndPoint.Address.ToString();

            if (IsIpBlocked(remoteIp)) return;

            int packets = _packetCounters.AddOrUpdate(remoteIp, 1, (_, count) => count + 1);
            if (packets > MAX_PACKETS_PER_SECOND)
            {
                return;
            }

            try
            {
                long sessionId = ReadLongBigEndian(buffer);

                if (!_sockets.TryGetValue(sessionId, out var client))
                {
                    if (_sessionCounters.GetValueOrDefault(remoteIp) >= MAX_SESSIONS_PER_IP)
                    {
                        BlockIp(remoteIp);
                        return;
                    }
                    UpdateSessionIp(remoteIp);
                    return; 
                }

                client.TCPConnection.BattlePort = _port;
                client.TCPConnection.MessageManager.HomeMode.Avatar.BattlePort = _port;

                if (!client.IsConnected)
                {
                    client.SetEndPoint(remoteEndPoint);

                    if (client.Battle == null) return;

                    if (!client.IsSpectator)
                    {
                        var player = client.Battle.GetPlayerBySessionId(client.SessionId);
                        if (player != null)
                        {
                            player.GameListener = new UDPGameListener(client, client.TCPConnection);
                        }
                    }
                }

                ByteStream stream = new(buffer, receivedBytes);
                stream.ReadLong(); 
                stream.ReadShort(); 

                client.ProcessReceive(stream);
            }
            catch (Exception e)
            {
                Logger.LogPrint($"Packet processing exception: {e.GetType().Name} - {e.Message}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long ReadLongBigEndian(byte[] buffer)
        {
            return ((long)buffer[0] << 56) |
                   ((long)buffer[1] << 48) |
                   ((long)buffer[2] << 40) |
                   ((long)buffer[3] << 32) |
                   ((long)buffer[4] << 24) |
                   ((long)buffer[5] << 16) |
                   ((long)buffer[6] << 8) |
                   buffer[7];
        }

        public UDPSocket CreateSocket()
        {
            long sessionId = Interlocked.Increment(ref _sessionCounter);
            var socket = new UDPSocket(sessionId)
            {
                Gateway = this,
                GatewayPort = GetGateWayPort()
            };

            _sockets.TryAdd(sessionId, socket);
            return socket;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SendTo(byte[] data, int startIndex, int count, EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null || _disposed) return;

            try
            {
                _socket.SendTo(data, startIndex, count, SocketFlags.None, remoteEndPoint);
            }
            catch (SocketException)
            {}
            catch (ObjectDisposedException)
            {}
        }

        public int GetCurrentPlayerCount()
        {
            int count = 0;
            foreach (var socket in _sockets.Values)
            {
                if (socket.IsConnected) count++;
            }
            return count;
        }

        public int GetGateWayPort()
        {
            if (_socket == null || !_socket.IsBound) return -1;
            return ((IPEndPoint)_socket.LocalEndPoint).Port;
        }

        public void Stop()
        {
            _cts.Cancel();
        }

        public void Shutdown()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            _resetTimer?.Dispose();

            try
            {
                _socket?.Shutdown(SocketShutdown.Both);
            }
            catch { }

            _socket?.Dispose();
            _sockets.Clear();

            _cts.Dispose();
        }
    }
}
