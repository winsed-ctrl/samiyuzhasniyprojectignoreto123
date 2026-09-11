namespace GromCore.Laser.Server.Networking
{
    using Masuda.Net;
    using GromCore.Laser.Logic.Message.Account;
    using GromCore.Laser.Logic.Message.Team.Stream;
    using GromCore.Laser.Logic.Team.Stream;
    using GromCore.Laser.Server.Networking.Session;
    using System.Threading;
    using System.Threading.Tasks;

    public static class Connections
    {
        public static int Count => ActiveConnections.Count;
        private static List<Connection> ActiveConnections;
        private static Timer _updateTimer;
        private static CancellationTokenSource _cancellationTokenSource;
        private static long _secondsGone;

        public static void Init()
        {
            ActiveConnections = new List<Connection>();
            _cancellationTokenSource = new CancellationTokenSource();

            _updateTimer = new Timer(async _ =>
            {
                var connections = ActiveConnections.ToList(); 

                foreach (Connection connection in connections)
                {
                    if (connection.Avatar != null && !Sessions.IsSessionActive(connection.Avatar.AccountId))
                    {
                        connection.Close();
                        ActiveConnections.Remove(connection);
                    }
                    else if (!connection.MessageManager.IsAlive())
                    {
                        if (connection.MessageManager.HomeMode != null)
                        {
                            Sessions.Remove(connection.Avatar.AccountId);
                        }
                        connection.Close();
                        ActiveConnections.Remove(connection);
                    }
                }

                _secondsGone++;
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public static void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _updateTimer?.Dispose();
        }

        public static void AddConnection(Connection connection)
        {
            ActiveConnections.Add(connection);
        }
    }
}