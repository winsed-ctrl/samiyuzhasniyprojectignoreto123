namespace GromCore.Laser.Logic.Home.Quest
{
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Titan.DataStream;
    using System.IO;

    public class Quest
    {
        public int MissionType { get; set; }
        public int CurrentGoal { get; set; }
        public int QuestGoal { get; set; }
        public List<int> CharacterIds { get; set; }
        public List<int> EventVariations { get; set; }
        public int RewardId { get; set; }
        public int RewardCount { get; set; }
        public int RewardGlobalId { get; set; }
        public int RewardExtra { get; set; }
        public GemOffer GemOffer { get; set; }
        public int Progress { get; set; }
        public bool Seen { get; set; }
        public int Id { get; set; }
        public int Time { get; set; }
        public bool ShouldShow { get; set; }
        public int QuestType { get; set; }
        public bool NeedPass { get; set; }
        public bool UsedRefresh { get; set; }

        public Quest Clone()
        {
            return new Quest()
            {
                MissionType = MissionType,
                CurrentGoal = CurrentGoal,
                QuestGoal = QuestGoal,
                CharacterIds = CharacterIds,
                EventVariations = EventVariations,
                RewardId = RewardId,
                RewardCount = RewardCount,
                RewardGlobalId = RewardGlobalId,
                RewardExtra = RewardExtra,
                Progress = Progress,
                Seen = Seen,
                GemOffer = GemOffer,
                Time = Time,
                ShouldShow = ShouldShow,
                QuestType = QuestType,
                NeedPass = NeedPass,
                UsedRefresh= UsedRefresh
            };
        }

        public void EncodeHome(ChecksumEncoder stream, int bpseason = 1)
        {
            /* Quest types:
             * 0 = special
             * 1 = season
             * 2 = daily
             */
            List<int> tempbrawlers = new();
            List<int> tempevents = new();
            if (CharacterIds != null) tempbrawlers = CharacterIds;
            if (EventVariations != null) tempevents = EventVariations;
            stream.WriteVInt(Id);
            stream.WriteVInt(bpseason);
            stream.WriteVInt(MissionType);
            stream.WriteVInt(CurrentGoal);
            stream.WriteVInt(QuestGoal);
            stream.WriteVInt(tempbrawlers.Count);
            foreach (int i in tempbrawlers)
            {
                ByteStreamHelper.WriteDataReference(stream, i);
            }

            stream.WriteVInt(tempevents.Count);
            foreach (int i in tempevents)
            {
                stream.WriteVInt(i);
            }
            stream.WriteVInt(0);

            stream.WriteVInt(Progress);
            stream.WriteBoolean(false);
            stream.WriteBoolean(false);
            if(GemOffer == null)
            {
                stream.WriteVInt(RewardId);
                stream.WriteVInt(RewardCount);
                ByteStreamHelper.WriteDataReference(stream, RewardGlobalId);
                stream.WriteVInt(RewardExtra);
            }
            else
            {
                GemOffer.Encode(stream);
            }
            stream.WriteBoolean(false);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0); // etap
            stream.WriteVInt(QuestType == 2 ? 66462 : Time); // Timer
            stream.WriteBoolean(NeedPass);
            stream.WriteBoolean(Seen);
            stream.WriteBoolean(UsedRefresh); // used refresh
            stream.WriteVInt(22);
            stream.WriteVInt(QuestType == 0 ? 0 : -1);
            stream.WriteVInt(QuestType == 2 ? 1719737999 : 0);
        }
    }
}