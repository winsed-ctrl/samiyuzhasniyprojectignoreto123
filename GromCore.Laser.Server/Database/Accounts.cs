namespace GromCore.Laser.Server.Database
{
    using System.Data;
    using Npgsql;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Settings;
    using GromCore.Laser.Server.Utils;
    using GromCore.Laser.Logic.Data;
    using System.Threading;
    using GromCore.Laser.Server.Networking.Session;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    public static class Accounts
    {
        private static long AvatarIdCounter;
        private static string ConnectionString;
        private static ConcurrentQueue<Account> SaveQueue;
        private static Thread SaveThread;
        private static bool SaveThreadRunning = true;

        public static void Init(string user, string password, string ip)
        {
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder();
            builder.Host = ip;
            builder.Username = user;
            builder.Password = password;
            builder.Database = Configuration.Instance.DatabaseName;
            builder.Encoding = "UTF8";
            builder.Timeout = 30;
            builder.Pooling = true;
            builder.ConnectionLifetime = 300;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            ConnectionString = builder.ToString();

            AccountCache.Init();

            AvatarIdCounter = GetMaxAvatarId();
            
            // Инициализация асинхронной очереди сохранения
            SaveQueue = new ConcurrentQueue<Account>();
            SaveThreadRunning = true;
            SaveThread = new Thread(SaveWorker);
            SaveThread.IsBackground = true;
            SaveThread.Start();
        }
        
        public static void StopSaveThread()
        {
            SaveThreadRunning = false;
            if (SaveThread != null && SaveThread.IsAlive)
            {
                // Ждем завершения обработки очереди (максимум 5 секунд)
                if (!SaveThread.Join(5000))
                {
                    Logger.Error("[Accounts] Save thread did not stop in time");
                }
            }
        }
        
        private static void SaveWorker()
        {
            while (SaveThreadRunning)
            {
                try
                {
                    int processed = 0;
                    const int batchSize = 10; // Обрабатываем по 10 за раз
                    
                    while (SaveQueue.TryDequeue(out Account account) && processed < batchSize)
                    {
                        try
                        {
                            SaveSync(account);
                            processed++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[SaveWorker] Error saving account {account?.AccountId}: {ex.Message}");
                        }
                    }
                    
                    if (processed == 0)
                    {
                        Thread.Sleep(100); // Если очередь пуста, ждем 100мс
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[SaveWorker] Critical error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }
        
        private static bool SaveSync(Account account)
        {
            if (account == null) return false;

            try
            {
                string json = JsonConvert.SerializeObject(account);

                using (var Connection = new NpgsqlConnection(ConnectionString))
                {
                    Connection.Open();
                    NpgsqlCommand command = new NpgsqlCommand(
                        $"UPDATE accounts SET \"Trophies\"={account.Avatar.Trophies}, \"Data\"=@data WHERE \"Id\" = '{account.AccountId}'", 
                        Connection);
                    command.CommandTimeout = 10;
                    var param = command.Parameters.Add("@data", NpgsqlTypes.NpgsqlDbType.Jsonb);
                    param.Value = json;
                    command.ExecuteNonQuery();
                    Connection.Close();
                }
                
                AccountCache.Cache(account);
                return true;
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[Accounts.SaveSync] PostgreSQL error for account {account.AccountId}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Accounts.SaveSync] Error for account {account.AccountId}: {ex.Message}");
                return false;
            }
        }

        public static long GetMaxAvatarId()
        {
            using (var Connection = new NpgsqlConnection(ConnectionString))
            {
                Connection.Open();
                NpgsqlCommand command = new NpgsqlCommand("SELECT coalesce(MAX(\"Id\"), 0) FROM accounts", Connection);

                long result = Convert.ToInt64(command.ExecuteScalar());
                Connection.Close();
                return result;
            }
        }

        public static Account Create()
        {
            Account account = new Account();
            account.AccountId = ++AvatarIdCounter;
            account.PassToken = Helpers.GenerateToken(account.AccountId);

            account.Avatar.AccountId = account.AccountId;
            account.Avatar.PassToken = account.PassToken;

            account.Home.HomeId = account.AccountId;

            Hero hero = new Hero(16000000);
            account.Avatar.Heroes.Add(hero);
            //hero.SelectedGadgetId=
            string json = JsonConvert.SerializeObject(account);

            using (var Connection = new NpgsqlConnection(ConnectionString))
            {
                Connection.Open();
                NpgsqlCommand command = new NpgsqlCommand($"INSERT INTO accounts (\"Id\", \"Trophies\", \"Data\") VALUES ({(long)account.AccountId}, {account.Avatar.Trophies}, @data)", Connection);
                var param = command.Parameters.Add("@data", NpgsqlTypes.NpgsqlDbType.Jsonb);
                param.Value = json;
                command.ExecuteNonQuery();
                Connection.Close();

                AccountCache.Cache(account);

                return account;
            }
        }
        public static void Delete(int id)
        {
            using (var Connection = new NpgsqlConnection(ConnectionString))
            {
                Connection.Open();
                NpgsqlCommand command = new NpgsqlCommand($"DELETE FROM accounts WHERE \"Id\" = {id}", Connection);
                command.ExecuteNonQuery();
                Connection.Close();
            }
        }

        public static void Save(Account account)
        {
            if (account == null) return;
            
            // Добавляем в очередь для асинхронного сохранения
            if (SaveQueue != null && SaveQueue.Count < 10000) // Ограничение размера очереди
            {
                SaveQueue.Enqueue(account);
                // Обновляем кэш сразу, чтобы данные были доступны
                AccountCache.Cache(account);
            }
            else
            {
                // Если очередь переполнена, сохраняем синхронно (но это не должно происходить)
                Logger.Error($"[Accounts.Save] Queue full, saving synchronously for account {account.AccountId}");
                SaveSync(account);
            }
        }
        
        // Синхронный метод для критических случаев (например, при завершении работы)
        public static void SaveImmediate(Account account)
        {
            SaveSync(account);
        }

        // Security-sensitive operations must know whether PostgreSQL actually accepted the
        // update. SaveImmediate is intentionally kept for existing fire-and-forget callers.
        public static bool TrySaveImmediate(Account account)
        {
            return SaveSync(account);
        }

        public static Account Load(long id)
        {
            if (AccountCache.IsAccountCached(id))
            {
                return AccountCache.GetAccount(id);
            }

            try
            {
                using (var Connection = new NpgsqlConnection(ConnectionString))
                {
                    Connection.Open();
                    NpgsqlCommand command = new NpgsqlCommand($"SELECT * FROM accounts WHERE \"Id\" = '{id}'", Connection);
                    command.CommandTimeout = 10; // Таймаут 10 секунд
                    NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Account account = JsonConvert.DeserializeObject<Account>((string)reader["Data"]);
                        AccountCache.Cache(account);
                        Connection.Close();
                        return account;
                    }
                    Connection.Close();
                    return null;
                }
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[Accounts.Load] PostgreSQL error for account {id}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Accounts.Load] Error for account {id}: {ex.Message}");
                return null;
            }
        }

        public static bool IsTooManyAccountsFromIP(string ip, int maxAccounts = 7)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    // Прямой SQL запрос вместо загрузки всех аккаунтов
                    using (var cmd = new NpgsqlCommand(
                        "SELECT COUNT(*) FROM accounts WHERE \"Data\"::jsonb->'Avatar'->>'IP' = @ip", 
                        connection))
                    {
                        cmd.Parameters.AddWithValue("@ip", ip);
                        cmd.CommandTimeout = 5; // Таймаут 5 секунд
                        long count = Convert.ToInt64(cmd.ExecuteScalar());
                        connection.Close();
                        return count >= maxAccounts;
                    }
                }
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[IsTooManyAccountsFromIP] DB error: {ex.Message}");
                return false; // В случае ошибки разрешаем подключение
            }
            catch (Exception ex)
            {
                Logger.Error($"[IsTooManyAccountsFromIP] Error: {ex.Message}");
                return false;
            }
        }
        public static List<Account> GetAll()
        {
            var accounts = new List<Account>();

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT \"Data\" FROM accounts LIMIT 50000", connection))
                    {
                        cmd.CommandTimeout = 30; // Таймаут 30 секунд для большого запроса
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string jsonData = reader.GetString("Data");
                                try
                                {
                                    Account account = JsonConvert.DeserializeObject<Account>(jsonData);
                                    if (account != null)
                                    {
                                        accounts.Add(account);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"ne udalos deserealEzirovat object!: {ex.Message}");
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[Accounts.GetAll] PostgreSQL error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Accounts.GetAll] Error: {ex.Message}");
            }

            return accounts;
        }

        public static List<Account> GetDebugAccounts()
        {
            var accounts = new List<Account>();

            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                connection.Open();
                using var command = new NpgsqlCommand(
                    "SELECT \"Data\" FROM accounts " +
                    "WHERE \"Data\" @> '{\"Avatar\":{\"IsDebugAccount\":true}}'::jsonb " +
                    "OR \"Data\" @> '{\"Avatar\":{\"InfiniteAmmo\":true}}'::jsonb " +
                    "OR \"Data\" @> '{\"Avatar\":{\"InfiniteUltimate\":true}}'::jsonb " +
                    "ORDER BY \"Id\"",
                    connection);
                command.CommandTimeout = 15;

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Account account = JsonConvert.DeserializeObject<Account>(reader.GetString(0));
                    if (account != null)
                        accounts.Add(account);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"[Accounts.GetDebugAccounts] Error: {ex.Message}");
            }

            return accounts;
        }

        public static Dictionary<int, List<Account>> GetBrawlersRankingList()
        {
            #region GetGlobal

            var list = new Dictionary<int, List<Account>>();
            var allianceCache = new Dictionary<long, string>(); // Кэш для имен альянсов
            
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // Сначала загружаем все альянсы в кэш одним запросом
                    using (var allianceCmd = new NpgsqlCommand(
                        "SELECT \"Id\", \"Name\" FROM alliances WHERE \"Id\" > 0", 
                        connection))
                    {
                        allianceCmd.CommandTimeout = 10;
                        using (var allianceReader = allianceCmd.ExecuteReader())
                        {
                            while (allianceReader.Read())
                            {
                                long id = allianceReader.GetInt64(0);
                                string name = allianceReader.GetString(1);
                                allianceCache[id] = name;
                            }
                        }
                    }

                    // Теперь загружаем аккаунты с LIMIT для производительности
                    using (var cmd = new NpgsqlCommand(
                        "SELECT \"Data\" FROM accounts ORDER BY \"Trophies\" DESC LIMIT 10000",
                        connection))
                    {
                        cmd.CommandTimeout = 15; // Увеличенный таймаут для большого запроса
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            try
                            {
                                var account = JsonConvert.DeserializeObject<Account>((string)reader["Data"]);
                                if (account == null) continue;
                                
                                long allianceId = account.Avatar.AllianceId;
                                if (allianceId > 0 && allianceCache.ContainsKey(allianceId))
                                {
                                    account.Avatar.AllianceName = allianceCache[allianceId];
                                }
                                
                                foreach (Hero hero in account.Avatar.Heroes)
                                {
                                    if (list.ContainsKey(hero.CharacterId))
                                    {
                                        list[hero.CharacterId].Add(account);
                                    }
                                    else
                                    {
                                        List<Account> a = new();
                                        a.Add(account);
                                        list.Add(hero.CharacterId, a);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.Error($"LB Error: {e.Message}");
                            }
                        }
                    }

                    connection.Close();
                }

                return list;
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[GetBrawlersRankingList] PostgreSQL error: {ex.Message}");
                return list;
            }
            catch (Exception ex)
            {
                Logger.Error($"[GetBrawlersRankingList] Error: {ex.Message}");
                return list;
            }

            #endregion
        }
        public static List<Account> GetRankingList()
        {
            #region GetGlobal

            var list = new List<Account>();
            //return list;
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand($"SELECT * FROM accounts ORDER BY \"Trophies\" DESC LIMIT 200",
                    // using (var cmd = new NpgsqlCommand($"SELECT * FROM accounts LIMIT 200",
                    connection))
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                            list.Add(JsonConvert.DeserializeObject<Account>((string)reader["Data"]));
                    }

                    connection.Close();
                }

                return list;
            }
            catch (Exception exception)
            {
                return list;
            }

            #endregion
        }

        public static List<Account> GetSoloRankingList()
        {
            var list = new List<Account>();
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                connection.Open();
                using var command = new NpgsqlCommand(
                    "SELECT \"Data\" FROM accounts ORDER BY CAST(\"Data\"->'Home'->>'RankedSoloProgress' AS INTEGER) DESC NULLS LAST LIMIT 200", connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        var account = JsonConvert.DeserializeObject<Account>(reader.GetString("Data"));
                        if (account != null)
                            list.Add(account);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Solo LB Error: " + ex.Message);
                    }
                }
            }
            catch (Exception) { }
            return list.OrderByDescending(x => x.Home.RankedSoloProgress).ToList();
        }

        public static List<Account> GetTrioRankingList()
        {
            var list = new List<Account>();
            try
            {
                using var connection = new NpgsqlConnection(ConnectionString);
                connection.Open();
                using var command = new NpgsqlCommand(
                    "SELECT \"Data\" FROM accounts ORDER BY CAST(\"Data\"->'Home'->>'RankedTrioProgress' AS INTEGER) DESC NULLS LAST LIMIT 200", connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        var account = JsonConvert.DeserializeObject<Account>(reader.GetString("Data"));
                        if (account != null)
                            list.Add(account);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Trio LB Error: " + ex.Message);
                    }
                }
            }
            catch (Exception) { }
            return list.OrderByDescending(x => x.Home.RankedTrioProgress).ToList();
        }

        public static List<Account> GetBrawlerRankingList(int HeroDataId)
        {
            #region GetGlobal
            var list = new List<Account>();
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();

                using (var cmd = new NpgsqlCommand($"SELECT * FROM accounts WHERE \"Trophies\" >= 1000",
                    connection))
                {
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        list.Add(JsonConvert.DeserializeObject<Account>((string)reader["Data"]));
                        if (!list.Last().Avatar.HasHero(HeroDataId)) list.Remove(list.Last());
                    }
                }

                connection.Close();
            }
            list.Sort(delegate (Account a, Account b)
            {
                return b.Avatar.GetHero(HeroDataId).Trophies - a.Avatar.GetHero(HeroDataId).Trophies;
            });
            return list;


            #endregion


        }
    }
}
