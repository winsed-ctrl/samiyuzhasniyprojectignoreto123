namespace GromCore.Laser.Logic.Home.Items
{
    class ReleaseEntry
    {
        public static int[] LogicReleaseEntryDefault = new int[]
        {
                16000076, (int)(DateTime.Parse("2025-09-03 10:30:00") - DateTime.Now).TotalSeconds,
                16000077, (int)(DateTime.Parse("2025-10-11 10:00:00") - DateTime.Now).TotalSeconds
        };
        public static int[] LogicReleaseEntryGrom = new int[]
        {
                16000078, (int)(DateTime.Parse("2025-11-29 10:00:00") - DateTime.Now).TotalSeconds,
                16000079, (int)(DateTime.Parse("2025-11-08 10:00:00") - DateTime.Now).TotalSeconds,
                16000080, (int)(DateTime.Parse("2026-01-28 10:00:00") - DateTime.Now).TotalSeconds
        };
        public static int[] LogicReleaseEntryMiopark = new int[]
        {
                16000098, (int)(DateTime.Parse("2025-10-04 10:00:00") - DateTime.Now).TotalSeconds,
                16000099, (int)(DateTime.Parse("2025-12-31 09:00:00") - DateTime.Now).TotalSeconds
        };
        public static int[] LogicReleaseEntryRetro = new int[]
        {
        };

        public static int[] LogicReleaaseEntry = new int[]
        {
        };


    }
}
