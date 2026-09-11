using GromCore.Laser.Logic.Avatar;
using GromCore.Laser.Logic.Battle;
using GromCore.Laser.Logic.Battle.Objects;
using GromCore.Laser.Logic.Battle.Structures;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Logic.Message;
using GromCore.Laser.Server.Logic.Game;
using GromCore.Laser.Server.Message;
using GromCore.Laser.Server.Networking.Session;
using GromCore.Laser.Server.Utils;
using GromCore.Laser.Titan.Debug;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace GromCore.Laser.Server.Networking
{
    public class Connection
    {
        public Messaging Messaging { get; }
        public MessageManager MessageManager { get; }

        public byte[] ReadBuffer { get; }
        public Socket Socket { get; private set; }

        public int Ping { get; private set; }

        public MemoryStream Memory { get; set; }

        public bool IsOpen { get; set; }
        public string IP { get; set; }  

        public int MatchmakeSlot;
        public MatchmakingEntry MatchmakingEntry;

        public long UdpSessionId;
        public string Nonce;

        public int BattlePort;

        public ClientHome Home => MessageManager?.HomeMode?.Home;
        public ClientAvatar Avatar => MessageManager?.HomeMode?.Avatar;

        private long _closed = 0;

        public Connection(Socket socket)
        {
            if (socket == null)
                throw new ArgumentNullException(nameof(socket));

            this.Socket = socket;
            this.ReadBuffer = new byte[1024];
            this.Memory = new MemoryStream();
            this.Messaging = new Messaging(this);
            this.MessageManager = new MessageManager(this);

            this.IsOpen = true;
            this.MatchmakeSlot = -1;
            this.UdpSessionId = -1;

            try
            {
                string ip = ((IPEndPoint)socket.RemoteEndPoint)?.Address.ToString() ?? "Unknown";
                this.IP = ip;
                ConnectionLimiter.AllowConnection(ip);
            }
            catch
            {
                this.IP = "Unknown";
            }
        }

        public void PingUpdated(int value)
        {
            this.Ping = value;
        }

        public void Send(GameMessage message)
        {
            if (message == null || !this.IsOpen || Socket == null || !Socket.Connected)
                return;

            try
            {
                this.Messaging.Send(message);
            }
            catch (Exception ex)
            {
                Logger.Error($"[Connection] Error: {ex.Message}");
                Close();
            }
        }

        public void Write(byte[] buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return;

            if (Interlocked.Read(ref _closed) != 0)
                return;

            var socket = Socket;
            if (socket == null || !socket.Connected)
                return;

            try
            {
                socket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, OnSend, socket);
            }
            catch (ObjectDisposedException)
            {
                Close();
            }
            catch (SocketException sex) when (sex.SocketErrorCode is 
                SocketError.ConnectionReset or 
                SocketError.NotConnected or 
                SocketError.OperationAborted or 
                SocketError.ConnectionAborted or
                SocketError.Shutdown)
            {
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogPrint($"[Connection] Unexpected error during send: {ex.Message}");
                Close();
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                var socket = (Socket)ar.AsyncState;
                if (socket != null && socket.Connected)
                {
                    socket.EndSend(ar);
                }
            }
            catch (SocketException sex) when (sex.SocketErrorCode is 
                SocketError.ConnectionReset or 
                SocketError.NotConnected or 
                SocketError.OperationAborted or 
                SocketError.ConnectionAborted or
                SocketError.Shutdown)
            {
                Close();
            }
            catch (ObjectDisposedException)
            {
                Close();
            }
            catch (OperationCanceledException)
            {
                Close();
            }
            catch (Exception ex) when (ex.Message.Contains("canceled") || ex.Message.Contains("aborted"))
            {
                Close();
            }
            catch (Exception ex)
            {
                Logger.LogPrint($"[Connection] Error ending send: {ex.Message}");
                Close();
            }
        }

        public void Close()
        {
            if (Interlocked.Exchange(ref _closed, 1) == 1)
                return;

            try
            {
                this.IsOpen = false;

                string ip = this.IP; 
                ConnectionLimiter.OnClientDisconnect(ip);
                IPConnectionLimiter.RemoveConnection(ip);

                if (Socket != null)
                {
                    try
                    {
                        if (Socket.Connected)
                        {
                            Socket.Shutdown(SocketShutdown.Both);
                        }
                    }
                    catch
                    {
                        
                    }

                    try
                    {
                        Socket.Close();
                    }
                    catch
                    {
                    }

                    Socket = null;
                }

                if (Memory != null)
                {
                    Memory.Dispose();
                    Memory = null;
                }

                if (MessageManager?.HomeMode?.Avatar != null)
                {
                    Sessions.Remove(MessageManager.HomeMode.Avatar.AccountId);

                    if (MessageManager.HomeMode.Avatar.BattleId > 0)
                    {
                        BattleMode battle = Battles.Get(MessageManager.HomeMode.Avatar.BattleId);
                        if (battle != null)
                        {
                            BattlePlayer player = battle.GetPlayerBySessionId(UdpSessionId);
                            if (player != null)
                            {
                                Character c = battle.GetGameObjectManager().GetCharacterByPlayer(player);
                                player.Bot = 1;
                                c?.SetBot(1);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Connection] Fatal error during close: {ex.Message}");
            }
        }
    }
}