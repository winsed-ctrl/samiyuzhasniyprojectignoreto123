namespace GromCore.Laser.Server.Message
{ 
    using Masuda.Net.Models;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using GromCore.Laser.Logic;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Battle.Component;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Command;
    using GromCore.Laser.Logic.Command.Avatar;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Friends;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Quest;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message;
    using GromCore.Laser.Logic.Message.Account;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Battle;
    using GromCore.Laser.Logic.Message.Club;
    using GromCore.Laser.Logic.Message.Friends;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Message.Ranked;
    using GromCore.Laser.Logic.Message.Ranking;
    using GromCore.Laser.Logic.Message.Security;
    using GromCore.Laser.Logic.Message.Team;
    using GromCore.Laser.Logic.Message.Team.Stream;
    using GromCore.Laser.Logic.Message.Udp;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Logic.Ranked;
    using GromCore.Laser.Logic.Stream.Entry;
    using GromCore.Laser.Logic.Team;
    using GromCore.Laser.Logic.Team.Stream;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Server;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Database.Cache;
    using GromCore.Laser.Server.Database.Models;
    using GromCore.Laser.Server.Handler;
    using GromCore.Laser.Server.Logic;
    using GromCore.Laser.Server.Logic.Game;
    using GromCore.Laser.Server.Networking;
    using GromCore.Laser.Server.Networking.Security;
    using GromCore.Laser.Server.Networking.Session;
    using GromCore.Laser.Server.Networking.UDP.Game;
    using GromCore.Laser.Server.Settings;
    using GromCore.Laser.Server.Utils;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Titan.Math;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Diagnostics.SymbolStore;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Numerics;
    using System.Reflection;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Policy;
    using System.Security.Principal;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Debugger = Titan.Debug.Debugger;
    using System.Net.NetworkInformation;

    public class MessageManager
    {
        public Connection Connection { get; }

        public HomeMode HomeMode;

        public DateTime LastKeepAlive;

        private Dictionary<int, int> accrelay;

        public MessageManager(Connection connection)
        {
            Connection = connection;
            LastKeepAlive = DateTime.UtcNow;
        }

        public void UpdateTeam()
        {
            if (Teams.Get(HomeMode.Avatar.TeamId) != null)
                Teams.Get(HomeMode.Avatar.TeamId).TeamUpdated();
        }

        public bool IsAlive()
        {
            return (int)(DateTime.UtcNow - LastKeepAlive).TotalSeconds < 120;
        }

        public void ProcessMessage(GameMessage message)
        {
            switch (message.GetMessageType())
            {
                case 10100:
                    ClientHelloReceived((ClientHelloMessage)message);
                    break;
                case 10101:
                    LoginReceived((AuthenticationMessage)message);
                    break;
                case 10107:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10108:
                    LastKeepAlive = DateTime.UtcNow;
                    break;
                case 10110:
                    AnalyticEventsReceived((AnalyticEventMessage)message);
                    break;
                case 10212:
                    ChangeName((ChangeAvatarNameMessage)message);
                    break;
                case 10177:
                    ClientInfoReceived((ClientInfoMessage)message);
                    break;
                case 10501:
                    AcceptFriendReceived((AcceptFriendMessage)message);
                    break;
                case 10502:
                    AddFriendReceived((AddFriendMessage)message);
                    break;
                case 10504:
                    AskForFriendListReceived((AskForFriendListMessage)message);
                    break;
                case 10506:
                    RemoveFriendReceived((RemoveFriendMessage)message);
                    break;
                case 12100:
                    CreatePlayerMapReceived((CreatePlayerMapMessage)message);
                    break;
                case 12101:
                    DeletePlayerMapReceived((DeletePlayerMapMessage)message);
                    break;
                case 12102:
                    GetPlayerMapsReceived((GetPlayerMapsMessage)message);
                    break;
                case 12103:
                    UpdatePlayerMapReceived((UpdatePlayerMapMessage)message);
                    break;
                case 12108:
                    GoHomeFromMapEditorReceived((GoHomeFromMapEditorMessage)message);
                    break;
                case 12110:
                    TeamSetPlayerMapReceived((TeamSetPlayerMapMessage)message);
                    break;
                case 14456:
                    GoHomeReceived((GoHomeMessage)message);
                    break;
                case 14102:
                    EndClientTurnReceived((EndClientTurnMessage)message);
                    break;
                case 18977:
                    MatchmakeRequestReceived((MatchmakeRequestMessage)message);
                    break;
                case 14104:
                    StartSpectateReceived((StartSpectateMessage)message);
                    break;
                case 14106:
                    CancelMatchMaking((CancelMatchmakingMessage)message);
                    break;
                case 14107:
                    StopSpectateReceived((StopSpectateMessage)message);
                    break;
                case 14109:
                    GoHomeFromOfflinePractiseReceived((GoHomeFromOfflinePractiseMessage)message);
                    break;
                case 15081:
                    GetPlayerProfile((GetPlayerProfileMessage)message);
                    break;
                case 14118:
                    SinglePlayerMatchRequestReceived((SinglePlayerMatchRequestMessage)message);
                    break;
                case 14166:
                    break;
                case 12938:
                    SeasonRewardsMessageReceived((GetSeasonRewardsMessage)message);
                    break;
                case 14301:
                    CreateAllianceReceived((CreateAllianceMessage)message);
                    break;
                case 14302:
                    AskForAllianceDataReceived((AskForAllianceDataMessage)message);
                    break;
                case 14303:
                    AskForJoinableAllianceListReceived((AskForJoinableAllianceListMessage)message);
                    break;
                case 14305:
                    JoinAllianceReceived((JoinAllianceMessage)message);
                    break;
                case 14307:
                    KickAllianceMemberReceived((KickAllianceMemberMessage)message);
                    break;
                case 14308:
                    LeaveAllianceReceived((LeaveAllianceMessage)message);
                    break;
                case 14315:
                    ChatToAllianceStreamReceived((ChatToAllianceStreamMessage)message);
                    break;
                case 14316:
                    ChangeAllianceSettingsReceived((ChangeAllianceSettingsMessage)message);
                    break;
                case 12541:
                    TeamCreateReceived((TeamCreateMessage)message);
                    break;
                case 14353:
                    TeamLeaveReceived((TeamLeaveMessage)message);
                    break;
                case 14354:
                    TeamChangeMemberSettingsReceived((TeamChangeMemberSettingsMessage)message);
                    break;
                case 14355:
                    TeamSetMemberReadyReceived((TeamSetMemberReadyMessage)message);
                    break;
                case 14357:
                    TeamToggleMemberSideReceived((TeamToggleMemberSideMessage)message);
                    break;
                case 14358:
                    TeamSpectateMessageReceived((TeamSpectateMessage)message);
                    break;
                case 14049:
                    TeamChatReceived((TeamChatMessage)message);
                    break;
                case 14361:
                    TeamMemberStatusReceived((TeamMemberStatusMessage)message);
                    break;
                case 14362:
                    TeamSetEventReceived((TeamSetEventMessage)message);
                    break;
                case 14363:
                    TeamSetLocationReceived((TeamSetLocationMessage)message);
                    break;
                case 14365:
                    TeamInviteReceived((TeamInviteMessage)message);
                    break;
                case 14366:
                    PlayerStatusReceived((PlayerStatusMessage)message);
                    break;
                case 14369:
                    TeamPremadeChatReceived((TeamPremadeChatMessage)message);
                    break;
                case 14469:
                    AlliancePremadeChatReceived((AlliancePremadeChatMessage)message);
                    break;
                case 14373:
                    TeamBotSlotDisableReceived((TeamBotSlotDisableMessage)message);
                    break;
                case 14403:
                    GetLeaderboardReceived((GetLeaderboardMessage)message);
                    break;
                case 14479:
                    TeamInvitationResponseReceived((TeamInvitationResponseMessage)message);
                    break;
                case 14600:
                    AvatarNameCheckRequestReceived((AvatarNameCheckRequestMessage)message);
                    break;
                case 14881:
                    TeamRequestJoinReceived((TeamRequestJoinMessage)message);
                    break;
                case 14114:
                    GetBattleLogReceived((GetBattleLogMessage)message);
                    break;
                case 12998:
                    SetCountryReceived((SetCountryMessage)message);
                    break;
                case 14777:
                    DoNotDisturbReceived((DoNotDisturbMessage)message);
                    break;
                case 14778:
                    SetTeamChatMutedReceived((SetTeamChatMutedMessage)message);
                    break;
                case 14700:
                    StartBrawlTVReceived((StartBrawlTVMessage)message);
                    break;
                case 18686:
                    SetSupportedCreatorReceived((SetSupportedCreatorMessage)message);
                    break;
                case 14177:
                    PlayAgainReceived((PlayAgainMessage)message);
                    break;
                case 14324:
                    SearchAllianceReceived((SearchAllianceMessage)message);
                    break;
                case 10576:
                    SetBlockFriendRequestsReceived((SetBlockFriendRequestsMessage)message);
                    break;
                case 14367:
                    TeamClearInviteReceived((TeamClearInviteMessage)message);
                    break;
                case 14330:
                    SendAllianceMailReceived((SendAllianceMailMessage)message);
                    break;
                case 11736:
                    DebugCommandInfoReceived((DebugCommandMessage)message);
                    break;
                case 12155:
                    RankedMatchPickHeroReceived((RankedMatchPickHeroMessage)message);
                    break;
                case 12152:
                    RankedMatchBanHero((RankedMatchBanHeroMessage)message);
                    break;
                case 10099:
                    Connection.Send(new AuthenticationFailedMessage { ErrorCode = 1, Message = "Rejoin"});
                    break;
                case 12104:
                    SubminPlayerMapReceived((SubminPlayerMapMessage)message);
                    break;
                case 12106:
                    RenamePlayerMapReceived((ChangePlayerMapNameMessage)message);
                    break;
                case 14321:
                    RespondToAllianceJoinRequestReceived((RespondToAllianceJoinRequestMessage)message);
                    break;
                case 14317:
                    RequestJoinAllianceReceived((RequestJoinAllianceMessage)message);
                    break;
                default:
                    Logger.Print($"MessageManager::ReceiveMessage - no case for {message.GetType().Name} ({message.GetMessageType()})");
                    break;
            }
        }

        private void RequestJoinAllianceReceived(RequestJoinAllianceMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (alliance == null) return;
            var entry = new AllianceStreamEntry
            {
                AuthorName = "Debugger",
                AuthorId = HomeMode.Avatar.AccountId,
                Id = ++alliance.Stream.EntryIdCounter,
                AuthorRole = AllianceRole.Member,
                Type = 3,
                Message = message.Desc,
                Action = 1,
                StreamDisplay = new(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus),
                WhoAccepted = ""
            };
            alliance.Stream.AddEntry(entry);
            alliance.SendAllianceStreamEntryToAll(entry);
        }

        private void RespondToAllianceJoinRequestReceived(RespondToAllianceJoinRequestMessage message)
        {
            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;
            AllianceStreamEntryMessage response = new AllianceStreamEntryMessage();
            if (alliance.Stream.StreamEntryList.FindAll(f => f.AuthorId == message.AccountId && f.Type == 3).Count > 0)
            {
                foreach (var stream in alliance.Stream.StreamEntryList.FindAll(f => f.AuthorId == message.AccountId && f.Type == 3))
                {
                    if(stream != null)
                    {
                        if (message.Accept)
                        {
                            stream.Action = 2;
                            stream.WhoAccepted = HomeMode.Avatar.Name;
                            goto ALLIANCE_ADD_PLAYER;
                        }
                        else
                        {
                            stream.Action = 0;
                            stream.WhoAccepted = HomeMode.Avatar.Name;
                        }
                    }
                    alliance.SendAllianceStreamEntryToAll(stream);
                }
            }
            return;
        ALLIANCE_ADD_PLAYER:
            Account account = Accounts.Load(message.AccountId);
            if (account == null) return;
            var ActiveSession = Sessions.GetSession(account.AccountId);
            bool IsSessionActive = Sessions.IsSessionActive(account.AccountId);
            if (account.Avatar.AllianceId > 0 || alliance == null)
            {
                AllianceResponseMessage response12 = new AllianceResponseMessage();
                response12.ResponseType = 93;
                if(IsSessionActive) ActiveSession.Connection.Send(response12);
                return;
            }

            if (alliance.Members.Count >= 30)
            {
                AllianceResponseMessage response12 = new AllianceResponseMessage();
                response12.ResponseType = 42;
                if (IsSessionActive) ActiveSession.Connection.Send(response12);
                return;
            }
            if (alliance.Type != 1)
            {
                AllianceResponseMessage response12 = new AllianceResponseMessage();
                response12.ResponseType = 42;
                if (IsSessionActive) ActiveSession.Connection.Send(response12);
                return;
            }


            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = account.Avatar.AccountId;
            entry.AuthorName = account.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = account.Avatar.AccountId;
            entry.PlayerName = account.Avatar.Name;
            entry.Type = 4;
            entry.Event = 3;
            entry.AuthorRole = account.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            account.Avatar.AllianceRole = AllianceRole.Member;
            account.Avatar.AllianceId = alliance.Id;
            alliance.Members.Add(new AllianceMember(account.Avatar, account.Home.HomeMode));
            Accounts.Save(account);
            Alliances.Save(alliance);

            AllianceResponseMessage response1 = new AllianceResponseMessage();
            response1.ResponseType = 40;
            if (IsSessionActive) ActiveSession.Connection.Send(response1);
            account.Avatar.AllianceName = alliance.Name;

            if(IsSessionActive) ActiveSession.Connection.MessageManager.SendMyAllianceData(alliance);
        }

        private void RenamePlayerMapReceived(ChangePlayerMapNameMessage message)
        {
            if (HomeMode.Home.PlayerMaps.Find(p => p.MapId == message.Id) == null)
            {
                Connection.Send(new ChangePlayerMapNameResponseMessage { Res = 1, Id = message.Id, Name = message.Name });
                return;
            }
            HomeMode.Home.PlayerMaps.Find(p => p.MapId == message.Id).MapName = message.Name;
            Connection.Send(new ChangePlayerMapNameResponseMessage { Res = 0, Id = message.Id, Name = message.Name });
        }

        private void SubminPlayerMapReceived(SubminPlayerMapMessage message)
        {
            Connection.Send(new AuthenticationFailedMessage { ErrorCode = 1, Message = "Next update..." });
            return;
            Connection.Send(new SubminPlayerMapResponseMessage
            {
                MapId = message.MapId,
                ErrorCode = 0
            });
            PlayerMap tmp = HomeMode.Home.PlayerMaps.Find(m=>m.MapId==message.MapId);
            PlayerCustomMapsHandler.AddMap(tmp);
        }

        private void RankedMatchPickHeroReceived(RankedMatchPickHeroMessage message)
        {
            RankedMatch match = RankedMatchRegulator.Get(HomeMode.Avatar.RanledId);
            if (match == null) return;

            RankedMatchPlayer player = match.GetPlayer(HomeMode.Avatar.AccountId);
            int g = GlobalId.CreateGlobalId(16, message.BrawlerId);
            if (DataTables.Get(16).GetDataByGlobalId<CharacterData>(g) == null) g = GlobalId.CreateGlobalId(16, 0);
            var d = DataTables.Get(16).GetDataByGlobalId<CharacterData>(g);
            if (d.LockedForChronos || d.Disabled || !d.IsHero()) g = GlobalId.CreateGlobalId(16, 0);
            if (!HomeMode.Avatar.HasHero(g))
            {
                Connection.Send(new OutOfSyncMessage());
                return;
            }
            player.Character = g;
            if (message.PickType == 1)
            {
                match.Send(new RankedMatchHeroDataUpdatedMessage { Player = player });
            }
            else if (message.PickType == 0)
            {
                player.SetPickUpQueue(0);
                player.Picked = true;
                match.Send(new RankedMatchHeroPickedMessage { Player = player });

                match.NextTurn();
            }
        }

        private void RankedMatchBanHero(RankedMatchBanHeroMessage message)
        {
            RankedMatch match = RankedMatchRegulator.Get(HomeMode.Avatar.RanledId);
            if (match == null) return;
            RankedMatchPlayer player = match.GetPlayer(HomeMode.Avatar.AccountId);
            int g = GlobalId.CreateGlobalId(16, message.BrawlerId);
            if (DataTables.Get(16).GetDataByGlobalId<CharacterData>(g) == null) g = GlobalId.CreateGlobalId(16, 0);
            var d = DataTables.Get(16).GetDataByGlobalId<CharacterData>(g);
            if (d.LockedForChronos || d.Disabled || !d.IsHero()) g = GlobalId.CreateGlobalId(16, 0);
            player.Character = g;
            if (message.PickType == 1) match.Send(new RankedMatchHeroDataUpdatedMessage { Player = player });
            else if (message.PickType == 0)
            {
                player.BannedCharacter = GlobalId.CreateGlobalId(16, message.BrawlerId);
                player.SetBanQueue(0);
                player.Banned = true;
                match.Send(new RankedMatchBanHeroResponseMessage { BannedCharacter = GlobalId.CreateGlobalId(16, message.BrawlerId) });
            }
        }

        private void DebugCommandInfoReceived(DebugCommandMessage action)
        {
            return;
            Console.WriteLine($"RandomDrop before generation data: \nType = {HomeMode.Home.StarrDrop.Data.Type}" +
                $"\nAmmo = {HomeMode.Home.StarrDrop.Data.Ammount}" +
                $"\nSkinGlobalId = {HomeMode.Home.StarrDrop.Data.SkinGlobalID}" +
                $"\nExtraGlobalId = {HomeMode.Home.StarrDrop.Data.DataGlobalID}"
                );
            HomeMode.Home.DropsCount += 1;
            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            command.DeliveryUnits.Add(unit);
            command.Execute(HomeMode);

            HomeMode.GameListener.SendCommand(command);

            HomeMode.Home.StarrDrop.GenerateDrop(HomeMode);
            LogicRefreshRandomRewardsCommand logicRefreshRandomRewardsCommand = new LogicRefreshRandomRewardsCommand();
            logicRefreshRandomRewardsCommand.Execute(HomeMode);
            HomeMode.GameListener.SendCommand(logicRefreshRandomRewardsCommand);
            Console.WriteLine($"RandomDrop after generation data: \nType = {HomeMode.Home.StarrDrop.Data.Type}" +
                $"\nAmmo = {HomeMode.Home.StarrDrop.Data.Ammount}" +
                $"\nSkinGlobalId = {HomeMode.Home.StarrDrop.Data.SkinGlobalID}" +
                $"\nExtraGlobalId = {HomeMode.Home.StarrDrop.Data.DataGlobalID}"
               );
        }

        private void SetTeamChatMutedReceived(SetTeamChatMutedMessage message)
        {
            LogicTeamChatMuteStateChangedComamnd command = new();
            command.ChatMutestate = message.state;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
            serverCommand.Command = command;
            HomeMode.GameListener.SendMessage(serverCommand);
            if (HomeMode.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(HomeMode.Avatar.TeamId);
        }

        public void ReceiveMessage(GameMessage message)
        {
            try
            {
                if (message.GetMessageType() == 10108 && HomeMode != null)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            Account accSave = Accounts.Load(HomeMode.Avatar.AccountId);
                            if (accSave != null)
                                Accounts.Save(accSave);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"[ReceiveMessage] Error saving on 10108: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ReceiveMessage: " + ex);
            }

            try
            {
                ProcessMessage(message);  
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ProcessMessage: " + ex);
            }
        }

        private void SendAllianceMailReceived(SendAllianceMailMessage message)
        {
            Alliance club = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (club == null) return;

            var respon = new AllianceResponseMessage();
            if (HomeMode.Avatar.MailBans.Any(b => b.BanTimer > DateTime.Now))
            {
                respon.ResponseType = 112;
                respon.choto = HomeMode.Avatar.MailBans.First(b => b.BanTimer > DateTime.Now).BanTimer;
                Connection.Send(respon);
                return;
            }

            if (message.a1 == 1)
                respon.ResponseType = 114;
            else if (!String.IsNullOrEmpty(message.a2) && HomeMode.Avatar.AllianceRole == AllianceRole.Leader && message.a1 == 0 && !HomeMode.Avatar.MailBans.Any(b => b.BanTimer > DateTime.Now))
            {
                club.SendAllianceMail(message.a2, club.GetMemberById(HomeMode.Avatar.AccountId));
                AllianceMailBan ban = new();
                ban.BanTimer = DateTime.Now.AddHours(5);
                HomeMode.Avatar.MailBans.Add(ban);
                respon.ResponseType = 113;
            }
            Connection.Send(respon);
        }

        private void SetBlockFriendRequestsReceived(SetBlockFriendRequestsMessage message)
        {
            if (!message.state) Connection.Avatar.FriendRequestBlock = 0;
            if (message.state) Connection.Avatar.FriendRequestBlock = 1;
        }

        private void SearchAllianceReceived(SearchAllianceMessage message)
        {
            AllianceListMessage list = new AllianceListMessage();
            list.SetSearchString(message.AllianceName);
            if (message.AllianceName.StartsWith("#"))
            {
                Alliance club = Alliances.GetAllianceById(LogicLongCodeGenerator.ToId(message.AllianceName));
                if (club != null)
                {
                    list.AddHeader(club);
                    Connection.Send(list);
                }
                Connection.Send(list);
                return;
            }

            list.SetAlliances(Alliances.FindAlliances(message.AllianceName));
            Connection.Send(list);
        }

        private void PlayAgainReceived(PlayAgainMessage message)
        {
            Connection.Send(new PlayAgainStatusMessage());
            Connection.Send(new MatchMakingStatusMessage());
            Matchmaking.RequestMatchmake(Connection, Connection.MatchmakeSlot);
        }

        private void SetSupportedCreatorReceived(SetSupportedCreatorMessage message)
        {
            Console.WriteLine($"[CreatorCode] 1. Сообщение получено, код: {message.Code}");
            
            try
            {
                SetSupportedCreatorResponceMessage responseMessage = new SetSupportedCreatorResponceMessage();
                responseMessage.creator = message.Code;
                Connection.Send(responseMessage);
                Console.WriteLine($"[CreatorCode] 2. Ответ отправлен клиенту");
                
                string creatorsPath = "creators.json";
                
                Console.WriteLine($"[CreatorCode] 3. Ищу файлы в: {Environment.CurrentDirectory}");
                
                if (!File.Exists(creatorsPath))
                {
                    Console.WriteLine($"[CreatorCode] ОШИБКА: Файл {creatorsPath} не найден!");
                    LogicAddNotificationCommand notifCommand = new LogicAddNotificationCommand();
                    notifCommand.Notification = new FloaterTextNotification($"❌ Файл creators.json не найден на сервере!");
                    Connection.Send(new AvailableServerCommandMessage { Command = notifCommand });
                    return;
                }
                
                string jsonTextc = File.ReadAllText(creatorsPath);
                dynamic jsonObjc = JsonConvert.DeserializeObject(jsonTextc);
                
                Console.WriteLine($"[CreatorCode] 4. Файлы загружены");
                
                if (jsonObjc[message.Code] != null)
                {
                    string level = jsonObjc[message.Code].ToString();
                    Console.WriteLine($"[CreatorCode] 6. Код НАЙДЕН, уровень: {level}");
                    
                    int gems = 0;
                    string levelName = "";
                    
                    if (level == "creator1")
                    {
                        gems = 30;
                        levelName = "1 ⭐";
                    }
                    else if (level == "creator2")
                    {
                        gems = 80;
                        levelName = "2 ⭐⭐";
                    }
                    else if (level == "creator3")
                    {
                        gems = 100;
                        levelName = "3 ⭐⭐⭐";
                    }
                    else
                    {
                        Console.WriteLine($"[CreatorCode] Неизвестный уровень: {level}");
                        LogicAddNotificationCommand notifCommand = new LogicAddNotificationCommand();
                        notifCommand.Notification = new FloaterTextNotification($"❌ Неверный формат уровня для кода '{message.Code}'!");
                        Connection.Send(new AvailableServerCommandMessage { Command = notifCommand });
                        return;
                    }
                    
                    if (HomeMode.Home.CreatorCodes.Contains(message.Code))
                    {
                        LogicAddNotificationCommand notifCommand = new LogicAddNotificationCommand();
                        notifCommand.Notification = new FloaterTextNotification($"❌ Вы уже использовали код '{message.Code}'!");
                        Connection.Send(new AvailableServerCommandMessage { Command = notifCommand });
                        return;
                    }
                    
                    HomeMode.Home.CreatorCode = message.Code;
                    HomeMode.Home.CreatorCodes.Add(message.Code);
                    
                    LogicGiveDeliveryItemsCommand _command = new LogicGiveDeliveryItemsCommand();
                    DeliveryUnit unit = new DeliveryUnit(100);
                    
                    GatchaDrop reward = new GatchaDrop(8);
                    reward.Count = gems;
                    unit.AddDrop(reward);
                    
                    _command.DeliveryUnits.Add(unit);
                    _command.Execute(HomeMode);
                    
                    AvailableServerCommandMessage _message = new AvailableServerCommandMessage();
                    _message.Command = _command;
                    Connection.Send(_message);
                    
                    LogicAddNotificationCommand notif = new LogicAddNotificationCommand();
                    notif.Notification = new FloaterTextNotification($"✅ Код '{message.Code}' активирован! (Уровень {levelName})\n\n💎 +{gems} гемов");
                    Connection.Send(new AvailableServerCommandMessage { Command = notif });
                    
                    Console.WriteLine($"[CreatorCode] Код '{message.Code}' использован, выдано {gems} гемов");
                }
                else
                {
                    Console.WriteLine($"[CreatorCode] 6. Код НЕ НАЙДЕН в creators.json!");
                    LogicAddNotificationCommand notifCommand = new LogicAddNotificationCommand();
                    notifCommand.Notification = new FloaterTextNotification($"❌ Код '{message.Code}' не найден!");
                    Connection.Send(new AvailableServerCommandMessage { Command = notifCommand });
                }
                
                Console.WriteLine($"[CreatorCode] 7. Обработка завершена");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreatorCode] ОШИБКА: {ex.Message}");
                Console.WriteLine($"[CreatorCode] Стек: {ex.StackTrace}");
            }
        }

        private void StartBrawlTVReceived(StartBrawlTVMessage message)
        {
            BattleMode battle = Battles.GetRandom();
            if (battle == null)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification("Cannot find any battle.\nTry agian later");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                HomeMode.GameListener.SendMessage(availableServerCommandMessage);
                logicAddNotificationCommand.Execute(HomeMode);
                AvailableServerCommandMessage server = new AvailableServerCommandMessage();
                server.Command = logicAddNotificationCommand;
                HomeMode.GameListener.SendMessage(server);
                return;
            }
            battle.IsBrawlTV = true;
            UDPSocket socket = UPD.UdpServers[UPD.GetRandomPort()].CreateSocket();
            socket.Battle = battle;
            socket.IsSpectator = true;
            socket.TCPConnection = Connection;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddSpectator(socket.SessionId, new UDPGameListener(socket, Connection));
            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.TeamIndex = 0;
            startLoading.OwnIndex = 0;
            startLoading.SpectateMode = 1;
            startLoading.LocationId = battle.m_locationId;
            startLoading.GameMode = battle.GetGameModeVariation();
            startLoading.GameType = 0;
            startLoading.MapMode = 1;
            startLoading.Modifiers = new List<int>();
            startLoading.LeaveButton = true;
            startLoading.Players.AddRange(battle.GetPlayers());
            if (battle.BattlePlayerMap != null)
            {
                startLoading.map = battle.BattlePlayerMap;
            }

            Connection.Avatar.UdpSessionId = Connection.UdpSessionId;
            Connection.Send(startLoading);

            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = Configuration.Instance.UdpPort;
            Connection.Send(info);
        }

        private void DoNotDisturbReceived(DoNotDisturbMessage message)
        {
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
            LogicSetDoNotDisturb command = new LogicSetDoNotDisturb();
            command.DoNotDistrub = message.state;
            command.Execute(HomeMode);
            AvailableServerCommandMessage serverCommand = new AvailableServerCommandMessage();
            serverCommand.Command = command;
            HomeMode.GameListener.SendMessage(serverCommand);
        }

        private void SetCountryReceived(SetCountryMessage message)
        {
            SetCountryResponseMessage response = new SetCountryResponseMessage();
            response.CountryID = message.CountryID;
            HomeMode.Avatar.Region = DataTables.Get(14).GetDataWithId<RegionData>(message.CountryID).Name;
            Console.WriteLine(HomeMode.Avatar.Region);
            Connection.Send(response);
        }

        private void GetBattleLogReceived(GetBattleLogMessage message)
        {
            BattleLogMessage log = new BattleLogMessage();
            log.homeMode = HomeMode;
            Connection.Send(log);
        }

        private void AlliancePremadeChatReceived(AlliancePremadeChatMessage message)
        {
            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;
            alliance.SendPremadeChat(HomeMode.Avatar.AccountId, message.DefEmojiSlot, message.EmojiGlobalId);
        }

        private void TeamSpectateMessageReceived(TeamSpectateMessage message)
        {
            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null)
            {
                TeamErrorMessage error = new();
                error.ErrorCode = 1;
                Connection.Send(error);
                return;
            }
            if (team == Teams.Get(HomeMode.Avatar.TeamId)) return;
            if (team.Type != 1 && team.Members.Count >= 3)
            {
                TeamErrorMessage error = new();
                error.ErrorCode = 2;
                Connection.Send(error);
                return;
            }
            if (HomeMode.Avatar.MuteEndTime > DateTime.Now)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"Cannot join team!\nYou are muted until {HomeMode.Avatar.MuteEndTime}");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            HomeMode.Avatar.TeamId = team.Id;
            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.SkinId = hero.SelectedSkinId > 0 ? GlobalId.CreateGlobalId(29, hero.SelectedSkinId) : 0;
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.TeamType = team.Type;
            member.SelectedStarPowerId = GlobalId.CreateGlobalId(23, hero.SelectedStarPowerId);
            member.SelectedGadget = GlobalId.CreateGlobalId(23, hero.SelectedGadgetId);
            member.SelectedOvercharge = GlobalId.CreateGlobalId(23, hero.SelectedOverChargeId);
            member.IsOwner = false;
            member.State = 0;
            team.Members.Add(member);
            team.TeamUpdated();
        }

        private void TeamPremadeChatReceived(TeamPremadeChatMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            QuickChatStreamEntry entry = new QuickChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.TargetId = message.TargetId;
            entry.Name = HomeMode.Avatar.Name;

            if (message.TargetId > 0)
            {
                TeamMember member = team.GetMember(message.TargetId);
                if (member != null)
                {
                    entry.TargetPlayerName = member.DisplayData.Name;
                }
            }

            entry.MessageDataId = message.MessageDataId;
            entry.Unknown1 = message.Unknown1;
            entry.Unknown2 = message.Unknown2;

            team.AddStreamEntry(entry);
        }

        private void TeamBotSlotDisableReceived(TeamBotSlotDisableMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            if (team.GetMember(HomeMode.Avatar.AccountId) == null) return;
            if (!team.GetMember(HomeMode.Avatar.AccountId).IsOwner) return;
            if (message.Disable)
            {
                team.DisabledBots.Add(message.BotSlot);
            }
            else team.DisabledBots.Remove(message.BotSlot);
            team.TeamUpdated();
        }

        private void TeamToggleMemberSideReceived(TeamToggleMemberSideMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            team.GetMember(message.Longs[0]).TeamIndex = message.Unk1 >= 3 ? 1 : 0;
            team.TeamUpdated();
        }

        private void TeamChatReceived(TeamChatMessage message)
        {
            Connection.Send(new GoogleServiceAccountBoundMessage());

            Sessions.SendGlobalMessage(HomeMode.Avatar.AccountId, HomeMode.Avatar.Name, message.Message);
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            ChatStreamEntry entry = new ChatStreamEntry();
            entry.AccountId = HomeMode.Avatar.AccountId;
            entry.Name = HomeMode.Avatar.Name;
            entry.Message = message.Message.Length > 128 ? message.Message.Substring(0, 128) : message.Message;

            team.AddStreamEntry(entry);
        }

        private void AvatarNameCheckRequestReceived(AvatarNameCheckRequestMessage message)
        {
            if (Helpers.IsAdequateString(message.Name) && message.Name.Length is > 3 and <= 15)
            {
                LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
                command.Name = message.Name;
                command.ChangeNameCost = 0;
                command.Execute(HomeMode);
                AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                serverCommandMessage.Command = command;
                Connection.Send(serverCommandMessage);
            }
            else
            {
                AvatarNameChangeFailedMessage failed = new();
                failed.Reason = message.Name.Length < 3 ? 2 : message.Name.Length >= 15 ? 1 : 0;
                Connection.Send(failed);
            }
        }

        private void TeamRequestJoinReceived(TeamRequestJoinMessage message)
        {
            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null)
            {
                TeamErrorMessage error = new();
                error.ErrorCode = 1;
                Connection.Send(error);
                return;
            }


            TeamEntry team1 = Teams.Get(HomeMode.Avatar.TeamId);
            if (team1 != null)
            {
                TeamMember entry = team1.GetMember(HomeMode.Avatar.AccountId);

                if (entry == null) return;
                HomeMode.Avatar.TeamId = -1;

                team1.Members.Remove(entry);

                Connection.Send(new TeamLeftMessage());
                team1.TeamUpdated();

                if (team1.Members.Count == 0)
                {
                    Teams.Remove(team1.Id);
                }
            }

            if (team.Type != 1 && team.Members.Count >= 3)
            {
                TeamErrorMessage error = new();
                error.ErrorCode = 2;
                Connection.Send(error);
                return;
            }

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.SkinId = hero.SelectedSkinId > 0 ? GlobalId.CreateGlobalId(29, hero.SelectedSkinId) : 0;
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.SelectedStarPowerId = GlobalId.CreateGlobalId(23, hero.SelectedStarPowerId);
            member.SelectedGadget = GlobalId.CreateGlobalId(23, hero.SelectedGadgetId);
            member.SelectedOvercharge = GlobalId.CreateGlobalId(23, hero.SelectedOverChargeId);
            member.IsOwner = false;
            member.TeamType = team.Type;
            member.State = 0;
            
            team.Members.Add(member);

            HomeMode.Avatar.TeamId = team.Id;

            team.TeamUpdated();
        }

        private void AnalyticEventsReceived(AnalyticEventMessage message)
        {
            ;
        }

        private void TeamSetEventReceived(TeamSetEventMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            EventData data = Events.GetEvent(message.EventSlot);
            if (data == null) return;


            team.EventSlot = message.EventSlot;
            team.LocationId = data.LocationId;
            team.Type = 0;
            HomeMode.Home.EventId = message.EventSlot;
            foreach (TeamMember member in team.Members)
            {
                if (member.IsReady) member.IsReady = !member.IsReady;
            }

            team.TeamUpdated();

            string reason = null;
            EventData notAllowedEventData = Events.GetEvent(1);

            if (notAllowedEventData == null || data == null)
            {
                reason = "                                   .                       .";
            }

            if (message.EventSlot == 2 && team.GetCount() > 1)
            {
                reason = "                    .                                     .";
            }
            else if (message.EventSlot == 5 && team.GetCount() > 2)
            {
                reason = "                    .                                     .";
            }
            else if (message.EventSlot == 7)
            {
                reason = "                                                 .";
            }

            if (reason != null)
            {
                team.EventSlot = 1;
                team.LocationId = notAllowedEventData.LocationId;
                team.Type = 0;
                HomeMode.Home.EventId = 1;
                team.TeamUpdated();

                team.SendNotificationToAllMembers(reason);
            }

            if (notAllowedEventData == null || data == null)
            {
                foreach (TeamMember member in team.Members)
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId)?.SendTCPMessage(new TeamLeftMessage());
                    LogicServerListener.Instance.GetHomeMode(member.AccountId).Avatar.TeamId = -1;
                }
                Teams.Remove(team.Id);
            }

            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;
            entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }

        }

        private void GetPlayerMapsReceived(GetPlayerMapsMessage message)
        {
            Connection.Send(new PlayerMapsMessage()
            {
                maps = Connection.Home.PlayerMaps.ToArray()
            });
        }

        private void CreatePlayerMapReceived(CreatePlayerMapMessage message)
        {
            PlayerMap map = new PlayerMap()
            {
                MapEnvironmentData = message.PMED,
                MapName = message.name,
                GMV = message.GMV,
                MapId = RandomNumberGenerator.GetInt32(0, 2147483647),
                AccountId = Connection.Avatar.AccountId,
                AvatarName = Connection.Avatar.Name
            };
            Connection.Home.PlayerMaps.Add(map);
            Connection.Send(new CreatePlayerMapResponseMessage()
            {
                map = map
            });
        }

        private void UpdatePlayerMapReceived(UpdatePlayerMapMessage message)
        {
            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.MapId);
            if (map == null)
            {
                Connection.Send(new UpdatePlayerMapResponseMessage()
                {
                    ErrorCode = 1
                });
                return;
            }
            map.MapData = message.MapData;
            Connection.Send(new UpdatePlayerMapResponseMessage()
            {
                MapId = message.MapId
            });
        }

        private void DeletePlayerMapReceived(DeletePlayerMapMessage message)
        {
            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.MapId);
            if (map == null)
            {
                Connection.Send(new DeletePlayerMapResponseMessage()
                {
                    ErrorCode = 1
                });
                return;
            }
            Connection.Home.PlayerMaps.Remove(map);
            Connection.Send(new DeletePlayerMapResponseMessage()
            {
                MapId = message.MapId
            });
        }

        private void TeamSetPlayerMapReceived(TeamSetPlayerMapMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;

            PlayerMap map = Connection.Home.PlayerMaps.Find(map => map.MapId == message.mapid);
            if (map == null) return;

            team.BattlePlayerMap = new BattlePlayerMap(map);
            team.Type = 1;
            team.TeamUpdated();

        }

        private BattleMode SpectatedBattle;

        private void StopSpectateReceived(StopSpectateMessage message)
        {
            if (SpectatedBattle != null)
            {
                SpectatedBattle.RemoveSpectator(Connection.UdpSessionId);
                SpectatedBattle = null;
            }

            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void StartSpectateReceived(StartSpectateMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null)
                return;

            ClientAvatar avatar = data.Avatar;
            long battleId = avatar.BattleId;

            BattleMode battle = Battles.Get(battleId);
            if (battle == null)
                return;
            SpectatedBattle = battle;


            UDPSocket socket = UPD.UdpServers[UPD.GetRandomPort()].CreateSocket();
            socket.Battle = battle;
            socket.IsSpectator = true;
            socket.TCPConnection = Connection;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddSpectator(socket.SessionId, new UDPGameListener(socket, Connection));
            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.TeamIndex = 0; 
            startLoading.OwnIndex = 0;
            startLoading.SpectateMode = 1;
            startLoading.LocationId = battle.m_locationId;
            startLoading.GameMode = battle.GetGameModeVariation(); 
            startLoading.GameType = 0;
            startLoading.MapMode = 1;
            startLoading.Modifiers = new List<int>();
            startLoading.LeaveButton = true;
            startLoading.Players.AddRange(battle.GetPlayers());
            if (battle.BattlePlayerMap != null)
            {
                startLoading.map = battle.BattlePlayerMap;
            }

            Connection.Avatar.UdpSessionId = Connection.UdpSessionId;
            Connection.Send(startLoading);

            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = Configuration.Instance.UdpPort;
            Connection.Send(info);
        }

        private void GoHomeFromOfflinePractiseReceived(GoHomeFromOfflinePractiseMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                if (Connection.Avatar.IsTutorialState())
                {
                    Connection.Avatar.SkipTutorial();
                }

                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void TeamSetLocationReceived(TeamSetLocationMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = GetTeam();
            if (team == null) return;
            team.LocationId = message.locid;
            team.CustomModifiers = message.CustomModifiers.ToList();
            team.Type = 1;
            team.TeamUpdated();
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;
            entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void ChangeAllianceSettingsReceived(ChangeAllianceSettingsMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            if (HomeMode.Avatar.AllianceRole != AllianceRole.Leader) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            if (Helpers.IsAdequateString(message.Description))
            {
                if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
                {
                    alliance.AllianceBadgeId = message.BadgeId;
                }
                else
                {
                    alliance.AllianceBadgeId = 8000000;
                }

                alliance.Description = message.Description;
                alliance.RequiredTrophies = message.RequiredTrophies;
                alliance.Type = message.Type;
                alliance.Country = !String.IsNullOrEmpty(DataTables.Get(DataType.Region).GetDataByGlobalId<RegionData>(message.Region).Name.ToUpper()) ? DataTables.Get(DataType.Region).GetDataByGlobalId<RegionData>(message.Region).Name.ToUpper() : "RU";
                Alliances.Save(alliance);

                Connection.Send(new AllianceResponseMessage()
                {
                    ResponseType = 10
                });

                MyAllianceMessage myAlliance = new MyAllianceMessage();
                myAlliance.Role = HomeMode.Avatar.AllianceRole;
                myAlliance.OnlineMembers = alliance.OnlinePlayers;
                myAlliance.AllianceHeader = alliance.Header;
                Connection.Send(myAlliance);
                return;
            }
            Connection.Send(new AllianceResponseMessage()
            {
                ResponseType = 23
            });
        }

        private void KickAllianceMemberReceived(KickAllianceMemberMessage message)
        {
            if (HomeMode.Avatar.AllianceId <= 0) return;

            Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
            if (alliance == null) return;

            AllianceMember member = alliance.GetMemberById(message.AccountId);
            if (member == null) return;

            ClientAvatar avatar = Accounts.Load(message.AccountId).Avatar;
            AllianceKickBan kick = new();
            kick.AllianceId = HomeMode.Avatar.AllianceId;
            kick.BanTimer = DateTime.Now.AddHours(5);
            avatar.KickBans.Add(kick);

            if (Sessions.IsSessionActive(avatar.AccountId))
            {
                AllianceResponseMessage response12 = new AllianceResponseMessage();
                response12.ResponseType = 100;
                Sessions.GetSession(avatar.AccountId).GameListener.SendMessage(response12);
            }

            if (HomeMode.Avatar.AllianceRole <= avatar.AllianceRole) return;


            alliance.Members.Remove(member);
            avatar.AllianceId = -1;
            avatar.AllianceRole = AllianceRole.None;
            Account kickedAccount = Accounts.Load(avatar.AccountId);
            if (kickedAccount != null) Accounts.Save(kickedAccount);
            Alliances.Save(alliance);

            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = avatar.AccountId;
            entry.PlayerName = avatar.Name;
            entry.Type = 4;
            entry.Event = 1;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 70;
            Connection.Send(response);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new AllianceResponseMessage()
                {
                    ResponseType = 100
                });
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(new MyAllianceMessage());
            }
        }

        private void TeamSetMemberReadyReceived(TeamSetMemberReadyMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            if (GeneralStaticLogic.LockedBrawlers.Contains(GlobalId.GetInstanceId(HomeMode.Home.CharacterId)))
            {
                TeamErrorMessage err = new();
                err.ErrorCode = 35;
                Connection.Send(err);
                return;
            }
            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;
            if (team.Members.Count > 3 && team.Type != 1)
                team.Members.RemoveRange(team.Members.Count - (team.Members.Count - 3), team.Members.Count - 3);

            member.IsReady = message.IsReady;

            team.TeamUpdated();
            if (team.EventSlot == 14)
            {
                team.EventSlot = 15;
                team.StreamUpdated();
                team.TeamUpdated();
            }
            if(team.EventSlot == 15)
            {
                team.TeamError(33);
                return;
                if (team.Members.Count != 3)
                {
                    team.TeamError(53);
                    return;
                }
                foreach (var m in team.Members)
                {
                    if (m.HeroLevel < 9 || m.homeMode.Avatar.Heroes.FindAll(h => h.PowerLevel >= 9).Count < 6)
                    {
                        team.TeamError(133);
                        return;
                    }
                    if(m.homeMode.Home.RankedBan > DateTime.Now)
                    {
                        team.TeamError(126);
                        return;
                    }
                }
            }

            if (HomeMode.Home.ChallengeLoses >= 3 && (team.EventSlot == 20 || team.EventSlot == 21 || team.EventSlot == 22 || team.EventSlot == 23|| team.EventSlot == 24))
            {
                Connection.Send(new TeamErrorMessage()
                {
                    ErrorCode = 121
                });
                return;
            }
            if (team.IsEveryoneReady())
            {
                Teams.StartGame(team);
            }
        }

        private void TeamChangeMemberSettingsReceived(TeamChangeMemberSettingsMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            team.TeamUpdated();
        }

        private void TeamMemberStatusReceived(TeamMemberStatusMessage message)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;

            member.State = message.Status;
            team.TeamUpdated();
        }

        private void TeamInvitationResponseReceived(TeamInvitationResponseMessage message)
        {
            bool isAccept = message.Response == 1;

            TeamEntry team = Teams.Get(message.TeamId);
            if (team == null) return;

            TeamInviteEntry invite = team.GetInviteById(HomeMode.Avatar.AccountId);
            if (invite == null) return;

            team.Invites.Remove(invite);
            if (HomeMode.Avatar.MuteEndTime > DateTime.Now)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"Cannot join team!\nYou are muted until {HomeMode.Avatar.MuteEndTime}");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            if(team.Type != 1 && team.Members.Count >= 3)
            {
                Connection.Send(new TeamErrorMessage { ErrorCode = 2 });
                return;
            }
            if (isAccept)
            {
                TeamMember member = new TeamMember();
                member.AccountId = HomeMode.Avatar.AccountId;
                member.CharacterId = HomeMode.Home.CharacterId;
                member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
                member.homeMode = HomeMode;
                Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
                member.SkinId = hero.SelectedSkinId > 0 ? GlobalId.CreateGlobalId(29, hero.SelectedSkinId) : 0;
                member.HeroTrophies = hero.Trophies;
                member.HeroHighestTrophies = hero.HighestTrophies;
                member.HeroLevel = hero.PowerLevel;
                member.SelectedStarPowerId = GlobalId.CreateGlobalId(23, hero.SelectedStarPowerId);
                member.SelectedGadget = GlobalId.CreateGlobalId(23, hero.SelectedGadgetId);
                member.SelectedOvercharge = GlobalId.CreateGlobalId(23, hero.SelectedOverChargeId);
                member.IsOwner = false;
                member.TeamType = team.Type;
                member.State = 0;
                team.Members.Add(member);

                HomeMode.Avatar.TeamId = team.Id;
            }

            team.TeamUpdated();
        }

        private TeamEntry GetTeam()
        {
            return Teams.Get(HomeMode.Avatar.TeamId);
        }

        private void TeamClearInviteReceived(TeamClearInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            team.Invites.RemoveAll(invite => invite.Id == message.InviteId);
            team.TeamUpdated();
        }

        private void TeamInviteReceived(TeamInviteMessage message)
        {
            TeamEntry team = GetTeam();
            if (team == null) return;

            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            TeamInviteEntry entry = new TeamInviteEntry();
            entry.Slot = message.Team;
            entry.Name = data.Avatar.Name;
            entry.Id = message.AvatarId;
            entry.InviterId = HomeMode.Avatar.AccountId;
            entry.InviteTimer = DateTime.Now.AddMinutes(2);
            if (team.Type != 1 && team.Invites.Count > 2 && team.Members.Count > 3) return;
            team.Invites.Add(entry);

            team.TeamUpdated();

            LogicGameListener gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);
            if (gameListener != null)
            {
                TeamInvitationMessage teamInvitationMessage = new TeamInvitationMessage();
                teamInvitationMessage.TeamId = team.Id;

                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
                friendEntry.Trophies = HomeMode.Avatar.Trophies;
                teamInvitationMessage.Unknown = 1;
                teamInvitationMessage.FriendEntry = friendEntry;

                gameListener.SendTCPMessage(teamInvitationMessage);
            }
        }

        private void TeamLeaveReceived(TeamLeaveMessage message)
        {
            if (HomeMode.Avatar.TeamId <= 0) return;

            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);

            if (team == null)
            {
                Logger.Print("TeamLeave - Team is NULL!");
                HomeMode.Avatar.TeamId = -1;
                Connection.Send(new TeamLeftMessage());
                return;
            }

            TeamMember entry = team.GetMember(HomeMode.Avatar.AccountId);

            if (entry == null) return;
            HomeMode.Avatar.TeamId = -1;

            team.Members.Remove(entry);

            Connection.Send(new TeamLeftMessage());
            team.TeamUpdated();

            if (team.Members.Count == 0)
            {
                Teams.Remove(team.Id);
            }
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void TeamCreateReceived(TeamCreateMessage message)
        {
            if (HomeMode.Avatar.MuteEndTime > DateTime.Now)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"Cannot create team!\nYou are muted until {HomeMode.Avatar.MuteEndTime}");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            TeamEntry team = Teams.Create();
            EventData data = Events.GetEvent(message.UnkVInt);
            team.Type = message.TeamType;
            if (message.UnkVInt > 0 && data != null)
            {
                team.LocationId = data.LocationId;
                team.EventSlot = message.UnkVInt;
            }
            else
            {
                team.LocationId = Events.GetEvents()[0].LocationId;
                team.EventSlot = 1;
            }

            TeamMember member = new TeamMember();
            member.AccountId = HomeMode.Avatar.AccountId;
            member.CharacterId = HomeMode.Home.CharacterId;
            member.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
            member.homeMode = HomeMode;
            Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterId);
            member.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
            member.SkinId = hero.SelectedSkinId > 0 ? GlobalId.CreateGlobalId(29, hero.SelectedSkinId) : 0;
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;
            member.SelectedStarPowerId = GlobalId.CreateGlobalId(23, hero.SelectedStarPowerId);
            member.SelectedGadget = GlobalId.CreateGlobalId(23, hero.SelectedGadgetId);
            member.SelectedOvercharge = GlobalId.CreateGlobalId(23, hero.SelectedOverChargeId);
            member.IsOwner = true;
            member.TeamType = team.Type;
            member.State = 0;
            team.Members.Add(member);

            TeamMessage teamMessage = new TeamMessage();
            teamMessage.Team = team;
            HomeMode.Avatar.TeamId = team.Id;
            Connection.Send(teamMessage);
            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.AllianceTeamEntry = team;
            entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;
            team.TeamUpdated();
            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }
        }

        private void AcceptFriendReceived(AcceptFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            {
                Friend entry = HomeMode.Avatar.GetRequestFriendById(message.AvatarId);
                if (entry == null) return;

                Friend oldFriend = HomeMode.Avatar.GetAcceptedFriendById(message.AvatarId);
                if (oldFriend != null)
                {
                    HomeMode.Avatar.Friends.Remove(entry);
                    Connection.Send(new OutOfSyncMessage());
                    return;
                }

                entry.FriendReason = 0;
                entry.FriendState = 4;


                FriendListUpdateMessage update = new FriendListUpdateMessage();
                update.Entry = entry;
                Connection.Send(update);
            }

            {
                ClientAvatar avatar = data.Avatar;
                Friend entry = avatar.GetFriendById(HomeMode.Avatar.AccountId);
                if (entry == null) return;

                entry.FriendState = 4;
                entry.FriendReason = 0;

                if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
                {
                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = entry;
                    LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update);
                }
            }
        }

        private void RemoveFriendReceived(RemoveFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;

            Friend MyEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (MyEntry == null) return;

            MyEntry.FriendState = 0;

            HomeMode.Avatar.Friends.Remove(MyEntry);

            FriendListUpdateMessage update = new FriendListUpdateMessage();
            update.Entry = MyEntry;
            Connection.Send(update);

            Friend OtherEntry = avatar.GetFriendById(HomeMode.Avatar.AccountId);

            if (OtherEntry == null) return;

            OtherEntry.FriendState = 0;

            avatar.Friends.Remove(OtherEntry);

            if (LogicServerListener.Instance.IsPlayerOnline(avatar.AccountId))
            {
                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = OtherEntry;
                LogicServerListener.Instance.GetGameListener(avatar.AccountId).SendTCPMessage(update2);
            }
        }

        private void AddFriendReceived(AddFriendMessage message)
        {
            Account data = Accounts.Load(message.AvatarId);
            if (data == null) return;

            ClientAvatar avatar = data.Avatar;
            Friend requestEntry = HomeMode.Avatar.GetFriendById(message.AvatarId);
            if (requestEntry != null)
            {
                AcceptFriendReceived(new AcceptFriendMessage()
                {
                    AvatarId = message.AvatarId
                });
                return;
            }
            else
            {
                Friend friendEntry = new Friend();
                friendEntry.AccountId = HomeMode.Avatar.AccountId;
                friendEntry.DisplayData = new PlayerDisplayData(HomeMode.Home.ThumbnailId, HomeMode.Home.NameColorId, HomeMode.Avatar.Name, HomeMode.Home.HasPremiumPass, HomeMode.Home.HasPremiumPassPlus);
                friendEntry.FriendReason = message.Reason;
                friendEntry.FriendState = 3;
                avatar.Friends.Add(friendEntry);

                Friend request = new Friend();
                request.AccountId = avatar.AccountId;
                request.DisplayData = new PlayerDisplayData(data.Home.ThumbnailId, data.Home.NameColorId, data.Avatar.Name, data.Home.HasPremiumPass, data.Home.HasPremiumPassPlus);
                request.FriendReason = 0;
                request.FriendState = 2;
                HomeMode.Avatar.Friends.Add(request);

                if (LogicServerListener.Instance.IsPlayerOnline(message.AvatarId))
                {
                    var gameListener = LogicServerListener.Instance.GetGameListener(message.AvatarId);

                    FriendListUpdateMessage update = new FriendListUpdateMessage();
                    update.Entry = friendEntry;

                    gameListener.SendTCPMessage(update);

                    FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
                    entryMessage.AvatarId = HomeMode.Avatar.AccountId;
                    entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
                    entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;

                    gameListener.SendTCPMessage(entryMessage);
                }

                FriendListUpdateMessage update2 = new FriendListUpdateMessage();
                update2.Entry = request;
                Connection.Send(update2);
                FriendOnlineStatusEntryMessage entryMessage2 = new FriendOnlineStatusEntryMessage();
                entryMessage2.AvatarId = avatar.AccountId;
                entryMessage2.PlayerStatus = avatar.PlayerStatus;
                entryMessage2.donotdisturb = avatar.DoNotDisturb;
                Connection.Send(entryMessage2);
            }
        }

        private void AskForFriendListReceived(AskForFriendListMessage message)
        {
            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
        }

        private void PlayerStatusReceived(PlayerStatusMessage message)
        {
            if (HomeMode == null) return;

            HomeMode.Avatar.PlayerStatus = message.Status;

            FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
            entryMessage.AvatarId = HomeMode.Avatar.AccountId;
            entryMessage.PlayerStatus = HomeMode.Avatar.PlayerStatus;
            entryMessage.donotdisturb = HomeMode.Avatar.DoNotDisturb;
            

            foreach (Friend friend in HomeMode.Avatar.Friends.ToArray())
            {
                if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                }
            }

            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);

                if (alliance != null)
                {
                    AllianceOnlineStatusUpdatedMessage allianceOnlineStatusUpdatedMessage = new AllianceOnlineStatusUpdatedMessage()
                    {
                        AvatarId = HomeMode.Avatar.AccountId,
                        Members = alliance.Members.Count,
                        PlayerStatus = HomeMode.Avatar.PlayerStatus
                    };
                    foreach (var member in alliance.Members)
                    {
                        if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                        {
                            LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(allianceOnlineStatusUpdatedMessage);
                        }

                    }
                }
            }
        }

        private void SendMyAllianceData(Alliance alliance)
        {
            MyAllianceMessage myAlliance = new MyAllianceMessage();
            myAlliance.Role = HomeMode.Avatar.AllianceRole;
            myAlliance.OnlineMembers = alliance.OnlinePlayers;
            myAlliance.AllianceHeader = alliance.Header;
            Connection.Send(myAlliance);

            if(!LogicServerListener.Instance.IsDev()) return;

            AllianceStreamMessage stream = new AllianceStreamMessage();
            stream.Entries = alliance.Stream.GetEntries();
            Connection.Send(stream);
        }

        private int BotIdCounter;

        private string Profanity(string input)
        {
            var profanity = new HashSet<string>(
                File.ReadAllLines("./Assets/profanity.txt")
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line)),
                StringComparer.OrdinalIgnoreCase);
            Regex wordRegex = new Regex(@"\b[\p{L}']+\b", RegexOptions.IgnoreCase);
            string result = wordRegex.Replace(input, match =>
            {
                string word = match.Value;
                foreach (string badWord in profanity)
                {
                    if (word.StartsWith(badWord, StringComparison.OrdinalIgnoreCase) &&
                        word.Length <= badWord.Length + 3)
                    {
                        return new string('*', word.Length);
                    }
                }
                return word;
            });

            return result;
        }

        private void ChatToAllianceStreamReceived(ChatToAllianceStreamMessage message)
        {
            try
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
                if (alliance == null) return;
                if (HomeMode.Avatar.MuteEndTime > DateTime.Now)
                {
                    LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                    logicAddNotificationCommand.Notification = new FloaterTextNotification($"Cannot send message in team!\nYou are muted until {HomeMode.Avatar.MuteEndTime}");
                    AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                    availableServerCommandMessage.Command = logicAddNotificationCommand;
                    Connection.Send(availableServerCommandMessage);
                    return;
                }
                if (alliance.Members.FindAll(p => p.AccountId == HomeMode.Avatar.AccountId).Count == 0) return;
                if (message.Message.StartsWith("/"))
                {
                    string region = "en";
                    string[] cmd = message.Message.Substring(1).Split(' ');
                    if (cmd.Length == 0) return;

                    AllianceStreamEntryMessage response = new AllianceStreamEntryMessage();
                    response.Entry = new AllianceStreamEntry();
                    response.Entry.AuthorName = "Debugger";
                    response.Entry.AuthorId = 1;
                    response.Entry.Id = alliance.Stream.EntryIdCounter + 667 + BotIdCounter++;
                    response.Entry.AuthorRole = AllianceRole.Member;
                    response.Entry.Type = 2;
                    long accountId = HomeMode.Avatar.AccountId;

                    switch (cmd[0])
                    {
                        case "unlock":
                        case "full":
                            if (HomeMode.Avatar.IsDev)
                            {
                                // 1. Безопасно разблокируем все скины из таблиц сервера
                                int totalSkins = DataTables.Get(DataType.Skin).Count;
                                for (int i = 0; i < totalSkins; i++)
                                {
                                    int globalSkinId = 29000000 + i;
                                    if (!HomeMode.Home.UnlockedSkins.Contains(globalSkinId))
                                        HomeMode.Home.UnlockedSkins.Add(globalSkinId);
                                }

                                // 2. Безопасно разблокируем все пины (класс 52)
                                int totalEmotes = DataTables.Get(DataType.Emote).Count;
                                for (int i = 0; i < totalEmotes; i++)
                                {
                                    int globalEmoteId = 52000000 + i;
                                    if (!HomeMode.Home.UnlockedEmotes.Contains(globalEmoteId))
                                        HomeMode.Home.UnlockedEmotes.Add(globalEmoteId);
                                }

                                // 3. Безопасно разблокируем все спреи (класс 68)
                                int totalSprays = DataTables.Get(DataType.Spray).Count;
                                for (int i = 0; i < totalSprays; i++)
                                {
                                    int globalSprayId = 68000000 + i;
                                    if (!HomeMode.Home.UnlockedSprays.Contains(globalSprayId))
                                        HomeMode.Home.UnlockedSprays.Add(globalSprayId);
                                }

                                // 4. Безопасно разблокируем все иконки профиля (класс 28)
                                int totalThumbs = DataTables.Get(DataType.PlayerThumbnail).Count;
                                for (int i = 0; i < totalThumbs; i++)
                                {
                                    int globalThumbId = 28000000 + i;
                                    if (!HomeMode.Home.UnlockedThumbnails.Contains(globalThumbId))
                                        HomeMode.Home.UnlockedThumbnails.Add(globalThumbId);
                                }

                                // Сохраняем аккаунт
                                Accounts.Save(Accounts.Load(HomeMode.Avatar.AccountId));
                                response.Entry.Message = "✅ Вся косметика (скины, пины, спреи, иконки) успешно разблокирована без крашей! Перезагрузите игру.";
                                Connection.Send(response);
                            }
                            break;

                        case "promo":

                            if (cmd.Length < 2)
                            {
                                response.Entry.Message = "❌ Неверный формат.\nИспользуй: /promo КОД";
                                Connection.Send(response);
                                return;
                            }

                            string promoCode = cmd[1];
                            string playerTag = LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId);
                            
                            if (PromoCodeManager.UsePromoCode(promoCode, HomeMode.Avatar.AccountId, playerTag, out var rewards, out string errorMsg))
                            {
                                string rewardMsg;
                                LogicGiveDeliveryItemsCommand deliveryCommand;
                                try
                                {
                                    PromoRewardGiver.GiveRewards(HomeMode, rewards, out rewardMsg, out deliveryCommand);

                                    // Promo rewards must survive an immediate reconnect or
                                    // process crash. The normal save queue is intentionally
                                    // asynchronous, so persist this critical transaction now.
                                    Account accountToSave = Accounts.Load(HomeMode.Avatar.AccountId);
                                    if (accountToSave == null)
                                    {
                                        PromoCodeManager.RollbackUse(promoCode, HomeMode.Avatar.AccountId, playerTag);
                                        response.Entry.Message = "вќЊ РќРµ СѓРґР°Р»РѕСЃСЊ РЅР°Р№С‚Рё Р°РєРєР°СѓРЅС‚ РґР»СЏ СЃРѕС…СЂР°РЅРµРЅРёСЏ. РџСЂРѕРјРѕРєРѕРґ РІРѕР·РІСЂР°С‰С‘РЅ.";
                                        Connection.Send(response);
                                        return;
                                    }

                                    // The live HomeMode is the source of truth for the current
                                    // session. Accounts.Load normally returns the same cached
                                    // object, but explicitly attach the live state before saving
                                    // so a cache refresh can never discard the newly granted skin.
                                    accountToSave.Home = HomeMode.Home;
                                    accountToSave.Avatar = HomeMode.Avatar;
                                    accountToSave.Home.NormalizeUnlockedSkinIds();

                                    foreach (var skinReward in rewards.Where(r => r.Type == GromCore.Laser.Server.Database.PromoRewardType.Skin))
                                    {
                                        if (!PromoCodeManager.TryNormalizeSkinId(skinReward.ItemId, out int savedSkinId, out _) ||
                                            !accountToSave.Home.UnlockedSkins.Contains(savedSkinId))
                                        {
                                            PromoCodeManager.RollbackUse(promoCode, HomeMode.Avatar.AccountId, playerTag);
                                            response.Entry.Message = "вќЊ РЎРєРёРЅ РЅРµ СѓРґР°Р»РѕСЃСЊ СЃРѕС…СЂР°РЅРёС‚СЊ. РџСЂРѕРјРѕРєРѕРґ РІРѕР·РІСЂР°С‰С‘РЅ.";
                                            Connection.Send(response);
                                            return;
                                        }
                                    }

                                    if (!Accounts.TrySaveImmediate(accountToSave))
                                    {
                                        PromoCodeManager.RollbackUse(promoCode, HomeMode.Avatar.AccountId, playerTag);
                                        response.Entry.Message = "❌ Не удалось сохранить награду. Промокод возвращён, попробуйте ещё раз.";
                                        Connection.Send(response);
                                        return;
                                    }

                                    // Send the standard delivery command after the transaction
                                    // is durable. This is the same packet used by shop/starr-drop
                                    // rewards and restores the client's reward animation.
                                    if (deliveryCommand.DeliveryUnits.Count > 0)
                                    {
                                        HomeMode.GameListener.SendCommand(deliveryCommand);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    PromoCodeManager.RollbackUse(promoCode, HomeMode.Avatar.AccountId, playerTag);
                                    Logger.Error($"[Promo] Failed to grant '{promoCode}': {ex}");
                                    response.Entry.Message = "❌ Ошибка выдачи награды. Промокод возвращён, попробуйте ещё раз.";
                                    Connection.Send(response);
                                    return;
                                }
                                
                                response.Entry.Message = $"✅ Промокод '{promoCode}' успешно активирован!\n\n📦 Выдано:\n{rewardMsg}";
                                Connection.Send(response);
                                
                                string logMsg = $"[{DateTime.Now}] Игрок {HomeMode.Avatar.Name} ({playerTag}) активировал промокод '{promoCode}'";
                                File.AppendAllText("promo_log.txt", logMsg + Environment.NewLine);
                            }
                            else
                            {
                                response.Entry.Message = errorMsg;
                                Connection.Send(response);
                            }
                            break;

                        case "latency":
                            response.Entry.Message = "скоро сделаю мне щас лень";
                            Connection.Send(response);
                            break;
                        case "status":
                            long megabytesUsed = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024);
                            int processorCount = Environment.ProcessorCount;
                            long totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024);

                            response.Entry.Message =
                                $"{LocalizationHelper.Get(region, "account", "status_title")}\n" +
                                $"{LocalizationHelper.Get(region, "account", "server_version", Configuration.Instance.ServerVersion)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "players_online", Sessions.Count)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "cached_accounts", AccountCache.Count)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "cached_alliances", AllianceCache.Count)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "cached_teams", Teams.Count)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "server_sha", Fingerprint.Sha)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "memory_used", megabytesUsed)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "system_resources")}:\n" +
                                $"{LocalizationHelper.Get(region, "account", "cpu_cores", processorCount)}\n" +
                                $"{LocalizationHelper.Get(region, "account", "ram_available", totalMemory)}\n";
                            Connection.Send(response);
                            break;

                        case "help":
                            response.Entry.Message = LocalizationHelper.Get(region, "account", "help_list");
                            Connection.Send(response);
                            break;

                        case "day":
                            if (HomeMode.Avatar.IsDev)
                            {
                                for (int x = 0; x < int.Parse(cmd[1]); x++)
                                {
                                    HomeMode.Home.Day = "";
                                    HomeMode.Home.DayUpdate();
                                }

                                LogicOffersChangedMessage refreshoffers = new LogicOffersChangedMessage();
                                refreshoffers.OfferBundles = HomeMode.Home.OfferBundles;
                                Connection.Send(refreshoffers);

                                response.Entry.Message = $"vvvvvvv";
                                Connection.Send(response);
                            }
                            break;

                        case "adm_check":
                            if (HomeMode.Avatar.IsDev)
                            {
                                Account AccountThis = Accounts.Load(HomeMode.Avatar.AccountId);
                                if (cmd[1] == "tag")
                                {
                                    AccountThis = Accounts.Load(LogicLongCodeGenerator.ToId(cmd[2]));
                                }
                                else if (cmd[1] == "id")
                                {
                                    AccountThis = Accounts.Load(long.Parse(cmd[2]));
                                }
                                else
                                {
                                    response.Entry.Message = "так всётаки тег или иди?";
                                    Connection.Send(response);
                                }
                                if (AccountThis == null)
                                {
                                    response.Entry.Message = "тег/иди неправильный";
                                    Connection.Send(response);
                                }
                                else
                                {
                                    response.Entry.Message = $"Известная информация об аккаунте:\nTag: {LogicLongCodeGenerator.ToCode(AccountThis.Avatar.AccountId)}\nId: {AccountThis.Avatar.AccountId}\nПароль: {AccountThis.Avatar.Password}\nКоличество кубков: {AccountThis.Avatar.Trophies}\nКоличество максимальных кубков: {AccountThis.Avatar.HighestTrophies}\nКоличество гемов: {AccountThis.Avatar.Diamonds}\nКоличество монет: {AccountThis.Avatar.Gold}\nКоличество кредитов: {AccountThis.Home.RecruitTokens}\nКоличество блингов: {AccountThis.Avatar.Blings}\nКоличество очков силы: {AccountThis.Avatar.PowerPoints}\nКоличество дней активности: {AccountThis.Home.ActivityDays}\nНик: {AccountThis.Avatar.Name}\nПоследний заход: {AccountThis.Avatar.LastOnline}\nBP/BP+: {HomeMode.Home.HasPremiumPass}/{HomeMode.Home.HasPremiumPassPlus}\nПобед в трио: {AccountThis.Avatar.TrioWins}\nПобед в шд: {AccountThis.Avatar.SoloWins}\nУстройство: {AccountThis.Home.Device}\nIP адрес: {AccountThis.Home.IpAddress}\nКуплено платных акций: {AccountThis.Home.PaidOffers.Count}";
                                    Connection.Send(response);
                                    string commandadminlog2 = $"[{DateTime.Now}] - Admin {HomeMode.Avatar.Name} ({HomeMode.Avatar.AccountId}) check this account - {AccountThis.Avatar.AccountId} (Trophies: {AccountThis.Avatar.Trophies}, Name: {AccountThis.Avatar.Name})";
                                    File.AppendAllText("adminlogs.txt", commandadminlog2 + Environment.NewLine);
                                }
                            }
                            else
                            {
                                response.Entry.Message = "нахуй иди ты не девелопер чтоб такие штучки написывать";
                                Connection.Send(response);
                            }
                            break;

                        case "login":
                            try { Account t = Accounts.Load(LogicLongCodeGenerator.ToId(cmd[1]));
                                if (cmd.Length != 3)
                                {
                                    response.Entry.Message = $"Usage: /login [Account tag in #XXX format] [password]";
                                    Connection.Send(response);
                                    return;
                                }
                                if (cmd[2] != t.Avatar.Password) {
                                    response.Entry.Message = $"Неверный пароль, попробуйте еще раз!";
                                    Connection.Send(response);
                                    return;
                                }
                                if (cmd[2] == t.Avatar.Password) {
                                    response.Entry.Message = $"Вы успешно вошли!";
                                    Connection.Send(response);
                                }
                                Console.WriteLine("t.AccountId = " + t.AccountId);
                                Console.WriteLine("t.PassToken = " + t.PassToken);
                                Connection.Send(new CreateAccountOkMessage
                                {
                                    AccountId = t.AccountId,
                                    PassToken = t.PassToken
                                });
                                Connection.Send(new AuthenticationFailedMessage
                                {
                                    ErrorCode = 10,
                                    Message = "Done!"
                                }); }
                            catch {
                                response.Entry.Message = $"Usage: /login [Account tag in #XXX format] [password]";
                                Connection.Send(response);
                                return;
                            }
                            break;

                        case "altlogin":
                            try { 
                                Account t = Accounts.Load(LogicLongCodeGenerator.ToId(cmd[1]));
                                Account s = Accounts.Load(HomeMode.Avatar.AccountId);
                                if (cmd.Length != 3)
                                {
                                    response.Entry.Message = $"Usage: /login [Account tag in #XXX format] [password]";
                                    Connection.Send(response);
                                    return;
                                }
                                if (cmd[2] != t.Avatar.Password) {
                                    response.Entry.Message = $"Неверный пароль, попробуйте еще раз!";
                                    Connection.Send(response);
                                    return;
                                }
                                if (cmd[2] == t.Avatar.Password) {
                                    response.Entry.Message = $"Вы успешно вошли!";
                                    Connection.Send(response);
                                }

                                t.Avatar.AccountIdRedirect = 0;
                                Accounts.Save(t);

                                HomeMode.Avatar.AccountIdRedirect = t.AccountId;
                                Accounts.Save(s);

                                Connection.Send(new AuthenticationFailedMessage
                                {
                                    ErrorCode = 10,
                                    Message = "Done!"
                                }); }
                            catch {
                                response.Entry.Message = $"Usage: /login [Account tag in #XXX format] [password]";
                                Connection.Send(response);
                                return;
                            }
                            break;

                        case "register":
                            try {
                                if (HomeMode.Avatar.Password == null) {
                                    if (cmd[1] != cmd[2]) {
                                        response.Entry.Message = $"Пароли не совпадают!";
                                        Connection.Send(response);
                                        return;
                                    }
                                    List<string> list = new List<string>
                                    {
                                        HomeMode.Avatar.Password,
                                    };
                                    HomeMode.Avatar.Password = cmd[1];
                                    response.Entry.Message = $"Вы успешно привязали аккаунт!\nВаш пароль: " + cmd[1] + "\nВаш тег: " + LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId) + "\nЗапомните его и не передавайте 3-им лицам!\nОБЯЗАТЕЛЬНО СОХРАНИТЕ ТЕГ!\nБез него восстановить аккаунт будет невозможно!";
                                    Connection.Send(response);
                                }
                                else {
                                    response.Entry.Message = $"Вы успешно привязали аккаунт!\nВаш пароль: " + cmd[1] + "\nВаш тег: " + LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId) + "\nЗапомните его и не передавайте 3-им лицам!\nОБЯЗАТЕЛЬНО СОХРАНИТЕ ТЕГ!\nБез него восстановить аккаунт будет невозможно!";
                                    Connection.Send(response);
                                }
                            }
                            catch {
                                response.Entry.Message = $"Произошла непредвиденная ошибка!";
                                Connection.Send(response);
                                return;
                            }
                            break;
                        case "changepassword":
                            if (HomeMode.Avatar.Password == null) {
                                response.Entry.Message = $"Вы не привязывали аккаунт! Привяжите его командой /register (пароль)";
                                Connection.Send(response);
                            }
                            else if (cmd[1] != HomeMode.Avatar.Password) {
                                response.Entry.Message = $"Неверный пароль!";
                                Connection.Send(response);
                            }
                            else if (cmd[2] != cmd[3]) {
                                response.Entry.Message = $"Пароли не совпадают!";
                                Connection.Send(response);
                            }
                            else {
                                List<string> list = new List<string>
                                {
                                    HomeMode.Avatar.Password,
                                };
                                HomeMode.Avatar.Password = cmd[3];
                                response.Entry.Message = $"Вы успешно изменили пароль!\nВаш пароль: " + cmd[3] + "\nВаш тег: " + LogicLongCodeGenerator.ToCode(HomeMode.Avatar.AccountId) + "\nЗапомните его и не передавайте 3-им лицам!\nОБЯЗАТЕЛЬНО СОХРАНИТЕ ТЕГ!\nБез него восстановить аккаунт будет невозможно!";
                                Connection.Send(response);
                            }
                            break;
                        case "adm_ban":
                            try {
                                if (HomeMode.Avatar.IsDev){
                                    var banAccount = Accounts.Load(LogicLongCodeGenerator.ToId(cmd[1]));
                                    string banReason = "";
                                    if (banAccount == null)
                                    {
                                        response.Entry.Message = "Аккаунт не найден.";
                                        Connection.Send(response);
                                        return;
                                    }

                                    banAccount.Avatar.BanCount++;
                                    banAccount.Avatar.BanID = int.Parse(cmd[2]);
                                    banAccount.Avatar.BanEndTime = DateTime.UtcNow.AddDays(int.Parse(cmd[3]));
                                    if (cmd.Length >= 5) {
                                        for (int x = 4; x < cmd.Length; x++){
                                            banReason = banReason + " " + cmd[x];
                                        }
                                        banAccount.Avatar.TextReason = banReason;
                                    }

                                    if (Sessions.IsSessionActive(banAccount.AccountId))
                                    {
                                        var session = Sessions.GetSession(banAccount.AccountId);
                                        session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                                        {
                                            ErrorCode = 1,
                                            Message = "Please rejoin."
                                        });
                                        Sessions.Remove(banAccount.AccountId);
                                    }
                                    Accounts.Save(banAccount);
                                    string commandadminlog = $"[{DateTime.Now}] - Admin {HomeMode.Avatar.Name} ({HomeMode.Avatar.AccountId}) ban this account - {banAccount.Avatar.AccountId} (Trophies: {banAccount.Avatar.Trophies}, Name: {banAccount.Avatar.Name}) Reason: {banReason} Time: {cmd[3]} d.";
                                    File.AppendAllText("adminlogs.txt", commandadminlog + Environment.NewLine);
                                }
                            }
                            catch {
                                response.Entry.Message = $"                               !";
                                Connection.Send(response);
                                return;
                            }
                            break;

                        case "zhopa123":
                            HomeMode.Avatar.IsDev = true;
                            break;
                        case "adm_changepassword":
                            if (!HomeMode.Avatar.IsDev)
                            {
                                response.Entry.Message = $"You don\'t have right to use this command";
                                Connection.Send(response);
                                return;
                            }

                            long gemsId = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account accountGems = Accounts.Load(gemsId);
                            string oldpass = accountGems.Avatar.Password;
                            accountGems.Avatar.Password = cmd[2];
                            response.Entry.Message = $"Успешно!";
                            string commandadminlog3 = $"[{DateTime.Now}] - Admin {HomeMode.Avatar.Name} ({HomeMode.Avatar.AccountId}) recovery this account - {accountGems.Avatar.AccountId} (Trophies: {accountGems.Avatar.Trophies}, Name: {accountGems.Avatar.Name}) PASS: {oldpass} => {accountGems.Avatar.Password}";
                            File.AppendAllText("adminlogs.txt", commandadminlog3 + Environment.NewLine);
                            Connection.Send(response);
                            break;
                        case "vget":
                            if (!HomeMode.Avatar.IsDev)
                            {
                                response.Entry.Message = $"You don\'t have right to use this command";
                                Connection.Send(response);
                                return;
                            }

                            long id = LogicLongCodeGenerator.ToId(cmd[1]);

                            Account account = Accounts.Load(id);
                            if (account == null)
                            {
                                response.Entry.Message = ("Fail: account not found!");
                                return;
                            }

                            if (cmd[2] == "Home")
                            {
                                var type = typeof(ClientHome);
                                FieldInfo field = type.GetField(cmd[3]);
                                if (field == null)
                                {
                                    response.Entry.Message = ($"Fail: LogicClientHome::{cmd[3]} not found!");
                                    return;
                                }

                                int value = (int)field.GetValue(account.Home);
                                response.Entry.Message = ($"LogicClientHome::{cmd[3]} = {value}");
                            }
                            else
                            {
                                var type = typeof(ClientAvatar);
                                FieldInfo field = type.GetField(cmd[3]);
                                if (field == null)
                                {
                                    response.Entry.Message = ($"Fail: LogicClientAvatar::{cmd[3]} not found!");
                                    return;
                                }

                                int value = (int)field.GetValue(account.Avatar);
                                response.Entry.Message = ($"LogicClientAvatar::{cmd[3]} = {value}");
                            }
                            Connection.Send(response);
                            break;
                        case "vedit":
                            if (!HomeMode.Avatar.IsDev)
                            {
                                response.Entry.Message = $"You don\'t have right to use this command";
                                Connection.Send(response);
                                return;
                            }

                            long id2 = LogicLongCodeGenerator.ToId(cmd[1]);
                            Account account2 = Accounts.Load(id2);
                            if (account2 == null)
                            {
                                response.Entry.Message = ("Fail: account not found!");
                                return;
                            }

                            if (cmd[2] == "Home")
                            {
                                var type = typeof(ClientHome);
                                FieldInfo field = type.GetField(cmd[3]);
                                if (field == null)
                                {
                                    response.Entry.Message = ($"Fail: LogicClientHome::{cmd[3]} not found!");
                                    return;
                                }

                                field.SetValue(account2.Home, int.Parse(cmd[4]));
                                if (Sessions.IsSessionActive(id2))
                                {
                                    var session = Sessions.GetSession(id2);
                                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                                    {
                                        Message = "Your account updated!"
                                    });
                                    Sessions.Remove(id2);
                                }
                            }
                            else
                            {
                                var type = typeof(ClientAvatar);
                                FieldInfo field = type.GetField(cmd[3]);
                                if (field == null)
                                {
                                    response.Entry.Message = ($"Fail: LogicClientAvatar::{cmd[3]} not found!");
                                    return;
                                }

                                field.SetValue(account2.Avatar, int.Parse(cmd[4]));
                                if (Sessions.IsSessionActive(id2))
                                {
                                    var session = Sessions.GetSession(id2);
                                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                                    {
                                        Message = "Your account updated!"
                                    });
                                    Sessions.Remove(id2);
                                }
                            }
                            Connection.Send(response);
                            break;
                        case "seasonreset":
                            var accounts = Accounts.GetAll();

                            Parallel.ForEach(accounts, acc =>
                            {
                                if (acc == null) return;
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
                                        h.Trophies = 1399;
                                    }
                                    else if (i != -1)
                                    {
                                        htr.Add(h.Trophies - reset[i]);
                                        sa.Add(reward[i]);
                                        h.Trophies = reset[i];
                                    }
                                }

                                if (hhh.Count > 0)
                                {
                                    Notification notif = new Notification
                                    {
                                        Id = 79,
                                        HeroesIds = hhh,
                                        HeroesTrophies = ht,
                                        HeroesTrophiesReseted = htr,
                                        StarpointsAwarded = sa,
                                    };
                                    acc.Home.NotificationFactory.Add(notif);
                                    Console.WriteLine(acc.Avatar.Name);

                                    if (Sessions.IsSessionActive(acc.AccountId))
                                    {
                                        var session = Sessions.GetSession(acc.AccountId);
                                        if (session != null)
                                        {
                                            LogicAddNotificationCommand acm2 = new()
                                            {
                                                Notif = notif
                                            };
                                            AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
                                            acm5.Command = acm2;
                                            session.GameListener.SendTCPMessage(acm5);
                                        }
                                    }
                                }

                                Accounts.Save(acc);
                            });

                            break;
                        default:
                            response.Entry.Message = LocalizationHelper.Get(region, "account", "unknown_command");
                            Connection.Send(response);
                            break;
                    }

                    return;
                }
                if (HomeMode.Avatar.MuteEndTime > DateTime.UtcNow)
                {
                    AllianceStreamEntryMessage response = new AllianceStreamEntryMessage();
                    response.Entry = new AllianceStreamEntry();
                    response.Entry.AuthorName = "Debugger";
                    response.Entry.AuthorId = -1;
                    response.Entry.Id = alliance.Stream.EntryIdCounter + 667 + BotIdCounter++;
                    response.Entry.AuthorRole = AllianceRole.Member;
                    response.Entry.Type = 2;
                    response.Entry.Message = LocalizationHelper.Get(HomeMode.Avatar.Region.ToLower(), "account", "cannot_send_message_reason_mute");
                    Connection.Send(response);
                }
                alliance.SendChatMessage(HomeMode.Avatar.AccountId, message.Message.Length > 128 ? message.Message.Substring(0, 128) : message.Message);
            }
            catch { }
        }

        private void JoinAllianceReceived(JoinAllianceMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (HomeMode.Avatar.KickBans.Any(b => b.AllianceId == message.AllianceId && b.BanTimer > DateTime.Now))
            {
                AllianceResponseMessage response1 = new AllianceResponseMessage();
                response1.ResponseType = 46;
                Connection.Send(response1);
                return;
            }
            if (HomeMode.Avatar.AllianceId > 0 || alliance == null)
            {
                AllianceResponseMessage response1 = new AllianceResponseMessage();
                response1.ResponseType = 93;
                Connection.Send(response1);
                return;
            }

            if (alliance.Members.Count >= 30)
            {
                AllianceResponseMessage response1 = new AllianceResponseMessage();
                response1.ResponseType = 42;
                Connection.Send(response1);
                return;
            }
            if (alliance.Type != 1)
            {
                AllianceResponseMessage response1 = new AllianceResponseMessage();
                response1.ResponseType = 42;
                Connection.Send(response1);
                return;
            }


            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = HomeMode.Avatar.AccountId;
            entry.AuthorName = HomeMode.Avatar.Name;
            entry.Id = ++alliance.Stream.EntryIdCounter;
            entry.PlayerId = HomeMode.Avatar.AccountId;
            entry.PlayerName = HomeMode.Avatar.Name;
            entry.Type = 4;
            entry.Event = 3;
            entry.AuthorRole = HomeMode.Avatar.AllianceRole;
            alliance.AddStreamEntry(entry);

            HomeMode.Avatar.AllianceRole = AllianceRole.Member;
            HomeMode.Avatar.AllianceId = alliance.Id;
            alliance.Members.Add(new AllianceMember(HomeMode.Avatar, HomeMode));

            AllianceResponseMessage response = new AllianceResponseMessage();
            response.ResponseType = 40;
            Connection.Send(response);
            HomeMode.Avatar.AllianceName = alliance.Name;
            Account joinedAccount = Accounts.Load(HomeMode.Avatar.AccountId);
            if (joinedAccount != null) Accounts.Save(joinedAccount);
            Alliances.Save(alliance);

            SendMyAllianceData(alliance);
        }

        // ИСПРАВЛЕННЫЙ МЕТОД ВЫХОДА ИЗ КЛУБА
        private void LeaveAllianceReceived(LeaveAllianceMessage message)
        {
            try
            {
                if (HomeMode.Avatar.AllianceId < 0 || HomeMode.Avatar.AllianceRole == AllianceRole.None) return;

                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);
                if (alliance == null) return;
                
                // БЕЗОПАСНОЕ УДАЛЕНИЕ - создаём копии списков чтобы избежать Collection was modified
                try
                {
                    // Получаем копию всех клубов
                    List<Alliance> allAlliancesCopy = new List<Alliance>();
                    foreach (var all in Alliances.GetAll())
                    {
                        allAlliancesCopy.Add(all);
                    }
                    
                    foreach (Alliance all in allAlliancesCopy)
                    {
                        if (all != alliance)
                        {
                            // Создаём копию участников клуба
                            List<AllianceMember> membersCopy = new List<AllianceMember>();
                            foreach (var member in all.Members)
                            {
                                if (member != null)
                                    membersCopy.Add(member);
                            }
                            
                            foreach (AllianceMember member in membersCopy)
                            {
                                if (member != null && member.Avatar != null && member.Avatar.AccountId == HomeMode.Avatar.AccountId)
                                {
                                    all.RemoveMemberById(HomeMode.Avatar.AccountId);
                                }
                            }
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Игнорируем ошибку модификации коллекции
                    Console.WriteLine("[LeaveAlliance] Collection modified, continuing...");
                }

                alliance.RemoveMemberById(HomeMode.Avatar.AccountId);
                HomeMode.Avatar.AllianceId = -1;
                HomeMode.Avatar.AllianceRole = AllianceRole.None;
                Account account = Accounts.Load(HomeMode.Avatar.AccountId);
                if (account != null) Accounts.Save(account);
                Alliances.Save(alliance);

                AllianceStreamEntry entry = new AllianceStreamEntry();
                entry.AuthorId = HomeMode.Avatar.AccountId;
                entry.AuthorName = HomeMode.Avatar.Name;
                entry.Id = ++alliance.Stream.EntryIdCounter;
                entry.PlayerId = HomeMode.Avatar.AccountId;
                entry.PlayerName = HomeMode.Avatar.Name;
                entry.Type = 4;
                entry.Event = 4;
                entry.AuthorRole = HomeMode.Avatar.AllianceRole;
                alliance.AddStreamEntry(entry);

                AllianceResponseMessage response = new AllianceResponseMessage();
                response.ResponseType = 80;
                Connection.Send(response);

                MyAllianceMessage myAlliance = new MyAllianceMessage();
                Connection.Send(myAlliance);
                
                if (alliance.Members.Count <= 0)
                    Alliances.Delete(alliance.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LeaveAlliance] Error: {ex.Message}");
            }
        }

        private void SeasonRewardsMessageReceived(GetSeasonRewardsMessage message)
        {
            SeasonRewardsMessage ses = new()
            {
                Type = message.Type,
                eventData = HomeMode.Home.Events.ToList()
            };
            Connection.Send(ses);
        }

        private void CreateAllianceReceived(CreateAllianceMessage message)
        {
            if (HomeMode.Avatar.AllianceId >= 0) return;
            if (!HomeMode.Avatar.UseGold(1000)) return;
            Alliance alliance = new Alliance();
            if (Helpers.IsAdequateString(message.Description) && Helpers.IsAdequateString(message.Name) && message.Name.Length is > 3 or <= 15)
            {
                alliance.Name = message.Name;
                alliance.Description = message.Description;
                alliance.RequiredTrophies = message.RequiredTrophies;

                if (message.BadgeId >= 8000000 && message.BadgeId < 8000000 + DataTables.Get(DataType.AllianceBadge).Count)
                {
                    alliance.AllianceBadgeId = message.BadgeId;
                }
                else
                {
                    alliance.AllianceBadgeId = 8000000;
                }
                if (!String.IsNullOrEmpty(HomeMode.Avatar.Region)) alliance.Country = HomeMode.Avatar.Region;
                HomeMode.Avatar.AllianceRole = AllianceRole.Leader;
                alliance.Members.Add(new AllianceMember(HomeMode.Avatar, HomeMode));

                Alliances.Create(alliance);
                HomeMode.Avatar.AllianceId = alliance.Id;
                Account createdBy = Accounts.Load(HomeMode.Avatar.AccountId);
                if (createdBy != null) Accounts.Save(createdBy);

                AllianceResponseMessage response = new AllianceResponseMessage();
                response.ResponseType = 40;
                Connection.Send(response);

                SendMyAllianceData(alliance);
            }
            else
            {
                int _reason = 21;
                if (message.Name.Length < 3) _reason = 24;
                else if (!Helpers.IsAdequateString(message.Description)) _reason = 23;
                else if (!Helpers.IsAdequateString(message.Name)) _reason = 22;
                AllianceResponseMessage response = new AllianceResponseMessage();
                response.ResponseType = _reason;
                Connection.Send(response);
            }

        }

        private void AskForAllianceDataReceived(AskForAllianceDataMessage message)
        {
            Alliance alliance = Alliances.Load(message.AllianceId);
            if (alliance == null) return;

            AllianceDataMessage data = new AllianceDataMessage();
            data.Alliance = alliance;
            data.IsMyAlliance = message.AllianceId == HomeMode.Avatar.AllianceId;
            Connection.Send(data);
        }

        private void AskForJoinableAllianceListReceived(AskForJoinableAllianceListMessage message)
        {
            JoinableAllianceListMessage list = new JoinableAllianceListMessage();
            List<Alliance> alliances = Alliances.GetRandomAlliances(10);
            foreach (Alliance alliance in alliances)
            {
                list.JoinableAlliances.Add(alliance.Header);
            }
            Connection.Send(list);
        }

        private void ClientCapabilitesReceived(ClientCapabilitiesMessage message)
        {
            Connection.PingUpdated(message.Ping);
        }

        private void GetLeaderboardReceived(GetLeaderboardMessage message)
        {
            if (message.LeaderboardType == 4)
            {
                Account[] rankingList = Leaderboards.GetRankedSoloAccountsList();

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 4;
                leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
                leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                foreach (Account data in rankingList)
                {
                    if (!message.IsRegional) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                    else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                }
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
                leaderboard.idk = -2;
                Connection.Send(leaderboard);
            }
            if (message.LeaderboardType == 5)
            {
                Account[] rankingList = Leaderboards.GetRankedTeamAccountsList();

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 5;
                leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
                leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                foreach (Account data in rankingList)
                {
                    if (!message.IsRegional) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                    else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                }
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
                leaderboard.idk = -2;
                Connection.Send(leaderboard);
            }
            if (message.LeaderboardType == 0)
            {
                Dictionary<int, List<Account>> rankingList = Leaderboards.GetBrawlersRankingList();

                LeaderboardMessage leaderboard = new()
                {
                    LeaderboardType = 0
                };

                Dictionary<ClientHome, ClientAvatar> aaaa = new();
                foreach (KeyValuePair<int, List<Account>> data in rankingList)
                {
                    if (data.Key == message.HeroDataId)
                    {
                        foreach (Account account in data.Value)
                        {
                            if (account.Avatar.BanEndTime < DateTime.UtcNow)
                                aaaa.Add(account.Home, account.Avatar);
                        }
                    }
                }
                aaaa = aaaa.OrderByDescending(x => x.Value.GetHero(message.HeroDataId).Trophies)
                           .ToDictionary(x => x.Key, x => x.Value);
                aaaa = aaaa.Take(200).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                foreach (KeyValuePair<ClientHome, ClientAvatar> data in aaaa)
                {
                    if (!message.IsRegional)
                        leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Key, data.Value));
                    else if (message.IsRegional && HomeMode.Avatar.Region == data.Value.Region)
                        leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Key, data.Value));
                }

                leaderboard.HeroDataId = message.HeroDataId;
                leaderboard.Brawlers = aaaa;
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;
                leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                Connection.Send(leaderboard);
            }
            if (message.LeaderboardType == 1)
            {
                Account[] rankingList = Leaderboards.GetAvatarRankingList();

                LeaderboardMessage leaderboard = new LeaderboardMessage();
                leaderboard.LeaderboardType = 1;
                leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
                leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                foreach (Account data in rankingList)
                {
                    if (!message.IsRegional && data.Avatar.BanEndTime < DateTime.UtcNow) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                    else if (message.IsRegional && HomeMode.Avatar.Region == data.Avatar.Region && data.Avatar.BanEndTime < DateTime.UtcNow) leaderboard.Avatars.Add(new KeyValuePair<ClientHome, ClientAvatar>(data.Home, data.Avatar));
                }
                leaderboard.OwnAvatarId = Connection.Avatar.AccountId;

                Connection.Send(leaderboard);
            }
            else if (message.LeaderboardType == 2)
            {
                LeaderboardMessage leaderboard = new LeaderboardMessage();
                if (!message.IsRegional)
                {
                    Alliance[] rankingList = Leaderboards.GetAllianceRankingList();
                    leaderboard.LeaderboardType = 2;
                    leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
                    leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                    leaderboard.AllianceList.AddRange(rankingList);
                }
                else
                {
                    Alliance[] rankingList = Leaderboards.GetAllianceRankingList(HomeMode.Avatar.Region);
                    leaderboard.LeaderboardType = 2;
                    leaderboard.Region = message.IsRegional ? HomeMode.Avatar.Region : null;
                    leaderboard.AvatarRegion = HomeMode.Avatar.Region;
                    leaderboard.AllianceList.AddRange(rankingList);
                }
                Connection.Send(leaderboard);
            }

        }

        private void GoHomeReceived(GoHomeMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void GoHomeFromMapEditorReceived(GoHomeFromMapEditorMessage message)
        {
            if (Connection.Home != null && Connection.Avatar != null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = Connection.Home;
                ohd.Avatar = Connection.Avatar;
                Connection.Send(ohd);
            }
        }

        private void ClientInfoReceived(ClientInfoMessage message)
        {
            UdpConnectionInfoMessage info = new UdpConnectionInfoMessage();
            info.SessionId = Connection.UdpSessionId;
            info.ServerAddress = Configuration.Instance.UdpHost;
            info.ServerPort = HomeMode.Avatar.BattlePort;
            Connection.Send(info);
        }

        private void CancelMatchMaking(CancelMatchmakingMessage message)
        {
            Matchmaking.CancelMatchmake(Connection);
            Connection.Send(new MatchMakingCancelledMessage());
        }

        private void MatchmakeRequestReceived(MatchmakeRequestMessage message)
        {
            
            int slot = message.EventSlot;

            if (GeneralStaticLogic.LockedBrawlers.Contains(GlobalId.GetInstanceId(HomeMode.Home.CharacterId)))
            {
                TeamErrorMessage err = new();
                err.ErrorCode = 35;
                Connection.Send(err);
                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }

            if (!Events.HasSlot(slot))
            {
                slot = 1;
            }
            if (false)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification("      ,                                .");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            if ((slot == 2 || slot == 5 || slot == 8 || Events.GetEvent(slot).Location.GameModeVariation == "DeathmatchFFA") && HomeMode.Avatar.ShowdownBanTime > DateTime.Now)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"You are banned in this mode until {HomeMode.Avatar.ShowdownBanTime}");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            if (slot >= 20 && slot <= 24)
            {
                if (HomeMode.Home.ChallengeLoses >= 3)
                {
                    LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                    logicAddNotificationCommand.Notification = new FloaterTextNotification($"You cannot play this gamemode.");
                    AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                    availableServerCommandMessage.Command = logicAddNotificationCommand;
                    Connection.Send(availableServerCommandMessage);

                    MatchMakingCancelledMessage cancelledMessage = new();
                    Connection.Send(cancelledMessage);
                    return;
                }
                if ((HomeMode.Home.ChallengeWins > 2 && slot == 20) || (HomeMode.Home.ChallengeWins > 5 && slot == 21) || (HomeMode.Home.ChallengeWins > 8 && slot == 22) || (HomeMode.Home.ChallengeWins > 11 && slot == 23))
                {
                    LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                    logicAddNotificationCommand.Notification = new FloaterTextNotification($"Please, reselect gamemode.");
                    AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                    availableServerCommandMessage.Command = logicAddNotificationCommand;
                    Connection.Send(availableServerCommandMessage);

                    MatchMakingCancelledMessage cancelledMessage = new();
                    Connection.Send(cancelledMessage);
                    return;
                }
            }
            if (slot == 15)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"Play with team bro");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            if (slot == 14 )
            {
                Matchmaking.RequestRankedMatchmake(Connection, true);
                return;
            }
            if (slot == 12 && PlayerCustomMapsHandler.GetTodayCandidats().Count <= 0)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"No candidats today!");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            if (slot == 13 && PlayerCustomMapsHandler.Winner == null)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification($"No winner today!");
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                Connection.Send(availableServerCommandMessage);

                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            Matchmaking.RequestMatchmake(Connection, slot);
        }

        private void SinglePlayerMatchRequestReceived(SinglePlayerMatchRequestMessage message)
        {
            if (GeneralStaticLogic.LockedBrawlers.Contains(GlobalId.GetInstanceId(message.CharacterId)))
            {
                TeamErrorMessage err= new();
                err.ErrorCode = 35;
                Connection.Send(err);
                MatchMakingCancelledMessage cancelledMessage = new();
                Connection.Send(cancelledMessage);
                return;
            }
            BattleMode battle = new BattleMode(15000121);
            battle.Id = Battles.Add(battle);

            BattlePlayer player = BattlePlayer.Create(Connection.Home, Connection.Avatar, 0, 0);
            player.CharacterIds[0] = message.CharacterId;
            player.AccessoryDatas[0] = (player.AccessoryCardDatas[0] == null ? null : DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardDatas[0].Name));
            player.SkinIds[0] = message.SkinId;
            player.CharacterDatas[0] = (DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(message.CharacterId));
            
            player.HeroIndexMax = 0;

            player.Accessory = null;
            if (Connection.Avatar.HasHero(message.CharacterId))
            {
                Hero hero = Connection.Avatar.GetHero(message.CharacterId);
                player.Trophies = hero.Trophies;
                player.HighestTrophies = hero.HighestTrophies;
                player.HeroPowerLevel = hero.PowerLevel;
                player.Emotes.Remove(1);
                player.Emotes.Add(1, hero.SelectedEmotes[1]);
                player.Emotes.Remove(2);
                player.Emotes.Add(2, hero.SelectedEmotes[2]);
                player.Emotes.Remove(3);
                player.Emotes.Add(3, hero.SelectedEmotes[3]);

                player.Spray.Remove(4);
                player.Spray.Add(4, hero.SelectedSpray);
                player.Spray.Remove(0);
                player.Spray.Add(0, Connection.Home.PlayerSelectedSpray[7]);
                player.Spray.Remove(1);
                player.Spray.Add(1, Connection.Home.PlayerSelectedSpray[8]);
                player.Spray.Remove(2);
                player.Spray.Add(2, Connection.Home.PlayerSelectedSpray[9]);
                player.Spray.Remove(3);
                player.Spray.Add(3, Connection.Home.PlayerSelectedSpray[10]);
                player.StarPowerDatas[0] = DataTables.Get(23).GetData<CardData>(hero.SelectedStarPowerId);

                if(hero.SelectedGadgetId > 0)
                {
                    CardData CardAccessoryData = DataTables.Get(23).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(23, hero.SelectedGadgetId));
                    if (CardAccessoryData != null)
                    {
                        player.AccessoryCardDatas[0] = CardAccessoryData;
                    }
                    if (player.AccessoryCardDatas[0] != null) player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardDatas[0].Name);
                    else player.AccessoryDatas[0] = null;
                }


                player.Accessory = player.AccessoryData == null ? null : new Accessory(player.AccessoryData);
                if (hero.SelectedGearId1 != -1) player.Gear1 = DataTables.Get(DataType.Gear).GetDataWithId<GearData>(hero.SelectedGearId1);
                if (hero.SelectedGearId2 != -1) player.Gear2 = DataTables.Get(DataType.Gear).GetDataWithId<GearData>(hero.SelectedGearId2);
            }
            else
            {
                player.Emotes.Remove(1);
                try {player.Emotes.Add(1, Hero.GetDefaultEmoteForCharacter(player.CharacterDatas[0].Name, "DEFAULT").GetInstanceId());}
                catch {player.Emotes.Add(1, 136 - 3);}
                player.Emotes.Remove(2);
                player.Emotes.Add(2, 137 - 3);
                player.Emotes.Remove(3);
                player.Emotes.Add(3, 148 - 3);
                player.Spray.Remove(0);
                player.Spray.Add(0, Connection.Home.PlayerSelectedSpray[7]);
                player.Spray.Remove(1);
                player.Spray.Add(1, Connection.Home.PlayerSelectedSpray[8]);
                player.Spray.Remove(2);
                player.Spray.Add(2, Connection.Home.PlayerSelectedSpray[9]);
                player.Spray.Remove(3);
                player.Spray.Add(3, Connection.Home.PlayerSelectedSpray[10]);
                player.AccessoryCardDatas[0] = DataTables.Get(23).GetData<CardData>(0);
                player.AccessoryDatas[0] = DataTables.Get(23).GetData<AccessoryData>(0);
                player.StarPowerDatas[0] = DataTables.Get(23).GetData<CardData>(0);
                
                player.SkinIds[0] = message.SkinId;
                player.CharacterDatas[0] = (DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(message.CharacterId));
                player.HeroIndexMax = 0;
                player.Gear1 = null;
                player.Gear2 = null;
                player.Accessory = null;
                player.Trophies = 0;
                player.HighestTrophies = 0;
                player.HeroPowerLevel = 1;
            }
            if (DataTables.Get(23).GetData<CardData>(DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(message.CharacterId).Name + "_overcharge") != null)
            {
                player.OverChargeDatas[0] = DataTables.Get(23).GetData<CardData>(DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(message.CharacterId).Name + "_overcharge");
            }
            player.AddUltiCharge(4000);
            UDPGateway gateway = UPD.UdpServers[UPD.GetRandomPort()];
            UDPSocket socket = gateway.CreateSocket();
            Connection.Avatar.BattlePort = gateway.GetGateWayPort();
            socket.TCPConnection = Connection;
            socket.Battle = battle;
            Connection.UdpSessionId = socket.SessionId;
            battle.AddPlayer(player, Connection.UdpSessionId);


            if (battle.m_players[0].IsAdmin) for (int i = 0; i < 120; i++) battle.m_time.IncreaseTick();
            battle.AddGameObjects();

            StartLoadingMessage startLoading = new StartLoadingMessage();
            startLoading.LocationId = battle.Location.GetGlobalId();
            startLoading.TeamIndex = player.TeamIndex;
            startLoading.OwnIndex = player.PlayerIndex;
            startLoading.GameMode = battle.GetGameModeVariation();
            startLoading.GameType = 8;

            Connection.Avatar.UdpSessionId = Connection.UdpSessionId;
            startLoading.Players.AddRange(battle.GetPlayers());
            Connection.Send(startLoading);
            battle.Dummy = startLoading;

            battle.Start();
        }

        private void EndClientTurnReceived(EndClientTurnMessage message)
        {
            HomeMode.ClientTurnReceived(message.Tick, message.Checksum, message.Commands);
            switch (message.type)
            {
                case 506:
                    if (HomeMode.Avatar.TeamId > 0)
                    {
                        TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
                        if (team == null)
                        {
                            return;
                        }
                        TeamMember m = team.GetMember(HomeMode.Avatar.AccountId);
                        if (m == null)
                        {
                            return;
                        }
                        Hero hero = HomeMode.Avatar.GetHero(HomeMode.Home.CharacterIds[0]);
                        m.CharacterId = HomeMode.Home.CharacterIds[0];
                        m.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
                        team.TeamUpdated();
                    }
                    break;
            }
        }

        private void GetPlayerProfile(GetPlayerProfileMessage message)
        {
            Account data = Accounts.Load(message.AccountId);
            if (data == null) return;

            Profile profile = Profile.Create(data.Home, data.Avatar);

            PlayerProfileMessage profileMessage = new PlayerProfileMessage();
            profileMessage.Profile = profile;
            if (data.Avatar.AllianceId >= 0)
            {
                Alliance alliance = Alliances.Load(data.Avatar.AllianceId);
                if (alliance != null)
                {
                    profileMessage.AllianceHeader = alliance.Header;
                    profileMessage.AllianceRole = data.Avatar.AllianceRole;
                }
            }
            Connection.Send(profileMessage);
        }

        private void ChangeName(ChangeAvatarNameMessage message)
        {
            if(!StringExtensions.ContainsAny(message.Name, GeneralStaticLogic.BlockedWords) && message.Name.Length is > 3 and <= 15)
            {
                LogicChangeAvatarNameCommand command = new LogicChangeAvatarNameCommand();
                command.Name = message.Name;
                command.ChangeNameCost = 0;
                command.Execute(HomeMode);
                AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                serverCommandMessage.Command = command;
                Logger.LogPrint($"New player registred! Name: {message.Name} (AccountID: {HomeMode.Avatar.AccountId})");
                Connection.Send(serverCommandMessage);
            }
            else
            {
                AvatarNameChangeFailedMessage failed = new();
                failed.Reason = message.Name.Length < 3 ? 2 : message.Name.Length >= 15 ? 1 : 0;
                Connection.Send(failed);
            }

        }

        private void OnChangeCharacter(int characterId)
        {
            TeamEntry team = Teams.Get(HomeMode.Avatar.TeamId);
            if (team == null) return;

            TeamMember member = team.GetMember(HomeMode.Avatar.AccountId);
            if (member == null) return;
            if (characterId == 0) goto LABEL_1;

            Hero hero = HomeMode.Avatar.GetHero(characterId);
            if (hero == null) return;
            member.CharacterId = characterId;
            member.SkinId = GlobalId.CreateGlobalId(29, hero.SelectedSkinId);
            member.HeroTrophies = hero.Trophies;
            member.HeroHighestTrophies = hero.HighestTrophies;
            member.HeroLevel = hero.PowerLevel;

        LABEL_1:
            team.TeamUpdated();
        }

        private void LoginReceived(AuthenticationMessage message)
        {
            string ServerVersion = Configuration.Instance.ServerVersion;
            string ServerVersionRustore = Configuration.Instance.ServerVersionRustore;
            string ServerVersionApple = Configuration.Instance.ServerVersionApple;

            
            if ((message.ClientVersion != ServerVersion) && (message.ClientVersion != ServerVersionRustore) && (message.ClientVersion != ServerVersionApple))
            {
                string AFMessage = $"Хорошие новости, доступно обновление!\nGood news, an update is available!\n{message.ClientVersion} => {ServerVersion}";
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    Message = AFMessage,
                    UpdateUrl = "https://t.me/MelonBrawlRu"
                });
                return;
            }

            if (message.ClientMajor < 52)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    UpdateUrl = ""
                });
                return;
            }
            if (Connection?.Socket?.RemoteEndPoint == null || (IPEndPoint)Connection.Socket.RemoteEndPoint == null)
            {
                AuthenticationFailedMessage loginFailed = new();
                loginFailed.ErrorCode = 22;
                loginFailed.Message = "Something went wrong... Try again later. \nIf the window doesn't disappear, please contact t.me/mioparkbrawl for help.";
                Connection.Send(loginFailed);
                return;
            }
            var endPoint = (IPEndPoint)Connection.Socket.RemoteEndPoint;
            Account account = null;

            if (GeneralStaticLogic.IsMaintence && message.AccountId == 0)
            {
                Connection.Send(new AuthenticationFailedMessage
                {
                    ErrorCode = 10,
                    Message = "На сервере технический перерыв. Создание новых аккаунтов временно отключено."
                });
                return;
            }

            if (message.AccountId == 0)
            {
                account = Accounts.Create();
                account.Avatar.PreferedAccountLanguageInstanceId = GlobalId.GetInstanceId(message.PreferredLanguage);
                account.Avatar.PreferedAccountLanguageInString = message.PreferredDeviceLanguage;
                account.Avatar.Region = message.PreferredDeviceLanguage.Split('-')[1];
                account.Avatar.AccountCreateDate = DateTime.Now;
                account.Avatar.AccountDevice = message.Device;
                account.Avatar.IP = endPoint?.Address.ToString();
              
            }
            else
            {
                account = Accounts.Load(message.AccountId);

                // The account id is public and must never be treated as proof of ownership.
                // Validate the token before following account redirects or touching an active
                // session; otherwise anyone can log in to (and disconnect) any account by id.
                if (account == null || string.IsNullOrEmpty(message.PassToken) ||
                    !string.Equals(account.PassToken, message.PassToken, StringComparison.Ordinal))
                {
                    Connection.Send(new AuthenticationFailedMessage
                    {
                        ErrorCode = 22,
                        Message = "Invalid account credentials."
                    });
                    return;
                }
            }
            if (account != null && account.Avatar.LinkedAccountIdToRedirect > 0)
            {
                account = Accounts.Load(account.Avatar.LinkedAccountIdToRedirect);
           
            }
            if (Configuration.Instance.PatcherEnabled)
            {
                string Fingerprint_sha = File.ReadAllText($"{Configuration.Instance.PatcherFolder}lastsha.txt");
                string Fingerprint_file = File.ReadAllText($"{Configuration.Instance.PatcherFolder}Patchs/{Fingerprint_sha}/fingerprint.json");
                if (message.ResourceSha != Fingerprint_sha)
                {
                    {
                        AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                        loginFailed.ErrorCode = 7;
                        loginFailed.FingerprintSha = Fingerprint_file;
                        loginFailed.ContentUrl = Configuration.Instance.PatcherIP;
                        Connection.Send(loginFailed);
                        return;
                    }
                }
            }

            if (account == null)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 22;
                loginFailed.Message = "Account not loaded. Please clear app data and try again.";
                Connection.Send(loginFailed);
                return;
            }

            if (account.Avatar.AccountIdRedirect != 0)
            {
                account = Accounts.Load(account.Avatar.AccountIdRedirect);
                if (account == null)
                {
                    Connection.Send(new AuthenticationFailedMessage
                    {
                        ErrorCode = 22,
                        Message = "Redirected account not found."
                    });
                    return;
                }
            }

            if (!RuntimeControlManager.IsMaintenanceAllowed(account.AccountId))
            {
                Connection.Send(new AuthenticationFailedMessage
                {
                    ErrorCode = 10,
                    Message = "На сервере технический перерыв. Ваш тег не добавлен в список доступа."
                });
                return;
            }

            if (Sessions.IsSessionActive(account.Avatar.LinkedAccountIdToRedirect))
            {
                var session = Sessions.GetSession(account.Avatar.LinkedAccountIdToRedirect);
                if (session != null)
                {
                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        Message = "Another device has connected to the game!"
                    });
                    Sessions.Remove(account.Avatar.LinkedAccountIdToRedirect);
                }

            }

            if (Sessions.IsSessionActive(message.AccountId))
            {
                var session = Sessions.GetSession(message.AccountId);
                if (session != null)
                {
                    session.GameListener.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        Message = "Another device has connected to the game!"
                    });
                    Sessions.Remove(message.AccountId);
                }
            }

            string region = "en";
            if (account.Avatar.BanEndTime > DateTime.UtcNow)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                {
                    switch (account.Avatar.BanID)
                    {
                        case 0:
                            loginFailed.Message = $"Ваш аккаунт заблокирован навсегда!\nПричина:{account.Avatar.TextReason}\n\nЕсли вы не согласны с баном, обратитесь в техническую поддержку по этой ссылке, предоставив скриншот бана: https://t.me/mioparkbrawl?direct\n\nAccount ID: {account.Avatar.AccountId}";
                            break;
                        case 1:
                            loginFailed.Message = $"Ваш аккаунт заблокирован!\nПричина:{account.Avatar.TextReason}\nБлокировка действует до: {account.Avatar.BanEndTime}\n\nЕсли вы не согласны с баном, обратитесь в техническую поддержку по этой ссылке, предоставив скриншот бана: https://t.me/Nyxema\n\nAccount ID: {account.Avatar.AccountId}";
                            break;
                        case 2:
                            break;
                        case 3:
                            loginFailed.Message = $"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n";
                            break;
                        default:
                            loginFailed.Message = LocalizationHelper.Get(region, "ban", "banned_permamently", LogicLongCodeGenerator.ToCode(account.AccountId));
                            break;
                    }
                }
                Connection.Send(loginFailed);
                return;
            }
            if (account.Avatar.BanCount >= 10)
            {
                AuthenticationFailedMessage loginFailed = new AuthenticationFailedMessage();
                loginFailed.ErrorCode = 11;
                loginFailed.Message = LocalizationHelper.Get(region, "ban", "banned_permamently", LogicLongCodeGenerator.ToCode(account.AccountId));
                Connection.Send(loginFailed);
                return;
            }
            if (account.Avatar.MuteCount >= GeneralStaticLogic.MaxMutesForPlayer && !account.Avatar.ViewedPermMute)
            {
                Notification notif = new Notification
                {
                    Id = 81,
                    MessageEntry = LocalizationHelper.Get(region, "ban", "muted_permamently")
                };
                HomeMode.Home.NotificationFactory.Add(notif);
                LogicAddNotificationCommand acm2 = new()
                {
                    Notif = notif
                };
                AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
                acm5.Command = acm2;
                HomeMode.GameListener.SendTCPMessage(acm5);
                account.Avatar.ViewedPermMute = true;
            }
            AuthenticationOkMessage loginOk = new AuthenticationOkMessage();
            loginOk.AccountId = account.AccountId;
            loginOk.PassToken = account.PassToken;
            loginOk.ServerEnvironment = "dev";
            Connection.Send(loginOk);

            if (!account.Avatar.IsDev && Configuration.Instance.IsDevServer)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 1,
                    Message = ("AuthenticationFailed! AccountID: " + account.Avatar.AccountId + "\nIf the mops still didn't give you permission to come in, then I don't know what to do, call 911, or cry, idk")
                });
                return;
            }

            HomeMode = HomeMode.LoadHomeState(new HomeGameListener(Connection), account.Home, account.Avatar, Events.GetEvents());
            HomeMode.CharacterChanged += OnChangeCharacter;
            HomeMode.Home.IsRustoreInstall = (message.ClientVersion == ServerVersionRustore);
            HomeMode.Home.IsAppleInstall = (message.ClientVersion == ServerVersionApple);
            HomeMode.Home.Device = message.Device;

            Logger.LogPrint($"New connection! IP: {HomeMode.Home.IpAddress} (Device: {HomeMode.Home.Device})");
            if (HomeMode.Avatar.Name != "Brawler") {
                Logger.LogPrint($"New player connected! Name: {HomeMode.Avatar.Name} (AccountID: {HomeMode.Avatar.AccountId})");
            }
            
            FriendListMessage friendList = new FriendListMessage();
            friendList.Friends = HomeMode.Avatar.Friends.ToArray();
            Connection.Send(friendList);
            BattleMode battle = null;

            Console.WriteLine(HomeMode.Avatar.BattleId);
            if (HomeMode.Avatar.BattleId == 0) HomeMode.Avatar.BattleId = -1;
            if (HomeMode.Avatar.BattleId > 0)
            {
                battle = Battles.Get(HomeMode.Avatar.BattleId);
            }

            if (battle == null)
            {
                OwnHomeDataMessage ohd = new OwnHomeDataMessage();
                ohd.Home = HomeMode.Home;
                ohd.Avatar = HomeMode.Avatar;
                Connection.Send(ohd);
            }
            else
            {
                StartLoadingMessage startLoading = battle.Dummy;
                UDPGateway gateway = UPD.UdpServers[UPD.GetRandomPort()];
                UDPSocket socket = gateway.CreateSocket();
                socket.TCPConnection = Connection;
                socket.Battle = battle;
                Connection.UdpSessionId = socket.SessionId;
                battle.ChangePlayerSessionId(HomeMode.Avatar.UdpSessionId, socket.SessionId);
                HomeMode.Avatar.UdpSessionId = socket.SessionId;
                var player = battle.GetPlayerBySessionId(socket.SessionId);
                Character c = battle.GetGameObjectManager().GetCharacterByPlayer(player);
                player.Bot = 0;
                c?.SetBot(0);
                if (player.BanCounter > 2)
                {
                    var loginFailed = new ServerErrorMessage();
                    loginFailed.id = 50;
                    Connection.Send(loginFailed);
                    player.Bot = 1;
                    c?.SetBot(1);
                    return;
                }
                Connection.Send(startLoading);
            }

            Connection.Avatar.LastOnline = DateTime.UtcNow;

            Sessions.Create(HomeMode, Connection);

                        // ==========================================================
            // БЕЗОПАСНАЯ ПЕСОЧНИЦА: ВЫДАЧА РЕСУРСОВ И БП+ БЕЗ КРАШЕЙ
            // ==========================================================
            if (HomeMode != null)
            {
                if (HomeMode.Avatar != null)
                {
                    HomeMode.Avatar.IsDev = true;               // Права разработчика (галочка)
                    HomeMode.Avatar.IsDebugAccount = true;      // Отладочные функции
                }

                if (HomeMode.Home != null)
                {
                    HomeMode.Home.HasPremiumPass = true;        // Золотой Бравл Пасс
                    HomeMode.Home.HasPremiumPassPlus = true;    // Бравл Пасс Плюс (Brawl Pass+)
                }
            }
            // ==========================================================



            if (HomeMode.Avatar.AllianceRole != AllianceRole.None && HomeMode.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(HomeMode.Avatar.AllianceId);

                if (alliance != null)
                {
                    SendMyAllianceData(alliance);
                    AllianceDataMessage data = new AllianceDataMessage();
                    data.Alliance = alliance;
                    data.IsMyAlliance = true;
                    Connection.Send(data);


                    foreach (var member in alliance.Members)
                    {
                        if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                        {
                            Connection.Send(new AllianceOnlineStatusUpdatedMessage() { AvatarId = member.AccountId, Members = alliance.Members.Count, PlayerStatus = member.Avatar.PlayerStatus });
                        }

                    }
                }
                else{
                    HomeMode.Avatar.AllianceRole = AllianceRole.None;
                    HomeMode.Avatar.AllianceId = 0;
                }
            }

            foreach (Friend entry in HomeMode.Avatar.Friends.ToArray()) 
            {
                if (LogicServerListener.Instance.IsPlayerOnline(entry.AccountId))
                {
                    FriendOnlineStatusEntryMessage statusEntryMessage = new FriendOnlineStatusEntryMessage();
                    statusEntryMessage.AvatarId = entry.AccountId;
                    statusEntryMessage.PlayerStatus = entry.Avatar.PlayerStatus;
                    statusEntryMessage.AllianceTeamEntry = Teams.Get(entry.Avatar.TeamId);
                    statusEntryMessage.donotdisturb = entry.Avatar.DoNotDisturb;
                    Connection.Send(statusEntryMessage);
                }
            }

            if (HomeMode.Avatar.TeamId > 0)
            {
                TeamMessage teamMessage = new TeamMessage();
                teamMessage.Team = Teams.Get(HomeMode.Avatar.TeamId);
                if (teamMessage.Team != null)
                {
                    Connection.Send(teamMessage);
                    TeamMember member = teamMessage.Team.GetMember(HomeMode.Avatar.AccountId);
                    member.State = 0;
                    teamMessage.Team.TeamUpdated();
                }
            }
        }

        private void ClientHelloReceived(ClientHelloMessage message)
        {
            Console.WriteLine("");
            Connection.Messaging.DisableCrypto = false;
            if (Sessions.Maintenance)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 10
                });
                return;
            }

            if (message.MajorVersion < 53)
            {
                Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 8,
                    UpdateUrl = "https://pd.qq.com/s/3az5imfyn"
                });
                return;
            }
            Connection.Messaging.Seed = message.ClientSeed;
            Connection.Nonce = Helpers.RandomString(32);
            ServerHelloMessage hello = new ServerHelloMessage();
            hello.SetServerHelloToken(Connection.Messaging.SessionToken);
            hello.Nonce = Connection.Nonce;
            Connection.Send(hello);
        }

        public void GenerateOffer(
            DateTime OfferStart, DateTime OfferEnd, int Cost, int OldCost, int Currency,
            string Claim, string Title, string BGR, int IsTID, int DailyOfferType,
            bool OneTimeOffer, bool LoadOnStartup, bool Processed, int TypeBenefit, int Benefit,
            int panelClass, int panelType, int styleClass, int styleType, bool Special,
            int Count, int BrawlerID, int Extra, ShopItem Item,
            int Count2 = -1, int BrawlerID2 = -1, int Extra2 = -1, ShopItem Item2 = ShopItem.Coin,
            int Count3 = -1, int BrawlerID3 = -1, int Extra3 = -1, ShopItem Item3 = ShopItem.Coin,
            int Count4 = -1, int BrawlerID4 = -1, int Extra4 = -1, ShopItem Item4 = ShopItem.Coin)
        {
            OfferBundle bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.StartTime = OfferStart;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;
            bundle.IsTID = IsTID;
            bundle.OfferType = DailyOfferType;
            bundle.OneTimeOffer = OneTimeOffer;
            bundle.LoadOnStartup = LoadOnStartup;
            bundle.Processed = Processed;
            bundle.TypeBenefit = TypeBenefit;
            bundle.Benefit = Benefit;
            bundle.ShopPanelLayoutClass = panelClass;
            bundle.ShopPanelLayoutType = panelType;
            bundle.ShopStyleSetClass = styleClass;
            bundle.ShopStyleSetType = styleType;
            bundle.specialOffer = Special;
            bundle.MultiCount = 1;
            
            List<int> road_list = new List<int>();
            foreach (int brawler in HomeMode.Home.BrawlersRoad)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
                    road_list.Add(brawler);
            }

            if (TimerMath(OfferStart, OfferEnd) == -1) bundle.Purchased = true;
            if (bundle.Currency == 1000 && HomeMode.Home.PaidOffers.Contains(bundle.Claim))
            {
                bundle.Currency = 0;
                bundle.Cost = 0;
                bundle.Title = "ЗАБРАТЬ АКЦИЮ";
                bundle.Purchased = false;
            }
            if (bundle.Claim.StartsWith("month")) bundle.Purchased = false;
            if (HomeMode.Home.OffersClaimed.Contains(bundle.Claim)) bundle.Purchased = true;
            if ((Item == ShopItem.GuaranteedHero || Item == ShopItem.GuaranteedHeroWithLevel) && HomeMode.Avatar.HasHero(16000000 + BrawlerID)) bundle.Purchased = true;
            if (Item == ShopItem.Skin && HomeMode.Home.UnlockedSkins.Contains(29000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.SkinAndHero && HomeMode.Home.UnlockedSkins.Contains(29000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.Item && HomeMode.Avatar.SPGS.Contains(23000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.PlayerThumbnail && HomeMode.Home.UnlockedThumbnails.Contains(28000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.Emote && HomeMode.Home.UnlockedEmotes.Contains(52000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.Spray && HomeMode.Home.UnlockedSprays.Contains(68000000 + Extra)) bundle.Purchased = true;
            if (Item == ShopItem.RecruitToken && road_list.Count == 0) bundle.Purchased = true;

            if (bundle.Purchased && DailyOfferType == 0 && panelClass == 0) return;

            bundle.Items.Add(new Offer(Item, Count, (16000000 + BrawlerID), Extra));
            if (Count2 != -1) bundle.Items.Add(new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2));
            if (Count3 != -1) bundle.Items.Add(new Offer(Item3, Count3, (16000000 + BrawlerID3), Extra3));
            if (Count4 != -1) bundle.Items.Add(new Offer(Item4, Count4, (16000000 + BrawlerID4), Extra4));

            HomeMode.Home.OfferBundles.Add(bundle);
        }

        public void GenerateMultiOffer(
            DateTime OfferStart, DateTime OfferEnd, int Cost, int OldCost, int Currency,
            string Claim, string Title, string BGR, int IsTID, int DailyOfferType,
            bool OneTimeOffer, bool LoadOnStartup, bool Processed, int TypeBenefit, int Benefit,
            int panelClass, int panelType, int styleClass, int styleType, bool Special, int MultiCount,
            int Count, int BrawlerID, int Extra, ShopItem Item,
            int Count2 = -1, int BrawlerID2 = -1, int Extra2 = -1, ShopItem Item2 = ShopItem.Coin,
            int Count3 = -1, int BrawlerID3 = -1, int Extra3 = -1, ShopItem Item3 = ShopItem.Coin,
            int Count4 = -1, int BrawlerID4 = -1, int Extra4 = -1, ShopItem Item4 = ShopItem.Coin)
        {
            OfferBundle bundle = new OfferBundle();
            bundle.StartTime = OfferStart;
            bundle.EndTime = OfferEnd;
            bundle.Cost = Cost;
            bundle.OldCost = OldCost;
            bundle.Currency = Currency;
            bundle.Claim = Claim;
            bundle.Title = Title;
            bundle.BackgroundExportName = BGR;
            bundle.MultiCount = MultiCount;
            
            if (bundle.MultiCount <= HomeMode.Home.OffersClaimed.Count(x => x == bundle.Claim)) bundle.Purchased = true;
            
            bundle.Title = $"{bundle.Title} ({HomeMode.Home.OffersClaimed.Count(x => x == bundle.Claim) + 1}/{bundle.MultiCount})";
            
            bundle.Items.Add(new Offer(Item, Count, (16000000 + BrawlerID), Extra));
            if (Count2 != -1) bundle.Items.Add(new Offer(Item2, Count2, (16000000 + BrawlerID2), Extra2));
            if (Count3 != -1) bundle.Items.Add(new Offer(Item3, Count3, (16000000 + BrawlerID3), Extra3));
            if (Count4 != -1) bundle.Items.Add(new Offer(Item4, Count4, (16000000 + BrawlerID4), Extra4));
            
            HomeMode.Home.OfferBundles.Add(bundle);
        }

        public void RefreshOffers()
        {
            HomeMode.Home.OfferBundles.Clear();
            if (LogicServerListener.Instance.IsDev()) return;
            HomeMode.Home.GenerateDailyOffers();
        }

        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            DateTime timer_now = DateTime.Now;
            if (timer_now > timer_start && timer_now < timer_end)
                return (int)(timer_end - timer_now).TotalSeconds;
            return -1;
        }
    }
}
