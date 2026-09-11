using GromCore.Laser.Logic.Home;
using System;
using System.Collections.Generic;
using System.Linq;
using GromCore.Laser.Titan.DataStream;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;
using System.Data;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Home.Gatcha;
using GromCore.Laser.Logic.Message.Home;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Security.Cryptography;
using GromCore.Laser.Logic.Command.Home;
using Masuda.Net.Models;
using GromCore.Laser.Logic;

namespace GromCore.Laser.Logic.Home.Items
{
    [JsonObject(MemberSerialization.OptOut)]
    public class Possibility
    {
        public int Type { get; set; }
        public int Ammount { get; set; }
        public int DataGlobalID { get; set; }
        public int SkinGlobalID { get; set; }
    }

    public enum StarrDropRarity
    {
        Rare = 0,
        SuperRare = 1,
        Epic = 2,
        Mythic = 3,
        Legendary = 4,
        Hypercharged = 5
    }

    public class StarrDrop
    {
        private static readonly Random random = new Random();

        public StarrDropRarity Rarity { get; private set; }
        public bool RandomDrop { get; private set; }
        public Possibility Data { get; private set; } = new Possibility();

        public StarrDrop(HomeMode mode)
        {
            if (mode == null) return;
            GenerateDrop(mode);
        }

        public void GenerateDrop(HomeMode init)
        {
            ClientHome home = init.Home;
            if (home.StarrDrop == null)
                home.StarrDrop = new StarrDrop(init);

            if (home.ExecuteLobbyDrop)
                home.ExecuteLobbyDrop = false;

            double chance = random.NextDouble() * 100;
            if (chance < 40) Rarity = StarrDropRarity.Rare;
            else if (chance < 50) Rarity = StarrDropRarity.SuperRare;
            else if (chance < 67) Rarity = StarrDropRarity.Epic;
            else if (chance < 75) Rarity = StarrDropRarity.Mythic;
            else if (chance < 85) Rarity = StarrDropRarity.Legendary;
            //else Rarity = StarrDropRarity.Hypercharged;

            GenerateRarityDrop(init, Rarity);
            home.StarrDrop.RandomDrop = true;
        }

        public void GenerateRarityDrop(HomeMode init, StarrDropRarity rarity)
        {
            ClientHome home = init.Home;
            if (home.StarrDrop == null)
                home.StarrDrop = new StarrDrop(init);

            home.StarrDrop.Rarity = rarity;
            home.StarrDrop.RandomDrop = false;

            switch (rarity)
            {
                case StarrDropRarity.Rare:
                    GenerateRareDrop(init);
                    break;
                case StarrDropRarity.SuperRare:
                    GenerateSuperRareDrop(init);
                    break;
                case StarrDropRarity.Epic:
                    GenerateEpicDrop(init);
                    break;
                case StarrDropRarity.Mythic:
                    GenerateMythicDrop(init);
                    break;
                case StarrDropRarity.Legendary:
                    GenerateLegendaryDrop(init);
                    break;
                case StarrDropRarity.Hypercharged:
                    GenerateOverchargeDrop(init);
                    break;
            }
        }

        public static int GetRandomSkin(HomeMode home, int price) 
        { 
            List<int> globalIds = new List<int>(); 
            foreach (SkinData emoteData in DataTables.Get(DataType.Skin).GetDatas()) 
            { CharacterData SkinOwner = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(0); 
                if (emoteData.Conf != null) SkinOwner = DataTables.Get(16).GetData<CharacterData>(DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(emoteData.Conf).Character); 
                if (!emoteData.Name.EndsWith("Default") && emoteData.PriceGems == price 
                    && !GeneralStaticLogic.BlockedObtainTypes.Contains(emoteData.ObtainType) 
                    && emoteData.Campaigns != "SILVER" && emoteData.Campaigns != "GOLD" 
                    && home.Avatar.HasHero(SkinOwner.GetGlobalId()) && 
                    !home.Home.UnlockedSkins.Contains(emoteData.GetGlobalId())) 
                { globalIds.Add(DataTables.Get(DataType.Skin).GetData<SkinData>(emoteData.Name).GetGlobalId()); 
                } 
            } 
            if (globalIds.Count > 0) 
            { 
                Random random = new();
                globalIds.ForEach(x => {
                    if (GlobalId.GetClassId(x) == 23)
                        x = GlobalId.CreateGlobalId(29, GlobalId.GetInstanceId(x));
                });
                return globalIds[random.Next(globalIds.Count)]; 
            } return -1; 
        }
        
        public static int GetRandomPin(HomeMode home, string price) 
        { List<int> globalIds = new List<int>(); foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas()) 
            { if (emoteData.Rarity == price 
                    && emoteData.Skin == null
                    && !home.Home.UnlockedEmotes.Contains(emoteData.GetGlobalId()) 
                    && home.Avatar.HasHero(DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character) != null 
                    ? DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Character).GetGlobalId() 
                    : GlobalId.CreateGlobalId(16,0))) 
                { globalIds.Add(DataTables.Get(DataType.Emote).GetData<EmoteData>(emoteData.Name).GetGlobalId()); } 
            } 
            if (globalIds.Count > 0) 
            { Random random = new Random(); 
                return globalIds[random.Next(globalIds.Count)]; 
            } return -1; }
        public static int GetRandomThumbnail(HomeMode home) { List<int> globalIds = new List<int>(); foreach (PlayerThumbnailData emoteData in DataTables.Get(DataType.PlayerThumbnail).GetDatas()) { if (emoteData.IsAvailableForOffers) { globalIds.Add(DataTables.Get(DataType.PlayerThumbnail).GetData<PlayerThumbnailData>(emoteData.Name).GetGlobalId()); } } if (globalIds.Count > 0) { Random random = new Random(); return globalIds[random.Next(globalIds.Count)]; } return -1; }
        public static int GetRandomBrawlerByRarity(string rare, HomeMode home) { List<int> globalIds = new List<int>(); 
            foreach (CharacterData emoteData in DataTables.Get(DataType.Character).GetDatas().Cast<CharacterData>()) 
            { CardData card = DataTables.Get(DataType.Card).GetData<CardData>(emoteData.Name + "_unlock"); 
                if (card != null && card.Rarity == rare && 
                    !emoteData.Disabled && !ReleaseEntry.LogicReleaaseEntry.Contains(emoteData.GetInstanceId()) && 
                    !home.Avatar.HasHero(emoteData.GetGlobalId())
                    && !emoteData.LockedForChronos) { globalIds.Add(DataTables.Get(DataType.Character).GetData<CharacterData>(emoteData.Name).GetGlobalId()); } 
            } if (globalIds.Count > 0) { Random random = new Random(); 
                return globalIds[random.Next(globalIds.Count)]; } return -1; }
        public static int GetRandomOvercharge(HomeMode init) { List<int> globalIds = new List<int>(); foreach (CardData emoteData in DataTables.Get(DataType.Card).GetDatas()) { if (emoteData.Name.EndsWith("overcharge") && !init.Avatar.SPGS.Contains(emoteData.GetGlobalId())) { globalIds.Add(emoteData.GetGlobalId()); } } if (globalIds.Count > 0) { Random random = new Random(); return globalIds[random.Next(globalIds.Count)]; } return -1; }
        public static int GetRandomSPG(HomeMode init) { List<int> globalIds = new List<int>(); foreach (CardData emoteData in DataTables.Get(DataType.Card).GetDatas()) { if (emoteData.Name.EndsWith("unique_2") || emoteData.Name.EndsWith("unique") && !init.Avatar.SPGS.Contains(emoteData.GetGlobalId())) { globalIds.Add(emoteData.GetGlobalId()); } } if (globalIds.Count > 0) { Random random = new Random(); return globalIds[random.Next(globalIds.Count)]; } return -1; }
        public static int GetRandomGadget(HomeMode init) { List<int> globalIds = new List<int>(); foreach (CardData emoteData in DataTables.Get(DataType.Card).GetDatas()) { if (emoteData.Type == "accessory" && !init.Avatar.SPGS.Contains(emoteData.GetGlobalId())) { globalIds.Add(emoteData.GetGlobalId()); } } if (globalIds.Count > 0) { Random random = new Random(); return globalIds[random.Next(globalIds.Count)]; } return -1; }

        private void SetData(int type, int amount, int dataId = 0, int skinId = 0)
        {
            Data.Type = type;
            Data.Ammount = amount;
            Data.DataGlobalID = dataId;
            Data.SkinGlobalID = skinId;
        }

        public void GenerateRareDrop(HomeMode init)
        {
            double chance = random.NextDouble() * 100;
            if (chance < 40) SetData(7, 50);
            else if (chance < 60) SetData(24, 25);
            else if (chance < 75) SetData(2, random.Next(100, 150));
            else if (chance < 85) SetData(22, 10); 
            else SetData(25, 100);
        }

        public void GenerateSuperRareDrop(HomeMode init)
        {
            double chance = random.NextDouble() * 100;
            if (chance < 40) SetData(7, 100);
            else if (chance < 70) SetData(24, 50);
            else if (chance < 81) SetData(2, random.Next(190, 250));
            else if (chance < 85) SetData(22, 30);
            else SetData(25, 50);
        }

        public void GenerateEpicDrop(HomeMode init)
        {
            double chance = random.NextDouble() * 100;
            if (chance < 30) SetData(7, 200); // Coins
            else if (chance < 50) SetData(2, random.Next(500, 575));
            else if (chance < 70) SetData(24, 100);
            else if (chance < 75) TrySkin(init, 29, 250);
            else if (chance < 79) TryPin(init, "RARE", 100);
            else if (chance < 89) TryPin(init, "COMMON", 100);
            else if (chance < 91) TryBrawler(init, "rare", 160);
            else TrySkin(init, 79, 500);
        }

        public void GenerateMythicDrop(HomeMode init)
        {
            double chance = random.NextDouble() * 100;
            if (chance < 35) SetData(7, 500);
            else if (chance < 45) SetData(24, 200);
            else if (chance < 65) TryPin(init, "EPIC", 350);
            else if (chance < 75) TryBrawler(init, "super_rare", 250);
            else if (chance < 80) TrySkin(init, 79, 500);
            else if (chance < 83) TryBrawler(init, "rare", 160);
            else if (chance < 86) TryBrawler(init, "epic", 500);
            else if (chance < 90) TryBrawler(init, "mega_epic", 650);
            else TrySkin(init, 29, 250);
        }

        public void GenerateLegendaryDrop(HomeMode init)
        {
            double chance = random.NextDouble() * 100;
            //if (chance < 60) TryCard(init, GetRandomGadget(init), 7, 1000);
            //else if (chance < 61) TryCard(init, GetRandomSPG(init), 7, 1000);
            //else if (chance < 62) TryCard(init, GetRandomOvercharge(init), 7, 1000);
            if (chance < 63) TrySkin(init, 79, 500);
            else if (chance < 65) TrySkin(init, 149, 2350);
            else if (chance < 70) TryBrawler(init, "epic", 350);
            else if (chance < 79) TryBrawler(init, "mega_epic", 500);
            else TryBrawler(init, "legendary", 750);
        }

        public void GenerateOverchargeDrop(HomeMode init)
        {
            int spg = GetRandomOvercharge(init);
            if (spg == -1)
                SetData(7, 1000);
            else
                SetData(4, 1, spg);
        }

        private void TrySkin(HomeMode init, int price, int fallbackBling)
        {
            int skin = GetRandomSkin(init, price);
            if (skin == -1) SetData(25, fallbackBling);
            else SetData(9, 1, 0, skin);
        }

        private void TryPin(HomeMode init, string rarity, int fallbackBling)
        {
            int pin = GetRandomPin(init, rarity);
            if (pin == -1) SetData(25, fallbackBling);
            else SetData(11, 1, pin);
        }

        private void TryBrawler(HomeMode init, string rarity, int fallbackCredits)
        {
            int brawler = GetRandomBrawlerByRarity(rarity, init);
            if (brawler == -1) SetData(22, fallbackCredits);
            else SetData(1, 1, brawler);
        }

        private void TryCard(HomeMode init, int cardId, int fallbackType, int fallbackAmount)
        {
            if (cardId == -1) SetData(fallbackType, fallbackAmount);
            else SetData(4, 1, cardId);
        }


        public void Encode(ByteStream byteStream, HomeMode player)
        {
            byteStream.WriteVInt(5);
            for (int i = 0; i < 5; i++)
            {
                byteStream.WriteDataReference(80, i);
                byteStream.WriteVInt(-1);
                byteStream.WriteVInt(0);
            }
            //if (player.Home.ExecuteLobbyDrop && player.Avatar.Trophies >= 50)
            //{
            //    player.Home.StarrDrop.GenerateDrop(player);
            //    byteStream.WriteBoolean(true);
            //    {
            //        byteStream.WriteDataReference(80, (int)player.Home.StarrDrop.Rarity);
            //        byteStream.WriteVInt(1);
            //        {
            //            byteStream.WriteVInt(player.Home.StarrDrop.Data.Type);
            //            byteStream.WriteVInt(player.Home.StarrDrop.Data.Ammount);
            //            byteStream.WriteDataReference(0, 0);
            //            byteStream.WriteVInt(0);

            //            byteStream.WriteVInt(0);
            //            byteStream.WriteVInt(0);
            //        }
            //    }
            //}
            //else
            //{
                byteStream.WriteVInt(0);
                byteStream.WriteVInt(0);
            //}
            // starrdrops here disabled for a looooooong time i guess

            byteStream.WriteInt(-1788180018);
            byteStream.WriteVInt(player.Avatar.Wins);
            byteStream.WriteVInt(0);
            byteStream.WriteVInt(0);
            byteStream.WriteVInt(0);
            byteStream.WriteVInt(0);
        }
    }
}
