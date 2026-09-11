namespace GromCore.Laser.Logic.Club
{
    using Masuda.Net.Models;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Club;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Message.Team;
    using GromCore.Laser.Logic.Stream;
    using GromCore.Laser.Logic.Stream.Entry;
    using GromCore.Laser.Titan.DataStream;
    using System.ComponentModel;
    using System.Reflection;
    using System.Threading;

    public class Alliance
    {
        [JsonProperty("id")] public long Id;

        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("desc")] public string Description { get; set; }
        [JsonProperty("members")] public List<AllianceMember> Members { get; set; }
        [JsonProperty("badge")] public int AllianceBadgeId { get; set; }
        [JsonProperty("required_trophies")] public int RequiredTrophies { get; set; }
        [JsonProperty("type")] public int Type { get; set; }
        [JsonProperty("stream")] public AllianceStream Stream { get; set; }
        [JsonProperty("mail")] public List<Home.Items.Notification> AllianceMail { get; set; }
        [JsonProperty("country")] public string Country { get; set; }
        [JsonProperty("hasactivepiggy")] public bool HasActivePiggy { get; set; }
        [JsonProperty("piggywins")] public bool PiggyBankWins { get; set; }
        [JsonProperty("listofdrops")] public List<int> PiggyBankBonusList { get; set; }

        [JsonIgnore]
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public Alliance()
        {
            Name = "Alliance";
            Description = string.Empty;
            Members = new List<AllianceMember>();
            AllianceBadgeId = GlobalId.CreateGlobalId(8, 0);
            Stream = new AllianceStream();
            Type = 1;
            AllianceMail = new();
            Country = "RU";
            PiggyBankBonusList = new();
        }

        public int OnlinePlayers
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    int result = 0;
                    foreach (AllianceMember entry in Members)
                    {
                        if (LogicServerListener.Instance.IsPlayerOnline(entry.AccountId)) result++;
                    }
                    return result;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public void AddStreamEntry(AllianceStreamEntry entry)
        {
            Stream.AddEntry(entry);
            SendAllianceStreamEntryToAll(entry);
        }

        public void SendChatMessage(long id, string message)
        {
            AllianceMember member = GetMemberById(id);
            if (member == null) return;
            AllianceStreamEntry entry = Stream.SendChatMessage(member, message);
            SendAllianceStreamEntryToAll(entry);
        }

        public void SendPremadeChat(long id, int slot, int emoteid)
        {
            AllianceMember member = GetMemberById(id);
            if (member == null) return;
            AllianceStreamEntry entry = Stream.SendPremadeChat(member, slot, emoteid);
            SendAllianceStreamEntryToAll(entry);
        }

        public void SendAllianceStreamEntryToAll(AllianceStreamEntry entry)
        {
            AllianceStreamEntryMessage message = new AllianceStreamEntryMessage();
            message.Entry = entry;

            List<AllianceMember> membersCopy;
            _lock.EnterReadLock();
            try
            {
                membersCopy = new List<AllianceMember>(Members);
            }
            finally
            {
                _lock.ExitReadLock();
            }

            foreach (AllianceMember member in membersCopy)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    try
                    {
                        LogicGameListener listener = LogicServerListener.Instance.GetGameListener(member.AccountId);
                        listener?.SendTCPMessage(message);
                    }
                    catch { }
                }
            }
        }

        public void SendAllianceMail(string text, AllianceMember owner)
        {
            _lock.EnterWriteLock();
            try
            {
                if (!Members.Contains(owner)) return;
                
                Home.Items.Notification notif = new();
                notif.Id = 82;
                notif.MessageEntry = text;
                notif.DateTime = DateTime.Now;
                notif.Index = 5000 + AllianceMail.Count;
                PlayerDisplayData disp = new(owner.HomeMode.Home.ThumbnailId, owner.HomeMode.Home.NameColorId, owner.Avatar.Name, owner.HomeMode.Home.HasPremiumPass, owner.HomeMode.Home.HasPremiumPassPlus);
                notif.BandDisplayData = disp;
                LogicAddNotificationCommand cmd = new() { Notif = notif };
                AvailableServerCommandMessage ascm = new AvailableServerCommandMessage();
                ascm.Command = cmd;
                
                List<AllianceMember> membersCopy = new List<AllianceMember>(Members);
                
                foreach (AllianceMember member in membersCopy)
                {
                    if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                    {
                        try
                        {
                            LogicGameListener listener = LogicServerListener.Instance.GetGameListener(member.AccountId);
                            listener?.SendTCPMessage(ascm);
                        }
                        catch { }
                    }
                }
                AllianceMail.Add(notif);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public AllianceMember GetMemberById(long id)
        {
            _lock.EnterReadLock();
            try
            {
                foreach (var member in Members)
                {
                    if (member.AccountId == id)
                        return member;
                }
                return null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public void RemoveMemberById(long id) 
        {
            _lock.EnterWriteLock();
            try
            {
                Members.RemoveAll(m => m.AccountId == id);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool LeaveClub(long accountId)
        {
            AllianceMember leavingMember = null;
            
            _lock.EnterReadLock();
            try
            {
                foreach (var member in Members)
                {
                    if (member.AccountId == accountId)
                    {
                        leavingMember = member;
                        break;
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            
            if (leavingMember == null)
                return false;

            // Выходит НЕ владелец
            if ((int)leavingMember.Role != 3)
            {
                RemoveMemberById(accountId);
                
                _lock.EnterReadLock();
                try
                {
                    if (Members.Count > 0)
                    {
                        AllianceStreamEntry leaveEntry = Stream.SendChatMessage(leavingMember, $"{leavingMember.DisplayData.Name} has left the club.");
                        SendAllianceStreamEntryToAll(leaveEntry);
                    }
                }
                finally
                {
                    _lock.ExitReadLock();
                }
                
                return true;
            }

            // Выходит ВЛАДЕЛЕЦ
            List<AllianceMember> otherMembers = new List<AllianceMember>();
            _lock.EnterReadLock();
            try
            {
                foreach (var member in Members)
                {
                    if (member.AccountId != accountId)
                        otherMembers.Add(member);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
            
            if (otherMembers.Count > 0)
            {
                AllianceMember newOwner = SelectNewOwner(otherMembers);
                
                if (newOwner != null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        var roleProperty = typeof(AllianceMember).GetProperty("Role");
                        if (roleProperty != null)
                        {
                            var ownerValue = Enum.ToObject(leavingMember.Role.GetType(), 3);
                            roleProperty.SetValue(newOwner, ownerValue);
                        }
                        
                        Members.RemoveAll(m => m.AccountId == accountId);
                        
                        if (Members.Count > 0)
                        {
                            AllianceStreamEntry ownerLeftEntry = Stream.SendChatMessage(newOwner, $"{leavingMember.DisplayData.Name} has transferred leadership to {newOwner.DisplayData.Name} and left the club.");
                            SendAllianceStreamEntryToAll(ownerLeftEntry);
                        }
                        
                        SendAllianceMail($"You are now the owner of {Name}!", newOwner);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                    
                    return true;
                }
            }
            
            // Удаляем клуб
            DeleteAlliance();
            return true;
        }

        private AllianceMember SelectNewOwner(List<AllianceMember> otherMembers)
        {
            if (otherMembers == null || otherMembers.Count == 0)
                return null;
            
            List<AllianceMember> vicePresidents = new List<AllianceMember>();
            foreach (var m in otherMembers)
            {
                if ((int)m.Role == 2)
                    vicePresidents.Add(m);
            }
            
            if (vicePresidents.Count > 0)
            {
                vicePresidents.Sort((a, b) => b.Trophies.CompareTo(a.Trophies));
                return vicePresidents[0];
            }
            
            List<AllianceMember> seniors = new List<AllianceMember>();
            foreach (var m in otherMembers)
            {
                if ((int)m.Role == 1)
                    seniors.Add(m);
            }
            
            if (seniors.Count > 0)
            {
                seniors.Sort((a, b) => b.Trophies.CompareTo(a.Trophies));
                return seniors[0];
            }
            
            AllianceMember memberWithMostTrophies = null;
            int maxTrophies = -1;
            foreach (var m in otherMembers)
            {
                if (m.Trophies > maxTrophies)
                {
                    maxTrophies = m.Trophies;
                    memberWithMostTrophies = m;
                }
            }
            
            if (memberWithMostTrophies != null)
                return memberWithMostTrophies;
            
            return otherMembers[0];
        }

        private void DeleteAlliance()
        {
            List<AllianceMember> membersCopy;
            _lock.EnterWriteLock();
            try
            {
                if (Members.Count == 0) return;
                
                membersCopy = new List<AllianceMember>(Members);
                Members.Clear();
                AllianceMail.Clear();
                PiggyBankBonusList.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
            foreach (AllianceMember member in membersCopy)
            {
                if (LogicServerListener.Instance.IsPlayerOnline(member.AccountId))
                {
                    try
                    {
                        LogicGameListener listener = LogicServerListener.Instance.GetGameListener(member.AccountId);
                        if (listener != null)
                        {
                            AllianceStreamEntry deleteEntry = Stream.SendChatMessage(null, "[SYSTEM] The club has been deleted.");
                            AllianceStreamEntryMessage message = new AllianceStreamEntryMessage();
                            message.Entry = deleteEntry;
                            listener.SendTCPMessage(message);
                        }
                    }
                    catch { }
                }
                
                var avatar = LogicServerListener.Instance.GetAvatar(member.AccountId);
                if (avatar != null)
                {
                    avatar.AllianceId = 0;
                }
            }
        }

        // ВАЖНО: Trophies НЕ использует блокировку, чтобы избежать рекурсии
        public int Trophies
        {
            get
            {
                int result = 0;
                foreach (AllianceMember member in Members)
                {
                    result += member.Avatar.Trophies;
                }
                return result;
            }
        }

        // Header также НЕ использует блокировку
        public AllianceHeader Header
        {
            get
            {
                return new AllianceHeader(Id, Name, AllianceBadgeId, Members.Count, Trophies, RequiredTrophies, Type, Country);
            }
        }

        public void Encode(ByteStream stream)
        {
            _lock.EnterReadLock();
            try
            {
                Header.Encode(stream);
                stream.WriteString(Description);

                AllianceMember[] members = Members.Take(30).ToArray();
                stream.WriteVInt(members.Length);
                foreach (AllianceMember member in members)
                {
                    member.Encode(stream);
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
}