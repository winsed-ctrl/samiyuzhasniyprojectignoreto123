namespace GromCore.Laser.Server.Handler
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Logic.Game;
    using GromCore.Laser.Server.Networking.Session;
    using System;
    using System.Reflection;
    using System.Threading;
    using Telegram.Bot.Types;
    using Telegram.Bots.Http;
    using Telegram.Bots.Requests;

    public static class CmdHandler
    {
        public static void Start()
        {
            while (true)
            {
                try
                {
                    string cmd = Console.ReadLine();
                    if (cmd == null) continue;
                    if (!cmd.StartsWith("/")) continue;
                    HandleCmd(cmd);
                }
                catch (Exception) { }
            }

        }
        public static void HandleCmd(string cmd, long OwnAccountId = -1)
        {
            cmd = cmd.Substring(1);
            string[] args = cmd.Split(" ");
            if (args.Length < 1) return;
            switch (args[0])
            {
                case "premium":
                    ExecuteGivePremiumToAccount(args);
                    break;
                case "ban":
                    ExecuteBanAccount(args);
                    break;
                case "dev":
                    ExecuteDevAccount(args);
                    break;
                case "ToID":
                    Console.WriteLine(LogicLongCodeGenerator.ToId(args[1]));
                    break;
                case "unban":
                    ExecuteUnbanAccount(args);
                    break;
                case "changename":
                    ExecuteChangeNameForAccount(args);
                    break;
                case "getvalue":
                    ExecuteGetFieldValue(args);
                    break;
                case "changevalue":
                    ExecuteChangeValueForAccount(args);
                    break;
                case "unlockall":
                    ExecuteUnlockAllForAccount(args);
                    break;
                case "removeall":
                    ExecuteRemoveAllForAccount(args);
                    break;
                case "notifall": // /notifall 3254
                    SendNotificationForAll(args);
                    break;
                case "testapprove":
                    test(args);
                    break;
                //case "remvanity":
                //    E
                case "executedevacc":
                    executedevacc(args);
                    break;
                case "convert":
                    ConvertV53Accounts(args);
                    break;
                case "seasonend":
                    SeasonEnd(args);
                    break;
                case "x13":
                    SeasonEndMULTI(args);
                    break;
                case "maintenance":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "m":
                    Console.WriteLine("Starting maintenance...");
                    ExecuteShutdown();
                    Console.WriteLine("Maintenance started!");
                    break;
                case "md":
                    List<LogicData> logicDatas = DataTables.Get(DataType.Map).GetDatas();
                    LogicData logicData = logicDatas[0];
                    string s = logicData.GetCSVRow().GetValueAt(1);
                    int o = logicData.GetCSVRow().GetArraySizeAt(1);
                    string[] m = MapLoader.InitWithMapFromDataTable(null, DataTables.Get(19), "Tutorial");
                    break;
                case "login":
                    if (OwnAccountId == -1) break;
                    long id = LogicLongCodeGenerator.ToId(args[1]);
                    Account account = Accounts.Load(id);
                    if (account == null)
                    {
                        Console.WriteLine("Fail: account not found!");
                        return;
                    }
                    if (LogicServerListener.Instance.IsPlayerOnline(OwnAccountId))
                    {
                        LogicServerListener.Instance.GetGameListener(OwnAccountId).SendTCPMessage(new UnlockAccountOkMessage()
                        {
                            AccountId = account.AccountId,
                            PassToken = account.PassToken
                        });
                    }
                    break;
                case "send":
                    send(args);
                    break;
                case "changetheme":
                    if (OwnAccountId == -1) break;
                    Account account1 = Accounts.Load(OwnAccountId);
                    int i = -1;
                    try
                    {
                        i = int.Parse(args[1]);
                    }
                    catch(Exception) { }
                    account1.Home.PreferredThemeId = i;
                    break;
            }
        }
        private static void executedevacc(string[] args){
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Refresh();
            account.Avatar.AddDiamonds(999999);
            account.Home.HasPremiumPass = true;
            account.Home.HasPremiumPassPlus = true;
            
            foreach(EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            { account.Home.UnlockedEmotes.Add(emoteData.GetGlobalId());}
                        foreach(TitlesData emoteData in DataTables.Get(DataType.Titul).GetDatas())
            { account.Home.UnlockedTituls.Add(emoteData.GetGlobalId());}
                        foreach(SprayData emoteData in DataTables.Get(DataType.Spray).GetDatas())
            { account.Home.UnlockedSprays.Add(emoteData.GetGlobalId());}
                        foreach(PlayerThumbnailData emoteData in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            { account.Home.UnlockedThumbnails.Add(emoteData.GetGlobalId());}
            

            account.Avatar.IsDebugAccount = true;
            AccountCache.SaveAll();
            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "6"
                });
                Sessions.Remove(id);
            }
        }
        private static void send(string[] args)
        {
            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }


            // if (Sessions.IsSessionActive(id))
            // {
            //     var session = Sessions.GetSession(id);
            //     session.GameListener.SendCommand(new LogicRecruitRoadSelectBrawlerCommand());
            // }
            
        }
        private static void test(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /test [index]");
                //return;
            }
            PlayerCustomMapsHandler.SetMapState(int.Parse(args[1]), int.Parse(args[2]));
        }
        private static void RemVanity(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }
        private static void ExecuteUnlockAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unlockall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Refresh();
            account.Avatar.AddDiamonds(99999);

            AccountCache.SaveAll();
            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "6"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteRemoveAllForAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /removeall [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            CharacterData character = DataTables.Get(16).GetData<CharacterData>("Cocooner");
            account.Avatar.RemoveHero(character.GetGlobalId());

            Logger.Print($"Successfully unlocked all brawlers for account {account.AccountId.GetHigherInt()}-{account.AccountId.GetLowerInt()} ({args[1]})");

            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void SendNotificationForAll(string[] args) // SendNotificationForAll(
        {
            // if (args.Length != 2)
            // {
            //     Console.WriteLine("Usage: /notifall MSG");
            //     return;
            // }

            string message_to_all = "";

            for (int x = 1; x < args.Length; x++){
                message_to_all = message_to_all + " " + args[x];
            }

            Sessions.NotificationForAll(message_to_all);
        }

        private static void ExecuteGivePremiumToAccount(string[] args)
        {


            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /premium [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }
            account.Avatar.AddDiamonds(14888);
            account.Avatar.IsPremium = true;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteUnbanAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /unban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.BanEndTime = new DateTime(1999, 05, 01, 12, 30, 00);
            account.Avatar.TextReason = null;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void SeasonEnd(string[] args)
        {
            var accounts = Accounts.GetAll();

            Parallel.ForEach(accounts, acc =>
            {
                try{
                if (acc == null) return;
                Console.WriteLine($"Account: {acc.Avatar.AccountId}/{Accounts.GetMaxAvatarId()}");
                if (acc.Avatar.Trophies < 550) return;

                var hhh = new List<int>();
                var ht = new List<int>();
                var htr = new List<int>();
                var sa = new List<int>();

                int[] start = { 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400 };
                int[] end = { 599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, int.MaxValue };
                int[] reward = { 50, 60, 80, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240 };
                int[] reset = { 549, 599, 649, 699, 749, 799, 849, 899, 924, 949, 974, 1024, 1049, 1074, 1099, 1124, 1149, 1174 };

                foreach (Hero h in acc.Avatar.Heroes)
                {
                    if (h.Trophies < start[0]) continue;

                    hhh.Add(h.CharacterId);
                    ht.Add(h.Trophies);

                    int i = -1;
                    for (int j = 0; j < start.Length; j++)
                    {
                        if (h.Trophies >= start[j] && h.Trophies <= end[j])
                        {
                            i = j;
                            break;
                        }
                    }

                    if (h.Trophies > 1400)
                    {
                        htr.Add(h.Trophies - 1399);
                        sa.Add(240);
                        acc.Avatar.AddBlings(240);
                        h.Trophies = 1399;
                    }
                    else if (i != -1)
                    {
                        htr.Add(h.Trophies - reset[i]);
                        sa.Add(reward[i]);
                        acc.Avatar.AddBlings(reward[i]);
                        h.Trophies = reset[i];
                    }
                }
                Accounts.Save(acc);}
                catch{
                    Console.WriteLine($"Account: catch!");
                }
            });
        }

        private static void SeasonEndMULTI(string[] args)
        {
            var accounts = Accounts.GetAll();

            Parallel.ForEach(accounts, acc =>
            {
                try{
                if (acc == null) return;
                Console.WriteLine($"Account: {acc.Avatar.AccountId}/{Accounts.GetMaxAvatarId()}");
                // if (acc.Avatar.Trophies < 550) return;

                var hhh = new List<int>();
                var ht = new List<int>();
                var htr = new List<int>();
                var sa = new List<int>();

                int[] start = { 550, 600, 650, 700, 750, 800, 850, 900, 950, 1000, 1050, 1100, 1150, 1200, 1250, 1300, 1350, 1400 };
                int[] end = { 599, 649, 699, 749, 799, 849, 899, 949, 999, 1049, 1099, 1149, 1199, 1249, 1299, 1349, 1399, int.MaxValue };
                int[] reward = { 50, 60, 80, 100, 110, 120, 130, 140, 150, 160, 170, 180, 190, 200, 210, 220, 230, 240 };
                int[] reset = { 549, 599, 649, 699, 749, 799, 849, 899, 924, 949, 974, 1024, 1049, 1074, 1099, 1124, 1149, 1174 };

                foreach (Hero h in acc.Avatar.Heroes)
                {
                    h.Trophies = h.Trophies / 13;
                    h.HighestTrophies = h.HighestTrophies / 13;
                    // if (h.Trophies < start[0]) continue;

                    // hhh.Add(h.CharacterId);
                    // ht.Add(h.Trophies);

                    // int i = -1;
                    // for (int j = 0; j < start.Length; j++)
                    // {
                    //     if (h.Trophies >= start[j] && h.Trophies <= end[j])
                    //     {
                    //         i = j;
                    //         break;
                    //     }
                    // }

                    // if (h.Trophies > 1400)
                    // {
                    //     htr.Add(h.Trophies - 1399);
                    //     sa.Add(240);
                    //     acc.Avatar.AddBlings(240);
                    //     h.Trophies = 1399;
                    // }
                    // else if (i != -1)
                    // {
                    //     htr.Add(h.Trophies - reset[i]);
                    //     sa.Add(reward[i]);
                    //     acc.Avatar.AddBlings(reward[i]);
                    //     h.Trophies = reset[i];
                    // }
                }
                Accounts.Save(acc);}
                catch{
                    Console.WriteLine($"Account: catch!");
                }
            });
        }

        private static void ExecuteBanAccount(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: /ban [TAG]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            if (!int.TryParse(args[2], out int banID))
            {
                return;
            }

            if (!DateTime.TryParse(args[3], out DateTime banEndTime))
            {
                //await BotClient.SendMessage(chatId, "? Неверный формат даты. Используйте: yyyy-MM-dd HH:mm:ss", cancellationToken: ct);
                return;
            }

            var banAccount = account;
            if (banAccount == null)
            {
                //await BotClient.SendMessage(chatId, "? Аккаунт не найден.", cancellationToken: ct);
                return;
            }

            banAccount.Avatar.BanCount++;
            banAccount.Avatar.BanID = banID;
            banAccount.Avatar.BanEndTime = banEndTime;
            if (args.Length >= 5) banAccount.Avatar.TextReason = args[4];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }

        }

        private static void ExecuteDevAccount(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: /ban [TAG]");
                return;
            }

            long id = 0;

            if (args[1].StartsWith("#")){
                id = LogicLongCodeGenerator.ToId(args[1]);
            }
            else{
                id = long.Parse(args[1]);
            }
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.IsDev = true;
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteChangeNameForAccount(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [NewName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            account.Avatar.Name = args[2];
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ExecuteGetFieldValue(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            int value = (int)field.GetValue(account.Avatar);
            Console.WriteLine($"LogicClientAvatar::{args[2]} = {value}");
        }

        private static void ExecuteChangeValueForAccount(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: /changevalue [TAG] [FieldName] [Value]");
                return;
            }

            long id = LogicLongCodeGenerator.ToId(args[1]);
            Account account = Accounts.Load(id);
            if (account == null)
            {
                Console.WriteLine("Fail: account not found!");
                return;
            }

            Type type = typeof(ClientAvatar);
            FieldInfo field = type.GetField(args[2]);
            if (field == null)
            {
                Console.WriteLine($"Fail: LogicClientAvatar::{args[2]} not found!");
                return;
            }

            field.SetValue(account.Avatar, int.Parse(args[3]));
            if (Sessions.IsSessionActive(id))
            {
                var session = Sessions.GetSession(id);
                session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                {
                    Message = "Your account updated!"
                });
                Sessions.Remove(id);
            }
        }

        private static void ConvertV53Accounts(string[] args)
        {
            long lastAccId = Accounts.GetMaxAvatarId();
            int broken = 0;
            int saved = 0;
            for (int accid = 0; accid <= lastAccId; accid++)
            {
                try
                {
                Account thisAcc = Accounts.Load(accid);

                Console.WriteLine("Account id: " + accid + "/" + lastAccId);
                
                if (thisAcc == null) continue;
                if (true) //(thisAcc.Avatar.Trophies > -3999)
                {
                    thisAcc.Avatar.AllianceRole = AllianceRole.None;
                    thisAcc.Avatar.AllianceId = 0;
                    Accounts.Save(thisAcc);
                }
                }
                catch{
                    Console.WriteLine("accid: " + accid + " is broken!");
                }
            }
            return;
        }

        private static void ConvertV53Accounts__(string[] args)
        {
            long lastAccId = Accounts.GetMaxAvatarId();
            int broken_ = 0;
            int saved_ = 0;
            int null_ = 0;
            for (int accid = 0; accid <= lastAccId; accid++)
            {
                try
                {
                Account thisAcc = Accounts.Load(accid);
                // Console.WriteLine("Account id: " + accid + "/" + lastAccId);
                if (thisAcc == null) {null_++;
                    continue;}
                if (true) //(thisAcc.Avatar.Trophies > -3999)
                {
                    var _HomeId = thisAcc.Home.HomeId;
                    var _AccountId = thisAcc.Avatar.AccountId;
                    var _PassToken = thisAcc.Avatar.PassToken;
                    var _Password = thisAcc.Avatar.Password;
                    var _Name = thisAcc.Avatar.Name;
                    var _NameSetByUser = thisAcc.Avatar.NameSetByUser;
                    var _TrioWins = thisAcc.Avatar.TrioWins;
                    var _Gold = thisAcc.Avatar.Gold;
                    var _Diamonds = thisAcc.Avatar.Diamonds;
                    var _StarPoints = thisAcc.Avatar.StarPoints;
                    var _TrophyRoadProgress = thisAcc.Home.TrophyRoadProgress;

                    var _Heroes = thisAcc.Avatar.Heroes;
                    var _UnlockedSkins = thisAcc.Home.UnlockedSkins;

                    thisAcc.Home = new ClientHome();
                    thisAcc.Avatar = new ClientAvatar();

                    // Hero hero = new Hero(16000000);
                    // hero.Trophies = 999;
                    // hero.HighestTrophies = 999;
                    // thisAcc.Avatar.Heroes.Add(hero);

                    thisAcc.AccountId = _AccountId;
                    thisAcc.PassToken = _PassToken;
                    thisAcc.Home.HomeId = _HomeId;
                    thisAcc.Avatar.AccountId = _AccountId;
                    thisAcc.Avatar.PassToken = _PassToken;
                    thisAcc.Avatar.Password = _Password;
                    thisAcc.Avatar.Name = _Name;
                    thisAcc.Avatar.NameSetByUser = _NameSetByUser;
                    thisAcc.Avatar.TrioWins = _TrioWins;
                    thisAcc.Home.RecruitTokens = _Gold;
                    thisAcc.Avatar.Diamonds = _Diamonds;
                    thisAcc.Avatar.Blings = _StarPoints;
                    thisAcc.Home.TrophyRoadProgress = _TrophyRoadProgress;

                    List<int> characters_ids = new List<int>{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 89, 90, 76, 85, 61, 54, 68, 62, 75, 86, 56, 64, 66, 77, 92, 91};

                    foreach (Hero h in _Heroes){
                        Hero hero = new Hero(16000000 + characters_ids[h.CharacterId - 16000000]);

                        hero.Trophies = h.Trophies;
                        hero.HighestTrophies = h.HighestTrophies;
                        hero.PowerPoints = h.PowerPoints;
                        hero.PowerLevel = h.PowerLevel;

                        thisAcc.Avatar.Heroes.Add(hero);
                    }

                    if (_UnlockedSkins.Contains(29000052)) thisAcc.Home.UnlockedSkins.Add(29000052);
                    if (_UnlockedSkins.Contains(29000214)) thisAcc.Home.UnlockedSkins.Add(29000214);
                    if (_UnlockedSkins.Contains(29000212)) thisAcc.Home.UnlockedSkins.Add(29000212);
                    if (_UnlockedSkins.Contains(29000178)) thisAcc.Home.UnlockedSkins.Add(29000178);
                    if (_UnlockedSkins.Contains(29000174)) thisAcc.Home.UnlockedSkins.Add(29000174);
                    if (_UnlockedSkins.Contains(29000180)) thisAcc.Home.UnlockedSkins.Add(29000180);

                    thisAcc.Home.OldMioparkComp = (_UnlockedSkins.Count * 10) + 50;

                    Accounts.Save(thisAcc);
                    saved_++;

                }}
                catch{
                    Console.WriteLine("accid: " + accid + " is broken!");
                    Accounts.Delete(accid);
                    broken_++;
                }
                Console.WriteLine("Broken: " + broken_ + " Saved: " + saved_ + " Null: " + null_ + " | " + broken_ + "/" + saved_ + "/" + null_ + " | (" + accid + "/" + lastAccId + ")");
            }
            int broken = 0;
            int saved = 0;
            for (int accid = 0; accid <= lastAccId; accid++)
            {
                try
                {
                Account thisAcc = Accounts.Load(accid);

                Console.WriteLine("Account id: " + accid + "/" + lastAccId);
                
                if (thisAcc == null) continue;
                if (true) //(thisAcc.Avatar.Trophies > -3999)
                {
                    if (thisAcc.Home.OldMioparkComp == 0) broken++;
                    else saved++;

                    Console.WriteLine("Broken: " + broken + " Saved: " + saved + " | " + broken + "/" + saved + " (" + thisAcc.Home.OldMioparkComp + ")");

                    Accounts.Save(thisAcc);
                }}
                catch{
                    Console.WriteLine("accid: " + accid + " is broken!");
                }
            }
            return;
        }

        private static void ConvertV53Accounts_(string[] args)
        {
            long lastAccId = Accounts.GetMaxAvatarId();
            int broken = 0;
            int saved = 0;
            int accid = 5951;
            // for (int accid = 0; accid <= lastAccId; accid++)
            // {
                // try
                // {
                Console.WriteLine("1");
                Account thisAcc = Accounts.Load(accid);
                Console.WriteLine("2");

                Console.WriteLine("Account id: " + accid + "/" + lastAccId);
                
                if (thisAcc == null) {return;
                Console.WriteLine("accid: " + accid + " is null!");
                }
                if (true) //(thisAcc.Avatar.Trophies > -3999)
                {
                    if (thisAcc.Home.OldMioparkComp == 0) {broken++;
                    var _HomeId = thisAcc.Home.HomeId;
                    var _AccountId = thisAcc.Avatar.AccountId;
                    var _PassToken = thisAcc.Avatar.PassToken;
                    var _Password = thisAcc.Avatar.Password;
                    var _Name = thisAcc.Avatar.Name;
                    var _NameSetByUser = thisAcc.Avatar.NameSetByUser;
                    var _TrioWins = thisAcc.Avatar.TrioWins;
                    var _Gold = thisAcc.Avatar.Gold;
                    var _Diamonds = thisAcc.Avatar.Diamonds;
                    var _StarPoints = thisAcc.Avatar.StarPoints;
                    var _TrophyRoadProgress = thisAcc.Home.TrophyRoadProgress;

                    var _Heroes = thisAcc.Avatar.Heroes;
                    var _UnlockedSkins = thisAcc.Home.UnlockedSkins;

                    thisAcc.Home = new ClientHome();
                    thisAcc.Avatar = new ClientAvatar();

                    // Hero hero = new Hero(16000000);
                    // hero.Trophies = 999;
                    // hero.HighestTrophies = 999;
                    // thisAcc.Avatar.Heroes.Add(hero);

                    thisAcc.AccountId = _AccountId;
                    thisAcc.PassToken = _PassToken;
                    thisAcc.Home.HomeId = _HomeId;
                    thisAcc.Avatar.AccountId = _AccountId;
                    thisAcc.Avatar.PassToken = _PassToken;
                    thisAcc.Avatar.Password = _Password;
                    thisAcc.Avatar.Name = _Name;
                    thisAcc.Avatar.NameSetByUser = _NameSetByUser;
                    thisAcc.Avatar.TrioWins = _TrioWins;
                    thisAcc.Home.RecruitTokens = _Gold;
                    thisAcc.Avatar.Diamonds = _Diamonds;
                    thisAcc.Avatar.Blings = _StarPoints;
                    thisAcc.Home.TrophyRoadProgress = _TrophyRoadProgress;

                    List<int> characters_ids = new List<int>{0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 89, 90, 76, 85, 61, 54, 68, 62, 75, 86, 56, 64, 66, 77, 92, 91};

                    foreach (Hero h in _Heroes){
                        Hero hero = new Hero(16000000 + characters_ids[h.CharacterId - 16000000]);

                        hero.Trophies = h.Trophies;
                        hero.HighestTrophies = h.HighestTrophies;
                        hero.PowerPoints = h.PowerPoints;
                        hero.PowerLevel = h.PowerLevel;

                        thisAcc.Avatar.Heroes.Add(hero);
                    }

                    if (_UnlockedSkins.Contains(29000052)) thisAcc.Home.UnlockedSkins.Add(29000052);
                    if (_UnlockedSkins.Contains(29000214)) thisAcc.Home.UnlockedSkins.Add(29000214);
                    if (_UnlockedSkins.Contains(29000212)) thisAcc.Home.UnlockedSkins.Add(29000212);
                    if (_UnlockedSkins.Contains(29000178)) thisAcc.Home.UnlockedSkins.Add(29000178);
                    if (_UnlockedSkins.Contains(29000174)) thisAcc.Home.UnlockedSkins.Add(29000174);
                    if (_UnlockedSkins.Contains(29000180)) thisAcc.Home.UnlockedSkins.Add(29000180);

                    thisAcc.Home.OldMioparkComp = (_UnlockedSkins.Count * 10) + 50;

                    Accounts.Save(thisAcc);}
                    else saved++;

                    Console.WriteLine("Fixed: " + broken + " Skip:" + saved);
                }
                // }
                // catch{
                //     Console.WriteLine("accid: " + accid + " is broken!");
                // }
            // }
            return;
        }

        private static void ExecuteShutdown()
        {
            Sessions.StartShutdown();
            AccountCache.SaveAll();
            AllianceCache.SaveAll();

            AccountCache.Started = false;
            AllianceCache.Started = false;
        }
    }
}
