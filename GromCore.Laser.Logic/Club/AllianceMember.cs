namespace GromCore.Laser.Logic.Club
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class AllianceMember
    {
        [JsonProperty("displayData")] public PlayerDisplayData DisplayData { get; set; }
        [JsonProperty("accountId")] public long AccountId { get; set; }
        [JsonProperty("trophies")] public int Trophies { get; set; }
        [JsonProperty("role")] public AllianceRole Role { get; set; }
        [JsonProperty("DoNotDisturb")] public bool DoNotDisturb { get; set; }
        [JsonProperty("pbankwins")] public int PiggyBankWins { get; set; }
        [JsonProperty("hasactive")] public bool HasActivePiggyBank { get; set; }

        [JsonIgnore]
        public ClientAvatar Avatar
        {
            get
            {
                return LogicServerListener.Instance.GetAvatar(AccountId);
            }
        }

        [JsonIgnore]
        public HomeMode HomeMode
        {
            get
            {
                return LogicServerListener.Instance.GetHomeMode(AccountId);
            }
        }

        [JsonIgnore]
        public bool IsOnline
        {
            get
            {
                return LogicServerListener.Instance.IsPlayerOnline(AccountId);
            }
        }

        public AllianceMember()
        {
            // For json...
        }

        public AllianceMember(ClientAvatar avatar, HomeMode home)
        {
            DisplayData = new PlayerDisplayData(home.Home.ThumbnailId, home.Home.NameColorId, avatar.Name, home.Home.HasPremiumPass, home.Home.HasPremiumPassPlus);
            AccountId = avatar.AccountId;
            Trophies = avatar.Trophies;
            Role = avatar.AllianceRole;
            if (avatar.DoNotDisturb == 1) DoNotDisturb = true;
            else DoNotDisturb = false;
        }

        public void Encode(ByteStream stream)
        {
            ClientAvatar avatar = Avatar;

            stream.WriteLong(AccountId);
            stream.WriteVInt((int)Role);
            stream.WriteVInt(avatar.Trophies);

            stream.WriteVInt(IsOnline ? avatar.PlayerStatus : 0); // PlayerStatus
            stream.WriteVInt(IsOnline ? -1 : (int)(DateTime.UtcNow - avatar.LastOnline).TotalSeconds);

            stream.WriteVInt(0);

            stream.WriteBoolean(true);
            DisplayData.Name = avatar.Name;
            DisplayData.Encode(stream);

            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);

            stream.WriteVInt(0);

            if (HasActivePiggyBank)
            {
                stream.WriteVInt(1);
                stream.WriteVInt(PiggyBankWins); 
                stream.WriteVInt(avatar.HomeMode.Home.PiggyBankTickets);
                stream.WriteVInt(1488);
            }
            else
                stream.WriteVInt(0);
        }
    }
}
