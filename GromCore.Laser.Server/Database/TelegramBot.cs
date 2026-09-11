using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Newtonsoft.Json;
using System.IO;
using GromCore.Laser.Server.Database.Cache;
using GromCore.Laser.Server.Database.Models;
using GromCore.Laser.Logic.Message.Account.Auth;
using GromCore.Laser.Server.Networking.Session;
using GromCore.Laser.Logic.Home.Items;
using GromCore.Laser.Logic.Command.Home;
using GromCore.Laser.Logic.Message.Home;
using GromCore.Laser.Server.Networking;
using GromCore.Laser.Server.Utils;
using GromCore.Laser.Logic.Club;
using GromCore.Laser.Logic.Avatar;
using GromCore.Laser.Logic.Data;
using GromCore.Laser.Logic.Data.Helper;
using GromCore.Laser.Logic.Helper;
using GromCore.Laser.Logic.Home;
using GromCore.Laser.Server.Logic.Game;
using System.Text;
using System.Diagnostics;
using System.IO.Compression;

// ==================== КЛАССЫ ДЛЯ СОВМЕСТИМОСТИ ====================
namespace GromCore.Laser.Logic
{
    public class CustomRewardData
    {
        public string Type { get; set; }
        public int Count { get; set; }
        public int DataId { get; set; }
    }

    public class CustomOfferData
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int Cost { get; set; }
        public int OldCost { get; set; }
        public int Currency { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string BackgroundName { get; set; }
        public bool IsActive { get; set; }
        public List<CustomRewardData> Rewards { get; set; }
    }

    public static class OfferBridge
    {
        private static List<CustomOfferData> _customOffers = new List<CustomOfferData>();
        
        public static List<CustomOfferData> GetCustomOffers()
        {
            return _customOffers;
        }
        
        public static void SetCustomOffers(List<CustomOfferData> offers)
        {
            _customOffers = offers ?? new List<CustomOfferData>();
            Console.WriteLine($"[OfferBridge] Получено {_customOffers.Count} акций");
        }
    }
}

namespace GromCore.Laser.Server.Database
{
    // ==================== МОДЕЛИ ДЛЯ АКЦИЙ ====================
    
    public class CustomOfferReward
    {
        [JsonProperty] public string Type { get; set; }
        [JsonProperty] public int Count { get; set; }
        [JsonProperty] public int DataId { get; set; }
        
        public string GetDescription()
        {
            string typeName = Type switch
            {
                "gems" => "💎 Гемы",
                "coins" => "🪙 Монеты",
                "pp" => "⚡ Очки силы",
                "bling" => "✨ Блинги",
                "sp" => "⭐ Старпоинты",
                "recruit" => "🎫 Рекруит токены",
                "skin" => "👕 Скин",
                "brawler" => "🤖 Боец",
                "emote" => "😊 Пин",
                "spray" => "🎨 Спрей",
                "title" => "🏆 Титул",
                "icon" => "🖼️ Иконка",
                "vip" => "👑 VIP",
                "pass" => "🎫 Brawl Pass",
                "passplus" => "⭐ Brawl Pass Plus",
                "chaos" => "🌀 Хаосдроп",
                "ultra" => "💥 Ультра-хаосдроп",
                _ => Type
            };
            if (DataId > 0)
                return $"{typeName} x{Count} (ID: {DataId})";
            if (Type == "vip" && DataId > 0)
                return $"{typeName} {Count} уровня на {DataId} дней";
            if (Type == "vip")
                return $"{typeName} {Count} уровня (бессрочно)";
            return $"{typeName} x{Count}";
        }
    }
    
    public class CustomOffer
    {
        [JsonProperty] public string Id { get; set; }
        [JsonProperty] public string Title { get; set; }
        [JsonProperty] public string Description { get; set; }
        [JsonProperty] public int Cost { get; set; }
        [JsonProperty] public int OldCost { get; set; }
        [JsonProperty] public int Currency { get; set; }
        [JsonProperty] public DateTime StartTime { get; set; }
        [JsonProperty] public DateTime EndTime { get; set; }
        [JsonProperty] public string BackgroundName { get; set; }
        [JsonProperty] public bool IsActive { get; set; }
        [JsonProperty] public List<CustomOfferReward> Rewards { get; set; }
        [JsonProperty] public DateTime CreatedAt { get; set; }
        [JsonProperty] public string CreatedBy { get; set; }
        
        public CustomOffer()
        {
            Id = Guid.NewGuid().ToString("N").Substring(0, 8);
            Rewards = new List<CustomOfferReward>();
            IsActive = true;
            CreatedAt = DateTime.Now;
            Currency = 0;
            BackgroundName = "offer_bgr_legendary";
        }
        
        public bool IsValid()
        {
            return IsActive && StartTime <= DateTime.Now && EndTime >= DateTime.Now;
        }
        
        public string GetStatusIcon()
        {
            if (!IsActive) return "❌";
            if (StartTime > DateTime.Now) return "⏳";
            if (EndTime < DateTime.Now) return "⌛";
            return "✅";
        }
        
        public string GetCurrencyName()
        {
            return Currency switch
            {
                0 => "💎 Гемы",
                1 => "🪙 Монеты",
                2 => "✨ Блинги",
                3 => "⭐ Старпоинты",
                _ => "Неизвестно"
            };
        }
    }
    
    // ==================== МЕНЕДЖЕР АКЦИЙ ====================
    
    public static class OffersManager
    {
        private static readonly string OffersFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_offers.json");
        private static List<CustomOffer> _customOffers = new List<CustomOffer>();
        private static Timer _offerUpdateTimer;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            LoadOffers();
            
            var bridgeOffers = new List<GromCore.Laser.Logic.CustomOfferData>();
            foreach (var offer in _customOffers)
            {
                var bridgeOffer = new GromCore.Laser.Logic.CustomOfferData
                {
                    Id = offer.Id,
                    Title = offer.Title,
                    Description = offer.Description,
                    Cost = offer.Cost,
                    OldCost = offer.OldCost,
                    Currency = offer.Currency,
                    StartTime = offer.StartTime,
                    EndTime = offer.EndTime,
                    BackgroundName = offer.BackgroundName,
                    IsActive = offer.IsActive,
                    Rewards = new List<GromCore.Laser.Logic.CustomRewardData>()
                };
                foreach (var reward in offer.Rewards)
                {
                    bridgeOffer.Rewards.Add(new GromCore.Laser.Logic.CustomRewardData
                    {
                        Type = reward.Type,
                        Count = reward.Count,
                        DataId = reward.DataId
                    });
                }
                bridgeOffers.Add(bridgeOffer);
            }
            GromCore.Laser.Logic.OfferBridge.SetCustomOffers(bridgeOffers);
            
            _offerUpdateTimer = new Timer(CheckAndApplyOffers, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
            Console.WriteLine($"[OffersManager] Инициализирован. Загружено {_customOffers.Count} акций");
        }

        private static void CheckAndApplyOffers(object state)
        {
            try
            {
                lock (_lock)
                {
                    var now = DateTime.Now;
                    bool needSave = false;
                    foreach (var offer in _customOffers)
                    {
                        if (offer.IsActive && offer.EndTime <= now)
                        {
                            offer.IsActive = false;
                            needSave = true;
                            Console.WriteLine($"[OffersManager] Акция '{offer.Title}' истекла");
                        }
                    }
                    if (needSave) SaveOffers();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffersManager] Ошибка проверки акций: {ex.Message}");
            }
        }

        public static void LoadOffers()
        {
            try
            {
                if (File.Exists(OffersFile))
                {
                    string json = File.ReadAllText(OffersFile);
                    _customOffers = JsonConvert.DeserializeObject<List<CustomOffer>>(json) ?? new List<CustomOffer>();
                    Console.WriteLine($"[OffersManager] Загружено {_customOffers.Count} акций из {OffersFile}");
                }
                else
                {
                    _customOffers = new List<CustomOffer>();
                    var sampleOffer = new CustomOffer
                    {
                        Title = "🎁 ПРИМЕР АКЦИИ",
                        Description = "Это пример акции. Вы можете её удалить.",
                        Cost = 99,
                        OldCost = 199,
                        Currency = 0,
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now.AddDays(7),
                        BackgroundName = "offer_bgr_legendary",
                        CreatedBy = "system"
                    };
                    sampleOffer.Rewards.Add(new CustomOfferReward { Type = "gems", Count = 100 });
                    sampleOffer.Rewards.Add(new CustomOfferReward { Type = "coins", Count = 5000 });
                    _customOffers.Add(sampleOffer);
                    SaveOffers();
                    Console.WriteLine("[OffersManager] Создан пример акции");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffersManager] Ошибка загрузки акций: {ex.Message}");
                _customOffers = new List<CustomOffer>();
            }
        }

        public static void SaveOffers()
        {
            try
            {
                lock (_lock)
                {
                    string json = JsonConvert.SerializeObject(_customOffers, Formatting.Indented);
                    File.WriteAllText(OffersFile, json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffersManager] Ошибка сохранения акций: {ex.Message}");
            }
        }

        public static bool CreateOffer(CustomOffer offer)
        {
            lock (_lock)
            {
                if (_customOffers.Any(o => o.Id == offer.Id))
                    return false;
                
                offer.IsActive = true;
                _customOffers.Add(offer);
                SaveOffers();
                Console.WriteLine($"[OffersManager] Создана акция: {offer.Title}, активна до {offer.EndTime}");
                return true;
            }
        }

        public static bool UpdateOffer(string id, CustomOffer updatedOffer)
        {
            lock (_lock)
            {
                var index = _customOffers.FindIndex(o => o.Id == id);
                if (index == -1) return false;
                
                updatedOffer.Id = id;
                updatedOffer.CreatedAt = _customOffers[index].CreatedAt;
                _customOffers[index] = updatedOffer;
                SaveOffers();
                ApplyToAllOnlinePlayers();
                return true;
            }
        }

        public static bool DeleteOffer(string id)
        {
            lock (_lock)
            {
                var removed = _customOffers.RemoveAll(o => o.Id == id) > 0;
                if (removed)
                {
                    SaveOffers();
                    ApplyToAllOnlinePlayers();
                }
                return removed;
            }
        }

        public static CustomOffer GetOffer(string id)
        {
            lock (_lock)
            {
                return _customOffers.FirstOrDefault(o => o.Id == id);
            }
        }

        public static List<CustomOffer> GetAllOffers()
        {
            lock (_lock)
            {
                return _customOffers.ToList();
            }
        }

        public static List<CustomOffer> GetActiveOffers()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                return _customOffers.Where(o => o.IsActive && o.StartTime <= now && o.EndTime >= now).ToList();
            }
        }

        public static void ApplyToAllOnlinePlayers()
        {
            try
            {
                Console.WriteLine($"[OffersManager] Обновление акций для онлайн игроков");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffersManager] Ошибка обновления акций: {ex.Message}");
            }
        }

        public static void ApplyOffersToPlayer(ClientHome home)
        {
            if (home == null) return;
            
            try
            {
                var now = DateTime.UtcNow;
                var activeOffers = _customOffers.Where(o => o.IsActive && o.StartTime <= now && o.EndTime >= now).ToList();
                
                home.OfferBundles.RemoveAll(b => b.Claim != null && b.Claim.StartsWith("custom_offer_"));
                
                foreach (var offer in activeOffers)
                {
                    var offerBundle = ConvertToOfferBundle(offer);
                    if (offerBundle != null)
                    {
                        // Telegram offers are one-time offers. Do not recreate an offer that
                        // the player has already claimed when the shop is refreshed (for
                        // example, after returning from a battle).
                        if (home.OffersClaimed?.Contains(offerBundle.Claim) == true)
                            continue;

                        home.OfferBundles.Add(offerBundle);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OffersManager] Ошибка: {ex.Message}");
            }
        }

        private static OfferBundle ConvertToOfferBundle(CustomOffer offer)
        {
            var bundle = new OfferBundle();
            bundle.IsDailyDeals = false;
            bundle.IsTrue = true;
            bundle.StartTime = offer.StartTime;
            bundle.EndTime = offer.EndTime;
            bundle.Cost = offer.Cost;
            bundle.OldCost = offer.OldCost;
            bundle.Currency = offer.Currency;
            bundle.Claim = $"custom_offer_{offer.Id}";
            bundle.Title = offer.Title;
            bundle.BackgroundExportName = offer.BackgroundName;
            bundle.IsTID = 0;
            bundle.OfferType = 0;
            bundle.OneTimeOffer = true;
            bundle.LoadOnStartup = false;
            bundle.Processed = false;
            bundle.TypeBenefit = 0;
            bundle.Benefit = 0;
            bundle.ShopPanelLayoutClass = 0;
            bundle.ShopPanelLayoutType = 0;
            bundle.ShopStyleSetClass = 0;
            bundle.ShopStyleSetType = 0;
            bundle.specialOffer = false;
            bundle.MultiCount = 1;
            bundle.Purchased = false;
            bundle.Items = new List<Offer>();
            
            foreach (var reward in offer.Rewards)
            {
                var shopItem = GetShopItemFromType(reward.Type);
                int itemDataId = reward.DataId;
                int skinDataId = 0;

                // Vanity rewards are consumed through Offer.SkinDataId by the
                // purchase code. The bot accepts full global IDs (for example,
                // 29000001), while Offer expects only the instance part (1).
                if (shopItem == ShopItem.Skin ||
                    shopItem == ShopItem.Emote ||
                    shopItem == ShopItem.Spray ||
                    shopItem == ShopItem.PlayerTitle ||
                    shopItem == ShopItem.PlayerThumbnail)
                {
                    itemDataId = 0;
                    skinDataId = GlobalId.GetInstanceId(reward.DataId);
                }

                // A skin offer must reference the character that owns the skin.
                // Using character 0 for every skin only works for Shelly skins and
                // makes the client treat skins for other brawlers as already owned.
                if (shopItem == ShopItem.Skin)
                {
                    SkinData skinData = DataTables.Get(DataType.Skin).GetData<SkinData>(skinDataId);
                    SkinConfData skinConf = skinData?.GetConf();
                    CharacterData characterData = skinConf == null
                        ? null
                        : DataTables.GetCharacterByName(skinConf.Character);

                    if (characterData != null)
                    {
                        itemDataId = characterData.GetGlobalId();
                    }
                }

                var offerItem = new Offer(shopItem, reward.Count, itemDataId, skinDataId);
                bundle.Items.Add(offerItem);
            }
            
            return bundle;
        }

        private static ShopItem GetShopItemFromType(string type)
        {
            return type switch
            {
                "gems" => ShopItem.Gems,
                "coins" => ShopItem.Coin,
                "pp" => ShopItem.PowerPoint,
                "bling" => ShopItem.Bling,
                "sp" => ShopItem.StarPoints,
                "recruit" => ShopItem.RecruitToken,
                "skin" => ShopItem.Skin,
                "brawler" => ShopItem.GuaranteedHero,
                "emote" => ShopItem.Emote,
                "spray" => ShopItem.Spray,
                "title" => ShopItem.PlayerTitle,
                "icon" => ShopItem.PlayerThumbnail,
                "chaos" => ShopItem.ChaosDrop,
                "ultra" => ShopItem.UltraChaosDrop,
                _ => ShopItem.Coin
            };
        }
    }

    // ==================== VIP КОНФИГ ====================
    
    public static class VipConfig
    {
        public static Dictionary<int, int> BonusTrophies = new Dictionary<int, int>
        {
            {1, 15}, {2, 30}, {3, 45}, {4, 55}, {5, 65},
            {6, 75}, {7, 85}, {8, 90}, {9, 130}, {10, 170}
        };
        
        public static Dictionary<int, string> VipNames = new Dictionary<int, string>
        {
            {1, "🥉 Бронзовый VIP"},
            {2, "🥈 Серебряный VIP"},
            {3, "🥇 Золотой VIP"},
            {4, "💎 Бриллиантовый VIP"},
            {5, "👑 Платиновый VIP"},
            {6, "⭐ Элитный VIP"},
            {7, "🔥 Мастер VIP"},
            {8, "💀 Легендарный VIP"},
            {9, "✨ Мифический VIP"},
            {10, "🏆 Бог VIP"}
        };
    }

    // ==================== TELEGRAM БОТ ====================

    public static class TelegramBot
    {
        private static ITelegramBotClient BotClient;
        private static string Token =>
            GromCore.Laser.Server.Settings.Configuration.Instance.BotToken;
        private static List<long> adminIds = new()
        {
            794811607,
        };
        private static readonly string BotWhitelistFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "telegram_whitelist.json");
        private static readonly string VipBonusConfigFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "vip_bonus_trophies.json");
        private static readonly string ThemeConfigFile = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "theme_config.json");
        private static readonly object BotWhitelistLock = new object();
        private static readonly object VipBonusConfigLock = new object();
        private static readonly object ThemeConfigLock = new object();
        private static HashSet<long> botWhitelist = new HashSet<long>();

        // Updates from Telegram can be processed concurrently.  Concurrent
        // dictionaries prevent a second message/callback from corrupting an
        // in-progress offer wizard.
        private static readonly ConcurrentDictionary<long, string> _userState = new();
        private static readonly ConcurrentDictionary<long, CustomOffer> _pendingOffer = new();
        private static readonly SemaphoreSlim BackupSemaphore = new SemaphoreSlim(1, 1);

        private static void ClearOfferState(long chatId)
        {
            _pendingOffer.TryRemove(chatId, out _);
            _userState.TryRemove(chatId, out _);
        }

        private static bool TryGetPendingOffer(long chatId, out CustomOffer offer)
        {
            if (_pendingOffer.TryGetValue(chatId, out offer)) return true;
            ClearOfferState(chatId);
            return false;
        }

        private sealed class BotCommandInfo
        {
            public string Name { get; init; }
            public string Category { get; init; }
            public string Usage { get; init; }
            public string Description { get; init; }

            public bool RequiresAdmin => Name switch
            {
                "start" or "help" or "commands" or "cancel" => false,
                _ => true
            };

            public string Access => RequiresAdmin
                ? "Только администратор или оператор из whitelist"
                : "Доступно всем пользователям";
        }

        private static readonly HashSet<string> PublicBotCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "start", "help", "commands", "cancel"
        };

        private static readonly HashSet<string> AdminCommandAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            "refreshvips", "updatevip", "updatevips", "vipbonus", "setvipbonus",
            "whitelist", "changetheme", "theme"
        };

        private static bool IsAdminCommandName(string command)
        {
            if (PublicBotCommands.Contains(command)) return false;
            return AdminCommandAliases.Contains(command) ||
                   AdminCommandCatalog.Any(item => item.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBotOwner(long telegramId) => adminIds.Contains(telegramId);

        private static bool IsBotOperator(long telegramId)
        {
            lock (BotWhitelistLock)
                return IsBotOwner(telegramId) || botWhitelist.Contains(telegramId);
        }

        private static void LoadBotWhitelist()
        {
            lock (BotWhitelistLock)
            {
                try
                {
                    if (!File.Exists(BotWhitelistFile))
                    {
                        botWhitelist = new HashSet<long>();
                        return;
                    }

                    var values = JsonConvert.DeserializeObject<List<long>>(
                        File.ReadAllText(BotWhitelistFile));
                    botWhitelist = values == null
                        ? new HashSet<long>()
                        : new HashSet<long>(values.Where(id => id > 0));
                }
                catch (Exception ex)
                {
                    botWhitelist = new HashSet<long>();
                    Console.WriteLine($"[TelegramBot] Не удалось загрузить whitelist: {ex.Message}");
                }
            }
        }

        private static void SaveBotWhitelist()
        {
            lock (BotWhitelistLock)
            {
                try
                {
                    File.WriteAllText(BotWhitelistFile,
                        JsonConvert.SerializeObject(botWhitelist.OrderBy(id => id).ToList(), Formatting.Indented));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Не удалось сохранить whitelist: {ex.Message}");
                }
            }
        }

        private static void SyncVipBonusConfig()
        {
            // ClientAvatar uses the Logic namespace configuration at runtime;
            // keep the bot's editable table and the gameplay table identical.
            foreach (var entry in VipConfig.BonusTrophies)
                GromCore.Laser.Logic.Avatar.VipConfig.BonusTrophies[entry.Key] = entry.Value;
        }

        private static void LoadVipBonusConfig()
        {
            lock (VipBonusConfigLock)
            {
                try
                {
                    if (File.Exists(VipBonusConfigFile))
                    {
                        var stored = JsonConvert.DeserializeObject<Dictionary<int, int>>(
                            File.ReadAllText(VipBonusConfigFile));
                        if (stored != null)
                        {
                            foreach (var entry in stored.Where(x => x.Key >= 1 && x.Key <= 10 && x.Value >= 0))
                                VipConfig.BonusTrophies[entry.Key] = entry.Value;
                        }
                    }
                    SyncVipBonusConfig();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Не удалось загрузить VIP-бонусы: {ex.Message}");
                    SyncVipBonusConfig();
                }
            }
        }

        private static void SaveVipBonusConfig()
        {
            lock (VipBonusConfigLock)
            {
                File.WriteAllText(VipBonusConfigFile,
                    JsonConvert.SerializeObject(VipConfig.BonusTrophies.OrderBy(x => x.Key)
                        .ToDictionary(x => x.Key, x => x.Value), Formatting.Indented));
            }
        }

        private static void LoadThemeConfig()
        {
            lock (ThemeConfigLock)
            {
                try
                {
                    if (!File.Exists(ThemeConfigFile)) return;

                    var config = JsonConvert.DeserializeObject<Dictionary<string, int>>(
                        File.ReadAllText(ThemeConfigFile));
                    if (config != null && config.TryGetValue("defaultThemeId", out int themeId) &&
                        themeId >= 0 && themeId <= 10000)
                    {
                        GromCore.Laser.Logic.GeneralStaticLogic.DefaultThemeId = themeId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Не удалось загрузить фон: {ex.Message}");
                }
            }
        }

        private static bool SaveThemeConfig()
        {
            lock (ThemeConfigLock)
            {
                try
                {
                    File.WriteAllText(ThemeConfigFile, JsonConvert.SerializeObject(
                        new Dictionary<string, int>
                        {
                            ["defaultThemeId"] = GromCore.Laser.Logic.GeneralStaticLogic.DefaultThemeId
                        }, Formatting.Indented));
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TelegramBot] Не удалось сохранить фон: {ex}");
                    return false;
                }
            }
        }

        // Единственный каталог команд для кнопок. Новая команда должна быть
        // добавлена сюда одновременно с case в HandleMessage — так меню не
        // расходится с реально работающими командами.
        private static readonly BotCommandInfo[] AdminCommandCatalog =
        {
            new() { Name="crtemp", Category="🎁 Акции", Usage="/crtemp", Description="создать акцию пошагово" },
            new() { Name="listtemp", Category="🎁 Акции", Usage="/listtemp", Description="список акций" },
            new() { Name="deltemp", Category="🎁 Акции", Usage="/deltemp ID", Description="удалить акцию" },
            new() { Name="offertemp", Category="🎁 Акции", Usage="/offertemp ID", Description="информация об акции" },
            new() { Name="edittemp", Category="🎁 Акции", Usage="/edittemp ID ПОЛЕ ЗНАЧЕНИЕ", Description="изменить акцию" },
            new() { Name="activatetemp", Category="🎁 Акции", Usage="/activatetemp ID", Description="включить акцию" },
            new() { Name="deactivatetemp", Category="🎁 Акции", Usage="/deactivatetemp ID", Description="выключить акцию" },
            new() { Name="refreshoffers", Category="🎁 Акции", Usage="/refreshoffers", Description="обновить акции у игроков" },
            new() { Name="crpromo", Category="🎟 Промокоды", Usage="/crpromo КОД ЛИМИТ НАГРАДЫ", Description="создать промокод" },
            new() { Name="listpromo", Category="🎟 Промокоды", Usage="/listpromo", Description="список промокодов" },
            new() { Name="promoinfo", Category="🎟 Промокоды", Usage="/promoinfo КОД", Description="информация о промокоде" },
            new() { Name="promovalidate", Category="🎟 Промокоды", Usage="/promovalidate КОД", Description="проверить и исправить награды" },
            new() { Name="delpromo", Category="🎟 Промокоды", Usage="/delpromo КОД", Description="удалить промокод" },
            new() { Name="setvip", Category="👤 Игроки", Usage="/setvip TAG УРОВЕНЬ [ДНИ]", Description="выдать VIP" },
            new() { Name="vipinfo", Category="👤 Игроки", Usage="/vipinfo TAG", Description="информация о VIP" },
            new() { Name="extendvip", Category="👤 Игроки", Usage="/extendvip TAG ДНИ", Description="продлить VIP" },
            new() { Name="upgradevip", Category="👤 Игроки", Usage="/upgradevip TAG", Description="повысить VIP" },
            new() { Name="refreshvip", Category="👤 Игроки", Usage="/refreshvip [TAG]", Description="обновить бонусы уже выданных VIP" },
            new() { Name="refreshvips", Category="👤 Игроки", Usage="/refreshvips [TAG]", Description="алиас обновления VIP-бонусов" },
            new() { Name="updatevip", Category="👤 Игроки", Usage="/updatevip [TAG]", Description="алиас обновления VIP-бонусов" },
            new() { Name="updatevips", Category="👤 Игроки", Usage="/updatevips [TAG]", Description="алиас обновления VIP-бонусов" },
            new() { Name="addgems", Category="💰 Ресурсы", Usage="/addgems TAG КОЛИЧЕСТВО", Description="выдать гемы" },
            new() { Name="addcoins", Category="💰 Ресурсы", Usage="/addcoins TAG КОЛИЧЕСТВО", Description="выдать монеты" },
            new() { Name="addpowerpoints", Category="💰 Ресурсы", Usage="/addpowerpoints TAG КОЛИЧЕСТВО", Description="выдать очки силы" },
            new() { Name="addbling", Category="💰 Ресурсы", Usage="/addbling TAG КОЛИЧЕСТВО", Description="выдать блинги" },
            new() { Name="addstarpoints", Category="💰 Ресурсы", Usage="/addstarpoints TAG КОЛИЧЕСТВО", Description="выдать старпоинты" },
            new() { Name="remgems", Category="💰 Ресурсы", Usage="/remgems TAG КОЛИЧЕСТВО", Description="забрать гемы" },
            new() { Name="remcoins", Category="💰 Ресурсы", Usage="/remcoins TAG КОЛИЧЕСТВО", Description="забрать монеты" },
            new() { Name="addpass", Category="🎫 Пропуск", Usage="/addpass TAG", Description="выдать Brawl Pass" },
            new() { Name="addpassplus", Category="🎫 Пропуск", Usage="/addpassplus TAG", Description="выдать Pass Plus" },
            new() { Name="removepass", Category="🎫 Пропуск", Usage="/removepass TAG", Description="забрать пропуск" },
            new() { Name="addbrawler", Category="🎨 Предметы", Usage="/addbrawler TAG ID", Description="выдать бойца" },
            new() { Name="removebrawler", Category="🎨 Предметы", Usage="/removebrawler TAG ID", Description="забрать бойца" },
            new() { Name="addallbrawlers", Category="🎨 Предметы", Usage="/addallbrawlers TAG", Description="выдать всех бойцов" },
            new() { Name="addskin", Category="🎨 Предметы", Usage="/addskin TAG ID", Description="выдать скин" },
            new() { Name="removeskin", Category="🎨 Предметы", Usage="/removeskin TAG ID", Description="забрать скин" },
            new() { Name="kybki", Category="👤 Игроки", Usage="/kybki TAG ID КОЛИЧЕСТВО", Description="изменить кубки бойца" },
            new() { Name="resetbrawler", Category="👤 Игроки", Usage="/resetbrawler TAG ID", Description="сбросить бойца" },
            new() { Name="seasonreset", Category="🖥 Сервер", Usage="/seasonreset TAG|all", Description="сбросить сезон" },
            new() { Name="ban", Category="🛡 Модерация", Usage="/ban TAG ДНИ [ПРИЧИНА]", Description="заблокировать игрока" },
            new() { Name="unban", Category="🛡 Модерация", Usage="/unban TAG", Description="снять бан" },
            new() { Name="mute", Category="🛡 Модерация", Usage="/mute TAG ДНИ", Description="выдать мут" },
            new() { Name="unmute", Category="🛡 Модерация", Usage="/unmute TAG", Description="снять мут" },
            new() { Name="sdban", Category="🛡 Модерация", Usage="/sdban TAG ДНИ", Description="заблокировать Starr Drop" },
            new() { Name="sdunban", Category="🛡 Модерация", Usage="/sdunban TAG", Description="снять блокировку Starr Drop" },
            new() { Name="setname", Category="👤 Игроки", Usage="/setname TAG ИМЯ", Description="изменить имя" },
            new() { Name="streak", Category="👤 Игроки", Usage="/streak TAG ЗНАЧЕНИЕ", Description="изменить серию" },
            new() { Name="setpassword", Category="👤 Игроки", Usage="/setpassword TAG ПАРОЛЬ", Description="установить пароль" },
            new() { Name="changepassword", Category="👤 Игроки", Usage="/changepassword TAG ПАРОЛЬ", Description="сменить пароль" },
            new() { Name="kick", Category="👤 Игроки", Usage="/kick TAG", Description="отключить игрока" },
            new() { Name="online", Category="🖥 Сервер", Usage="/online", Description="онлайн игроков" },
            new() { Name="debuglist", Category="🧪 Отладка", Usage="/debuglist", Description="список debug-аккаунтов" },
            new() { Name="debugaccounts", Category="🧪 Отладка", Usage="/debugaccounts", Description="диагностика аккаунтов" },
            new() { Name="debug", Category="🧪 Отладка", Usage="/debug TAG ПАРАМЕТР on|off", Description="изменить debug-режим" },
            new() { Name="undebug", Category="🧪 Отладка", Usage="/undebug TAG", Description="снять debug-режим" },
            new() { Name="maps", Category="🖥 Сервер", Usage="/maps [СЛОТ] [СТРАНИЦА]", Description="список карт" },
            new() { Name="setmap", Category="🖥 Сервер", Usage="/setmap СЛОТ ID", Description="сменить карту" },
            new() { Name="themes", Category="🖼 Оформление", Usage="/themes", Description="список доступных фонов" },
            new() { Name="settheme", Category="🖼 Оформление", Usage="/settheme ID [TAG]", Description="сменить фон" },
            new() { Name="setbackground", Category="🖼 Оформление", Usage="/setbackground ID [TAG]", Description="алиас смены фона" },
            new() { Name="changetheme", Category="🖼 Оформление", Usage="/changetheme ID [TAG]", Description="алиас смены фона" },
            new() { Name="theme", Category="🖼 Оформление", Usage="/theme ID [TAG]", Description="алиас смены фона" },
            new() { Name="events", Category="🖥 Сервер", Usage="/events", Description="статус событий" },
            new() { Name="event", Category="🖥 Сервер", Usage="/event ID on|off", Description="включить или выключить событие" },
            new() { Name="maintenance", Category="🖥 Сервер", Usage="/maintenance [on|off|list]", Description="технический режим" },
            new() { Name="tech", Category="🖥 Сервер", Usage="/tech [on|off|list]", Description="алиас технического режима" },
            new() { Name="backup", Category="🖥 Сервер", Usage="/backup", Description="создать резервную копию" },
            new() { Name="start", Category="ℹ️ Система", Usage="/start", Description="открыть главное меню" },
            new() { Name="admin", Category="ℹ️ Система", Usage="/admin", Description="открыть админ-панель" },
            new() { Name="help", Category="ℹ️ Система", Usage="/help", Description="показать справку" },
            new() { Name="commands", Category="ℹ️ Система", Usage="/commands", Description="показать команды" },
            new() { Name="cancel", Category="ℹ️ Система", Usage="/cancel", Description="отменить активный мастер" }
        };

        private static IEnumerable<BotCommandInfo> GetAdminCommandCatalog()
        {
            return AdminCommandCatalog.Concat(new[]
            {
                new BotCommandInfo
                {
                    Name = "whitelist",
                    Category = "Access",
                    Usage = "/whitelist [list|add|remove] TELEGRAM_ID",
                    Description = "manage Telegram bot access"
                },
                new BotCommandInfo
                {
                    Name = "vipbonus",
                    Category = "VIP",
                    Usage = "/vipbonus [LEVEL BONUS]",
                    Description = "view or change VIP trophy bonuses"
                },
                new BotCommandInfo
                {
                    Name = "setvipbonus",
                    Category = "VIP",
                    Usage = "/setvipbonus LEVEL BONUS",
                    Description = "change the trophy bonus for a VIP level"
                }
            });
        }

        public static void Start()
        {
            LoadBotWhitelist();
            LoadVipBonusConfig();
            LoadThemeConfig();
            BotClient = new TelegramBotClient(Token);
            var receiverOptions = new ReceiverOptions();
            BotClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: CancellationToken.None
            );
            
            OffersManager.Initialize();
            Console.WriteLine("[TelegramBot] Бот запущен.");
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken ct)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message when update.Message?.Text != null:
                        await HandleMessage(update.Message, ct);
                        break;
                    case UpdateType.CallbackQuery when update.CallbackQuery != null:
                        await HandleCallbackQuery(update.CallbackQuery, ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в HandleUpdateAsync: {ex.Message}");
            }
        }

        private static async Task HandleMessage(Telegram.Bot.Types.Message message, CancellationToken ct)
        {
            long chatId = message.Chat.Id;
            string text = message.Text?.Trim() ?? string.Empty;
            bool isAdmin = message.From != null && IsBotOperator(message.From.Id);

            // Global controls are available even while an interactive wizard is open.
            // This prevents a mistyped value from leaving the administrator stuck in
            // a half-created offer forever.
            if (text.Equals("/cancel", StringComparison.OrdinalIgnoreCase))
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "✅ Текущий мастер отменён. Данные не сохранены.", cancellationToken: ct);
                return;
            }

            if (text.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("/commands", StringComparison.OrdinalIgnoreCase))
            {
                if (isAdmin) await ShowCommandPage(chatId, 0, 0, ct);
                else await ShowUserHelp(chatId, ct);
                return;
            }

            if (_userState.TryGetValue(chatId, out string state))
            {
                if (state == "waiting_offer_rewards")
                {
                    await ProcessOfferRewardsInput(message, ct);
                    return;
                }
                if (state == "waiting_offer_title")
                {
                    await HandleCreateOfferTitle(message, chatId, ct);
                    return;
                }
                if (state == "waiting_offer_desc")
                {
                    await HandleCreateOfferDesc(message, chatId, ct);
                    return;
                }
                if (state == "waiting_offer_cost")
                {
                    await HandleCreateOfferCost(message, chatId, ct);
                    return;
                }
                if (state == "waiting_offer_duration")
                {
                    await HandleCreateOfferDuration(message, chatId, ct);
                    return;
                }
                if (state == "waiting_offer_background")
                {
                    await HandleCreateOfferBackground(message, chatId, ct);
                    return;
                }
            }

            if (text.ToLower() == "/start")
            {
                await ShowMainMenu(chatId, message.From?.Id ?? 0, ct);
                return;
            }

            if (text.ToLower() == "/admin")
            {
                if (!isAdmin)
                {
                    await BotClient.SendMessage(chatId, "❌ У вас нет доступа к админ-командам.", cancellationToken: ct);
                    return;
                }
                await ShowAdminMenu(chatId, ct);
                return;
            }

            if (!text.StartsWith("/"))
            {
                await BotClient.SendMessage(chatId,
                    "ℹ️ Я принимаю команды. Начните с /start или откройте список возможностей через /help.",
                    cancellationToken: ct);
                return;
            }

            string[] args = text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (args.Length < 1)
            {
                await BotClient.SendMessage(chatId, "❌ Неверная команда", cancellationToken: ct);
                return;
            }

            string command = args[0].ToLowerInvariant();
            int commandMentionIndex = command.IndexOf('@');
            if (commandMentionIndex > 0)
                command = command[..commandMentionIndex];

            // Telegram clients may send commands as /help@bot_name in groups.
            // Handle the normalized system commands before the admin router.
            if (command is "help" or "commands")
            {
                if (isAdmin) await ShowCommandPage(chatId, 0, 0, ct);
                else await ShowUserHelp(chatId, ct);
                return;
            }
            if (command == "start")
            {
                await ShowMainMenu(chatId, message.From?.Id ?? 0, ct);
                return;
            }
            if (command == "admin")
            {
                if (!isAdmin)
                {
                    await BotClient.SendMessage(chatId, "❌ У вас нет доступа к админ-командам.", cancellationToken: ct);
                    return;
                }
                await ShowAdminMenu(chatId, ct);
                return;
            }
            if (command == "cancel")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "✅ Текущий мастер отменён. Данные не сохранены.", cancellationToken: ct);
                return;
            }

            bool isAdminCommand = IsAdminCommandName(command);

            if (isAdminCommand && !isAdmin)
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }

            try
            {
                switch (command)
                {
                    // АКЦИИ
                    case "crtemp":
                        await HandleCreateOfferInteractive(message, chatId, ct);
                        break;
                    case "listtemp":
                        await HandleListOffers(message, chatId, ct);
                        break;
                    case "deltemp":
                        await HandleDeleteOffer(message, chatId, ct, args);
                        break;
                    case "offertemp":
                        await HandleOfferInfo(message, chatId, ct, args);
                        break;
                    case "edittemp":
                        await HandleEditOffer(message, chatId, ct, args);
                        break;
                    case "activatetemp":
                        await HandleActivateOffer(message, chatId, ct, args);
                        break;
                    case "deactivatetemp":
                        await HandleDeactivateOffer(message, chatId, ct, args);
                        break;
                    case "refreshoffers":
                        await HandleRefreshOffers(message, chatId, ct);
                        break;

                    // СЕРВЕРНЫЕ ИВЕНТЫ И ТЕХПЕРЕРЫВ
                    case "event":
                    case "events":
                        await HandleServerEvents(chatId, ct, args);
                        break;
                    case "maintenance":
                    case "tech":
                        await HandleMaintenance(message, chatId, ct, args);
                        break;
                    case "whitelist":
                        await HandleBotWhitelist(message, chatId, ct, args);
                        break;

                    // КАРТЫ
                    case "maps":
                        await HandleListMaps(chatId, ct, args);
                        break;
                    case "setmap":
                        await HandleSetMap(chatId, ct, args);
                        break;
                    case "themes":
                        await HandleListThemes(chatId, ct);
                        break;
                    case "settheme":
                    case "setbackground":
                    case "changetheme":
                    case "theme":
                        await HandleSetTheme(chatId, ct, args);
                        break;
                    
                    // ПРОМОКОДЫ
                    case "crpromo":
                        await HandleCreatePromo(message, chatId, ct);
                        break;
                    case "listpromo":
                        await HandleListPromoCodes(message, chatId, ct);
                        break;
                    case "delpromo":
                        await HandleDeletePromo(message, chatId, ct);
                        break;
                    case "promoinfo":
                        await HandlePromoInfo(message, chatId, ct);
                        break;
                    case "promovalidate":
                        await HandlePromoValidate(message, chatId, ct);
                        break;
                    
                    // VIP
                    case "setvip":
                        await HandleSetVip(message, chatId, ct, args);
                        break;
                    case "vipinfo":
                        await HandleVipInfo(message, chatId, ct, args);
                        break;
                    case "extendvip":
                        await HandleExtendVip(message, chatId, ct, args);
                        break;
                    case "upgradevip":
                        await HandleUpgradeVip(message, chatId, ct, args);
                        break;
                    case "refreshvip":
                    case "refreshvips":
                    case "updatevip":
                    case "updatevips":
                        await HandleRefreshVip(message, chatId, ct, args);
                        break;
                    case "vipbonus":
                    case "setvipbonus":
                        await HandleVipBonus(message, chatId, ct, args, command == "setvipbonus");
                        break;
                    
                    // РЕСУРСЫ
                    case "addgems":
                        await HandleAddGems(message, chatId, ct, args);
                        break;
                    case "addcoins":
                        await HandleAddCoins(message, chatId, ct, args);
                        break;
                    case "addpowerpoints":
                        await HandleAddPowerPoints(message, chatId, ct, args);
                        break;
                    case "addbling":
                        await HandleAddBling(message, chatId, ct, args);
                        break;
                    case "addstarpoints":
                        await HandleAddStarPoints(message, chatId, ct, args);
                        break;
                    case "remgems":
                        await HandleRemGems(message, chatId, ct, args);
                        break;
                    case "remcoins":
                        await HandleRemCoins(message, chatId, ct, args);
                        break;
                    
                    // BRAWL PASS
                    case "addpass":
                        await HandleAddPass(message, chatId, ct, args);
                        break;
                    case "addpassplus":
                        await HandleAddPassPlus(message, chatId, ct, args);
                        break;
                    case "removepass":
                        await HandleRemovePass(message, chatId, ct, args);
                        break;
                    
                    // БОЙЦЫ И СКИНЫ
                    case "addbrawler":
                        await HandleAddBrawler(message, chatId, ct, args);
                        break;
                    case "removebrawler":
                        await HandleRemoveBrawler(message, chatId, ct, args);
                        break;
                    case "addallbrawlers":
                        await HandleAddAllBrawlers(message, chatId, ct, args);
                        break;
                    case "addskin":
                        await HandleAddSkin(message, chatId, ct, args);
                        break;
                    case "removeskin":
                        await HandleRemoveSkin(message, chatId, ct, args);
                        break;
                    
                    // ТРОФЕИ
                    case "kybki":
                        await HandleKybki(message, chatId, ct, args);
                        break;
                    case "resetbrawler":
                        await HandleResetBrawler(message, chatId, ct, args);
                        break;
                    case "seasonreset":
                        await HandleSeasonReset(message, chatId, ct, args);
                        break;
                    
                    // БАНЫ И МУТЫ
                    case "ban":
                        await HandleBan(message, chatId, ct, args);
                        break;
                    case "unban":
                        await HandleUnban(message, chatId, ct, args);
                        break;
                    case "mute":
                        await HandleMute(message, chatId, ct, args);
                        break;
                    case "unmute":
                        await HandleUnmute(message, chatId, ct, args);
                        break;
                    case "sdban":
                        await HandleSdBan(message, chatId, ct, args);
                        break;
                    case "sdunban":
                        await HandleSdUnban(message, chatId, ct, args);
                        break;
                    
                    // ПРОЧЕЕ
                    case "setname":
                        await HandleSetName(message, chatId, ct, args);
                        break;
                    case "streak":
                        await HandleStreak(message, chatId, ct, args);
                        break;
                    case "setpassword":
                    case "changepassword":
                        await HandleSetPassword(message, chatId, ct, args);
                        break;
                    case "kick":
                        await HandleKick(message, chatId, ct, args);
                        break;
                    case "online":
                        await HandleOnline(chatId, ct, args);
                        break;
                    case "debuglist":
                    case "debugaccounts":
                        await HandleDebugAccountsList(chatId, ct, args);
                        break;
                    case "debug":
                        await HandleDebugAccount(chatId, ct, args);
                        break;
                    case "undebug":
                        await HandleRemoveDebugAccount(chatId, ct, args);
                        break;
                    case "backup":
                        await HandleBackup(chatId, ct);
                        break;
                    
                    default:
                        await BotClient.SendMessage(chatId, "❌ Неизвестная команда. Используйте /help или /commands.", cancellationToken: ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в команде '{command}': {ex}");
                await BotClient.SendMessage(chatId,
                    "❌ Не удалось выполнить команду. Проверьте формат данных или попробуйте ещё раз. " +
                    "Подробности записаны в журнал сервера.", cancellationToken: ct);
            }
        }

        // ==================== КАРТЫ ====================

        private const int MapsPerPage = 20;

        private static async Task HandleListMaps(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length == 1)
            {
                var activeEvents = Events.GetEvents()
                    .Where(eventData => eventData?.Location != null && !eventData.IsChallengeSlot)
                    .OrderBy(eventData => eventData.Slot)
                    .ToArray();

                if (activeEvents.Length == 0)
                {
                    await BotClient.SendMessage(chatId, "❌ Активные игровые карты пока не загружены.", cancellationToken: ct);
                    return;
                }

                var response = new StringBuilder("🗺 Активные карты:\n\n");
                foreach (var eventData in activeEvents)
                {
                    response.AppendLine(
                        $"Слот {eventData.Slot}: {eventData.Location.GetName()} " +
                        $"[{eventData.Location.GameModeVariation}], ID {eventData.LocationId}");
                }

                response.AppendLine();
                response.AppendLine("Список карт режима: /maps <слот> [страница]");
                response.Append("Смена карты: /setmap <слот> <ID карты>");

                await BotClient.SendMessage(chatId, response.ToString(), cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[1], out int slot))
            {
                await BotClient.SendMessage(chatId, "❌ Слот должен быть числом. Пример: /maps 1", cancellationToken: ct);
                return;
            }

            int page = 1;
            if (args.Length >= 3 && (!int.TryParse(args[2], out page) || page < 1))
            {
                await BotClient.SendMessage(chatId, "❌ Страница должна быть положительным числом.", cancellationToken: ct);
                return;
            }

            var currentEvent = Events.GetEvent(slot);
            if (currentEvent?.Location == null || currentEvent.IsChallengeSlot)
            {
                await BotClient.SendMessage(chatId, $"❌ Активный игровой слот {slot} не найден.", cancellationToken: ct);
                return;
            }

            LocationData[] maps = Events.GetAvailableMaps(slot);
            if (maps.Length == 0)
            {
                await BotClient.SendMessage(
                    chatId,
                    $"❌ Для режима {currentEvent.Location.GameModeVariation} нет доступных карт.",
                    cancellationToken: ct);
                return;
            }

            int totalPages = (maps.Length + MapsPerPage - 1) / MapsPerPage;
            if (page > totalPages)
            {
                await BotClient.SendMessage(chatId, $"❌ Страницы {page} нет. Доступно страниц: {totalPages}.", cancellationToken: ct);
                return;
            }

            var pageMaps = maps.Skip((page - 1) * MapsPerPage).Take(MapsPerPage);
            var list = new StringBuilder(
                $"🗺 Карты режима {currentEvent.Location.GameModeVariation}\n" +
                $"Слот {slot}, страница {page}/{totalPages}\n\n");

            foreach (var map in pageMaps)
            {
                string currentMarker = map.GetGlobalId() == currentEvent.LocationId ? " ← активна" : string.Empty;
                list.AppendLine($"{map.GetGlobalId()} — {map.GetName()}{currentMarker}");
            }

            list.AppendLine();
            list.Append($"Для смены: /setmap {slot} <ID карты>");
            await BotClient.SendMessage(chatId, list.ToString(), cancellationToken: ct);
        }

        private static async Task HandleSetMap(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 3 || !int.TryParse(args[1], out int slot) || !int.TryParse(args[2], out int locationId))
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Формат: /setmap <слот> <ID карты>\nСлоты и ID можно посмотреть командами /maps и /maps <слот>.",
                    cancellationToken: ct);
                return;
            }

            if (!Events.TryChangeMap(slot, locationId, out var changedEvent, out string error))
            {
                await BotClient.SendMessage(chatId, $"❌ {error}", cancellationToken: ct);
                return;
            }

            await BotClient.SendMessage(
                chatId,
                $"✅ Карта в слоте {slot} изменена на {changedEvent.Location.GetName()} " +
                $"[{changedEvent.Location.GameModeVariation}], ID {changedEvent.LocationId}.\n" +
                "Изменение уже отправлено подключённым игрокам.",
                cancellationToken: ct);
        }

        private static Dictionary<int, string> LoadThemeNames()
        {
            var result = new Dictionary<int, string>();
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "csv_logic", "themes.csv");
            if (!File.Exists(file)) return result;

            try
            {
                int id = 0;
                foreach (string line in File.ReadLines(file).Skip(2))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int comma = line.IndexOf(',');
                    string name = (comma < 0 ? line : line[..comma]).Trim().Trim('"');
                    if (string.IsNullOrEmpty(name)) continue;
                    result[id++] = name;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Не удалось прочитать список фонов: {ex.Message}");
            }

            return result;
        }

        private static async Task HandleListThemes(long chatId, CancellationToken ct)
        {
            var themes = LoadThemeNames();
            var text = new StringBuilder("🖼 Доступные фоны главного экрана:\n\n");
            if (themes.Count == 0)
            {
                text.AppendLine("Список не найден в игровых данных.");
                text.AppendLine("Используйте ID фона из themes.csv (например, 88).");
            }
            else
            {
                foreach (var theme in themes)
                    text.AppendLine($"{theme.Key} — {theme.Value}");
            }

            text.AppendLine();
            text.AppendLine("Для всех игроков: /settheme ID");
            text.Append("Для одного игрока: /settheme ID TAG");
            await BotClient.SendMessage(chatId, text.ToString(), cancellationToken: ct);
        }

        private static async Task HandleSetTheme(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId,
                    "❌ Формат: /settheme <ID> [TAG]\n" +
                    "Без TAG фон меняется у всех игроков. Список ID: /themes.",
                    cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[1], out int themeId) || themeId < 0 || themeId > 10000)
            {
                await BotClient.SendMessage(chatId,
                    "❌ ID фона должен быть целым числом от 0 до 10000. Список ID: /themes.",
                    cancellationToken: ct);
                return;
            }

            string themeName = LoadThemeNames().TryGetValue(themeId, out string name) ? name : null;
            string targetToken = args.Length >= 3 ? args[2] : null;
            if (targetToken != null && !targetToken.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                long targetId;
                try { targetId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(targetToken); }
                catch
                {
                    await BotClient.SendMessage(chatId, "❌ Неверный тег игрока.", cancellationToken: ct);
                    return;
                }

                Account account = Accounts.Load(targetId);
                if (account == null || account.Home == null)
                {
                    await BotClient.SendMessage(chatId, $"❌ Игрок {targetToken} не найден.", cancellationToken: ct);
                    return;
                }

                account.Home.PreferredThemeId = themeId;
                account.Home.ThemeId = themeId;
                Accounts.Save(account);
                SendThemeUpdateToOnlinePlayer(account.AccountId, themeId);

                await BotClient.SendMessage(chatId,
                    $"✅ Фон игрока {targetToken} изменён на {themeId}" +
                    (themeName == null ? "." : $" ({themeName})."), cancellationToken: ct);
                return;
            }

            GromCore.Laser.Logic.GeneralStaticLogic.DefaultThemeId = themeId;
            bool themeSaved = SaveThemeConfig();
            int updated = 0;
            var activeIds = new HashSet<long>();

            foreach (var session in Sessions.ActiveSessions.Values.ToArray())
            {
                if (session?.Home?.Home == null || session.Home.Avatar == null) continue;
                long accountId = session.Home.Avatar.AccountId;
                activeIds.Add(accountId);
                session.Home.Home.PreferredThemeId = themeId;
                session.Home.Home.ThemeId = themeId;
                Accounts.Save(new Account
                {
                    AccountId = accountId,
                    PassToken = session.Home.Avatar.PassToken,
                    Home = session.Home.Home,
                    Avatar = session.Home.Avatar
                });
                SendThemeUpdate(session.Home);
                updated++;
            }

            foreach (Account account in Accounts.GetAll())
            {
                if (account == null || activeIds.Contains(account.AccountId) || account.Home == null) continue;
                account.Home.PreferredThemeId = themeId;
                account.Home.ThemeId = themeId;
                Accounts.Save(account);
                updated++;
            }

            await BotClient.SendMessage(chatId,
                $"✅ Фон изменён для всех игроков ({updated}) на {themeId}" +
                (themeName == null ? "." : $" ({themeName}).") +
                (themeSaved ? string.Empty : "\n⚠️ Не удалось сохранить настройку на диск; после перезапуска повторите команду."),
                cancellationToken: ct);
        }

        private static void SendThemeUpdateToOnlinePlayer(long accountId, int themeId)
        {
            var session = Sessions.GetSession(accountId);
            if (session?.Home == null) return;
            session.Home.Home.PreferredThemeId = themeId;
            session.Home.Home.ThemeId = themeId;
            SendThemeUpdate(session.Home);
        }

        private static void SendThemeUpdate(HomeMode homeMode)
        {
            try
            {
                homeMode.Home.SetGeneralLogicData();
                homeMode.GameListener.SendTCPMessage(new OwnHomeDataMessage
                {
                    Home = homeMode.Home,
                    Avatar = homeMode.Avatar
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Не удалось обновить фон онлайн-игрока: {ex.Message}");
            }
        }

        // ==================== АКЦИИ ====================

        private static async Task HandleCreateOfferInteractive(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }

            var newOffer = new CustomOffer
            {
                CreatedBy = $"@{message.From.Username ?? message.From.Id.ToString()}"
            };
            
            _pendingOffer[chatId] = newOffer;
            _userState[chatId] = "waiting_offer_rewards";
            
            await BotClient.SendMessage(chatId,
                "🎯 *Создание новой акции*\n\n" +
                "Отправьте список наград в формате:\n\n" +
                "`gems:100 coins:5000 pp:1000 skin:29000001`\n\n" +
                "📝 *Доступные типы наград:*\n" +
                "• `gems:кол-во` - гемы\n" +
                "• `coins:кол-во` - монеты\n" +
                "• `pp:кол-во` - очки силы\n" +
                "• `bling:кол-во` - блинги\n" +
                "• `sp:кол-во` - старпоинты\n" +
                "• `recruit:кол-во` - рекруит токены\n" +
                "• `skin:ID` - скин (ID как 29000001)\n" +
                "• `brawler:ID` - боец (ID как 16000001)\n" +
                "• `emote:ID` - пин\n" +
                "• `spray:ID` - спрей\n" +
                "• `title:ID` - титул\n" +
                "• `icon:ID` - иконка\n" +
                "• `vip:уровень:дни` - VIP\n" +
                "• `pass` - Brawl Pass\n" +
                "• `passplus` - Brawl Pass Plus\n" +
                "• `chaos:кол-во` - хаосдропы\n" +
                "• `ultra:кол-во` - ультра-хаосдропы\n\n" +
                "Пример: `gems:100 coins:5000 skin:29000001`\n\n" +
                "Отправьте `готово` когда закончите, или `отмена` для отмены",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task ProcessOfferRewardsInput(Telegram.Bot.Types.Message message, CancellationToken ct)
        {
            long chatId = message.Chat.Id;
            string text = message.Text?.Trim() ?? string.Empty;
            
            if (!_pendingOffer.TryGetValue(chatId, out var offer))
            {
                _userState.TryRemove(chatId, out _);
                await BotClient.SendMessage(chatId,
                    "ℹ️ Мастер создания акции уже завершён или был отменён. Запустите /crtemp заново.",
                    cancellationToken: ct);
                return;
            }
            
            if (text.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            if (text.ToLower() == "готово")
            {
                if (offer.Rewards.Count == 0)
                {
                    await BotClient.SendMessage(chatId, "❌ Добавьте хотя бы одну награду!", cancellationToken: ct);
                    return;
                }
                
                _userState[chatId] = "waiting_offer_title";
                await BotClient.SendMessage(chatId, 
                    "📝 Введите название акции (до 50 символов):\n\n" +
                    "Пример: `🔥 МЕГА АКЦИЯ`\n\n" +
                    "Отправьте `отмена` для отмены",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var rewards = new List<CustomOfferReward>();
            bool hasError = false;
            
            foreach (string part in parts)
            {
                string[] rewardParts = part.Split(':');
                if (rewardParts.Length < 1) continue;
                
                string type = rewardParts[0].ToLower();
                var reward = new CustomOfferReward { Type = type };
                
                switch (type)
                {
                    case "gems":
                    case "coins":
                    case "pp":
                    case "bling":
                    case "sp":
                    case "recruit":
                    case "chaos":
                    case "ultra":
                        if (rewardParts.Length >= 2 && int.TryParse(rewardParts[1], out int count) && count > 0)
                        {
                            reward.Count = count;
                            rewards.Add(reward);
                        }
                        else
                        {
                            await BotClient.SendMessage(chatId, $"❌ Неверное количество для {type}", cancellationToken: ct);
                            hasError = true;
                        }
                        break;
                        
                    case "skin":
                        if (rewardParts.Length >= 2 && int.TryParse(rewardParts[1], out int skinId))
                        {
                            int skinInstanceId = GlobalId.GetInstanceId(skinId);
                            var skinTable = DataTables.Get(DataType.Skin);
                            int skinClassId = GlobalId.GetClassId(skinId);

                            if (skinInstanceId < 0 || skinInstanceId >= skinTable.Count ||
                                (skinClassId != 0 && skinClassId != (int)DataType.Skin))
                            {
                                await BotClient.SendMessage(chatId,
                                    $"❌ Скин с ID {skinId} не найден. Используйте полный ID вида 29000001 или instance ID.",
                                    cancellationToken: ct);
                                hasError = true;
                                break;
                            }

                            reward.Count = 1;
                            reward.DataId = GlobalId.CreateGlobalId((int)DataType.Skin, skinInstanceId);
                            rewards.Add(reward);
                        }
                        else
                        {
                            await BotClient.SendMessage(chatId, "❌ Неверный ID для skin", cancellationToken: ct);
                            hasError = true;
                        }
                        break;

                    case "brawler":
                    case "emote":
                    case "spray":
                    case "title":
                    case "icon":
                        if (rewardParts.Length >= 2 && int.TryParse(rewardParts[1], out int id))
                        {
                            reward.Count = 1;
                            reward.DataId = id;
                            rewards.Add(reward);
                        }
                        else
                        {
                            await BotClient.SendMessage(chatId, $"❌ Неверный ID для {type}", cancellationToken: ct);
                            hasError = true;
                        }
                        break;
                        
                    case "vip":
                        if (rewardParts.Length >= 2 && int.TryParse(rewardParts[1], out int vipLevel) && vipLevel is >= 1 and <= 10)
                        {
                            reward.Count = vipLevel;
                            if (rewardParts.Length >= 3 && int.TryParse(rewardParts[2], out int vipDays) && vipDays > 0)
                                reward.DataId = vipDays;
                            rewards.Add(reward);
                        }
                        else
                        {
                            await BotClient.SendMessage(chatId, $"❌ Неверный формат для VIP. Используйте vip:уровень:дни", cancellationToken: ct);
                            hasError = true;
                        }
                        break;
                        
                    case "pass":
                    case "passplus":
                        reward.Count = 1;
                        rewards.Add(reward);
                        break;
                        
                    default:
                        await BotClient.SendMessage(chatId, $"❌ Неизвестный тип награды: {type}", cancellationToken: ct);
                        hasError = true;
                        break;
                }
            }
            
            if (hasError) return;
            
            if (rewards.Count == 0)
            {
                await BotClient.SendMessage(chatId, "❌ Не добавлено ни одной награды!", cancellationToken: ct);
                return;
            }
            
            if (!_pendingOffer.TryGetValue(chatId, out var offer1))
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            foreach (var r in rewards)
                offer1.Rewards.Add(r);
            
            string currentRewards = string.Join("\n• ", offer1.Rewards.Select(r => r.GetDescription()));
            await BotClient.SendMessage(chatId, 
                $"✅ Добавлены награды!\n\n📦 Текущие награды:\n• {currentRewards}\n\n" +
                $"Отправьте `готово` для продолжения, или добавьте ещё наград через пробел",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleCreateOfferTitle(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string title = message.Text?.Trim() ?? string.Empty;
            
            if (title.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            if (string.IsNullOrEmpty(title) || title.Length > 50)
            {
                await BotClient.SendMessage(chatId, "❌ Название должно быть от 1 до 50 символов!", cancellationToken: ct);
                return;
            }
            
            if (!TryGetPendingOffer(chatId, out var offer))
            {
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            offer.Title = title;
            
            _userState[chatId] = "waiting_offer_desc";
            await BotClient.SendMessage(chatId,
                "📝 Введите описание акции (до 100 символов):\n\n" +
                "Пример: `Скидка 50% на гемы!`\n\n" +
                "Отправьте `пропустить` чтобы пропустить",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleCreateOfferDesc(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string desc = message.Text?.Trim() ?? string.Empty;
            
            if (desc.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            if (!TryGetPendingOffer(chatId, out var offer))
            {
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            if (desc.ToLower() != "пропустить")
            {
                if (desc.Length > 100)
                {
                    await BotClient.SendMessage(chatId, "❌ Описание должно быть до 100 символов!", cancellationToken: ct);
                    return;
                }
                offer.Description = desc;
            }
            
            _userState[chatId] = "waiting_offer_cost";
            await BotClient.SendMessage(chatId,
                "💰 Введите стоимость акции:\n\n" +
                "Формат: `<цена> <валюта>`\n\n" +
                "📝 Валюты:\n" +
                "• `gems` - гемы\n" +
                "• `coins` - монеты\n" +
                "• `bling` - блинги\n" +
                "• `sp` - старпоинты\n\n" +
                "Примеры:\n" +
                "`99 gems` - 99 гемов\n" +
                "`5000 coins` - 5000 монет\n" +
                "`0` - бесплатно\n\n" +
                "Отправьте `отмена` для отмены",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleCreateOfferCost(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string text = message.Text?.Trim() ?? string.Empty;
            
            if (text.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int cost = 0;
            int currency = 0;
            
            if (parts.Length == 1 && int.TryParse(parts[0], out cost) && cost == 0)
            {
                currency = 0;
            }
            else if (parts.Length == 2 && int.TryParse(parts[0], out cost) && cost > 0)
            {
                string currencyStr = parts[1].ToLower();
                currency = currencyStr switch
                {
                    "gems" => 0,
                    "coins" => 1,
                    "bling" => 2,
                    "sp" => 3,
                    _ => -1
                };
                
                if (currency == -1)
                {
                    await BotClient.SendMessage(chatId, "❌ Неверная валюта! Используйте: gems, coins, bling, sp", cancellationToken: ct);
                    return;
                }
            }
            else
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат! Примеры: `99 gems` или `0`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            if (!TryGetPendingOffer(chatId, out var offer))
            {
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            offer.Cost = cost;
            offer.OldCost = (int)Math.Min((long)cost * 2, int.MaxValue);
            offer.Currency = currency;
            
            _userState[chatId] = "waiting_offer_duration";
            await BotClient.SendMessage(chatId,
                "⏰ Введите длительность акции:\n\n" +
                "Формат: `<кол-во> <единица>`\n\n" +
                "Единицы: `дней`, `часов`, `минут`\n\n" +
                "Примеры:\n" +
                "`7 дней` - на 7 дней\n" +
                "`24 часа` - на 24 часа\n" +
                "`30 минут` - на 30 минут\n\n" +
                "Отправьте `отмена` для отмены",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleCreateOfferDuration(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string text = message.Text?.Trim() ?? string.Empty;
            
            if (text.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат! Пример: `7 дней`", cancellationToken: ct);
                return;
            }
            
            if (!int.TryParse(parts[0], out int duration) || duration <= 0)
            {
                await BotClient.SendMessage(chatId, "❌ Длительность должна быть положительным числом.", cancellationToken: ct);
                return;
            }
            
            string unit = parts[1].ToLower();
            TimeSpan timeSpan;
            
            switch (unit)
            {
                case "дней":
                case "день":
                case "дня":
                    timeSpan = TimeSpan.FromDays(duration);
                    break;
                case "часов":
                case "час":
                case "часа":
                    timeSpan = TimeSpan.FromHours(duration);
                    break;
                case "минут":
                case "минута":
                case "минуты":
                    timeSpan = TimeSpan.FromMinutes(duration);
                    break;
                default:
                    await BotClient.SendMessage(chatId, "❌ Неверная единица! Используйте: дней, часов, минут", cancellationToken: ct);
                    return;
            }
            
            if (!TryGetPendingOffer(chatId, out var offer))
            {
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            offer.StartTime = DateTime.Now;
            offer.EndTime = DateTime.Now.Add(timeSpan);
            
            _userState[chatId] = "waiting_offer_background";
            await BotClient.SendMessage(chatId,
                "🎨 Введите фон акции (опционально):\n\n" +
                "Доступные фоны:\n" +
                "• `offer_bgr_legendary` - легендарный (по умолчанию)\n" +
                "• `offer_bgr_epic` - эпический\n" +
                "• `offer_bgr_mythic` - мифический\n" +
                "• `offer_bgr_xmas23` - рождественский\n" +
                "• `offer_bgr_wf2023` - чемпионский\n\n" +
                "Отправьте `пропустить` для использования стандартного фона",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleCreateOfferBackground(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string text = message.Text?.Trim() ?? string.Empty;
            
            if (text.ToLower() == "отмена")
            {
                ClearOfferState(chatId);
                await BotClient.SendMessage(chatId, "❌ Создание акции отменено.", cancellationToken: ct);
                return;
            }
            
            if (!TryGetPendingOffer(chatId, out var offer))
            {
                await BotClient.SendMessage(chatId, "❌ Состояние мастера потеряно. Запустите /crtemp заново.", cancellationToken: ct);
                return;
            }
            if (text.ToLower() != "пропустить")
            {
                offer.BackgroundName = text;
            }
            
            if (OffersManager.CreateOffer(offer))
            {
                string rewardsDesc = string.Join("\n• ", offer.Rewards.Select(r => r.GetDescription()));
                string currencyName = offer.GetCurrencyName();
                string costText = offer.Cost == 0 ? "БЕСПЛАТНО" : $"{offer.Cost} {currencyName}";
                string oldCostText = offer.OldCost > offer.Cost ? $"~~{offer.OldCost}~~ " : "";
                
                await BotClient.SendMessage(chatId,
                    $"✅ *Акция успешно создана!*\n\n" +
                    $"📋 ID: `{offer.Id}`\n" +
                    $"🏷️ Название: {offer.Title}\n" +
                    $"📝 Описание: {offer.Description ?? "—"}\n" +
                    $"💰 Цена: {oldCostText}{costText}\n" +
                    $"🎁 Награды:\n• {rewardsDesc}\n" +
                    $"⏰ Начало: {offer.StartTime:dd.MM.yyyy HH:mm}\n" +
                    $"⏰ Конец: {offer.EndTime:dd.MM.yyyy HH:mm}\n" +
                    $"🎨 Фон: {offer.BackgroundName}\n\n" +
                    $"🔄 Акция появится в магазине у всех онлайн игроков в течение 10 секунд",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, "❌ Ошибка при создании акции!", cancellationToken: ct);
            }
            
            ClearOfferState(chatId);
        }

        private static async Task HandleListOffers(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            var offers = OffersManager.GetAllOffers();
            
            if (offers.Count == 0)
            {
                await BotClient.SendMessage(chatId, "📭 Список акций пуст.\n\nСоздайте акцию командой /crtemp", cancellationToken: ct);
                return;
            }
            
            string result = "*📋 СПИСОК АКЦИЙ*\n\n";
            int index = 1;
            var now = DateTime.Now;
            
            foreach (var offer in offers)
            {
                string statusIcon = offer.GetStatusIcon();
                string timeStatus = "";
                if (offer.StartTime > now)
                    timeStatus = $" (старт {offer.StartTime:dd.MM HH:mm})";
                else if (offer.EndTime < now)
                    timeStatus = $" (истекла {offer.EndTime:dd.MM HH:mm})";
                else
                    timeStatus = $" (до {offer.EndTime:dd.MM HH:mm})";
                
                result += $"{index}. {statusIcon} `{offer.Id}`\n";
                result += $"   📌 {offer.Title}{timeStatus}\n";
                result += $"   💰 Цена: {(offer.Cost == 0 ? "Бесплатно" : $"{offer.Cost} {offer.GetCurrencyName()}")}\n";
                result += $"   🎁 Наград: {offer.Rewards.Count}\n";
                index++;
            }
            
            result += $"\n📊 Всего: {offers.Count} акций\n\n" +
                      "💡 Команды:\n" +
                      "• `/offertemp <id>` - информация об акции\n" +
                      "• `/deltemp <id>` - удалить акцию\n" +
                      "• `/edittemp <id>` - редактировать\n" +
                      "• `/activatetemp <id>` - активировать\n" +
                      "• `/deactivatetemp <id>` - деактивировать";
            
            await BotClient.SendMessage(chatId, result, parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleOfferInfo(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/offertemp <id>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string id = args[1];
            var offer = OffersManager.GetOffer(id);
            
            if (offer == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Акция с ID `{id}` не найдена!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string rewardsDesc = string.Join("\n• ", offer.Rewards.Select(r => r.GetDescription()));
            string status = offer.IsValid() ? "✅ Активна" : (offer.IsActive ? "⏳ Ожидает" : "❌ Деактивирована");
            string timeStatus = offer.StartTime > DateTime.Now ? "📅 Ещё не началась" : 
                               (offer.EndTime < DateTime.Now ? "⌛ Истекла" : "🟢 Активна сейчас");
            
            await BotClient.SendMessage(chatId,
                $"*📊 ИНФОРМАЦИЯ О АКЦИИ*\n\n" +
                $"🆔 ID: `{offer.Id}`\n" +
                $"📌 Статус: {status}\n" +
                $"⏰ Время: {timeStatus}\n" +
                $"🏷️ Название: {offer.Title}\n" +
                $"📝 Описание: {offer.Description ?? "—"}\n" +
                $"💰 Цена: {(offer.Cost == 0 ? "БЕСПЛАТНО" : $"{offer.Cost} {offer.GetCurrencyName()}")}\n" +
                $"~~💰 Старая цена: {(offer.OldCost == 0 ? "—" : $"{offer.OldCost} {offer.GetCurrencyName()}")}~~\n" +
                $"🎁 Награды:\n• {rewardsDesc}\n" +
                $"⏰ Начало: {offer.StartTime:dd.MM.yyyy HH:mm}\n" +
                $"⏰ Конец: {offer.EndTime:dd.MM.yyyy HH:mm}\n" +
                $"🎨 Фон: {offer.BackgroundName}\n" +
                $"👤 Создал: {offer.CreatedBy}\n" +
                $"📅 Создана: {offer.CreatedAt:dd.MM.yyyy HH:mm}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleDeleteOffer(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/deltemp <id>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string id = args[1];
            var offer = OffersManager.GetOffer(id);
            
            if (offer == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Акция с ID `{id}` не найдена!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            if (OffersManager.DeleteOffer(id))
            {
                await BotClient.SendMessage(chatId, $"✅ Акция `{offer.Title}` (ID: {id}) удалена!\n\nАкции обновлены у всех онлайн игроков.", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"❌ Ошибка при удалении акции!", cancellationToken: ct);
            }
        }

        private static async Task HandleEditOffer(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Формат: `/edittemp <id> <поле> <значение>`\n\n" +
                    "📝 Доступные поля:\n" +
                    "• `title` - название\n" +
                    "• `desc` - описание\n" +
                    "• `cost` - цена (число)\n" +
                    "• `oldcost` - старая цена\n" +
                    "• `currency` - валюта (gems/coins/bling/sp)\n" +
                    "• `days` - добавить дней\n" +
                    "• `hours` - добавить часов\n" +
                    "• `bg` - фон\n\n" +
                    "Пример: `/edittemp abc123 title НОВОЕ НАЗВАНИЕ`",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string id = args[1];
            var offer = OffersManager.GetOffer(id);
            
            if (offer == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Акция с ID `{id}` не найдена!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Укажите поле для изменения!", cancellationToken: ct);
                return;
            }
            
            string field = args[2].ToLower();
            string value = args.Length > 3 ? string.Join(" ", args.Skip(3)) : "";
            
            bool changed = false;
            string oldValue = "";
            
            switch (field)
            {
                case "title":
                    oldValue = offer.Title;
                    offer.Title = value;
                    changed = true;
                    break;
                case "desc":
                    oldValue = offer.Description ?? "—";
                    offer.Description = value;
                    changed = true;
                    break;
                case "cost":
                    if (int.TryParse(value, out int newCost))
                    {
                        oldValue = offer.Cost.ToString();
                        offer.Cost = newCost;
                        changed = true;
                    }
                    else
                    {
                        await BotClient.SendMessage(chatId, "❌ Цена должна быть числом!", cancellationToken: ct);
                        return;
                    }
                    break;
                case "oldcost":
                    if (int.TryParse(value, out int newOldCost))
                    {
                        oldValue = offer.OldCost.ToString();
                        offer.OldCost = newOldCost;
                        changed = true;
                    }
                    else
                    {
                        await BotClient.SendMessage(chatId, "❌ Цена должна быть числом!", cancellationToken: ct);
                        return;
                    }
                    break;
                case "currency":
                    int newCurrency = value.ToLower() switch
                    {
                        "gems" => 0,
                        "coins" => 1,
                        "bling" => 2,
                        "sp" => 3,
                        _ => -1
                    };
                    if (newCurrency != -1)
                    {
                        oldValue = offer.GetCurrencyName();
                        offer.Currency = newCurrency;
                        changed = true;
                    }
                    else
                    {
                        await BotClient.SendMessage(chatId, "❌ Неверная валюта! Используйте: gems, coins, bling, sp", cancellationToken: ct);
                        return;
                    }
                    break;
                case "days":
                    if (int.TryParse(value, out int days))
                    {
                        oldValue = offer.EndTime.ToString("dd.MM.yyyy HH:mm");
                        offer.EndTime = offer.EndTime.AddDays(days);
                        changed = true;
                    }
                    break;
                case "hours":
                    if (int.TryParse(value, out int hours))
                    {
                        oldValue = offer.EndTime.ToString("dd.MM.yyyy HH:mm");
                        offer.EndTime = offer.EndTime.AddHours(hours);
                        changed = true;
                    }
                    break;
                case "bg":
                    oldValue = offer.BackgroundName;
                    offer.BackgroundName = value;
                    changed = true;
                    break;
                default:
                    await BotClient.SendMessage(chatId, $"❌ Неизвестное поле: {field}", cancellationToken: ct);
                    return;
            }
            
            if (changed)
            {
                if (OffersManager.UpdateOffer(id, offer))
                {
                    await BotClient.SendMessage(chatId,
                        $"✅ Поле `{field}` изменено!\n\n" +
                        $"📝 Было: `{oldValue}`\n" +
                        $"📝 Стало: `{(field == "currency" ? offer.GetCurrencyName() : value)}`\n\n" +
                        $"🔄 Акции обновлены у всех онлайн игроков.",
                        parseMode: ParseMode.Markdown, cancellationToken: ct);
                }
                else
                {
                    await BotClient.SendMessage(new ChatId(chatId), "❌ Ошибка при обновлении акции!", cancellationToken: ct);
                }
            }
        }

        private static async Task HandleActivateOffer(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/activatetemp <id>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string id = args[1];
            var offer = OffersManager.GetOffer(id);
            
            if (offer == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Акция с ID `{id}` не найдена!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            if (offer.IsActive)
            {
                await BotClient.SendMessage(chatId, $"⚠️ Акция `{offer.Title}` уже активна!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            offer.IsActive = true;
            if (OffersManager.UpdateOffer(id, offer))
            {
                await BotClient.SendMessage(chatId,
                    $"✅ Акция `{offer.Title}` активирована!\n\n" +
                    $"🔄 Акции обновлены у всех онлайн игроков.",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, "❌ Ошибка при активации акции!", cancellationToken: ct);
            }
        }

        private static async Task HandleDeactivateOffer(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/deactivatetemp <id>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string id = args[1];
            var offer = OffersManager.GetOffer(id);
            
            if (offer == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Акция с ID `{id}` не найдена!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            if (!offer.IsActive)
            {
                await BotClient.SendMessage(chatId, $"⚠️ Акция `{offer.Title}` уже деактивирована!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            offer.IsActive = false;
            if (OffersManager.UpdateOffer(id, offer))
            {
                await BotClient.SendMessage(chatId,
                    $"✅ Акция `{offer.Title}` деактивирована!\n\n" +
                    $"🔄 Акции обновлены у всех онлайн игроков.",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, "❌ Ошибка при деактивации акции!", cancellationToken: ct);
            }
        }

        private static async Task HandleRefreshOffers(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }
            
            await BotClient.SendMessage(chatId, "🔄 Принудительное обновление акций для всех онлайн игроков...", cancellationToken: ct);
            
            OffersManager.ApplyToAllOnlinePlayers();
            
            var activeCount = OffersManager.GetActiveOffers().Count;
            await BotClient.SendMessage(chatId, 
                $"✅ Акции обновлены!\n\n" +
                $"📊 Активных акций: {activeCount}\n" +
                $"👥 Обновлено для всех онлайн игроков",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        // ==================== ПРОМОКОДЫ ====================

        private static async Task HandleCreatePromo(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string[] args = message.Text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (args.Length < 4)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Неверный формат.\n\n📝 *Создание промокода:*\n`/crpromo <код> <лимит> <тип1:кол-во[:ID]> [тип2:кол-во[:ID]] ...`\n\n🏷️ *Типы наград:*\n" +
                    "• `gems:100` - гемы\n• `coins:5000` - монеты\n• `pp:1000` - очки силы\n• `bling:2000` - блинги\n" +
                    "• `sp:500` - старпоинты\n• `recruit:50` - рекруит токены\n• `skin:29000001` - скин\n" +
                    "• `brawler:16000001` - боец\n• `emote:2037` - пин\n• `spray:68000001` - спрей\n" +
                    "• `title:76000104` - титул\n• `icon:28000186` - иконка\n" +
                    "• `vip:1` - VIP 1 бессрочно\n• `vip:1:30` - VIP 1 на 30 дней\n• `vip:5:7` - VIP 5 на 7 дней\n" +
                    "• `pass` - Brawl Pass\n• `passplus` - Brawl Pass Plus\n" +
                    "• `chaos:3` - хаосдропы\n• `ultra:1` - ультра-хаосдропы\n\n💡 *Примеры:*\n" +
                    "`/crpromo TEST 10 vip:5:30 gems:100 coins:5000`\n" +
                    "`/crpromo VIP2024 1 vip:10:365` - VIP 10 на год\n" +
                    "`/crpromo WEEKEND 50 vip:3:7 pp:500`",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string code = args[1];

            if (code.Length < 3 || code.Length > 32 || code.Any(char.IsWhiteSpace))
            {
                await BotClient.SendMessage(chatId, "❌ Код должен быть длиной 3–32 символа и без пробелов.", cancellationToken: ct);
                return;
            }
            
            if (!int.TryParse(args[2], out int maxUses))
            {
                await BotClient.SendMessage(chatId, "❌ Лимит использований должен быть числом!", cancellationToken: ct);
                return;
            }

            if (maxUses < 0)
            {
                await BotClient.SendMessage(chatId, "❌ Лимит использований не может быть отрицательным.", cancellationToken: ct);
                return;
            }

            var rewards = new List<PromoReward>();
            
            for (int i = 3; i < args.Length; i++)
            {
                string[] parts = args[i].Split(':');
                if (parts.Length < 1) continue;
                
                string type = parts[0].ToLower();
                
                switch (type)
                {
                    case "gems":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int gemsCount))
                            rewards.Add(new PromoReward(PromoRewardType.Gems, gemsCount));
                        break;
                    case "coins":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int coinsCount))
                            rewards.Add(new PromoReward(PromoRewardType.Coins, coinsCount));
                        break;
                    case "pp":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int ppCount))
                            rewards.Add(new PromoReward(PromoRewardType.PowerPoints, ppCount));
                        break;
                    case "bling":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int blingCount))
                            rewards.Add(new PromoReward(PromoRewardType.Bling, blingCount));
                        break;
                    case "sp":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int spCount))
                            rewards.Add(new PromoReward(PromoRewardType.StarPoints, spCount));
                        break;
                    case "recruit":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int recruitCount))
                            rewards.Add(new PromoReward(PromoRewardType.RecruitTokens, recruitCount));
                        break;
                    case "skin":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int skinId))
                            rewards.Add(new PromoReward(PromoRewardType.Skin, 1, skinId));
                        break;
                    case "brawler":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int brawlerId))
                            rewards.Add(new PromoReward(PromoRewardType.Brawler, 1, brawlerId));
                        break;
                    case "emote":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int emoteId))
                            rewards.Add(new PromoReward(PromoRewardType.Emote, 1, emoteId));
                        break;
                    case "spray":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int sprayId))
                            rewards.Add(new PromoReward(PromoRewardType.Spray, 1, sprayId));
                        break;
                    case "title":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int titleId))
                            rewards.Add(new PromoReward(PromoRewardType.PlayerTitle, 1, titleId));
                        break;
                    case "icon":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int iconId))
                            rewards.Add(new PromoReward(PromoRewardType.PlayerThumbnail, 1, iconId));
                        break;
                    case "vip":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int vipLevel) && vipLevel >= 1 && vipLevel <= 10)
                        {
                            int vipDuration = 0;
                            if (parts.Length >= 3 && int.TryParse(parts[2], out int duration) && duration > 0)
                                vipDuration = duration;
                            rewards.Add(new PromoReward(PromoRewardType.VIP, vipLevel, 0, vipDuration.ToString()));
                        }
                        break;
                    case "pass":
                        rewards.Add(new PromoReward(PromoRewardType.BrawlPass, 1));
                        break;
                    case "passplus":
                        rewards.Add(new PromoReward(PromoRewardType.BrawlPassPlus, 1));
                        break;
                    case "chaos":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int chaosCount))
                            rewards.Add(new PromoReward(PromoRewardType.ChaosDrop, chaosCount));
                        break;
                    case "ultra":
                        if (parts.Length >= 2 && int.TryParse(parts[1], out int ultraCount))
                            rewards.Add(new PromoReward(PromoRewardType.UltraChaosDrop, ultraCount));
                        break;
                    default:
                        await BotClient.SendMessage(chatId, $"❌ Неизвестный тип награды: {type}", cancellationToken: ct);
                        return;
                }
            }

            if (rewards.Count == 0)
            {
                await BotClient.SendMessage(chatId, "❌ Нужно указать хотя бы одну награду!", cancellationToken: ct);
                return;
            }

            if (!PromoCodeManager.NormalizeRewards(rewards, out string rewardError))
            {
                await BotClient.SendMessage(chatId, $"❌ Награда не прошла проверку: {rewardError}", cancellationToken: ct);
                return;
            }

            if (PromoCodeManager.CreatePromoCode(code, rewards, maxUses, null, message.From.Id.ToString()))
            {
                string rewardsDesc = string.Join("\n• ", rewards.Select(r => r.GetDescription()));
                await BotClient.SendMessage(chatId, 
                    $"✅ *Промокод создан!*\n\n🔑 Код: `{code}`\n📊 Лимит: {maxUses} использований\n🎁 Награды:\n• {rewardsDesc}\n\n💡 Игроки могут активировать его в игре командой `/promo {code}`",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"❌ Промокод `{code}` уже существует!", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }

        private static async Task HandleListPromoCodes(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            var promoCodes = PromoCodeManager.GetAllPromoCodes();
            
            if (promoCodes.Count == 0)
            {
                await BotClient.SendMessage(chatId, "📭 Список промокодов пуст.", cancellationToken: ct);
                return;
            }
            
            string result = "*📋 Список промокодов:*\n\n";
            int index = 1;
            
            foreach (var promo in promoCodes.Take(20))
            {
                string status = promo.IsValid() ? "✅" : "❌";
                result += $"{index}. {status} `{promo.Code}`\n";
                result += $"   📊 Использовано: {promo.CurrentUses}/{promo.MaxUses}\n";
                index++;
            }
            
            if (promoCodes.Count > 20)
                result += $"... и ещё {promoCodes.Count - 20} промокодов\n";
            
            await BotClient.SendMessage(chatId, result, parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleDeletePromo(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string[] args = message.Text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/delpromo <код>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string code = args[1].ToUpper();
            
            if (PromoCodeManager.DeletePromoCode(code))
            {
                await BotClient.SendMessage(chatId, $"✅ Промокод `{code}` удалён!", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"❌ Промокод `{code}` не найден!", parseMode: ParseMode.Markdown, cancellationToken: ct);
            }
        }

        private static async Task HandlePromoInfo(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string[] args = message.Text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/promoinfo <код>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string code = args[1].ToUpper();
            var promo = PromoCodeManager.GetPromoCode(code);
            
            if (promo == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Промокод `{code}` не найден!", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            string rewardDesc = string.Join("\n• ", promo.Rewards.Select(r => r.GetDescription()));
            string status = promo.IsValid() ? "✅ Активен" : "❌ Истёк/Использован";
            
            await BotClient.SendMessage(chatId,
                $"*📊 Информация о промокоде:*\n\n🔑 Код: `{promo.Code}`\n📊 Статус: {status}\n🎁 Награды:\n• {rewardDesc}\n📈 Использований: {promo.CurrentUses}/{promo.MaxUses}\n📅 Создан: {promo.CreatedAt:dd.MM.yyyy HH:mm}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandlePromoValidate(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct)
        {
            string[] args = message.Text.Substring(1).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Формат: `/promovalidate <код>`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string code = args[1].Trim().ToUpperInvariant();
            var promo = PromoCodeManager.GetPromoCode(code);
            if (promo == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Промокод `{code}` не найден.", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            if (!PromoCodeManager.NormalizeRewards(promo.Rewards, out string error))
            {
                await BotClient.SendMessage(chatId, $"❌ Промокод повреждён: {error}\nИсправь или пересоздай его.", cancellationToken: ct);
                return;
            }

            // Persist canonical IDs for old promo files as well.
            PromoCodeManager.Save();
            string rewards = string.Join("\n• ", promo.Rewards.Select(r => r.GetDescription()));
            await BotClient.SendMessage(chatId,
                $"✅ Промокод `{promo.Code}` исправен.\n\n🎁 Награды:\n• {rewards}\n📊 Использований: {promo.CurrentUses}/{promo.MaxUses}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        // ==================== VIP ====================

        private static async Task HandleSetVip(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Неверный формат.\n\n📝 *Примеры:*\n" +
                    "`/setvip #токен 1` - бессрочный VIP 1 уровня\n" +
                    "`/setvip #токен 2 30` - VIP 2 уровня на 30 дней\n" +
                    "`/setvip #токен 3 7` - VIP 3 уровня на 7 дней\n" +
                    "`/setvip #токен 0` - снять VIP\n\n" +
                    "*VIP уровни и бонусы:*\n" +
                    "• VIP 1 → +15 кубков\n" +
                    "• VIP 2 → +30 кубков\n" +
                    "• VIP 3 → +45 кубков\n" +
                    "• VIP 4 → +55 кубков\n" +
                    "• VIP 5 → +65 кубков\n" +
                    "• VIP 6 → +75 кубков\n" +
                    "• VIP 7 → +85 кубков\n" +
                    "• VIP 8 → +90 кубков\n" +
                    "• VIP 9 → +130 кубков\n" +
                    "• VIP 10 → +170 кубков\n\n" +
                    "*Дни:* от 1 до 365 (опционально)",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string vipToken = args[1];
            var vipLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(vipToken));

            if (vipLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{vipToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int vipLevel) || vipLevel < 0 || vipLevel > 10)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный уровень VIP. Используйте 0-10.", cancellationToken: ct);
                return;
            }

            int durationDays = 0;
            if (args.Length >= 4 && vipLevel > 0)
            {
                if (!int.TryParse(args[3], out durationDays) || durationDays < 1 || durationDays > 365)
                {
                    await BotClient.SendMessage(chatId, "❌ Неверное количество дней. Используйте от 1 до 365.", cancellationToken: ct);
                    return;
                }
            }

            var vipAccount = Accounts.Load(vipLink.AccountId);
            if (vipAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            string issuer = $"@{message.From.Username ?? message.From.Id.ToString()}";
            vipAccount.Avatar.SetVipLevel(vipLevel, durationDays, issuer);
            Accounts.Save(vipAccount);

            string durationText = durationDays > 0 ? $" на {durationDays} дней" : (vipLevel > 0 ? " бессрочно" : "");
            string actionText = vipLevel == 0 ? "VIP снят" : $"выдан VIP уровень {vipLevel}{durationText} (+{VipConfig.BonusTrophies[vipLevel]} кубков)";
            
            await BotClient.SendMessage(chatId, $"✅ Игроку #{vipToken} {actionText}", cancellationToken: ct);
        }

        private static async Task HandleExtendVip(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Неверный формат.\n\n📝 *Пример:* `/extendvip #токен 30` - продлить VIP на 30 дней",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string extendToken = args[1];
            var extendLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(extendToken));

            if (extendLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{extendToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int extendDays) || extendDays < 1 || extendDays > 365)
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество дней. Используйте от 1 до 365.", cancellationToken: ct);
                return;
            }

            var extendAccount = Accounts.Load(extendLink.AccountId);
            if (extendAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            if (extendAccount.Avatar.VipLevel == 0)
            {
                await BotClient.SendMessage(chatId, $"❌ У игрока #{extendToken} нет активного VIP для продления.", cancellationToken: ct);
                return;
            }

            string issuer = $"@{message.From.Username ?? message.From.Id.ToString()}";
            extendAccount.Avatar.ExtendVip(extendDays, issuer);
            Accounts.Save(extendAccount);

            string newExpiration = extendAccount.Avatar.VipExpirationTime == DateTime.MinValue ? "бессрочно" : extendAccount.Avatar.VipExpirationTime.ToString("dd.MM.yyyy");
            
            await BotClient.SendMessage(chatId, 
                $"✅ VIP игрока #{extendToken} продлён на {extendDays} дней!\n📅 Новый срок: {newExpiration}", 
                cancellationToken: ct);
        }

        private static async Task HandleVipInfo(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: `/vipinfo #токен`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string vipInfoToken = args[1];
            var vipInfoLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(vipInfoToken));

            if (vipInfoLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{vipInfoToken} не найден.", cancellationToken: ct);
                return;
            }

            var vipInfoAccount = Accounts.Load(vipInfoLink.AccountId);
            if (vipInfoAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            vipInfoAccount.Avatar.CheckVipExpiration();
            Accounts.Save(vipInfoAccount);

            int currentLevel = vipInfoAccount.Avatar.VipLevel;
            int currentBonus = vipInfoAccount.Avatar.VipBonusTrophies;
            int totalBonusReceived = vipInfoAccount.Avatar.VipTotalBonusReceived;
            string timeRemaining = vipInfoAccount.Avatar.GetVipTimeRemaining();
            string expirationDate = vipInfoAccount.Avatar.VipExpirationTime == DateTime.MinValue ? "Бессрочно" : vipInfoAccount.Avatar.VipExpirationTime.ToString("dd.MM.yyyy HH:mm");
            string issuedBy = string.IsNullOrEmpty(vipInfoAccount.Avatar.VipIssuedBy) ? "—" : vipInfoAccount.Avatar.VipIssuedBy;
            bool isActive = vipInfoAccount.Avatar.IsVipActive();
            
            string vipStatus = isActive 
                ? $"✅ ACTIVE\n\n👑 Уровень: {currentLevel}\n✨ Бонус: +{currentBonus} кубков\n📅 Действует до: {expirationDate}\n⏰ Осталось: {timeRemaining}\n👤 Выдал: {issuedBy}\n📊 Всего получено бонусов: {totalBonusReceived}"
                : "❌ VIP НЕ АКТИВЕН";
            
            string vipName = currentLevel > 0 && VipConfig.VipNames.ContainsKey(currentLevel) ? VipConfig.VipNames[currentLevel] : "";
            
            await BotClient.SendMessage(chatId, 
                $"📊 *VIP информация об аккаунте #{vipInfoToken}*\n\n{vipName}\n{vipStatus}", 
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleUpgradeVip(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOperator(message.From.Id))
            {
                await BotClient.SendMessage(chatId, "❌ У вас нет прав для этой команды.", cancellationToken: ct);
                return;
            }

            bool isGlobal = args.Length < 2;
            string targetToken = isGlobal ? null : args[1];

            if (isGlobal)
            {
                await BotClient.SendMessage(chatId, "🔄 Повышение VIP 3-5 → 8-10 на 30 дней запущено для ВСЕХ игроков...\n⏳ Это может занять некоторое время.", cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"🔄 Повышение VIP для игрока #{targetToken} на 30 дней...", cancellationToken: ct);
            }

            var allAccounts = Accounts.GetAll();
            int upgraded3to8 = 0;
            int upgraded4to9 = 0;
            int upgraded5to10 = 0;
            int total = 0;

            foreach (var acc in allAccounts)
            {
                if (acc == null || acc.Avatar == null) continue;

                if (!isGlobal)
                {
                    string accountToken = Laser.Logic.Util.LogicLongCodeGenerator.ToCode(acc.AccountId);
                    if (accountToken != targetToken) continue;
                }

                int oldLevel = acc.Avatar.VipLevel;
                int newLevel = 0;

                if (oldLevel == 3) { newLevel = 8; upgraded3to8++; }
                else if (oldLevel == 4) { newLevel = 9; upgraded4to9++; }
                else if (oldLevel == 5) { newLevel = 10; upgraded5to10++; }

                if (newLevel > 0)
                {
                    total++;
                    string issuer = $"system_upgrade_by_{message.From.Username ?? message.From.Id.ToString()}";
                    
                    acc.Avatar.SetVipLevel(newLevel, 30, issuer);
                    Accounts.Save(acc);
                    
                    Console.WriteLine($"[VIP Upgrade] {acc.Avatar.Name} (ID:{acc.AccountId}): VIP {oldLevel} → {newLevel} на 30 дней");
                }
            }

            if (!isGlobal && total == 0)
            {
                await BotClient.SendMessage(chatId, $"❌ Игрок #{targetToken} не найден или не имеет VIP 3-5.", cancellationToken: ct);
                return;
            }

            string resultMessage = isGlobal 
                ? $"✅ *Повышение VIP завершено!*\n\n" +
                  $"📊 *Статистика:*\n" +
                  $"• VIP 3 → VIP 8 (30 дней): {upgraded3to8} игроков\n" +
                  $"• VIP 4 → VIP 9 (30 дней): {upgraded4to9} игроков\n" +
                  $"• VIP 5 → VIP 10 (30 дней): {upgraded5to10} игроков\n" +
                  $"━━━━━━━━━━━━━━━━━━━━\n" +
                  $"📈 *Всего обновлено:* {total} игроков\n\n" +
                  $"📋 *Новые бонусы:*\n" +
                  $"• VIP 8 → +{VipConfig.BonusTrophies[8]} кубков\n" +
                  $"• VIP 9 → +{VipConfig.BonusTrophies[9]} кубков\n" +
                  $"• VIP 10 → +{VipConfig.BonusTrophies[10]} кубков\n\n" +
                  $"⏰ *Срок действия:* 30 дней"
                : $"✅ *Повышение VIP для #{targetToken} завершено!*\n\n" +
                  $"📊 Повышение: VIP {(upgraded3to8 > 0 ? "3 → 8" : (upgraded4to9 > 0 ? "4 → 9" : "5 → 10"))}\n" +
                  $"⏰ *Срок действия:* 30 дней";

            await BotClient.SendMessage(chatId, resultMessage, parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        // ==================== РЕСУРСЫ ====================

        private static async Task HandleRefreshVip(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            bool hasTarget = args.Length >= 2;
            string targetToken = hasTarget ? args[1] : null;
            long targetId = 0;

            if (hasTarget)
            {
                try
                {
                    targetId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(targetToken);
                }
                catch
                {
                    targetId = -1;
                }

                if (targetId < 0)
                {
                    await BotClient.SendMessage(chatId, "❌ Неверный тег игрока. Пример: `/refreshvip #токен`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                    return;
                }
            }

            int inspected = 0;
            int updated = 0;
            int expired = 0;
            var processedIds = new HashSet<long>();

            bool RefreshAndSave(Account account)
            {
                if (account?.Avatar == null) return false;
                inspected++;
                int oldLevel = account.Avatar.VipLevel;
                bool changed = account.Avatar.RefreshVipBonusesFromConfig();
                if (oldLevel > 0 && account.Avatar.VipLevel == 0) expired++;
                if (changed)
                {
                    updated++;
                    Accounts.Save(account);
                }
                processedIds.Add(account.AccountId);
                return changed;
            }

            void RefreshOnline(HomeMode homeMode)
            {
                if (homeMode?.Avatar == null) return;
                var account = new Account
                {
                    AccountId = homeMode.Avatar.AccountId,
                    PassToken = homeMode.Avatar.PassToken,
                    Home = homeMode.Home,
                    Avatar = homeMode.Avatar
                };
                RefreshAndSave(account);
            }

            if (hasTarget && Sessions.IsSessionActive(targetId))
            {
                RefreshOnline(Sessions.GetSession(targetId)?.Home);
            }
            else if (hasTarget)
            {
                RefreshAndSave(Accounts.Load(targetId));
            }
            else
            {
                // Refresh live session objects first; otherwise a later login can
                // overwrite the newly calculated value with its old in-memory one.
                foreach (var session in Sessions.ActiveSessions.Values.ToArray())
                    RefreshOnline(session?.Home);

                foreach (var account in Accounts.GetAll())
                {
                    if (!processedIds.Contains(account.AccountId))
                        RefreshAndSave(account);
                }
            }

            if (hasTarget && inspected == 0)
            {
                await BotClient.SendMessage(chatId, $"❌ Игрок {targetToken} не найден.", cancellationToken: ct);
                return;
            }

            string scope = hasTarget ? $"игрока {targetToken}" : "всех игроков";
            await BotClient.SendMessage(chatId,
                $"✅ VIP-бонусы для {scope} обновлены.\n📊 Проверено: {inspected}\n🔄 Изменено: {updated}" +
                (expired > 0 ? $"\n⏰ Истекло VIP: {expired}" : ""),
                cancellationToken: ct);
        }

        private static async Task HandleVipBonus(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct,
            string[] args, bool setMode)
        {
            if (setMode && args.Length != 3)
            {
                await BotClient.SendMessage(chatId,
                    "Формат: /setvipbonus <уровень 1-10> <дополнительные кубки>\nПример: /setvipbonus 8 120",
                    cancellationToken: ct);
                return;
            }

            if (!setMode && args.Length != 1)
            {
                await BotClient.SendMessage(chatId,
                    "Формат: /vipbonus — показать значения\n/setvipbonus <уровень 1-10> <дополнительные кубки> — изменить значение",
                    cancellationToken: ct);
                return;
            }

            if (setMode)
            {
                if (!int.TryParse(args[1], out int level) || level < 1 || level > 10 ||
                    !int.TryParse(args[2], out int bonus) || bonus < 0 || bonus > 100000)
                {
                    await BotClient.SendMessage(chatId,
                        "Уровень должен быть от 1 до 10, бонус — от 0 до 100000 кубков.",
                        cancellationToken: ct);
                    return;
                }

                lock (VipBonusConfigLock)
                {
                    VipConfig.BonusTrophies[level] = bonus;
                    SyncVipBonusConfig();
                    SaveVipBonusConfig();
                }

                int refreshed = 0;
                foreach (var account in Accounts.GetAll())
                {
                    if (account?.Avatar == null || account.Avatar.VipLevel <= 0) continue;
                    if (account.Avatar.RefreshVipBonusesFromConfig())
                    {
                        Accounts.Save(account);
                        refreshed++;
                    }
                }

                await BotClient.SendMessage(chatId,
                    $"VIP {level}: бонус изменён на +{bonus} кубков. Обновлено активных аккаунтов: {refreshed}.",
                    cancellationToken: ct);
                return;
            }

            string values;
            lock (VipBonusConfigLock)
            {
                values = string.Join("\n", VipConfig.BonusTrophies.OrderBy(x => x.Key)
                    .Select(x => $"VIP {x.Key} — +{x.Value} кубков"));
            }
            await BotClient.SendMessage(chatId, "Текущие VIP-бонусы:\n" + values, cancellationToken: ct);
        }

        private static async Task HandleAddGems(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addgems #токен 1000", cancellationToken: ct);
                return;
            }

            string gemsToken = args[1];
            var gemsLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(gemsToken));

            if (gemsLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{gemsToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int gemsCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество гемов.", cancellationToken: ct);
                return;
            }

            var gemsAccount = Accounts.Load(gemsLink.AccountId);
            if (gemsAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            gemsAccount.Avatar.AddDiamonds(gemsCount);
            Accounts.Save(gemsAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {gemsCount} гемов игроку #{gemsToken}", cancellationToken: ct);
        }

        private static async Task HandleAddCoins(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addcoins #токен 1000", cancellationToken: ct);
                return;
            }

            string coinsToken = args[1];
            var coinsLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(coinsToken));

            if (coinsLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{coinsToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int coinsCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество монет.", cancellationToken: ct);
                return;
            }

            var coinsAccount = Accounts.Load(coinsLink.AccountId);
            if (coinsAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            coinsAccount.Avatar.AddGold(coinsCount);
            Accounts.Save(coinsAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {coinsCount} монет игроку #{coinsToken}", cancellationToken: ct);
        }

        private static async Task HandleAddPowerPoints(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addpowerpoints #токен 500", cancellationToken: ct);
                return;
            }

            string ppToken = args[1];
            var ppLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(ppToken));

            if (ppLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{ppToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int ppCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество очков силы.", cancellationToken: ct);
                return;
            }

            var ppAccount = Accounts.Load(ppLink.AccountId);
            if (ppAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            ppAccount.Avatar.AddPowerPoints(ppCount);
            Accounts.Save(ppAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {ppCount} очков силы игроку #{ppToken}", cancellationToken: ct);
        }

        private static async Task HandleAddBling(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addbling #токен 1000", cancellationToken: ct);
                return;
            }

            string blingToken = args[1];
            var blingLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(blingToken));

            if (blingLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{blingToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int blingCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество блингов.", cancellationToken: ct);
                return;
            }

            var blingAccount = Accounts.Load(blingLink.AccountId);
            if (blingAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            blingAccount.Avatar.AddBlings(blingCount);
            Accounts.Save(blingAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {blingCount} блингов игроку #{blingToken}", cancellationToken: ct);
        }

        private static async Task HandleAddStarPoints(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addstarpoints #токен 1000", cancellationToken: ct);
                return;
            }

            string spToken = args[1];
            var spLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(spToken));

            if (spLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{spToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int spCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество старпоинтов.", cancellationToken: ct);
                return;
            }

            var spAccount = Accounts.Load(spLink.AccountId);
            if (spAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            spAccount.Avatar.AddStarPoints(spCount);
            Accounts.Save(spAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {spCount} старпоинтов игроку #{spToken}", cancellationToken: ct);
        }

        private static async Task HandleRemGems(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /remgems #токен 1000", cancellationToken: ct);
                return;
            }

            string remGemsToken = args[1];
            var remGemsLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(remGemsToken));

            if (remGemsLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{remGemsToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int remGemsCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество гемов.", cancellationToken: ct);
                return;
            }

            var remGemsAccount = Accounts.Load(remGemsLink.AccountId);
            if (remGemsAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            remGemsAccount.Avatar.RevokeDiamonds(remGemsCount);
            Accounts.Save(remGemsAccount);

            await BotClient.SendMessage(chatId, $"✅ Снято {remGemsCount} гемов у #{remGemsToken}", cancellationToken: ct);
        }

        private static async Task HandleRemCoins(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /remcoins #токен 1000", cancellationToken: ct);
                return;
            }

            string remCoinsToken = args[1];
            var remCoinsLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(remCoinsToken));

            if (remCoinsLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{remCoinsToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int remCoinsCount))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество монет.", cancellationToken: ct);
                return;
            }

            var remCoinsAccount = Accounts.Load(remCoinsLink.AccountId);
            if (remCoinsAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            remCoinsAccount.Avatar.Gold -= remCoinsCount;
            if (remCoinsAccount.Avatar.Gold < 0) remCoinsAccount.Avatar.Gold = 0;
            Accounts.Save(remCoinsAccount);

            await BotClient.SendMessage(chatId, $"✅ Снято {remCoinsCount} монет у #{remCoinsToken}", cancellationToken: ct);
        }

        // ==================== BRAWL PASS ====================

        private static async Task HandleAddPass(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addpass #токен", cancellationToken: ct);
                return;
            }

            string passToken = args[1];
            var passLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(passToken));

            if (passLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{passToken} не найден.", cancellationToken: ct);
                return;
            }

            var passAccount = Accounts.Load(passLink.AccountId);
            if (passAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            passAccount.Home.HasPremiumPass = true;
            Accounts.Save(passAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдан Brawl Pass игроку #{passToken}", cancellationToken: ct);
        }

        private static async Task HandleAddPassPlus(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addpassplus #токен", cancellationToken: ct);
                return;
            }

            string plusToken = args[1];
            var plusLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(plusToken));

            if (plusLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{plusToken} не найден.", cancellationToken: ct);
                return;
            }

            var plusAccount = Accounts.Load(plusLink.AccountId);
            if (plusAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            plusAccount.Home.HasPremiumPass = true;
            plusAccount.Home.HasPremiumPassPlus = true;
            Accounts.Save(plusAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдан Brawl Pass Plus игроку #{plusToken}", cancellationToken: ct);
        }

        private static async Task HandleRemovePass(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /removepass #токен", cancellationToken: ct);
                return;
            }

            string removePassToken = args[1];
            var removePassLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(removePassToken));

            if (removePassLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{removePassToken} не найден.", cancellationToken: ct);
                return;
            }

            var removePassAccount = Accounts.Load(removePassLink.AccountId);
            if (removePassAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            removePassAccount.Home.HasPremiumPass = false;
            removePassAccount.Home.HasPremiumPassPlus = false;
            Accounts.Save(removePassAccount);

            await BotClient.SendMessage(chatId, $"✅ Brawl Pass снят с игрока #{removePassToken}", cancellationToken: ct);
        }

        // ==================== БОЙЦЫ И СКИНЫ ====================

        private static async Task HandleAddBrawler(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addbrawler #токен 16000001", cancellationToken: ct);
                return;
            }

            string brawlerToken = args[1];
            var brawlerLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(brawlerToken));

            if (brawlerLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{brawlerToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int brawlerId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID бойца.", cancellationToken: ct);
                return;
            }

            var brawlerAccount = Accounts.Load(brawlerLink.AccountId);
            if (brawlerAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            if (brawlerAccount.Avatar.HasHero(brawlerId))
            {
                await BotClient.SendMessage(chatId, $"⚠️ У игрока уже есть боец с ID {brawlerId}", cancellationToken: ct);
                return;
            }

            brawlerAccount.Avatar.UnlockHero(brawlerId);
            Accounts.Save(brawlerAccount);

            await BotClient.SendMessage(chatId, $"✅ Боец с ID {brawlerId} выдан игроку #{brawlerToken}", cancellationToken: ct);
        }

        private static async Task HandleRemoveBrawler(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /removebrawler #токен 16000001", cancellationToken: ct);
                return;
            }

            string removeBrawlerToken = args[1];
            var removeBrawlerLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(removeBrawlerToken));

            if (removeBrawlerLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{removeBrawlerToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int removeBrawlerId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID бойца.", cancellationToken: ct);
                return;
            }

            var removeBrawlerAccount = Accounts.Load(removeBrawlerLink.AccountId);
            if (removeBrawlerAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            if (!removeBrawlerAccount.Avatar.HasHero(removeBrawlerId))
            {
                await BotClient.SendMessage(chatId, $"⚠️ У игрока нет бойца с ID {removeBrawlerId}", cancellationToken: ct);
                return;
            }

            removeBrawlerAccount.Avatar.RemoveHero(removeBrawlerId);
            Accounts.Save(removeBrawlerAccount);

            await BotClient.SendMessage(chatId, $"✅ Боец с ID {removeBrawlerId} удалён у игрока #{removeBrawlerToken}", cancellationToken: ct);
        }

        private static async Task HandleAddAllBrawlers(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addallbrawlers #токен", cancellationToken: ct);
                return;
            }

            string allBrawlersToken = args[1];
            var allBrawlersLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(allBrawlersToken));

            if (allBrawlersLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{allBrawlersToken} не найден.", cancellationToken: ct);
                return;
            }

            var allBrawlersAccount = Accounts.Load(allBrawlersLink.AccountId);
            if (allBrawlersAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            int count = 0;
            for (int i = 1; i <= 80; i++)
            {
                int brawlerGlobalId = 16000000 + i;
                if (!allBrawlersAccount.Avatar.HasHero(brawlerGlobalId))
                {
                    allBrawlersAccount.Avatar.UnlockHero(brawlerGlobalId);
                    count++;
                }
            }
            Accounts.Save(allBrawlersAccount);

            await BotClient.SendMessage(chatId, $"✅ Выдано {count} новых бойцов игроку #{allBrawlersToken}", cancellationToken: ct);
        }

        private static async Task HandleAddSkin(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /addskin #токен 29000001", cancellationToken: ct);
                return;
            }

            string skinToken = args[1];
            var skinLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(skinToken));

            if (skinLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{skinToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int skinId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID скина.", cancellationToken: ct);
                return;
            }

            var skinAccount = Accounts.Load(skinLink.AccountId);
            if (skinAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            if (!skinAccount.Home.UnlockedSkins.Contains(skinId))
            {
                skinAccount.Home.UnlockedSkins.Add(skinId);
                Accounts.Save(skinAccount);
                await BotClient.SendMessage(chatId, $"✅ Скин с ID {skinId} выдан игроку #{skinToken}", cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"⚠️ У игрока уже есть скин с ID {skinId}", cancellationToken: ct);
            }
        }

        private static async Task HandleRemoveSkin(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат. Пример: /removeskin #токен 29000001", cancellationToken: ct);
                return;
            }

            string removeSkinToken = args[1];
            var removeSkinLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(removeSkinToken));

            if (removeSkinLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{removeSkinToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int removeSkinId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID скина.", cancellationToken: ct);
                return;
            }

            var removeSkinAccount = Accounts.Load(removeSkinLink.AccountId);
            if (removeSkinAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            if (removeSkinAccount.Home.UnlockedSkins.Contains(removeSkinId))
            {
                removeSkinAccount.Home.UnlockedSkins.Remove(removeSkinId);
                Accounts.Save(removeSkinAccount);
                await BotClient.SendMessage(chatId, $"✅ Скин с ID {removeSkinId} удалён у игрока #{removeSkinToken}", cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, $"⚠️ У игрока нет скина с ID {removeSkinId}", cancellationToken: ct);
            }
        }

        // ==================== ТРОФЕИ ====================

        private static async Task HandleKybki(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 4)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Неверный формат.\n📝 Пример: `/kybki #токен 1 100`", parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string trophyToken = args[1];
            long trophyAccountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(trophyToken);
            var trophyAccount = Accounts.Load(trophyAccountId);

            if (trophyAccount == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{trophyToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int trophyBrawlerId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID бойца.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[3], out int trophyChange))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное количество трофеев.", cancellationToken: ct);
                return;
            }

            int trophyBrawlerGlobalId = 16000000 + trophyBrawlerId;

            if (!trophyAccount.Avatar.HasHero(trophyBrawlerGlobalId))
            {
                await BotClient.SendMessage(chatId, $"❌ У игрока нет бойца с ID {trophyBrawlerId}.", cancellationToken: ct);
                return;
            }

            var trophyHero = trophyAccount.Avatar.GetHero(trophyBrawlerGlobalId);
            int trophyOld = trophyHero.Trophies;
            
            trophyHero.Trophies += trophyChange;
            if (trophyHero.Trophies < 0) trophyHero.Trophies = 0;
            
            if (trophyHero.Trophies > trophyHero.HighestTrophies)
                trophyHero.HighestTrophies = trophyHero.Trophies;
            
            Accounts.Save(trophyAccount);
            
            string trophyBrawlerName = trophyHero.CharacterData?.Name ?? $"ID:{trophyBrawlerId}";
            string trophySign = trophyChange >= 0 ? "+" : "";
            
            await BotClient.SendMessage(chatId, 
                $"✅ *Трофеи изменены!*\n\n👤 Игрок: #{trophyToken}\n🤖 Боец: {trophyBrawlerName}\n📊 Старые трофеи: {trophyOld}\n🔄 Изменение: {trophySign}{trophyChange}\n🏆 Новые трофеи: {trophyHero.Trophies}\n📈 Рекорд: {trophyHero.HighestTrophies}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleResetBrawler(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Неверный формат.\n📝 Примеры:\n`/resetbrawler #токен 1` - сбросить Шелли до 0\n`/resetbrawler #токен all 10000` - сбросить всех",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            string resetToken = args[1];
            long resetAccountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(resetToken);
            var resetAccount = Accounts.Load(resetAccountId);

            if (resetAccount == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{resetToken} не найден.", cancellationToken: ct);
                return;
            }

            if (args[2].ToLower() == "all")
            {
                int resetLimit = 10000;
                if (args.Length >= 4 && int.TryParse(args[3], out int customLimit))
                    resetLimit = customLimit;
                
                int beforeTrophies = resetAccount.Avatar.Trophies;
                int resetCount = resetAccount.Avatar.SilentSeasonReset(resetLimit);
                Accounts.Save(resetAccount);
                
                await BotClient.SendMessage(chatId, 
                    $"✅ *Сброс всех бойцов выполнен!*\n\n👤 Игрок: #{resetToken}\n📊 Было трофеев: {beforeTrophies}\n🎯 Лимит: {resetLimit}\n🔄 Сброшено бойцов: {resetCount}\n🏆 Стало трофеев: {resetAccount.Avatar.Trophies}",
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int resetBrawlerId))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный ID бойца.", cancellationToken: ct);
                return;
            }

            int resetToAmount = 0;
            if (args.Length >= 4 && int.TryParse(args[3], out int customReset))
                resetToAmount = customReset;

            int resetGlobalId = 16000000 + resetBrawlerId;

            if (!resetAccount.Avatar.HasHero(resetGlobalId))
            {
                await BotClient.SendMessage(chatId, $"❌ У игрока нет бойца с ID {resetBrawlerId}.", cancellationToken: ct);
                return;
            }

            var resetHero = resetAccount.Avatar.GetHero(resetGlobalId);
            int oldTrophies = resetHero.Trophies;
            
            resetAccount.Avatar.ResetBrawlerTrophies(resetGlobalId, resetToAmount);
            Accounts.Save(resetAccount);
            
            string resetBrawlerName = resetHero.CharacterData?.Name ?? $"ID:{resetBrawlerId}";
            
            await BotClient.SendMessage(chatId, 
                $"✅ *Трофеи сброшены!*\n\n👤 Игрок: #{resetToken}\n🤖 Боец: {resetBrawlerName}\n📊 Было трофеев: {oldTrophies}\n🎯 Сброшено до: {resetToAmount}",
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        private static async Task HandleSeasonReset(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, 
                    "❌ Использование:\n`/seasonreset #токен` - сброс игрока\n`/seasonreset all` - глобальный сброс", 
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }

            if (args[1] == "all")
            {
                await BotClient.SendMessage(chatId, "🔄 Глобальный сброс запущен...", cancellationToken: ct);
                
                var allAccounts = Accounts.GetAll();
                int totalReset = 0;
                int totalBlings = 0;
                int count = 0;
                
                foreach (var acc in allAccounts)
                {
                    if (acc == null) continue;
                    
                    var r = acc.Avatar.SeasonResetWithReward(6000);
                    if (r.totalBlings > 0)
                    {
                        Accounts.Save(acc);
                        totalReset += r.resetCount;
                        totalBlings += r.totalBlings;
                    }
                    count++;
                }
                
                await BotClient.SendMessage(chatId, 
                    $"✅ *Глобальный сброс завершён!*\n\n📊 Обработано: {count}\n🔄 Сброшено бойцов: {totalReset}\n✨ Выдано блингов: {totalBlings}", 
                    parseMode: ParseMode.Markdown, cancellationToken: ct);
                return;
            }
            
            long id = Laser.Logic.Util.LogicLongCodeGenerator.ToId(args[1]);
            var acc2 = Accounts.Load(id);
            
            if (acc2 == null)
            {
                await BotClient.SendMessage(chatId, "❌ Аккаунт не найден", cancellationToken: ct);
                return;
            }
            
            var r2 = acc2.Avatar.SeasonResetWithReward(6000);
            Accounts.Save(acc2);
            
            await BotClient.SendMessage(chatId, 
                $"✅ *Сброс выполнен!*\n\n🔄 Сброшено бойцов: {r2.resetCount}\n✨ Блингов: {r2.totalBlings}", 
                parseMode: ParseMode.Markdown, cancellationToken: ct);
        }

        // ==================== БАНЫ И МУТЫ ====================

        private static async Task HandleBan(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 4)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /ban #токен 1 2025-12-31 23:59:59 [причина]", cancellationToken: ct);
                return;
            }

            string banToken = args[1];
            var banLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(banToken));

            if (banLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{banToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int banID))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный BanID.", cancellationToken: ct);
                return;
            }

            if (!DateTime.TryParse(args[3], out DateTime banEndTime))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат даты. Используйте: yyyy-MM-dd HH:mm:ss", cancellationToken: ct);
                return;
            }

            var banAccount = Accounts.Load(banLink.AccountId);
            if (banAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            banAccount.Avatar.BanCount++;
            banAccount.Avatar.BanID = banID;
            banAccount.Avatar.BanEndTime = banEndTime;
            if (args.Length >= 5) banAccount.Avatar.TextReason = args[4];

            if (Sessions.IsSessionActive(banAccount.AccountId))
            {
                var session = Sessions.GetSession(banAccount.AccountId);
                if (session != null)
                {
                    session.GameListener?.SendTCPMessage(new AuthenticationFailedMessage()
                    {
                        ErrorCode = 1,
                        Message = "Please rejoin."
                    });
                    Sessions.Remove(banAccount.AccountId);
                }
            }

            Accounts.Save(banAccount);
            await BotClient.SendMessage(chatId, $"✅ Игрок #{banToken} заблокирован до {banEndTime:g}.", cancellationToken: ct);
        }

        private static async Task HandleUnban(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /unban #токен", cancellationToken: ct);
                return;
            }

            string unbanToken = args[1];
            var unbanLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(unbanToken));

            if (unbanLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{unbanToken} не найден.", cancellationToken: ct);
                return;
            }

            var unbanAccount = Accounts.Load(unbanLink.AccountId);
            if (unbanAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            unbanAccount.Avatar.BanEndTime = new DateTime(1999, 05, 01, 12, 30, 00);
            unbanAccount.Avatar.TextReason = null;
            Accounts.Save(unbanAccount);

            await BotClient.SendMessage(chatId, $"✅ Аккаунт #{unbanToken} разблокирован.", cancellationToken: ct);
        }

        private static async Task HandleMute(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /mute #токен 2025-12-31 23:59:59", cancellationToken: ct);
                return;
            }

            string muteToken = args[1];
            var muteLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(muteToken));

            if (muteLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{muteToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!DateTime.TryParse(args[2], out DateTime muteTime))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат времени.", cancellationToken: ct);
                return;
            }

            var muteAccount = Accounts.Load(muteLink.AccountId);
            if (muteAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            muteAccount.Avatar.MuteEndTime = muteTime;
            Accounts.Save(muteAccount);
            await BotClient.SendMessage(chatId, $"✅ Игрок #{muteToken} замучен до {muteTime:g}", cancellationToken: ct);
        }

        private static async Task HandleUnmute(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /unmute #токен", cancellationToken: ct);
                return;
            }

            string unmuteToken = args[1];
            var unmuteLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(unmuteToken));

            if (unmuteLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{unmuteToken} не найден.", cancellationToken: ct);
                return;
            }

            var unmuteAccount = Accounts.Load(unmuteLink.AccountId);
            if (unmuteAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            unmuteAccount.Avatar.MuteEndTime = new DateTime(1999, 05, 01, 12, 30, 00);
            Accounts.Save(unmuteAccount);

            await BotClient.SendMessage(chatId, $"✅ Игрок #{unmuteToken} размучен.", cancellationToken: ct);
        }

        private static async Task HandleSdBan(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /sdban #токен 2025-12-31 23:59:59", cancellationToken: ct);
                return;
            }

            string sdbanToken = args[1];
            var sdbanLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(sdbanToken));

            if (sdbanLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{sdbanToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!DateTime.TryParse(args[2], out DateTime sdbanEndTime))
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат даты.", cancellationToken: ct);
                return;
            }

            var sdbanAccount = Accounts.Load(sdbanLink.AccountId);
            if (sdbanAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            sdbanAccount.Avatar.ShowdownBanTime = sdbanEndTime;
            Accounts.Save(sdbanAccount);

            await BotClient.SendMessage(chatId, $"✅ Игрок #{sdbanToken} получил бан ШД до {sdbanEndTime:g}.", cancellationToken: ct);
        }

        private static async Task HandleSdUnban(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 2)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /sdunban #токен", cancellationToken: ct);
                return;
            }

            string sdunbanToken = args[1];
            var sdunbanLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(sdunbanToken));

            if (sdunbanLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{sdunbanToken} не найден.", cancellationToken: ct);
                return;
            }

            var sdunbanAccount = Accounts.Load(sdunbanLink.AccountId);
            if (sdunbanAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            sdunbanAccount.Avatar.ShowdownBanTime = new DateTime(1999, 05, 01, 12, 30, 00);
            Accounts.Save(sdunbanAccount);

            await BotClient.SendMessage(chatId, $"✅ Игрок #{sdunbanToken} разбанен в ШД.", cancellationToken: ct);
        }

        // ==================== ПРОЧЕЕ ====================

        private static async Task HandleSetName(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /setname #токен НовыйНик", cancellationToken: ct);
                return;
            }

            string nameToken = args[1];
            var nameLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(nameToken));

            if (nameLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{nameToken} не найден.", cancellationToken: ct);
                return;
            }

            string newName = args[2];
            var nameAccount = Accounts.Load(nameLink.AccountId);
            if (nameAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            string oldName = nameAccount.Avatar.Name;
            nameAccount.Avatar.Name = newName;

            if (nameAccount.Avatar.AllianceId > 0)
            {
                Alliance alliance = Alliances.Load(nameAccount.Avatar.AllianceId);
                var member = Alliances.GetMember(nameAccount.Avatar.AllianceId, nameAccount.AccountId);
                if (member != null)
                {
                    member.Avatar.Name = newName;
                    Alliances.Save(alliance);
                }
            }

            Accounts.Save(nameAccount);
            await BotClient.SendMessage(chatId, $"✅ Имя изменено с '{oldName}' на '{newName}' для #{nameToken}", cancellationToken: ct);
        }

        private static async Task HandleStreak(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length < 3)
            {
                await BotClient.SendMessage(chatId, "❌ Неверный формат.\nПример: /streak #токен 10", cancellationToken: ct);
                return;
            }

            string streakToken = args[1];
            var streakLink = Accounts.Load(Laser.Logic.Util.LogicLongCodeGenerator.ToId(streakToken));

            if (streakLink == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт #{streakToken} не найден.", cancellationToken: ct);
                return;
            }

            if (!int.TryParse(args[2], out int streakValue))
            {
                await BotClient.SendMessage(chatId, "❌ Неверное значение винстрика.", cancellationToken: ct);
                return;
            }

            var streakAccount = Accounts.Load(streakLink.AccountId);
            if (streakAccount == null)
            {
                await BotClient.SendMessage(chatId, "❌ Не удалось загрузить аккаунт.", cancellationToken: ct);
                return;
            }

            streakAccount.Avatar.WinStreak = streakValue;
            Accounts.Save(streakAccount);

            await BotClient.SendMessage(chatId, $"✅ Винстрик изменён на {streakValue} для #{streakToken}", cancellationToken: ct);
        }

        private static async Task HandleDebugAccountsList(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 1)
            {
                await BotClient.SendMessage(chatId, "❌ Использование: /debuglist", cancellationToken: ct);
                return;
            }

            List<Account> debugAccounts = Accounts.GetDebugAccounts();
            if (debugAccounts.Count == 0)
            {
                await BotClient.SendMessage(chatId, "ℹ️ Debug-аккаунтов нет.", cancellationToken: ct);
                return;
            }

            const int telegramSafeLimit = 3800;
            var messages = new List<string>();
            var page = new StringBuilder($"🧪 Debug-аккаунты: {debugAccounts.Count}\n\n");

            foreach (Account account in debugAccounts)
            {
                string tag = Laser.Logic.Util.LogicLongCodeGenerator.ToCode(account.AccountId);
                if (!tag.StartsWith('#')) tag = "#" + tag;
                bool infiniteAmmo = account.Avatar.InfiniteAmmo == true;
                bool infiniteUltimate = account.Avatar.InfiniteUltimate ?? account.Avatar.IsDebugAccount;
                string line = $"• {tag} — {account.Avatar.Name}\n" +
                              $"  Атака: {FormatSwitch(infiniteAmmo)}, ульта: {FormatSwitch(infiniteUltimate)}\n";

                if (page.Length + line.Length > telegramSafeLimit)
                {
                    messages.Add(page.ToString().TrimEnd());
                    page.Clear();
                    page.Append("🧪 Debug-аккаунты — продолжение:\n\n");
                }

                page.Append(line);
            }

            if (page.Length > 0)
                messages.Add(page.ToString().TrimEnd());

            foreach (string pageText in messages)
                await BotClient.SendMessage(chatId, pageText, cancellationToken: ct);
        }

        private static async Task HandleDebugAccount(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 2 && args.Length != 4)
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Форматы:\n" +
                    "/debug #ТЕГ — состояние\n" +
                    "/debug #ТЕГ ammo on|off — бесконечная атака\n" +
                    "/debug #ТЕГ ulti on|off — бесконечная ульта\n" +
                    "/debug #ТЕГ all on|off — обе возможности",
                    cancellationToken: ct);
                return;
            }

            if (!TryLoadAccountByTag(args[1], out string accountTag, out Account account))
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт {accountTag} не найден или тег некорректен.", cancellationToken: ct);
                return;
            }

            if (args.Length == 2)
            {
                bool infiniteAmmo = account.Avatar.InfiniteAmmo == true;
                bool infiniteUltimate = account.Avatar.InfiniteUltimate ?? account.Avatar.IsDebugAccount;
                await BotClient.SendMessage(
                    chatId,
                    $"🧪 {accountTag} — {account.Avatar.Name}\n" +
                    $"Debug: {FormatSwitch(account.Avatar.IsDebugAccount)}\n" +
                    $"Бесконечная атака: {FormatSwitch(infiniteAmmo)}\n" +
                    $"Бесконечная ульта: {FormatSwitch(infiniteUltimate)}",
                    cancellationToken: ct);
                return;
            }

            string option = args[2].ToLowerInvariant();
            if (!TryParseSwitch(args[3], out bool enabled) ||
                option is not ("ammo" or "attack" or "атака" or "ulti" or "ultimate" or "ульта" or "all"))
            {
                await BotClient.SendMessage(chatId, "❌ Пример: /debug #ТЕГ ulti on", cancellationToken: ct);
                return;
            }

            bool oldDebug = account.Avatar.IsDebugAccount;
            bool? oldInfiniteAmmo = account.Avatar.InfiniteAmmo;
            bool? oldInfiniteUltimate = account.Avatar.InfiniteUltimate;

            // Materialize legacy defaults before changing IsDebugAccount so enabling one
            // ability cannot accidentally enable the other through the legacy fallback.
            account.Avatar.InfiniteAmmo ??= false;
            account.Avatar.InfiniteUltimate ??= account.Avatar.IsDebugAccount;

            if (option is "ammo" or "attack" or "атака" or "all")
                account.Avatar.InfiniteAmmo = enabled;
            if (option is "ulti" or "ultimate" or "ульта" or "all")
                account.Avatar.InfiniteUltimate = enabled;
            if (enabled)
                account.Avatar.IsDebugAccount = true;

            if (!Accounts.TrySaveImmediate(account))
            {
                account.Avatar.IsDebugAccount = oldDebug;
                account.Avatar.InfiniteAmmo = oldInfiniteAmmo;
                account.Avatar.InfiniteUltimate = oldInfiniteUltimate;
                AccountCache.Cache(account);
                await BotClient.SendMessage(chatId, "❌ Не удалось сохранить debug-настройки в базе данных.", cancellationToken: ct);
                return;
            }

            DisconnectForDebugUpdate(account.AccountId);
            await BotClient.SendMessage(
                chatId,
                $"✅ Настройки {accountTag} сохранены. " +
                $"Бесконечная атака: {FormatSwitch(account.Avatar.InfiniteAmmo == true)}, " +
                $"бесконечная ульта: {FormatSwitch(account.Avatar.InfiniteUltimate ?? account.Avatar.IsDebugAccount)}.\n" +
                "Если игрок был онлайн, ему нужно войти заново.",
                cancellationToken: ct);
        }

        private static async Task HandleRemoveDebugAccount(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 2)
            {
                await BotClient.SendMessage(chatId, "❌ Использование: /undebug #ТЕГ", cancellationToken: ct);
                return;
            }

            if (!TryLoadAccountByTag(args[1], out string accountTag, out Account account))
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт {accountTag} не найден или тег некорректен.", cancellationToken: ct);
                return;
            }

            bool oldDebug = account.Avatar.IsDebugAccount;
            bool? oldInfiniteAmmo = account.Avatar.InfiniteAmmo;
            bool? oldInfiniteUltimate = account.Avatar.InfiniteUltimate;

            account.Avatar.IsDebugAccount = false;
            account.Avatar.InfiniteAmmo = false;
            account.Avatar.InfiniteUltimate = false;

            if (!Accounts.TrySaveImmediate(account))
            {
                account.Avatar.IsDebugAccount = oldDebug;
                account.Avatar.InfiniteAmmo = oldInfiniteAmmo;
                account.Avatar.InfiniteUltimate = oldInfiniteUltimate;
                AccountCache.Cache(account);
                await BotClient.SendMessage(chatId, "❌ Не удалось снять debug-статус в базе данных.", cancellationToken: ct);
                return;
            }

            DisconnectForDebugUpdate(account.AccountId);
            await BotClient.SendMessage(
                chatId,
                $"✅ Debug-статус снят с {accountTag} ({account.Avatar.Name}). Бесконечная атака и ульта отключены.",
                cancellationToken: ct);
        }

        private static bool TryLoadAccountByTag(string rawTag, out string accountTag, out Account account)
        {
            accountTag = rawTag?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!accountTag.StartsWith('#')) accountTag = "#" + accountTag;
            account = null;

            try
            {
                long accountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(accountTag);
                if (accountId <= 0) return false;
                account = Accounts.Load(accountId);
                return account != null;
            }
            catch
            {
                return false;
            }
        }

        private static void DisconnectForDebugUpdate(long accountId)
        {
            var session = Sessions.GetSession(accountId);
            if (session == null) return;

            session.GameListener?.SendTCPMessage(new AuthenticationFailedMessage
            {
                ErrorCode = 1,
                Message = "Настройки debug-аккаунта обновлены. Войдите в игру заново."
            });
            Sessions.Remove(accountId);
        }

        private static async Task HandleServerEvents(long chatId, CancellationToken ct, string[] args)
        {
            RuntimeControlState state = RuntimeControlManager.GetSnapshot();

            if (args.Length == 1)
            {
                await BotClient.SendMessage(
                    chatId,
                    "🎉 Серверные ивенты:\n" +
                    $"1. +300 монет за победу: {FormatSwitch(state.CoinsForWinEvent)}\n" +
                    $"2. +36 кубков за победу: {FormatSwitch(state.BonusTrophiesForWinEvent)}\n" +
                    $"3. +1 старрдроп за победу: {FormatSwitch(state.StarrDropForWinEvent)}\n\n" +
                    "Управление: /event <1-3> <on|off>",
                    cancellationToken: ct);
                return;
            }

            if (args.Length != 3 || !int.TryParse(args[1], out int eventId) || eventId < 1 || eventId > 3 ||
                !TryParseSwitch(args[2], out bool enabled))
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Неверный формат.\nПример: /event 1 on\nОтключение: /event 1 off",
                    cancellationToken: ct);
                return;
            }

            RuntimeControlManager.SetWinEvent(eventId, enabled);
            string eventName = eventId switch
            {
                1 => "+300 монет за победу",
                2 => "+36 кубков за победу",
                3 => "+1 старрдроп за победу",
                _ => "ивент"
            };

            await BotClient.SendMessage(
                chatId,
                $"✅ Ивент «{eventName}» {(enabled ? "включён" : "выключен")}.",
                cancellationToken: ct);
        }

        private static async Task HandleBotWhitelist(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (!IsBotOwner(message.From?.Id ?? 0))
            {
                await BotClient.SendMessage(chatId, "Access denied: only the bot owner can manage the whitelist.", cancellationToken: ct);
                return;
            }

            string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
            if (action == "list")
            {
                List<long> users;
                lock (BotWhitelistLock) users = botWhitelist.OrderBy(id => id).ToList();
                string result = users.Count == 0
                    ? "Telegram whitelist is empty."
                    : "Telegram whitelist:\n" + string.Join("\n", users.Select(id => $"• {id}"));
                await BotClient.SendMessage(chatId, result, cancellationToken: ct);
                return;
            }

            if (action is not ("add" or "remove") || args.Length > 3)
            {
                await BotClient.SendMessage(chatId,
                    "Usage: /whitelist list\n/whitelist add TELEGRAM_ID\n/whitelist remove TELEGRAM_ID\nYou can also reply to a user's message with /whitelist add.",
                    cancellationToken: ct);
                return;
            }

            long targetId;
            if (args.Length == 2 && message.ReplyToMessage?.From != null)
                targetId = message.ReplyToMessage.From.Id;
            else if (args.Length == 3 && long.TryParse(args[2], out long parsedId))
                targetId = parsedId;
            else
            {
                await BotClient.SendMessage(chatId, "Telegram ID must be a numeric ID (for example: /whitelist add 123456789).", cancellationToken: ct);
                return;
            }

            if (targetId <= 0 || IsBotOwner(targetId))
            {
                await BotClient.SendMessage(chatId, "This Telegram ID cannot be changed.", cancellationToken: ct);
                return;
            }

            bool changed;
            lock (BotWhitelistLock)
            {
                changed = action == "add" ? botWhitelist.Add(targetId) : botWhitelist.Remove(targetId);
            }
            if (changed) SaveBotWhitelist();

            await BotClient.SendMessage(chatId,
                changed
                    ? $"Whitelist {(action == "add" ? "updated: user added" : "updated: user removed")}: {targetId}"
                    : $"User {targetId} is already {(action == "add" ? "in" : "outside")} the whitelist.",
                cancellationToken: ct);
        }

        private static async Task HandleMaintenance(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length == 1 || string.Equals(args[1], "status", StringComparison.OrdinalIgnoreCase))
            {
                RuntimeControlState state = RuntimeControlManager.GetSnapshot();
                await BotClient.SendMessage(
                    chatId,
                    $"🛠 Техперерыв: {FormatSwitch(state.MaintenanceEnabled)}\n" +
                    $"Тегов в списке доступа: {state.MaintenanceAllowedAccountIds.Count}\n\n" +
                    "/maintenance on — включить\n" +
                    "/maintenance off — выключить\n" +
                    "/maintenance add #ТЕГ — разрешить вход\n" +
                    "/maintenance remove #ТЕГ — убрать доступ\n" +
                    "/maintenance list — список доступа",
                    cancellationToken: ct);
                return;
            }

            string action = args[1].ToLowerInvariant();
            if (action is "on" or "off" or "вкл" or "выкл")
            {
                if (args.Length != 2)
                {
                    await BotClient.SendMessage(chatId, "❌ Пример: /maintenance on", cancellationToken: ct);
                    return;
                }

                bool enabled = action is "on" or "вкл";
                RuntimeControlManager.SetMaintenance(enabled);
                await BotClient.SendMessage(
                    chatId,
                    enabled
                        ? "✅ Техперерыв включён. Все аккаунты вне списка доступа отключены; новые подключения для них заблокированы."
                        : "✅ Техперерыв выключен. Вход снова доступен всем аккаунтам.",
                    cancellationToken: ct);
                return;
            }

            if (action == "list")
            {
                RuntimeControlState state = RuntimeControlManager.GetSnapshot();
                if (state.MaintenanceAllowedAccountIds.Count == 0)
                {
                    await BotClient.SendMessage(chatId, "ℹ️ Список доступа техперерыва пуст.", cancellationToken: ct);
                    return;
                }

                var lines = new List<string>();
                foreach (long allowedAccountId in state.MaintenanceAllowedAccountIds.OrderBy(id => id))
                {
                    Account account = Accounts.Load(allowedAccountId);
                    string tag = Laser.Logic.Util.LogicLongCodeGenerator.ToCode(allowedAccountId);
                    if (!tag.StartsWith('#')) tag = "#" + tag;
                    lines.Add($"• {tag} — {account?.Avatar?.Name ?? "аккаунт не найден"}");
                }

                await BotClient.SendMessage(
                    chatId,
                    "🛠 Доступ во время техперерыва:\n" + string.Join("\n", lines),
                    cancellationToken: ct);
                return;
            }

            if (action is not ("add" or "remove") || args.Length != 3)
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Неверный формат. Используйте /maintenance для просмотра команд.",
                    cancellationToken: ct);
                return;
            }

            string accountTag = args[2].Trim().ToUpperInvariant();
            if (!accountTag.StartsWith('#')) accountTag = "#" + accountTag;

            long accountId;
            try
            {
                accountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(accountTag);
            }
            catch
            {
                await BotClient.SendMessage(chatId, "❌ Некорректный игровой тег.", cancellationToken: ct);
                return;
            }

            Account targetAccount = accountId > 0 ? Accounts.Load(accountId) : null;
            if (targetAccount == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт {accountTag} не найден.", cancellationToken: ct);
                return;
            }

            bool changed = action == "add"
                ? RuntimeControlManager.AddMaintenanceAccount(accountId)
                : RuntimeControlManager.RemoveMaintenanceAccount(accountId);

            string admin = message.From?.Username != null
                ? "@" + message.From.Username
                : message.From?.Id.ToString() ?? "unknown";
            Console.WriteLine($"[TelegramBot] Admin {admin} maintenance access {action}: {accountTag} ({accountId}), changed={changed}.");

            string result = action == "add"
                ? changed
                    ? $"✅ {accountTag} ({targetAccount.Avatar.Name}) добавлен в список доступа."
                    : $"ℹ️ {accountTag} уже находится в списке доступа."
                : changed
                    ? $"✅ {accountTag} ({targetAccount.Avatar.Name}) удалён из списка доступа."
                    : $"ℹ️ {accountTag} отсутствует в списке доступа.";

            await BotClient.SendMessage(chatId, result, cancellationToken: ct);
        }

        private static bool TryParseSwitch(string value, out bool enabled)
        {
            switch (value.Trim().ToLowerInvariant())
            {
                case "on":
                case "start":
                case "enable":
                case "вкл":
                    enabled = true;
                    return true;
                case "off":
                case "stop":
                case "disable":
                case "выкл":
                    enabled = false;
                    return true;
                default:
                    enabled = false;
                    return false;
            }
        }

        private static string FormatSwitch(bool enabled)
        {
            return enabled ? "🟢 включён" : "🔴 выключен";
        }

        private static async Task HandleSetPassword(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 3)
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Неверный формат.\nПример: /setpassword #ТЕГ НовыйПароль",
                    cancellationToken: ct);
                return;
            }

            string accountTag = args[1].Trim().ToUpperInvariant();
            if (!accountTag.StartsWith('#'))
            {
                accountTag = "#" + accountTag;
            }

            long accountId;
            try
            {
                accountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(accountTag);
            }
            catch
            {
                await BotClient.SendMessage(chatId, "❌ Некорректный игровой тег.", cancellationToken: ct);
                return;
            }

            if (accountId <= 0)
            {
                await BotClient.SendMessage(chatId, "❌ Некорректный игровой тег.", cancellationToken: ct);
                return;
            }

            var account = Accounts.Load(accountId);
            if (account == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт {accountTag} не найден.", cancellationToken: ct);
                return;
            }

            string newPassword = args[2];
            if (newPassword.Length < 4 || newPassword.Length > 64)
            {
                await BotClient.SendMessage(chatId, "❌ Пароль должен содержать от 4 до 64 символов и не иметь пробелов.", cancellationToken: ct);
                return;
            }

            bool wasLinked = !string.IsNullOrEmpty(account.Avatar.Password);
            string oldPassword = account.Avatar.Password;
            account.Avatar.Password = newPassword;
            if (!Accounts.TrySaveImmediate(account))
            {
                // Accounts.Load commonly returns the cached instance, so restore it as well
                // when persistence fails instead of leaving a cache-only password behind.
                account.Avatar.Password = oldPassword;
                AccountCache.Cache(account);
                await BotClient.SendMessage(
                    chatId,
                    "❌ Не удалось сохранить новый пароль в базе данных. Пароль не изменён.",
                    cancellationToken: ct);
                return;
            }

            string admin = message.From?.Username != null
                ? "@" + message.From.Username
                : message.From?.Id.ToString() ?? "unknown";
            Console.WriteLine($"[TelegramBot] Admin {admin} changed the password for account {accountTag} ({account.AccountId}).");

            string action = wasLinked ? "изменён" : "создан и установлен";
            await BotClient.SendMessage(
                chatId,
                $"✅ Пароль {action} для {accountTag} ({account.Avatar.Name}).",
                cancellationToken: ct);
        }

        private static async Task HandleKick(Telegram.Bot.Types.Message message, long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 2)
            {
                await BotClient.SendMessage(
                    chatId,
                    "❌ Неверный формат.\nПример: /kick #ТЕГ",
                    cancellationToken: ct);
                return;
            }

            string accountTag = args[1].Trim().ToUpperInvariant();
            if (!accountTag.StartsWith('#'))
            {
                accountTag = "#" + accountTag;
            }

            long accountId;
            try
            {
                accountId = Laser.Logic.Util.LogicLongCodeGenerator.ToId(accountTag);
            }
            catch
            {
                await BotClient.SendMessage(chatId, "❌ Некорректный игровой тег.", cancellationToken: ct);
                return;
            }

            var account = accountId > 0 ? Accounts.Load(accountId) : null;
            if (account == null)
            {
                await BotClient.SendMessage(chatId, $"❌ Аккаунт {accountTag} не найден.", cancellationToken: ct);
                return;
            }

            // Every installation stores the same account PassToken. Rotating it is what logs
            // out devices which are currently offline: their old token will be rejected on
            // the next connection attempt.
            string oldPassToken = account.PassToken;
            string oldAvatarPassToken = account.Avatar.PassToken;
            string newPassToken = Helpers.GenerateToken(account.AccountId);
            account.PassToken = newPassToken;
            account.Avatar.PassToken = newPassToken;

            if (!Accounts.TrySaveImmediate(account))
            {
                account.PassToken = oldPassToken;
                account.Avatar.PassToken = oldAvatarPassToken;
                AccountCache.Cache(account);
                await BotClient.SendMessage(
                    chatId,
                    "❌ Не удалось отозвать устройства: новый ключ входа не сохранился в базе данных.",
                    cancellationToken: ct);
                return;
            }

            var session = Sessions.GetSession(accountId);
            bool wasOnline = session != null;
            if (session != null)
            {
                session.GameListener?.SendTCPMessage(new AuthenticationFailedMessage
                {
                    ErrorCode = 1,
                    Message = "Все устройства аккаунта были отключены администратором. Войдите заново с помощью тега и пароля."
                });
            }

            // Removing the account session also makes the connection cleanup loop close every
            // remaining connection authenticated as this account.
            Sessions.Remove(accountId);

            string admin = message.From?.Username != null
                ? "@" + message.From.Username
                : message.From?.Id.ToString() ?? "unknown";
            Console.WriteLine($"[TelegramBot] Admin {admin} revoked all device credentials for account {accountTag} ({account.AccountId}). Online: {wasOnline}.");

            string recoveryHint = string.IsNullOrEmpty(account.Avatar.Password)
                ? "\n⚠️ У аккаунта не установлен пароль. Перед повторным входом задайте его через /setpassword."
                : "\nДля повторного входа используйте тег и пароль через /login.";
            string onlineStatus = wasOnline
                ? "Активная сессия отключена."
                : "Аккаунт был не в сети; старые ключи будут отклонены при следующем подключении.";

            await BotClient.SendMessage(
                chatId,
                $"✅ Все устройства аккаунта {accountTag} отключены. {onlineStatus}{recoveryHint}",
                cancellationToken: ct);
        }

        private static async Task HandleOnline(long chatId, CancellationToken ct, string[] args)
        {
            if (args.Length != 1)
            {
                await BotClient.SendMessage(chatId, "❌ Использование: /online", cancellationToken: ct);
                return;
            }

            var onlinePlayers = Sessions.ActiveSessions?.Values
                .Where(session => session?.Home?.Avatar != null)
                .Select(session => session.Home.Avatar)
                .OrderBy(avatar => avatar.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(avatar => avatar.AccountId)
                .ToArray() ?? Array.Empty<ClientAvatar>();

            if (onlinePlayers.Length == 0)
            {
                await BotClient.SendMessage(chatId, "👥 Сейчас на сервере нет игроков.", cancellationToken: ct);
                return;
            }

            const int telegramSafeLimit = 3800;
            var messages = new List<string>();
            var page = new StringBuilder($"👥 Онлайн: {onlinePlayers.Length}\n\n");

            for (int i = 0; i < onlinePlayers.Length; i++)
            {
                ClientAvatar avatar = onlinePlayers[i];
                string tag;
                try
                {
                    tag = Laser.Logic.Util.LogicLongCodeGenerator.ToCode(avatar.AccountId);
                    if (!tag.StartsWith('#')) tag = "#" + tag;
                }
                catch
                {
                    tag = avatar.AccountId.ToString();
                }

                string status = avatar.BattleId > 0
                    ? "⚔️ в бою"
                    : avatar.TeamId > 0
                        ? "👥 в команде"
                        : "🏠 в меню";
                string line = $"{i + 1}. {tag} — {avatar.Name} — {status}\n";

                if (page.Length + line.Length > telegramSafeLimit)
                {
                    messages.Add(page.ToString().TrimEnd());
                    page.Clear();
                    page.Append("👥 Онлайн — продолжение:\n\n");
                }

                page.Append(line);
            }

            if (page.Length > 0)
                messages.Add(page.ToString().TrimEnd());

            foreach (string pageText in messages)
                await BotClient.SendMessage(chatId, pageText, cancellationToken: ct);
        }

        private static async Task HandleBackup(long chatId, CancellationToken ct)
        {
            if (!await BackupSemaphore.WaitAsync(0, ct))
            {
                await BotClient.SendMessage(chatId, "⏳ Другой бэкап уже создаётся.", cancellationToken: ct);
                return;
            }

            string stagingDirectory = null;
            string archivePath = null;

            try
            {
                await BotClient.SendMessage(chatId, "⏳ Создаю полный бэкап сервера...", cancellationToken: ct);

                // Persist in-memory changes before PostgreSQL is dumped.
                AccountCache.SaveAll();
                AllianceCache.SaveAll();

                string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                string backupsDirectory = Path.Combine(AppContext.BaseDirectory, "backups");
                Directory.CreateDirectory(backupsDirectory);

                stagingDirectory = Path.Combine(backupsDirectory, ".backup_" + timestamp);
                Directory.CreateDirectory(stagingDirectory);

                string databaseDumpPath = Path.Combine(stagingDirectory, "database.dump");
                var config = GromCore.Laser.Server.Settings.Configuration.Instance;

                var processInfo = new ProcessStartInfo
                {
                    FileName = "pg_dump",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                processInfo.ArgumentList.Add("--format=custom");
                processInfo.ArgumentList.Add("--no-owner");
                processInfo.ArgumentList.Add("--no-privileges");
                processInfo.ArgumentList.Add("--host");
                processInfo.ArgumentList.Add(config.DatabaseIP);
                processInfo.ArgumentList.Add("--username");
                processInfo.ArgumentList.Add(config.DatabaseUsername);
                processInfo.ArgumentList.Add("--file");
                processInfo.ArgumentList.Add(databaseDumpPath);
                processInfo.ArgumentList.Add(config.DatabaseName);
                processInfo.Environment["PGPASSWORD"] = config.DatabasePassword;

                using (var process = Process.Start(processInfo))
                {
                    if (process == null)
                        throw new InvalidOperationException("Не удалось запустить pg_dump.");

                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    string processError = await errorTask;

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"pg_dump завершился с кодом {process.ExitCode}: {processError.Trim()}");
                }

                if (!File.Exists(databaseDumpPath) || new FileInfo(databaseDumpPath).Length == 0)
                    throw new InvalidOperationException("pg_dump не создал файл базы данных.");

                string[] additionalFiles =
                {
                    "custom_offers.json",
                    "promocodes.json",
                    "battle_player_map.json",
                    "gameplay.json",
                    "runtime_control.json"
                };

                foreach (string fileName in additionalFiles)
                {
                    string sourcePath = Path.Combine(AppContext.BaseDirectory, fileName);
                    if (File.Exists(sourcePath))
                        File.Copy(sourcePath, Path.Combine(stagingDirectory, fileName), overwrite: true);
                }

                archivePath = Path.Combine(backupsDirectory, $"gromcore_backup_{timestamp}.zip");
                ZipFile.CreateFromDirectory(stagingDirectory, archivePath, CompressionLevel.Optimal, includeBaseDirectory: false);

                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(archivePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }

                var archive = new FileInfo(archivePath);
                double sizeMb = archive.Length / 1024d / 1024d;
                await BotClient.SendMessage(
                    chatId,
                    $"✅ Бэкап создан.\n📁 {archive.FullName}\n💾 Размер: {sizeMb:F2} МБ",
                    cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Backup error: {ex}");

                if (!string.IsNullOrEmpty(archivePath) && File.Exists(archivePath))
                    File.Delete(archivePath);

                string error = ex.Message;
                if (error.Length > 700) error = error.Substring(0, 700) + "...";
                string hint = error.Contains("pg_dump", StringComparison.OrdinalIgnoreCase)
                    ? "\nУстанови PostgreSQL client: sudo apt install postgresql-client"
                    : string.Empty;

                await BotClient.SendMessage(chatId, $"❌ Не удалось создать бэкап: {error}{hint}", cancellationToken: ct);
            }
            finally
            {
                if (!string.IsNullOrEmpty(stagingDirectory) && Directory.Exists(stagingDirectory))
                    Directory.Delete(stagingDirectory, recursive: true);

                BackupSemaphore.Release();
            }
        }

        // ==================== МЕНЮ ====================

        private static async Task ShowCallbackAccessDenied(long chatId, int messageId, CancellationToken ct)
        {
            try
            {
                await BotClient.EditMessageText(chatId, messageId,
                    "⛔ У вас нет доступа к этому разделу.",
                    replyMarkup: GetBackButton(), cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Не удалось показать отказ в доступе: {ex.Message}");
            }
        }

        private static async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken ct)
        {
            if (callbackQuery == null) return;

            try
            {
                // Always acknowledge button presses so Telegram stops showing the
                // loading spinner, including when a callback later fails.
                await BotClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Не удалось подтвердить кнопку: {ex.Message}");
            }

            if (callbackQuery.Message == null) return;
            var chatId = callbackQuery.Message.Chat.Id;
            var messageId = callbackQuery.Message.MessageId;
            bool isAdmin = IsBotOperator(callbackQuery.From.Id);

            try
            {
                switch (callbackQuery.Data)
                {
                    case "main_menu":
                        await ShowMainMenu(chatId, callbackQuery.From?.Id ?? 0, ct);
                        break;
                    case "user_help":
                        await ShowUserHelp(chatId, ct);
                        break;
                    case "admin_panel":
                        if (!isAdmin)
                        {
                            await ShowCallbackAccessDenied(chatId, messageId, ct);
                            return;
                        }
                        await ShowAdminMenu(chatId, ct);
                        break;
                    case "admin_promos":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "🎟 Промокоды",
                            "Управление промокодами:\n\n" +
                            "`/crpromo КОД ЛИМИТ НАГРАДЫ` — создать\n" +
                            "`/listpromo` — список\n" +
                            "`/promoinfo КОД` — подробности\n" +
                            "`/promovalidate КОД` — проверка и исправление\n" +
                            "`/delpromo КОД` — удалить",
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_offers":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "🎁 Акции",
                            "Управление магазином:\n\n" +
                            "`/crtemp` — создать акцию пошагово\n" +
                            "`/listtemp` — список акций\n" +
                            "`/offertemp ID` — информация\n" +
                            "`/activatetemp ID` / `/deactivatetemp ID`\n" +
                            "`/deltemp ID` — удалить\n" +
                            "`/refreshoffers` — обновить у игроков",
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_players":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "👤 Игроки", BuildPlayerCommandsSection(),
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_resources":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "💰 Ресурсы", BuildResourceCommandsSection(),
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_server":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "🖥 Сервер",
                            "Сервисные команды:\n\n" +
                            "`/events` — события\n" +
                            "`/maintenance` — техперерыв\n" +
                            "`/backup` — резервная копия\n" +
                            "`/maps` — карты\n" +
                            "`/debuglist` — debug-аккаунты",
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_themes":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowAdminSection(chatId, messageId, "🖼 Оформление",
                            "Управление фоном главного экрана:\n\n" +
                            "`/themes` — список ID и названий\n" +
                            "`/settheme ID` — установить всем\n" +
                            "`/settheme ID TAG` — установить одному игроку\n\n" +
                            "Пример: `/settheme 88`",
                            new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Панель", "admin_panel") } }, ct);
                        break;
                    case "admin_help":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowCommandPage(chatId, messageId, 0, ct);
                        break;
                    case "admin_commands":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        await ShowCommandPage(chatId, messageId, 0, ct);
                        break;
                    case "admin_commands_prev":
                    case "admin_commands_next":
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        // Legacy navigation callbacks do not carry a page number;
                        // keep them safe by returning to the first page.
                        await ShowCommandPage(chatId, messageId,
                            callbackQuery.Data.EndsWith("next", StringComparison.OrdinalIgnoreCase) ? 1 : 0, ct);
                        break;
                    case var commandPage when commandPage != null && commandPage.StartsWith("admin_commands_page_"):
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        int page = int.TryParse(commandPage.Substring("admin_commands_page_".Length), out int parsedPage) ? parsedPage : 0;
                        await ShowCommandPage(chatId, messageId, page, ct);
                        break;
                    case var commandCallback when commandCallback != null && commandCallback.StartsWith("admin_cmd_"):
                        if (!isAdmin) { await ShowCallbackAccessDenied(chatId, messageId, ct); return; }
                        string[] commandParts = commandCallback.Substring("admin_cmd_".Length).Split('_');
                        string commandName = commandParts[0];
                        int commandPageNumber = commandParts.Length > 1 && int.TryParse(commandParts[^1], out int commandPageValue) ? commandPageValue : 0;
                        await ShowCommandInfo(chatId, messageId, commandName, commandPageNumber, ct);
                        break;
                    default:
                        await BotClient.EditMessageText(chatId, messageId, "❌ Неизвестная команда.", replyMarkup: GetBackButton(), cancellationToken: ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в HandleCallbackQuery: {ex}");
                try
                {
                    await BotClient.EditMessageText(chatId, messageId,
                        "❌ Не удалось обработать кнопку. Откройте /start и попробуйте ещё раз.",
                        replyMarkup: GetBackButton(), cancellationToken: ct);
                }
                catch { }
            }
        }

        private static async Task ShowUserHelp(long chatId, CancellationToken ct)
        {
            var publicCommands = AdminCommandCatalog.Where(item => !item.RequiresAdmin).ToArray();
            var text = new StringBuilder("📘 *Справка GromCore Bot*\n\n");
            text.AppendLine("Доступные команды:");
            foreach (var item in publicCommands)
            {
                text.AppendLine($"`{item.Usage}` — {item.Description}.");
                text.AppendLine($"Пример: `{item.Usage}`");
                text.AppendLine($"Права: {item.Access}.\n");
            }

            text.AppendLine("Если промокод не активируется, отправьте администратору точный текст ошибки и игровой тег.");
            await BotClient.SendMessage(chatId, text.ToString(),
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("🏠 Главное меню", "main_menu") }
                }), cancellationToken: ct);
        }

        private static async Task ShowAdminSection(long chatId, int messageId, string title, string body,
            InlineKeyboardButton[][] buttons, CancellationToken ct)
        {
            string text = $"*{title}*\n\n{body}";
            await BotClient.EditMessageText(chatId, messageId, text,
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: ct);
        }

        private static string BuildPlayerCommandsSection()
        {
            var categories = new[] { "👤 Игроки", "🎫 Пропуск", "🎨 Предметы", "🛡 Модерация" };
            var text = new StringBuilder(
                "Все команды ниже требуют права администратора или оператора из whitelist.\n" +
                "Вместо `TAG` используйте игровой тег игрока.\n\n");

            foreach (string category in categories)
            {
                var commands = GetAdminCommandCatalog()
                    .Where(item => item.Category == category)
                    .ToArray();
                if (commands.Length == 0) continue;

                text.AppendLine(category + ":");
                foreach (var command in commands)
                    text.AppendLine($"`{command.Usage}` — {command.Description}");
                text.AppendLine();
            }

            text.AppendLine("Примеры:");
            text.AppendLine("`/setvip #ABC123 5 30` — VIP 5 на 30 дней");
            text.AppendLine("`/addbrawler #ABC123 16000001` — выдать бойца");
            text.Append("`/addskin #ABC123 29000001` — выдать скин");
            return text.ToString();
        }

        private static string BuildResourceCommandsSection()
        {
            var text = new StringBuilder(
                "Выдача и списание ресурсов игрока. Все команды требуют права администратора или оператора из whitelist.\n" +
                "Вместо `TAG` используйте игровой тег игрока.\n\n");
            foreach (var command in GetAdminCommandCatalog().Where(item => item.Category == "💰 Ресурсы"))
                text.AppendLine($"`{command.Usage}` — {command.Description}");

            text.AppendLine();
            text.AppendLine("Примеры:");
            text.AppendLine("`/addgems #ABC123 500` — выдать 500 гемов");
            text.AppendLine("`/addcoins #ABC123 10000` — выдать 10 000 монет");
            text.AppendLine("`/addpowerpoints #ABC123 1000` — выдать очки силы");
            text.Append("`/remgems #ABC123 100` — списать гемы");
            return text.ToString();
        }

        private static async Task ShowCommandPage(long chatId, int messageId, int page, CancellationToken ct)
        {
            const int pageSize = 8;
            var catalog = GetAdminCommandCatalog().ToArray();
            int pageCount = Math.Max(1, (catalog.Length + pageSize - 1) / pageSize);
            page = Math.Clamp(page, 0, pageCount - 1);

            var items = catalog.Skip(page * pageSize).Take(pageSize).ToArray();
            var rows = new List<InlineKeyboardButton[]>();
            foreach (var item in items)
                rows.Add(new[] { InlineKeyboardButton.WithCallbackData($"{item.Category}  /{item.Name}", $"admin_cmd_{item.Name}_{page}") });

            var navigation = new List<InlineKeyboardButton>();
            if (page > 0) navigation.Add(InlineKeyboardButton.WithCallbackData("◀️ Назад", $"admin_commands_page_{page - 1}"));
            navigation.Add(InlineKeyboardButton.WithCallbackData($"{page + 1}/{pageCount}", $"admin_commands_page_{page}"));
            if (page < pageCount - 1) navigation.Add(InlineKeyboardButton.WithCallbackData("Вперёд ▶️", $"admin_commands_page_{page + 1}"));
            rows.Add(navigation.ToArray());
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Админ-панель", "admin_panel") });

            string text = $"*📚 Все команды*\nСтраница {page + 1} из {pageCount}. Нажмите команду, чтобы увидеть формат, пример и права.";
            var markup = new InlineKeyboardMarkup(rows);
            if (messageId > 0)
            {
                await BotClient.EditMessageText(chatId, messageId, text,
                    parseMode: ParseMode.Markdown, replyMarkup: markup, cancellationToken: ct);
            }
            else
            {
                await BotClient.SendMessage(chatId, text,
                    parseMode: ParseMode.Markdown, replyMarkup: markup, cancellationToken: ct);
            }
        }

        private static async Task ShowCommandInfo(long chatId, int messageId, string commandName, int page, CancellationToken ct)
        {
            var item = GetAdminCommandCatalog().FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
            if (item == null) return;
            await BotClient.EditMessageText(chatId, messageId,
                $"*{item.Category}*\n\n" +
                $"Название: `/{item.Name}`\n" +
                $"Описание: {item.Description}.\n\n" +
                $"Пример использования: `{item.Usage}`\n" +
                $"Права и ограничения: {item.Access}.\n\n" +
                "Команда выполняется после отправки текста в чат.",
                parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ К списку команд", $"admin_commands_page_{page}") },
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ Админ-панель", "admin_panel") }
                }), cancellationToken: ct);
        }

        private static async Task ShowCleanMainMenu(long chatId, long actorId, CancellationToken ct)
        {
            bool isAdmin = IsBotOperator(actorId);
            var rows = new List<InlineKeyboardButton[]>
            {
                new[] { InlineKeyboardButton.WithCallbackData("📖 Справка", "user_help") }
            };
            if (isAdmin)
                rows.Insert(0, new[] { InlineKeyboardButton.WithCallbackData("🛠 Админ-панель", "admin_panel") });

            string text = isAdmin
                ? "*GromCore Bot*\n\nДобро пожаловать в панель управления сервером.\nВыберите нужный раздел ниже."
                : "*GromCore Bot*\n\nБот управления игровым сервером.\nНажмите «Справка», если нужна помощь.";
            await BotClient.SendMessage(chatId, text, parseMode: ParseMode.Markdown,
                replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
        }

        private static async Task ShowLegacyAdminMenu(long chatId, CancellationToken ct)
        {
string adminText = """
⚙️ АДМИН ПАНЕЛЬ

📋 Доступные команды:

━━━━━━━━━━━━━━━━━━━━
🎟️ АКЦИИ:
/crtemp - создать акцию (пошагово)
/listtemp - список всех акций
/offertemp <id> - информация об акции
/edittemp <id> <поле> <значение> - редактировать
/deltemp <id> - удалить акцию
/activatetemp <id> - активировать
/deactivatetemp <id> - деактивировать
/refreshoffers - принудительно обновить акции

━━━━━━━━━━━━━━━━━━━━
🎫 Промокоды:
/crpromo <код> <лимит> <награды> - создать промокод
/listpromo - список промокодов
/promoinfo <код> - инфо о промокоде
/delpromo <код> - удалить промокод

👑 VIP система:
/setvip #токен <уровень> [дни] - выдать VIP
/extendvip #токен <дни> - продлить VIP
/vipinfo #токен - инфо о VIP
/upgradevip - повысить VIP 3-5 → 8-10
/refreshvip [#токен] - пересчитать бонусы выданных VIP по текущей конфигурации

🏆 Трофеи:
/kybki #токен ID_бойца кол-во
/resetbrawler #токен ID [лимит]
/seasonreset #токен или all

👥 Бойцы:
/addbrawler #токен ID
/removebrawler #токен ID
/addallbrawlers #токен

🎨 Скины:
/addskin #токен ID
/removeskin #токен ID

💰 Ресурсы:
/addgems #токен кол-во
/addcoins #токен кол-во
/addpowerpoints #токен кол-во
/addbling #токен кол-во
/addstarpoints #токен кол-во
/remgems #токен кол-во
/remcoins #токен кол-во

🎫 Brawl Pass:
/addpass #токен
/addpassplus #токен
/removepass #токен

🔨 Баны/Муты:
/ban #токен ID дата [причина]
/unban #токен
/mute #токен дата
/unmute #токен
/sdban #токен дата
/sdunban #токен

🗺 Карты:
/maps - активные слоты и карты
/maps <слот> [страница] - карты режима
/setmap <слот> <ID карты> - сменить карту

🎉 Серверные ивенты:
/events - статус трёх ивентов
/event <1-3> <on|off> - включить или выключить

🛠 Техперерыв:
/maintenance - статус и справка
/maintenance on|off - управление
/maintenance add|remove #тег - доступ
/maintenance list - белый список

🧪 Debug-аккаунты:
/debuglist - список debug-аккаунтов
/debug #тег - текущее состояние
/debug #тег ammo on|off - бесконечная атака
/debug #тег ulti on|off - бесконечная ульта
/debug #тег all on|off - обе возможности
/undebug #тег - снять debug-статус

📝 Прочее:
/online - текущий онлайн и список игроков
/setname #токен ник
/streak #токен значение
/setpassword #тег новый_пароль
/kick #тег - отключить все устройства, включая офлайн
/backup - создать полный бэкап

━━━━━━━━━━━━━━━━━━━━
💡 Пример создания акции:
1. /crtemp
2. Введите награды: gems:100 coins:5000 skin:29000001
3. Введите готово
4. Введите название
5. Введите описание или пропустить
6. Введите цену: 99 gems или 0
7. Введите длительность: 7 дней
8. Введите фон или пропустить
/promovalidate <код> — проверить и исправить ID наград старого промокода
/cancel — отменить любой активный мастер
""";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Главное меню", "main_menu") }
            });
            await BotClient.SendMessage(chatId, adminText, replyMarkup: keyboard, cancellationToken: ct);
        }

        private static async Task ShowCleanAdminMenu(long chatId, CancellationToken ct)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("🎟 Промокоды", "admin_promos"), InlineKeyboardButton.WithCallbackData("🎁 Акции", "admin_offers") },
                new[] { InlineKeyboardButton.WithCallbackData("👤 Игроки", "admin_players"), InlineKeyboardButton.WithCallbackData("💰 Ресурсы", "admin_resources") },
                new[] { InlineKeyboardButton.WithCallbackData("🖥 Сервер", "admin_server") },
                new[] { InlineKeyboardButton.WithCallbackData("🖼 Оформление", "admin_themes") },
                new[] { InlineKeyboardButton.WithCallbackData("📚 Все команды", "admin_help") },
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Главное меню", "main_menu") }
            });
            await BotClient.SendMessage(chatId,
                "*🛠 Админ-панель*\n\nВыберите раздел. Команды и ошибки теперь отображаются в понятном формате.",
                parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
        }

        public static async Task ShowMainMenu(long chatId, CancellationToken ct)
        {
            await ShowMainMenu(chatId, chatId, ct);
        }

        private static async Task ShowMainMenu(long chatId, long actorId, CancellationToken ct)
        {
            await ShowCleanMainMenu(chatId, actorId, ct);
            return;
#if LEGACY_MENU
            try
            {
                var buttons = new List<List<InlineKeyboardButton>>();
                
                if (IsBotOperator(chatId))
                {
                    buttons.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData("⚙️ Админ панель", "admin_panel")
                    });
                }
                
                var keyboard = new InlineKeyboardMarkup(buttons);

                string messageText = "🔹 *GromCore Laser Bot*\n\n🤖 Бот для управления игровым сервером\n\n";
                
                if (IsBotOperator(chatId))
                {
                    messageText += "⚙️ *Вы вошли как администратор*\nНажмите 'Админ панель' или используйте /admin для доступа к командам";
                }
                else
                {
                    messageText += "❌ У вас нет доступа к этому боту.";
                }

                await BotClient.SendMessage(chatId, messageText, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в ShowMainMenu: {ex.Message}");
            }
#endif
        }

        public static async Task ShowAdminMenu(long chatId, CancellationToken ct)
        {
            await ShowCleanAdminMenu(chatId, ct);
            return;
#if LEGACY_MENU
            try
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main_menu") }
                });

                string adminText = "⚙️ *АДМИН ПАНЕЛЬ*\n\n📋 *Доступные команды:*\n\n━━━━━━━━━━━━━━━━━━━━\n";
                adminText += "*🎟️ АКЦИИ:*\n";
                adminText += "`/crtemp` - создать акцию (пошагово)\n";
                adminText += "`/listtemp` - список всех акций\n";
                adminText += "`/offertemp <id>` - информация об акции\n";
                adminText += "`/edittemp <id> <поле> <значение>` - редактировать\n";
                adminText += "`/deltemp <id>` - удалить акцию\n";
                adminText += "`/activatetemp <id>` - активировать\n";
                adminText += "`/deactivatetemp <id>` - деактивировать\n";
                adminText += "`/refreshoffers` - принудительно обновить акции\n\n";
                adminText += "━━━━━━━━━━━━━━━━━━━━\n";
                adminText += "*🎫 Промокоды:*\n`/crpromo <код> <лимит> <награды>` - создать промокод\n`/listpromo` - список промокодов\n`/promoinfo <код>` - инфо о промокоде\n`/delpromo <код>` - удалить промокод\n\n";
                adminText += "*👑 VIP система:*\n`/setvip #токен <уровень> [дни]` - выдать VIP\n`/extendvip #токен <дни>` - продлить VIP\n`/vipinfo #токен` - инфо о VIP\n`/upgradevip` - повысить VIP 3-5 → 8-10\n`/refreshvip [#токен]` - пересчитать бонусы выданных VIP\n\n";
                adminText += "*🏆 Трофеи:*\n`/kybki #токен ID_бойца кол-во`\n`/resetbrawler #токен ID [лимит]`\n`/seasonreset #токен` или `all`\n\n";
                adminText += "*👥 Бойцы:*\n`/addbrawler #токен ID`\n`/removebrawler #токен ID`\n`/addallbrawlers #токен`\n\n";
                adminText += "*🎨 Скины:*\n`/addskin #токен ID`\n`/removeskin #токен ID`\n\n";
                adminText += "*💰 Ресурсы:*\n`/addgems #токен кол-во`\n`/addcoins #токен кол-во`\n`/addpowerpoints #токен кол-во`\n`/addbling #токен кол-во`\n`/addstarpoints #токен кол-во`\n`/remgems #токен кол-во`\n`/remcoins #токен кол-во`\n\n";
                adminText += "*🎫 Brawl Pass:*\n`/addpass #токен`\n`/addpassplus #токен`\n`/removepass #токен`\n\n";
                adminText += "*🔨 Баны/Муты:*\n`/ban #токен ID дата [причина]`\n`/unban #токен`\n`/mute #токен дата`\n`/unmute #токен`\n`/sdban #токен дата`\n`/sdunban #токен`\n\n";
                adminText += "*🗺 Карты:*\n`/maps` - активные слоты и карты\n`/maps <слот> [страница]` - карты режима\n`/setmap <слот> <ID карты>` - сменить карту\n\n";
                adminText += "*🎉 Серверные ивенты:*\n`/events` - статус трёх ивентов\n`/event <1-3> <on|off>` - включить или выключить\n\n";
                adminText += "*🛠 Техперерыв:*\n`/maintenance` - статус и справка\n`/maintenance on|off` - управление\n`/maintenance add|remove #тег` - доступ\n`/maintenance list` - белый список\n\n";
                adminText += "*🧪 Debug-аккаунты:*\n`/debuglist` - список debug-аккаунтов\n`/debug #тег` - текущее состояние\n`/debug #тег ammo on|off` - бесконечная атака\n`/debug #тег ulti on|off` - бесконечная ульта\n`/debug #тег all on|off` - обе возможности\n`/undebug #тег` - снять debug-статус\n\n";
                adminText += "*📝 Прочее:*\n`/online` - текущий онлайн и список игроков\n`/setname #токен ник`\n`/streak #токен значение`\n`/setpassword #тег новый_пароль`\n`/kick #тег` - отключить все устройства, включая офлайн\n`/backup` - создать полный бэкап\n\n━━━━━━━━━━━━━━━━━━━━\n";
                adminText += "💡 *Пример создания акции:*\n1. `/crtemp`\n2. Введите награды: `gems:100 coins:5000 skin:29000001`\n3. Введите `готово`\n4. Введите название\n5. Введите описание или `пропустить`\n6. Введите цену: `99 gems` или `0`\n7. Введите длительность: `7 дней`\n8. Введите фон или `пропустить`";

                adminText += "\n`/promovalidate <код>` — проверить и исправить ID наград старого промокода\n`/cancel` — отменить любой активный мастер";
                await BotClient.SendMessage(chatId, adminText, parseMode: ParseMode.Markdown, replyMarkup: keyboard, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TelegramBot] Ошибка в ShowAdminMenu: {ex.Message}");
                await BotClient.SendMessage(chatId, "❌ Ошибка при загрузке админ-меню", cancellationToken: ct);
            }
#endif
        }

        public static InlineKeyboardMarkup GetBackButton()
        {
            return new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", "main_menu") } });
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken ct)
        {
            Console.WriteLine($"[TelegramBot] Ошибка polling ({source}): {exception}");
            return Task.CompletedTask;
        }
    }
}

