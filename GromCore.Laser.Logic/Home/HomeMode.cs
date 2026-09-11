namespace GromCore.Laser.Logic.Home
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Command;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home.Gatcha;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.Math;
    using System.Security.Cryptography;

    public static class ShuffleExtensions
    {
        public static void ShuffleInPlace<T>(this IList<T> list, Random? rng = null)
        {
            rng ??= Random.Shared;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1); // 0..i
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }

    public class HomeMode
    {
        public const int UNLOCKABLE_HEROES_COUNT = 38;

        public readonly LogicGameListener GameListener;

        public ClientHome Home;
        public ClientAvatar Avatar;

        public Action<int> CharacterChanged;

        public HomeMode(ClientHome home, ClientAvatar avatar, LogicGameListener gameListener)
        {
            Home = home;
            Avatar = avatar;

            Home.HomeMode = this;
            Avatar.HomeMode = this;

            GameListener = gameListener;
        }

        public bool HasHeroUnlocked(int Brawler)
        {
            return Avatar.HasHero(Brawler);
        }
        public static HomeMode LoadHomeState(LogicGameListener gameListener, ClientHome home, ClientAvatar avatar, EventData[] events)
        {
            home ??= new ClientHome();
            home.NormalizeUnlockedSkinIds();
            home.Events = events;

            HomeMode homeMode = new HomeMode(home, avatar, gameListener);
            homeMode.Enter(DateTime.UtcNow);

            return homeMode;
        }

        private bool GetRandomBrawlerForGatcha(Random rand, DeliveryUnit unit)
        {
            int brawlersCount = UNLOCKABLE_HEROES_COUNT;
            int brawlerId = -1;

            bool done = false;
            int attempts = 0;
            while (!done && attempts < 25)
            {
                attempts++;
                brawlerId = GlobalId.CreateGlobalId(16, rand.Next(0, brawlersCount));

                CharacterData data = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(brawlerId);
                done = !data.Disabled && !Avatar.HasHero(brawlerId);

                if (done)
                {
                    CardData card = DataTables.Get(DataType.Card).GetData<CardData>(data.Name + "_unlock");
                    done = card.Rarity != "common";
                    if (done)
                    {
                        done = card.Name != "Blower_unlock";
                        if (done)
                        {
                            if (card.Rarity == "epic")
                            {
                                if (Avatar.RollsSinceGoodDrop > 6)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                            else if (card.Rarity == "mega_epic")
                            {
                                if (Avatar.RollsSinceGoodDrop > 25)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                            else if (card.Rarity == "legendary")
                            {
                                if (Avatar.RollsSinceGoodDrop > 100)
                                {
                                    Avatar.RollsSinceGoodDrop = 0;
                                    done = true;
                                }
                                else
                                {
                                    done = Avatar.RollsSinceGoodDrop > rand.Next(1000);
                                    if (done) Avatar.RollsSinceGoodDrop = 0;
                                }
                            }
                        }
                    }
                }

                if (done)
                {
                    GatchaDrop drop = new GatchaDrop(1);
                    drop.DataGlobalId = brawlerId;
                    drop.Count = 1;
                    unit.AddDrop(drop);
                    break;
                }
            }

            return done;
        }

        public void SimulateGatcha(DeliveryUnit unit)
        {
            Avatar.RollsSinceGoodDrop++;
            Random rand = new Random();

            int unlockedBrawlersCount = Avatar.GetUnlockedHeroesCount();
            int setChaosDropRarity = -1;

            List<int> powerPoints = new List<int>();
            if (unit.Type == 10)
            {
                int brawlerChance = 10;
                if (unlockedBrawlersCount < 4)
                {
                    brawlerChance = 70;
                }
                else if (unlockedBrawlersCount < 6)
                {
                    brawlerChance = 40;
                }

                bool isBrawler = rand.Next(0, 100) < brawlerChance;
                if (isBrawler)
                {
                    isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                }

                if (!isBrawler)
                {
                    GatchaDrop coins = new GatchaDrop(7);
                    coins.Count = rand.Next(15, 40);
                    unit.AddDrop(coins);

                    for (int i = 0; i < 2; i++)
                    {
                        List<Hero> unlockedHeroes = Avatar.Heroes;
                        bool heroValid = false;
                        int generateAttempts = 0;
                        int idx = -1;
                        while (!heroValid && generateAttempts < 10)
                        {
                            generateAttempts++;
                            idx = rand.Next(unlockedHeroes.Count);
                            heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                            if (heroValid)
                            {
                                GatchaDrop drop = new GatchaDrop(6);
                                drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                                if (powerPoints.Contains(drop.DataGlobalId))
                                {
                                    continue;
                                }
                                powerPoints.Add(drop.DataGlobalId);
                                drop.Count = LogicMath.Min(rand.Next(5, 25), (1410 - unlockedHeroes[idx].PowerPoints));
                                unit.AddDrop(drop);
                            }
                        }
                    }
                }
            }
            else if (unit.Type == 11)
            {
                GatchaDrop coins = new GatchaDrop(7);
                coins.Count = rand.Next(322, 600);
                unit.AddDrop(coins);

                var PowerPointsHeroes = new List<int>{};

                foreach (Hero h in Avatar.Heroes){
                    if (h.PowerPoints < 1410){
                        PowerPointsHeroes.Add(h.CharacterId);
                    }
                }

                int ppcount = 5;

                if (PowerPointsHeroes.Count < ppcount) ppcount = PowerPointsHeroes.Count;

                PowerPointsHeroes.ShuffleInPlace();

                int powerPointsCount = 50;

                for (int i = 0; i < ppcount; i++){
                    powerPointsCount += rand.Next(20);
                    GatchaDrop drop = new GatchaDrop(6);
                    drop.DataGlobalId = PowerPointsHeroes[i];
                    drop.Count = powerPointsCount;
                    unit.AddDrop(drop);
                }

                GatchaDropRolls(10, unit, true);

                if (rand.Next(2) == 0){
                    var gemsCounts = new List<int>{2,3,5,7,12};
                    int gemsCount = gemsCounts[rand.Next(gemsCounts.Count)];
                    if (rand.Next(100) == 0) gemsCount = 100;
                    GatchaDrop drop = new GatchaDrop(8);
                    drop.Count = gemsCount;
                    unit.AddDrop(drop);
                }

                if (rand.Next(2) == 0){
                    var gemsCounts = new List<int>{200,200,200,400,600};
                    int gemsCount = gemsCounts[rand.Next(gemsCounts.Count)];
                    if (rand.Next(100) == 0) gemsCount = 1000;
                    GatchaDrop drop = new GatchaDrop(2);
                    drop.Count = gemsCount;
                    unit.AddDrop(drop);
                }

                // for (int i = 0; i < 5; i++)
                // {
                //     List<Hero> unlockedHeroes = Avatar.Heroes;
                //     bool heroValid = false;
                //     int generateAttempts = 0;
                //     int idx = -1;
                //     while (!heroValid && generateAttempts < 10)
                //     {
                //         generateAttempts++;
                //         idx = rand.Next(unlockedHeroes.Count);
                //         heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                //         if (heroValid)
                //         {
                //             GatchaDrop drop = new GatchaDrop(6);
                //             drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                //             if (powerPoints.Contains(drop.DataGlobalId))
                //             {
                //                 continue;
                //             }
                //             powerPoints.Add(drop.DataGlobalId);
                //             drop.Count = LogicMath.Min(rand.Next(40, 80), (1410 - unlockedHeroes[idx].PowerPoints));
                //             unit.AddDrop(drop);
                //         }
                //     }
                // }

                // int brawlerChance = 20;
                // if (unlockedBrawlersCount < 4)
                // {
                //     brawlerChance = 70;
                // }
                // else if (unlockedBrawlersCount < 6)
                // {
                //     brawlerChance = 40;
                // }

                // bool isBrawler = rand.Next(0, 100) < brawlerChance;
                // if (isBrawler)
                // {
                //     isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                // }
            }
            else if (unit.Type == 13)
            {
                GatchaDrop coins = new GatchaDrop(7);
                coins.Count = rand.Next(1000, 2000);
                unit.AddDrop(coins);

                var PowerPointsHeroes = new List<int>{};

                foreach (Hero h in Avatar.Heroes){
                    if (h.PowerPoints < 1410){
                        PowerPointsHeroes.Add(h.CharacterId);
                    }
                }

                int ppcount = 10;

                if (PowerPointsHeroes.Count < ppcount) ppcount = PowerPointsHeroes.Count;

                PowerPointsHeroes.ShuffleInPlace();

                int powerPointsCount = 100;

                for (int i = 0; i < ppcount; i++){
                    powerPointsCount += rand.Next(50);
                    GatchaDrop drop = new GatchaDrop(6);
                    drop.DataGlobalId = PowerPointsHeroes[i];
                    drop.Count = powerPointsCount;
                    unit.AddDrop(drop);
                }

                Avatar.RollsSinceGoodDrop = 100000;
                GatchaDropRolls(30, unit);

                if (true){
                    var gemsCounts = new List<int>{25};
                    int gemsCount = gemsCounts[rand.Next(gemsCounts.Count)];
                    if (rand.Next(100) == 0) gemsCount = 100;
                    GatchaDrop drop = new GatchaDrop(8);
                    drop.Count = gemsCount;
                    unit.AddDrop(drop);
                }

                // for (int i = 0; i < 5; i++)
                // {
                //     List<Hero> unlockedHeroes = Avatar.Heroes;
                //     bool heroValid = false;
                //     int generateAttempts = 0;
                //     int idx = -1;
                //     while (!heroValid && generateAttempts < 10)
                //     {
                //         generateAttempts++;
                //         idx = rand.Next(unlockedHeroes.Count);
                //         heroValid = unlockedHeroes[idx].PowerPoints < 1410;
                //         if (heroValid)
                //         {
                //             GatchaDrop drop = new GatchaDrop(6);
                //             drop.DataGlobalId = unlockedHeroes[idx].CharacterId;
                //             if (powerPoints.Contains(drop.DataGlobalId))
                //             {
                //                 continue;
                //             }
                //             powerPoints.Add(drop.DataGlobalId);
                //             drop.Count = LogicMath.Min(rand.Next(40, 80), (1410 - unlockedHeroes[idx].PowerPoints));
                //             unit.AddDrop(drop);
                //         }
                //     }
                // }

                // int brawlerChance = 20;
                // if (unlockedBrawlersCount < 4)
                // {
                //     brawlerChance = 70;
                // }
                // else if (unlockedBrawlersCount < 6)
                // {
                //     brawlerChance = 40;
                // }

                // bool isBrawler = rand.Next(0, 100) < brawlerChance;
                // if (isBrawler)
                // {
                //     isBrawler = GetRandomBrawlerForGatcha(rand, unit);
                // }
            }
            else if (unit.Type == 14)
            {
                for (int x = 0; x < 2; x++){
                    int itemdroprandom = rand.Next(10);
                    GatchaDrop itemdrop = new GatchaDrop(7);
                    if (itemdroprandom < 3){
                        itemdrop = new GatchaDrop(7);
                        itemdrop.Count = 50;
                    }
                    else if (itemdroprandom >= 3 && itemdroprandom < 6){
                        itemdrop = new GatchaDrop(24);
                        itemdrop.Count = 25;
                    }
                    else if (itemdroprandom == 6){
                        itemdrop = new GatchaDrop(25);
                        itemdrop.Count = 100;
                    }
                    else if (itemdroprandom == 7){
                        itemdrop = new GatchaDrop(22);
                        itemdrop.Count = 25;
                    }
                    else if (itemdroprandom == 8){
                        itemdrop = new GatchaDrop(8);
                        itemdrop.Count = 3;
                    }
                    else if (itemdroprandom == 9){

                        int superitemdroprandom = rand.Next(10);
                        if (superitemdroprandom < 3){
                            itemdrop = new GatchaDrop(7);
                            itemdrop.Count = 1000;
                        }
                        else if (superitemdroprandom >= 3 && superitemdroprandom < 6){
                            itemdrop = new GatchaDrop(24);
                            itemdrop.Count = 500;
                        }
                        else if (superitemdroprandom >= 6 && superitemdroprandom < 8){
                            itemdrop = new GatchaDrop(25);
                            itemdrop.Count = 1000;
                        }
                        else if (superitemdroprandom == 8){
                            itemdrop = new GatchaDrop(22);
                            itemdrop.Count = 500;
                        }
                        else if (superitemdroprandom == 9){
                            itemdrop = new GatchaDrop(8);
                            itemdrop.Count = 100;
                        }
                        else{
                            Console.WriteLine("What???");
                        }

                    }
                    else{
                        Console.WriteLine("What???");
                    }
                    unit.AddDrop(itemdrop);
                }

                GatchaDrop eventitemdrop = new GatchaDrop(7);
                int eventitemdroprandom = rand.Next(20);
                
                if (eventitemdroprandom < 7){
                    if (GetEventIcons(1).Count != 0){
                        eventitemdrop = new GatchaDrop(11);
                        eventitemdrop.DataGlobalId = GlobalId.CreateGlobalId(28, GetEventIcons(1)[rand.Next(GetEventIcons(1).Count)]);
                    }
                    else{
                        eventitemdrop = new GatchaDrop(25);
                        eventitemdrop.Count = 250;
                    }
                }
                else if (eventitemdroprandom >= 7 && eventitemdroprandom < 14){
                    if (GetEventPins(1).Count != 0){
                        eventitemdrop = new GatchaDrop(11);
                        eventitemdrop.DataGlobalId = GlobalId.CreateGlobalId(52, GetEventPins(1)[rand.Next(GetEventPins(1).Count)]);
                    }
                    else{
                        eventitemdrop = new GatchaDrop(25);
                        eventitemdrop.Count = 125;
                    }
                }
                else if (eventitemdroprandom >= 14 && eventitemdroprandom < 17){
                    if (GetEventBrawlers(1).Count != 0){
                        eventitemdrop = new GatchaDrop(1);
                        eventitemdrop.Count = rand.Next(11)+1;
                        eventitemdrop.DataGlobalId = GlobalId.CreateGlobalId(16, GetEventBrawlers(1)[rand.Next(GetEventBrawlers(1).Count)]);
                    }
                    else{
                        eventitemdrop = new GatchaDrop(22);
                        eventitemdrop.Count = 250;
                    }
                }
                else if (eventitemdroprandom >= 17){
                    if (GetEventSkins(1).Count != 0){
                        eventitemdrop = new GatchaDrop(9);
                        eventitemdrop.SkinGlobalId = GlobalId.CreateGlobalId(29, GetEventSkins(1)[rand.Next(GetEventSkins(1).Count)]);
                    }
                    else{
                        eventitemdrop = new GatchaDrop(25);
                        eventitemdrop.Count = 1000;
                    }
                }
                unit.AddDrop(eventitemdrop);
            }
            else if (unit.Type == 15){
                unit.Type = 100;
                GatchaDrop eventitemdrop = new GatchaDrop(7);
                int itemdroprandom1 = rand.Next(4);

                if (itemdroprandom1 == 0){
                    eventitemdrop = new GatchaDrop(7);
                    eventitemdrop.Count = 2000;
                }
                else if (itemdroprandom1 == 0){
                    eventitemdrop = new GatchaDrop(24);
                    eventitemdrop.Count = 1000;
                }
                else if (itemdroprandom1 == 0){
                    eventitemdrop = new GatchaDrop(25);
                    eventitemdrop.Count = 5000;
                }
                else{
                    int itemdroprandom2 = rand.Next(25);
                    if (itemdroprandom2 < 8){
                        eventitemdrop = new GatchaDrop(7);
                        eventitemdrop.Count = 5000;
                    }
                    else if (itemdroprandom2 >= 8 && itemdroprandom2 < 16){
                        eventitemdrop = new GatchaDrop(24);
                        eventitemdrop.Count = 2500;
                    }
                    else if (itemdroprandom2 >= 16 && itemdroprandom2 < 24){
                        eventitemdrop = new GatchaDrop(25);
                        eventitemdrop.Count = 10000;
                    }
                    else{
                        int itemdroprandom3 = rand.Next(10);
                        if (itemdroprandom3 < 3){
                            eventitemdrop = new GatchaDrop(7);
                            eventitemdrop.Count = 10000;
                        }
                        else if (itemdroprandom3 >= 3 && itemdroprandom3 < 6){
                            eventitemdrop = new GatchaDrop(24);
                            eventitemdrop.Count = 5000;
                        }
                        else if (itemdroprandom3 >= 6 && itemdroprandom3 < 9){
                            eventitemdrop = new GatchaDrop(25);
                            eventitemdrop.Count = 20000;
                        }
                        else{
                            int itemdroprandom4 = rand.Next(1000);
                            if (itemdroprandom4 < 333){
                                eventitemdrop = new GatchaDrop(7);
                                eventitemdrop.Count = 25000;
                            }
                            else if (itemdroprandom4 >= 333 && itemdroprandom4 < 666){
                                eventitemdrop = new GatchaDrop(24);
                                eventitemdrop.Count = 25000;
                            }
                            else if (itemdroprandom4 >= 666 && itemdroprandom4 < 999){
                                eventitemdrop = new GatchaDrop(25);
                                eventitemdrop.Count = 25000;
                            }
                            else{
                                eventitemdrop = new GatchaDrop(8);
                                eventitemdrop.Count = 1000000;
                            }
                        }
                    }
                }

                unit.AddDrop(eventitemdrop);
            }
            else if (unit.Type == 16){
                unit.Type = 100;
                // GatchaDrop eventitemdrop = new GatchaDrop(9);

                // if (GetEventSkins(1000).Count != 0){
                //     eventitemdrop = new GatchaDrop(9);
                //     eventitemdrop.SkinGlobalId = GlobalId.CreateGlobalId(29, GetEventSkins(1000)[rand.Next(GetEventSkins(1000).Count)]);
                // }
                // else{
                //     eventitemdrop = new GatchaDrop(25);
                //     eventitemdrop.Count = 8233;
                // }
                // unit.AddDrop(eventitemdrop);

                GatchaDrop eventitemdrop2 = new GatchaDrop(9);

                if (GetEventSkins(1001).Count != 0){
                    eventitemdrop2 = new GatchaDrop(9);
                    eventitemdrop2.SkinGlobalId = GlobalId.CreateGlobalId(29, GetEventSkins(1001)[rand.Next(GetEventSkins(1001).Count)]);
                }
                else{
                    eventitemdrop2 = new GatchaDrop(25);
                    eventitemdrop2.Count = 7500;
                }

                unit.AddDrop(eventitemdrop2);
            }
            else if (unit.Type == 20){
                unit.Type = 100;
                GatchaDrop eventitemdrop = new GatchaDrop(9);

                if (GetEventSkins(1000).Count != 0){
                    eventitemdrop = new GatchaDrop(9);
                    eventitemdrop.SkinGlobalId = GlobalId.CreateGlobalId(29, GetEventSkins(1000)[rand.Next(GetEventSkins(1000).Count)]);
                }
                else{
                    eventitemdrop = new GatchaDrop(25);
                    eventitemdrop.Count = 15000;
                }
                unit.AddDrop(eventitemdrop);

                // GatchaDrop eventitemdrop2 = new GatchaDrop(9);

                // if (GetEventSkins(1001).Count != 0){
                //     eventitemdrop2 = new GatchaDrop(9);
                //     eventitemdrop2.SkinGlobalId = GlobalId.CreateGlobalId(29, GetEventSkins(1001)[rand.Next(GetEventSkins(1001).Count)]);
                // }
                // else{
                //     eventitemdrop2 = new GatchaDrop(25);
                //     eventitemdrop2.Count = 8233;
                // }

                // unit.AddDrop(eventitemdrop2);
            }
            else if (unit.Type == 17){
                unit.Type = 100;
                GatchaDrop eventitemdrop = new GatchaDrop(1);
                List<int> road_list = new List<int> { };
                foreach (int brawler in Home.BrawlersRoad)
                {
                    if (!HasHeroUnlocked(16000000 + brawler))
                    {
                        road_list.Add(brawler);
                    }
                }

                if (road_list.Count != 0){
                    eventitemdrop.Count = rand.Next(5, 12);
                    eventitemdrop.DataGlobalId = GlobalId.CreateGlobalId(16, road_list[rand.Next(road_list.Count)]);
                }
                else{
                    eventitemdrop = new GatchaDrop(22);
                    eventitemdrop.Count = 2661;
                }

                unit.AddDrop(eventitemdrop);
            }
            else if (unit.Type == 19){
                setChaosDropRarity = 5;
                unit.Type = 18;
            }
            if (unit.Type == 18){
                int chaosDropCount = 0;
                int chaosDropRarity = -1;
                // unit.Type = 10;

                var RandomInt1 = rand.Next(100);
                var RandomInt2 = rand.Next(100);

                if (RandomInt1 < 2) chaosDropRarity = 5;
                else if (RandomInt1 < 8) chaosDropRarity = 4;
                else if (RandomInt1 < 20) chaosDropRarity = 3;
                else if (RandomInt1 < 48) chaosDropRarity = 2;
                else if (RandomInt1 < 100) chaosDropRarity = 1;

                if (setChaosDropRarity != -1) chaosDropRarity = setChaosDropRarity;

                if (RandomInt2 < 2) chaosDropCount = 8;
                else if (RandomInt2 < 8) chaosDropCount = 4;
                else if (RandomInt2 < 28) chaosDropCount = 2;
                else if (RandomInt2 < 100) chaosDropCount = 1;

                List<int> UnlockedBrawlersInDrop = new();


                for (int x = 0; x < chaosDropCount; x++){
                    int GatchaDropRandom = rand.Next(100);
                    int GatchaDropId = 1;
                    int GatchaDropCount = 1;
                    int GatchaDropBrawler = -1;
                    int GatchaDropSkin = -1;
                    int GatchaDropData = -1;
                    int GatchaDropGlobalId = -1;

                    if (chaosDropRarity == 1){
                        if (GatchaDropRandom < 4){
                            GatchaDropId = 25;
                            GatchaDropCount = 50;
                        }
                        else if (GatchaDropRandom < 8){
                            GatchaDropId = 22;
                            GatchaDropCount = 30;
                        }
                        else if (GatchaDropRandom < 40){
                            GatchaDropId = 24;
                            GatchaDropCount = 50;
                        }
                        else{
                            GatchaDropId = 7;
                            GatchaDropCount = 100;
                        }
                    }
                    else if (chaosDropRarity == 2){
                        if (GatchaDropRandom < 10){
                            int rareSkin = StarrDrop.GetRandomSkin(this, 29);
                            if (rareSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = rareSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 250;
                            }
                        }
                        else if (GatchaDropRandom < 20){
                            GatchaDropId = 22;
                            GatchaDropCount = 150;
                        }
                        else if (GatchaDropRandom < 60){
                            GatchaDropId = 24;
                            GatchaDropCount = 100;
                        }
                        else{
                            GatchaDropId = 7;
                            GatchaDropCount = 200;
                        }
                    }
                    else if (chaosDropRarity == 3){
                        if (GatchaDropRandom < 4){
                            int superRareSkin = StarrDrop.GetRandomSkin(this, 79);
                            if (superRareSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = superRareSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 1000;
                            }
                        }
                        else if (GatchaDropRandom < 20){
                            int rareSkin = StarrDrop.GetRandomSkin(this, 29);
                            if (rareSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = rareSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 250;
                            }
                        }
                        else if (GatchaDropRandom < 23){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 1000;
                            }
                        }
                        else if (GatchaDropRandom < 35){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 500;
                            }
                        }
                        else if (GatchaDropRandom < 43){
                            GatchaDropId = 22;
                            GatchaDropCount = 150;
                        }
                        else if (GatchaDropRandom < 63){
                            GatchaDropId = 7;
                            GatchaDropCount = 500;
                        }
                        else if (GatchaDropRandom < 88){
                            GatchaDropId = 24;
                            GatchaDropCount = 200;
                        }
                        else{
                            GatchaDropId = 7;
                            GatchaDropCount = 1000;
                        }
                    }
                    else if (chaosDropRarity == 4){
                        if (GatchaDropRandom < 22){
                            int epicSkin = StarrDrop.GetRandomSkin(this, 149);
                            if (epicSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = epicSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 2000;
                            }
                        }
                        else if (GatchaDropRandom < 25){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 1000;
                            }
                        }
                        else if (GatchaDropRandom < 31){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 1000;
                            }
                        }
                        else if (GatchaDropRandom < 48){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(3, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 500;
                            }
                        }
                        else if (GatchaDropRandom < 74){
                            GatchaDropId = 24;
                            GatchaDropCount = 1000;
                        }
                        else{
                            GatchaDropId = 7;
                            GatchaDropCount = 2000;
                        }
                    }
                    else if (chaosDropRarity == 5){
                        if (GatchaDropRandom < 10){
                            int epicSkin = StarrDrop.GetRandomSkin(this, 149);
                            if (epicSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = epicSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 2000;
                            }
                        }
                        else if (GatchaDropRandom < 14){
                            int mythicSkin = StarrDrop.GetRandomSkin(this, 199);
                            if (mythicSkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = mythicSkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 3000;
                            }
                        }
                        else if (GatchaDropRandom < 16){
                            int legendarySkin = StarrDrop.GetRandomSkin(this, 149);
                            if (legendarySkin != -1){
                                GatchaDropId = 9;
                                GatchaDropSkin = legendarySkin - 29000000;
                            }
                            else{
                                GatchaDropId = 25;
                                GatchaDropCount = 5000;
                            }
                        }
                        else if (GatchaDropRandom < 41){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(4, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 500;
                            }
                        }
                        else if (GatchaDropRandom < 49){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(5, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 1000;
                            }
                        }
                        else if (GatchaDropRandom < 50){
                            if (GetLockedBrawlersByRarityWithUnlockedBrawlers(6, UnlockedBrawlersInDrop).Count != 0){
                                int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(6, UnlockedBrawlersInDrop)[rand.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(6, UnlockedBrawlersInDrop).Count)];
                                UnlockedBrawlersInDrop.Add(UnlockedBrawler);
                                GatchaDropId = 1;
                                GatchaDropBrawler = UnlockedBrawler;
                            }
                            else{
                                GatchaDropId = 22;
                                GatchaDropCount = 2000;
                            }
                        }
                        else if (GatchaDropRandom < 75){
                            GatchaDropId = 24;
                            GatchaDropCount = 1000;
                        }
                        else{
                            GatchaDropId = 7;
                            GatchaDropCount = 2000;
                        }
                    }


                    GatchaDrop chaosdropitem = new GatchaDrop(GatchaDropId);
                    chaosdropitem.Count = GatchaDropCount;
                    if (GatchaDropBrawler != -1) chaosdropitem.DataGlobalId = GlobalId.CreateGlobalId(16, GatchaDropBrawler);
                    if (GatchaDropSkin != -1) chaosdropitem.SkinGlobalId = GlobalId.CreateGlobalId(29, GatchaDropSkin);
                    if (GatchaDropData != -1) chaosdropitem.DataGlobalId = GlobalId.CreateGlobalId(GatchaDropData, GatchaDropGlobalId);
                    unit.AddDrop(chaosdropitem);
                }


            }


            // bool bonus = rand.Next(0, 100) < 50;
            // if (bonus) // add gems bonus
            // {
            //     int count = rand.Next(2, 7) + 1;
            //     GatchaDrop drop = new GatchaDrop(8);
            //     drop.Count = count;
            //     unit.AddDrop(drop);
            // }
        }

        public List<int> GetEventIcons(int id){
            List<int> LockedIDs = new();
            List<int> IDs = new();
            if (id == 1) IDs = new List<int>{332,142,143,144,145,146,210,187,206,192,233,215,240};

            foreach (int x in IDs){
                if (!Home.UnlockedThumbnails.Contains(28000000+x)) LockedIDs.Add(x);
            }

            return LockedIDs;
        }

        public List<int> GetEventPins(int id){
            List<int> LockedIDs = new();
            List<int> IDs = new();
            if (id == 1) IDs = new List<int>{1209,1211,1212,1213,1214,1215,1216,1217,309,311,819,373,375,376,450,452,453,163,816,267,631,633,634,419,421,422,785,787,788};

            foreach (int x in IDs){
                if (!Home.UnlockedEmotes.Contains(52000000+x)) LockedIDs.Add(x);
            }

            return LockedIDs;
        }

        public List<int> GetEventBrawlers(int id){
            List<int> LockedIDs = new();
            List<int> IDs = new();
            if (id == 1) IDs = new List<int>{43,50,39,30,28,5,1,54,2,6,61,60,4,72,77,17,63,65,35};

            foreach (int x in IDs){
                if (!Avatar.HasHero(x+16000000)) LockedIDs.Add(x);
            }

            return LockedIDs;
        }

        public List<int> GetEventSkins(int id){
            List<int> LockedIDs = new();
            List<int> IDs = new();
            if (id == 1) IDs = new List<int>{208,209,210,214,956,957,213,751,2,68,467,389,167,118,466,500,486,715,111,759,609,613,27,29,608,282,281};
            if (id == 1000) IDs = new List<int>{180,201,203,343,308,307,357,358,380,934,935,758,755,756,762,763,765,766};
            if (id == 1001) IDs = new List<int>{120,122,126,215,387,412,415,417,557,587,591,722,727};

            foreach (int x in IDs){
                if (!Home.UnlockedSkins.Contains(x+29000000)) LockedIDs.Add(x);
            }

            return LockedIDs;
        }

        public void GatchaDropRolls(int count, DeliveryUnit unit, bool IsEventDrop = false){
            var r = new Random();
            var Drop = new List<int>();
            var BrawlersGet = new List<int>{0,0,0,0,0,0,0};
            var SkinsGet = new List<int>{0,0};
            for (int x = 0; x < count; x++){
                Avatar.RollsSinceGoodDrop++;
                if (r.Next(10000) < (268 + Avatar.RollsSinceGoodDrop) && (GetLockedBrawlersByRarity(1).Count-BrawlersGet[1]) > 0) {
                    Drop.Add(1);
                    BrawlersGet[1]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < (120 + (Avatar.RollsSinceGoodDrop/2))  && (GetLockedBrawlersByRarity(2).Count-BrawlersGet[2]) > 0) {
                    Drop.Add(2);
                    BrawlersGet[2]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < (54 + (Avatar.RollsSinceGoodDrop/3)) && (GetLockedBrawlersByRarity(3).Count-BrawlersGet[3]) > 0) {
                    Drop.Add(3);
                    BrawlersGet[3]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < (24 + (Avatar.RollsSinceGoodDrop/4)) && (GetLockedBrawlersByRarity(4).Count-BrawlersGet[4]) > 0) {
                    Drop.Add(4);
                    BrawlersGet[4]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < (10 + (Avatar.RollsSinceGoodDrop/5)) && (GetLockedBrawlersByRarity(5).Count-BrawlersGet[5]) > 0) {
                    Drop.Add(5);
                    BrawlersGet[5]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < (7 + (Avatar.RollsSinceGoodDrop/6)) && (GetLockedBrawlersByRarity(6).Count-BrawlersGet[6]) > 0) {
                    Drop.Add(6);
                    BrawlersGet[6]++;
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (Avatar.RollsSinceGoodDrop > 60000){
                    Drop.Add(10);
                    Avatar.RollsSinceGoodDrop = 0;
                }
                else if (r.Next(10000) < ((268 + Avatar.RollsSinceGoodDrop)*2) && (GetLockedEventSkins(1).Count-SkinsGet[1]) > 0 && IsEventDrop){
                    Drop.Add(11);
                    SkinsGet[1]++;
                    Avatar.RollsSinceGoodDrop = 0;
                    Console.WriteLine("QWERGGFYJRHHHHHHHHHHHHHHHHHHH");
                }
                // Console.WriteLine("r.Next(1000) < ((268 + Avatar.RollsSinceGoodDrop)*2) = " + (r.Next(1000) < ((268 + Avatar.RollsSinceGoodDrop)*2)));
                // Console.WriteLine("(GetLockedEventSkins(1).Count-SkinsGet[1]) > 0 && IsEventDrop = " + ((GetLockedEventSkins(1).Count-SkinsGet[1]) > 0 && IsEventDrop));
                // Console.WriteLine("IsEventDrop = " + (IsEventDrop));
                // Console.WriteLine("(GetLockedEventSkins(1).Count-SkinsGet[1]) = " + ((GetLockedEventSkins(1).Count-SkinsGet[1]) > 0));
                // Console.WriteLine("(GetLockedEventSkins(1).Count) = " + GetLockedEventSkins(1).Count);
            }
            List<int> UnlockedBrawlers = new List<int>{};
            List<int> UnlockedSkins = new List<int>{};
            foreach (int dropID in Drop){
                if (dropID < 10 && GetLockedBrawlersByRarityWithUnlockedBrawlers(dropID, UnlockedBrawlers).Count > 0){
                    GatchaDrop reward = new GatchaDrop(1);
                    reward.Count = 1;
                    int UnlockedBrawler = GetLockedBrawlersByRarityWithUnlockedBrawlers(dropID, UnlockedBrawlers)[r.Next(GetLockedBrawlersByRarityWithUnlockedBrawlers(dropID, UnlockedBrawlers).Count)];
                    UnlockedBrawlers.Add(UnlockedBrawler);
                    reward.DataGlobalId = 16000000 + UnlockedBrawler;
                    unit.AddDrop(reward);
                    // LogicStarroadRefreshCommand commands = new LogicStarroadRefreshCommand();
                    // commands.Execute(HomeMode);
                    // AvailableServerCommandMessage serverCommandMessage = new AvailableServerCommandMessage();
                    // serverCommandMessage.Command = commands;
                    // HomeMode.GameListener.SendMessage(serverCommandMessage);
                }
                if (dropID == 10){
                    GatchaDrop reward = new GatchaDrop(22);
                    reward.Count = 1000;
                    unit.AddDrop(reward);
                }
                if (dropID > 10 && GetLockedEventSkinsWithUnlockedSkins(dropID-10, UnlockedSkins).Count > 0){
                    GatchaDrop reward = new GatchaDrop(9);
                    int UnlockedSkin = GetLockedEventSkinsWithUnlockedSkins(dropID-10, UnlockedSkins)[r.Next(GetLockedEventSkinsWithUnlockedSkins(dropID-10, UnlockedSkins).Count)];
                    UnlockedSkins.Add(UnlockedSkin);
                    reward.SkinGlobalId = 29000000 + UnlockedSkin;
                    unit.AddDrop(reward);
                }
            }
        }

        public List<int> GetLockedEventSkins(int SkinSet){
            List<int> SkinsLocked = new List<int>();
            List<int> SkinsID = new List<int>();
            if (SkinSet == 1) SkinsID = new List<int>{844, 845, 848, 849, 850, 851, 852, 2, 29, 30, 44};
            foreach (int Skin in SkinsID)
            {
                if (!Home.UnlockedSkins.Contains(Skin)){
                    SkinsLocked.Add(Skin);
                }
            }
            return SkinsLocked;
        }

        public List<int> GetLockedEventSkinsWithUnlockedSkins(int SkinSet, List<int> UnlockedSkins){
            List<int> SkinsLocked = new List<int>();
            List<int> SkinsID = new List<int>();
            if (SkinSet == 1) SkinsID = new List<int>{844, 845, 848, 849, 850, 851, 852};
            foreach (int Skin in SkinsID)
            {
                if (!Home.UnlockedSkins.Contains(Skin) && !UnlockedSkins.Contains(Skin)){
                    SkinsLocked.Add(Skin);
                }
            }
            return SkinsLocked;
        }

        public List<int> GetLockedBrawlersByRarity(int BrawlersRare){
            List<int> BrawlersLocked = new List<int>();
            List<int> BrawlersID = new List<int>();
            if (BrawlersRare == 1) BrawlersID = GeneralStaticLogic.rare;
            if (BrawlersRare == 2) BrawlersID = GeneralStaticLogic.super_rare;
            if (BrawlersRare == 3) BrawlersID = GeneralStaticLogic.epic;
            if (BrawlersRare == 4) BrawlersID = GeneralStaticLogic.mythic;
            if (BrawlersRare == 5) BrawlersID = GeneralStaticLogic.legendary;
            if (BrawlersRare == 6) BrawlersID = GeneralStaticLogic.ultra_legendary;
            foreach (int Brawler in BrawlersID)
            {
                if (!Avatar.HasHero(Brawler+16000000)){
                    BrawlersLocked.Add(Brawler);
                }
            }
            return BrawlersLocked;
        }

        public List<int> GetLockedBrawlersByRarityWithUnlockedBrawlers(int BrawlersRare, List<int> UnlockedBrawlers){
            List<int> BrawlersLocked = new List<int>();
            List<int> BrawlersID = new List<int>();
            if (BrawlersRare == 1) BrawlersID = GeneralStaticLogic.rare;
            if (BrawlersRare == 2) BrawlersID = GeneralStaticLogic.super_rare;
            if (BrawlersRare == 3) BrawlersID = GeneralStaticLogic.epic;
            if (BrawlersRare == 4) BrawlersID = GeneralStaticLogic.mythic;
            if (BrawlersRare == 5) BrawlersID = GeneralStaticLogic.legendary;
            if (BrawlersRare == 6) BrawlersID = GeneralStaticLogic.ultra_legendary;
            foreach (int Brawler in BrawlersID)
            {
                if (!Avatar.HasHero(Brawler+16000000) && !UnlockedBrawlers.Contains(Brawler)){
                    BrawlersLocked.Add(Brawler);
                }
            }
            return BrawlersLocked;
        }

        public void Enter(DateTime dateTime)
        {
            Home.HomeVisited();
        }

        public static string GenerateRandomString(int maxLength)
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            int length = random.Next(1, maxLength + 1); // �� 1 �� 7 ��������
            char[] result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }

            return new string(result);
        }
        public void ClientTurnReceived(int tick, int checksum, List<Command> commands)
        {
            foreach (Command command in commands)
            {
                if (command.Execute(this) != 0)
                {
                    OutOfSyncMessage outOfSync = new OutOfSyncMessage();
                    GameListener.SendMessage(outOfSync);
                }
            }
            Home.Tick();
        }
    }
}
    

