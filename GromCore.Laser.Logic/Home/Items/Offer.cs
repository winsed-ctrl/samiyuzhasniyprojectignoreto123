namespace GromCore.Laser.Logic.Home.Items
{
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Titan.DataStream;

    public class Offer
    {
        public ShopItem Type;
        public int Count;

        public int ItemDataId;
        public int SkinDataId;

        public Offer()
        {
            // Json.
        }

        public Offer(ShopItem type, int count)
        {
            Type = type;
            Count = count;
        }

        public Offer(ShopItem type, int count, int itemGlobalId) : this(type, count)
        {
            ItemDataId = itemGlobalId;
        }

        public Offer(ShopItem type, int count, int itemGlobalId, int skinGlobalId) : this(type, count, itemGlobalId)
        {
            SkinDataId = skinGlobalId;
        }

        public void Encode(ByteStream stream)
        {
            stream.WriteVInt((int)Type);
            stream.WriteVInt(Count);
            ByteStreamHelper.WriteDataReference(stream, ItemDataId);
            stream.WriteVInt(SkinDataId);
        }
    }

    public enum ShopItem
    {
        FreeBox = 0,
        BrawlBox = 6,
        BigBox = 14,
        MegaBox = 100,
        GigaBox = 101,
        Ticket = 7,
        HeroPower = 8,
        WildcardPower = 12,
        CoinDoubler = 9,
        Keys = 11,
        EventSlot = 13,
        AdBox = 15,
        Coin = 1,
        Gems = 16,
        StarPoints = 17,
        BrawlPassTokens = 28,
        Emote = 19,
        EmoteBundle = 20,
        RandomEmotes = 21,
        RandomEmotesForBrawler = 22,
        RandomEmoteOfRarity = 23,
        RandomEmotesPackOfRarity = 27,
        PlayerThumbnail = 25,
        GuaranteedBox = 2,
        GuaranteedHero = 3,
        Skin = 4,
        SkinAndHero = 24,
        PurchaseOptionSkin = 26,
        Item = 5,
        ClubFeature = 29,
        GuaranteedHeroWithLevel = 30,
        GuaranteedBoxWithLevel = 31,
        GearToken = 32,
        GearScrap = 33,
        Spray = 35,
        PowerPoint = 41,
        Bling = 45,
        RecruitToken = 38,
        OverchargeStarrDrop = 47,
        BrawlerUpgradeFromLVLToLVL = 48,
        RarityStarrDrop = 49,
        RandomStarrDrop = 50,
        BrawlPassBundle = 46, 
        BrawlPassPlusBundle = 52, 
        Candy = 102, 
        ZombieBox = 103, 
        EventTokens = 89, 
        EventGatcha = 90, 
        PlayerTitle = 43, 
        ChaosDrop = 91, 
        UltraChaosDrop = 92, 
        KeyResource = 93, 
        KeyBuffies = 94, 
        KeySkins = 95, 
        KeyBrawlers = 96
    }
}
