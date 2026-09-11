namespace GromCore.Laser.Logic.Command.Avatar
{
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Stream.Entry;
    using GromCore.Laser.Titan.DataStream;
    using System.Collections.Generic;

    public class LogicBrawlerRecruitRoadChangedCommand : Command
    {
        readonly List<int> rare = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24 };
        readonly List<int> super_rare = new List<int> { 7, 9, 18, 19, 22, 25, 27, 34, 61, 4 };
        readonly List<int> epic = new List<int> { 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 60, 68, 79, 82, 86, 89, 96 };
        readonly List<int> mythic = new List<int> { 11, 17, 21, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 62, 66, 78, 81, 83, 84, 87, 90, 91, 92, 93, 95, 97, 98 };
        readonly List<int> legendary = new List<int> { 5, 12, 23, 28, 40, 52, 63, 76, 38, 70, 76, 80, 85};
        readonly List<int> ultra_legendary = new List<int> {94};
        List<int> brawlers_list = new List<int> { 1, 2, 3, 6, 8, 10, 13, 24, 7, 9, 18, 19, 22, 25, 27, 34, 61, 4, 14, 15, 16, 20, 26, 29, 30, 36, 43, 45, 48, 50, 58, 69, 77, 35, 39, 46, 51, 53, 65, 72, 77, 11, 17, 21, 35, 31, 32, 37, 42, 47, 64, 67, 71, 73, 74, 75, 41, 44, 49, 54, 56, 57, 59, 60, 62, 66, 68, 5, 12, 23, 28, 40, 52, 63, 76, 38, 70 };
        List<int> road_list = new List<int> { };

        private int RecruitBrawler;
        private int RecruitTokens;
        private int FameTokens;
        public HomeMode homeMode;
        public override void Encode(ByteStream stream)
        {
            stream.WriteVInt(0);
            stream.WriteVInt(0); 
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            homeMode.Home.RecruitRoad.CommandEncode(stream, homeMode.Home);
        }

        public override int Execute(HomeMode homeMode)
        {
            return 0;
            LogicRecruitRoad r = homeMode.Home.RecruitRoad;
            if (r.NowRecruit != null && r.NowRecruit.GlobalId != 0)
            {
                int currentBrawlerId = GlobalId.GetInstanceId(r.NowRecruit.GlobalId);
                if (homeMode.HasHeroUnlocked(r.NowRecruit.GlobalId))
                {
                    if(r.UnlockList.Contains(r.NowRecruit)) r.UnlockList.Remove(r.NowRecruit);
                    if (r.UnlockList.Count > 0)
                    {
                        r.NowRecruit = r.UnlockList[0];
                    }
                    else
                    {
                        r.NowRecruit = null;
                    }
                }
            }
            r.UnlockList.RemoveAll(recruit =>
                homeMode.HasHeroUnlocked(recruit.GlobalId) ||
                recruit.OtherUnlockVariations.Any(otherId => homeMode.HasHeroUnlocked(otherId)));

            if (r.UnlockList.Count == 0 && r.UnlockList.Count > 0)
            {
                //r.RebuildUnlockList(r.UnlockList.FindAll, homeMode);
            }

            return 0;
        }

        public override int GetCommandType()
        {
            return 227;
        }
    }
}
