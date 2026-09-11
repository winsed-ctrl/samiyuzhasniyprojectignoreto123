namespace GromCore.Laser.Server.Settings
{
    public class ServerConfiguration
    {
        public ServerPropertyData ServerProperty { get; set; } = new ServerPropertyData();
        public DatabasePropertyData DatabaseProperty { get; set; } = new DatabasePropertyData();
        public DebugData Debug { get; set; } = new DebugData();
    }

    public class ServerPropertyData
    {
        public string UdpHost { get; set; } = "2.26.53.125";
        public int UdpPort { get; set; } = 9339;
        public List<int> UdpPorts { get; set; } = new List<int>();
        public int TcpPort { get; set; } = 9339;
        public bool BotEnabled { get; set; } = true;
        public string BotToken { get; set; } = "";
        public string ServerVersion { get; set; } = "1.0";
        public string ServerVersionRustore { get; set; } = "1.0";
        public string ServerVersionApple { get; set; } = "1.0_ipa";
        public bool IsCustom { get; set; } = false;
        public bool PatcherEnabled { get; set; } = false;
        public string PatcherFolder { get; set; } = "/content/Patch/";
        public string PatcherIP { get; set; } = "http://127.0.0.1:8080/";
    }

    public class DatabasePropertyData
    {
        public string DatabaseIp { get; set; } = "localhost";
        public string DatabaseUsername { get; set; } = "melon001";
        public string DatabasePassword { get; set; } = "melon001";
        public string DatabaseName { get; set; } = "melon001";
    }

    public class DebugData
    {
        public bool IsDevServer { get; set; } = false;
        public bool MessageLogger { get; set; } = true;
    }
}