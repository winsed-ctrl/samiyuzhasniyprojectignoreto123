using System.Collections.Generic;

namespace GromCore.Laser.Logic.Home
{
    public enum PromoRewardType
    {
        Gems = 0,
        Coins = 1,
        PowerPoints = 2,
        Bling = 3,
        StarPoints = 4,
        Skin = 5,
        Brawler = 6,
        VIP = 7,
        BrawlPass = 8,
        BrawlPassPlus = 9,
        RecruitTokens = 10,
        ChaosDrop = 11,
        UltraChaosDrop = 12,
        Emote = 13,
        Spray = 14,
        PlayerTitle = 15,
        PlayerThumbnail = 16
    }

    public class PromoReward
    {
        public PromoRewardType Type { get; set; }
        public int Amount { get; set; }
        public int ItemId { get; set; }

        public PromoReward() { }
        public PromoReward(PromoRewardType type, int amount, int itemId = 0)
        {
            Type = type;
            Amount = amount;
            ItemId = itemId;
        }
    }
}