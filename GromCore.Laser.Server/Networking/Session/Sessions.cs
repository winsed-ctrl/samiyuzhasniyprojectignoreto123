namespace GromCore.Laser.Server.Networking.Session
{
    using Masuda.Net;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Friends;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Friends;
    using GromCore.Laser.Logic.Message.Team.Stream;
    using GromCore.Laser.Logic.Team.Stream;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Server.Database;
    using GromCore.Laser.Server.Logic.Game;
    using System.Collections.Concurrent;
    using System.Net.Sockets;

    public static class Sessions
    {
        public static bool Maintenance;
        public static ConcurrentDictionary<long, Session> ActiveSessions = new ConcurrentDictionary<long, Session>();
        public static long GlobalMessageCount;

        public static int Count
        {
            get
            {
                return ActiveSessions.Count;
            }
        }

        public static void Update()
        {
            
            //Thread.Sleep(1000);
        }
        public static void Init()
        {
            ActiveSessions = new ConcurrentDictionary<long, Session>();
            GlobalMessageCount = 0;
            //new Thread(Update).Start();
        }

        public static void StartShutdown()
        {
            Maintenance = true;
            foreach (var session in ActiveSessions.Values.ToArray())
            {
                session.Connection.Send(new ShutdownStartedMessage());
            }

            Thread.Sleep(1000);

            foreach (var session in ActiveSessions.Values.ToArray())
            {
                session.Connection.Send(new AuthenticationFailedMessage()
                {
                    ErrorCode = 10,
                });
            }
        }

        public static void NotificationForAll(string Text)
        {
            foreach (var session in ActiveSessions.Values.ToArray())
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new LogicAddNotificationCommand();
                logicAddNotificationCommand.Notification = new FloaterTextNotification(Text);
                AvailableServerCommandMessage availableServerCommandMessage = new AvailableServerCommandMessage();
                availableServerCommandMessage.Command = logicAddNotificationCommand;
                session.Connection.Send(availableServerCommandMessage);
                MatchMakingCancelledMessage cancelledMessage = new();
                session.Connection.Send(cancelledMessage);
                return;
            }
        }

        public static void SendGlobalMessage(long AccountId, string name, string message)
        {
            foreach (var session in ActiveSessions.Values.ToArray())
            {
                if (session.Connection.MessageManager.IsAlive())
                {
                    session.Connection.Send(new TeamStreamMessage() { TeamId = -1, Entries = new TeamStreamEntry[] { new ChatStreamEntry() { Id = GlobalMessageCount, AccountId = AccountId, Name = name, Message = message } } });
                }
            }
            GlobalMessageCount++;

        }
        public static void Remove(long id)
        {
            if (ActiveSessions.ContainsKey(id))
            {
                ActiveSessions[id].Home.Avatar.LastOnline = DateTime.UtcNow;
                ActiveSessions[id].Home.Avatar.PlayerStatus = 0;

                FriendOnlineStatusEntryMessage entryMessage = new FriendOnlineStatusEntryMessage();
                entryMessage.AvatarId = ActiveSessions[id].Home.Avatar.AccountId;
                entryMessage.PlayerStatus = 0;


                foreach (Friend friend in ActiveSessions[id].Home.Avatar.Friends.ToArray())
                {
                    if (LogicServerListener.Instance.IsPlayerOnline(friend.AccountId))
                    {
                        LogicServerListener.Instance.GetGameListener(friend.AccountId).SendTCPMessage(entryMessage);
                    }
                }

                if (ActiveSessions[id].Home.Avatar.AllianceRole != AllianceRole.None && ActiveSessions[id].Home.Avatar.AllianceId > 0)
                {
                    Alliance alliance = Alliances.Load(ActiveSessions[id].Home.Avatar.AllianceId);

                    if (alliance != null)
                    {
                        AllianceOnlineStatusUpdatedMessage allianceOnlineStatusUpdatedMessage = new AllianceOnlineStatusUpdatedMessage()
                        {
                            AvatarId = ActiveSessions[id].Home.Avatar.AccountId,
                            Members = alliance.Members.Count,
                            PlayerStatus = 0
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

                if (ActiveSessions[id].Home.Avatar.TeamId > 0)
                {
                    var team = Teams.Get(ActiveSessions[id].Home.Avatar.TeamId);
                    if (team != null)
                    {
                        var member = team.GetMember(id);
                        if (member != null)
                        {
                            member.State = 0;
                            team.TeamUpdated();
                        }
                    }
                }
            }
            //return;
            ActiveSessions.Remove(id, out _);
        }

        public static Session Create(HomeMode home, Connection connection)
        {

            if (ActiveSessions.ContainsKey(home.Avatar.AccountId))
            {
                Session oldSession = ActiveSessions[home.Avatar.AccountId];
                Session s = new Session(home, connection);
                ActiveSessions[home.Avatar.AccountId] = s;
                return s;
            }

            Session session = new Session(home, connection);
            ActiveSessions[home.Avatar.AccountId] = session;
            return session;
        }

        public static bool IsSessionActive(long id)
        {
            //if (!ActiveSessions.ContainsKey(id))
            //{
            //    ;
            //}
            return ActiveSessions.ContainsKey(id);
        }

        public static Session GetSession(long id)
        {
            if (ActiveSessions.ContainsKey(id))
            {
                return ActiveSessions[id];
            }
            return null;
        }
    }
}
