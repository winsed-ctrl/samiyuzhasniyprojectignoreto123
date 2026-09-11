namespace GromCore.Laser.Server.Settings
{
    using Newtonsoft.Json;

    public class Configuration
    {
        public static Configuration Instance;

        [JsonProperty("udp_host")] public readonly string UdpHost;
        [JsonProperty("udp_port")] public readonly int UdpPort;
        [JsonProperty("udp_ports")] public readonly List<int> UdpPorts;
        [JsonProperty("tcp_port")] public readonly int TcpPort;
        [JsonProperty("bot_token")] public readonly string BotToken;
        [JsonProperty("bot_enabled")] public readonly bool BotEnabled;

        [JsonProperty("database_username")] public readonly string DatabaseUsername;
        [JsonProperty("database_password")] public readonly string DatabasePassword;
        [JsonProperty("database_name")] public readonly string DatabaseName;
        [JsonProperty("database_ip")] public readonly string DatabaseIP;

        [JsonProperty("PatcherEnabled")] public readonly bool PatcherEnabled;
        [JsonProperty("PatcherFolder")] public readonly string PatcherFolder;
        [JsonProperty("PatcherIP")] public readonly string PatcherIP;

        [JsonProperty("update_sha")] public readonly string UpdateSha;
        [JsonProperty("ContentUrl")] public readonly string ContentUrl;
        [JsonProperty("Fingerprint")] public readonly string Fingerprint;

        [JsonProperty("MessageLogger")] public readonly bool MsgLogger;
        [JsonProperty("MatchmakingTimer")] public readonly int MatchmakingTimer;

        [JsonProperty("ServerVersion")] public readonly string ServerVersion;
        [JsonProperty("ServerVersionRustore")] public readonly string ServerVersionRustore;
        [JsonProperty("ServerVersionApple")] public readonly string ServerVersionApple;

        [JsonProperty("IsDevServer")] public readonly bool IsDevServer;
        [JsonProperty("IsLocalServer")] public readonly bool IsLocalServer;
        [JsonProperty("TimeUntilRestart")] public readonly int TimeUntilRestart;
        public static Configuration LoadFromFile(string filename)
        {
            return JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filename));
        }
    }
}
