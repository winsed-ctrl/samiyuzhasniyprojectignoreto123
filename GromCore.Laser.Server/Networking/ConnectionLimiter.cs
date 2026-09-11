using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace GromCore.Laser.Server.Networking
{
    public static class ConnectionLimiter
    {
        private static readonly ConcurrentDictionary<string, List<DateTime>> ConnectionAttempts = new();
        private static readonly TimeSpan TimeFrame = TimeSpan.FromSeconds(10);
        private static readonly int MaxConnectionsPerTimeFrame = 50;
        private static readonly HashSet<string> BlockedIPs = new();
        private static readonly object LockObj = new();

        public static void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(10000);
                    CleanOldAttempts();
                }
            });
        }

        public static bool IsBlocked(string ip)
        {
            return BlockedIPs.Contains(ip);
        }

        public static void BlockIP(string ip, TimeSpan? duration = null)
        {
            if (string.IsNullOrEmpty(ip))
                return;

            lock (LockObj)
            {
                if (!BlockedIPs.Contains(ip))
                {
                    BlockedIPs.Add(ip);
                    Logger.LogPrint($"[ConnectionLimiter] ? IP {ip} заблокирован.");
                }
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(duration ?? TimeSpan.FromMinutes(5));
                UnblockIP(ip);
            });
        }

        public static void UnblockIP(string ip)
        {
            lock (LockObj)
            {
                if (BlockedIPs.Contains(ip))
                    BlockedIPs.Remove(ip);

                if (ConnectionAttempts.ContainsKey(ip))
                    ConnectionAttempts.TryRemove(ip, out _);
            }
        }

        public static bool AllowConnection(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return false;

            if (IsBlocked(ip))
            {
                return false;
            }

            var now = DateTime.Now;

            if (!ConnectionAttempts.TryGetValue(ip, out var attempts))
            {
                attempts = new List<DateTime>();
                ConnectionAttempts[ip] = attempts;
            }
            attempts.RemoveAll(t => t < now - TimeFrame);
            if (attempts.Count >= MaxConnectionsPerTimeFrame)
            {
                BlockIP(ip); 
                return false;
            }

            attempts.Add(now);
            return true;
        }

        public static void OnClientDisconnect(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return;

            if (ConnectionAttempts.TryGetValue(ip, out var attempts))
            {
                lock (attempts)
                {
                    if (attempts.Count > 0)
                        attempts.RemoveAt(attempts.Count - 1); 
                }
            }
        }

        private static void CleanOldAttempts()
        {
            var now = DateTime.Now;

            foreach (var key in ConnectionAttempts.Keys.ToList())
            {
                if (ConnectionAttempts.TryGetValue(key, out var attempts))
                {
                    lock (attempts)
                    {
                        attempts.RemoveAll(t => t < now - TimeFrame);

                        if (attempts.Count == 0)
                            ConnectionAttempts.TryRemove(key, out _);
                    }
                }
            }
        }
    }
}