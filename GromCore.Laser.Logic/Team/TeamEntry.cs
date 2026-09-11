namespace GromCore.Laser.Logic.Team
{
    using System.Globalization;
    using System.Xml.Linq;
    using Microsoft.Win32.SafeHandles;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Message.Team;
    using GromCore.Laser.Logic.Message.Team.Stream;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Logic.Team.Stream;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class TeamEntry
    {
        public long Id;
        public int Type;

        public int LocationId;

        public List<TeamMember> Members { get; set; }
        public List<TeamInviteEntry> Invites { get; set; }
        public List<TeamJoinRequest> JoinRequests { get; set; }
        public List<int> DisabledBots { get; set; }
        public int EventSlot { get; set; }
        public List<int> CustomModifiers;

        public BattlePlayerMap BattlePlayerMap;
        private long EntryCounter;
        public List<TeamStreamEntry> Stream { get; private set; }

        public TeamEntry()
        {
            Members = new List<TeamMember>();
            Invites = new List<TeamInviteEntry>();
            JoinRequests = new List<TeamJoinRequest>();
            DisabledBots = new List<int>();
            Stream = new List<TeamStreamEntry>();
            CustomModifiers = new List<int>();

            EventSlot = 1;
            EntryCounter = 0;
        }

        public void AddStreamEntry(TeamStreamEntry entry)
        {
            if (entry == null) return;

            entry.Id = ++EntryCounter;
            Stream.Add(entry);
            TeamStreamMessage message = new TeamStreamMessage();
            message.TeamId = Id;
            message.Entries = new TeamStreamEntry[] { entry };

            foreach (var member in Members)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
        }

        public void StreamUpdated()
        {
            
        }

        public TeamMember GetMember(long id)
        {
            return Members.Find(member => member.AccountId == id);
        }

        public TeamInviteEntry GetInviteById(long id)
        {
            return Invites.Find(invite => invite.Id == id);
        }

        public bool IsEveryoneReady()
        {
            foreach (TeamMember member in Members)
            {
                if (!member.IsReady) return false;
            }
            return true;
        }

        public void TeamUpdated()
        {
            TeamMessage message = new TeamMessage();
            message.Team = this;

            foreach (var member in Members)
            {
                if(member.homeMode.Avatar.HasHero(member.CharacterId)){
                    var h = member.homeMode.Avatar.GetHero(member.CharacterId);
                    member.HeroTrophies = h.Trophies;
                    member.HeroHighestTrophies = h.HighestTrophies;
                }
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
            if(Type != 1 && Members.Count > 3)
            {
                Members.Remove(Members.First());
            }
            if(Members.Count(m => m.IsOwner) < 1)
            {
                if (Members.FirstOrDefault() != null)
                {
                    Members.FirstOrDefault().IsOwner = true;
                    TeamUpdated();
                }
            }
            
        }

        public List<TeamMember> GetMembers()
        {
            return Members;
        }

        public void SendNotificationToAllMembers(string message)
        {
            foreach (TeamMember member in Members)
            {
                LogicAddNotificationCommand logicAddNotificationCommand = new()
                {
                    Notification = new FloaterTextNotification(message)
                };

                AvailableServerCommandMessage availableServerCommandMessage = new()
                {
                    Command = logicAddNotificationCommand
                };

                LogicServerListener.Instance.GetGameListener(member.AccountId)?.SendTCPMessage(availableServerCommandMessage);
            }
        }
        public void TeamError(int id)
        {
            TeamErrorMessage message = new();
            message.ErrorCode = id;
            foreach (var member in Members)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    LogicServerListener.Instance.GetGameListener(member.AccountId).SendTCPMessage(message);
                }
            }
        }

        public int GetCount() { return Members.Count; }
        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteBoolean(Type == 1);
            stream.WriteVInt(3); // Team capacity

            stream.WriteLong(Id);

            stream.WriteVInt(0);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(Type == 1 ? 0 : EventSlot);
            stream.WriteVInt(0);

            //if(LocationId==15000057) ByteStreamHelper.WriteDataReference(stream, 15000459);
            ByteStreamHelper.WriteDataReference(stream, LocationId);
            ByteStreamHelper.WriteBattlePlayerMap(stream, BattlePlayerMap);

            stream.WriteVInt(Members.Count);
            foreach (TeamMember entry in Members.ToArray())
            {
                entry.Encode(stream);
            }

            stream.WriteVInt(Invites.Count);
            foreach (TeamInviteEntry entry in Invites.ToArray())
            {
                entry.Encode(stream);
            }

            stream.WriteVInt(JoinRequests.Count);
            foreach (TeamJoinRequest request in JoinRequests.ToArray())
            {
                request.Encode(stream);
            }

            stream.WriteVInt(DisabledBots.Count);
            foreach(int i in DisabledBots)
            {
                stream.WriteVInt(i);
            }
            stream.WriteBoolean(!Members.Any(m => m.homeMode != null && m.homeMode.Avatar.ChatMutestate == 1));//enable chat
            stream.WriteBoolean(true);//Show Accessory
            stream.WriteBoolean(true);//Show Gears
            //stream.WriteBoolean(false);

            ByteStreamHelper.WriteIntList(stream, CustomModifiers);
        }
        
        public void EncodeAllianceTeamEntry(ByteStream stream)
        {
            stream.WriteVInt(Type);
            stream.WriteVInt(3);//MaxPlayer
            stream.WriteLong(Id);
            stream.WriteVInt(0);//?

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);

            stream.WriteVInt(Members.Count);
            foreach (TeamMember entry in Members.ToArray())
            {
                entry.Encode(stream);
            }

            ByteStreamHelper.WriteDataReference(stream, LocationId);
            stream.WriteVInt(0);
            stream.WriteBoolean(false);
        }
    }
}
