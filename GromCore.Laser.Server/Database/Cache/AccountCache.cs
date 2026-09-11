namespace GromCore.Laser.Server.Database.Cache
{
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Logic.Game;
    using System.Collections.Concurrent;

    public static class AccountCache
    {
        private static ConcurrentDictionary<long, Account> CachedAccounts;
        private static Thread _thread;

        public static int Count
        {
            get
            {
                return CachedAccounts?.Count ?? 0;
            }
        }

        public static void Init()
        {
            CachedAccounts = new ConcurrentDictionary<long, Account>();
            _thread = new Thread(Update);
            _thread.Start();
        }

        public static bool Started = true;

        private static void Update()
        {
            while (Started)
            {
                // SaveAll();
                // AccountCache.SaveAll();
                // SaveStatus();
                Thread.Sleep(3000);
            }
        }

        public static void SaveAll()
        {
            try
            {
                if (CachedAccounts == null) return;
                
                int saved = 0;
                int errors = 0;
                
                foreach (var account in CachedAccounts.Values)
                {
                    try
                    {
                        Accounts.SaveImmediate(account); // Синхронное сохранение при завершении
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        Logger.Error($"Error saving account {account?.AccountId}: {ex.Message}");
                    }
                }
                
                Logger.Print($"[AccountCache.SaveAll] Saved {saved} accounts, errors: {errors}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[AccountCache.SaveAll] Critical error: {ex.Message}");
            }
        }

        // public static void SaveStatus()
        // {
        //     try
        //     {
        //         Accounts.SaveStatus();
        //     }
        //     catch (Exception) { }
        // }

        public static bool IsAccountCached(long id)
        {
            return CachedAccounts?.ContainsKey(id) ?? false;
        }

        public static Account GetAccount(long id)
        {
            if (CachedAccounts == null) return null;
            CachedAccounts.TryGetValue(id, out var account);
            return account;
        }

        public static void Cache(Account account)
        {
            if (account == null || CachedAccounts == null) return;
            CachedAccounts.AddOrUpdate(account.AccountId, account, (key, oldValue) => account);
        }
    }
}