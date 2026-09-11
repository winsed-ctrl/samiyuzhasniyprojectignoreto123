namespace GromCore.Laser.Logic.Avatar
{
    using Newtonsoft.Json;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Command.Avatar;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Friends;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using System.IO;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Security.Principal;

    public enum AllianceRole
    {
        None = 0,
        Member = 1,
        Leader = 2,
        Elder = 3,
        CoLeader = 4
    }

    public class AllianceKickBan
    {
        public long AllianceId;
        public DateTime BanTimer;
    }

    public class AllianceMailBan
    {
        public long AllianceId;
        public DateTime BanTimer;
    }

    // VIP Конфигурация (10 уровней)
    public static class VipConfig
    {
        public static readonly Dictionary<int, int> BonusTrophies = new Dictionary<int, int>
        {
            { 1, 15 },    // VIP I
            { 2, 30 },    // VIP II
            { 3, 45 },    // VIP III
            { 4, 55 },    // VIP IV
            { 5, 65 },    // VIP V
            { 6, 75 },    // VIP VI
            { 7, 85 },    // VIP VII
            { 8, 90 },    // VIP VIII
            { 9, 130 },   // VIP IX
            { 10, 170 },  // VIP X
        };

        public static readonly Dictionary<int, int> BonusMastery = new Dictionary<int, int>
        {
            { 1, 5 },    // VIP I
            { 2, 10 },   // VIP II
            { 3, 15 },   // VIP III
            { 4, 20 },   // VIP IV
            { 5, 25 },   // VIP V
            { 6, 30 },   // VIP VI
            { 7, 35 },   // VIP VII
            { 8, 40 },   // VIP VIII
            { 9, 45 },   // VIP IX
            { 10, 50 }   // VIP X
        };

        public static readonly Dictionary<int, string> VipNames = new Dictionary<int, string>
        {
            { 1, "🔥 VIP I" },
            { 2, "⚡ VIP II" },
            { 3, "🏆 VIP III" },
            { 4, "👾 VIP IV" },
            { 5, "👑 VIP V" },
            { 6, "⭐ VIP VI" },
            { 7, "✨ VIP VII" },
            { 8, "🌟 VIP VIII" },
            { 9, "💎 VIP IX" },
            { 10, "👑 VIP X" }
        };
    }

    // Класс для хранения данных о сбросе трофеев
    public class TrophyResetData
    {
        public int HeroId;
        public string HeroName;
        public int OldTrophies;
        public int NewTrophies;
        public int BlingsEarned;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ClientAvatar
    {
        [JsonIgnore] public int BattlePort;
        [JsonProperty] public long AccountId;
        [JsonProperty] public string PassToken;

        [JsonProperty] public string Name;
        [JsonProperty] public string Password;
        [JsonProperty] public bool NameSetByUser;
        [JsonProperty] public int TutorialsCompletedCount = 2;

        [JsonIgnore] public HomeMode HomeMode;

        [JsonProperty] public int Gold;
        [JsonProperty] public int Diamonds;

        [JsonProperty] public List<Hero> Heroes;

        [JsonProperty] public int TrioWins;
        [JsonProperty] public int DuoWins;
        [JsonProperty] public int SoloWins;
        [JsonProperty] public int EventWins;

        [JsonProperty] public int Tokens;
        [JsonProperty] public int StarTokens;
        [JsonProperty] public int StarPoints;
        [JsonProperty] public int Blings;
        [JsonProperty] public int PowerPoints;
        [JsonProperty] public int EventTokenCap;

        [JsonProperty] public bool IsDev;
        [JsonProperty] public bool IsPremium;
        [JsonProperty] public bool IsDebugAccount;
        // Nullable fields preserve the legacy behavior for existing debug accounts while
        // allowing Telegram administration to explicitly enable or disable each ability.
        [JsonProperty] public bool? InfiniteAmmo;
        [JsonProperty] public bool? InfiniteUltimate;

        [JsonProperty] public long AllianceId;
        [JsonProperty] public AllianceRole AllianceRole;
        [JsonProperty] public string AllianceName;

        [JsonProperty] public DateTime LastOnline;

        [JsonProperty] public List<Friend> Friends;
        [JsonProperty] public bool Banned;

        [JsonProperty] public int MuteCount;
        [JsonProperty] public bool ViewedPermMute;
        [JsonProperty] public int BanCount;
        [JsonProperty] public int BanType;
        [JsonProperty] public int BanID;
        [JsonProperty] public string TextReason;
        [JsonProperty] public DateTime BanEndTime;

        [JsonProperty] public DateTime PremiumTime;

        [JsonProperty] public List<string> ClaimedRewards;

        [JsonIgnore] public int PlayerStatus;
        [JsonIgnore] public long TeamId;
        [JsonProperty] public int RareTokens;
        [JsonProperty] public int FameTokens;

        [JsonIgnore] public long BattleId;
        [JsonIgnore] public long RanledId;
        [JsonIgnore] public long UdpSessionId;
        [JsonIgnore] public int TeamIndex;
        [JsonIgnore] public int OwnIndex;

        [JsonProperty] public int RollsSinceGoodDrop;

        [JsonProperty] public List<int> SPGS;
        [JsonProperty] public List<int> SelectedSPGS;

        [JsonProperty] public string Region;
        [JsonProperty] public int WinStreak;
        [JsonProperty] public int DoNotDisturb;
        [JsonProperty] public int Wins;
        [JsonProperty] public int NameChangeCost;
        [JsonProperty] public int ChatMutestate;
        [JsonProperty] public DateTime MuteEndTime;
        [JsonProperty] public DateTime NameChangeTimer;
        [JsonProperty] public int PreferedAccountLanguageInstanceId;
        [JsonProperty] public string PreferedAccountLanguageInString;
        [JsonProperty] public string AccountDevice;
        [JsonProperty] public DateTime AccountCreateDate;
        [JsonProperty] public bool AccountLinked;
        [JsonProperty] public bool AccountLinkedToTelegram;
        [JsonProperty] public int LinkedAccountIdToRedirect;
        [JsonProperty] public long AccountIdRedirect;
        [JsonProperty] public List<AllianceKickBan> KickBans = new();
        [JsonProperty] public List<AllianceMailBan> MailBans = new();
        [JsonProperty] public int FriendRequestBlock;
        [JsonProperty] public string IP;
        [JsonProperty] public int MaxWinstreak;
        [JsonProperty] public DateTime ShowdownBanTime;

        // VIP System с поддержкой времени (10 уровней)
        [JsonProperty] public int VipLevel { get; set; } = 0;
        [JsonProperty] public int VipBonusTrophies { get; set; } = 0;
        [JsonProperty] public int VipBonusMastery { get; set; } = 0;
        [JsonProperty] public int VipTotalBonusReceived { get; set; } = 0;
        [JsonProperty] public int VipMasteryTotalBonusReceived { get; set; } = 0;
        [JsonProperty] public DateTime VipExpirationTime { get; set; } = DateTime.MinValue;
        [JsonProperty] public string VipIssuedBy { get; set; } = string.Empty;

        public int Trophies
        {
            get
            {
                int result = 0;
                foreach (Hero hero in Heroes.ToArray())
                {
                    result += hero.Trophies;
                }
                return result;
            }
        }

        public int HighestTrophies
        {
            get
            {
                int result = 0;
                foreach (Hero hero in Heroes.ToArray())
                {
                    result += hero.HighestTrophies;
                }
                return result;
            }
        }

        // Проверка активности VIP
        public bool IsVipActive()
        {
            if (VipLevel == 0) return false;
            if (VipExpirationTime == DateTime.MinValue) return true;
            return DateTime.UtcNow < VipExpirationTime;
        }

        // Проверка и автоматическое снятие истёкшего VIP
        public void CheckVipExpiration()
        {
            if (VipLevel > 0 && VipExpirationTime != DateTime.MinValue && DateTime.UtcNow >= VipExpirationTime)
            {
                Console.WriteLine($"[VIP] VIP игрока {Name} (ID: {AccountId}) истёк! Был уровень {VipLevel}");
                
                VipLevel = 0;
                VipBonusTrophies = 0;
                VipBonusMastery = 0;
                VipExpirationTime = DateTime.MinValue;
                VipIssuedBy = string.Empty;
                
                SendVipExpiredNotification();
            }
        }

        // Установка VIP уровня с поддержкой длительности (до 10 уровня)
        public void SetVipLevel(int level, int durationDays = 0, string issuedBy = "admin")
        {
            if (level < 0 || level > 10)
            {
                Console.WriteLine($"[VIP] Ошибка: Неверный уровень VIP {level}. Допустимые значения: 0-10");
                return;
            }
            
            int oldLevel = VipLevel;
            bool wasActive = IsVipActive();
            int oldTrophyBonus = VipBonusTrophies;
            int oldMasteryBonus = VipBonusMastery;
            
            VipLevel = level;
            VipIssuedBy = issuedBy;
            
            if (durationDays > 0 && level > 0)
            {
                VipExpirationTime = DateTime.UtcNow.AddDays(durationDays);
            }
            else if (level == 0)
            {
                VipExpirationTime = DateTime.MinValue;
                VipIssuedBy = string.Empty;
            }
            else if (durationDays == 0 && level > 0)
            {
                VipExpirationTime = DateTime.MinValue;
            }
            
            if (level == 0)
            {
                VipBonusTrophies = 0;
                VipBonusMastery = 0;
            }
            else
            {
                if (VipConfig.BonusTrophies.ContainsKey(level))
                {
                    VipBonusTrophies = VipConfig.BonusTrophies[level];
                }
                else
                {
                    Console.WriteLine($"[VIP] Ошибка: Бонус кубков для уровня VIP {level} не найден");
                    VipBonusTrophies = 0;
                }
                
                if (VipConfig.BonusMastery.ContainsKey(level))
                {
                    VipBonusMastery = VipConfig.BonusMastery[level];
                }
                else
                {
                    Console.WriteLine($"[VIP] Ошибка: Бонус мастерства для уровня VIP {level} не найден");
                    VipBonusMastery = 0;
                }
            }
            
            string durationTextForLog = "";
            if (durationDays > 0)
            {
                durationTextForLog = $" на {durationDays} дней";
            }
            else if (level > 0)
            {
                durationTextForLog = " бессрочно";
            }
            
            string actionText = level == 0 ? "VIP снят" : $"выдан VIP уровень {level}{durationTextForLog} (+{VipBonusTrophies} кубков, +{VipBonusMastery} мастерства)";
            
            Console.WriteLine($"[VIP] Игрок {Name} (ID: {AccountId}) - VIP: {oldLevel} -> {level}, действие: {actionText}, бонус кубков: {VipBonusTrophies}, бонус мастерства: {VipBonusMastery}, выдал: {issuedBy}");
            
            if (level > 0 && (oldLevel != level || !wasActive || oldTrophyBonus != VipBonusTrophies || oldMasteryBonus != VipBonusMastery))
            {
                SendVipNotification(level, durationDays);
            }
            else if (level == 0 && oldLevel > 0)
            {
                SendVipRemovedNotification();
            }
        }

        // Продление VIP
        /// <summary>
        /// Recalculates cached VIP bonuses from the current configuration while
        /// preserving the VIP level, expiration time and accumulated statistics.
        /// </summary>
        public bool RefreshVipBonusesFromConfig()
        {
            int oldLevel = VipLevel;
            DateTime oldExpiration = VipExpirationTime;
            string oldIssuedBy = VipIssuedBy;
            int oldTrophyBonus = VipBonusTrophies;
            int oldMasteryBonus = VipBonusMastery;

            CheckVipExpiration();

            int newTrophyBonus = 0;
            int newMasteryBonus = 0;
            if (VipLevel > 0 && IsVipActive())
            {
                VipConfig.BonusTrophies.TryGetValue(VipLevel, out newTrophyBonus);
                VipConfig.BonusMastery.TryGetValue(VipLevel, out newMasteryBonus);
            }

            VipBonusTrophies = newTrophyBonus;
            VipBonusMastery = newMasteryBonus;
            return oldLevel != VipLevel || oldExpiration != VipExpirationTime ||
                   oldIssuedBy != VipIssuedBy || oldTrophyBonus != VipBonusTrophies ||
                   oldMasteryBonus != VipBonusMastery;
        }

        public void ExtendVip(int additionalDays, string issuedBy = "admin")
        {
            if (VipLevel == 0)
            {
                Console.WriteLine($"[VIP] Нельзя продлить VIP - у игрока {Name} нет активного VIP");
                return;
            }
            
            if (VipExpirationTime == DateTime.MinValue)
            {
                string vipLevelNameForMsg = VipConfig.VipNames.ContainsKey(VipLevel) ? VipConfig.VipNames[VipLevel] : $"уровень {VipLevel}";
                SendSystemMessage($"✨ Ваш бессрочный VIP {vipLevelNameForMsg} подтверждён!");
                return;
            }
            
            DateTime oldExpiration = VipExpirationTime;
            VipExpirationTime = VipExpirationTime.AddDays(additionalDays);
            VipIssuedBy = issuedBy;
            
            Console.WriteLine($"[VIP] VIP игрока {Name} продлён на {additionalDays} дней. Был до {oldExpiration}, стал до {VipExpirationTime}");
            
            string vipLevelNameForMsg2 = VipConfig.VipNames.ContainsKey(VipLevel) ? VipConfig.VipNames[VipLevel] : $"VIP уровень {VipLevel}";
            SendSystemMessage($"🎉 ВАШ VIP ПРОДЛЁН! 🎉\nВаш {vipLevelNameForMsg2} продлён на {additionalDays} дней!\n✨ Новый срок действия: {VipExpirationTime:dd.MM.yyyy} ✨");
        }

        // Получение оставшегося времени VIP
        public string GetVipTimeRemaining()
        {
            if (VipLevel == 0) return "Нет";
            if (VipExpirationTime == DateTime.MinValue) return "Бессрочно";
            if (DateTime.UtcNow >= VipExpirationTime) return "Истёк";
            
            TimeSpan remaining = VipExpirationTime - DateTime.UtcNow;
            if (remaining.Days > 0)
                return $"{remaining.Days} дн. {remaining.Hours} ч.";
            if (remaining.Hours > 0)
                return $"{remaining.Hours} ч. {remaining.Minutes} мин.";
            return $"{remaining.Minutes} мин.";
        }

        // Уведомление о выдаче VIP
        private void SendVipNotification(int newLevel, int durationDays = 0)
        {
            if (HomeMode?.GameListener == null) return;
            
            string vipLevelName = VipConfig.VipNames.ContainsKey(newLevel) ? VipConfig.VipNames[newLevel] : $"VIP уровень {newLevel}";
            string durationText = "";
            string expirationText = "";
            
            if (durationDays > 0)
            {
                durationText = $" на {durationDays} дн.";
                if (VipExpirationTime != DateTime.MinValue)
                {
                    expirationText = $"\n📅 Действует до: {VipExpirationTime:dd.MM.yyyy}";
                }
            }
            else
            {
                durationText = " (бессрочно)";
            }
            
            SendSystemMessage($"🎉 ПОЗДРАВЛЯЕМ! 🎉\nВы получили статус {vipLevelName}{durationText}!\n✨ Бонусы: +{VipBonusTrophies} кубков и +{VipBonusMastery} мастерства за победу ✨{expirationText}");
        }

        // Уведомление о снятии VIP
        private void SendVipRemovedNotification()
        {
            if (HomeMode?.GameListener == null) return;
            SendSystemMessage($"❌ Ваш VIP статус был снят. Бонусы {VipBonusTrophies} кубков и {VipBonusMastery} мастерства больше не активны.");
        }

        // Уведомление об истечении VIP
        private void SendVipExpiredNotification()
        {
            if (HomeMode?.GameListener == null) return;
            SendSystemMessage($"⚠️ ВНИМАНИЕ! ⚠️\nВаш VIP статус истёк.\nПриобретите VIP снова, чтобы продолжить получать бонусы!");
        }

        // Отправка системного сообщения
        private void SendSystemMessage(string message)
        {
            if (HomeMode?.GameListener == null) return;
            
            GromCore.Laser.Logic.Home.Items.Notification notif = new GromCore.Laser.Logic.Home.Items.Notification
            {
                Id = 81,
                MessageEntry = message,
                IsViewed = false,
                Index = 0
            };
            
            HomeMode.Home.NotificationFactory.Add(notif);
            LogicAddNotificationCommand acm2 = new LogicAddNotificationCommand { Notif = notif };
            AvailableServerCommandMessage acm5 = new AvailableServerCommandMessage();
            acm5.Command = acm2;
            HomeMode.GameListener.SendTCPMessage(acm5);
        }

        // Добавление бонусных кубков (вызывается при победе)
        public void AddVipBonusTrophies(ref int baseTrophies)
        {
            if (IsVipActive() && VipBonusTrophies > 0)
            {
                baseTrophies += VipBonusTrophies;
                VipTotalBonusReceived += VipBonusTrophies;
                Console.WriteLine($"[VIP] Игрок {Name} получил VIP бонус +{VipBonusTrophies} кубков (всего получено: {VipTotalBonusReceived})");
            }
        }

        // Добавление бонусных очков мастерства (вызывается при победе)
        public void AddVipBonusMastery(ref int baseMastery)
        {
            if (IsVipActive() && VipBonusMastery > 0)
            {
                baseMastery += VipBonusMastery;
                VipMasteryTotalBonusReceived += VipBonusMastery;
                Console.WriteLine($"[VIP] Игрок {Name} получил VIP бонус к мастерству +{VipBonusMastery} (всего получено: {VipMasteryTotalBonusReceived})");
            }
        }

        // Получить бонус мастерства без изменения переданного значения (для поражений)
        public int GetVipMasteryBonus()
        {
            if (IsVipActive() && VipBonusMastery > 0)
            {
                VipMasteryTotalBonusReceived += VipBonusMastery;
                Console.WriteLine($"[VIP] Игрок {Name} получил VIP бонус к мастерству +{VipBonusMastery} (поражение, всего: {VipMasteryTotalBonusReceived})");
                return VipBonusMastery;
            }
            return 0;
        }

        public void AddTrophies(int t)
        {
            return;
        }

        public int GetUnlockedBrawlersCountWithRarity(string rarity)
        {
            return Heroes.Count(x => x.CardData.Rarity == rarity);
        }

        public void ResetTrophies()
        {
            foreach (Hero hero in Heroes.ToArray())
            {
                hero.Trophies = 0;
                hero.HighestTrophies = 0;
            }
        }

        // ==================== МЕТОДЫ ДЛЯ СБРОСА ТРОФЕЕВ ====================
        
        public void ResetBrawlerTrophies(int characterId, int resetToAmount = 0)
        {
            Hero hero = GetHero(characterId);
            if (hero == null) return;
            
            int oldTrophies = hero.Trophies;
            hero.Trophies = resetToAmount;
            if (hero.Trophies < 0) hero.Trophies = 0;
            
            Console.WriteLine($"[ResetBrawler] Боец {hero.CharacterData?.Name} (ID:{characterId}) сброшен: {oldTrophies} -> {resetToAmount}");
        }
        
        public int SilentSeasonReset(int maxTrophies = 10000)
        {
            int resetCount = 0;
            foreach (Hero hero in Heroes.ToArray())
            {
                if (hero.Trophies > maxTrophies)
                {
                    hero.Trophies = maxTrophies;
                    resetCount++;
                    Console.WriteLine($"[SilentSeasonReset] {hero.CharacterData?.Name}: {hero.Trophies} -> {maxTrophies}");
                }
            }
            Console.WriteLine($"[SilentSeasonReset] Игрок {Name} (ID:{AccountId}) - сброшено бойцов: {resetCount}");
            return resetCount;
        }

        public (int totalBlings, int resetCount) SeasonResetWithReward(int maxTrophies = 10000)
        {
            int totalBlings = 0;
            int resetCount = 0;
            var resetDetails = new List<TrophyResetData>();
            
            foreach (Hero hero in Heroes.ToArray())
            {
                if (hero.Trophies > maxTrophies)
                {
                    int oldTrophies = hero.Trophies;
                    int newTrophies = maxTrophies;
                    int trophiesOverLimit = oldTrophies - maxTrophies;
                    int reward = trophiesOverLimit;
                    
                    hero.Trophies = newTrophies;
                    totalBlings += reward;
                    resetCount++;
                    
                    resetDetails.Add(new TrophyResetData
                    {
                        HeroId = hero.CharacterId,
                        HeroName = hero.CharacterData?.Name ?? $"ID:{hero.CharacterId}",
                        OldTrophies = oldTrophies,
                        NewTrophies = newTrophies,
                        BlingsEarned = reward
                    });
                    
                    Console.WriteLine($"[SeasonReset] {hero.CharacterData?.Name}: {oldTrophies} -> {newTrophies}, награда: {reward} блингов");
                }
            }
            
            if (totalBlings > 0)
            {
                AddBlings(totalBlings);
                
                if (HomeMode?.GameListener != null && resetCount > 0)
                {
                    string rewardMessage = $"🏆 *СЕЗОННЫЙ СБРОС!*\n\nСброшено бойцов: {resetCount}\n✨ Получено блингов: {totalBlings}";
                    
                    foreach (var detail in resetDetails.Take(5))
                    {
                        rewardMessage += $"\n• {detail.HeroName}: {detail.OldTrophies} → {detail.NewTrophies} (+{detail.BlingsEarned}✨)";
                    }
                    if (resetDetails.Count > 5)
                        rewardMessage += $"\n... и ещё {resetDetails.Count - 5} бойцов";
                    
                    Notification notif = new Notification
                    {
                        Id = 81,
                        MessageEntry = rewardMessage
                    };
                    HomeMode.Home.NotificationFactory.Add(notif);
                    LogicAddNotificationCommand cmd = new() { Notif = notif };
                    HomeMode.GameListener.SendCommand(cmd);
                }
            }
            
            Console.WriteLine($"[SeasonReset] Игрок {Name} (ID:{AccountId}) - сброшено: {resetCount}, награда: {totalBlings} блингов");
            return (totalBlings, resetCount);
        }

        // ==================== ОСТАЛЬНЫЕ МЕТОДЫ ====================

        public int GetUnlockedHeroesCount()
        {
            return Heroes.Count;
        }

        public void UnlockHero(int characterId)
        {
            Hero heroEntry = new Hero(characterId);
            Heroes.Add(heroEntry);
        }

        public void UpgradeHero(int characterId)
        {
            Hero heroEntry = GetHero(characterId);
            if (heroEntry.SelectedOverChargeId == 0)
            {
                CardData o = heroEntry.GetDefaultMetaForHero(6);
                if (o != null) heroEntry.SelectedOverChargeId = o.GetInstanceId();
            }
        }

        public void RevokeDiamonds(int count) { Diamonds -= count; }
        
        public void RemoveHero(int characterId)
        {
            Heroes.RemoveAll(x => x.CharacterId == characterId);
        }

        public bool HasHero(int characterId)
        {
            return Heroes.Find(x => x.CharacterId == characterId) != null;
        }
        
        public Hero GetHero(int characterId)
        {
            return Heroes.Find(x => x.CharacterId == characterId);
        }
        
        public Hero GetHeroForCard(CardData cardData)
        {
            return GetHero(DataTables.Get(16).GetData<CharacterData>(cardData.Target).GetInstanceId() + 16000000);
        }

        public void SetEmoteForBrawler(int characterId, int slot, int pin)
        {
            Hero heroEntry = GetHero(characterId);
            heroEntry.SelectedEmotes[slot] = pin;
        }

        public void SetSprayForBrawler(int characterId, int pin)
        {
            Hero heroEntry = GetHero(characterId);
            heroEntry.SelectedSpray = pin;
        }

        public bool UseDiamonds(int count)
        {
            if (count > Diamonds) return false;
            Diamonds -= count;
            return true;
        }

        public bool UseBlings(int count)
        {
            if (count > Blings) return false;
            Blings -= count;
            return true;
        }

        public bool UseGold(int count)
        {
            if (count > Gold) return false;
            Gold -= count;
            return true;
        }

        public bool UsePowerPoints(int count, Hero hero = null)
        {
            if (hero != null){
                if (hero.PowerPoints >= count) {
                    hero.PowerPoints -= count;
                    Console.WriteLine("HHHHHH3");
                }
                else if ((hero.PowerPoints + PowerPoints) >= count){
                    Console.WriteLine("HHHHHH1");
                    PowerPoints -= count - hero.PowerPoints;
                    hero.PowerPoints = 0;
                }
            }
            else{
                if (count > PowerPoints) return false;
                PowerPoints -= count;
                Console.WriteLine("HHHHHH2");
            }
            return true;
        }

        public void AddDiamonds(int count)
        {
            Diamonds += count;
        }

        public void AddGold(int count)
        {
            Gold += count;
        }

        public bool UseTokens(int count)
        {
            if (count > Tokens) return false;
            Tokens -= count;
            return true;
        }

        public void AddTokens(int count)
        {
            HomeMode.Home.BrawlPassTokens += count;
        }

        public bool UseStarTokens(int count)
        {
            if (count > StarTokens) return false;
            StarTokens -= count;
            return true;
        }

        public void AddStarTokens(int count)
        {
            StarTokens += count;
        }

        public void AddPowerPoints(int count)
        {
            PowerPoints += count;
        }

        public bool UseStarPoints(int count)
        {
            if (count > StarPoints) return false;
            StarPoints -= count;
            return true;
        }

        public void AddStarPoints(int count, bool gained = true)
        {
            StarPoints += count;
            HomeMode.Home.StarPointsGained += count;
        }

        public void AddRareTokens(int count)
        {
            List<int> road_list = new List<int> { };
            foreach (int brawler in HomeMode.Home.BrawlersRoad)
            {
                if (!HomeMode.HasHeroUnlocked(16000000 + brawler))
                {
                    road_list.Add(brawler);
                }
            }
            RareTokens += count;
            HomeMode.Home.RecruitTokens += count;
            if (road_list.Count == 0) {
                HomeMode.Home.FameTokens += HomeMode.Home.RecruitTokens;
                HomeMode.Avatar.FameTokens = HomeMode.Home.FameTokens;
                RareTokens = 0;
                HomeMode.Home.RecruitTokens = 0;
            }
        }

        public void AddBlings(int count)
        {
            Blings += count;
            Console.WriteLine($"[Blings] Игрок {Name} получил {count} блингов. Всего: {Blings}");
        }

        public ClientAvatar()
        {
            Name = "Brawler";
            Gold = 100;
            Diamonds = 0;
            Heroes = new List<Hero>();
            ClaimedRewards = new List<string>();
            SPGS = new List<int>();
            SelectedSPGS = new List<int>();
            IsDev = false;
            IsPremium = false;
            AllianceRole = AllianceRole.None;
            AllianceId = -1;
            LastOnline = DateTime.UtcNow;
            Friends = new List<Friend>();
            VipLevel = 0;
            VipBonusTrophies = 0;
            VipBonusMastery = 0;
            VipTotalBonusReceived = 0;
            VipMasteryTotalBonusReceived = 0;
            VipExpirationTime = DateTime.MinValue;
            VipIssuedBy = string.Empty;
        }

        public void SkipTutorial()
        {
            TutorialsCompletedCount = 2;
        }

        public bool IsTutorialState()
        {
            return TutorialsCompletedCount < 2;
        }

        public Friend GetRequestFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id && friend.FriendState != 4);
        }

        public Friend GetAcceptedFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id && friend.FriendState == 4);
        }
        
        public EmoteData GetDefaultEmoteForCharacter(string Character, string Type)
        {
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Character == Character && emoteData.EmoteType == Type) return emoteData;
            }
            return null;
        }
        
        public Friend GetFriendById(long id)
        {
            return Friends.Find(friend => friend.AccountId == id);
        }
        
        public void Refresh()
        {
            for (int i = 0; ; i++)
            {
                CharacterData character = DataTables.Get(16).GetDataWithId<CharacterData>(i);
                if (character.Disabled && !character.LockedForChronos)
                {
                    RemoveHero(character.GetGlobalId());
                    continue;
                }
                if (!HasHero(16000000 + i))
                {
                    if (!character.IsHero()) break;
                    UnlockHero(character.GetGlobalId());
                }
                else
                {
                    if (!character.IsHero()) break;
                    Hero h = GetHero(character.GetGlobalId());
                    if (h == null) break;
                    h.PowerLevel = 11;
                }
                CardData c = DataTables.Get(DataType.Card).GetDataWithId<CardData>(i);
                if (c.MetaType == 4) SPGS.Add(c.GetGlobalId());
                if (c.MetaType == 5) SPGS.Add(c.GetGlobalId());
            }
        }
        
        public int Checksum
        {
            get
            {
                ChecksumEncoder encoder = new ChecksumEncoder();
                Encode(encoder);
                return encoder.GetCheckSum();
            }
        }

        public void Encode(ChecksumEncoder stream)
        {
            stream.WriteVLong(AccountId);
            stream.WriteVLong(AccountId);
            stream.WriteVLong(AccountId);
            stream.WriteString(Name);
            stream.WriteBoolean(NameSetByUser);
            stream.WriteInt(-1);

            stream.WriteVInt(17);
            stream.WriteVInt(6 + Heroes.Count);
            {
                ByteStreamHelper.WriteDataReference(stream, 5000008);
                stream.WriteVInt(-1);
                stream.WriteVInt(Gold);

                ByteStreamHelper.WriteDataReference(stream, 5000023);
                stream.WriteVInt(-1);
                stream.WriteVInt(Blings);
                
                ByteStreamHelper.WriteDataReference(stream, 5000022);
                stream.WriteVInt(-1);
                stream.WriteVInt(PowerPoints);

                ByteStreamHelper.WriteDataReference(stream, 5000021);
                stream.WriteVInt(-1);
                stream.WriteVInt(HomeMode.Home.FameTokens);

                ByteStreamHelper.WriteDataReference(stream, 5000010);
                stream.WriteVInt(-1);
                stream.WriteVInt(StarPoints);

                ByteStreamHelper.WriteDataReference(stream, 5000018);
                stream.WriteVInt(-1);
                stream.WriteVInt(6);

                foreach (Hero hero in Heroes)
                {
                    ByteStreamHelper.WriteDataReference(stream, hero.CardData);
                    stream.WriteVInt(-1);
                    stream.WriteVInt(1);
                }
            }

            stream.WriteVInt(Heroes.Count);
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.Trophies);
            }

            stream.WriteVInt(Heroes.Count);
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.HighestTrophies);
            }

            stream.WriteVInt(Heroes.Count);
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(RandomNumberGenerator.GetInt32(4));
            }

            stream.WriteVInt(Heroes.Count);
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.PowerPoints);
            }

            stream.WriteVInt(Heroes.Count);
            foreach (Hero hero in Heroes)
            {
                ByteStreamHelper.WriteDataReference(stream, hero.CharacterData);
                stream.WriteVInt(-1);
                stream.WriteVInt(hero.PowerLevel - 1);
            }

            stream.WriteVInt(SPGS.Count);
            foreach (int s in SPGS) 
            {
                ByteStreamHelper.WriteDataReference(stream, s);
                stream.WriteVInt(-1);
                stream.WriteVInt(SelectedSPGS.Contains(s) ? 2 : 1);
            }

            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);

            stream.WriteVInt(Diamonds);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(1);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(2);
            stream.WriteVInt(19);
            stream.WriteVInt(1000);
            stream.WriteVInt(15);
            stream.WriteString(null);
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(1);
        }
    }
}
