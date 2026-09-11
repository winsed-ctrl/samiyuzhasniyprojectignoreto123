using GromCore.Laser.Logic;
using GromCore.Laser.Logic.Message.Account.Auth;
using GromCore.Laser.Server.Database.Cache;
using GromCore.Laser.Server.Networking.Session;
using Newtonsoft.Json;

namespace GromCore.Laser.Server.Logic.Game
{
    public sealed class RuntimeControlState
    {
        public bool CoinsForWinEvent { get; set; }
        public bool BonusTrophiesForWinEvent { get; set; }
        public bool StarrDropForWinEvent { get; set; }
        public bool MaintenanceEnabled { get; set; }
        public HashSet<long> MaintenanceAllowedAccountIds { get; set; } = new();
    }

    public static class RuntimeControlManager
    {
        private static readonly object Sync = new();
        private static readonly string StateFile = Path.Combine(AppContext.BaseDirectory, "runtime_control.json");
        private static RuntimeControlState _state = new();
        private static bool _initialized;

        public static void Initialize()
        {
            lock (Sync)
            {
                if (_initialized)
                    return;

                try
                {
                    if (File.Exists(StateFile))
                    {
                        string json = File.ReadAllText(StateFile);
                        _state = JsonConvert.DeserializeObject<RuntimeControlState>(json) ?? new RuntimeControlState();
                        _state.MaintenanceAllowedAccountIds ??= new HashSet<long>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RuntimeControl] Failed to load state: {ex.Message}");
                    _state = new RuntimeControlState();
                }

                ApplyStateLocked();
                _initialized = true;
            }
        }

        public static RuntimeControlState GetSnapshot()
        {
            EnsureInitialized();
            lock (Sync)
            {
                return new RuntimeControlState
                {
                    CoinsForWinEvent = _state.CoinsForWinEvent,
                    BonusTrophiesForWinEvent = _state.BonusTrophiesForWinEvent,
                    StarrDropForWinEvent = _state.StarrDropForWinEvent,
                    MaintenanceEnabled = _state.MaintenanceEnabled,
                    MaintenanceAllowedAccountIds = new HashSet<long>(_state.MaintenanceAllowedAccountIds)
                };
            }
        }

        public static bool SetWinEvent(int eventId, bool enabled)
        {
            EnsureInitialized();
            lock (Sync)
            {
                switch (eventId)
                {
                    case 1:
                        _state.CoinsForWinEvent = enabled;
                        break;
                    case 2:
                        _state.BonusTrophiesForWinEvent = enabled;
                        break;
                    case 3:
                        _state.StarrDropForWinEvent = enabled;
                        break;
                    default:
                        return false;
                }

                ApplyStateLocked();
                SaveStateLocked();
                return true;
            }
        }

        public static void SetMaintenance(bool enabled)
        {
            EnsureInitialized();
            lock (Sync)
            {
                _state.MaintenanceEnabled = enabled;
                ApplyStateLocked();
                SaveStateLocked();
            }

            if (enabled)
            {
                // Persist all in-memory account and club changes before disconnecting
                // players, otherwise maintenance/restart can roll club membership back.
                AccountCache.SaveAll();
                AllianceCache.SaveAll();
                DisconnectUnauthorizedSessions();
            }
        }

        public static bool AddMaintenanceAccount(long accountId)
        {
            EnsureInitialized();
            lock (Sync)
            {
                bool added = _state.MaintenanceAllowedAccountIds.Add(accountId);
                if (added)
                    SaveStateLocked();
                return added;
            }
        }

        public static bool RemoveMaintenanceAccount(long accountId)
        {
            EnsureInitialized();
            bool removed;
            bool maintenanceEnabled;
            lock (Sync)
            {
                removed = _state.MaintenanceAllowedAccountIds.Remove(accountId);
                maintenanceEnabled = _state.MaintenanceEnabled;
                if (removed)
                    SaveStateLocked();
            }

            if (removed && maintenanceEnabled)
                DisconnectAccount(accountId);

            return removed;
        }

        public static bool IsMaintenanceAllowed(long accountId)
        {
            EnsureInitialized();
            lock (Sync)
            {
                return !_state.MaintenanceEnabled || _state.MaintenanceAllowedAccountIds.Contains(accountId);
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                Initialize();
        }

        private static void ApplyStateLocked()
        {
            GeneralStaticLogic.CoinsForWinEvent = _state.CoinsForWinEvent;
            GeneralStaticLogic.BonusTrophiesForWinEvent = _state.BonusTrophiesForWinEvent;
            GeneralStaticLogic.StarrDropForWinEvent = _state.StarrDropForWinEvent;
            GeneralStaticLogic.IsMaintence = _state.MaintenanceEnabled;
        }

        private static void SaveStateLocked()
        {
            string json = JsonConvert.SerializeObject(_state, Formatting.Indented);
            string temporaryFile = StateFile + ".tmp";
            File.WriteAllText(temporaryFile, json);
            File.Move(temporaryFile, StateFile, overwrite: true);
        }

        private static void DisconnectUnauthorizedSessions()
        {
            if (Sessions.ActiveSessions == null)
                return;

            long[] accountIds = Sessions.ActiveSessions.Keys.ToArray();
            foreach (long accountId in accountIds)
            {
                if (!IsMaintenanceAllowed(accountId))
                    DisconnectAccount(accountId);
            }
        }

        private static void DisconnectAccount(long accountId)
        {
            Session session = Sessions.GetSession(accountId);
            if (session == null)
                return;

            session.GameListener?.SendTCPMessage(new AuthenticationFailedMessage
            {
                ErrorCode = 10,
                Message = "На сервере начался технический перерыв. Ваш аккаунт не добавлен в список доступа."
            });
            Sessions.Remove(accountId);
        }
    }
}
