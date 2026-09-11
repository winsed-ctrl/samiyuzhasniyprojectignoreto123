namespace GromCore.Laser.Logic.Battle.Structures
{
    using Masuda.Net.Models;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Avatar.Structures;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;
    using GromCore.Laser.Titan.Util;
    using System.Diagnostics.SymbolStore;
    using System.Numerics;
    using System.Security.Cryptography;

    public class TickDamage
    {
        public int Tick { get; set; }
        public int Damage { get; set; }
    }
    public class BattlePlayer
    {
        public long AccountId;
        public int PlayerIndex;
        public int TeamIndex;
        public Dictionary<int, int> Emotes = new Dictionary<int, int>();
        public Dictionary<int, int> Spray = new Dictionary<int, int>();
        public long TeamId = -1;
        public int HeroIndex;
        public int HeroIndexMax;
        public PlayerDisplayData DisplayData;
        public int CharacterId => CharacterIds[HeroIndex];
        public int SkinId => SkinIds[HeroIndex];
        public long SessionId;
        public LogicGameListener GameListener;
        public int[] CharacterIds = new int[3];
        public int[] SkinIds = new int[3];
        public int OwnObjectId;
        public int PreviousObjectId;
        public int ControlTicksLeft;
        public int LastHandledInput;
        public int Trophies, HighestTrophies;
        public int HeroPowerLevel;
        private int Score;
        public int EventScore;
        private LogicVector2 SpawnPoint;
        public int StartUsingPinTicks;
        public int PinIndex;
        public int SprayIndex;
        public SkinConfData SkinConfData => SkinId == 0 ? null : DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(SkinId).Conf);
        public SkinData SkinData => SkinId == 0 ? null : DataTables.Get(DataType.Skin).GetDataByGlobalId<SkinData>(SkinId);
        public CardData AccessoryCardData => AccessoryCardDatas[HeroIndex];
        public CardData[] AccessoryCardDatas;
        public AccessoryData AccessoryData => AccessoryDatas[HeroIndex];
        public AccessoryData[] AccessoryDatas;
        public CardData StarPowerData => StarPowerDatas[HeroIndex];
        public CardData[] StarPowerDatas;
        public Accessory Accessory;
        public CharacterData CharacterData => CharacterDatas[HeroIndex];
        public CharacterData[] CharacterDatas;
        public GearData Gear1;
        public GearData Gear2;
        public CardData[] OverChargeDatas;
        public CardData OverChargeData => OverChargeDatas[HeroIndex];
        public int PowerLevel;
        public int Bot;
        public bool IsAdmin;
        public bool InfiniteAmmo;
        public bool InfiniteUltimate;
        private int UltiCharge;
        private int OverCharge;
        public bool OverCharging;
        public bool OverChargeActivated;
        public bool OverChargeStarted;
        public bool OverChargeEnded;
        public bool IsAlive;
        public int BattleRoyaleRank;
        public List<PlayerKillEntry> KillList;
        public int Kills;
        public int Damage;
        public int Heals;
        public int DeathTick;
        public int Deaths;
        public int T1;
        public int T2;
        public int E;
        public int Ti;
        public int LastPinUseTicks = -200;
        public int LastSprayUseTicks;
        public Character Character;
        public Item CharacterSpray;
        public Item Portal1;
        public Item Portal2;
        public List<Petrol> PlayerPetrols = new List<Petrol>();
        public int Mastery;
        public int Fame;
        public int AllStarsEarned;
        public Character SplitterAttachedLegs;
        private const int MAX_CHUCK_FLAGS = 4;
        public List<Item> ChuckFlags = new List<Item>();
        public List<TickDamage> _damageHistory = new();
        public List<Item> BoMines = new();
        public bool HasStar;
        public int EventCollectTokens;
        public int TemporaryTeamOverride;
        public Character ControlledCharacter;
        public Character ControlCharacter;
        public bool CanSendBattleEnd;
        public bool ServerEventRewardsGranted;
        public int BanCounter;
        public AreaEffect ChainedRespawnAOE;
        public bool Vibrate;
        public BattlePlayer()
        {
            DisplayData = new PlayerDisplayData();
            SpawnPoint = new LogicVector2();

            StartUsingPinTicks = -9999;

            BattleRoyaleRank = -1;

            KillList = new List<PlayerKillEntry>();
            if (IsAdmin) UltiCharge = 4000;

            UltiCharge = 0;
            OverCharge = 0;
            DeathTick = 1;
            TemporaryTeamOverride = -1;
        }
        public void AddChuckFlag(Item flag)
        {
            if (flag == null) return;
            if (ChuckFlags.Count >= MAX_CHUCK_FLAGS)
            {
                var oldFlag = ChuckFlags[0];
                ChuckFlags.RemoveAt(0);
                if (oldFlag != null)
                {
                    try
                    {
                        var battle = oldFlag.GetBattle();
                        if (battle != null)
                        {
                            battle.GetGameObjectManager().RemoveGameObject(oldFlag);
                        }
                    }
                    catch
                    {
                    }
                }
            }

            ChuckFlags.Add(flag);
        }

        public void ClearChuckFlags()
        {
            foreach (var flag in ChuckFlags)
            {
                if (flag != null)
                {
                    try
                    {
                        var battle = flag.GetBattle();
                        if (battle != null)
                        {
                            battle.GetGameObjectManager().RemoveGameObject(flag);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            ChuckFlags.Clear();
        }
        public void SetCharacter(Character character)
        {
            Character = character;
        }
        public void Healed(int heals)
        {
            Heals += heals;
        }

        public int GetDps(Int64 a1 = 0)
        {
            return _damageHistory.Sum(d => d.Damage);
        }
        public void DamageDealed(int damage, int currentTick = 0)
        {
            Damage += damage;
            if (damage > 0)
            {
                _damageHistory.Add(new TickDamage { Tick = currentTick, Damage = damage });
            }
        }

        public int GetSprayIDBySprayIndex(int i)
        {
            return Spray[i];
        }

        public void KilledPlayer(int index, int bountyStars, bool hasStars = false)
        {
            KillList.Add(new PlayerKillEntry()
            {
                PlayerIndex = index,
                BountyStarsEarned = bountyStars
            });
            if (hasStars) HasStar = true;

            Kills++;
        }

        public bool HasUlti()
        {
            return UltiCharge >= 4000;
        }

        public bool OverChargeReady()
        {
            return OverCharge >= 4000;
        }

        public int GetUltiCharge()
        {
            return UltiCharge;
        }

        public int GetOverCharge()
        {
            return OverCharge;
        }
        public int GetCardValueForPassive(string a2, int a3)
        {
            if (StarPowerData != null)
            {
                if (StarPowerData.Type == a2)
                {
                    if (a3 == 2) return StarPowerData.Value3;
                    return a3 > 0 ? StarPowerData.Value : StarPowerData.Value2;
                }
                else return -1;

            }
            else
                return -1;
        }
        public void AddUltiCharge(int amount)
        {
            UltiCharge = LogicMath.Clamp(UltiCharge + amount, 0, 4000);
        }
        public int GetGearBoost(int LogicType)
        {
            if (Gear1 != null)
            {
                if (Gear1.LogicType == LogicType)
                {
                    return Gear1.ModifierValue;
                }
            }
            if (Gear2 != null)
            {
                if (Gear2.LogicType == LogicType)
                {
                    return Gear2.ModifierValue;
                }
            }
            return 0;
        }
        public void ChargeUlti(int a2, bool a3, bool a4)
        {
            int v9 = 100;
            if (!a4)
            {
                if (a3)
                    v9 = CharacterData.UltiChargeUltiMul;
                else
                    v9 = CharacterData.UltiChargeMul;
            }
            int result = v9 * a2 / 100 + UltiCharge;
            if (result > 4000)
                result = 4000;
            if (UltiCharge < 4000) UltiCharge = result;
            int Div = 2;
            if (CharacterData.Name == "Roller") Div = 20;
            if (HasOverCharge() && !OverCharging) OverCharge = LogicMath.Min(4000, OverCharge + v9 * a2 / 100 / Div);
        }

        public void UseUlti()
        {
            if (!InfiniteUltimate) UltiCharge = 0;
            //UltiCharge = 0;
        }

        public void UseOverCharge(Character character)
        {
            if (OverChargeReady() && !OverCharging) OverCharging = true;
        }

        public void UpdateOverCharge()
        {
            OverChargeStarted = OverCharging && !OverChargeActivated;
            OverChargeActivated = OverCharging;
            OverChargeEnded = false;
            if (OverCharging)
            {
                OverCharge -= 40;
                if (!IsAlive || OverCharge <= 0)
                {
                    OverChargeEnded = true;
                    OverCharge = 0;
                    OverCharging = false;
                }
            }
        }
        public int IsBot()
        {
            return Bot;
        }
        public void CallVibrate()
        {
            if (LogicServerListener.Instance.IsDev()) Console.WriteLine("Vibration Called");
            Vibrate = true;
        }
        public BattlePlayer(ClientHome home, ClientAvatar avatar) : this()
        {
            Home = home;
            Avatar = avatar;
        }

        public void AddScore(int a, bool hasstar = false)
        {
            Score += a;
            if (hasstar) HasStar = true;
        }

        public void AddEventScore(int a)
        {
            EventScore += a;
        }

        public void ResetScore()
        {
            Score = 0;
        }
        public static EmoteData GetDefaultEmoteForCharacter(string Character, string Type)
        {
            foreach (EmoteData emoteData in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (emoteData.Character == Character && emoteData.EmoteType == Type) return emoteData;
            }
            return null;
        }
        public void UsePin(int index, int ticks)
        {
            StartUsingPinTicks = ticks;
            PinIndex = index;
            LastPinUseTicks = ticks;
        }
        public bool CanUsePin(int ticks) => LastPinUseTicks + 280 <= ticks;
        public bool CanUsePinVisu(int ticks) => LastPinUseTicks + 300 <= ticks;
        public bool IsUsingPin(int ticks)
        {
            return LastPinUseTicks + 80 >= ticks;
        }

        public bool IsUsingSpray(int ticks)
        {
            return ticks - LastSprayUseTicks < 20;
        }
        public void SetHeroIndex(int Index)
        {
            HeroIndex = Index;
            Accessory = AccessoryDatas[Index] == null ? null : new Accessory(AccessoryDatas[Index]);

        }
        public int GetScore()
        {
            return Score;
        }

        public void SetSpawnPoint(int x, int y)
        {
            SpawnPoint.Set(x, y);
        }

        public void SetSpawnPoint(LogicVector2 pos)
        {
            SpawnPoint = pos;
        }

        public LogicVector2 GetSpawnPoint()
        {
            return SpawnPoint.Clone();
        }
        public bool HasOverCharge()
        {
            return OverChargeData != null;
        }
        private bool canEncodeLastSpray;
        public void Encode(ByteStream stream, bool shouldEncodeName)
        {
            stream.WriteLong(AccountId);
            stream.WriteVInt(PlayerIndex);
            stream.WriteVInt(TeamIndex);
            stream.WriteVInt(0);
            stream.WriteInt(0);
            //68 44 VInt 40 Int 32 Boolean 28 string
            stream.WriteVInt(HeroIndexMax + 1);
            for (int i = 0; i <= HeroIndexMax; i++)
            {
                ByteStreamHelper.WriteDataReference(stream, CharacterIds[i] > 16000080 ? 16000000 : CharacterIds[i]);
                if (stream.WriteBoolean(true))
                {
                    //logic Hero Upgrades
                    stream.WriteVInt(HeroPowerLevel);
                    ByteStreamHelper.WriteDataReference(stream, StarPowerDatas[i]);
                    ByteStreamHelper.WriteDataReference(stream, AccessoryCardDatas[i]); 
                    ByteStreamHelper.WriteDataReference(stream, Gear1);
                    ByteStreamHelper.WriteDataReference(stream, Gear2);
                    ByteStreamHelper.WriteDataReference(stream, OverChargeDatas[i]);
                }
                if (stream.WriteBoolean(true))
                {
                    stream.WriteVInt(5);

                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(1, out var value) ? value : 137 - 3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(2, out var value2) ? value2 : 137 - 3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(3, out var value3) ? value3 : 137 - 3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(4, out var value4) ? value4 : 137 - 3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(52, Emotes.TryGetValue(5, out var value5) ? value5 : 137 - 3));
                    stream.WriteVInt(5);
                }
                if (stream.WriteBoolean(true))
                {
                    int vanityCount = 0; 
                    if (!canEncodeLastSpray) vanityCount = 3;
                    else if (Spray.TryGetValue(4, out int value123) && value123 != -1) vanityCount = 5;
                    else vanityCount = 5;

                    stream.WriteVInt(vanityCount);
                        ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(0, out var value) ? value : 7 - 3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(1, out var value2) ? value2 : 8-3));
                    ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(2, out var value3) ? value3 : 18-3));
                    if (vanityCount > 3) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(3, out var value4) ? value4 : 4));
                    if(vanityCount == 5) ByteStreamHelper.WriteDataReference(stream, GlobalId.CreateGlobalId(68, Spray.TryGetValue(4, out var value5) ? value5 : 5));

                }
                string tmp = DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(CharacterIds[i]).Name;
                if (tmp == "MechaDude" || tmp == "CannonGirl" || tmp == "Digger" || tmp == "DragonRider" || tmp == "Geisha") ByteStreamHelper.WriteDataReference(stream, null);
                else ByteStreamHelper.WriteDataReference(stream, SkinIds[i]);
                ByteStreamHelper.WriteDataReference(stream, null);
                stream.WriteVInt(0);
            }
            DisplayData.Encode(stream, !shouldEncodeName);

            stream.WriteBoolean(false);
            if (stream.WriteBoolean(false))
            {
                stream.WriteVLong(AccountId);
                stream.WriteString("SB");
                ByteStreamHelper.WriteDataReference(stream, null);//8
            }
            //new
            stream.WriteVInt(0);
            stream.WriteVInt(0);
            stream.WriteVInt(Fame); // fame credits
            ByteStreamHelper.WriteDataReference(stream, T1);
            ByteStreamHelper.WriteDataReference(stream, T2);
            ByteStreamHelper.WriteDataReference(stream, E);
            ByteStreamHelper.WriteDataReference(stream, Ti);
            stream.WriteVInt(Mastery); //           
        }

        [JsonIgnore] public readonly ClientHome Home;
        [JsonIgnore] public readonly ClientAvatar Avatar;
        public int RankedCharacterId = -1;
        public static BattlePlayer Create(ClientHome home, ClientAvatar avatar, int playerIndex, int teamIndex, EventData e = null, int iw = -1)
        {
            
            BattlePlayer player = new BattlePlayer(home, avatar);
            player.InfiniteAmmo = avatar.InfiniteAmmo ?? false;
            player.InfiniteUltimate = avatar.InfiniteUltimate ?? avatar.IsDebugAccount;
            player.DisplayData.Name = avatar.Name;
            int character = home.CharacterIds[0];
            if (!avatar.HasHero(character)) character = GlobalId.CreateGlobalId(16, 0);
            if (home.ChaosTempHero != -1 && e != null && e.Slot == 34) character = home.ChaosTempHero;
            if (iw != -1) character = iw;
            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.CharacterDatas = new CharacterData[3];
            player.OverChargeDatas = new CardData[3];
            
            player.DisplayData.ThumbnailId = home.ThumbnailId;
            player.DisplayData.NameColorId = home.NameColorId;
            player.AccountId = avatar.AccountId;
            Hero geroin = null;
            
            for (int i = 0; i < home.CharacterIds.Length; i++)
            {
                geroin = avatar.GetHero(character);
                player.CharacterIds[i] = character;
                if (avatar.GetHero(character) != null && avatar.GetHero(character).SelectedSkinId > 0) {
                    player.SkinIds[i] = GlobalId.CreateGlobalId(29, avatar.GetHero(character).SelectedSkinId);
                }
                CharacterData DATA = DataTables.Get(16).GetDataWithId<CharacterData>(character);
                if (avatar.HasHero(character) && geroin != null)
                {
                    player.StarPowerDatas[i] = DataTables.Get(23).GetData<CardData>(avatar.GetHero(character).SelectedStarPowerId);
                    player.CharacterDatas[i] = DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(character);
                    CardData data = DataTables.Get(23).GetData<CardData>(DATA.Name + "_overcharge");
                    if (data != null)
                    {
                        if (avatar.SPGS.Contains(data.GetGlobalId()))
                        {
                            player.OverChargeDatas[i] = data;
                        }
                    }
                    else player.OverChargeDatas[i] = null;
                    CardData CardAccessoryData = DataTables.Get(23).GetDataByGlobalId<CardData>(GlobalId.CreateGlobalId(23, geroin.SelectedGadgetId));
                    if (CardAccessoryData != null)
                    {
                        if (avatar.SPGS.Contains(CardAccessoryData.GetGlobalId()))
                        {
                            player.AccessoryCardDatas[0] = CardAccessoryData;
                        }
                    }
                    if (player.AccessoryCardDatas[0] != null) player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardDatas[0].Name);
                    player.Mastery = geroin.MasteryPoints;
                }
                
                
                
            }
            // Console.WriteLine("player.AccessoryDatas[0] = " + player.AccessoryDatas[0]);
            // if (player.StarPowerDatas[0] != null && !GeneralStaticLogic.AllowedSPGs.Contains(player.StarPowerDatas[0].GetInstanceId())) player.StarPowerDatas[0] = null;
            // if (player.AccessoryDatas[0] != null && !GeneralStaticLogic.AllowedGadgets.Contains(player.AccessoryDatas[0].GetInstanceId())) player.AccessoryDatas[0] = null;
            // if (player.OverChargeDatas[0] != null && !GeneralStaticLogic.AllowedOvercharges.Contains(player.OverChargeDatas[0].GetInstanceId())) player.OverChargeDatas[0] = null;
            
            if(e != null && e.Slot == 34)
            {
                if (DataTables.Get(23).GetData<CardData>(DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(home.ChaosTempHero).Name + "_overcharge") != null)
                {
                    player.OverChargeDatas[0] = DataTables.Get(23).GetData<CardData>(DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(home.ChaosTempHero).Name + "_overcharge");
                }
            }

            player.Fame = home.FameTokens;
            if(geroin != null)
            {
                player.Emotes.Add(1, geroin.SelectedEmotes[1]);
                player.Emotes.Add(2, geroin.SelectedEmotes[2]);
                player.Emotes.Add(3, geroin.SelectedEmotes[3]);
                player.Emotes.Add(4, home.PlayerSelectedEmotes[4]);
                player.Emotes.Add(5, home.PlayerSelectedEmotes[5]);

                player.Spray.Add(4, geroin.SelectedSpray);
                player.Spray.Add(0, home.PlayerSelectedSpray[7]);
                player.Spray.Add(1, home.PlayerSelectedSpray[8]);
                player.Spray.Add(2, home.PlayerSelectedSpray[9]);
                player.Spray.Add(3, home.PlayerSelectedSpray[10]);
            }
            else
            {
                player.Emotes.Remove(1);
                try { player.Emotes.Add(1, player.CharacterDatas[0] != null ? Hero.GetDefaultEmoteForCharacter(player.CharacterDatas[0].Name, "DEFAULT").GetInstanceId() : 136 - 3); }
                catch { player.Emotes.Add(1, 136 - 3); }
                player.Emotes.Remove(2);
                player.Emotes.Add(2, 137 - 3);
                player.Emotes.Remove(3);
                player.Emotes.Add(3, 148 - 3);
                player.Spray.Remove(0);
                player.Spray.Add(0, home.PlayerSelectedSpray[7]);
                player.Spray.Remove(1);
                player.Spray.Add(1, home.PlayerSelectedSpray[8]);
                player.Spray.Remove(2);
                player.Spray.Add(2, home.PlayerSelectedSpray[9]);
                player.Spray.Remove(3);
                player.Spray.Add(3, home.PlayerSelectedSpray[10]);
            }
            player.canEncodeLastSpray = true;
            player.HeroIndexMax = home.CharacterIds.Length - 1;
            player.Accessory = player.AccessoryData == null ? null : new Accessory(player.AccessoryData);
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            Hero hero = null;
            if (avatar.GetHero(character) != null) hero = avatar.GetHero(character);
            if (hero != null && hero.PowerLevel >= 7 && hero.SelectedGearId1 != -1) player.Gear1 = DataTables.Get(DataType.Gear).GetDataWithId<GearData>(hero.SelectedGearId1);
            if (hero != null && hero.PowerLevel >= 9 && hero.SelectedGearId2 != -1) player.Gear2 = DataTables.Get(DataType.Gear).GetDataWithId<GearData>(hero.SelectedGearId2);
            if(hero != null)
            {
                player.Trophies = hero.Trophies;
                player.HighestTrophies = hero.HighestTrophies;
                player.HeroPowerLevel = hero.PowerLevel;
            }

            if(e != null && (e.Slot == 34 || e.IsChallengeSlot)) player.HeroPowerLevel = 11;

            player.T1 = home.DefaultBattleCard.Thumbnail1;
            player.T2 = home.DefaultBattleCard.Thumbnail2;
            player.E = home.DefaultBattleCard.Emote;
            player.Ti = home.DefaultBattleCard.Title;
            return player;
        }

        public static BattlePlayer CreateBotInfo(string name, int playerIndex, int teamIndex, int character = 16000000)
        {

            BattlePlayer player = new BattlePlayer();
            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.CharacterDatas = new CharacterData[3];
            player.OverChargeDatas = new CardData[3];
            player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>("ShotgunGirl_Dash");
            player.AccessoryCardDatas[0] = DataTables.Get(23).GetData<CardData>("ShotgunGirl_Dash");
            player.Accessory = new Accessory(player.AccessoryData);
            var rand = new Random();
            List<string> CreditsRandom = new List<string>{
            "bot"
};
            int CreditsRandomInt = rand.Next(CreditsRandom.Count);
            player.DisplayData.Name = CreditsRandom[CreditsRandomInt];
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            player.DisplayData.NameColorId = GlobalId.CreateGlobalId(43, 0);
            player.AccountId = 100000 + playerIndex;
            player.CharacterIds[0] = (character);
            player.AccessoryCardDatas[0] = DataTables.Get(23).GetData<CardData>(255);
            // player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardData.Name);
            //player.AccessoryUsesLeft = 3;
            List<int> DefEmojis = new List<int>();
            List<int> SkinsForBot = new List<int>();
            List<int> TitleForBot = new List<int>();
            List<int> IconForBot = new List<int>();
            foreach (EmoteData emote in DataTables.Get(DataType.Emote).GetDatas())
            {
                if (!emote.Disabled && emote.Character == null)
                {
                    if (emote.Rarity == "DEFAULT" || emote.Rarity == "COLLECTORS")
                    {
                        DefEmojis.Add(emote.GetInstanceId());
                    }
                }
            }
            foreach (PlayerThumbnailData icon in DataTables.Get(DataType.PlayerThumbnail).GetDatas())
            {
                if (icon.PriceGems >= 1)
                {
                    IconForBot.Add(icon.GetInstanceId());
                }
            }
            foreach (TitlesData title in DataTables.Get(DataType.Titul).GetDatas())
            {
                    TitleForBot.Add(title.GetInstanceId());
            }
            List<string> rarity = new List<string> { "HAPPY", "SAD", "THANKS", "SPECIAL", "ANGRY", "DEFAULT" };
            player.Emotes.Add(1, GetDefaultEmoteForCharacter(DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(character).Name, rarity[Random.Shared.Next(0, rarity.Count)])?.GetInstanceId() ?? 0);
            player.Emotes.Add(2, GetDefaultEmoteForCharacter(DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(character).Name, rarity[Random.Shared.Next(0, rarity.Count)])?.GetInstanceId() ?? 0);
            player.Emotes.Add(3, GetDefaultEmoteForCharacter(DataTables.Get(DataType.Character).GetDataByGlobalId<CharacterData>(character).Name, rarity[Random.Shared.Next(0, rarity.Count)])?.GetInstanceId() ?? 0);
            player.Emotes.Add(4, DefEmojis[Random.Shared.Next(DefEmojis.Count)]);
            player.Emotes.Add(5, DefEmojis[Random.Shared.Next(DefEmojis.Count)]);

            player.Spray.Add(0,1);
            player.Spray.Add(1, 3);
            player.Spray.Add(2, 2);
            player.Spray.Add(3, 4);
            player.Spray.Add(4, 5);
            player.Gear1 = null;
            player.Gear2 = null;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;
            player.Bot = 1;
            player.CharacterDatas[0] = (DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(character));
            foreach (SkinData skin1 in DataTables.Get(DataType.Skin).GetDatas())
            {
                SkinConfData skin = DataTables.Get(DataType.SkinConf).GetData<SkinConfData>(skin1.Conf);
                if (skin1 != null && !skin1.Disabled && skin != null && player.CharacterDatas[0] != null && skin.Character == player.CharacterDatas[0].Name && !skin.Name.EndsWith("Default") && skin1.PriceGems > 28 && !GeneralStaticLogic.BlockedObtainTypes.Contains(skin1.ObtainType))

                {

                   SkinsForBot.Add(skin1.GetInstanceId());
                    
                }
            }
            
            player.SkinIds[0] = GlobalId.CreateGlobalId(29, SkinsForBot[Random.Shared.Next(SkinsForBot.Count)]);
            player.HeroPowerLevel = new Random().Next(5, 11);
            if(new Random().Next(10) == 5)player.T1 = GlobalId.CreateGlobalId((int)DataType.PlayerThumbnail, IconForBot[Random.Shared.Next(IconForBot.Count)]);
            if (new Random().Next(6) == 1) player.T2 = GlobalId.CreateGlobalId((int)DataType.PlayerThumbnail, IconForBot[Random.Shared.Next(IconForBot.Count)]);
            if (new Random().Next(4) == 2) player.E = GlobalId.CreateGlobalId((int)DataType.Emote, DefEmojis[Random.Shared.Next(DefEmojis.Count)]);
            if (new Random().Next(7) == 2) player.Ti = GlobalId.CreateGlobalId((int)DataType.Titul, TitleForBot[Random.Shared.Next(TitleForBot.Count)]);
            player.HeroIndexMax = 0;

            return player;
        }
        public static BattlePlayer CreateStoryModeDummy(string name, int playerIndex, int teamIndex, int character = 0, int skinid = 0, int accessory = 0)
        {

            BattlePlayer player = new BattlePlayer();
            player.AccessoryCardDatas = new CardData[3];
            player.AccessoryDatas = new AccessoryData[3];
            player.StarPowerDatas = new CardData[3];
            player.CharacterDatas = new CharacterData[3];
            player.OverChargeDatas = new CardData[3];
            player.DisplayData.Name = name;
            player.DisplayData.ThumbnailId = GlobalId.CreateGlobalId(28, 0);
            player.DisplayData.NameColorId = GlobalId.CreateGlobalId(43, 0);
            player.AccountId = 100000 + playerIndex;
            player.CharacterIds[0] = (GlobalId.CreateGlobalId(16, character));
            if (accessory != 0)
            {
                //player.AccessoryCardDatas[0] = null;
                //player.AccessoryDatas[0] = DataTables.Get(DataType.Accessory).GetData<AccessoryData>(player.AccessoryCardDatas[0].Name);
            }
            
            player.HeroIndexMax = 0;
            player.Accessory = null;
            player.Gear1 = null;
            player.Gear2 = null;
            player.PlayerIndex = playerIndex;
            player.TeamIndex = teamIndex;
            player.SessionId = -1;
            if (skinid != 0) player.SkinIds[0] = (GlobalId.CreateGlobalId(29, skinid));
            player.CharacterDatas[0] = (DataTables.Get(DataType.Character).GetDataWithId<CharacterData>(character));

            return player;
        }

    }
}
