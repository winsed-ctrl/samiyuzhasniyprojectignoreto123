namespace GromCore.Laser.Logic.Listener
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Ranked;

    public interface LogicServerListener
    {
        public static LogicServerListener Instance;

        ClientAvatar GetAvatar(long id);
        LogicGameListener GetGameListener(long id);
        HomeMode GetHomeMode(long id);
        bool IsPlayerOnline(long id);
        int GetOnlinePlayersCount();
        void UpdateTeam(long id);
        List<Home.Items.Notification> GetAllianceMail(long id);
        string GetAllianceName(long id);
        EventData[] GetEvents();
        void StartMatchBattle(List<RankedMatchPlayer> entries, EventData edata, int Location = -1, int id = 0, int round = 0);
        RankedMatch GetRankedMatch(long id);
        int GetIntForBMap();
        PlayerMap GetWinnerMap();
        List<PlayerMap> GetCandidats();
        bool IsDev();
        List<int> GetIntListFromField(string field);
    }
}
