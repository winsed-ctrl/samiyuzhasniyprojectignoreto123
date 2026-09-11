namespace GromCore.Laser.Server.Logic.Game
{
    using Polly;
    using GromCore.Laser.Logic;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Networking.Session;
    using System.Security.Principal;
    using System.Threading;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Settings;

    public static class EventDelayTicks
    {
        public static void Init()
        {
            new Thread(Update).Start();
        }

        private static void Update()
        {
            int TimeUntilRestart = Configuration.Instance.TimeUntilRestart;
            GeneralStaticLogic.InitPreset();
            while (true)
            {
                // foreach (Account account in Accounts.GetAll())
                // {
                //     if (StringExtensions.ContainsAny(account.Avatar.Name, GeneralStaticLogic.BlockedWords) || account.Avatar.Name.Length is <= 2 or >= 25)
                //     {
                //         if (account.Avatar.IsDebugAccount) break;
                //         account.Avatar.Name = "Brawler";

                //         if (account.Avatar.AllianceId > 0)
                //         {
                //             if (account.Avatar.AllianceId > 0)
                //             {
                //                 Alliance alliance = Alliances.Load(account.Avatar.AllianceId);


                //                 var member = Alliances.GetMember(account.Avatar.AllianceId, account.AccountId);
                //                 if (member != null)
                //                 {
                //                     member.Avatar.Name = "Brawler";
                //                     Alliances.Save(alliance);
                //                 }
                //             }
                //         }
                //         if (Sessions.IsSessionActive(account.AccountId))
                //         {
                //             var session = Sessions.GetSession(account.AccountId);
                //             session.Home.Avatar.Name = "Brawler";
                //             session.Connection.Send(new AuthenticationFailedMessage()
                //             {
                //                 ErrorCode = 1,
                //                 Message = "Account updated"
                //             });
                //         }

                //         //account.Avatar.BanCount++;
                //         //account.Avatar.BanID = 1;
                //         //account.Avatar.BanEndTime = DateTime.Now.AddMonths(1);
                //         Accounts.Save(account);
                //     }
                //     if (account.Avatar.AllianceId <= 0) account.Avatar.AllianceName = "";
                //     // if (account.Avatar.AllianceId >= 0 && String.IsNullOrEmpty(account.Avatar.AllianceName)) account.Avatar.AllianceName = Alliances.GetAllianceById(account.Avatar.AllianceId).Name;
                //     // ÓÌÓ ÔÓ ÔËÍÓÎÛ ÎÓÊËÚ ÒÂ‚Â Ò ·‡ÁÓÈ ÓÚ ‚39 ˇÂ·‡Î
                //     // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’

                // }
                // foreach(Alliance alliance in Alliances.GetAll())
                // {
                //     if(StringExtensions.ContainsAny(alliance.Name, GeneralStaticLogic.BlockedWords) || alliance.Name.Length is <= 2 or >= 25)
                //     {
                //         Alliances.Delete(alliance.Id);
                //     }
                //     if(StringExtensions.ContainsAny(alliance.Description, GeneralStaticLogic.BlockedWords) || alliance.Description.Length >= 250)
                //     {
                //         alliance.Description = "Description deleted by admins.";
                //         Alliances.Save(alliance);
                //     }

                //     //foreach(Account allianceMember in Alliances.GetPlayersInAlliance(alliance.Id))
                //     //{
                //         //if (allianceMember == null || allianceMember.Avatar.AllianceId != alliance.Id) alliance.RemoveMemberById(allianceMember.AccountId);
                //     //}
                // }

                // // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’
                // // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’


                // // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’
                // // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’
                // // ¬–”¡¿“‹ ÃŒ∆ÕŒ “ŒÀ‹ Œ œŒ—À≈ –≈À»«¿  Œ√ƒ¿ Õ»Œƒ»Õ ¿   »  À”¡ V39 Õ≈ ¡”ƒ≈“ ¬ ∆»¬€’

                // // Û ÌÂ„Ó ‡ÎÎÂ„Ëˇ Ì‡ ‚39 ÓÌÓ ÏÌÂ Á‡Ô‡ÓÎÓ ÔÂÂ·Ó ‡ÍÍÓ‚ ÏÌÂ ÓÔˇÚ ÏÌÓ„Ó ÏËÌÛÚ ÍÓÔËÓ‚‡Ú¸ ·‡ÁÛ
                // GeneralStaticLogic.InitPreset();
                TimeUntilRestart -= 1;
                if (TimeUntilRestart == 1 || TimeUntilRestart == 2 || TimeUntilRestart == 3 || TimeUntilRestart == 4 || TimeUntilRestart == 5 || TimeUntilRestart == 15) {
                    Console.WriteLine($"Restarting server in {TimeUntilRestart} m.");
                    Sessions.NotificationForAll($"Restarting server in {TimeUntilRestart} m.\nœÂÂÁ‡ÔÛÒÍ ÒÂ‚Â‡ ˜ÂÂÁ {TimeUntilRestart} Ï.");
                }
                else if (TimeUntilRestart % 15 == 0) {
                    Console.WriteLine($"Restarting server in {TimeUntilRestart} m.");
                }
                if (TimeUntilRestart == 0){
                    Console.WriteLine($"Restarting server... (1/4)");
                    Sessions.StartShutdown();
                    Console.WriteLine($"Restarting server... (2/4)");
                    AccountCache.SaveAll();
                    Console.WriteLine($"Restarting server... (3/4)");
                    var saveTask = Task.Run(() => AllianceCache.SaveAll());
                    if (!saveTask.Wait(TimeSpan.FromSeconds(60))) // Ú‡ÈÏ‡ÛÚ 10 ÒÂÍÛÌ‰
                    {
                        Console.WriteLine("SaveAll timed out, forcing exit...");
                    }
                    Console.WriteLine($"Restarting server... (4/4)");
                    Environment.Exit(0);
                }
                Thread.Sleep(60000);
            }
        }
    }
}