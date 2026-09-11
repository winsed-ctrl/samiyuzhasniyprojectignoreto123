namespace GromCore.Laser.Server.Database.Models
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Home;

    public class Account
    {
        [JsonProperty] public long AccountId;
        [JsonProperty] public string PassToken;

        [JsonProperty] public ClientHome Home;
        [JsonProperty] public ClientAvatar Avatar;

        public Account()
        {
            Home = new ClientHome();
            Avatar = new ClientAvatar();
        }
    }
}
