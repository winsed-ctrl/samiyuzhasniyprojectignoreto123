namespace GromCore.Laser.Server
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Logic;
    using GromCore.Laser.Server.Logic.Game;
    using GromCore.Laser.Server.Message;
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Networking.Session;
    using GromCore.Laser.Server.Settings;
    using GromCore.Laser.Server.Fingerprint;
    using GromCore.Laser.Server.Utils;

    internal static class Resources
    {
        /// <summary>
        /// Initializes the databases
        /// </summary>
        public static void InitDatabase()
        {
            Accounts.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword, Configuration.Instance.DatabaseIP);
            Alliances.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword, Configuration.Instance.DatabaseIP);
            RuntimeControlManager.Initialize();
            //AccountLinkSystem.Init(Configuration.Instance.DatabaseUsername, Configuration.Instance.DatabasePassword, Configuration.Instance.DatabaseIP);
            if (Configuration.Instance.BotEnabled) TelegramBot.Start();
        }

        /// <summary>
        /// Initializes the logic part of server
        /// </summary>
        public static void InitLogic()
        {
            Fingerprint.Fingerprint.Load();
            DataTables.Load();
            Events.Init();
            Sessions.Init();
            Leaderboards.Init();
            Battles.Init();
            Matchmaking.Init();
            Teams.Init();
            ProfanityManager.Initialize("Assets/profanity.txt");
            LocalizationHelper.Load();
            EventDelayTicks.Init();
            RankedMatchRegulator.Init();
            PlayerCustomMapsHandler.EnsureInitialized();
        }

        /// <summary>
        /// Initializes the network part of server
        /// </summary>
        public static void InitNetwork()
        {
            Processor.Init();
            Connections.Init();
            LogicServerListener.Instance = new ServerListener();

            foreach (int port in Configuration.Instance.UdpPorts)
            {
                UPD.UdpServers[port] = new UDPGateway();
                UPD.UdpServers[port].Init("0.0.0.0", port);
            }
            TCPGateway.Init("0.0.0.0", Configuration.Instance.TcpPort);
        }
    }
}
