namespace GromCore.Laser.Server.Database.Models
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Home;

    public class AccountLinkModel
    {
        [JsonProperty] public int AccountId;
        [JsonProperty] public string AccountLogin;
        [JsonProperty] public string AccountEmail;
        [JsonProperty] public string AccountPassword;
        [JsonProperty] public bool AccountLinkedToTelegram;
        [JsonProperty] public long? AccountTelegramLinkedId;
        [JsonProperty] public string LinkToken;
        [JsonProperty] public int VerificationCode;
        [JsonProperty] public DateTime VerificationCodeAlive;
        [JsonProperty] public string TelegramLinkCode; 
        [JsonProperty] public DateTime TelegramLinkCodeExpire;
    }
}
