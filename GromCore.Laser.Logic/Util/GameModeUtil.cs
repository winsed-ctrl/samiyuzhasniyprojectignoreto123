namespace GromCore.Laser.Logic.Util
{
    using System.Text.RegularExpressions;
    using GromCore.Laser.Logic.Battle;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Logic.Data;

    public static class GameModeUtil
    {
        public static bool PlayersCollectPowerCubes(int variation)
        {
            int v1 = variation - 6;
            if (v1 <= 8)
                return ((0x119 >> v1) & 1) != 0;
            else
                return false;
        }

        public static int GetBattleTicks(int v)
        {
            int v2 = 2400;
            switch (v)
            {
                case 0:
                case 5:
                case 16:
                case 22:
                case 23:
                    v2 = 4200;
                    goto LABEL_9;
                case 3:
                case 7:
                case 8:
                    goto LABEL_9;
                case 6:
                case 9:
                case 10:
                case 12:
                case 13:
                case 18:
                case 20:
                    return BattleMode.NO_TIME_TICKS;
                case 14:
                    v2 = 9600;
                    goto LABEL_9;
                case 17:
                case 21:
                    v2 = 3600;
                    goto LABEL_9;
                default:
                    v2 = 20 * ((BattleMode.NORMAL_TICKS - BattleMode.INTRO_TICKS) / 20);
                LABEL_9:
                    return BattleMode.INTRO_TICKS + v2;
            }
        }


        public static int GetRespawnSeconds(int variation)
        {
            switch (variation)
            {
                case 0:
                case 2:
                    return 3;
                case 3:
                    return 1;
                case 13:
                case 7:
                    return 3;
                case 10:
                    return 15;
                case 15:
                    return 1;
                case 9:
                    return 15;
                case 17:
                    return 3;
                default:
                    return 5;
            }
        }

        public static bool PlayersCollectBountyStars(int variation)
        {
            return variation == 3 || variation == 15;
        }

        public static int GetNumberOfRounds(int a1)
        {
            bool v2; // zf
            int result; // r0

            v2 = a1 == 20;
            result = 3;
            if (!v2 && a1 != 35) // supercell lore:
            {
                if (a1 == 24)
                    return 5;
                else
                    return 1;
            }
            return result;

        }

        public static bool HasTwoTeams(int variation)
        {
            if (variation == 15) return false;
            if (variation == 28) return false;
            if (variation == 6) return false;
            if (variation == 9) return false;
            if (variation == 33) return false;
            return true;
        }

        public static bool HasBigMap(int variation)
        {
            if (variation == 6 || variation == 0x21 || variation == 0x2E || variation == 15 || variation == 9)
            {
                return true;
            }
            return false;
        }

        public static bool HasFiveTeams(int variation)
        { if (variation == 9) return true; return false; }


        public static bool HasTwoBases(int variation)
        {
            return variation == 2 || variation == 11;
        }

        public static bool Mode5v5(int variation)
        {
            if (variation == 31 || variation == 32 || variation == 33 || variation == 34) return true;
            return false;
        }
        public static bool PONOSXMOPSER( int variation)
        {
            if (variation == 5) return true;
            if (variation == 24) return true;
            if (variation == 22) return true;
            if (variation == 23) return true;
            if (variation == 32) return true;
            return false;
        }
        public static bool ModeHasCarryables(int variation)
        {
            if (variation == 5) return true;
            if (variation == 16) return true;
            if (variation == 24) return true;
            if (variation == 22) return true;
            if (variation == 23) return true;
            if (variation == 21) return true;
            if (variation == 32) return true;
            if (variation == 26) return true;
            return false;
        }

        public static int ZN17LogicGameModeUtil25isBossWithDifferentStagesEiPK18LogicCharacterData(int a1, CharacterData a2)
        {
            Int64 result; // x0

            if ((a2.IsBoss(a1) & 1) == 0)
                return 0;
            result = 1L;
            if (a1 != 10 && a1 != 18)
                return 0;
            return (int)result;
        }
        public static bool IsGemGrab(int variation)
        {
            if (variation == 0 || variation == 0x21 || variation == 0x2E)
            {
                return true;
            }
            return false;
        }

        public static int GetGameModeVariation(string mode)
        {
            switch (mode)
            {
                case "CoinRush":
                    return 0;
                case "GemGrab":
                    return 0;
                case "Heist":
                    return 2;
                case "BossFight":
                    return 10;
                case "Bounty":
                    return 3;
                case "Artifact":
                    return 4;
                case "BrawlBall":
                    return 5;
                case "Showdown":
                    return 6;
                case "BigGame":
                    return 7;
                case "DuoShowdown":
                    return 9;
                case "RoboRumble":
                    return 8;
                case "Raid":
                    return 10;
                case "Siege":
                    return 11;
                case "Tutorial":
                    return 12;
                case "Training":
                    return 13;
                case "CTF":
                    return 16;
                case "KingOfHill":
                    return 17;
                case "TagTeam":
                    return 103;
                case "ReachExit":
                    return 30;
                case "Knockout":
                    return 20;
                case "LoneStar":
                    return 15;
                case "Deathmatch":
                    return 25;
                case "MapPrint":
                    return 99;
                case "BossFight_TownCrush":
                    return 18;
                case "Deathmatch5v5":
                    return 31;
                case "BrawlBall5v5":
                    return 32;
                case "GemGrab5v5":
                    return 33;
                case "HoldTheBall":
                    return 21;
                case "BasketBrawl":
                    return 22;
                case "VolleyBrawl":
                    return 23;
                case "Payload":
                    return 26;
                case "Invasion":
                    return 27;
                case "ProtectKing":
                    return 19;
                case "DeathmatchFFA":
                    return 28;
                case "LastStand":
                    return 29;
                case "Bounty1v1":
                    return 103;
                default:
                    Debugger.Error("Wrong game mode!");
                    return -1;
            }
        }
    }
}
