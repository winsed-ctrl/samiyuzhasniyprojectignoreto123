namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Command.Avatar;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;

    
    public class LogicPurchaseBrawlerCommand : Command
    {
        private int Brawler;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt();
            Brawler = stream.ReadVInt();
            Console.WriteLine(stream.ReadVInt());
        }

        public override int Execute(HomeMode homeMode)
        {
                List<int> rare = homeMode.Home.GetBrawlersByRarity("rare");
                List<int> super_rare = homeMode.Home.GetBrawlersByRarity("super_rare");
                List<int> epic = homeMode.Home.GetBrawlersByRarity("epic");
                List<int> mythic = homeMode.Home.GetBrawlersByRarity("mythic");
                List<int> legendary = homeMode.Home.GetBrawlersByRarity("legendary");
                List<int> ultra_legendary = homeMode.Home.GetBrawlersByRarity("ultra_legendary");
                int brawlerId = Brawler; 
                int tokenCost = 0;
                int gemCost = 0;

                if (rare.Contains(brawlerId))
                {
                    tokenCost = 160;
                    gemCost = 29;
                }
                else if (super_rare.Contains(brawlerId))
                {
                    tokenCost = 430;
                    gemCost = 79;
                }
                else if (epic.Contains(brawlerId))
                {
                    tokenCost = 925;
                    gemCost = 149;
                }
                else if (mythic.Contains(brawlerId))
                {
                    tokenCost = 1900;
                    gemCost = 349;
                }
                else if (legendary.Contains(brawlerId))
                {
                    tokenCost = 3800;
                    gemCost = 699;
                }
                else if (ultra_legendary.Contains(brawlerId))
                {
                    tokenCost = 5500;
                    gemCost = 999;
                }
                else
                {
                    return -1;
                }
            if (homeMode.HasHeroUnlocked(GlobalId.CreateGlobalId(16, Brawler))) return -1;
            //if (Brawler == homeMode.Home.RecruitBrawler) return -1;
            if (!homeMode.Avatar.UseDiamonds(gemCost)) return -1;

            LogicGiveDeliveryItemsCommand command = new LogicGiveDeliveryItemsCommand();
            DeliveryUnit unit = new DeliveryUnit(100);
            GatchaDrop reward = new GatchaDrop(1);
            reward.DataGlobalId = GlobalId.CreateGlobalId(16,Brawler);
            reward.Count = 1;
            unit.AddDrop(reward);
            command.DeliveryUnits.Add(unit);
            command.Execute(homeMode);
            AvailableServerCommandMessage message = new AvailableServerCommandMessage();
            message.Command = command;
            homeMode.GameListener.SendMessage(message);
            LogicStarroadRefreshCommand command1 = new LogicStarroadRefreshCommand();
            command1.Execute(homeMode);
            AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
            serverCommandMessage.Command = command1;
            homeMode.GameListener.SendMessage(serverCommandMessage);


            // bool TryRecruitBrawler()
            // {
            //     //homeMode.Home.BrawlPassTokens += 10000;
            //     List<int> rare = homeMode.Home.GetBrawlersByRarity("rare");
            //     List<int> super_rare = homeMode.Home.GetBrawlersByRarity("super_rare");
            //     List<int> epic = homeMode.Home.GetBrawlersByRarity("epic");
            //     List<int> mythic = homeMode.Home.GetBrawlersByRarity("mythic");
            //     List<int> legendary = homeMode.Home.GetBrawlersByRarity("legendary");
            //     List<int> ultra_legendary = homeMode.Home.GetBrawlersByRarity("ultra_legendary");
            //     int brawlerId = Brawler; 
            //     int tokenCost = 0;
            //     int gemCost = 0;

            //     if (rare.Contains(brawlerId))
            //     {
            //         tokenCost = 160;
            //         gemCost = 29;
            //     }
            //     else if (super_rare.Contains(brawlerId))
            //     {
            //         tokenCost = 430;
            //         gemCost = 79;
            //     }
            //     else if (epic.Contains(brawlerId))
            //     {
            //         tokenCost = 925;
            //         gemCost = 149;
            //     }
            //     else if (mythic.Contains(brawlerId))
            //     {
            //         tokenCost = 1900;
            //         gemCost = 349;
            //     }
            //     else if (legendary.Contains(brawlerId))
            //     {
            //         tokenCost = 3800;
            //         gemCost = 699;
            //     }
            //     else if (ultra_legendary.Contains(brawlerId))
            //     {
            //         tokenCost = 5500;
            //         gemCost = 999;
            //     }
            //     else
            //     {
            //         return false;
            //     }

            //     int deficit = tokenCost - homeMode.Home.RecruitTokens;
            //     double gemsNeeded = deficit / 5.45;
            //     int roundedGemsNeeded = (int)Math.Ceiling(gemsNeeded)-1;
                

            //     if (homeMode.Avatar.Diamonds >= roundedGemsNeeded)
            //     {
            //         if(brawlerId == homeMode.Home.RecruitBrawler) homeMode.Home.RecruitTokens = 0;
            //         homeMode.Avatar.UseDiamonds(roundedGemsNeeded);
            //         return true;
            //     }
            //     else
            //     {
            //         return false;
            //     } 
                
            // }
            return 0;
        }

        public override int GetCommandType()
        {
            return 560;
        }
    }
}