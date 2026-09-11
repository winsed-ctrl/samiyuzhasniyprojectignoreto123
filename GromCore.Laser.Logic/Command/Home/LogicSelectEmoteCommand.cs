namespace GromCore.Laser.Logic.Command.Home
{
    using System.Runtime.InteropServices;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSelectEmoteCommand : Command
    {
        private int EmoteID;
        private int EmoteSlot;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            EmoteID = stream.ReadVInt();
            EmoteSlot =stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            EmoteData emoteData = DataTables.Get(DataType.Emote).GetDataWithId<EmoteData>(EmoteID);
            if (EmoteSlot < 0 || EmoteSlot > 6) return -1;
            if(emoteData == null) return -1;
            if(emoteData.Rarity != "DEFAULT" && !homeMode.Home.UnlockedEmotes.Contains(emoteData.GetGlobalId())) return -1;
            switch (EmoteSlot)
            {
                case 4:
                    homeMode.Home.PlayerSelectedEmotes[4] = EmoteID;
                    return 0;
                case 5:
                    homeMode.Home.PlayerSelectedEmotes[5] = EmoteID;
                    return 0;
            }

            

            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character);
            if (characterData == null) return -1;
            Hero hero = homeMode.Avatar.GetHero(characterData.GetGlobalId()) ?? null;
            
            if (emoteData.Character != null && characterData != null) homeMode.Avatar.SetEmoteForBrawler(characterData.GetGlobalId(), EmoteSlot, EmoteID);
            if (hero.SelectedEmotes[1] == hero.SelectedEmotes[2] || hero.SelectedEmotes[1] == hero.SelectedEmotes[3]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).SelectedEmotes[1] = 160 - 3;
            if (hero.SelectedEmotes[2] == hero.SelectedEmotes[1] || hero.SelectedEmotes[3] == hero.SelectedEmotes[1]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).SelectedEmotes[2] = 148 - 3;
            if (hero.SelectedEmotes[3] == hero.SelectedEmotes[1] || hero.SelectedEmotes[3] == hero.SelectedEmotes[2]) homeMode.Avatar.GetHero(characterData.GetGlobalId()).SelectedEmotes[3] = 137 - 3;
            return 0;
        }


        public override int GetCommandType()
        {
            return 538;
        }
    }
}
