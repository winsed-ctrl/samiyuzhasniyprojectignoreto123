namespace GromCore.Laser.Logic.Command.Avatar
{
    using System.Collections.Generic;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Titan.DataStream;

    public class LogicStarroadRefreshCommand : Command
    {
        List<int> rare = new List<int> {};
        List<int> super_rare = new List<int> {};
        List<int> epic = new List<int> {};
        List<int> mythic = new List<int> {};
        List<int> legendary = new List<int> {};
        List<int> ultra_legendary = new List<int> {};
        List<int> brawlers_list = new List<int> {};
        List<int> road_list = new List<int> { };

        private int RecruitBrawler;
        private int RecruitTokens;
        private int FameTokens;

        public override void Encode(ByteStream stream)
        {

            stream.WriteVInt(0); // DayArrayRange
            stream.WriteVInt(0); // Timer
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0); // Road (if Poco Road - 1, if Brock Road - 2);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            // Road (if Poco Road - 1, if Brock Road - 2);
            // stream.WriteBoolean(RecruitBrawler > 77); 
            // stream.WriteBoolean(RecruitBrawler > 77);
            // stream.WriteBoolean(RecruitBrawler > 77);


            stream.WriteBoolean(RecruitBrawler != -1);

            if (RecruitBrawler != -1)
            {

                stream.WriteDataReference(16, RecruitBrawler); // brawler id

                if (rare.Contains(RecruitBrawler))
                    stream.WriteVInt(160);
                else if (super_rare.Contains(RecruitBrawler))
                    stream.WriteVInt(430);
                else if (epic.Contains(RecruitBrawler))
                    stream.WriteVInt(925);
                else if (mythic.Contains(RecruitBrawler))
                    stream.WriteVInt(1900);
                else if (legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(3800);
                else if (ultra_legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(5500);
                else if (RecruitBrawler > 77)
                    stream.WriteVInt(99999);
                else
                    stream.WriteVInt(0);


                if (rare.Contains(RecruitBrawler))
                    stream.WriteVInt(29);
                else if (super_rare.Contains(RecruitBrawler))
                    stream.WriteVInt(79);
                else if (epic.Contains(RecruitBrawler))
                    stream.WriteVInt(169);
                else if (mythic.Contains(RecruitBrawler))
                    stream.WriteVInt(349);
                else if (legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(699);
                else if (ultra_legendary.Contains(RecruitBrawler))
                    stream.WriteVInt(999);
                else if (RecruitBrawler > 77)
                    stream.WriteVInt(99999);
                else
                    stream.WriteVInt(0);


                stream.WriteVInt(0);
                stream.WriteVInt(RecruitTokens);
                stream.WriteVInt(0);
                stream.WriteVInt(0);


                int allbrawlers = road_list.Count - 1;
                int srb_index = 0;
                stream.WriteVInt(allbrawlers);
                foreach (int x in road_list)
                {
                    if (srb_index == 0)
                    {
                        srb_index++;
                        continue;
                    }

                    stream.WriteDataReference(16, x);

                    if (rare.Contains(x))
                        stream.WriteVInt(160);
                    else if (super_rare.Contains(x))
                        stream.WriteVInt(430);
                    else if (epic.Contains(x))
                        stream.WriteVInt(925);
                    else if (mythic.Contains(x))
                        stream.WriteVInt(1900);
                    else if (legendary.Contains(x))
                        stream.WriteVInt(3800);
                    else if (ultra_legendary.Contains(x))
                        stream.WriteVInt(5500);
                    else
                        stream.WriteVInt(0);


                    if (rare.Contains(x))
                        stream.WriteVInt(29);
                    else if (super_rare.Contains(x))
                        stream.WriteVInt(79);
                    else if (epic.Contains(x))
                        stream.WriteVInt(169);
                    else if (mythic.Contains(x))
                        stream.WriteVInt(349);
                    else if (legendary.Contains(x))
                        stream.WriteVInt(699);
                    else if (ultra_legendary.Contains(x))
                        stream.WriteVInt(999);
                    else
                        stream.WriteVInt(0);


                    stream.WriteVInt(0);
                    stream.WriteVInt(0);
                    stream.WriteVInt(srb_index); //
                    srb_index++;
                    stream.WriteVInt(0);
                }


                stream.WriteVInt(0);

                stream.WriteVInt(0);
            }
            else
            {
                stream.WriteVInt(0);
                stream.WriteVInt(0);
                stream.WriteVInt(0);
            }

            // stream.WriteVInt(1);
            // stream.WriteVInt(0);
            // stream.WriteVInt(0);
            // stream.WriteVInt(0);
        }

        public override int Execute(HomeMode homeMode)
        {
            RecruitTokens = homeMode.Home.RecruitTokens;
            FameTokens = homeMode.Home.FameTokens;
            brawlers_list = homeMode.Home.BrawlersRoad;
            // while (homeMode.HasHeroUnlocked(16000000 + homeMode.Home.RecruitBrawler) && homeMode.Home.RecruitBrawler < 77) homeMode.Home.RecruitBrawler++;
            if (homeMode.Home.NewRecruitBrawler != 0)
            {
                if (!homeMode.HasHeroUnlocked(16000000 + homeMode.Home.NewRecruitBrawler))
                {
                    road_list.Add(homeMode.Home.NewRecruitBrawler);
                }
            }
            foreach (int brawler in brawlers_list)
            {
                if (!homeMode.HasHeroUnlocked(16000000 + brawler))
                {
                    road_list.Add(brawler);
                }
            }
            rare = homeMode.Home.GetBrawlersByRarity("rare");
            super_rare = homeMode.Home.GetBrawlersByRarity("super_rare");
            epic = homeMode.Home.GetBrawlersByRarity("epic");
            mythic = homeMode.Home.GetBrawlersByRarity("mythic");
            legendary = homeMode.Home.GetBrawlersByRarity("legendary");
            ultra_legendary = homeMode.Home.GetBrawlersByRarity("ultra_legendary");
            brawlers_list = homeMode.Home.GetBrawlers();
            if (road_list.Count != 0) RecruitBrawler = road_list[0];
            else RecruitBrawler = -1;
            return 0;
        }

        public override int GetCommandType()
        {
            return 227;
        }
    }
}
