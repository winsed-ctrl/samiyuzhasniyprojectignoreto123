namespace GromCore.Laser.Logic.Command.Home
{
    using System.Runtime.InteropServices;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;

    public class LogicSelectSprayCommand : Command
    {
        private int EmoteID;
        private int EmoteSlot;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            EmoteID = stream.ReadVInt();
            EmoteSlot = stream.ReadVInt();
          
        }

        public override int Execute(HomeMode homeMode)
        {
            
            SprayData emoteData = DataTables.Get(DataType.Spray).GetDataWithId<SprayData>(EmoteID);
            if (EmoteSlot < 6 || EmoteSlot > 10) return -1;
            if (emoteData == null) return -1;
            //if (emoteData.Rarity != "DEFAULT" && !homeMode.Home.UnlockedEmotes.Contains(emoteData.GetGlobalId())) return -1;
            switch (EmoteSlot)
            {
                case 7:
                case 8:
                case 9:
                case 10:
                    {
                        int foundSlot = -1;
                        for (int i = 7; i <= 10; i++)
                        {
                            if (i != EmoteSlot && homeMode.Home.PlayerSelectedSpray[i] == EmoteID)
                            {
                                foundSlot = i;
                                break;
                            }
                        }
                        if (foundSlot != -1)
                        {
                            int temp = homeMode.Home.PlayerSelectedSpray[EmoteSlot];
                            homeMode.Home.PlayerSelectedSpray[EmoteSlot] = EmoteID;
                            homeMode.Home.PlayerSelectedSpray[foundSlot] = temp;
                        }
                        else homeMode.Home.PlayerSelectedSpray[EmoteSlot] = EmoteID;

                        return 0;
                    }
            }



            CharacterData characterData = DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character);
            if (characterData == null) return -1;
            Hero hero = homeMode.Avatar.GetHero(characterData.GetGlobalId()) ?? null;

            if (emoteData.Character != null && characterData != null) homeMode.Avatar.SetSprayForBrawler(characterData.GetGlobalId(), EmoteID);
            
            
            return 0;
        }


        public override int GetCommandType()
        {
            return 555;
        }
    }
}
