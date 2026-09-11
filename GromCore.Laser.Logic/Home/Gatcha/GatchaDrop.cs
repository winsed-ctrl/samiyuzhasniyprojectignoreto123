namespace GromCore.Laser.Logic.Home.Gatcha
{
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Titan.DataStream;

    public class GatchaDrop
    {
        public static readonly int[] UpgradePowerPointsTable = new int[]
        {
            20, 50, 100, 180, 310, 520, 860, 1410, 2300, 2300 + 1440
        };

        public int Count;
        public int DataGlobalId;
        public int SkinGlobalId;
        public int Type;
        public int CardGlobalId;

        public GatchaDrop(int type)
        {
            Type = type;
        }

        public void DoDrop(HomeMode homeMode)
        {
            ClientAvatar avatar = homeMode.Avatar;

            switch (Type)
            {
                case 1: // Unlock a hero
                    CharacterData characterData = DataTables.Get(16).GetDataByGlobalId<CharacterData>(DataGlobalId);
                    if (characterData == null) return;

                    CardData cardData = DataTables.Get(23).GetData<CardData>(characterData.Name + "_unlock");
                    if (cardData == null) return;

                    if(!avatar.HasHero(characterData.GetGlobalId())) avatar.UnlockHero(characterData.GetGlobalId());

                    if (Count > 1) {
                        foreach (Hero uhero in avatar.Heroes){
                            if (uhero.CharacterId == characterData.GetGlobalId()){
                                uhero.PowerLevel = Count;
                                uhero.PowerPoints = UpgradePowerPointsTable[uhero.PowerLevel-2];
                            }
                        }
                    }
                    break;
                case 4: // card
                    CardData spg = DataTables.Get(23).GetData<CardData>(CardGlobalId);
                    if (spg == null || spg.Name == null || spg.DirectPurchasePrice == 0) return;
                    homeMode.Avatar.SPGS.Add(spg.GetGlobalId());

                    CharacterData hero;
                    CardData card = DataTables.Get(DataType.Card).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(29, CardGlobalId));
                    if (card == null) return;

                    string m = card.Name.Replace("_2", "");
                    m = m.Replace("_3", "");
                    CardData card1 = DataTables.Get(DataType.Card).GetData<CardData>(m);
                    CardData card2 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_2");
                    CardData card3 = DataTables.Get(DataType.Card).GetData<CardData>(m + "_3");
                    hero = DataTables.Get(DataType.Character).GetData<CharacterData>(card.Name.Split("_")[0]);
                    Hero h = homeMode.Avatar.GetHero(hero.GetGlobalId());

                    homeMode.Avatar.SelectedSPGS.Remove(card1.GetGlobalId());
                    if(card2 != null)homeMode.Avatar.SelectedSPGS.Remove(card2.GetGlobalId());
                    if (card3 != null)
                    {
                        homeMode.Avatar.SelectedSPGS.Remove(card3.GetGlobalId());
                    }

                    Hero playerHero = homeMode.Avatar.GetHeroForCard(card);
                    if (spg.MetaType == 4) playerHero.SelectedStarPowerId = spg.GetInstanceId();
                    else playerHero.SelectedGadgetId = spg.GetInstanceId();
                    
                    homeMode.Avatar.SelectedSPGS.Add(spg.GetGlobalId());
                    homeMode.CharacterChanged.Invoke(0);
                    break;
                case 2:
                    homeMode.Home.TokenDoublers += Count;
                    break;
                case 6: // Add power points
                    Hero herop = avatar.GetHero(DataGlobalId);
                    if (herop == null) return;

                    herop.PowerPoints += Count;
                    break;
                case 7: // Add gold
                    avatar.AddGold(Count);
                    break;
                case 8: // Add Gems (Bonus)
                    avatar.AddDiamonds(Count);
                    break;
                case 9: // 
                    homeMode.Home.UnlockedSkins ??= new List<int>();
                    if (!homeMode.Home.UnlockedSkins.Contains(SkinGlobalId))
                        homeMode.Home.UnlockedSkins.Add(SkinGlobalId);
                    break;
                case 11:
                    if (DataGlobalId > 28000000 && DataGlobalId < 29000000) { homeMode.Home.UnlockedThumbnails ??= new List<int>(); if (!homeMode.Home.UnlockedThumbnails.Contains(DataGlobalId)) homeMode.Home.UnlockedThumbnails.Add(DataGlobalId); }
                    if (DataGlobalId > 52000000 && DataGlobalId < 53000000) { homeMode.Home.UnlockedEmotes ??= new List<int>(); if (!homeMode.Home.UnlockedEmotes.Contains(DataGlobalId)) homeMode.Home.UnlockedEmotes.Add(DataGlobalId); }
                    if (DataGlobalId > 76000000 && DataGlobalId < 77000000) { homeMode.Home.UnlockedTituls ??= new List<int>(); if (!homeMode.Home.UnlockedTituls.Contains(DataGlobalId)) homeMode.Home.UnlockedTituls.Add(DataGlobalId); }
                    if (DataGlobalId > 68000000 && DataGlobalId < 69000000) { homeMode.Home.UnlockedSprays ??= new List<int>(); if (!homeMode.Home.UnlockedSprays.Contains(DataGlobalId)) homeMode.Home.UnlockedSprays.Add(DataGlobalId); }
                    break;
                case 12: // 
                    homeMode.Avatar.AddStarPoints(Count, false);
                    break;
                case 27:
                case 22:
                    if(Count > 0) avatar.AddRareTokens(Count);  
                    break;
                case 24:
                    avatar.AddPowerPoints(Count);
                    break;
                case 25:
                    avatar.AddBlings(Count);
                    break;
                case 26:
                    homeMode.Home.HasPremiumPass = true;
                    break;
                case 31:
                    homeMode.Home.HasPremiumPass = true;
                    homeMode.Home.HasPremiumPassPlus = true;
                    homeMode.Home.BrawlPassTokens += 8000;
                    break;
                case 10:
                    homeMode.Home.UnlockedEmotes.Add(DataGlobalId);
                    break;
            }
        }

        public static int GetGatchaDropByShopItem(int ShopItem)
        {
            switch (ShopItem)
            {
                case 1:
                    return 7;
                case 9:
                    return 2;
                case 41:
                    return 24;
                case 45:
                    return 25;
                case 16:
                    return 8;
                case 3:
                    return 1;
                case 4:
                    return 9;
                case 38:
                    return 22;
                case 19:
                    return 11;
                case 25:
                    return 11;
                case 102:
                    return 12;
                default:
                    return -1;
            }
        }


        public void Encode(ByteStream stream)
        {
            stream.WriteVInt(Count);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            stream.WriteVInt(Type);

            ByteStreamHelper.WriteDataReference(stream, SkinGlobalId);
            ByteStreamHelper.WriteDataReference(stream, DataGlobalId);
            if (CardGlobalId != 0) ByteStreamHelper.WriteDataReference(stream, CardGlobalId + 23000000);
            else ByteStreamHelper.WriteDataReference(stream, CardGlobalId);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
        }
    }
}
