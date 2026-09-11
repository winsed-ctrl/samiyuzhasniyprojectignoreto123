namespace GromCore.Laser.Server
{
    using Masuda.Net;
    using Masuda.Net.HelpMessage;
    using Masuda.Net.Models;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Handler;
    using GromCore.Laser.Server.Settings;
    using GromCore.Laser.Titan.Debug;
    using System.Drawing;
    using GromCore.Laser.Server.Networking.Session;
    using GromCore.Laser.Server.Fingerprint;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Logic.Battle.Structures;
    using System.Threading;
    using Timer = System.Threading.Timer;
    using Thread = System.Threading.Thread;
    using GromCore.Laser.Logic;

    static class Program
    {
        private static Mutex _mutex;
        private static Timer _offerRefreshTimer;
        
        private static void Main(string[] args)
        {
            bool createdNew;
            _mutex = new Mutex(true, "Global\\GromCoreLaserServer", out createdNew);
            if (!createdNew)
            {
                Console.WriteLine("❌ Сервер уже запущен! Завершите существующий процесс и попробуйте снова.");
                Console.WriteLine("Для принудительного завершения выполните: pkill -f GromCore");
                return;
            }
            
            Console.Title = "GromCore.Laser";
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            
            RefreshFingerprint.Main();

            Colorful.Console.WriteWithGradient(
                @"                                                                                          
                                                                                          
                                                                                          
   ▒████▒  ██████▒    ░████░   ███  ███              ▒████▒   ░████░   ██████▒   ████████ 
  ▓██████  ███████▓   ██████   ███  ███             ▓██████   ██████   ███████▓  ████████ 
 ▒██▒  ░█  ██   ▒██  ▒██  ██▒  ███▒▒███            ▒██▒  ░█  ▒██  ██▒  ██   ▒██  ██       
 ██▒       ██    ██  ██▒  ▒██  ███▓▓███            ██▓       ██▒  ▒██  ██    ██  ██       
 ██░       ██   ▒██  ██    ██  ██▓██▓██            ██░       ██    ██  ██   ▒██  ██       
 ██        ███████▒  ██    ██  ██▒██▒██            ██        ██    ██  ███████▒  ███████  
 ██  ████  ██████▓   ██    ██  ██░██░██            ██        ██    ██  ██████▓   ███████  
 ██░ ████  ██  ▓██░  ██    ██  ██ ██ ██            ██░       ██    ██  ██  ▓██░  ██       
 ██▒   ██  ██   ██▓  ██▒  ▒██  ██    ██            ██▓       ██▒  ▒██  ██   ██▓  ██       
 ▒██▒  ██  ██   ▒██  ▒██  ██▒  ██    ██            ▒██▒  ░█  ▒██  ██▒  ██   ▒██  ██       
  ███████  ██    ██▒  ██████   ██    ██             ▓██████   ██████   ██    ██▒ ████████ 
   ▒████░  ██    ███  ░████░   ██    ██              ▒████▒   ░████░   ██    ███ ████████ 
                                                                                          
                                                                                          
                                                                                          
                                                                                          " + "\n\n\n", Color.DarkRed, Color.Blue, 10);
            
            StableErrorHandler.Initialize();
            Logger.Init();
            Configuration.Instance = Configuration.LoadFromFile("config.json");

            Resources.InitDatabase();
            Resources.InitLogic();
            Resources.InitNetwork();

            // ========== ИНИЦИАЛИЗАЦИЯ МЕНЕДЖЕРА АКЦИЙ ==========
            try
            {
                // Проверяем существование файла custom_offers.json
                string offersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_offers.json");
                if (!File.Exists(offersFile))
                {
                    File.WriteAllText(offersFile, "[]");
                    Console.WriteLine("[Program] Created empty custom_offers.json file");
                }
                
                OffersManager.Initialize();
                Console.WriteLine("[Program] OffersManager initialized successfully!");
                
                // Ждём 3 секунды для полной загрузки аккаунтов
                Console.WriteLine("[Program] Waiting 3 seconds for accounts to load...");
                Thread.Sleep(3000);
                
                // Применяем акции ко всем аккаунтам
                Console.WriteLine("[Program] Applying offers to all accounts...");
                var allAccounts = Accounts.GetAll();
                int appliedCount = 0;
                int errorCount = 0;
                
                foreach (var acc in allAccounts)
                {
                    if (acc == null) continue;
                    
                    try
                    {
                        var fullAccount = Accounts.Load(acc.AccountId);
                        if (fullAccount != null && fullAccount.Home != null)
                        {
                            OffersManager.ApplyOffersToPlayer(fullAccount.Home);
                            Accounts.Save(fullAccount);
                            appliedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Console.WriteLine($"[Program] Error applying offers to account {acc.AccountId}: {ex.Message}");
                    }
                }
                
                Console.WriteLine($"[Program] Offers applied to {appliedCount} accounts, errors: {errorCount}");
                
                // Запускаем таймер для периодического обновления акций (каждые 30 секунд)
                _offerRefreshTimer = new Timer(_ =>
                {
                    try
                    {
                        var accounts = Accounts.GetAll();
                        int count = 0;
                        foreach (var acc in accounts)
                        {
                            if (acc == null) continue;
                            
                            try
                            {
                                var fullAccount = Accounts.Load(acc.AccountId);
                                if (fullAccount?.Home != null)
                                {
                                    OffersManager.ApplyOffersToPlayer(fullAccount.Home);
                                    Accounts.Save(fullAccount);
                                    count++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Program] Auto-refresh error for account {acc.AccountId}: {ex.Message}");
                            }
                        }
                        if (count > 0)
                            Console.WriteLine($"[Program] Auto-refresh: offers applied to {count} accounts");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Program] Auto-refresh error: {ex.Message}");
                    }
                }, null, 30000, 30000);
                
                Console.WriteLine("[Program] Auto-refresh timer started (every 30 seconds)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Program] Failed to initialize OffersManager: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Logger.LogPrint("Server started!");
            ExitHandler.Init();
            CmdHandler.Start();
        }
    }
}
