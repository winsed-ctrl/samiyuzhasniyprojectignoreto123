namespace GromCore.Laser.Server.Database.Cache
{
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Server.Logic.Game;
    using System.Diagnostics.CodeAnalysis;
    using System.Collections.Concurrent;

    public static class AllianceCache
    {
        private static ConcurrentDictionary<long, Alliance> CachedAlliances;
        private static Thread _thread;
        private static readonly object _lock = new object();

        public static bool Started = true;

        public static int Count
        {
            get
            {
                return CachedAlliances?.Count ?? 0;
            }
        }

        public static void Init()
        {
            CachedAlliances = new ConcurrentDictionary<long, Alliance>();
            _thread = new Thread(Update);
            _thread.Start();
        }

        private static void Update()
        {
            while (Started)
            {
                // SaveAll();
                Thread.Sleep(60000);
                // Matchmaking.Reload();
            }
        }

        public static void SaveAll()
        {
            try
            {
                if (CachedAlliances == null) return;
                
                int saved = 0;
                int errors = 0;
                
                foreach (var alliance in CachedAlliances.Values)
                {
                    try
                    {
                        Alliances.SaveImmediate(alliance);
                        saved++;
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        Logger.Error($"Error saving alliance {alliance?.Id}: {ex.Message}");
                    }
                }
                
                Logger.Print($"[AllianceCache.SaveAll] Saved {saved} alliances, errors: {errors}");
            }
            catch (Exception ex)
            {
                Logger.Error($"[AllianceCache.SaveAll] Critical error: {ex.Message}");
            }
        }

        public static bool IsAllianceCached(long id)
        {
            if (CachedAlliances == null) return false;
            return CachedAlliances.ContainsKey(id);
        }

        public static Alliance GetAlliance(long id)
        {
            if (CachedAlliances == null) return null;
            
            if (CachedAlliances.TryGetValue(id, out Alliance alliance))
            {
                return alliance;
            }
            return null;
        }

        public static void RemoveAlliance(long id)
        {
            if (CachedAlliances == null) return;
            CachedAlliances.TryRemove(id, out _);
        }

        public static void Cache(Alliance alliance)
        {
            if (alliance == null) return;
            if (CachedAlliances == null) return;
            
            CachedAlliances[alliance.Id] = alliance;
        }
    }
}