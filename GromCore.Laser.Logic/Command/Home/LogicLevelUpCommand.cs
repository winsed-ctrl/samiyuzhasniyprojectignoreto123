namespace GromCore.Laser.Logic.Command.Home
{
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using System.Numerics;

    public class LogicLevelUpCommand : Command
    {
        private int CharacterId;
        const double GEMS_PER_COIN = 0.1;
        public override void Decode(ByteStream stream)
        {
            base.Decode(stream);
            stream.ReadVInt(); // class
            CharacterId = stream.ReadVInt(); // я забыл
        }

        public override int Execute(HomeMode homeMode)
        {
            Hero hero = homeMode.Avatar.GetHero(GlobalId.CreateGlobalId(16, CharacterId));
            if (hero == null) return -1;
            // bool Check(int count)
            // {
            //     if (count > homeMode.Avatar.Gold || )
            //     {
            //         int remainingCoins = homeMode.Avatar.Gold;
            //         homeMode.Avatar.Gold = 0;
            //         int deficit = count - remainingCoins;
            //         double totalGemsNeeded = deficit * GEMS_PER_COIN;
            //         int roundedGemsNeeded = (int)Math.Ceiling(totalGemsNeeded);

            //         if (!homeMode.Avatar.UseDiamonds(roundedGemsNeeded))
            //         {
            //             return false;
            //         }

            //         return true;
            //     }

            //     homeMode.Avatar.Gold -= count;
            //     return true;
            // }

            int roundedGemsCoinsNeeded = 0;
            int roundedGemsPowerNeeded = 0;
            int count = 0;

            count = Hero.UpgradeCostTable[hero.PowerLevel - 1];
            if (count > homeMode.Avatar.Gold){
                int remainingCoins = homeMode.Avatar.Gold;
                //homeMode.Avatar.Gold = 0;
                int deficit = count - remainingCoins;
                double totalGemsNeeded = deficit * GEMS_PER_COIN;
                roundedGemsCoinsNeeded = (int)Math.Ceiling(totalGemsNeeded);
            }
            count = Hero.PowerPointsTable[hero.PowerLevel-1];
            if (count > (homeMode.Avatar.PowerPoints + hero.PowerPoints)){
                int remainingPowerPoints = homeMode.Avatar.PowerPoints;
                //homeMode.Avatar.Gold = 0;
                int deficit = count - remainingPowerPoints;
                double totalGemsNeeded = deficit * GEMS_PER_COIN;
                roundedGemsPowerNeeded = (int)Math.Ceiling(totalGemsNeeded*2);
            }

            int roundedGemsNeeded = roundedGemsCoinsNeeded + roundedGemsPowerNeeded;
            if (roundedGemsNeeded == 0){
                if (!homeMode.Avatar.UseGold(Hero.UpgradeCostTable[hero.PowerLevel - 1])) return -2;
                if (!homeMode.Avatar.UsePowerPoints(Hero.PowerPointsTable[hero.PowerLevel-1], hero)) return -2;
            }
            else if (homeMode.Avatar.UseDiamonds(roundedGemsNeeded)){
                if (roundedGemsCoinsNeeded != 0) homeMode.Avatar.Gold = 0;
                else homeMode.Avatar.UseGold(Hero.UpgradeCostTable[hero.PowerLevel - 1]);
                if (roundedGemsPowerNeeded != 0) homeMode.Avatar.PowerPoints = 0;
                else homeMode.Avatar.UsePowerPoints(Hero.PowerPointsTable[hero.PowerLevel-1], hero);
            }
            else return -2;

            //if (!Check(Hero.UpgradeCostTable[hero.PowerLevel - 1])) return -2;
            

            hero.PowerPoints += Hero.PowerPointsTable[hero.PowerLevel-1];
            hero.PowerLevel++;

            homeMode.Home.LvLUpOffers = true;
            homeMode.Home.RefreshOffers();
            LogicOffersChangedMessage refreshoffers = new LogicOffersChangedMessage();
            refreshoffers.OfferBundles = homeMode.Home.OfferBundles;
            homeMode.GameListener.SendMessage(refreshoffers);
            if(homeMode.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(homeMode.Avatar.TeamId);
            return 0;
        }

        public override int GetCommandType()
        {
            return 520;
        }
    }
}
