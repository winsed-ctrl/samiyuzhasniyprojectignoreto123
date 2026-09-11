using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GromCore.Laser.Server.Database.Cache;
using GromCore.Laser.Server.Logic.Game;
using GromCore.Laser.Server.Networking.Session;
using GromCore.Laser.Titan.Debug;

namespace GromCore.Laser.Server.Utils
{
    public static class ProfanityManager
    {
        private static string? FPath;
        private static string[]? Lines;

        public static void Initialize(string? path)
        {
            try
            {
                FPath = path;
                Lines = File.ReadAllLines(FPath!);
            }
            catch (Exception exe)
            {
                Console.WriteLine("Cannot init profanityManager: " + exe.Message);
                Debugger.Warning("Server shutting down...");
                Sessions.StartShutdown();
                AccountCache.SaveAll();
                AllianceCache.SaveAll();
                AccountCache.Started = false;
                AllianceCache.Started = false;
                Environment.Exit(0);
            }
        }

        public static string ProfanitySerialize(string deltaText)
        {
            return Lines!.Any(value => deltaText.Contains(value, StringComparison.CurrentCultureIgnoreCase))
                ? new string('*', deltaText.Length)
                : deltaText;
        }

        public static bool ProfanityContainCheck(string deltaText)
        {
            return Lines!.Any(value => deltaText.Contains(value, StringComparison.CurrentCultureIgnoreCase));
        }
    }
}
