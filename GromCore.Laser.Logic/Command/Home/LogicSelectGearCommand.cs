namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;

    public class LogicSelectGearCommand : Command
    {
        public int characterId;
        public int gearid;
        public int gearslot;

        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            characterId = stream.ReadVInt();
            stream.ReadVInt();
            gearid = stream.ReadVInt();
            gearslot = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {
            if (gearid < 0 || gearid > 999) return -1;
            int globalId = GlobalId.CreateGlobalId(16, characterId);
            Console.WriteLine(gearid);
            Console.WriteLine(gearslot);
            if (homeMode.Avatar.HasHero(globalId))
            {
                Hero hero = homeMode.Avatar.GetHero(globalId);
                if (gearslot == 0)
                {
                    if (gearid == hero.ChampieSelectedGearId2)
                    {
                        hero.ChampieSelectedGearId2 = hero.SelectedGearId1;
                        hero.ChampieSelectedGearId1 = gearid;
                    }
                    else
                    {
                        hero.ChampieSelectedGearId1 = gearid;
                    }
                }
                else if (gearslot == 1)
                {
                    if (gearid == hero.ChampieSelectedGearId1)
                    {
                        hero.ChampieSelectedGearId1 = hero.SelectedGearId2;
                        hero.ChampieSelectedGearId2 = gearid;
                    }
                    else
                    {
                        hero.ChampieSelectedGearId2 = gearid;
                    }
                }
                if (hero.ChampieSelectedGearId1 == hero.ChampieSelectedGearId2 && hero.OwnedGears.Count > 1)
                {
                    hero.ChampieSelectedGearId2 = hero.OwnedGears.LastOrDefault();
                }
                if (!hero.OwnedGears.Contains(gearid)) return 0;
                if (gearslot == 0)
                {
                    if (gearid == hero.SelectedGearId2)
                    {
                        hero.SelectedGearId2 = hero.SelectedGearId1;
                        hero.SelectedGearId1 = gearid;
                    }
                    else hero.SelectedGearId1 = gearid;
                }
                else if(gearslot == 1)
                {
                    if (gearid == hero.SelectedGearId1)
                    {
                        hero.SelectedGearId1 = hero.SelectedGearId2;
                        hero.SelectedGearId2 = gearid;
                    }
                    else hero.SelectedGearId2 = gearid;
                }
                if (hero.SelectedGearId1 == hero.SelectedGearId2 && hero.OwnedGears.Count > 1) hero.SelectedGearId2 = hero.OwnedGears.LastOrDefault();
                return 0;
            }
            return -1;
        }

        public override int GetCommandType()
        {
            return 543;
        }
    }
}
