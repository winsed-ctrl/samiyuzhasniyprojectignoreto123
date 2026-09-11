using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GromCore.Laser.Logic.Avatar;
using GromCore.Laser.Logic.Command.Home;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Logic.Home.Gatcha;
using GromCore.Laser.Logic.Message.Home;
using GromCore.Laser.Server.Networking.Session;
using GromCore.Laser.Logic.Club;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Data.Helper;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Titan.Math;

namespace GromCore.Laser.Server.Database
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
        public string ItemName { get; set; }

        public PromoReward() { }

        public PromoReward(PromoRewardType type, int amount, int itemId = 0, string itemName = null)
        {
            Type = type;
            Amount = amount;
            ItemId = itemId;
            ItemName = itemName ?? type.ToString();
        }

        public string GetDescription()
        {
            switch (Type)
            {
                case PromoRewardType.Gems: return $"{Amount} 💎 гемов";
                case PromoRewardType.Coins: return $"{Amount} 🪙 монет";
                case PromoRewardType.PowerPoints: return $"{Amount} ⚡ очков силы";
                case PromoRewardType.Bling: return $"{Amount} ✨ блингов";
                case PromoRewardType.StarPoints: return $"{Amount} ⭐ старпоинтов";
                case PromoRewardType.Skin: return $"🎨 Скин #{ItemId}";
                case PromoRewardType.Brawler: return $"🤖 Боец #{ItemId}";
                case PromoRewardType.VIP: return $"👑 VIP уровень {Amount}";
                case PromoRewardType.BrawlPass: return $"🎫 Brawl Pass";
                case PromoRewardType.BrawlPassPlus: return $"👑 Brawl Pass Plus";
                case PromoRewardType.RecruitTokens: return $"🎲 {Amount} рекруит токенов";
                case PromoRewardType.ChaosDrop: return $"🌀 {Amount} хаосдропов";
                case PromoRewardType.UltraChaosDrop: return $"💫 {Amount} ультра-хаосдропов";
                case PromoRewardType.Emote: return $"📌 Пин #{ItemId}";
                case PromoRewardType.Spray: return $"🎨 Спрей #{ItemId}";
                case PromoRewardType.PlayerTitle: return $"🏆 Титул #{ItemId}";
                case PromoRewardType.PlayerThumbnail: return $"🖼️ Иконка #{ItemId}";
                default: return "Неизвестная награда";
            }
        }
    }

    public class PromoCode
    {
        public string Code { get; set; }
        public List<PromoReward> Rewards { get; set; }
        public int MaxUses { get; set; }
        public int CurrentUses { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string CreatedBy { get; set; }
        public List<string> UsedBy { get; set; }

        public PromoCode()
        {
            Rewards = new List<PromoReward>();
            UsedBy = new List<string>();
            CreatedAt = DateTime.UtcNow;
        }

        public bool IsValid()
        {
            if (MaxUses > 0 && CurrentUses >= MaxUses) return false;
            if (ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value) return false;
            return true;
        }

        public bool CanBeUsedBy(long accountId, string playerTag)
        {
            return !UsedBy.Contains(accountId.ToString()) && !UsedBy.Contains(playerTag);
        }

        public void MarkUsed(long accountId, string playerTag)
        {
            CurrentUses++;
            if (!UsedBy.Contains(accountId.ToString())) UsedBy.Add(accountId.ToString());
            if (!UsedBy.Contains(playerTag) && playerTag != accountId.ToString()) UsedBy.Add(playerTag);
        }
    }

    public static class PromoCodeManager
    {
        private static readonly string PromoCodesFile = "promocodes.json";
        private static Dictionary<string, PromoCode> _promoCodes = new Dictionary<string, PromoCode>();
        private static readonly object SyncRoot = new object();

        static PromoCodeManager() { Load(); }

        public static void Load()
        {
            if (File.Exists(PromoCodesFile))
            {
                try
                {
                    string json = File.ReadAllText(PromoCodesFile);
                    _promoCodes = JsonConvert.DeserializeObject<Dictionary<string, PromoCode>>(json) ?? new Dictionary<string, PromoCode>();
                    Console.WriteLine($"[PromoCode] Загружено {_promoCodes.Count} промокодов");
                }
                catch (Exception ex) { Console.WriteLine($"[PromoCode] Ошибка загрузки: {ex.Message}"); _promoCodes = new Dictionary<string, PromoCode>(); }
            }
            else { _promoCodes = new Dictionary<string, PromoCode>(); Save(); }
        }

        public static void Save()
        {
            try { File.WriteAllText(PromoCodesFile, JsonConvert.SerializeObject(_promoCodes, Formatting.Indented)); }
            catch (Exception ex) { Console.WriteLine($"[PromoCode] Ошибка сохранения: {ex.Message}"); }
        }

        public static bool CreatePromoCode(string code, List<PromoReward> rewards, int maxUses, DateTime? expiresAt, string createdBy)
        {
            if (string.IsNullOrWhiteSpace(code) || rewards == null || rewards.Count == 0 || maxUses < 0) return false;
            // Store canonical data references at creation time. This keeps old
            // callers (not only the Telegram parser) from persisting short or
            // wrong-table ids that later cannot be delivered.
            if (!NormalizeRewards(rewards, out _)) return false;
            code = code.Trim().ToUpperInvariant();
            lock (SyncRoot)
            {
                if (_promoCodes.ContainsKey(code)) return false;
                var promo = new PromoCode { Code = code, Rewards = rewards, MaxUses = maxUses, CreatedBy = createdBy, ExpiresAt = expiresAt };
                _promoCodes[code] = promo;
                Save();
                return true;
            }
        }

        public static PromoCode GetPromoCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            lock (SyncRoot)
                return _promoCodes.TryGetValue(code.Trim().ToUpperInvariant(), out var promo) ? promo : null;
        }

        public static bool TryNormalizeSkinId(int requestedId, out int globalId, out SkinData skinData)
        {
            globalId = 0;
            skinData = null;
            if (requestedId < 0) return false;

            // Accept both the short table instance id (e.g. 123) and the
            // canonical skin global id (e.g. 29000123), but never interpret a
            // global id from another data table as a skin just because its
            // instance part happens to exist in the skin table.
            if (requestedId >= 1_000_000 &&
                GlobalId.GetClassId(requestedId) != (int)DataType.Skin)
                return false;

            int instanceId = GlobalId.GetInstanceId(requestedId);
            var table = DataTables.Get(DataType.Skin);
            if (table == null || instanceId < 0 || instanceId >= table.Count) return false;
            skinData = table.GetData<SkinData>(instanceId);
            if (skinData == null) return false;
            globalId = skinData.GetGlobalId();
            return true;
        }

        public static bool NormalizeReward(PromoReward reward, out string error)
        {
            error = null;
            if (reward == null) { error = "пустая награда"; return false; }
            if (reward.Amount <= 0) { error = $"количество для {reward.Type} должно быть больше нуля"; return false; }
            if (reward.Type >= PromoRewardType.Skin && reward.ItemId < 0)
            { error = "ID предмета не может быть отрицательным"; return false; }
            switch (reward.Type)
            {
                case PromoRewardType.Skin:
                    if (!TryNormalizeSkinId(reward.ItemId, out int skinGlobalId, out _))
                    { error = $"скин с ID {reward.ItemId} не найден"; return false; }
                    reward.ItemId = skinGlobalId;
                    reward.Amount = 1;
                    break;
                case PromoRewardType.Brawler:
                    reward.ItemId = GlobalId.GetInstanceId(reward.ItemId);
                    if (DataTables.Get(DataType.Character)?.GetData<CharacterData>(reward.ItemId) == null)
                    { error = $"боец с ID {reward.ItemId} не найден"; return false; }
                    reward.Amount = 1;
                    break;
                case PromoRewardType.Emote:
                    reward.ItemId = GlobalId.GetInstanceId(reward.ItemId);
                    if (DataTables.Get(DataType.Emote)?.GetData<EmoteData>(reward.ItemId) == null)
                    { error = $"пин с ID {reward.ItemId} не найден"; return false; }
                    reward.Amount = 1;
                    break;
                case PromoRewardType.Spray:
                    reward.ItemId = GlobalId.GetInstanceId(reward.ItemId);
                    if (DataTables.Get(DataType.Spray)?.GetData<SprayData>(reward.ItemId) == null)
                    { error = $"спрей с ID {reward.ItemId} не найден"; return false; }
                    reward.Amount = 1;
                    break;
                case PromoRewardType.PlayerTitle:
                    reward.ItemId = GlobalId.GetInstanceId(reward.ItemId);
                    if (DataTables.Get(DataType.Titul)?.GetData<TitlesData>(reward.ItemId) == null)
                    { error = $"титул с ID {reward.ItemId} не найден"; return false; }
                    reward.Amount = 1;
                    break;
                case PromoRewardType.PlayerThumbnail:
                    reward.ItemId = GlobalId.GetInstanceId(reward.ItemId);
                    if (DataTables.Get(DataType.PlayerThumbnail)?.GetData<PlayerThumbnailData>(reward.ItemId) == null)
                    { error = $"иконка с ID {reward.ItemId} не найдена"; return false; }
                    reward.Amount = 1;
                    break;
            }
            return true;
        }

        public static bool NormalizeRewards(List<PromoReward> rewards, out string error)
        {
            error = null;
            if (rewards == null || rewards.Count == 0) { error = "не указаны награды"; return false; }
            foreach (var reward in rewards)
                if (!NormalizeReward(reward, out error)) return false;
            return true;
        }

        public static bool UsePromoCode(string code, long accountId, string playerTag, out List<PromoReward> rewards, out string error)
        {
            rewards = null; error = null;
            var promo = GetPromoCode(code);
            if (promo != null && !NormalizeRewards(promo.Rewards, out string rewardError))
            { error = $"Promo reward is invalid: {rewardError}"; return false; }
            if (promo == null) { error = "❌ Промокод не найден!"; return false; }
            if (!promo.IsValid()) { error = promo.MaxUses > 0 && promo.CurrentUses >= promo.MaxUses ? "❌ Лимит использований исчерпан!" : "❌ Срок действия истёк!"; return false; }
            if (promo.CanBeUsedBy(accountId, playerTag))
            {
                lock (SyncRoot)
                {
                    if (!promo.IsValid() || !promo.CanBeUsedBy(accountId, playerTag))
                    { error = "Promo code already used or exhausted"; return false; }
                    promo.MarkUsed(accountId, playerTag);
                    Save();
                    rewards = promo.Rewards;
                    return true;
                }
            }
            else { error = "❌ Вы уже использовали этот промокод!"; return false; }
        }

        public static void RollbackUse(string code, long accountId, string playerTag)
        {
            var promo = GetPromoCode(code);
            if (promo == null) return;
            lock (SyncRoot)
            {
                if (promo.CurrentUses > 0) promo.CurrentUses--;
                promo.UsedBy?.Remove(accountId.ToString());
                if (!string.IsNullOrEmpty(playerTag)) promo.UsedBy?.Remove(playerTag);
                Save();
            }
        }

        public static List<PromoCode> GetAllPromoCodes()
        {
            lock (SyncRoot) return _promoCodes.Values.ToList();
        }

        public static bool DeletePromoCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            lock (SyncRoot)
            {
                if (_promoCodes.Remove(code.Trim().ToUpperInvariant())) { Save(); return true; }
                return false;
            }
        }
    }

    public static class PromoRewardGiver
    {
        public static void GiveRewards(HomeMode homeMode, List<PromoReward> rewards, out string rewardMessage)
        {
            GiveRewards(homeMode, rewards, out rewardMessage, out _);
        }

        /// <summary>
        /// Grants the rewards and returns the exact delivery command that must be
        /// sent to the client.  Executing the command updates the account state;
        /// encoding/sending the same command restores the normal reward animation.
        /// </summary>
        public static void GiveRewards(HomeMode homeMode, List<PromoReward> rewards,
            out string rewardMessage, out LogicGiveDeliveryItemsCommand deliveryCommand)
        {
            if (homeMode == null) throw new ArgumentNullException(nameof(homeMode));
            if (rewards == null) throw new ArgumentNullException(nameof(rewards));
            homeMode.Home.UnlockedSkins ??= new List<int>();
            homeMode.Home.UnlockedEmotes ??= new List<int>();
            homeMode.Home.UnlockedSprays ??= new List<int>();
            homeMode.Home.UnlockedTituls ??= new List<int>();
            homeMode.Home.UnlockedThumbnails ??= new List<int>();
            var command = new LogicGiveDeliveryItemsCommand();
            var messages = new List<string>();

            foreach (var reward in rewards)
            {
                switch (reward.Type)
                {
                    case PromoRewardType.Gems:
                        homeMode.Avatar.AddDiamonds(reward.Amount);
                        messages.Add($"💎 +{reward.Amount} гемов");
                        break;

                    case PromoRewardType.Coins:
                        homeMode.Avatar.AddGold(reward.Amount);
                        messages.Add($"🪙 +{reward.Amount} монет");
                        break;

                    case PromoRewardType.PowerPoints:
                        homeMode.Avatar.AddPowerPoints(reward.Amount);
                        messages.Add($"⚡ +{reward.Amount} очков силы");
                        break;

                    case PromoRewardType.Bling:
                        homeMode.Avatar.AddBlings(reward.Amount);
                        messages.Add($"✨ +{reward.Amount} блингов");
                        break;

                    case PromoRewardType.StarPoints:
                        homeMode.Avatar.AddStarPoints(reward.Amount);
                        messages.Add($"⭐ +{reward.Amount} старпоинтов");
                        break;

                    case PromoRewardType.RecruitTokens:
                        homeMode.Home.RecruitTokens += reward.Amount;
                        messages.Add($"🎲 +{reward.Amount} рекруит токенов");
                        break;

                    case PromoRewardType.Skin:
                        if (!PromoCodeManager.TryNormalizeSkinId(reward.ItemId, out int skinGlobalId, out SkinData skinData))
                        {
                            messages.Add($"Skin #{reward.ItemId} was not found");
                            break;
                        }
                        if (!homeMode.Home.UnlockedSkins.Contains(skinGlobalId))
                        {
                            homeMode.Home.UnlockedSkins.Add(skinGlobalId);
                            messages.Add($"🎨 Скин #{reward.ItemId} добавлен!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(9);
                            drop.SkinGlobalId = skinGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            homeMode.Avatar.AddBlings(500);
                            messages.Add($"🎨 Скин уже есть, +500 блингов");
                        }
                        break;

                    case PromoRewardType.Brawler:
                        int brawlerGlobalId = GlobalId.CreateGlobalId(16, GlobalId.GetInstanceId(reward.ItemId));
                        if (!homeMode.Avatar.HasHero(brawlerGlobalId))
                        {
                            homeMode.Avatar.UnlockHero(brawlerGlobalId);
                            messages.Add($"🤖 Новый боец #{reward.ItemId} добавлен!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(1);
                            drop.DataGlobalId = brawlerGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            homeMode.Avatar.AddGold(1000);
                            messages.Add($"🤖 Боец уже есть, +1000 монет");
                        }
                        break;

                    case PromoRewardType.Emote:
                        int emoteGlobalId = GlobalId.CreateGlobalId(52, GlobalId.GetInstanceId(reward.ItemId));
                        if (!homeMode.Home.UnlockedEmotes.Contains(emoteGlobalId))
                        {
                            homeMode.Home.UnlockedEmotes.Add(emoteGlobalId);
                            messages.Add($"📌 Пин #{reward.ItemId} добавлен!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(11);
                            drop.DataGlobalId = emoteGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            messages.Add($"📌 Пин #{reward.ItemId} уже есть");
                        }
                        break;

                    case PromoRewardType.Spray:
                        int sprayGlobalId = GlobalId.CreateGlobalId(68, GlobalId.GetInstanceId(reward.ItemId));
                        if (!homeMode.Home.UnlockedSprays.Contains(sprayGlobalId))
                        {
                            homeMode.Home.UnlockedSprays.Add(sprayGlobalId);
                            messages.Add($"🎨 Спрей #{reward.ItemId} добавлен!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(11);
                            drop.DataGlobalId = sprayGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            messages.Add($"🎨 Спрей #{reward.ItemId} уже есть");
                        }
                        break;

                    case PromoRewardType.PlayerTitle:
                        int titleGlobalId = GlobalId.CreateGlobalId(76, GlobalId.GetInstanceId(reward.ItemId));
                        if (!homeMode.Home.UnlockedTituls.Contains(titleGlobalId))
                        {
                            homeMode.Home.UnlockedTituls.Add(titleGlobalId);
                            messages.Add($"🏆 Титул #{reward.ItemId} добавлен!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(11);
                            drop.DataGlobalId = titleGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            messages.Add($"🏆 Титул #{reward.ItemId} уже есть");
                        }
                        break;

                    case PromoRewardType.PlayerThumbnail:
                        int thumbnailGlobalId = GlobalId.CreateGlobalId(28, GlobalId.GetInstanceId(reward.ItemId));
                        if (!homeMode.Home.UnlockedThumbnails.Contains(thumbnailGlobalId))
                        {
                            homeMode.Home.UnlockedThumbnails.Add(thumbnailGlobalId);
                            messages.Add($"🖼️ Иконка #{reward.ItemId} добавлена!");
                            
                            DeliveryUnit unit = new DeliveryUnit(100);
                            GatchaDrop drop = new GatchaDrop(11);
                            drop.DataGlobalId = thumbnailGlobalId;
                            drop.Count = 1;
                            unit.AddDrop(drop);
                            command.DeliveryUnits.Add(unit);
                        }
                        else
                        {
                            messages.Add($"🖼️ Иконка #{reward.ItemId} уже есть");
                        }
                        break;

                    case PromoRewardType.VIP:
                        homeMode.Avatar.SetVipLevel(reward.Amount);
                        messages.Add($"👑 VIP уровень изменён на {reward.Amount}");
                        break;

                    case PromoRewardType.BrawlPass:
                        homeMode.Home.HasPremiumPass = true;
                        messages.Add($"🎫 Brawl Pass активирован!");
                        break;

                    case PromoRewardType.BrawlPassPlus:
                        homeMode.Home.HasPremiumPass = true;
                        homeMode.Home.HasPremiumPassPlus = true;
                        messages.Add($"👑 Brawl Pass Plus активирован!");
                        break;

                    case PromoRewardType.ChaosDrop:
                        for (int i = 0; i < reward.Amount; i++)
                        {
                            DeliveryUnit unit = new DeliveryUnit(18);
                            homeMode.SimulateGatcha(unit);
                            command.DeliveryUnits.Add(unit);
                        }
                        messages.Add($"🌀 +{reward.Amount} хаосдропов");
                        break;

                    case PromoRewardType.UltraChaosDrop:
                        for (int i = 0; i < reward.Amount; i++)
                        {
                            DeliveryUnit unit = new DeliveryUnit(19);
                            homeMode.SimulateGatcha(unit);
                            command.DeliveryUnits.Add(unit);
                        }
                        messages.Add($"💫 +{reward.Amount} ультра-хаосдропов");
                        break;

                    default:
                        messages.Add($"❓ Неизвестная награда");
                        break;
                }
            }

            if (command.DeliveryUnits.Count > 0)
            {
                command.Execute(homeMode);
            }

            rewardMessage = string.Join("\n", messages);
            deliveryCommand = command;
        }
    }
}
