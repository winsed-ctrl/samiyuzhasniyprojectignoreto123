namespace GromCore.Laser.Server.Logic.Game
{
    using System.Data;
    using System.Text;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Server.Database.Models;
    using Telegram.Bot.Types;

    public static class Leaderboards
    {
        private static List<Account> Accounts;
        private static List<Account> RankedSoloAccounts;
        private static List<Account> RankedTeamAccounts;
        private static List<Alliance> Alliances;
        private static Thread Thread;
        private static Dictionary<int, List<Account>> Brawlers;

        public static void Init()
        {
            Accounts = new List<Account>();
            RankedSoloAccounts = new List<Account>();
            RankedTeamAccounts = new List<Account>();
            Alliances = new List<Alliance>();
            Brawlers = new Dictionary<int, List<Account>>();

            Thread = new Thread(Update);
            Thread.Start();
        }

        public static Account[] GetAvatarRankingList()
        {
            return Accounts.ToArray();
        }

        public static Account[] GetRankedSoloAccountsList()
        {
            return RankedSoloAccounts.ToArray();
        }
        public static Account[] GetRankedTeamAccountsList()
        {
            return RankedTeamAccounts.ToArray();
        }

        public static Dictionary<int, List<Account>> GetBrawlersRankingList()
        {
            return Brawlers;
        }

        public static Alliance[] GetAllianceRankingList(string reg = null)
        {
            if (String.IsNullOrEmpty(reg))
                return Alliances.ToArray();
            else
            {
                List<Alliance> temp = new();
                foreach (Alliance tmp in Alliances)
                {
                    if (tmp.Country.ToUpper() == reg.ToUpper()) temp.Add(tmp);
                }
                return temp.ToArray();
            }
        }

        private static void Update()
        {
            int updateCounter = 0;
            while (true)
            {
                try
                {
                    Accounts = Database.Accounts.GetRankingList();
                    RankedSoloAccounts = Database.Accounts.GetSoloRankingList();
                    RankedTeamAccounts = Database.Accounts.GetSoloRankingList(); //мне лень это фиксть бтв
                    Alliances = Database.Alliances.GetRankingList();
                    if (updateCounter == 0)
                    {
                        Brawlers = Database.Accounts.GetBrawlersRankingList();
                    }
                    updateCounter = (updateCounter + 1) % 5;
                }
                catch (Exception ex)
                {
                    Logger.Error($"[Leaderboards.Update] Error: {ex.Message}");
                }
                
                Thread.Sleep(60000); // 1 минута
            }
        }
    }
}
