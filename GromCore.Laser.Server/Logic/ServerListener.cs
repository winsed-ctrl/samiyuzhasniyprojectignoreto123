namespace GromCore.Laser.Server.Logic
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Ranked;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Logic.Game;
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Networking.Session;
    using GromCore.Laser.Server.Settings;

    public class ServerListener : LogicServerListener
    {
        public ClientAvatar GetAvatar(long id)
        {
            return Accounts.Load(id).Avatar;
        }

        public LogicGameListener GetGameListener(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).GameListener;
            }
            return null;
        }

        public HomeMode GetHomeMode(long id)
        {
            if (Sessions.IsSessionActive(id))
            {
                return Sessions.GetSession(id).Home;
            }
            return null;
        }

        public bool IsPlayerOnline(long id)
        {
            return Sessions.IsSessionActive(id);
        }

        public int GetOnlinePlayersCount()
        {
            return Sessions.Count;
        }

        public void UpdateTeam(long id)
        {
            Teams.Get(id)?.TeamUpdated();
        }
        public List<Notification> GetAllianceMail(long id)
        {
            if (Alliances.Load(id) != null && Alliances.Load(id).AllianceMail != null) return Alliances.Load(id).AllianceMail;
            return null;
        }
        public string GetAllianceName(long id) => Alliances.Load(id) != null ? Alliances.Load(id).Name : "";
        public EventData[] GetEvents() => Events.GetEvents();
        public void StartMatchBattle(List<RankedMatchPlayer> entries, EventData edata, int Location = -1, int id = 0, int round = 0)
        {
            List<MatchmakingEntry> list = new();

            foreach(RankedMatchPlayer ranked in entries)
            {
                Connection conn = null;
                if (Sessions.IsSessionActive(ranked.AccountId))
                    conn = Sessions.GetSession(ranked.AccountId).Connection;
                MatchmakingEntry e = new(conn);
                e.PrefferedTeam = ranked.TeamIndex;
                e.CharacterId = ranked.Character;
                list.Add(e);
            }
            RankedMatchRegulator.StartMatchBattle(list, edata, Location, id, round);
        }
        public RankedMatch GetRankedMatch(long id) => RankedMatchRegulator.Get(id);
        public int GetIntForBMap() => PlayerCustomMapsHandler.GetRandomMapWithGameModeVariation(PlayerCustomMapsHandler.GameMode);
        public PlayerMap GetWinnerMap() => PlayerCustomMapsHandler.Winner;
        public List<PlayerMap> GetCandidats() => PlayerCustomMapsHandler.GetMaps();
        public bool IsDev() => Configuration.Instance.IsLocalServer;
        public List<int> GetIntListFromField(string field) => ContentDeliveryNetworkManager.GetIntListFromField(field);
    }
}
