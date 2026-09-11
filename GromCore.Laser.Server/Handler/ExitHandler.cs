namespace GromCore.Laser.Server.Handler
{
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Networking.Session;

    internal static class ExitHandler
    {
        public static void Exit(object sender, ConsoleCancelEventArgs e)
        {
            Sessions.StartShutdown();

            Logger.LogPrint("Stopping save threads...");
            // Останавливаем потоки сохранения
            Database.Accounts.StopSaveThread();
            Database.Alliances.StopSaveThread();
            
            Logger.LogPrint("Saving changes...");
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;

            Console.WriteLine("Done! Press any key to continue");
            Console.ReadLine();

            Environment.Exit(0);
        }

        public static void Init()
        {
            Console.CancelKeyPress += ExitHandler.Exit;
        }
    }
}
