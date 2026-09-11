using GromCore.Laser.Logic.Battle;
using GromCore.Laser.Logic.Battle.Objects;
using GromCore.Laser.Logic.Battle.Structures;
using GromCore.Laser.Server.Logic.Game;
using GromCore.Laser.Server.Networking.Session;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace GromCore.Laser.Server.Networking
{
    public static class RateLimiter
    {
        private static readonly ConcurrentDictionary<string, ConnectionData> Data = new();

        private sealed class ConnectionData
        {
            public int PacketCount;
            public int BytesCount;
            public long LastResetTicks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllowMessage(string ip, int size)
        {
            long nowTicks = DateTime.UtcNow.Ticks;
            long limitTicks = TimeSpan.FromMilliseconds(TCPGateway.RATE_LIMIT_TIME_MS).Ticks;

            var entry = Data.GetOrAdd(ip, _ => new ConnectionData
            {
                PacketCount = 0,
                BytesCount = 0,
                LastResetTicks = nowTicks
            });

            if (nowTicks - entry.LastResetTicks >= limitTicks)
            {
                entry.PacketCount = 0;
                entry.BytesCount = 0;
                entry.LastResetTicks = nowTicks;
            }

            if (entry.PacketCount >= TCPGateway.RATE_LIMIT || entry.BytesCount > TCPGateway.MAX_TRAFFIC_PER_SECOND)
            {
                BlockIP(ip);
                return false;
            }

            Interlocked.Increment(ref entry.PacketCount);
            Interlocked.Add(ref entry.BytesCount, size);
            return true;
        }

        public static void BlockIP(string ip)
        {
            IPConnectionLimiter.BlockIP(ip);
        }
    }

    public static class IPConnectionLimiter
    {
        private static readonly ConcurrentDictionary<string, int> ConnectionLimits = new();
        private static readonly ConcurrentDictionary<string, long> BlockedUntil = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllowConnection(string ip)
        {
            if (BlockedUntil.TryGetValue(ip, out var untilTicks))
            {
                if (DateTime.UtcNow.Ticks < untilTicks) return false;
                BlockedUntil.TryRemove(ip, out _);
            }

            int currentCount = ConnectionLimits.AddOrUpdate(ip, 1, (_, count) => count + 1);
            if (currentCount > TCPGateway.MAX_CONNECTIONS_PER_IP)
            {
                ConnectionLimits.AddOrUpdate(ip, 0, (_, count) => count - 1);
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveConnection(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return;
            ConnectionLimits.AddOrUpdate(ip, 0, (_, count) => Math.Max(0, count - 1));
        }

        public static void BlockIP(string ip)
        {
            BlockedUntil[ip] = DateTime.UtcNow.AddMinutes(1).Ticks;
        }

        public static void UnblockIP(string ip)
        {
            BlockedUntil.TryRemove(ip, out _);
        }
    }

    public static class ConnectionRateLimiter
    {
        private static readonly ConcurrentDictionary<string, ConnectionWindow> Windows = new();

        private sealed class ConnectionWindow
        {
            public int Count;
            public long WindowStartTicks;
        }

        private static readonly long WindowTicks = TimeSpan.FromSeconds(5).Ticks;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AllowNewConnection(string ip)
        {
            long nowTicks = DateTime.UtcNow.Ticks;

            var window = Windows.GetOrAdd(ip, _ => new ConnectionWindow
            {
                Count = 0,
                WindowStartTicks = nowTicks
            });

            if (nowTicks - window.WindowStartTicks > WindowTicks)
            {
                window.Count = 0;
                window.WindowStartTicks = nowTicks;
            }

            if (window.Count >= TCPGateway.MAX_CONNECTIONS_PER_SECOND)
                return false;

            Interlocked.Increment(ref window.Count);
            return true;
        }
    }

    public static class TCPGateway
    {
        private static readonly ConcurrentDictionary<int, Connection> ActiveConnections = new();
        private static Socket _listenerSocket;
        private static CancellationTokenSource _cts;
        private static int _connectionIdCounter;

        public const int MAX_PACKET_SIZE = 32 * 1024;
        public const int MAX_CONNECTIONS_PER_IP = 7;
        public const int MAX_ACTIVE_CONNECTIONS = 5000;
        public const int RATE_LIMIT = 100;
        public const int RATE_LIMIT_TIME_MS = 1000;
        public const int MAX_TRAFFIC_PER_SECOND = 128 * 1024;
        public const int MAX_CONNECTIONS_PER_SECOND = 5;
        public const int IDLE_TIMEOUT_MS = 30000;
        private const int RECEIVE_BUFFER_SIZE = 4096;
        private const int SOCKET_BACKLOG = 1000;

        public static void Init(string host, int port)
        {
            try
            {
                _cts = new CancellationTokenSource();

                _listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true,
                    LingerState = new LingerOption(false, 0)
                };

                _listenerSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listenerSocket.Bind(new IPEndPoint(IPAddress.Parse(host), port));
                _listenerSocket.Listen(SOCKET_BACKLOG);

                ConnectionLimiter.Start();

                Logger.LogPrint($"TCPSocket started: {host}:{port}");

                _ = AcceptLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
            }
        }

        private static async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Socket clientSocket = await _listenerSocket.AcceptAsync();
                    _ = HandleClientAsync(clientSocket);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private static async Task HandleClientAsync(Socket clientSocket)
        {
            string ip = null;
            Connection connection = null;

            try
            {
                ip = ((IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();

                if (!ConnectionRateLimiter.AllowNewConnection(ip))
                {
                    clientSocket.Close();
                    return;
                }

                if (!IPConnectionLimiter.AllowConnection(ip))
                {
                    clientSocket.Close();
                    return;
                }

                if (ActiveConnections.Count >= MAX_ACTIVE_CONNECTIONS)
                {
                    IPConnectionLimiter.RemoveConnection(ip);
                    clientSocket.Close();
                    return;
                }

                clientSocket.NoDelay = true;
                clientSocket.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;
                clientSocket.SendBufferSize = RECEIVE_BUFFER_SIZE;

                int connectionId = Interlocked.Increment(ref _connectionIdCounter);
                connection = new Connection(clientSocket)
                {
                    IP = ip
                };

                ActiveConnections.TryAdd(connectionId, connection);
                Connections.AddConnection(connection);


                await ReceiveLoopAsync(connection, connectionId);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (connection != null)
                {
                    HandleDisconnect(connection, Interlocked.Increment(ref _connectionIdCounter) - 1);
                }
                else if (ip != null)
                {
                    IPConnectionLimiter.RemoveConnection(ip);
                }
            }
        }

        private static async Task ReceiveLoopAsync(Connection connection, int connectionId)
        {
            byte[] buffer = new byte[RECEIVE_BUFFER_SIZE];

            while (connection.IsOpen)
            {
                int received;
                try
                {
                    received = await connection.Socket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                }
                catch
                {
                    break;
                }

                if (received <= 0) break;

                if (!RateLimiter.AllowMessage(connection.IP, received)) break;

                connection.Memory.Write(buffer, 0, received);

                if (connection.Memory.Length > MAX_PACKET_SIZE) break;

                if (connection.Messaging.OnReceive() != 0) break;
            }

            HandleDisconnect(connection, connectionId);
        }

        private static void HandleDisconnect(Connection connection, int connectionId)
        {
            if (connection == null) return;

            try
            {
                if (!connection.IsOpen) return;
                connection.IsOpen = false;

                string ip = connection.IP;
                ConnectionLimiter.OnClientDisconnect(ip);
                IPConnectionLimiter.RemoveConnection(ip);
                ActiveConnections.TryRemove(connectionId, out _);

                if (connection.MessageManager?.HomeMode?.Avatar != null)
                {
                    Sessions.Remove(connection.MessageManager.HomeMode.Avatar.AccountId);
                }

                BattleMode battle = Battles.Get(connection.Home?.HomeMode?.Avatar?.BattleId ?? -1);
                if (battle != null && connection.UdpSessionId > 0)
                {
                    BattlePlayer player = battle.GetPlayerBySessionId(connection.UdpSessionId);
                    if (player != null)
                    {
                        if(!battle.BattleWithTrophies && battle.m_players.FindAll(p=>p.IsBot() ==0).Count == 1)
                        {
                            battle.RemovePlayer(player);
                            battle.GameOver();
                            return;
                        }
                        Character c = battle.GetGameObjectManager().GetCharacterByPlayer(player);
                        player.Bot = 1;
                        c?.SetBot(1);
                    }
                }

                try { connection.Socket?.Shutdown(SocketShutdown.Both); } catch { }
                connection.Socket?.Dispose();
                connection.Memory?.Dispose();
            }
            catch (Exception ex)
            {
            }
        }

        public static void Stop()
        {
            _cts?.Cancel();
            try { _listenerSocket?.Shutdown(SocketShutdown.Both); } catch { }
            _listenerSocket?.Dispose();
        }

        public static void Restart()
        {
            Stop();
            Task.Delay(1000).Wait();
            Init("0.0.0.0", Settings.Configuration.Instance.TcpPort);
        }
    }
}
