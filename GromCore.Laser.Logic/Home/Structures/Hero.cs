namespace GromCore.Laser.Logic.Home.Structures
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Util;
    using System.Security.Cryptography;

    [JsonObject(MemberSerialization.OptIn)]
    public class Hero
    {

        public static readonly int[] UpgradePowerPointsTable = new int[]
        {
            20, 50, 100, 180, 310, 520, 860, 1410, 2300, 2300 + 1440
        };

        public static readonly int[] PowerPointsTable = new int[]
        {
            20, 30, 50, 80, 130, 210, 340, 550, 890, 1440
        };

        public static readonly int[] UpgradeCostTable = new int[]
        {
            20, 35, 75, 140, 290, 480, 880, 1250, 1875, 2800
        };

        [JsonProperty] public int CharacterId;
        [JsonProperty] public int CardId;

        [JsonProperty] public int Trophies;
        [JsonProperty] public int HighestTrophies;

        [JsonProperty] public int PowerPoints;
        [JsonProperty] public int PowerLevel;

        [JsonProperty] public int SelectedStarPowerId;
        [JsonProperty] public int SelectedGadgetId;
        [JsonProperty] public int SelectedGearId1;
        [JsonProperty] public int SelectedGearId2;
        [JsonProperty] public int SelectedSkinId;
        [JsonProperty] public int SelectedOverChargeId;

        [JsonProperty] public int ChampieSelectedStarPowerId;
        [JsonProperty] public int ChampieSelectedGadgetId;
        [JsonProperty] public int ChampieSelectedGearId1;
        [JsonProperty] public int ChampieSelectedGearId2;
        [JsonProperty] public int ChampieSelectedSkinId;
        [JsonProperty] public int ChampieSelectedOverChargeId;

        [JsonProperty] public Dictionary<int, int> SelectedEmotes;
        [JsonProperty] public int MasteryPoints;
        [JsonProperty] public int ClaimedMasteryLVL;
        [JsonProperty] public List<int> OwnedGears;
        [JsonProperty] public int SelectedSpray;

        public CharacterData CharacterData => DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterId);
        public CardData CardData => DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(CardId);
        public static EmoteData GetDefaultEmoteForCharacter(string Character, string Type)
        {
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Character == Character && emoteData.EmoteType == Type) return emoteData;
            }
            return null;
        }
        public Hero(int characterId)
        {
            try{
            CharacterId = characterId;
            CardData g = GetDefaultMetaForHero(5);
            CharacterData characterData = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(characterId);
            if (g != null) SelectedGadgetId = 23000000;
            CardData s = GetDefaultMetaForHero(4);
            if (s != null) SelectedStarPowerId = 23000000;
            CardData o = GetDefaultMetaForHero(6);
            if (o != null) SelectedOverChargeId = 23000000;
            CardId = DataTables.GetUnlockCardFor(CharacterData).GetGlobalId();
            PowerLevel = 1;
            SelectedSpray = -1;
            SelectedGearId1 = -1;
            SelectedGearId2 = -1;
            SelectedSkinId = 0;
            SelectedEmotes = new Dictionary<int, int>();
            if(GetDefaultEmoteForCharacter(characterData.Name, "DEFAULT") != null) SelectedEmotes.Add(1, GetDefaultEmoteForCharacter(characterData.Name, "DEFAULT").GetInstanceId());
            SelectedEmotes.Add(3, 137 - 3);
            SelectedEmotes.Add(2, 148 - 3);
            OwnedGears = new List<int>();}
            catch{;}
        }

        public void AddTrophies(int trophies)
        {
            Trophies += trophies;
            if (Trophies > HighestTrophies) HighestTrophies = Trophies;
        }

        public void AddMastery(int trophies)
        {
            MasteryPoints += trophies;
        }

        public void SetTrophies(int trophies)
        {
            Trophies = trophies;
            HighestTrophies = LogicMath.Max(HighestTrophies, Trophies);
        }
        public CardData GetDefaultMetaForHero(int MetaType)
        {
            foreach (CardData carddata in DataTables.Get(DataType.Card).GetDatas())
            {
                if (carddata.Target == CharacterData.Name && carddata.MetaType == MetaType)
                {
                    return carddata;
                }
            }
            return null;
        }
        public void Encode(ByteStream stream)
        {
            ByteStreamHelper.WriteDataReference(stream, CharacterData);
            ByteStreamHelper.WriteDataReference(stream, null);
            stream.WriteVInt(Trophies);
            stream.WriteVInt(HighestTrophies);
            stream.WriteVInt(PowerLevel);
        }
    }
}
