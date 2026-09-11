namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;
    using System.Net.Http.Headers;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using System.Reflection.Metadata.Ecma335;

    public class LogicPurchaseGearCommand : Command
    {
        private int _gearID;
        private int _gearTarget;
        private const double GEMS_PER_COIN = 0.1;
        private int _gearSlot;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // class
            _gearTarget = stream.ReadVInt();
            stream.ReadVInt(); // class
            _gearID = stream.ReadVInt();
            _gearSlot = stream.ReadVInt();
        }

        public override int Execute(HomeMode homeMode)
        {

            bool Check(int count)
            {
                if (count > homeMode.Avatar.Gold)
                {
                    homeMode.Avatar.Gold = 0;
                    int deficit = count - homeMode.Avatar.Gold;
                    if (!homeMode.Avatar.UseDiamonds((int)Math.Ceiling(deficit * GEMS_PER_COIN - 10)))
                    {
                        return false;
                    }
                    return true;
                }
                homeMode.Avatar.Gold -= count;
                return true;
            }
            Hero hero = homeMode.Avatar.GetHero(GlobalId.CreateGlobalId(16, _gearTarget));
            if (hero == null) return -1;
            GearData gearData = DataTables.Get(DataType.Gear).GetDataWithId<GearData>(_gearID);
            if (gearData == null) return -1;

            if (homeMode.Avatar.Heroes.All(hero => !hero.OwnedGears.Any()))
            {
                hero.OwnedGears.Add(_gearID);
                switch (_gearSlot)
                {
                    case 0:
                        hero.SelectedGearId1 = _gearID;
                        hero.ChampieSelectedGearId1 = _gearID;
                        return 0;
                    case 1:
                        hero.SelectedGearId2 = _gearID;
                        hero.ChampieSelectedGearId2 = _gearID;
                        return 0;
                    default:
                        return -1;
                }
            }
            switch (gearData.Rarity)
            {
                case "RareGear":
                    Check(1000);
                    break;
                case "SuperRareGear":
                    Check(1500);
                    break;
                case "EpicGear":
                    Check(2000);
                    break;
                default:
                    return -1;
            }
            hero.OwnedGears.Add(_gearID);

            switch (_gearSlot)
            {
                case 0:
                    hero.SelectedGearId1 = _gearID;
                    hero.ChampieSelectedGearId1 = _gearID;
                    return 0;
                case 1:
                    hero.SelectedGearId2 = _gearID;
                    hero.ChampieSelectedGearId2 = _gearID;
                    return 0;
            }
            return -1;
        }

        public override int GetCommandType()
        {
            return 558;
        }
    }
}
