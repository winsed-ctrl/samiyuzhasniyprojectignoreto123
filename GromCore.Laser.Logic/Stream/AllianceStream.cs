namespace GromCore.Laser.Logic.Stream
{
    using Masuda.Net.Models;
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Club;
    using GromCore.Laser.Logic.Stream.Entry;

    public class AllianceStream
    {
        public const int MAX_STREAM_ENTRY_COUNT = 50;

        [JsonProperty("entry_id_counter")] public long EntryIdCounter;
        [JsonProperty("entries")] public List<AllianceStreamEntry> StreamEntryList;

        public AllianceStream()
        {
            StreamEntryList = new List<AllianceStreamEntry>();
        }

        public AllianceStreamEntry SendChatMessage(AllianceMember author, string content)
        {
            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = author.AccountId;
            entry.AuthorName = author.DisplayData.Name;
            entry.AuthorRole = author.Role;
            entry.Id = ++EntryIdCounter;
            entry.Type = 2;
            entry.Message = content.Length > 128 ? content.Substring(0, 128) : content;
            AddEntry(entry);

            return entry;
        }

        public AllianceStreamEntry SendPremadeChat(AllianceMember author, int slot, int emoteid)
        {
            AllianceStreamEntry entry = new AllianceStreamEntry();
            entry.AuthorId = author.AccountId;
            entry.AuthorName = author.DisplayData.Name;
            entry.AuthorRole = author.Role;
            entry.Id = ++EntryIdCounter;
            entry.Type = 8;
            entry.MessageData = slot;
            entry.EmoteId = emoteid;
            AddEntry(entry);

            return entry;
        }

        public void AddEntry(AllianceStreamEntry entry)
        {
            if (StreamEntryList.Count >= MAX_STREAM_ENTRY_COUNT)
            {
                StreamEntryList.Remove(StreamEntryList[0]);
            }
            StreamEntryList.Add(entry);
        }

        public AllianceStreamEntry[] GetEntries()
        {
            if (StreamEntryList.Count >= MAX_STREAM_ENTRY_COUNT)
            {
                StreamEntryList.Remove(StreamEntryList[0]);
            }
            return StreamEntryList.ToArray();
        }
    }
}
