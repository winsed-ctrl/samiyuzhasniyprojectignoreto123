namespace GromCore.Laser.Server.Database
{
    using Npgsql;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Settings;
    using System.Collections.Concurrent;
    using System.Threading;

    public static class Alliances
    {
        private static long AllianceIdCounter;
        private static string ConnectionString;
        private static ConcurrentQueue<Alliance> SaveQueue;
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

            AllianceCache.Init();

            AllianceIdCounter = GetMaxAllianceId();
            
            // Инициализация асинхронной очереди сохранения
            SaveQueue = new ConcurrentQueue<Alliance>();
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
                    Logger.Error("[Alliances] Save thread did not stop in time");
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
                    const int batchSize = 10;
                    
                    while (SaveQueue.TryDequeue(out Alliance alliance) && processed < batchSize)
                    {
                        try
                        {
                            SaveSync(alliance);
                            processed++;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[SaveWorker] Error saving alliance {alliance?.Id}: {ex.Message}");
                        }
                    }
                    
                    if (processed == 0)
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"[SaveWorker] Critical error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }
        
        private static void SaveSync(Alliance alliance)
        {
            if (alliance == null) return;

            try
            {
                string json = JsonConvert.SerializeObject(alliance);

                using (var Connection = new NpgsqlConnection(ConnectionString))
                {
                    Connection.Open();
                    NpgsqlCommand command = new NpgsqlCommand(
                        $"UPDATE alliances SET \"Trophies\"={alliance.Trophies}, \"Data\"=@data WHERE \"Id\" = '{(long)alliance.Id}'", 
                        Connection);
                    command.CommandTimeout = 10;
                    var param = command.Parameters.Add("@data", NpgsqlTypes.NpgsqlDbType.Jsonb);
                    param.Value = json;
                    command.ExecuteNonQuery();
                    Connection.Close();
                }
                
                AllianceCache.Cache(alliance);
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[Alliances.SaveSync] PostgreSQL error for alliance {alliance.Id}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[Alliances.SaveSync] Error for alliance {alliance.Id}: {ex.Message}");
            }
        }

        public static long GetMaxAllianceId()
        {
            using (var Connection = new NpgsqlConnection(ConnectionString))
            {
                Connection.Open();
                NpgsqlCommand command = new NpgsqlCommand("SELECT coalesce(MAX(\"Id\"), 0) FROM alliances", Connection);

                long result = Convert.ToInt64(command.ExecuteScalar());
                Connection.Close();
                return result;
            }
        }

        public static void Create(Alliance alliance)
        {
            if (alliance == null) return;
            alliance.Id = ++AllianceIdCounter;
            string json = JsonConvert.SerializeObject(alliance);

            using (var Connection = new NpgsqlConnection(ConnectionString))
            {
                Connection.Open();
                NpgsqlCommand command = new NpgsqlCommand($"INSERT INTO alliances (\"Id\", \"Name\", \"Trophies\", \"Data\") VALUES ({(long)alliance.Id}, @name, {alliance.Trophies}, @data)", Connection);
                var param = command.Parameters.Add("@data", NpgsqlTypes.NpgsqlDbType.Jsonb);
                param.Value = json;
                command.Parameters?.AddWithValue("@name", alliance.Name);
                command.ExecuteNonQuery();
                Connection.Close();

                AllianceCache.Cache(alliance);
            }
        }

        public static void Save(Alliance alliance)
        {
            if (alliance == null) return;
            
            // Добавляем в очередь для асинхронного сохранения
            if (SaveQueue != null && SaveQueue.Count < 10000)
            {
                SaveQueue.Enqueue(alliance);
                AllianceCache.Cache(alliance);
            }
            else
            {
                Logger.Error($"[Alliances.Save] Queue full, saving synchronously for alliance {alliance.Id}");
                SaveSync(alliance);
            }
        }
        
        // Синхронный метод для критических случаев
        public static void SaveImmediate(Alliance alliance)
        {
            SaveSync(alliance);
        }

        public static Alliance Load(long id)
        {
            if (AllianceCache.IsAllianceCached(id))
            {
                return AllianceCache.GetAlliance(id);
            }

            try
            {
                using (var Connection = new NpgsqlConnection(ConnectionString))
                {
                    Connection.Open();
                    NpgsqlCommand command = new NpgsqlCommand($"SELECT * FROM alliances WHERE \"Id\" = '{id}'", Connection);
                    command.CommandTimeout = 10; // Таймаут 10 секунд
                    NpgsqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        Alliance alliance = JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]);
                        AllianceCache.Cache(alliance);
                        Connection.Close();
                        return alliance;
                    }
                    Connection.Close();
                    return null;
                }
            }
            catch (NpgsqlException ex)
            {
                Logger.Error($"[Alliances.Load] PostgreSQL error for alliance {id}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Alliances.Load] Error for alliance {id}: {ex.Message}");
                return null;
            }
        }

        public static AllianceMember GetMember(long allianceId, long accountId)
        {
            var alliance = Load(allianceId);
            if (alliance == null) return null;

            return alliance.Members.FirstOrDefault(m => m.AccountId == accountId);
        }

        public static List<Alliance> GetAll()
        {
            var alliances = new List<Alliance>();

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Data\" FROM alliances", connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string jsonData = reader.GetString(reader.GetOrdinal("Data"));
                                try
                                {
                                    Alliance alliance = JsonConvert.DeserializeObject<Alliance>(jsonData);
                                    if (alliance != null)
                                    {
                                        alliances.Add(alliance);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"err2: {ex.Message}");
                                }
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"err: {ex.Message}");
            }

            return alliances;
        }


        public static bool Delete(long allianceId)
        {
            try
            {
                var alliance = Load(allianceId);
                if (alliance == null) return false;
                

                foreach (var member in alliance.Members.ToList())
                {
                    var account = Accounts.Load(member.AccountId);
                    if (account != null)
                    {
                        account.Avatar.AllianceId = -1;
                        account.Avatar.AllianceName = "";
                        Accounts.Save(account);
                    }
                }

                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var cmd = new NpgsqlCommand("DELETE FROM alliances WHERE \"Id\" = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", allianceId);
                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected <= 0)
                        {
                            return false;
                        }
                    }
                    using (var cmd = new NpgsqlCommand("DELETE FROM alliance_members WHERE \"AllianceId\" = @id", connection))
                    {
                        cmd.Parameters.AddWithValue("@id", allianceId);
                        cmd.ExecuteNonQuery();
                    }

                    connection.Close();
                }
                AllianceCache.SaveAll();

                Logger.Print($"✅ {allianceId} ");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"err: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }
        public static List<Alliance> GetRankingList()
        {
            #region GetGlobal

            var list = new List<Alliance>();
            //return list;
            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand($"SELECT * FROM alliances ORDER BY \"Trophies\" DESC LIMIT 200",
                        connection))
                    {
                        var reader = cmd.ExecuteReader();

                        while (reader.Read())
                            list.Add(JsonConvert.DeserializeObject<Alliance>((string)reader["Data"]));
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

        public static List<Account> GetPlayersInAlliance(long allianceId)
        {
            var players = new List<Account>();

            if (allianceId <= 0)
                return players;

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Data\" FROM accounts", connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                long accountId = reader.GetInt64(reader.GetOrdinal("Id"));
                                string jsonData = reader.GetString(reader.GetOrdinal("Data"));

                                try
                                {
                                    var account = JsonConvert.DeserializeObject<Account>(jsonData);
                                    if (account?.Avatar?.AllianceId == allianceId)
                                    {
                                        players.Add(account);
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"err: {ex.Message}");
            }

            return players;
        }


        public static Alliance[] FindAlliances(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return Array.Empty<Alliance>();

            var result = new List<Alliance>();

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT \"Data\" FROM alliances WHERE \"Name\" ILIKE @search", connection))
                    {
                        cmd.Parameters.AddWithValue("@search", $"%{searchTerm}%");

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string jsonData = reader.GetString(reader.GetOrdinal("Data"));
                                var alliance = JsonConvert.DeserializeObject<Alliance>(jsonData);
                                if (alliance != null)
                                {
                                    result.Add(alliance);
                                }
                            }
                        }
                    }

                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"err: {ex.Message}");
            }

            return result.ToArray();
        }

        public static Alliance GetAllianceById(long allianceId)
        {
            if (allianceId <= 0)
                return null;

            if (AllianceCache.IsAllianceCached(allianceId))
                return AllianceCache.GetAlliance(allianceId);

            try
            {
                using (var connection = new NpgsqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new NpgsqlCommand("SELECT \"Id\", \"Name\", \"Trophies\", \"Data\" FROM alliances WHERE \"Id\" = @id", connection))
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", allianceId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows && reader.Read())
                            {
                                long id = reader.GetInt64(reader.GetOrdinal("Id"));
                                string name = reader.GetString(reader.GetOrdinal("Name"));
                                int trophies = reader.GetInt32(reader.GetOrdinal("Trophies"));
                                string data = reader.IsDBNull(reader.GetOrdinal("Data")) ? null : reader.GetString(reader.GetOrdinal("Data"));

                                if (!string.IsNullOrEmpty(data))
                                {
                                    var alliance = JsonConvert.DeserializeObject<Alliance>(data);
                                    if (alliance != null)
                                    {
                                        AllianceCache.Cache(alliance);
                                        return alliance;
                                    }
                                }
                            }
                        }
                    }

                    return null;
                }
            }
            catch (NpgsqlException npgEx)
            {
                Logger.Error($"[Alliances] ❌ PostgreSQL ошибка: {npgEx.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Alliances] ⚠ Неизвестная ошибка: {ex.Message}");
                return null;
            }
        }

        public static List<Alliance> GetRandomAlliances(int maxCount)
        {
            long count = Math.Min(maxCount, AllianceIdCounter);

            var list = new List<Alliance>();
            var addedIds = new HashSet<long>(); 

            try
            {
                Random rand = new Random();
                int attempts = 0;
                const int MaxAttemptsPerAlliance = 5;

                while (list.Count < count && attempts < count * MaxAttemptsPerAlliance)
                {
                    long id = rand.NextInt64(1, AllianceIdCounter + 1);

                    if (!addedIds.Contains(id))
                    {
                        var alliance = Load(id);
                        if (alliance != null)
                        {
                            list.Add(alliance);
                            addedIds.Add(id);
                        }
                    }

                    attempts++;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"err: {ex.Message}");
            }

            return list;
        }
    }
}
