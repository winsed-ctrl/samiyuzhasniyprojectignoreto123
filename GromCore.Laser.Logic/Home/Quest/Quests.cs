namespace GromCore.Laser.Logic.Home.Quest
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using System;

    public class Quests
    {
        private static readonly int[] ALLOWED_MODES = { 0, 3, 6, 2, 28, 25 };
        private static readonly Dictionary<int, int> KILLS_GOAL_TABLE = new Dictionary<int, int> { { 15, 500 }, { 25, 1000 }, { 50, 2500 } };
        private static readonly Dictionary<int, int> DAMAGE_GOAL_TABLE = new Dictionary<int, int> { { 100000, 500 } };
        private static readonly Dictionary<int, int> HEAL_GOAL_TABLE = new Dictionary<int, int> { { 5, 500 }, { 10, 1000 } };
        private static readonly Dictionary<int, int> WIN_GOAL_TABLE = new Dictionary<int, int>{ { 5, 500 }, { 8, 1000 } };

        [JsonProperty("quest_list")] public List<Quest> QuestList;

        public Quests()
        {
            QuestList = new List<Quest>();
        }
        public void AddGemOfferQuset(int missiontype, GemOffer g, int questgoal, List<int> charids, List<int> evids, int timer, int specialId, bool s)
        {
            var quest = new Quest();
            if (g == null) return;
            quest.MissionType = missiontype;
            quest.GemOffer = g;
            quest.QuestGoal = questgoal;
            if (charids != null) quest.CharacterIds = charids;
            if (evids != null) quest.EventVariations = evids;
            quest.Id = specialId;
            quest.Time = timer;
            quest.ShouldShow = s;
            quest.QuestType = 0;
            QuestList.Add(quest);
        }
        public Quest GenerateQuest(List<Hero> u, bool season, bool needpass = false)
        {
            
            List<int> characters = u.Select(x => x.CharacterId).ToList();
            Random rand = new();
            Quest quest = new();

            quest.MissionType = rand.Next(1, 2);

            bool forCharacter = (rand.Next(0, 2) == 0) ? true : false;
            if (forCharacter)
            {
                List<int> temp = new List<int>();
                int tempint = (rand.Next(0, 2) == 0) ? 1 : 3;

                for (int ij = 0; ij < tempint; ij++)
                {
                    int characterarleadyadded;
                    do
                        characterarleadyadded = characters[rand.Next(0, characters.Count)];
                    while (temp.Contains(characterarleadyadded));

                    temp.Add(characterarleadyadded);
                }

                quest.CharacterIds = temp;
            }

            else
            {
                List<int> temp = new List<int>();
                int tempint = (rand.Next(0, 2) == 0) ? 1 : 2;

                for (int ij = 0; ij < tempint; ij++)
                {
                    int eventarealyadded;
                    do
                        eventarealyadded = ALLOWED_MODES[rand.Next(0, ALLOWED_MODES.Length)];
                    while (temp.Contains(eventarealyadded));

                    temp.Add(eventarealyadded);
                }

                quest.EventVariations = temp;
            }

            switch (quest.MissionType)
            {
                case 1:
                    var idk = WIN_GOAL_TABLE.ElementAt(0);
                    double chance = rand.NextDouble() * 100;
                    if (chance < 40)
                    {
                        idk = WIN_GOAL_TABLE.ElementAt(0);
                    }
                    else if (chance < 50)
                    {
                        idk = WIN_GOAL_TABLE.ElementAt(1);
                    }
                    if (!season)
                    {
                        quest.QuestGoal = 3;
                        quest.RewardCount = 200;
                        break;
                    } 
                    quest.QuestGoal = idk.Key;
                    quest.RewardCount = idk.Value;
                    break;
                case 2:
                    var idk1 = KILLS_GOAL_TABLE.ElementAt(0);
                    double chance1 = rand.NextDouble() * 100;
                    if (chance1 < 40)
                    {
                        idk = KILLS_GOAL_TABLE.ElementAt(0);
                    }
                    else if (chance1 < 50)
                    {
                        idk = KILLS_GOAL_TABLE.ElementAt(1);
                    }
                    else if (chance1 < 60)
                    {
                        idk = KILLS_GOAL_TABLE.ElementAt(2);
                    }
                    if (!season)
                    {
                        quest.QuestGoal = 9;
                        quest.RewardCount = 200;
                        break;
                    }
                    quest.QuestGoal = idk1.Key;
                    quest.RewardCount = idk1.Value;
                    break;
                case 3:
                    var idk2 = DAMAGE_GOAL_TABLE.ElementAt(0);
                    double chance2 = rand.NextDouble() * 100;
                    idk = DAMAGE_GOAL_TABLE.ElementAt(0);
                    if (!season)
                    {
                        quest.QuestGoal = 60000;
                        quest.RewardCount = 200;
                        break;
                    }
                    quest.QuestGoal = idk2.Key;
                    quest.RewardCount = idk2.Value;
                    

                    break;
            }
            quest.NeedPass = needpass;
            quest.QuestType = season ? 1 : 2;
            quest.Id = QuestList.Count + 1;
            return quest;
        }
        public void AddRandomQuests(List<Hero> unlockedHeroes, bool haspass)
        {
            int _defaultQuestsCount = 2;
            if (haspass) _defaultQuestsCount = 3;
            List<int> _seasonQuestCount = new List<int> { 2, 1 };

            QuestList.RemoveAll(q => q.QuestType == 2);

            for (int i = 0; i < _defaultQuestsCount; i++)
                QuestList.Add(GenerateQuest(unlockedHeroes, false));

            for (int i = 0; i < _seasonQuestCount[0]; i++)
                QuestList.Add(GenerateQuest(unlockedHeroes, true));

            for (int i = 0; i < _seasonQuestCount[1]; i++)
                QuestList.Add(GenerateQuest(unlockedHeroes, true, true));
        }

        public List<Quest> UpdateQuestsProgress(int gameModeVariation, int characterId, int kills, int damage, int heals, ClientHome home)
        {
            List<Quest> completed = new List<Quest>();
            List<Quest> progressive = new List<Quest>();

            foreach (Quest quest in QuestList.ToArray())
            {
                if (!home.HasPremiumPass && quest.NeedPass) continue;
                if ((quest.EventVariations != null && quest.EventVariations.Contains(gameModeVariation)) || (quest.CharacterIds != null && quest.CharacterIds.Contains(characterId)))
                {
                    if (quest.MissionType == 1)
                    {
                        var progress = quest.Clone();
                        progress.Progress = 1;

                        quest.CurrentGoal += 1;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 2)
                    {
                        var progress = quest.Clone();
                        progress.Progress = kills;

                        quest.CurrentGoal += kills;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 3)
                    {
                        var progress = quest.Clone();
                        progress.Progress = damage;

                        quest.CurrentGoal += damage;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 4)
                    {
                        break;
                        var progress = quest.Clone();
                        progress.Progress = heals;
                        if (progress.CurrentGoal + progress.Progress > progress.QuestGoal)
                        {
                            //progress.Progress = progress.QuestGoal - progress.CurrentGoal;
                        }

                        quest.CurrentGoal += heals;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            //completed.Add(quest);
                        }

                        //progressive.Add(progress);
                    }
                }
                if (quest.EventVariations == null && quest.CharacterIds == null)
                {
                    if (quest.MissionType == 1)
                    {
                        var progress = quest.Clone();
                        progress.Progress = 1;

                        quest.CurrentGoal += 1;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 2)
                    {
                        var progress = quest.Clone();
                        progress.Progress = kills;

                        quest.CurrentGoal += kills;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                    else if (quest.MissionType == 3)
                    {
                        var progress = quest.Clone();
                        progress.Progress = damage;

                        quest.CurrentGoal += damage;
                        if (quest.CurrentGoal >= quest.QuestGoal)
                        {
                            completed.Add(quest);
                        }

                        progressive.Add(progress);
                    }
                }
            }

            foreach (Quest quest in completed)
            {
                if(quest.GemOffer != null) goto LABEL_1;
                QuestList.Remove(quest);
                home.TokenReward += quest.RewardCount;
                home.BrawlPassTokens += quest.RewardCount;
            LABEL_1:
                home.PendingReward.Add(quest.GemOffer);
                QuestList.Remove(quest);
            }

            return progressive;
        }

        public void Encode(ChecksumEncoder encoder, int season)
        {
            /*
             *             encoder.WriteVInt(QuestList.FindAll(q => !q.ShouldShow).Count);
            foreach (Quest quest in QuestList.ToArray())
            {
                Console.WriteLine(quest.QuestGoal);
                if(!quest.ShouldShow) quest.EncodeHome(encoder,season);
            }
            */
            encoder.WriteVInt(QuestList.Count);
            foreach (Quest quest in QuestList.ToArray())
            {
                quest.EncodeHome(encoder, season);
            }
        }
    }
}