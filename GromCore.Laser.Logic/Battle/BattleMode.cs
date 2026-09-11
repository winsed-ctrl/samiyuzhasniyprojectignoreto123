using GromCore.Laser.Logic.Stream.Entry;
using System.Threading.Tasks;

namespace GromCore.Laser.Logic.Battle
{
    using DeenGames.Utils.AStarPathFinder;
    using Masuda.Net.HelpMessage;
    using Masuda.Net.Models;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using GromCore.Laser.Logic.Avatar;
    using GromCore.Laser.Logic.Battle.Component;
    using GromCore.Laser.Logic.Battle.Input;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Battle.Level.Factory;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Command.Home;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;
    using GromCore.Laser.Logic.Home;
    using GromCore.Laser.Logic.Home.Items;
    using GromCore.Laser.Logic.Home.Structures;
    using GromCore.Laser.Logic.Listener;
    using GromCore.Laser.Logic.Message.Account;
    using GromCore.Laser.Logic.Message.Account.Auth;
    using GromCore.Laser.Logic.Message.Battle;
    using GromCore.Laser.Logic.Message.Club;
    using GromCore.Laser.Logic.Message.Home;
    using GromCore.Laser.Logic.Notification;
    using GromCore.Laser.Logic.Ranked;
    using GromCore.Laser.Logic.Time;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Titan.Json;
    using GromCore.Laser.Titan.Math;
    using GromCore.Laser.Titan.Util;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Frozen;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.ComponentModel.Design;
    using System.Diagnostics.Contracts;
    using System.Diagnostics.Metrics;
    using System.Globalization;
    using System.Numerics;
    using System.Reflection.Emit;
    using System.Reflection.Metadata;
    using System.Reflection.Metadata.Ecma335;
    using System.Runtime.CompilerServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using System.Threading;
    using static GromCore.Laser.Logic.Battle.Objects.Character;

    public struct DiedEntry
    {
        public int DeathTick;
        public BattlePlayer Player;
    }

    public class KingOfHillState
    {
        public int ZoneIndex;
        public int ZonePercentage; 
        public int ZonePercentageTeam0 { get; set; } = 0;
        public int ZonePercentageTeam1 { get; set; } = 0;
        public bool CaptureEffectActiveTeam0 { get; set; }
        public bool CaptureEffectActiveTeam1 { get; set; }
        public AreaEffect CurrentCaptureEffectTeam0 { get; set; }
        public AreaEffect CurrentCaptureEffectTeam1 { get; set; }

        public bool GaugeTeam0 { get; set; }
        public bool GaugeTeam1 { get; set; }
        public AreaEffect GaugeTeam0Effect { get; set; }
        public AreaEffect GaugeTeam1Effect { get; set; }

        public int LastUpTickTeam0 { get; set; }
        public int LastUpTickTeam1 { get; set; }
        public bool HillCompleteEffectSpawnedTeam0 { get; set; }
        public bool CanContinueUpTeam0 = true;

        public bool HillCompleteEffectSpawnedTeam1 { get; set; }
        public bool CanContinueUpTeam1 = true;

        public KingOfHillState(int zoneIndex)
        {
            this.ZoneIndex = zoneIndex;
        }
    }

    public class CarryableBallState
    {
        public Character Ball { get; set; }
        public bool IsStuck { get; set; }
        public int StuckDetectionTick { get; set; }
        public int LastPositionX { get; set; }
        public int LastPositionY { get; set; }
        public int StuckCheckCounter { get; set; }
        public bool IsBeingReset { get; set; }

        public void ResetStuckDetection(int currentTick, int x, int y)
        {
            IsStuck = false;
            StuckDetectionTick = currentTick;
            LastPositionX = x;
            LastPositionY = y;
            StuckCheckCounter = 0;
        }

        public bool CheckIfStuck(int currentTick, int x, int y)
        {
            if (Ball == null || !Ball.IsAlive()) return false;

            if (LastPositionX == x && LastPositionY == y)
            {
                StuckCheckCounter++;
                if (StuckCheckCounter >= 300)
                {
                    IsStuck = true;
                    return true;
                }
            }
            else
            {
                StuckCheckCounter = 0;
                LastPositionX = x;
                LastPositionY = y;
            }
            return false;
        }
    }

    public class BattleMode
    {
        public const int ORBS_TO_COLLECT_NORMAL = 0xA;
        public const int INTRO_TICKS = 180;
        public const int NO_TIME_TICKS = 16000;
        public const int NORMAL_TICKS = 3180;
        public StartLoadingMessage Dummy;
        public Timer m_updateTimer;
        public int m_locationId;
        public int m_gameModeVariation;
        private int m_playersCountWithGameModeVariation;
        public bool BattleWithTrophies;
        private Queue<ClientInput> m_inputQueue;
        public List<BattlePlayer> m_players;
        private Dictionary<long, BattlePlayer> m_playersBySessionId;
        private Dictionary<long, LogicGameListener> m_spectators;
        private PROJECTGROM_ONTOP m_gameObjectManager;
        private Rect m_playArea;
        private TileMap m_tileMap;
        public GameTime m_time;
        private LogicRandom m_random;
        private int m_randomSeed;
        public List<GameObject> StoryModeStuffs;
        public int m_winnerTeam;
        public BattlePlayerMap BattlePlayerMap;
        private int m_playersAlive;
        private int m_gemGrabCountdown;
        public bool IsStoryMode;
        private LogicPathFinder _logicPathFinder;
        public bool IsGameOver { get; set; }
        public List<int> EventModifiers;
        public StoryMode StoryMode;
        private int Zone1Score;
        private int Zone2Score;
        public int _playersAliveForDuo;
        public int _StopTicks;
        public bool _SetStopTicks;
        private int Team1King;
        private int Team2King;
        List<int> PlayerIndexesForTeam1 = new List<int> { 0, 1, 2 };
        List<int> PlayerIndexesForTeam2 = new List<int> { 3, 4, 5 };
        private int m_ticksSinceLastExplosion = 0;
        private const int ExplosionInterval = 20 * 15;
        private List<KingOfHillState> _koHStates = new List<KingOfHillState>();
        public Character Carryable;
        public Character CarryableTeam0;
        public Character CarryableTeam1;
        private bool _pinsReseted;
        public int EndTicks = 0;
        public bool IsChampionship;
        public int ToGoalTeam = -1;
        public int GoalTicks;
        public bool CanReset = true;
        public EventData ChampieData { private get; set; }
        public bool IsSecret;
        public int TmpBrawlballTick;
        public bool IsChaos;
        public bool IsRanked;
        public int RankedId;
        public int Round;

        public int RoundTicks;
        public int RoundResetTicks;
        public List<int> TScore = new List<int> { 0, 0 };
        public int RoundCount;
        public int[] RoundWins = { 0, 0, 0 };
        public int SpikeTileTick;
        private List<BattlePlayer> _playersSnapshot;
        public bool IsBrawlTV;
        public int Diff;
        public int CoopRoundTick;
        public int CoopRound;
        public bool IsSpecialTrophyMode;

        private CarryableBallState _carryableBallState;
        private int _lastCarryableResetTick = 0;
        private const int CARRYABLE_RESET_COOLDOWN_TICKS = 600;

        public readonly Dictionary<int, int> CoopTickPerRound = new Dictionary<int, int>
        {
            {1, 15*20 },
            {2, 14*20 },
            {3, 13*20 },
            {4, 13*20 },
            {5, 13*20 },
            {6, 12*20 },
            {7, 12*20 },
            {8, 12*20 },
            {9, 11*20 },
            {10, 11*20 },
            {11, 11*20 },
            {12, 10*20 },
            {13, 9*20 },
            {14, 8*20 },
            {15, 8*20 },
            {16, 7*20 }
        };
        public bool IsPiggy;
        public static bool FreezeBots = true;
        public BattleMode(int locationId)
        {
            m_winnerTeam = -1;
            m_locationId = locationId;
            m_gameModeVariation = GameModeUtil.GetGameModeVariation(Location.GameModeVariation);
            EventModifiers = new List<int>();
            if (m_gameModeVariation == 3 && EventModifiers.Contains(15)) m_playersCountWithGameModeVariation = 2;
            else m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(m_gameModeVariation);
            if (LogicServerListener.Instance.IsDev() && false) m_playersCountWithGameModeVariation = 1;
            m_inputQueue = new Queue<ClientInput>();
            m_randomSeed = 0;
            m_random = new LogicRandom(m_randomSeed);
            m_players = new List<BattlePlayer>();
            m_playersBySessionId = new Dictionary<long, BattlePlayer>();
            m_time = new GameTime();
            m_tileMap = TileMapFactory.CreateTileMap(Location.Map);
            m_playArea = new Rect(0, 0, m_tileMap.LogicWidth, m_tileMap.LogicHeight);
            m_gameObjectManager = new PROJECTGROM_ONTOP(this);
            m_spectators = new Dictionary<long, LogicGameListener>();
            _logicPathFinder = new LogicPathFinder(GetTileMap(), true);
            PlayerIndexesForTeam1.Clear();
            PlayerIndexesForTeam2.Clear();
            _playersSnapshot = m_players;
            _carryableBallState = new CarryableBallState();
        }
        

        public int TimerMath(DateTime timer_start, DateTime timer_end)
        {
            {
                DateTime timer_now = DateTime.Now;
                if (timer_now > timer_start)
                {
                    if (timer_now < timer_end)
                        return (int)(timer_end - timer_now).TotalSeconds;
                    else
                        return -1;
                }
                else
                    return -1;
            }
        }
        public void SetPlayerMap(BattlePlayerMap map)
        {
            BattlePlayerMap = map;
            m_tileMap = TileMapFactory.CreatePlayerMap(map.MapData);
            m_gameModeVariation = map.GMV;
            m_playersCountWithGameModeVariation = GamePlayUtil.GetPlayerCountWithGameModeVariation(map.GMV);
        }
        public void SetEventModifiers(List<int> ms) => EventModifiers = ms.ToList();

        #region ReturnMethods
        public LogicPathFinder GetPathFinder() => _logicPathFinder ??= new LogicPathFinder(GetTileMap(), true);
        public bool IsInPlayArea(int x, int y) => m_playArea.IsInside(x + 150, y + 150);
        public int GetGemGrabCountdown() => m_gemGrabCountdown;
        public LogicRandom GetLogicRandom() => m_random;
        public PROJECTGROM_ONTOP GetGameObjectManager() => m_gameObjectManager;
        public int GetPlayersAliveCountForBattleRoyale() => m_playersAlive;
        public bool HasEventModifier(int m) => EventModifiers.Contains(m);
        public int GetKOHZoneCount() => _koHStates.Count;
        public KingOfHillState GetZoneByUID(int a1) => _koHStates[a1] != null ? _koHStates[a1] : null;
        public LocationData Location
        {
            get
            {
                return DataTables.Get(DataType.Location).GetDataByGlobalId<LocationData>(m_locationId);
            }
        }
        public int GetZone2Score() => Zone2Score;
        public int GetTeam1King() => Team1King;
        public int GetTeam2King() => Team2King;
        public int GetZone1Score() => Zone1Score;
        public BattlePlayer[] GetPlayers() => m_players.ToArray();
        public int GetRandomInt(int min, int max) => m_random.Rand(max - min) + min;
        public int GetRandomInt(int max) => m_random.Rand(max);
        public int GetTicksGone() => m_time.GetTick();
        public int GetGameModeVariation() => m_gameModeVariation;
        public int GetPlayersCountWithGameModeVariation() => m_playersCountWithGameModeVariation;
        public int GetRandomSeed() => m_randomSeed;
        public bool IsPlayFieldMirroredForPlayer(int teamIndex) => teamIndex > 0 && GameModeUtil.HasTwoTeams(m_gameModeVariation);
        public BattlePlayer GetPlayer(int globalId)
        {
            if (m_gameObjectManager.GetGameObjectByID(globalId) == null) return null;
            return m_gameObjectManager.GetGameObjectByID(globalId).GetPlayer();
        }
        public TileMap GetTileMap() => m_tileMap;
        public int GetGoalTeam() => ToGoalTeam;
        public int ChampieWinCount()
        {
            int chislo = 0;
            var events = LogicServerListener.Instance.GetEvents();
            foreach (EventData data in LogicServerListener.Instance.GetEvents())
            {
                if (data.Slot < 20) continue;
                if (data.Slot > 24) continue;
                chislo += 3;
            }
            return chislo;
        }
        public static int CalculateIndexForChallengeWinsFrom(int wins)
        {
            if (wins <= 0) return 0;
            return (wins - 1) % 3;
        }
        public static int CalculateLosesFrom(int lose)
        {
            const int maxAttempts = 3;
            return Math.Max(maxAttempts - lose, 0);
        }
        #endregion
        public void AddWinInLaserBall(int teamIndex)
        {
            BattlePlayer plr = m_players.Find(p => p.PlayerIndex == teamIndex);

            if (plr != null)
            {
                plr.AddScore(1);
            }
        }
        public void SetGoalLaserBall(int team)
        {
            ToGoalTeam = team;
            GoalTicks = 3 * 20;
        }
        public bool nokbool;
        public void ResetRoundLaserBall(bool ballr, bool isResettedByServerError = false)
        {
            if (_carryableBallState.IsBeingReset) return;
            if (!CanReset && ballr) return;
            if (_SetStopTicks) return;

            try
            {
                _carryableBallState.IsBeingReset = true;

                if (isResettedByServerError && Carryable != null)
                {
                    try
                    {
                        Carryable.CauseDamage(null, 99999, 99999, false, null, false, false, -1, -1, false, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Carryable Reset] Error destroying stuck ball: {ex.Message}");
                    }
                }

                foreach (Character character in m_gameObjectManager.GetCharacters())
                {
                    if (character == null) continue;

                    if (character.GetPlayer() != null && character.CharacterData.IsHero())
                    {
                        try
                        {
                            m_gameObjectManager.RemoveGameObject(character);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error removing character: {ex.Message}");
                        }
                    }
                    else
                    {
                        try
                        {
                            character.CauseDamage(null, 99999, 99999, false, null, false);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error damaging character: {ex.Message}");
                        }
                    }
                }

                foreach (BattlePlayer playerrrr in m_players)
                {
                    if (playerrrr != null)
                    {
                        try
                        {
                            InstantRespawn(playerrrr);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error respawning player: {ex.Message}");
                        }
                    }
                }

                foreach (Item item in m_gameObjectManager.GetItems())
                {
                    if (item != null)
                    {
                        try
                        {
                            m_gameObjectManager.RemoveGameObject(item);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error removing item: {ex.Message}");
                        }
                    }
                }

                foreach (AreaEffect area in m_gameObjectManager.GetAreaEffects())
                {
                    if (area != null)
                    {
                        try
                        {
                            m_gameObjectManager.RemoveGameObject(area);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error removing area effect: {ex.Message}");
                        }
                    }
                }

                foreach (Projectile proj in m_gameObjectManager.GetProjectiles())
                {
                    if (proj != null)
                    {
                        try
                        {
                            m_gameObjectManager.RemoveGameObject(proj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Carryable Reset] Error removing projectile: {ex.Message}");
                        }
                    }
                }

                m_gameObjectManager.Petrols.Clear();

                if (!ballr)
                {
                    nokbool = !nokbool;
                    RoundTicks = 0;
                    _carryableBallState.IsBeingReset = false;
                    return;
                }

                CharacterData ballData = DataTables.Get(16).GetData<CharacterData>("LaserBall");
                if (ballData == null)
                {
                    Console.WriteLine("[Carryable Reset] CRITICAL: LaserBall data not found!");
                    _carryableBallState.IsBeingReset = false;
                    return;
                }

                Character ball = new Character(ballData);
                ball.SetPosition(3150, 4950, 0);
                ball.SetIndex(0);
                m_gameObjectManager.AddGameObject(ball);
                Carryable = ball;

                _carryableBallState.Ball = ball;
                _carryableBallState.ResetStuckDetection(GetTicksGone(), ball.GetX(), ball.GetY());
                _carryableBallState.IsBeingReset = false;
                _lastCarryableResetTick = GetTicksGone();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Carryable Reset] CRITICAL ERROR during reset: {ex.Message}");
                _carryableBallState.IsBeingReset = false;
            }
        }

        private void CheckAndFixStuckCarryable()
        {
            if (_carryableBallState.IsBeingReset) return;

            bool isCarryableMode = (m_gameModeVariation == 5 || m_gameModeVariation == 21 || m_gameModeVariation == 16);
            if (!isCarryableMode) return;

            if (Carryable == null) return;

            if (!Carryable.IsAlive())
            {
                Console.WriteLine("[Carryable Fix] Ball is dead, respawning...");
                ForceRespawnCarryable();
                return;
            }

            int currentX = Carryable.GetX();
            int currentY = Carryable.GetY();
            int currentTick = GetTicksGone();

            if (_carryableBallState.CheckIfStuck(currentTick, currentX, currentY))
            {
                Console.WriteLine($"[Carryable Fix] Ball detected as stuck at position ({currentX}, {currentY})! Resetting...");
                ForceRespawnCarryable();
                return;
            }

            if (!IsInPlayArea(currentX, currentY))
            {
                Console.WriteLine($"[Carryable Fix] Ball out of play area at ({currentX}, {currentY})! Resetting...");
                ForceRespawnCarryable();
                return;
            }

            int tileX = currentX / 300;
            int tileY = currentY / 300;
            if (tileX >= 0 && tileX < m_tileMap.Width && tileY >= 0 && tileY < m_tileMap.Height)
            {
                Tile tile = m_tileMap.GetTile(tileX, tileY, true);
                if (tile != null && tile.Data.BlocksMovement && !tile.IsDestructed())
                {
                    Console.WriteLine($"[Carryable Fix] Ball stuck in wall at ({currentX}, {currentY})! Resetting...");
                    ForceRespawnCarryable();
                    return;
                }
            }

            if (_carryableBallState.LastPositionX != currentX || _carryableBallState.LastPositionY != currentY)
            {
                _carryableBallState.LastPositionX = currentX;
                _carryableBallState.LastPositionY = currentY;
                _carryableBallState.StuckCheckCounter = 0;
            }
        }

        private void ForceRespawnCarryable()
        {
            if (_carryableBallState.IsBeingReset) return;

            int currentTick = GetTicksGone();
            if (currentTick - _lastCarryableResetTick < CARRYABLE_RESET_COOLDOWN_TICKS)
            {
                Console.WriteLine($"[Carryable Fix] Reset on cooldown, skipping...");
                return;
            }

            try
            {
                _carryableBallState.IsBeingReset = true;

                Character oldCarrier = Carryable?.CarringCharacter;

                if (Carryable != null)
                {
                    try
                    {
                        Carryable.CauseDamage(null, 99999, 99999, false, null, false, false, -1, -1, false, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Carryable Fix] Error destroying old ball: {ex.Message}");
                    }
                }

                CharacterData ballData = DataTables.Get(16).GetData<CharacterData>("LaserBall");
                if (ballData == null)
                {
                    Console.WriteLine("[Carryable Fix] CRITICAL: LaserBall data not found!");
                    _carryableBallState.IsBeingReset = false;
                    return;
                }

                Character newBall = new Character(ballData);

                int centerX = GetTileMap().LogicWidth / 2;
                int centerY = GetTileMap().LogicHeight / 2;
                newBall.SetPosition(centerX, centerY, 0);
                newBall.SetIndex(0);
                m_gameObjectManager.AddGameObject(newBall);
                Carryable = newBall;

                if (oldCarrier != null)
                {
                    oldCarrier.CarryablePersonImunTicks = 0;
                }

                _carryableBallState.Ball = newBall;
                _carryableBallState.ResetStuckDetection(currentTick, newBall.GetX(), newBall.GetY());
                _carryableBallState.IsBeingReset = false;
                _lastCarryableResetTick = currentTick;

                Console.WriteLine($"[Carryable Fix] Ball successfully respawned at ({centerX}, {centerY})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Carryable Fix] CRITICAL ERROR during force respawn: {ex.Message}");
                _carryableBallState.IsBeingReset = false;
            }
        }

        public int MasteryReward(int trophies)
        {
            int reward = (trophies / 100 * 5) + 5;
            if (reward > 100) reward = 100;
            return reward;
        }
        public Petrol AddPetrol(int a2, int a3, int a4, int a5, int a6, int a7)
        {
            Petrol v32 = new Petrol(a2, a3, a4, a5, a6, a7);
            m_gameObjectManager.Petrols.Add(v32);
            return v32;
        }
        public void InstantRespawn(BattlePlayer player)
        {
            if (player == null) return;

            try
            {
                foreach (Character sahur in m_gameObjectManager.GetCharacters())
                {
                    if (sahur == null) continue;
                    if (sahur.GetPlayer() != null && sahur.GetPlayer().PlayerIndex == player.PlayerIndex)
                    {
                        m_gameObjectManager.RemoveGameObject(sahur);
                    }
                }

                if (player.CharacterData == null)
                {
                    Console.WriteLine("[InstantRespawn] Player CharacterData is null!");
                    return;
                }

                Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);

                LogicVector2 spawnPoint = player.GetSpawnPoint();
                if (spawnPoint == null)
                {
                    Console.WriteLine("[InstantRespawn] Spawn point is null!");
                    character.SetPosition(3150, 4950, 0);
                }
                else
                {
                    character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                }

                character.SetBot(player.IsBot());
                character.SpawnTick = GetTicksGone();
                player.OwnObjectId = character.GetGlobalID();
                player.IsAlive = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[InstantRespawn] Error: {ex.Message}");
            }
        }
        private void TickSpawnHeroes()
        {
            if (GoalTicks > 0) return;
            foreach (BattlePlayer player in m_players)
            {
                if (player == null) continue;

                if (m_gameModeVariation == 9 && m_gameObjectManager.GetGameObjectByID(player.OwnObjectId) != null)
                {
                    GameObject obj = m_gameObjectManager.GetGameObjectByID(player.OwnObjectId);
                    if (obj != null)
                    {
                        player.SetSpawnPoint(obj.GetX(), obj.GetY());
                    }
                }
                if (player.IsAlive) continue;
                if (m_gameModeVariation == 6 || m_gameModeVariation == 20) return;

                LogicVector2 spawnPoint = player.GetSpawnPoint();
                if (spawnPoint == null) continue;

                if (player.CharacterData != null && player.CharacterData.Name == "WeaponThrower")
                    player.ChargeUlti(4000, false, false);

                AreaEffect v90 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName("HeroSpawn"));
                if (v90 == null) continue;

                int randomindex = new Random().Next(3);
                if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation) && m_gameModeVariation != 9)
                {
                    if (player.DeathTick != 1)
                    {
                        v90.SetPosition(spawnPoint.X, spawnPoint.Y, 0);
                        if (m_gameModeVariation == 5 && player.TeamIndex == 0 && GetTileMap().LaserBallInstantRespawnTileTeam1 != null && GetTileMap().LaserBallInstantRespawnTileTeam1.Count > randomindex)
                            v90.SetPosition(
                                GetTileMap().LaserBallInstantRespawnTileTeam1[randomindex].X + 150,
                                GetTileMap().LaserBallInstantRespawnTileTeam1[randomindex].Y + 150,
                                0);
                        if (m_gameModeVariation == 5 && player.TeamIndex == 1 && GetTileMap().LaserBallInstantRespawnTileTeam2 != null && GetTileMap().LaserBallInstantRespawnTileTeam2.Count > randomindex)
                            v90.SetPosition(
                                GetTileMap().LaserBallInstantRespawnTileTeam2[randomindex].X + 150,
                                GetTileMap().LaserBallInstantRespawnTileTeam2[randomindex].Y + 150,
                                0);

                        v90.SetIndex(player.TeamIndex * 16 + player.PlayerIndex);
                        m_gameObjectManager.AddGameObject(v90);
                        v90.Trigger();
                    }
                }

                if (m_gameModeVariation == 9)
                {
                    if (IsTeammateAlive(player.TeamIndex, player.PlayerIndex))
                    {
                        if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation))
                        {
                            Character teammate = GetTeammate(player.TeamIndex, player.PlayerIndex);
                            if (teammate != null && teammate.GetPlayer() != null)
                            {
                                LogicVector2 teammateSpawn = teammate.GetPlayer().GetSpawnPoint();
                                if (teammateSpawn != null)
                                {
                                    v90.SetPosition(teammateSpawn.X, teammateSpawn.Y, 0);
                                    v90.SetIndex(player.TeamIndex * 16 + player.PlayerIndex);
                                    m_gameObjectManager.AddGameObject(v90);
                                    v90.Trigger();
                                    player.ChainedRespawnAOE = v90;
                                }
                            }
                        }

                        if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation) + 39)
                        {
                            if (player.CharacterData != null)
                            {
                                Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
                                if (player.ChainedRespawnAOE != null)
                                {
                                    character.SetPosition(player.ChainedRespawnAOE.GetX(), player.ChainedRespawnAOE.GetY(), 0);
                                }
                                character.SetBot(player.IsBot());
                                character.SpawnTick = GetTicksGone();
                                character.IsInvincible = false;
                                player.OwnObjectId = character.GetGlobalID();
                                player.IsAlive = true;
                            }
                        }
                    }
                    return;
                }
                                if (GetTicksGone() == player.DeathTick + 20 * GameModeUtil.GetRespawnSeconds(m_gameModeVariation) + 40)
                {
                    if (player.CharacterData == null) continue;

                    Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);

                    character.SetPosition(spawnPoint.X, spawnPoint.Y, 0);

                    if (m_gameModeVariation == 5 && player.TeamIndex == 0 && GetTileMap().LaserBallInstantRespawnTileTeam1 != null && GetTileMap().LaserBallInstantRespawnTileTeam1.Count > randomindex)
                        character.SetPosition(
                            GetTileMap().LaserBallInstantRespawnTileTeam1[randomindex].X + 150,
                            GetTileMap().LaserBallInstantRespawnTileTeam1[randomindex].Y + 150,
                            0);
                    if (m_gameModeVariation == 5 && player.TeamIndex == 1 && GetTileMap().LaserBallInstantRespawnTileTeam2 != null && GetTileMap().LaserBallInstantRespawnTileTeam2.Count > randomindex)
                        character.SetPosition(
                            GetTileMap().LaserBallInstantRespawnTileTeam2[randomindex].X + 150,
                            GetTileMap().LaserBallInstantRespawnTileTeam2[randomindex].Y + 150,
                            0);
                    character.SetBot(player.IsBot());
                    character.SpawnTick = GetTicksGone();
                    character.IsInvincible = false;
                    player.OwnObjectId = character.GetGlobalID();
                    player.IsAlive = true;
                }
            }
        }

        public Character SpawnHero(CharacterData a2, int a3, int a4, int a5, bool a6)
        {
            if (a2 == null) return null;

            Character v9 = new Character(a2);
            m_gameObjectManager.AddGameObject(v9);
            if (a2.AreaEffect != null)
                v9.AddAreaEffect(0, 0, null, 1, false);
            v9.SetIndex(a4);
            if (IsChampionship) a3 = 11;
            v9.SetUpgrades(a3);
            if (m_gameModeVariation == 13 && v9.GetPlayer() != null) v9.ChargeUlti(8000, true, true, v9.GetPlayer(), null);
            if (v9.GetPlayer() != null)
            {
                if (v9.GetPlayer().Gear1 != null) v9.Gear1 = new(v9.GetPlayer().Gear1);
                if (v9.GetPlayer().Gear2 != null) v9.Gear2 = new(v9.GetPlayer().Gear2);
            }

            return v9;
        }

        static void AddKillsToFile(long killsToAdd, string path = "kills.txt")
        {

            long current = 0;

            if (File.Exists(path))
            {
                var text = File.ReadAllText(path).Trim();
                long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out current);
            }

            long updated = checked(current + killsToAdd);
            File.WriteAllText(path, updated.ToString(CultureInfo.InvariantCulture));
        }
        public void PlayerDied(BattlePlayer player)
        {
            if (player == null) return;

            if (m_gameModeVariation == 0) player.ResetScore();
            player.Deaths += 1;
            if (!IsTeammateAlive(player.TeamIndex, player.PlayerIndex) && m_gameModeVariation == 9)
            {
                foreach (BattlePlayer bplr in GetPlayersByTeam(player.TeamIndex))
                {
                    if (bplr != null && !bplr.CanSendBattleEnd)
                    {
                        SendBattleEndToPlayer(bplr);
                        bplr.CanSendBattleEnd = true;
                    }
                }
            }
            if (m_gameModeVariation == 6)
            {
                int rank = m_playersAlive;
                m_playersAlive--;
                player.BattleRoyaleRank = rank;
                BattleEndMessage message = new BattleEndMessage();
                message.GameMode = 2;
                message.IsPvP = BattleWithTrophies;
                message.BattleWithoutTrophies = !BattleWithTrophies;
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;

                if (BattleWithTrophies)
                {
                    try
                    {
                        HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                        if (homeMode != null)
                        {
                            if (homeMode.Home.Quests != null)
                            {
                                message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                            }
                        }
                    }
                    catch {; }



                    if (player.Avatar == null) return;
                    player.Avatar.BattleId = -1;

                    if (player.GameListener == null) return;

                    Hero hero = player.Avatar.GetHero(player.CharacterId);
                    if (IsSpecialTrophyMode)
                    {
                        message.Minus = 2;
                        message.Plus = player.Kills*2;
                        if (hero != null) hero.AddTrophies(-2);
                        if (hero != null) hero.AddTrophies(message.Plus);
                    }
                    int oldTrophies = hero != null ? hero.HighestTrophies : 0;
                    if (hero != null && hero.Trophies >= hero.HighestTrophies)
                    {
                        hero.HighestTrophies = hero.Trophies;
                    }

                    message.Result = rank;
                    int tokensReward = 40 / rank;
                    if (BattleWithTrophies) message.TokensReward = tokensReward;
                    if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                    {
                        if (tokensReward * 2 <= player.Home.TokenDoublers)
                        {
                            player.Home.TokenDoublers -= tokensReward;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = player.Home.TokenDoublers;
                            tokensReward *= 2;
                        }
                        else
                        {
                            tokensReward += player.Home.TokenDoublers;
                            player.Home.TokenDoublers = 0;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = 0;
                        }
                    }

                    int brawlerTrophies = 0;
                    if (hero != null)
                    {
                        brawlerTrophies = hero.Trophies;
                    }

                    int[] Trophies = new int[10];
                    if (brawlerTrophies <= 49)
                        Trophies = new[] { 10, 8, 7, 6, 4, 2, 2, 1, 0, 0 };
                    else if (brawlerTrophies <= 99)
                        Trophies = new[] { 10, 8, 7, 6, 3, 2, 2, 0, -1, -2 };
                    else if (brawlerTrophies <= 199)
                        Trophies = new[] { 10, 8, 7, 6, 3, 1, 0, -1, -2, -2 };
                    else if (brawlerTrophies <= 299)
                        Trophies = new[] { 10, 8, 6, 5, 3, 1, 0, -2, -3, -3 };
                    else if (brawlerTrophies <= 399)
                        Trophies = new[] { 10, 8, 6, 5, 2, 0, 0, -3, -4, -4 };
                    else if (brawlerTrophies <= 499)
                        Trophies = new[] { 10, 8, 6, 5, 2, -1, -2, -3, -5, -5 };
                    else if (brawlerTrophies <= 599)
                        Trophies = new[] { 10, 8, 6, 4, 2, -1, -2, -5, -6, -6 };
                    else if (brawlerTrophies <= 699)
                        Trophies = new[] { 10, 8, 6, 4, 1, -2, -2, -5, -7, -8 };
                    else if (brawlerTrophies <= 799)
                        Trophies = new[] { 10, 8, 6, 4, 1, -3, -4, -5, -8, -9 };
                    else if (brawlerTrophies <= 899)
                        Trophies = new[] { 9, 7, 5, 2, 0, -3, -4, -7, -9, -10 };
                    else if (brawlerTrophies <= 999)
                        Trophies = new[] { 8, 6, 4, 1, -1, -3, -6, -8, -10, -11 };
                    else if (brawlerTrophies <= 1099)
                        Trophies = new[] { 6, 5, 3, 1, -2, -5, -6, -9, -11, -12 };
                    else if (brawlerTrophies <= 1199)
                        Trophies = new[] { 5, 4, 1, 0, -2, -6, -7, -10, -12, -13 };
                    else // 1200+
                        Trophies = new[] { 5, 3, 0, -1, -2, -6, -8, -11, -12, -13 };

                    if (rank < 5 && BattleWithTrophies)
                    {
                        player.Avatar.Wins += 1;
                        switch (player.Avatar.Wins)
                        {
                            case 1:
                            case 4:
                            case 8:
                                if (player.Home != null)
                                {
                                    player.Home.DropsCount++;
                                    if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                }
                                break;
                            default:
                                if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                break;
                        }

                        player.Avatar.WinStreak += 1;
                        if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                        message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                        if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                        player.Avatar.AddTrophies(message.WinstreakTrophies);

                        int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                        int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                        if (vipBonusTrophies > 0)
                        {
                            trophiesReward += vipBonusTrophies;
                            player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков (всего: {player.Avatar.VipTotalBonusReceived})");
                        }

                        int vipBonusMastery = player.Avatar.VipBonusMastery;
                        int masteryGained = MasteryReward(brawlerTrophies);
                        if (vipBonusMastery > 0)
                        {
                            masteryGained += vipBonusMastery;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGained;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                        message.TrophiesReward = trophiesReward;

                        player.Avatar.AddTokens(tokensReward);
                        if (player.Home != null)
                        {
                            player.Home.TokenReward += tokensReward;
                            player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        }
                        if (hero != null) hero.AddTrophies(message.TrophiesReward);
                        if (BattleWithTrophies)
                        {
                            try
                            {
                                HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                                if (homeMode != null)
                                {
                                    if (homeMode.Home.Quests != null)
                                    {
                                        message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                                    }
                                }
                            }
                            catch {; }

                        }
                    }
                    else if (rank == 5 && BattleWithTrophies)
                    {
                        int vipBonusMastery = player.Avatar.VipBonusMastery;
                        int masteryGained = 25;
                        if (vipBonusMastery > 0)
                        {
                            masteryGained += vipBonusMastery;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGained;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                        int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                        message.TrophiesReward = trophiesReward;

                        player.Avatar.AddTokens(tokensReward);
                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                        if (hero != null) hero.AddTrophies(message.TrophiesReward);
                        if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                    }
                    else
                    {
                        if (BattleWithTrophies)
                        {
                            player.Avatar.WinStreak = 0;
                            int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                            message.TrophiesReward = trophiesReward;

                            player.Avatar.AddTokens(tokensReward);
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            if (player.Home != null) player.Home.TrophiesReward += trophiesReward;

                            if (hero != null) hero.AddTrophies(message.TrophiesReward);
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        }

                    }
                    int serverEventTrophyBonus = ApplyServerWinEventRewards(player, rank == 1);
                    message.TrophiesReward += serverEventTrophyBonus;
                    int newTrophies = hero != null ? hero.HighestTrophies : 0;
                    if (hero != null && BattleWithTrophies && player.Home != null)
                    {
                        for (int i = 0; i < 34; i++)
                        {
                            MilestoneData m = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId(39, i));
                            int progress = m.ProgressStart + m.Progress;
                            if (oldTrophies < progress && newTrophies >= progress)
                            {
                                message.MilestoneId = i;
                                player.Home.BlingsReward += m.SecondaryLvlUpRewardCount;
                                player.Avatar.AddBlings(m.SecondaryLvlUpRewardCount);
                                message.TokensReward += m.PrimaryLvlUpRewardCount;
                                player.Avatar.AddTokens(m.PrimaryLvlUpRewardCount);
                                player.Home.TokenReward += m.PrimaryLvlUpRewardCount;
                            }
                        }
                    }
                    message.Winstreak = player.Avatar.WinStreak;


                    player.GameListener.SendTCPMessage(message);
                    var temp = new List<BattleLogPlayerEntry>();

                    if (player.Avatar != null && player.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(player.Avatar.TeamId);

                }


            }
        }

        public BattlePlayer GetPlayerWithObject(int globalId)
        {
            GameObject gameObject = m_gameObjectManager.GetGameObjectByID(globalId);
            if (gameObject == null) return null;
            return gameObject.GetPlayer();
        }

        public void Start() => m_updateTimer = new Timer(new TimerCallback(Update), null, 0, 1000 / 20);

        public void Update(object stateInfo)
        {
            try
            {
                this.ExecuteOneTick();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BattleMode Update] CRITICAL ERROR: {ex.Message}\n{ex.StackTrace}");
                try
                {
                    if (Carryable != null && !Carryable.IsAlive())
                    {
                        ForceRespawnCarryable();
                    }
                }
                catch { }
            }
        }

        public void AddSpectator(long sessionId, LogicGameListener gameListener)
        {

            if (m_spectators.ContainsKey(sessionId))
            {
                m_spectators.Remove(sessionId);
            }
            while (!m_spectators.ContainsKey(sessionId))
            {
                if (gameListener != null)
                {
                    Console.WriteLine("sendded vhto");
                    m_spectators.Add(sessionId, gameListener);
                }
            }

        }

        public void ChangePlayerSessionId(long old, long newId)
        {
            if (m_playersBySessionId.ContainsKey(old))
            {
                BattlePlayer player = m_playersBySessionId[old];
                player.LastHandledInput = 0;
                m_playersBySessionId.Remove(old);
                m_playersBySessionId.Add(newId, player);
            }
        }

        public BattlePlayer GetPlayerBySessionId(long sessionId)
        {
            if (m_playersBySessionId.ContainsKey(sessionId))
            {
                return m_playersBySessionId[sessionId];
            }
            return null;
        }

        public void AddPlayer(BattlePlayer player, long sessionId)
        {
            if (Debugger.DoAssert(player != null, "LogicBattle::AddPlayer - player is NULL!"))
            {
                player.SessionId = sessionId;
                m_players.Add(player);
                if (m_players.Count == 1 && m_gameModeVariation == 30) StoryMode.Player = player;
                if (sessionId > 0)
                {
                    m_playersBySessionId.Add(sessionId, player);
                }
                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = Id;
                    player.Avatar.TeamIndex = player.TeamIndex;
                    player.Avatar.OwnIndex = player.PlayerIndex;
                    if (GamePlayUtil.IsAdmin(player.AccountId)) player.IsAdmin = true;
                    player.IsAdmin = player.Avatar.IsDebugAccount;
                    if (LogicServerListener.Instance.IsDev()) player.IsAdmin = true;
                }
                //if (HasEventModifier(13) || player.InfiniteUltimate) player.AddUltiCharge(4000);
                if (m_gameModeVariation == 13)
                {
                    player.Accessory = null;
                    player.AccessoryCardDatas[0] = null;
                    player.AccessoryDatas[0] = null;
                }
                if (IsChampionship) player.PowerLevel = 11;
            }
        }

        public void RemovePlayer(BattlePlayer player)
        {
            if (Debugger.DoAssert(player != null, "LogicBattle::AddPlayer - player is NULL!"))
            {
                player.GameListener = null;
                m_playersBySessionId.Remove(player.SessionId);
                if (player.Avatar != null)
                {
                    player.Avatar.BattleId = -1;
                }
            }
        }

        public void AddGameObjects()
        {
            m_playersAlive = m_players.Count;
            double averageLevel = 0;
            foreach (var p in m_players) if (p != null) averageLevel += p.HeroPowerLevel;
            averageLevel = m_players.Count > 0 ? averageLevel / m_players.Count : 0;
            int team1Indexer = 0;
            int team2Indexer = 0;
            PlayerIndexesForTeam1 = GetPlayerIndexesByTeam(0);
            PlayerIndexesForTeam2 = GetPlayerIndexesByTeam(1);
            if (m_gameModeVariation == 19)
            {
                if (PlayerIndexesForTeam1.Count > 0)
                    Team1King = PlayerIndexesForTeam1[new Random().Next(PlayerIndexesForTeam1.Count)];
                if (PlayerIndexesForTeam2.Count > 0)
                    Team2King = PlayerIndexesForTeam2[new Random().Next(PlayerIndexesForTeam2.Count)];
            }


            foreach (BattlePlayer player in m_players)
            {
                if (player == null) continue;
                Character character = SpawnHero(player.CharacterData, player.HeroPowerLevel, player.TeamIndex * 16 + player.PlayerIndex, 0, false);
                if (character == null) continue;

                if (player.CharacterData != null && player.CharacterData.Name == "WeaponThrower") player.ChargeUlti(4000, false, false);
                player.IsAlive = true;
                character.SetBot(player.IsBot());
                if (GameModeUtil.HasTwoTeams(m_gameModeVariation))
                {
                    if (player.TeamIndex == 0)
                    {
                        if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > team1Indexer)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++ % m_tileMap.SpawnPointsTeam1.Count];
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                        }
                    }
                    else
                    {
                        if (m_tileMap.SpawnPointsTeam2 != null && m_tileMap.SpawnPointsTeam2.Count > team2Indexer)
                        {
                            Tile tile = m_tileMap.SpawnPointsTeam2[team2Indexer++ % m_tileMap.SpawnPointsTeam2.Count];
                            character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                            player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                        }
                    }
                }
                else if (GameModeUtil.HasFiveTeams(m_gameModeVariation))
                {
                    Dictionary<int, int> teamSpawnCounters = null;
                    if (teamSpawnCounters == null)
                    {
                        teamSpawnCounters = new Dictionary<int, int>();
                        for (int i = 0; i < 5; i++)
                        {
                            teamSpawnCounters[i] = 0;
                        }
                    }

                    int spawnPointIndex = player.TeamIndex % 5;
                    if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > spawnPointIndex * 2)
                    {
                        Tile baseTile = m_tileMap.SpawnPointsTeam1[spawnPointIndex * 2];
                        int positionInTeam = teamSpawnCounters[player.TeamIndex] % 2;
                        teamSpawnCounters[player.TeamIndex]++;
                        int offsetX = positionInTeam * 600;
                        int offsetY = 0;

                        character.SetPosition(baseTile.X + 150 + offsetX, baseTile.Y + 150 + offsetY, 0);
                        player.SetSpawnPoint(baseTile.X + 150 + offsetX, baseTile.Y + 150 + offsetY);
                    }
                }
                else
                {
                    if (m_tileMap.SpawnPointsTeam1 != null && m_tileMap.SpawnPointsTeam1.Count > team1Indexer)
                    {
                        Tile tile = m_tileMap.SpawnPointsTeam1[team1Indexer++ % m_tileMap.SpawnPointsTeam1.Count];
                        character.SetPosition(tile.X + 150, tile.Y + 150, 0);
                        player.SetSpawnPoint(tile.X + 150, tile.Y + 150);
                    }
                }

                player.OwnObjectId = character.GetGlobalID();
                character.SetHeroLevel(player.HeroPowerLevel - 1);
                if (m_gameModeVariation == 19)
                {
                    if (player.PlayerIndex == Team2King) character.SetKing();
                    if (player.PlayerIndex == Team1King) character.SetKing();
                }
            }
            if (m_tileMap.HealTiles != null)
            {
                for (int i = 0; i < m_tileMap.HealTiles.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("Healing"));
                    HealingObject.SetPosition(m_tileMap.HealTiles[i].X + 300, m_tileMap.HealTiles[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesDown != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesDown.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardDown"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesDown[i].X + 300, m_tileMap.TramplineTilesDown[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesLeft != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesLeft.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardLeft"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesLeft[i].X + 300, m_tileMap.TramplineTilesLeft[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesRight != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesRight.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardRight"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesRight[i].X + 300, m_tileMap.TramplineTilesRight[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesUpRight != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesUpRight.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardUpRight"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesUpRight[i].X + 300, m_tileMap.TramplineTilesUpRight[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesUpLeft != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesUpLeft.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardUpLeft"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesUpLeft[i].X + 300, m_tileMap.TramplineTilesUpLeft[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesDownLeft != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesDownLeft.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardDownLeft"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesDownLeft[i].X + 300, m_tileMap.TramplineTilesDownLeft[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesDownRight != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesDownRight.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardDownRight"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesDownRight[i].X + 300, m_tileMap.TramplineTilesDownRight[i].Y + 300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            if (m_tileMap.TramplineTilesUp != null)
            {
                for (int i = 0; i < m_tileMap.TramplineTilesUp.Count; i++)
                {
                    Item HealingObject = new(DataTables.GetItemByName("SpringBoardUp"));
                    HealingObject.SetPosition(m_tileMap.TramplineTilesUp[i].X+300, m_tileMap.TramplineTilesUp[i].Y+300, 0);
                    m_gameObjectManager.AddGameObject(HealingObject);
                }
            }
            switch (m_gameModeVariation)
            {
                case 0:
                    Item OrbSpawner = new Item(DataTables.GetItemByName("OrbSpawner"));
                    OrbSpawner.SetPosition(3150, 4950, 0);
                    m_gameObjectManager.AddGameObject(OrbSpawner);
                    break;
                case 8:
                    CharacterData b1 = DataTables.GetCharacterByName("Safe");
                    if (b1 != null)
                    {
                        Character base11 = new Character(b1);
                        if (m_tileMap.CoopBaseSpawn != null && m_tileMap.CoopBaseSpawn.Count > 0)
                        {
                            base11.SetPosition(m_tileMap.CoopBaseSpawn[0].X + 150, m_tileMap.CoopBaseSpawn[0].Y + 150, 0);
                        }
                        base11.SetIndex(0);
                        m_gameObjectManager.AddGameObject(base11);
                    }
                    break;
                case 3:
                    Item Money = new Item(DataTables.Get(18).GetData<ItemData>("Money"));
                    Money.SetPosition(3150, 4950, 0);
                    m_gameObjectManager.AddGameObject(Money);
                    break;
                case 6 or 9:
                    if (m_tileMap.Height > 0 && m_tileMap.Width > 0)
                    {
                        for (int i = 0; i < m_tileMap.Height; i++)
                        {
                            for (int j = 0; j < m_tileMap.Width; j++)
                            {
                                Tile tile = m_tileMap.GetTile(i, j, true);
                                if (tile != null && tile.Code == '4')
                                {
                                    bool sh = GetRandomInt(0, 120) < 60;
                                    if (sh)
                                    {
                                        Character box = new Character(DataTables.Get(16).GetData<CharacterData>("LootBox"));
                                        if (box != null)
                                        {
                                            box.SetPosition(tile.X + 150, tile.Y + 150, 0);
                                            box.SetIndex(-16);
                                            switch ((int)Math.Floor(averageLevel))
                                            {
                                                case <= 6:
                                                    box.m_hitpoints = 4500;
                                                    box.m_maxHitpoints = 4500;
                                                    break;
                                                case <= 8:
                                                    box.m_hitpoints = 6000;
                                                    box.m_maxHitpoints = 6000;
                                                    break;
                                                case <= 10 or > 10:
                                                    box.m_hitpoints = 8000;
                                                    box.m_maxHitpoints = 8000;
                                                    break;
                                            }
                                            m_gameObjectManager.AddGameObject(box);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 2:
                    CharacterData b = DataTables.GetCharacterByName("Safe");
                    if (b != null)
                    {
                        Character base1 = new Character(b);
                        if (m_tileMap.SpawnPointsBases != null && m_tileMap.SpawnPointsBases.Count > 0)
                        {
                            base1.SetPosition(m_tileMap.SpawnPointsBases[0].X + 150, m_tileMap.SpawnPointsBases[0].Y + 150, 0);
                        }
                        base1.SetIndex(22);
                        base1.TeamIndex = 1;
                        m_gameObjectManager.AddGameObject(base1);
                        Character base2 = new Character(b);
                        if (m_tileMap.SpawnPointsBases != null && m_tileMap.SpawnPointsBases.Count > 1)
                        {
                            base2.SetPosition(m_tileMap.SpawnPointsBases[1].X + 150, m_tileMap.SpawnPointsBases[1].Y + 150, 0);
                        }
                        base2.SetIndex(6);
                        base2.TeamIndex = 0;
                        m_gameObjectManager.AddGameObject(base2);
                        if (averageLevel >= 9)
                        {
                            base1.m_hitpoints = 60000;
                            base1.m_maxHitpoints = 60000;
                            base2.m_hitpoints = 60000;
                            base2.m_maxHitpoints = 60000;
                        }
                    }
                    break;
                case 10:
                    Character wswrefd = new Character(DataTables.Get(16).GetData<CharacterData>("RaidBoss"));
                    if (wswrefd != null)
                    {
                        wswrefd.SetPosition(3000, 5000, 0);
                        wswrefd.SetIndex(-16);
                        wswrefd.MoveAngle = 90;
                        wswrefd.AttackAngle = 90;
                        m_gameObjectManager.AddGameObject(wswrefd);
                    }
                    break;
                case 13:
                    Character TrainingDummyBig = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyBig"));
                    Character TrainingDummyShooting = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyShooting"));
                    if (TrainingDummyBig != null && m_tileMap.TrainingDummyBigSpawners != null && m_tileMap.TrainingDummyBigSpawners.Count > 0)
                    {
                        TrainingDummyBig.SetPosition(m_tileMap.TrainingDummyBigSpawners[0].X + 150, m_tileMap.TrainingDummyBigSpawners[0].Y + 150, 0);
                        TrainingDummyBig.SetIndex(-16);
                        TrainingDummyBig.MoveAngle = 90;
                        TrainingDummyBig.AttackAngle = 90;
                        m_gameObjectManager.AddGameObject(TrainingDummyBig);
                    }
                    if (m_tileMap.TrainingDummyMediumSpawners != null)
                    {
                        for (int i = 0; i < m_tileMap.TrainingDummyMediumSpawners.Count; i++)
                        {
                            Character TrainingDummyMedium = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummyMedium"));
                            if (TrainingDummyMedium != null)
                            {
                                TrainingDummyMedium.SetPosition(m_tileMap.TrainingDummyMediumSpawners[i].X + 150, m_tileMap.TrainingDummyMediumSpawners[i].Y + 150, 0);
                                TrainingDummyMedium.MoveAngle = 90;
                                TrainingDummyMedium.AttackAngle = 90;
                                TrainingDummyMedium.SetIndex(-16);
                                m_gameObjectManager.AddGameObject(TrainingDummyMedium);
                            }
                        }
                    }
                    if (TrainingDummyShooting != null && m_tileMap.TrainingDummyShooting != null)
                    {
                        for (int i = 0; i < m_tileMap.TrainingDummyShooting.Count; i++)
                        {
                            Tile spawner = m_tileMap.TrainingDummyShooting[i];
                            TrainingDummyShooting.SetPosition(spawner.X + 150, spawner.Y + 150, 0);
                            TrainingDummyShooting.MoveAngle = 90;
                            TrainingDummyShooting.AttackAngle = 90;
                            TrainingDummyShooting.SetIndex(-16);
                            m_gameObjectManager.AddGameObject(TrainingDummyShooting);
                        }
                    }
                    if (m_tileMap.TrainingDummySmallSpawners != null)
                    {
                        for (int i = 0; i < m_tileMap.TrainingDummySmallSpawners.Count; i++)
                        {
                            Character TrainingDummySmall = new Character(DataTables.Get(16).GetData<CharacterData>("TrainingDummySmall"));
                            if (TrainingDummySmall != null)
                            {
                                TrainingDummySmall.SetPosition(m_tileMap.TrainingDummySmallSpawners[i].X + 150, m_tileMap.TrainingDummySmallSpawners[i].Y + 150, 0);
                                TrainingDummySmall.MoveAngle = 90;
                                TrainingDummySmall.AttackAngle = 90;
                                TrainingDummySmall.SetIndex(-16);
                                m_gameObjectManager.AddGameObject(TrainingDummySmall);
                            }
                        }
                    }

                    Item spawner1 = new Item(DataTables.GetItemByName("SpringBoardUpRight"));
                    spawner1.SetIndex(0);
                    spawner1.SetPosition(3000, 5000, 0);
                    Item spawne11 = new Item(DataTables.GetItemByName("Teleport1"));
                    spawne11.SetPosition(5000, 3000, 0);
                    AreaEffect area = new AreaEffect(DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("FishTankBlast"));
                    area.SetPosition(3000, 5000, 0);
                    area.SetIndex(16);
                    break;
                case 18:
                    Character RaidBoss_TownCrush = new Character(DataTables.Get(16).GetData<CharacterData>("RaidBoss_TownCrush"));
                    if (RaidBoss_TownCrush != null)
                    {
                        RaidBoss_TownCrush.SetPosition(3000, 5000, 0);
                        RaidBoss_TownCrush.SetIndex(-16);
                        RaidBoss_TownCrush.MoveAngle = 90;
                        RaidBoss_TownCrush.AttackAngle = 90;
                        m_gameObjectManager.AddGameObject(RaidBoss_TownCrush);
                    }
                    break;
                case 21:
                    CharacterData holdingBallData = DataTables.Get(16).GetData<CharacterData>("HoldingBall");
                    if (holdingBallData != null)
                    {
                        Character HoldingBall = SpawnHero(holdingBallData, 11, -16, 0, false);
                        if (HoldingBall != null)
                        {
                            HoldingBall.SetPosition(3150, 4950, 0);
                            m_gameObjectManager.AddGameObject(HoldingBall);
                            Carryable = HoldingBall;
                            _carryableBallState.Ball = HoldingBall;
                            _carryableBallState.ResetStuckDetection(GetTicksGone(), HoldingBall.GetX(), HoldingBall.GetY());
                        }
                    }
                    break;
                case 16:
                    AreaEffectData ctfBaseData = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("CTFHomeBase");
                    if (ctfBaseData != null && m_tileMap.CTFBaseSpawn != null && m_tileMap.CTFBaseSpawn.Count > 0)
                    {
                        AreaEffect CTFHomeBaseFor0Team = new AreaEffect(ctfBaseData);
                        CTFHomeBaseFor0Team.SetPosition(m_tileMap.CTFBaseSpawn[0].X + 150, m_tileMap.CTFBaseSpawn[0].Y + 150, 0);
                        CTFHomeBaseFor0Team.SetIndex(-16);
                        m_gameObjectManager.AddGameObject(CTFHomeBaseFor0Team);
                        CTFHomeBaseFor0Team.Trigger();
                        CharacterData captureFlagData = DataTables.Get(16).GetData<CharacterData>("CaptureFlag");
                        if (captureFlagData != null)
                        {
                            Character teleport = SpawnHero(captureFlagData, 11, -16, 0, false);
                            if (teleport != null)
                            {
                                teleport.SetPosition(m_tileMap.CTFBaseSpawn[0].X + 150, m_tileMap.CTFBaseSpawn[0].Y + 150, 0);
                                CarryableTeam0 = teleport;
                            }
                        }
                    }

                    if (ctfBaseData != null && m_tileMap.CTFBaseSpawn != null && m_tileMap.CTFBaseSpawn.Count > 1)
                    {
                        AreaEffect area1 = new AreaEffect(ctfBaseData);
                        area1.SetPosition(m_tileMap.CTFBaseSpawn[1].X + 150, m_tileMap.CTFBaseSpawn[1].Y + 150, 0);
                        area1.SetIndex(0);
                        m_gameObjectManager.AddGameObject(area1);
                        area1.Trigger();
                        CharacterData captureFlagData = DataTables.Get(16).GetData<CharacterData>("CaptureFlag");
                        if (captureFlagData != null)
                        {
                            Character teleport1 = SpawnHero(captureFlagData, 11, 0 + (16 * 0), 0, false);
                            if (teleport1 != null)
                            {
                                teleport1.SetPosition(m_tileMap.CTFBaseSpawn[1].X + 150, m_tileMap.CTFBaseSpawn[1].Y + 150, 0);
                                CarryableTeam1 = teleport1;
                            }
                        }
                    }
                    break;
                case 17:
                    if (m_tileMap.ZonesSpawnPoints != null)
                    {
                        for (int i = 0; i < m_tileMap.ZonesSpawnPoints.Count; i++)
                        {
                            AreaEffectData kingOfHillAreaData = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("KingOfHillArea");
                            if (kingOfHillAreaData != null)
                            {
                                AreaEffect KingOfHillArea = new AreaEffect(kingOfHillAreaData);
                                KingOfHillArea.SetPosition(m_tileMap.ZonesSpawnPoints[i].X + 150, m_tileMap.ZonesSpawnPoints[i].Y + 150, 0);
                                KingOfHillArea.SetIndex(16);
                                KingOfHillArea.KOHUID = i;
                                _koHStates.Add(new KingOfHillState(i));
                                m_gameObjectManager.AddGameObject(KingOfHillArea);
                                KingOfHillArea.Trigger();
                            }
                        }
                    }
                    break;
                case 5:
                    CharacterData ballData = DataTables.Get(16).GetData<CharacterData>("LaserBall");
                    if (ballData != null)
                    {
                        Character ball = new Character(ballData);
                        ball.SetPosition(3150, 4950, 0);
                        ball.SetIndex(0);
                        m_gameObjectManager.AddGameObject(ball);
                        Carryable = ball;
                        _carryableBallState.Ball = ball;
                        _carryableBallState.ResetStuckDetection(GetTicksGone(), ball.GetX(), ball.GetY());
                    }
                    break;
            }
        }

        public void RespawnCTFCarryable(Character carryable)
        {
            if (carryable == null) return;
            
            if (carryable == CarryableTeam0)
            {
                if (CarryableTeam0 != null)
                {
                    CarryableTeam0.CauseDamage(null, 99999, 99999, false, null, false);
                    CarryableTeam0.CarringCharacter = null;
                }
                CharacterData captureFlagData = DataTables.Get(16).GetData<CharacterData>("CaptureFlag");
                if (captureFlagData != null && m_tileMap.CTFBaseSpawn != null && m_tileMap.CTFBaseSpawn.Count > 0)
                {
                    Character teleport1 = SpawnHero(captureFlagData, 11, -16, 0, false);
                    if (teleport1 != null)
                    {
                        teleport1.SetPosition(m_tileMap.CTFBaseSpawn[0].X + 150, m_tileMap.CTFBaseSpawn[0].Y + 150, 0);
                        CarryableTeam0 = teleport1;
                    }
                }
            }

            if (carryable == CarryableTeam1)
            {
                if (CarryableTeam1 != null)
                {
                    CarryableTeam1.CauseDamage(null, 99999, 99999, false, null, false);
                    CarryableTeam1.CarringCharacter = null;
                }
                CharacterData captureFlagData = DataTables.Get(16).GetData<CharacterData>("CaptureFlag");
                if (captureFlagData != null && m_tileMap.CTFBaseSpawn != null && m_tileMap.CTFBaseSpawn.Count > 1)
                {
                    Character teleport1 = SpawnHero(captureFlagData, 11, 0, 0, false);
                    if (teleport1 != null)
                    {
                        teleport1.SetPosition(m_tileMap.CTFBaseSpawn[1].X + 150, m_tileMap.CTFBaseSpawn[1].Y + 150, 0);
                        CarryableTeam1 = teleport1;
                    }
                }
            }

        }
        public void RemoveSpectator(long id)
        {
            m_spectators.Remove(id);
        }
        public int GetTeamPlayersCount(int teamIndex)
        {
            int result = 0;
            foreach (BattlePlayer player in GetPlayers())
            {
                if (player != null && player.TeamIndex == teamIndex) result++;
            }
            return result;
        }

        public void AddClientInput(ClientInput input, long sessionId)
        {
            if (input == null) return;
            
            if (!m_playersBySessionId.ContainsKey(sessionId))
                return;

            try
            {
                input.OwnerSessionId = sessionId;
                m_inputQueue.Enqueue(input);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is IndexOutOfRangeException)
            {
                var newQueue = new Queue<ClientInput>();
                var oldQueue = Interlocked.Exchange(ref m_inputQueue, newQueue);
                if (oldQueue != null)
                {
                    foreach (var item in oldQueue)
                    {
                        if (item != null) newQueue.Enqueue(item);
                    }
                }
                newQueue.Enqueue(input);
            }
        }

        public void HandleSpectatorInput(ClientInput input, long sessionId)
        {
            if (input == null) return;

            if (!m_spectators.ContainsKey(sessionId)) return;
            if (m_spectators[sessionId] != null)
                m_spectators[sessionId].HandledInputs = input.Index;
        }

        private void HandleClientInput(ClientInput input)
        {
            if (input == null) return;
            BattlePlayer player = GetPlayerBySessionId(input.OwnerSessionId);
            if (player == null) return;
            if (player.LastHandledInput >= input.Index) return;
            player.LastHandledInput = input.Index;
            Character character1 = GetCharacterSafe(player.OwnObjectId);
            if ((character1 == null || !character1.IsAlive()) && input.Type != 4 && input.Type != 9) return;
            if (character1 != null && character1.ViusalChargeType > 0 && input.Type >= 2 && input.Type <= 8) return;
            if (player.Bot == 1) player.Bot = 0;
            if (player.Bot == 1 && character1 != null) character1.SetBot(0);
            switch (input.Type)
            {
                case 0:
                    {
                        if (_SetStopTicks) return;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        if (character == null) return;
                        Skill skill = character.GetWeaponSkill();
                        if (skill == null) return;

                        character.UltiDisabled();
                        if (input.AutoAttack)
                        {
                            Character target = GetCharacterSafe(input.AutoAttackTarget);
                            if (target != null)
                            {
                                input.X = target.GetX();
                                input.Y = target.GetY();
                            }
                            else
                            {
                                target = character.GetClosestVisibleEnemy();
                                if (target != null)
                                {
                                    input.X = target.GetX();
                                    input.Y = target.GetY();
                                }
                            }
                            character.ActivateSkill(0, input.X, input.Y);
                            character.AttackAngle = LogicMath.GetAngle(input.X - character.GetX(), input.Y - character.GetY());
                            break;
                        }
                        if (!skill.SkillData.IsPositionalTargeted())
                        {
                            character.ActivateSkill(0, input.X + character.GetX(), input.Y + character.GetY());
                        }
                        else character.ActivateSkill(0, input.X, input.Y);


                        var counter = character1.GetFadeCounter();
                        character1.IncrementFadeCounter();
                        character.SetFadeCounter(counter);

                        character1.ResetAFKTicks();
                        
                        if (Carryable != null && m_gameModeVariation == 5 && character != null && character.CharacterData != null && !character.CharacterData.IsHero())
                        {
                            OnCarryableDamaged(character, 0);
                        }

                        break;
                    }
                case 1:
                    {
                        if (_SetStopTicks || (character1 != null && character1.IsControlled)) return;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        if (character == null) return;
                        if (character.m_skills != null && character.m_skills.Count > 1 && character.m_skills[1] != null)
                            character.m_skills[1].SeemToBeActive = false;

                        if (character.DisplayUltiTmp) return;
                        Skill skill = character.GetUltimateSkill();
                        if (skill == null) return;
                        character?.UltiDisabled();
                        if (!player.HasUlti()) return;

                        character.UltiEnabled();
                        skill.IsFromAutoAttack = input.AutoAttack;
                        if (input.AutoAttack && !skill.SkillData.MovementBasedAutoshoot)
                        {
                            Character target = GetCharacterSafe(input.AutoAttackTarget);
                            if (target != null)
                            {
                                input.X = target.GetX();
                                input.Y = target.GetY();
                            }
                            else 
                            {
                                target = character.GetClosestVisibleEnemy();
                                if (target != null)
                                {
                                    input.X = target.GetX();
                                    input.Y = target.GetY();
                                }
                            }
                            character.ActivateSkill(1, input.X, input.Y);
                            break;
                        }
                        if (input.AutoAttack && skill.SkillData.MovementBasedAutoshoot)
                        {
                            character.ActivateSkill(1, input.X, input.Y);
                            break;
                        }
                        if (!skill.SkillData.IsPositionalTargeted())
                        {
                            character.ActivateSkill(1, input.X + character.GetX(), input.Y + character.GetY());
                        }
                        else character.ActivateSkill(1, input.X, input.Y);
                        var counter = character1.GetFadeCounter();
                        character1.IncrementFadeCounter();
                        character.SetFadeCounter(counter);

                        character1.ResetAFKTicks();
                        
                        if (Carryable != null && m_gameModeVariation == 5)
                        {
                            OnCarryableDamaged(character, 0);
                        }
                        
                        break;
                    }
                case 2:
                    {
                        if (_SetStopTicks) return;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        if (character == null) return;
                        character.MoveTo(0, input.X, input.Y, 0, 0, 0, 0);
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 4:
                    {
                        if (_SetStopTicks) return;
                        SendBattleEndToPlayer(player);
                        RemovePlayer(player);
                        if (m_gameModeVariation == 13)
                        {
                            this.IsGameOver = true;
                            EndTicks++;
                            if (m_updateTimer != null)
                                this.m_updateTimer.Dispose();
                        }
                        return;
                    }
                case 5:
                    {
                        if (_SetStopTicks || (character1 != null && character1.IsControlled)) return;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        character?.UltiEnabled();
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 6:
                    {
                        if (_SetStopTicks || (character1 != null && character1.IsControlled)) return;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        character?.UltiDisabled();
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 8:
                    {
                        if (_SetStopTicks) return;
                        if (KnockoutTicks > 0) return;
                        if (StoryMode != null && StoryMode.StartWaitingTick > GetTicksGone())
                        {
                            StoryMode.DoorTest ^= true;
                            StoryMode.WaitSkipped = true;
                            return;
                        }
                        if (character1 != null && character1.IsControlled) return;
                        Accessory accessory = player.Accessory;
                        Character character = GetCharacterSafe(player.OwnObjectId);
                        if (accessory != null) accessory.TriggerAccessory(character, input.X, input.Y);
                        player?.CallVibrate();
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 9:
                    {
                        if (character1 == null || character1.IsControlled) return;
                        if (player.CanUsePin(GetTicksGone())) player.UsePin(input.EmoteIndex, GetTicksGone());
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 10:
                    {
                        if (_SetStopTicks) return;
                        if (character1 != null && character1.IsSteerMovementActive())
                        {
                            if ((input.X | input.Y) < 0) character1.SteerAngle = -1;
                            else
                                character1.SteerAngle = LogicMath.GetAngle(-input.X + character1.GetX(), -input.Y + character1.GetY());
                            return;
                        }
                        Projectile projectile = character1 != null ? character1.GetControlledProjectile() : null;
                        if (projectile == null) return;
                        int v26;
                        if ((input.X | input.Y) < 0) v26 = -1;
                        else
                        {
                            v26 = LogicMath.GetAngle(input.X - projectile.GetX(), input.Y - projectile.GetY());
                        }
                        projectile.SteerAngle = v26;
                        if (character1 != null) character1.ResetAFKTicks();
                        break;
                    }
                case 13:
                    if (_SetStopTicks) return;
                    input.X = input.X * 150 / 100;
                    if (character1 != null && character1.GetSkillHoldedTicks() <= 0)
                    {
                        character1.HoldSkillStarted();
                        if (character1.m_skills != null && character1.m_skills.Count > 0 && character1.m_skills[0] != null && character1.m_skills[0].SkillData != null)
                        {
                            if (character1.m_skills[0].SkillData.AttackPattern == 13 || character1.m_skills[0].SkillData.AttackPattern == 15 || character1.m_skills[0].SkillData.AttackPattern == 17) break;
                        }
                    }
                    if (character1 != null && character1.m_skills != null && character1.m_skills.Count > 0 && character1.m_skills[0] != null && character1.m_skills[0].SkillData != null)
                    {
                        if (character1.m_skills[0].SkillData.AttackPattern == 13 || character1.m_skills[0].SkillData.AttackPattern == 15 || character1.m_skills[0].SkillData.AttackPattern == 17) break;
                    }
                    break;
                case 14:
                    if (_SetStopTicks) return;
                    if (character1 != null)
                    {
                        character1.LastHoldTick = character1.SkillHoldTicks;
                        character1.SkillHoldTicks = -1;
                        if (character1.m_skills != null && character1.m_skills.Count > 1 && character1.m_skills[1] != null)
                            character1.m_skills[1].SeemToBeActive = false;
                        character1.ResetAFKTicks();
                    }
                    break;
                case 15:
                    Item Spray = new Item(DataTables.GetItemByName("Spray"));
                    if (input.SprayIndex < 0) return;
                    if (player.LastSprayUseTicks + 20 > GetTicksGone()) return;
                    player.LastSprayUseTicks = GetTicksGone();
                    player.SprayIndex = input.SprayIndex;
                    Spray.SprayData = DataTables.Get(DataType.Spray).GetDataWithId<SprayData>(player.GetSprayIDBySprayIndex(input.SprayIndex));
                    Spray.Owner = character1;
                    if (player.CharacterSpray != null) m_gameObjectManager.RemoveGameObject(player.CharacterSpray);
                    player.CharacterSpray = Spray;

                    Spray.SetPosition(
                        character1.GetX() + LogicMath.GetRotatedX(500, 0, input.SprayAngle),
                        character1.GetY() + LogicMath.GetRotatedY(500, 0, input.SprayAngle),
                        0
                    );
                    Spray.SetIndex(character1.GetIndex());
                    m_gameObjectManager.AddGameObject(Spray);
                    if (character1 != null) character1.ResetAFKTicks();
                    break;
                case 17:
                    if (_SetStopTicks || nokbool) return;
                    if (!player.HasOverCharge()) return;
                    player.CallVibrate();
                    player.OverCharging = true;
                    if (character1 != null) character1.ResetAFKTicks();
                    break;
                default:
                    Debugger.Warning("Input is unhandled: " + input.Type);
                    break;
            }
        }
        
        public void OnCarryableDamaged(Character attacker, int damage)
        {
            if (Carryable == null) return;
            
            Task.Delay(1).ContinueWith(_ => 
            {
                try
                {
                    if (Carryable != null && !Carryable.IsAlive())
                    {
                        ForceRespawnCarryable();
                    }
                    else if (Carryable != null && _carryableBallState.CheckIfStuck(GetTicksGone(), Carryable.GetX(), Carryable.GetY()))
                    {
                        ForceRespawnCarryable();
                    }
                }
                catch { }
            });
        }

        public bool IsTileOnPoisonArea(int xTile, int yTile)
        {
            int tick = GetTicksGone();
            if (m_gameModeVariation == 20)
            {
                return false;
                if (tick > 500)
                {
                    int poisons = 0;
                    poisons += (tick - 500) / 100;

                    if (xTile <= poisons || xTile >= 22 - poisons || yTile <= poisons || yTile >= 33 - poisons)
                    {
                        return true;
                    }
                }
            }
            if (m_gameModeVariation != 6 && m_gameModeVariation != 9) return false;
            if (tick > 500)
            {
                int poisons = 0;
                poisons += (tick - 500) / 100;

                if (xTile <= poisons || xTile >= 59 - poisons || yTile <= poisons || yTile >= 59 - poisons)
                {
                    return true;
                }
            }
            return false;
        }

        private void HandleIncomingInputMessages()
        {
            try
            {
                while (m_inputQueue.Count > 0)
                {
                    this.HandleClientInput(m_inputQueue.Dequeue());
                }
            }
            catch { }
        }
        private bool _tmpb;
        private int _tmpi;
        public void ExecuteOneTick()
        {
            try
            {
                this.HandleIncomingInputMessages();
                
                CheckAndFixStuckCarryable();
                
                if (CalculateIsGameOver())
                {
                    if (IsRanked)
                    {
                        if (!_tmpb)
                        {
                            RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (r != null)
                            {
                                if (m_winnerTeam == 0) r.TrueTeamWins++;
                                if (m_winnerTeam == 1) r.TrueRedWins++;
                                r.GameRounds++;
                            }
                            _tmpi = GetTicksGone();
                            _tmpb = true;
                            GameOver();

                        }
                        if (_tmpi + (10 * 20) == GetTicksGone())
                        {
                            RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (r != null && r.RestartRound() == -1)
                            {
                                this.IsGameOver = true;
                                EndTicks++;
                                if (m_updateTimer != null)
                                    this.m_updateTimer.Dispose();
                                return;
                            }
                        }
                        if (_tmpi + (11 * 20) == GetTicksGone())
                        {
                            this.IsGameOver = true;
                            if (m_updateTimer != null)
                                this.m_updateTimer.Dispose();
                        }
                        m_time.IncreaseTick();
                        return;
                    }
                    GameOver();
                    this.IsGameOver = true;
                    EndTicks++;
                    if (m_updateTimer != null)
                        this.m_updateTimer.Dispose();
                    return;
                }
                this.SendVisionUpdateToPlayers();
                foreach (BattlePlayer player in GetPlayers())
                {
                    if (player != null)
                    {
                        player.KillList.Clear();
                    }
                }
                this.m_gameObjectManager.PreTick();

                this.Tick();
                this.m_time.IncreaseTick();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ExecuteOneTick] Error: {ex.Message}");
            }
        }

        public void GameOver()
        {
            SendBattleEndToPlayers();
            foreach (LogicGameListener gl in m_spectators.Values)
            {
                if (gl != null)
                {
                    gl.SendMessage(new AuthenticationFailedMessage { ErrorCode = 1, Message = "Battle ended. End screen will be added later." });
                }
            }
        }

        private int ApplyServerWinEventRewards(BattlePlayer player, bool isVictory)
        {
            if (player == null || player.ServerEventRewardsGranted)
                return 0;

            // Only real, rewarded matches are eligible. This prevents farming the events in
            // ranked, club-pig and challenge battles where normal trophy rewards are disabled.
            if (!isVictory || !BattleWithTrophies || IsRanked || IsPiggy || IsChampionship ||
                player.SessionId < 0 || player.Avatar == null)
                return 0;

            player.ServerEventRewardsGranted = true;

            if (GeneralStaticLogic.CoinsForWinEvent)
                player.Avatar.AddGold(300);

            int trophyBonus = 0;
            if (GeneralStaticLogic.BonusTrophiesForWinEvent)
            {
                Hero eventHero = player.Avatar.GetHero(player.CharacterId);
                if (eventHero != null)
                {
                    trophyBonus = 36;
                    eventHero.AddTrophies(trophyBonus);
                }
            }

            if (GeneralStaticLogic.StarrDropForWinEvent && player.Home != null)
                player.Home.DropsCount++;

            if (GeneralStaticLogic.CoinsForWinEvent || trophyBonus > 0 || GeneralStaticLogic.StarrDropForWinEvent)
            {
                Console.WriteLine($"[ServerEvents] Win reward for {player.Avatar.Name} ({player.AccountId}): " +
                                  $"coins={(GeneralStaticLogic.CoinsForWinEvent ? 300 : 0)}, " +
                                  $"trophies={trophyBonus}, starrDrop={GeneralStaticLogic.StarrDropForWinEvent}");
            }

            return trophyBonus;
        }

        public void SendBattleEndToPlayers()
        {
            foreach (BattlePlayer player in m_players)
            {
                if (player == null) continue;
                if (player.SessionId < 0) continue;
                if (player.BattleRoyaleRank == -1) player.BattleRoyaleRank = 1;
                if (player.Avatar == null) continue;
                if (player.CanSendBattleEnd) continue;
                int rank = player.BattleRoyaleRank;
                if (m_gameModeVariation == 9) rank = _playersAliveForDuo + 1;
                player.Avatar.BattleId = -1;
                bool isVictory = m_winnerTeam == player.TeamIndex;
                BattleEndMessage message = new BattleEndMessage();
                Hero hero = player.Avatar.GetHero(player.CharacterId);
                message.BattleWithoutTrophies = !BattleWithTrophies;
                int oldTrophies = hero != null ? hero.HighestTrophies : 0;
                if (hero != null && hero.Trophies >= hero.HighestTrophies)
                    hero.HighestTrophies = hero.Trophies;

                if (BattleWithTrophies)
                {
                    try
                    {
                        HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                        if (homeMode != null)
                        {
                            if (homeMode.Home.Quests != null)
                            {
                                message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                            }
                        }
                    }
                    catch {; }


                    if (m_gameModeVariation != 6 && m_gameModeVariation != 28 && m_gameModeVariation != 9 && !IsSecret)
                    {
                        message.GameMode = 1;
                        message.IsPvP = BattleWithTrophies;
                        message.Players = m_players;
                        message.OwnPlayer = player;

                        if (m_winnerTeam == -1 && BattleWithTrophies)
                        {
                            message.Result = 2;
                            message.TrophiesReward = 0;
                        }
                        int brawlerTrophies = 0;
                        if (hero != null)
                        {
                            brawlerTrophies = hero.Trophies;
                        }

                        int winTrophies = 0;
                        int loseTrophies = 0;
                        if (brawlerTrophies < 800) { winTrophies = 8; loseTrophies = -5; }
                        else if (brawlerTrophies < 900) { winTrophies = 7; loseTrophies = -9; }
                        else if (brawlerTrophies < 1000) { winTrophies = 6; loseTrophies = -10; }
                        else if (brawlerTrophies < 1100) { winTrophies = 5; loseTrophies = -11; }
                        else if (brawlerTrophies < 1200) { winTrophies = 4; loseTrophies = -12; }
                        else { winTrophies = 3; loseTrophies = -12; }


                        if (isVictory && BattleWithTrophies)
                        {
                            player.Avatar.Wins += 1;
                            switch (player.Avatar.Wins)
                            {
                                case 1:
                                case 4:
                                case 8:
                                    if (player.Home != null)
                                    {
                                        player.Home.DropsCount++;
                                        if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                    }
                                    break;
                                default:
                                    if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                    break;
                            }
                            if (!IsChampionship)
                            {
                                if (!IsRanked && !IsPiggy) player.Avatar.WinStreak += 1;
                                if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                                if (!IsRanked && !IsPiggy) message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                                if (!IsRanked && !IsPiggy) if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                                
                                int vipBonusMastery = player.Avatar.VipBonusMastery;
                                int masteryGained = MasteryReward(brawlerTrophies);
                                if (vipBonusMastery > 0)
                                {
                                    masteryGained += vipBonusMastery;
                                    player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                                }
                                message.MasteryGained = masteryGained;
                                if (player.Avatar.GetHero(player.CharacterId) != null)
                                    player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                                message.Result = 0;

                                int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                                int trophiesReward = winTrophies + message.WinstreakTrophies;
                                if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                                {
                                    trophiesReward += vipBonusTrophies;
                                    player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков (всего: {player.Avatar.VipTotalBonusReceived})");
                                }
                                message.TrophiesReward = trophiesReward;

                                if (!IsRanked && !IsPiggy && hero != null) hero.AddTrophies(trophiesReward);
                                int tokensReward = 10;
                                if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                                {
                                    if (tokensReward * 2 <= player.Home.TokenDoublers)
                                    {
                                        player.Home.TokenDoublers -= tokensReward;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                        message.TokensReward *= 2;
                                    }
                                    else
                                    {
                                        message.TokensReward += player.Home.TokenDoublers;
                                        player.Home.TokenDoublers = 0;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = 0;
                                    }
                                }
                                message.TokensReward = tokensReward;
                                player.Avatar.AddTokens(tokensReward);
                                player.Avatar.TrioWins++;
                                if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                                {
                                    if (player.Avatar.EventTokenCap < 200)
                                    {
                                        player.Avatar.AddStarPoints(10);
                                        player.Avatar.EventTokenCap += 10;
                                    }
                                }

                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                                if (!IsRanked && !IsPiggy && player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                                if (IsPiggy && player.Home != null)
                                {
                                    player.Home.PiggyBankTickets--;
                                }
                                if (IsRanked)
                                {
                                    message.RankedMatch = true;
                                    RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                                    if (r == null) return;
                                    switch (r.Solo)
                                    {
                                        case true:
                                            int v1s = player.Home != null ? player.Home.RankedSoloMaxRank : 0;
                                            int tempRewards = new Random().Next(40, 150);
                                            int a1s = player.Home != null ? player.Home.RankedSoloMaxProgress : 0;
                                            if (player.Home != null) player.Home.RankedSoloProgress += tempRewards;
                                            if (player.Home != null && player.Home.RankedSoloProgress > player.Home.RankedSoloMaxProgress)
                                                if (player.Home != null) player.Home.RankedSoloMaxProgress = player.Home.RankedSoloProgress;
                                            int a2s = player.Home != null ? player.Home.RankedSoloMaxProgress : 0;
                                            int ps = (player.Home != null ? player.Home.RankedSoloRank : 0) * 1000;
                                            if (a1s < ps && a2s >= ps)
                                                if (player.Home != null) player.Home.RankedSoloRank += 1;
                                            if (player.Home != null && player.Home.RankedSoloRank > player.Home.RankedSoloMaxRank)
                                                if (player.Home != null) player.Home.RankedSoloMaxRank = player.Home.RankedSoloRank;
                                            if (v1s != (player.Home != null ? player.Home.RankedSoloMaxRank : 0)) message.UpRank = player.Home != null ? player.Home.RankedSoloMaxRank : 0;
                                            message.Add = tempRewards;
                                            message.ELO = player.Home != null ? player.Home.RankedSoloProgress : 0;
                                            message.Rank = (player.Home != null ? player.Home.RankedSoloRank : 0) + 1;
                                            break;
                                        case false:
                                            int v1t = player.Home != null ? player.Home.RankedTrioMaxRank : 0;
                                            int tempReward = 100;
                                            int a1t = player.Home != null ? player.Home.RankedTrioMaxProgress : 0;
                                            if (player.Home != null) player.Home.RankedTrioProgress += tempReward;
                                            if (player.Home != null && player.Home.RankedTrioProgress > player.Home.RankedTrioMaxProgress)
                                                if (player.Home != null) player.Home.RankedTrioMaxProgress = player.Home.RankedTrioProgress;
                                            int a2t = player.Home != null ? player.Home.RankedTrioMaxProgress : 0;
                                            int pt = (player.Home != null ? player.Home.RankedTrioRank : 0) * 1000;
                                            if (a1t < pt && a2t >= pt)
                                                if (player.Home != null) player.Home.RankedTrioRank += 1;
                                            if (player.Home != null && player.Home.RankedTrioRank > player.Home.RankedTrioMaxRank)
                                                if (player.Home != null) player.Home.RankedTrioMaxRank = player.Home.RankedTrioRank;
                                            if (v1t != (player.Home != null ? player.Home.RankedTrioMaxRank : 0)) message.UpRank = player.Home != null ? player.Home.RankedTrioMaxRank : 0;
                                            message.ELO = player.Home != null ? player.Home.RankedTrioProgress : 0;
                                            message.Rank = (player.Home != null ? player.Home.RankedTrioRank : 0) + 1;
                                            break;
                                    }
                                    if (r != null)
                                    {
                                        message.TrueBlueTeamWins = r.TrueTeamWins;
                                        message.TrueRedTeamWins = r.TrueRedWins;
                                        message.Round = !r.MatchOver ? r.Round : 0;
                                    }
                                    message.Solo = r != null && r.Solo;
                                }
                            }
                            else
                            {
                                message.Result = 0;
                                int tokensReward = 10;
                                if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                                {
                                    if (tokensReward * 2 <= player.Home.TokenDoublers)
                                    {
                                        player.Home.TokenDoublers -= tokensReward;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                        message.TokensReward *= 2;
                                    }
                                    else
                                    {
                                        message.TokensReward += player.Home.TokenDoublers;
                                        player.Home.TokenDoublers = 0;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = 0;
                                    }
                                }
                                message.TokensReward = tokensReward;
                                player.Avatar.AddTokens(tokensReward);
                                player.Avatar.TrioWins++;

                                if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                                {
                                    if (player.Avatar.EventTokenCap < 200)
                                    {
                                        player.Avatar.AddStarPoints(10);
                                        player.Avatar.EventTokenCap += 10;
                                    }
                                }
                                if (player.Home != null)
                                {
                                    player.Home.ChallengeWins++;
                                    message.reward = new(ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferType,
                                        ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferCount,
                                        ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData1,
                                        ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData2,
                                        ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferExtra);
                                    player.Home.NotificationFactory.Add(new()
                                    {
                                        Id = 63,
                                        gemOffer = new(ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferType,
            ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferCount,
            ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData1,
            ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData2,
            ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferExtra),
                                        Victory = player.Home.ChallengeWins,
                                        ChallengeName = ChampieData.Title,
                                        ChallengeType = ChampieData.ChallengeType
                                    });
                                }
                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                            }
                            if (BattleWithTrophies)
                            {
                                try
                                {
                                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                                    if (homeMode != null)
                                    {
                                        if (homeMode.Home.Quests != null)
                                        {
                                            message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                                        }
                                    }
                                }
                                catch {; }
                            }
                        }
                        else if (m_winnerTeam != -1 && BattleWithTrophies)
                        {
                            if (!IsChampionship)
                            {
                                message.Result = 1;
                                message.TokensReward = 10;

                                int trophiesReward = loseTrophies + message.WinstreakTrophies;
                                if (!IsRanked && !IsPiggy && hero != null && hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                                if (!IsRanked && !IsPiggy) message.TrophiesReward = trophiesReward;

                                if (!IsRanked && !IsPiggy && hero != null) hero.AddTrophies(trophiesReward);
                                int tokensReward = 10;
                                if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                                {
                                    if (tokensReward * 2 <= player.Home.TokenDoublers)
                                    {
                                        player.Home.TokenDoublers -= tokensReward;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                        message.TokensReward *= 2;
                                    }
                                    else
                                    {
                                        message.TokensReward += player.Home.TokenDoublers;
                                        player.Home.TokenDoublers = 0;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = 0;
                                    }
                                }
                                message.TokensReward = tokensReward;
                                player.Avatar.AddTokens(tokensReward);
                                if (!IsRanked && !IsPiggy) player.Avatar.WinStreak = 0;

                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                                if (!IsRanked && !IsPiggy && player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                                if (IsPiggy && player.Home != null) player.Home.PiggyBankTickets--;
                                if (IsRanked)
                                {
                                    message.RankedMatch = true;
                                    RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                                    if (r == null) return;
                                    switch (r.Solo)
                                    {
                                        case true:
                                            int tempRewards = new Random().Next(40, 69);
                                            if (player.Home != null) player.Home.RankedSoloProgress -= tempRewards;
                                            if (player.Home != null && player.Home.RankedSoloProgress < 0) player.Home.RankedSoloProgress = 0;
                                            message.ELO = player.Home != null ? player.Home.RankedSoloProgress : 0;
                                            message.Rank = (player.Home != null ? player.Home.RankedSoloRank : 0) + 1;
                                            break;
                                        case false:
                                            int tempReward = 65;
                                            if (player.Home != null) player.Home.RankedTrioProgress -= tempReward;
                                            if (player.Home != null && player.Home.RankedTrioProgress < 0) player.Home.RankedTrioProgress = 0;
                                            message.ELO = player.Home != null ? player.Home.RankedTrioProgress : 0;
                                            message.Rank = (player.Home != null ? player.Home.RankedTrioRank : 0) + 1;
                                            break;
                                    }
                                    if (r != null)
                                    {
                                        message.TrueBlueTeamWins = r.TrueTeamWins;
                                        message.TrueRedTeamWins = r.TrueRedWins;
                                        message.Round = !r.MatchOver ? r.Round : 0;
                                    }
                                    message.Solo = r != null && r.Solo;
                                }
                            }
                            else
                            {
                                message.Result = 1;
                                message.TokensReward = 10;

                                int tokensReward = 10;
                                if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                                {
                                    if (tokensReward * 2 <= player.Home.TokenDoublers)
                                    {
                                        player.Home.TokenDoublers -= tokensReward;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                        message.TokensReward *= 2;
                                    }
                                    else
                                    {
                                        message.TokensReward += player.Home.TokenDoublers;
                                        player.Home.TokenDoublers = 0;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = 0;
                                    }
                                }
                                message.TokensReward = tokensReward;
                                player.Avatar.AddTokens(tokensReward);
                                if (player.Home != null) player.Home.ChallengeLoses++;
                                message.tryes = player.Home != null ? player.Home.ChallengeLoses : 0;
                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                            }
                        }
                        else
                            message.Result = 2;
                    }
                    else if (m_gameModeVariation == 6 || m_gameModeVariation == 9)
                    {
                        int brawlerTrophies = 0;
                        if (hero != null)
                        {
                            brawlerTrophies = hero.Trophies;
                        }

                    int[] Trophies = new int[10];
                    if (brawlerTrophies <= 49)
                        Trophies = new[] { 10, 8, 7, 6, 4, 2, 2, 1, 0, 0 };
                    else if (brawlerTrophies <= 99)
                        Trophies = new[] { 10, 8, 7, 6, 3, 2, 2, 0, -1, -2 };
                    else if (brawlerTrophies <= 199)
                        Trophies = new[] { 10, 8, 7, 6, 3, 1, 0, -1, -2, -2 };
                    else if (brawlerTrophies <= 299)
                        Trophies = new[] { 10, 8, 6, 5, 3, 1, 0, -2, -3, -3 };
                    else if (brawlerTrophies <= 399)
                        Trophies = new[] { 10, 8, 6, 5, 2, 0, 0, -3, -4, -4 };
                    else if (brawlerTrophies <= 499)
                        Trophies = new[] { 10, 8, 6, 5, 2, -1, -2, -3, -5, -5 };
                    else if (brawlerTrophies <= 599)
                        Trophies = new[] { 10, 8, 6, 4, 2, -1, -2, -5, -6, -6 };
                    else if (brawlerTrophies <= 699)
                        Trophies = new[] { 10, 8, 6, 4, 1, -2, -2, -5, -7, -8 };
                    else if (brawlerTrophies <= 799)
                        Trophies = new[] { 10, 8, 6, 4, 1, -3, -4, -5, -8, -9 };
                    else if (brawlerTrophies <= 899)
                        Trophies = new[] { 9, 7, 5, 2, 0, -3, -4, -7, -9, -10 };
                    else if (brawlerTrophies <= 999)
                        Trophies = new[] { 8, 6, 4, 1, -1, -3, -6, -8, -10, -11 };
                    else if (brawlerTrophies <= 1099)
                        Trophies = new[] { 6, 5, 3, 1, -2, -5, -6, -9, -11, -12 };
                    else if (brawlerTrophies <= 1199)
                        Trophies = new[] { 5, 4, 1, 0, -2, -6, -7, -10, -12, -13 };
                    else // 1200+
                        Trophies = new[] { 5, 3, 0, -1, -2, -6, -8, -11, -12, -13 };
                        if (GetGameModeVariation() == 9)
                        {
                            rank = rank -1;
                            if (brawlerTrophies >= 0 && brawlerTrophies <= 49)
                            {
                                 Trophies = new[] { 46, 8, 5, 3, 0 };
                            }
                            else if (brawlerTrophies >= 50 && brawlerTrophies <= 99)
                            {
                                 Trophies = new[] { 46, 7, 4, 0, 0 };
                            }
                            else if (brawlerTrophies >= 100 && brawlerTrophies <= 199)
                            {
                                 Trophies = new[] { 46, 7, 4, 0, -1 };
                            }
                            else if (brawlerTrophies >= 200 && brawlerTrophies <= 299)
                            {
                                 Trophies = new[] { 46, 7, 4, 0, -2 };
                            }
                            else if (brawlerTrophies >= 300 && brawlerTrophies <= 399)
                            {
                                 Trophies = new[] { 46, 7, 4, 0, -3 };
                            }
                            else if (brawlerTrophies >= 400 && brawlerTrophies <= 499)
                            {
                                 Trophies = new[] { 46, 6, 3, -1, -4 };
                            }
                            else if (brawlerTrophies >= 500 && brawlerTrophies <= 599)
                            {
                                 Trophies = new[] { 46, 6, 3, -2, -5 };
                            }
                            else if (brawlerTrophies >= 600 && brawlerTrophies <= 699)
                            {
                                 Trophies = new[] { 46, 6, 2, -2, -6 };
                            }
                            else if (brawlerTrophies >= 700 && brawlerTrophies <= 799)
                            {
                                 Trophies = new[] { 46, 5, 1, -2, -7 };
                            }
                            else if (brawlerTrophies >= 800 && brawlerTrophies <= 899)
                            {
                                 Trophies = new[] { 46, 5, 0, -3, -8 };
                            }
                            else if (brawlerTrophies >= 900 && brawlerTrophies <= 999)
                            {
                                 Trophies = new[] { 46, 4, 0, -3, -9 };
                            }
                            else if (brawlerTrophies >= 1000 && brawlerTrophies <= 1099)
                            {
                                 Trophies = new[] { 46, 3, -1, -4, -10 };
                            }
                            else if (brawlerTrophies >= 1100 && brawlerTrophies <= 1199)
                            {
                                 Trophies = new[] { 46, 2, -2, -5, -11 };
                            }
                            else if (brawlerTrophies >= 1200)
                            {
                                 Trophies = new[] { 46, 2, -3, -6, -12 };
                            }
                        }
                        
                        int vipBonusMastery = player.Avatar.VipBonusMastery;
                        int masteryGained = MasteryReward(brawlerTrophies);
                        if (vipBonusMastery > 0)
                        {
                            masteryGained += vipBonusMastery;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGained;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;
                        
                        message.IsPvP = BattleWithTrophies;
                        message.GameMode = 2;
                        message.Result = player.BattleRoyaleRank;
                        message.Players = new List<BattlePlayer>();
                        message.Players.Add(player);
                        message.OwnPlayer = player;
                        if (hero != null && hero.Trophies >= hero.HighestTrophies)
                        {
                            hero.HighestTrophies = hero.Trophies;
                        }
                        if (!BattleWithTrophies)
                        {
                            message.BattleWithoutTrophies = true;
                        }
                        
                        int tokensReward = 40 / rank;
                        if (BattleWithTrophies) message.TokensReward = tokensReward;

                        Console.WriteLine("rank = " + rank);

                        if (GetGameModeVariation() == 9)
                        {
                            if (rank == 2 && BattleWithTrophies)
                            {
                                player.Avatar.Wins += 1;
                                player.Avatar.WinStreak += 1;
                                if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                                message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                                if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                                switch (player.Avatar.Wins)
                                {
                                    case 1:
                                    case 4:
                                    case 8:
                                        if (player.Home != null)
                                        {
                                            player.Home.DropsCount++;
                                            if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                        }
                                        break;
                                    default:
                                        if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                        break;
                                }
                                int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                                int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                                if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                                {
                                    trophiesReward += vipBonusTrophies;
                                    player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в дуо (всего: {player.Avatar.VipTotalBonusReceived})");
                                }
                                message.TrophiesReward = trophiesReward;
                                if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                                {
                                    if (tokensReward * 2 <= player.Home.TokenDoublers)
                                    {
                                        player.Home.TokenDoublers -= tokensReward;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                        message.TokensReward *= 2;
                                    }
                                    else
                                    {
                                        message.TokensReward += player.Home.TokenDoublers;
                                        player.Home.TokenDoublers = 0;
                                        message.TokenDoublers = tokensReward;
                                        message.TokenDoublersRemaining = 0;
                                    }
                                }
                                message.TokensReward = tokensReward;
                                player.Avatar.AddTokens(tokensReward);
                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                                player.Avatar.SoloWins += 1;
                                if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                                {
                                    if (player.Avatar.EventTokenCap < 200)
                                    {
                                        player.Avatar.AddStarPoints(10);
                                        player.Avatar.EventTokenCap += 10;
                                    }
                                }
                                if (hero != null) hero.AddTrophies(message.TrophiesReward);
                                if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                            }
                            else if (rank == 3 && BattleWithTrophies)
                            {
                                int vipBonusMasteryForLoss = player.Avatar.VipBonusMastery;
                                int masteryGainedLoss = 25;
                                if (vipBonusMasteryForLoss > 0)
                                {
                                    masteryGainedLoss += vipBonusMasteryForLoss;
                                    player.Avatar.VipMasteryTotalBonusReceived += vipBonusMasteryForLoss;
                                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMasteryForLoss} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                                }
                                message.MasteryGained = masteryGainedLoss;
                                if (player.Avatar.GetHero(player.CharacterId) != null)
                                    player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGainedLoss;
                                
                                int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                                message.TrophiesReward = trophiesReward;

                                player.Avatar.AddTokens(tokensReward);
                                if (player.Home != null) player.Home.TokenReward += tokensReward;
                                if (hero != null) hero.AddTrophies(message.TrophiesReward);
                                if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                            }
                            else
                            {
                                if (BattleWithTrophies)
                                {
                                    player.Avatar.WinStreak = 0;
                                    int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                                    message.TrophiesReward = trophiesReward;

                                    player.Avatar.AddTokens(tokensReward);
                                    if (player.Home != null) player.Home.TokenReward += tokensReward;
                                    if (player.Home != null) player.Home.TrophiesReward += trophiesReward;

                                    if (hero != null) hero.AddTrophies(message.TrophiesReward);
                                    if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                                }
                            }
                        }
                        if (rank == 1 && BattleWithTrophies)
                        {
                            if (IsSpecialTrophyMode)
                            {
                                message.Plus = player.Kills * 2;
                                if (hero != null) hero.AddTrophies(message.Plus);
                            }
                            player.Avatar.Wins += 1;
                            player.Avatar.WinStreak += 1;
                            if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                            message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                            if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                            switch (player.Avatar.Wins)
                            {
                                case 1:
                                case 4:
                                case 8:
                                    if (player.Home != null)
                                    {
                                        player.Home.DropsCount++;
                                        if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                    }
                                    break;
                                default:
                                    if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                    break;
                            }
                            int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                            int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                            if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                            {
                                trophiesReward += vipBonusTrophies;
                                player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                                Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в соло (всего: {player.Avatar.VipTotalBonusReceived})");
                            }
                            message.TrophiesReward = trophiesReward;
                            if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                            {
                                if (tokensReward * 2 <= player.Home.TokenDoublers)
                                {
                                    player.Home.TokenDoublers -= tokensReward;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                    message.TokensReward *= 2;
                                }
                                else
                                {
                                    message.TokensReward += player.Home.TokenDoublers;
                                    player.Home.TokenDoublers = 0;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = 0;
                                }
                            }
                            message.TokensReward = tokensReward;
                            player.Avatar.AddTokens(tokensReward);
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            player.Avatar.SoloWins += 1;
                            if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                            {
                                if (player.Avatar.EventTokenCap < 200)
                                {
                                    player.Avatar.AddStarPoints(10);
                                    player.Avatar.EventTokenCap += 10;
                                }
                            }
                            if (hero != null) hero.AddTrophies(message.TrophiesReward);
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                        }
                    }
                    else if (m_gameModeVariation == 28)
                    {
                        int brawlerTrophies = 0;
                        if (hero != null)
                        {
                            brawlerTrophies = hero.Trophies;
                        }

                        int[] Trophies = new int[10];
                        if (brawlerTrophies >= 0 && brawlerTrophies <= 49)
                        {
                            Trophies = new[] { 46, 40, 7, 6, 4, 2, 2, 1, 0, 0 };
                        }
                        else if (brawlerTrophies >= 50 && brawlerTrophies <= 99)
                        {
                            Trophies = new[] { 46, 40, 7, 6, 4, 2, 2, 1, 0, 0 };
                        }
                        else if (brawlerTrophies >= 100 && brawlerTrophies <= 199)
                        {
                            Trophies = new[] { 46, 40, 7, 6, 4, 2, 2, 1, 0, 0 };
                        }
                        else if (brawlerTrophies >= 200 && brawlerTrophies <= 299)
                        {
                            Trophies = new[] { 46, 40, 7, 6, 4, 2, 2, 1, 0, 0 };
                        }
                        else if (brawlerTrophies >= 300 && brawlerTrophies <= 399)
                        {
                             Trophies = new[] { 46, 8, 6, 5, 2, 0, 0, -3, -4, -4 };
                        }
                        else if (brawlerTrophies >= 400 && brawlerTrophies <= 499)
                        {
                             Trophies = new[] { 46, 8, 6, 5, 2, -1, -2, -3, -5, -5 };
                        }
                        else if (brawlerTrophies >= 500 && brawlerTrophies <= 599)
                        {
                             Trophies = new[] { 46, 8, 6, 4, 2, -1, -2, -5, -6, -6 };
                        }
                        else if (brawlerTrophies >= 600 && brawlerTrophies <= 699)
                        {
                             Trophies = new[] { 46, 8, 6, 4, 1, -2, -2, -5, -7, -8 };
                        }
                        else if (brawlerTrophies >= 700 && brawlerTrophies <= 799)
                        {
                             Trophies = new[] { 46, 8, 6, 4, 1, -3, -4, -5, -8, -9 };
                        }
                        else if (brawlerTrophies >= 800 && brawlerTrophies <= 899)
                        {
                             Trophies = new[] { 46, 7, 5, 2, 0, -3, -4, -7, -9, -10 };
                        }
                        else if (brawlerTrophies >= 900 && brawlerTrophies <= 999)
                        {
                             Trophies = new[] { 46, 6, 4, 1, -1, -3, -6, -8, -10, -11 };
                        }
                        else if (brawlerTrophies >= 1000 && brawlerTrophies <= 1099)
                        {
                             Trophies = new[] { 46, 5, 3, 1, -2, -5, -6, -9, -11, -12 };
                        }
                        else if (brawlerTrophies >= 1100 && brawlerTrophies <= 1199)
                        {
                             Trophies = new[] { 46, 4, 1, 0, -2, -6, -7, -10, -12, -13 };
                        }
                        else if (brawlerTrophies >= 1200)
                        {
                             Trophies = new[] { 46, 3, 0, -1, -2, -6, -8, -11, -12, -13 };
                        }
                        rank = GetTeamRank(player.TeamIndex);
                        
                        int vipBonusMastery = player.Avatar.VipBonusMastery;
                        int masteryGained = MasteryReward(brawlerTrophies);
                        if (vipBonusMastery > 0)
                        {
                            masteryGained += vipBonusMastery;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGained;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;
                        
                        message.IsPvP = BattleWithTrophies;
                        message.GameMode = 2;
                        message.Result = rank;
                        message.Players = new List<BattlePlayer>();
                        message.Players.Add(player);
                        message.OwnPlayer = player;
                        if (hero != null && hero.Trophies >= hero.HighestTrophies)
                        {
                            hero.HighestTrophies = hero.Trophies;
                        }
                        if (!BattleWithTrophies)
                        {
                            message.BattleWithoutTrophies = true;
                        }

                        int tokensReward = 40 / rank;
                        if (BattleWithTrophies) message.TokensReward = tokensReward;
                        if (rank == 1 && BattleWithTrophies)
                        {
                            player.Avatar.Wins += 1;
                            player.Avatar.WinStreak += 1;
                            if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                            message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                            if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                            switch (player.Avatar.Wins)
                            {
                                case 1:
                                case 4:
                                case 8:
                                    if (player.Home != null)
                                    {
                                        player.Home.DropsCount++;
                                        if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                    }
                                    break;
                                default:
                                    if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                    break;
                            }
                            int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                            int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                            if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                            {
                                trophiesReward += vipBonusTrophies;
                                player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                                Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков (всего: {player.Avatar.VipTotalBonusReceived})");
                            }
                            message.TrophiesReward = trophiesReward;
                            if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                            {
                                if (tokensReward * 2 <= player.Home.TokenDoublers)
                                {
                                    player.Home.TokenDoublers -= tokensReward;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                    message.TokensReward *= 2;
                                }
                                else
                                {
                                    message.TokensReward += player.Home.TokenDoublers;
                                    player.Home.TokenDoublers = 0;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = 0;
                                }
                            }
                            message.TokensReward = tokensReward;
                            player.Avatar.AddTokens(tokensReward);
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            player.Avatar.SoloWins += 1;
                            if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                            {
                                if (player.Avatar.EventTokenCap < 200)
                                {
                                    player.Avatar.AddStarPoints(10);
                                    player.Avatar.EventTokenCap += 10;
                                }
                            }
                            if (hero != null) hero.AddTrophies(message.TrophiesReward);
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                        }
                    }
                    if (IsSecret)
                    {
                        message.GameMode = 1;
                        message.IsPvP = BattleWithTrophies;
                        message.Players = m_players;
                        message.OwnPlayer = player;

                        if (m_winnerTeam == -1 && BattleWithTrophies)
                        {
                            message.Result = 2;
                            message.TrophiesReward = 0;
                        }
                        int brawlerTrophies = 0;
                        if (hero != null)
                        {
                            brawlerTrophies = hero.Trophies;
                        }
                        int winTrophies = 46;
                        int loseTrophies = -8;

                        if (isVictory && BattleWithTrophies)
                        {
                            player.Avatar.Wins += 1;
                            switch (player.Avatar.Wins)
                            {
                                case 1:
                                case 4:
                                case 8:
                                    if (player.Home != null)
                                    {
                                        player.Home.DropsCount++;
                                        if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                    }
                                    break;
                                default:
                                    if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                    break;
                            }
                            player.Avatar.WinStreak += 1;
                            if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                            message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                            if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                            
                            int vipBonusMastery = player.Avatar.VipBonusMastery;
                            int masteryGained = MasteryReward(brawlerTrophies);
                            if (vipBonusMastery > 0)
                            {
                                masteryGained += vipBonusMastery;
                                player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                                Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                            }
                            message.MasteryGained = masteryGained;
                            if (player.Avatar.GetHero(player.CharacterId) != null)
                                player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                            message.Result = 0;

                            int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                            int trophiesReward = winTrophies + message.WinstreakTrophies;
                            if (vipBonusTrophies > 0)
                            {
                                trophiesReward += vipBonusTrophies;
                                player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                                Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в секретном режиме");
                            }
                            message.TrophiesReward = trophiesReward;
                            trophiesReward += player.Kills;
                            trophiesReward -= player.Deaths;
                            if (hero != null) hero.AddTrophies(trophiesReward);
                            int tokensReward = 10;
                            if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                            {
                                if (tokensReward * 2 <= player.Home.TokenDoublers)
                                {
                                    player.Home.TokenDoublers -= tokensReward;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                    message.TokensReward *= 2;
                                }
                                else
                                {
                                    message.TokensReward += player.Home.TokenDoublers;
                                    player.Home.TokenDoublers = 0;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = 0;
                                }
                            }
                            message.TokensReward = tokensReward;
                            player.Avatar.AddTokens(tokensReward);
                            player.Avatar.TrioWins++;
                            if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                            {
                                if (player.Avatar.EventTokenCap < 200)
                                {
                                    player.Avatar.AddStarPoints(10);
                                    player.Avatar.EventTokenCap += 10;
                                }
                            }
                            message.Plus = player.Kills;
                            message.Minus = player.Deaths;
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        }
                        else if (m_winnerTeam != -1 && BattleWithTrophies)
                        {
                            message.Result = 1;
                            message.TokensReward = 10;

                            int trophiesReward = loseTrophies + message.WinstreakTrophies;
                            if (hero != null && hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                            message.TrophiesReward = trophiesReward;
                            trophiesReward += player.Kills;
                            trophiesReward -= player.Deaths;
                            if (hero != null) hero.AddTrophies(trophiesReward);
                            int tokensReward = 10;
                            if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                            {
                                if (tokensReward * 2 <= player.Home.TokenDoublers)
                                {
                                    player.Home.TokenDoublers -= tokensReward;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                    message.TokensReward *= 2;
                                }
                                else
                                {
                                    message.TokensReward += player.Home.TokenDoublers;
                                    player.Home.TokenDoublers = 0;
                                    message.TokenDoublers = tokensReward;
                                    message.TokenDoublersRemaining = 0;
                                }
                            }
                            message.TokensReward = tokensReward;
                            player.Avatar.AddTokens(tokensReward);
                            player.Avatar.WinStreak = 0;
                            message.Plus = player.Kills;
                            message.Minus = player.Deaths;
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        }
                        else
                            message.Result = 2;
                    }
                    bool serverEventVictory = m_gameModeVariation == 6 || m_gameModeVariation == 9 || m_gameModeVariation == 28
                        ? rank == 1
                        : isVictory;
                    int serverEventTrophyBonus = ApplyServerWinEventRewards(player, serverEventVictory);
                    message.TrophiesReward += serverEventTrophyBonus;
                    int newTrophies = hero != null ? hero.HighestTrophies : 0;
                    if (hero != null && BattleWithTrophies && player.Home != null)
                    {
                        for (int i = 0; i < 34; i++)
                        {
                            MilestoneData m = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId(39, i));
                            int progress = m.ProgressStart + m.Progress;
                            if (oldTrophies < progress && newTrophies >= progress)
                            {
                                message.MilestoneId = i;
                                player.Home.BlingsReward += m.SecondaryLvlUpRewardCount;
                                player.Avatar.AddBlings(m.SecondaryLvlUpRewardCount);
                                message.TokensReward += m.PrimaryLvlUpRewardCount;
                                player.Avatar.AddTokens(m.PrimaryLvlUpRewardCount);
                                player.Home.TokenReward += m.PrimaryLvlUpRewardCount;
                            }
                        }
                    }
                    var temp = new List<BattleLogPlayerEntry>();
                    foreach (BattlePlayer plr in _playersSnapshot)
                    {
                        if (plr == null) continue;
                        if (plr.Avatar == null)
                        {
                            var LOGBOT = new BattleLogPlayerEntry
                            {
                                Data = plr.DisplayData,
                                Brawler = plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0,
                                BrawlerPowerLVL = plr.HeroPowerLevel,
                                IsStarPlayer = false,
                                AccountId = -1,
                                Index = plr.TeamIndex,
                                Trophies = new Random().Next(0, 1000)
                            };
                            temp.Add(LOGBOT);
                            continue;
                        }
                        var log = new BattleLogPlayerEntry
                        {
                            Data = plr.DisplayData,
                            Brawler = plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0,
                            BrawlerPowerLVL = plr.HeroPowerLevel,
                            IsStarPlayer = false,
                            AccountId = plr.AccountId,
                            Index = plr.TeamIndex,
                            Trophies = plr.Avatar.GetHero(plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0) != null ? plr.Avatar.GetHero(plr.CharacterIds[0]).Trophies : 0
                        };
                        temp.Add(log);
                    }
                    int logresult = message.Result;
                    if (m_gameModeVariation == 6) logresult = player.BattleRoyaleRank;
                    if (m_gameModeVariation == 9) logresult = _playersAliveForDuo;
                    if (player.Home != null)
                    {
                        player.Home.BattleLogs.Add(new BattleLogEntry
                        {
                            CreationDate = DateTime.Now,
                            Type = 1,
                            Result = message.TrophiesReward,
                            Ticks = GetTicksGone() / 20,
                            BattleWithoutTrophies = false,
                            Location = Location.GetGlobalId(),
                            ResultType = logresult,
                            PlayerEntries = temp,
                            IternalId = player.Home.BattleLogs.GetIndex() + 1

                        });
                    }
                    if (player.GameListener == null) return;
                    if (IsChampionship)
                    {
                        message.BattleWithoutTrophies = false;
                        message.IsPvP = false;
                        message.var = ChampieData.ChallengeType;
                        message.Step = player.Avatar.HomeMode.Home.ChallengeWins;
                        message.end = player.Avatar.HomeMode.Home.ChallengeWins == ChampieWinCount();
                        if (player.Home != null && player.Home.ChallengeWins >= ChampieWinCount())
                        {
                            player.Home.NotificationFactory.Add(new()
                            {
                                Id = 63,
                                gemOffer = new(ChampieData.GemOfferType,
                                ChampieData.GemOfferCount,
                                ChampieData.GemOfferData1,
                                ChampieData.GemOfferData2,
                                ChampieData.GemOfferExtra),
                                Victory = player.Home.ChallengeWins,
                                ChallengeName = ChampieData.Title,
                                ChallengeType = ChampieData.ChallengeType
                            });
                        }
                    }
                    if (m_gameModeVariation == 9)
                    {
                        message.Players.Clear();
                        foreach (BattlePlayer p in GetPlayersByTeamInSnapshot(player.TeamIndex))
                            if (p != null) message.Players.Add(p);
                    }
                    message.Winstreak = player.Avatar.WinStreak;
                    player.GameListener.SendTCPMessage(message);

                    if (player.Avatar != null && player.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(player.Avatar.TeamId);
                }
            }
        }


        public void SendBattleEndToPlayer(BattlePlayer player)
        {
            if (player == null) return;
            if (player.Avatar == null) return;
            if (player.CanSendBattleEnd) return;
            if (player.BattleRoyaleRank == -1) player.BattleRoyaleRank = 1;
            int rank = player.BattleRoyaleRank;
            if (m_gameModeVariation == 9) rank = _playersAliveForDuo + 1;
            player.Avatar.BattleId = -1;
            bool isVictory = m_winnerTeam == player.TeamIndex;
            BattleEndMessage message = new BattleEndMessage();
            Hero hero = player.Avatar.GetHero(player.CharacterId);

            if (BattleWithTrophies)
            {
                try
                {
                    HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                    if (homeMode != null)
                    {
                        if (homeMode.Home.Quests != null)
                        {
                            message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                        }
                    }
                }
                catch {; }

            }

            int oldTrophies = hero != null ? hero.HighestTrophies : 0;
            if (hero != null && hero.Trophies >= hero.HighestTrophies)
                hero.HighestTrophies = hero.Trophies;



            if (m_gameModeVariation != 6 && m_gameModeVariation != 28 && m_gameModeVariation != 9 && !IsSecret)
            {
                message.GameMode = 1;
                message.IsPvP = BattleWithTrophies;
                message.Players = m_players;
                message.OwnPlayer = player;

                if (m_winnerTeam == -1 && BattleWithTrophies)
                {
                    message.Result = 2;
                    message.TrophiesReward = 0;
                }
                int brawlerTrophies = 0;
                if (hero != null)
                {
                    brawlerTrophies = hero.Trophies;
                }

                int winTrophies = 0;
                int loseTrophies = 0;
                if (brawlerTrophies < 800) { winTrophies = 8; loseTrophies = -5; }
                else if (brawlerTrophies < 900) { winTrophies = 7; loseTrophies = -9; }
                else if (brawlerTrophies < 1000) { winTrophies = 6; loseTrophies = -10; }
                else if (brawlerTrophies < 1100) { winTrophies = 5; loseTrophies = -11; }
                else if (brawlerTrophies < 1200) { winTrophies = 4; loseTrophies = -12; }
                else { winTrophies = 3; loseTrophies = -12; }
               
               

                if (isVictory && BattleWithTrophies)
                {
                    player.Avatar.Wins += 1;
                    switch (player.Avatar.Wins)
                    {
                        case 1:
                        case 4:
                        case 8:
                            if (player.Home != null)
                            {
                                player.Home.DropsCount++;
                                if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                            }
                            break;
                        default:
                            if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                            break;
                    }
                    if (!IsChampionship)
                    {
                        if (!IsRanked && !IsPiggy) player.Avatar.WinStreak += 1;
                        if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                        if (!IsRanked && !IsPiggy) message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                        if (!IsRanked && !IsPiggy) if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                        
                        int vipBonusMastery = player.Avatar.VipBonusMastery;
                        int masteryGained = MasteryReward(brawlerTrophies);
                        if (vipBonusMastery > 0)
                        {
                            masteryGained += vipBonusMastery;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGained;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                        message.Result = 0;

                        int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                        int trophiesReward = winTrophies + message.WinstreakTrophies;
                        if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                        {
                            trophiesReward += vipBonusTrophies;
                            player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков (всего: {player.Avatar.VipTotalBonusReceived})");
                        }
                        message.TrophiesReward = trophiesReward;

                        if (!IsRanked && !IsPiggy && hero != null) hero.AddTrophies(trophiesReward);
                        int tokensReward = 10;
                        if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                        {
                            if (tokensReward * 2 <= player.Home.TokenDoublers)
                            {
                                player.Home.TokenDoublers -= tokensReward;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                message.TokensReward *= 2;
                            }
                            else
                            {
                                message.TokensReward += player.Home.TokenDoublers;
                                player.Home.TokenDoublers = 0;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = 0;
                            }
                        }
                        message.TokensReward = tokensReward;
                        player.Avatar.AddTokens(tokensReward);
                        player.Avatar.TrioWins++;
                        if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                        {
                            if (player.Avatar.EventTokenCap < 200)
                            {
                                player.Avatar.AddStarPoints(10);
                                player.Avatar.EventTokenCap += 10;
                            }
                        }

                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                        if (!IsRanked && !IsPiggy && player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        if (IsRanked)
                        {
                            message.RankedMatch = true;
                            RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (r == null) return;
                            switch (r.Solo)
                            {
                                case true:
                                    int v1s = player.Home != null ? player.Home.RankedSoloMaxRank : 0;
                                    int tempRewards = new Random().Next(40, 150);
                                    int a1s = player.Home != null ? player.Home.RankedSoloMaxProgress : 0;
                                    if (player.Home != null) player.Home.RankedSoloProgress += tempRewards;
                                    if (player.Home != null && player.Home.RankedSoloProgress > player.Home.RankedSoloMaxProgress)
                                        if (player.Home != null) player.Home.RankedSoloMaxProgress = player.Home.RankedSoloProgress;
                                    int a2s = player.Home != null ? player.Home.RankedSoloMaxProgress : 0;
                                    int ps = (player.Home != null ? player.Home.RankedSoloRank : 0) * 1000;
                                    if (a1s < ps && a2s >= ps)
                                        if (player.Home != null) player.Home.RankedSoloRank += 1;
                                    if (player.Home != null && player.Home.RankedSoloRank > player.Home.RankedSoloMaxRank)
                                        if (player.Home != null) player.Home.RankedSoloMaxRank = player.Home.RankedSoloRank;
                                    if (v1s != (player.Home != null ? player.Home.RankedSoloMaxRank : 0)) message.UpRank = player.Home != null ? player.Home.RankedSoloMaxRank : 0;
                                    message.Add = tempRewards;
                                    message.ELO = player.Home != null ? player.Home.RankedSoloProgress : 0;
                                    message.Rank = (player.Home != null ? player.Home.RankedSoloRank : 0) + 1;
                                    break;
                                case false:
                                    int v1t = player.Home != null ? player.Home.RankedTrioMaxRank : 0;
                                    int tempReward = 100;
                                    int a1t = player.Home != null ? player.Home.RankedTrioMaxProgress : 0;
                                    if (player.Home != null) player.Home.RankedTrioProgress += tempReward;
                                    if (player.Home != null && player.Home.RankedTrioProgress > player.Home.RankedTrioMaxProgress)
                                        if (player.Home != null) player.Home.RankedTrioMaxProgress = player.Home.RankedTrioProgress;
                                    int a2t = player.Home != null ? player.Home.RankedTrioMaxProgress : 0;
                                    int pt = (player.Home != null ? player.Home.RankedTrioRank : 0) * 1000;
                                    if (a1t < pt && a2t >= pt)
                                        if (player.Home != null) player.Home.RankedTrioRank += 1;
                                    if (player.Home != null && player.Home.RankedTrioRank > player.Home.RankedTrioMaxRank)
                                        if (player.Home != null) player.Home.RankedTrioMaxRank = player.Home.RankedTrioRank;
                                    if (v1t != (player.Home != null ? player.Home.RankedTrioMaxRank : 0)) message.UpRank = player.Home != null ? player.Home.RankedTrioMaxRank : 0;
                                    message.ELO = player.Home != null ? player.Home.RankedTrioProgress : 0;
                                    message.Rank = (player.Home != null ? player.Home.RankedTrioRank : 0) + 1;
                                    break;
                            }
                            if (r != null)
                            {
                                message.TrueBlueTeamWins = r.TrueTeamWins;
                                message.TrueRedTeamWins = r.TrueRedWins;
                                message.Round = !r.MatchOver ? r.Round : 0;
                            }
                            message.Solo = r != null && r.Solo;
                        }
                    }
                    else
                    {
                        message.Result = 0;
                        int tokensReward = 10;
                        if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                        {
                            if (tokensReward * 2 <= player.Home.TokenDoublers)
                            {
                                player.Home.TokenDoublers -= tokensReward;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                message.TokensReward *= 2;
                            }
                            else
                            {
                                message.TokensReward += player.Home.TokenDoublers;
                                player.Home.TokenDoublers = 0;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = 0;
                            }
                        }
                        message.TokensReward = tokensReward;
                        player.Avatar.AddTokens(tokensReward);
                        player.Avatar.TrioWins++;

                        if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                        {
                            if (player.Avatar.EventTokenCap < 200)
                            {
                                player.Avatar.AddStarPoints(10);
                                player.Avatar.EventTokenCap += 10;
                            }
                        }
                        if (player.Home != null)
                        {
                            player.Home.ChallengeWins++;
                            message.reward = new(ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferType,
                                ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferCount,
                                ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData1,
                                ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData2,
                                ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferExtra);
                            player.Home.NotificationFactory.Add(new()
                            {
                                Id = 63,
                                gemOffer = new(ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferType,
    ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferCount,
    ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData1,
    ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferData2,
    ChampieData.cgemofferlist[CalculateIndexForChallengeWinsFrom(player.Home.ChallengeWins)].RewardGemOfferExtra),
                                Victory = player.Home.ChallengeWins,
                                ChallengeName = ChampieData.Title,
                                ChallengeType = ChampieData.ChallengeType
                            });
                        }
                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                    }
                    if (BattleWithTrophies)
                    {
                        try
                        {
                            HomeMode homeMode = LogicServerListener.Instance.GetHomeMode(player.AccountId);
                            if (homeMode != null)
                            {
                                if (homeMode.Home.Quests != null)
                                {
                                    message.ProgressiveQuests = homeMode.Home.Quests.UpdateQuestsProgress(m_gameModeVariation, player.CharacterId, player.Kills, player.Damage, player.Heals, homeMode.Home);
                                }
                            }
                        }
                        catch {; }
                    }
                }
                else if (m_winnerTeam != -1 && BattleWithTrophies)
                {
                    if (!IsChampionship)
                    {
                        message.Result = 1;
                        message.TokensReward = 10;

                        int trophiesReward = loseTrophies + message.WinstreakTrophies;
                        if (!IsRanked && !IsPiggy && hero != null && hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                        if (!IsRanked && !IsPiggy) message.TrophiesReward = trophiesReward;

                        if (!IsRanked && !IsPiggy && hero != null) hero.AddTrophies(trophiesReward);
                        int tokensReward = 10;
                        if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                        {
                            if (tokensReward * 2 <= player.Home.TokenDoublers)
                            {
                                player.Home.TokenDoublers -= tokensReward;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                message.TokensReward *= 2;
                            }
                            else
                            {
                                message.TokensReward += player.Home.TokenDoublers;
                                player.Home.TokenDoublers = 0;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = 0;
                            }
                        }
                        message.TokensReward = tokensReward;
                        player.Avatar.AddTokens(tokensReward);
                        if (!IsRanked && !IsPiggy) player.Avatar.WinStreak = 0;

                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                        if (!IsRanked && !IsPiggy && player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        if (IsPiggy && player.Home != null) player.Home.PiggyBankTickets--;
                        if (IsRanked)
                        {
                            message.RankedMatch = true;
                            RankedMatch r = LogicServerListener.Instance.GetRankedMatch(RankedId);
                            if (r == null) return;
                            switch (r.Solo)
                            {
                                case true:
                                    int tempRewards = new Random().Next(40, 69);
                                    if (player.Home != null) player.Home.RankedSoloProgress -= tempRewards;
                                    if (player.Home != null && player.Home.RankedSoloProgress < 0) player.Home.RankedSoloProgress = 0;
                                    message.ELO = player.Home != null ? player.Home.RankedSoloProgress : 0;
                                    message.Rank = (player.Home != null ? player.Home.RankedSoloRank : 0) + 1;
                                    break;
                                case false:
                                    int tempReward = 65;
                                    if (player.Home != null) player.Home.RankedTrioProgress -= tempReward;
                                    if (player.Home != null && player.Home.RankedTrioProgress < 0) player.Home.RankedTrioProgress = 0;
                                    message.ELO = player.Home != null ? player.Home.RankedTrioProgress : 0;
                                    message.Rank = (player.Home != null ? player.Home.RankedTrioRank : 0) + 1;
                                    break;
                            }
                            if (r != null)
                            {
                                message.TrueBlueTeamWins = r.TrueTeamWins;
                                message.TrueRedTeamWins = r.TrueRedWins;
                                message.Round = !r.MatchOver ? r.Round : 0;
                            }
                            message.Solo = r != null && r.Solo;
                        }
                    }
                    else
                    {
                        message.Result = 1;
                        message.TokensReward = 10;

                        int tokensReward = 10;
                        if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                        {
                            if (tokensReward * 2 <= player.Home.TokenDoublers)
                            {
                                player.Home.TokenDoublers -= tokensReward;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                message.TokensReward *= 2;
                            }
                            else
                            {
                                message.TokensReward += player.Home.TokenDoublers;
                                player.Home.TokenDoublers = 0;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = 0;
                            }
                        }
                        message.TokensReward = tokensReward;
                        player.Avatar.AddTokens(tokensReward);
                        if (player.Home != null) player.Home.ChallengeLoses++;
                        message.tryes = player.Home != null ? player.Home.ChallengeLoses : 0;
                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                    }
                }
                else
                    message.Result = 2;
            }
                        else if (m_gameModeVariation == 6 || m_gameModeVariation == 9)
            {
                int brawlerTrophies = 0;
                if (hero != null)
                {
                    brawlerTrophies = hero.Trophies;
                }

                int[] Trophies = new int[10];
                if (brawlerTrophies >= 0 && brawlerTrophies <= 299)
                {
                    Trophies = new[] { 10, 8, 7, 6, 4, 2, 1, 0, -1, -2 };
                }
                else if (brawlerTrophies >= 300 && brawlerTrophies <= 499)
                {
                    Trophies = new[] { 10, 8, 6, 5, 3, 1, -1, -2, -3, -4 };
                }
                else 
                {
                    Trophies = new[] { 10, 8, 6, 4, 2, -1, -2, -5, -7, -8 };
                }

                if (GetGameModeVariation() == 9)
                {
                    rank = rank - 1;
                    if (brawlerTrophies <= 499)
                    {
                        Trophies = new[] { 9, 7, 4, 0, -2, 0, 0, 0, 0, 0 };
                    }
                    else 
                    {
                        Trophies = new[] { 9, 7, 2, -3, -6, 0, 0, 0, 0, 0 };
                    }
                }
                
                int vipBonusMastery = player.Avatar.VipBonusMastery;
                int masteryGained = MasteryReward(brawlerTrophies);
                if (vipBonusMastery > 0)
                {
                    masteryGained += vipBonusMastery;
                    player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                }
                message.MasteryGained = masteryGained;
                if (player.Avatar.GetHero(player.CharacterId) != null)
                    player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;
                
                message.IsPvP = BattleWithTrophies;
                message.GameMode = m_gameModeVariation;
                message.Result = player.BattleRoyaleRank;
                Console.WriteLine($"Trophies: {Trophies[rank - 1]}");
                if (m_gameModeVariation == 9) 
                {
                    message.GameMode = 2;
                    message.Result = rank; 
                }
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;
                if (hero != null && hero.Trophies >= hero.HighestTrophies)
                {
                    hero.HighestTrophies = hero.Trophies;
                }
                if (!BattleWithTrophies)
                {
                    message.BattleWithoutTrophies = true;
                }

                Console.WriteLine("rank = " + rank);

                int tokensReward = 40 / rank;
                if (BattleWithTrophies) message.TokensReward = tokensReward;
                if (GetGameModeVariation() == 9)
                {
                    if (rank == 2 && BattleWithTrophies)
                    {
                        player.Avatar.Wins += 1;
                        player.Avatar.WinStreak += 1;
                        if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                        message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                        if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                        switch (player.Avatar.Wins)
                        {

                            case 1:
                            case 4:
                            case 8:
                                if (player.Home != null)
                                {
                                    player.Home.DropsCount++;
                                    if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                                }
                                break;
                            default:
                                if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                                break;
                        }
                        int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                        int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                        if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                        {
                            trophiesReward += vipBonusTrophies;
                            player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в дуо (всего: {player.Avatar.VipTotalBonusReceived})");
                        }
                        message.TrophiesReward = trophiesReward;
                        if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                        {
                            if (tokensReward * 2 <= player.Home.TokenDoublers)
                            {
                                player.Home.TokenDoublers -= tokensReward;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = player.Home.TokenDoublers;
                                message.TokensReward *= 2;
                            }
                            else
                            {
                                message.TokensReward += player.Home.TokenDoublers;
                                player.Home.TokenDoublers = 0;
                                message.TokenDoublers = tokensReward;
                                message.TokenDoublersRemaining = 0;
                            }
                        }
                        message.TokensReward = tokensReward;
                        player.Avatar.AddTokens(tokensReward);
                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                        player.Avatar.SoloWins += 1;
                        if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                        {
                            if (player.Avatar.EventTokenCap < 200)
                            {
                                player.Avatar.AddStarPoints(10);
                                player.Avatar.EventTokenCap += 10;
                            }
                        }
                        if (hero != null) hero.AddTrophies(message.TrophiesReward);
                        if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                    }
                    else if (rank == 3 && BattleWithTrophies)
                    {
                        int vipBonusMasteryForLoss = player.Avatar.VipBonusMastery;
                        int masteryGainedLoss = 25;
                        if (vipBonusMasteryForLoss > 0)
                        {
                            masteryGainedLoss += vipBonusMasteryForLoss;
                            player.Avatar.VipMasteryTotalBonusReceived += vipBonusMasteryForLoss;
                            Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMasteryForLoss} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                        }
                        message.MasteryGained = masteryGainedLoss;
                        if (player.Avatar.GetHero(player.CharacterId) != null)
                            player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGainedLoss;
                        
                        int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                        message.TrophiesReward = trophiesReward;

                        player.Avatar.AddTokens(tokensReward);
                        if (player.Home != null) player.Home.TokenReward += tokensReward;
                        if (hero != null) hero.AddTrophies(message.TrophiesReward);
                        if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                    }
                    else
                    {
                        if (BattleWithTrophies)
                        {
                            player.Avatar.WinStreak = 0;
                            int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                            message.TrophiesReward = trophiesReward;

                            player.Avatar.AddTokens(tokensReward);
                            if (player.Home != null) player.Home.TokenReward += tokensReward;
                            if (player.Home != null) player.Home.TrophiesReward += trophiesReward;

                            if (hero != null) hero.AddTrophies(message.TrophiesReward);
                            if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                        }

                    }
                }
                if (rank == 1 && BattleWithTrophies)
                {
                    if (IsSpecialTrophyMode)
                    {
                        message.Plus = player.Kills * 2;
                        if (hero != null) hero.AddTrophies(message.Plus);
                    }
                    player.Avatar.Wins += 1;
                    player.Avatar.WinStreak += 1;
                    if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                    message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                    if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                    switch (player.Avatar.Wins)
                    {
                        case 1:
                        case 4:
                        case 8:
                            if (player.Home != null)
                            {
                                player.Home.DropsCount++;
                                if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                            }
                            break;
                        default:
                            if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                            break;
                    }
                    int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                    int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                    if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                    {
                        trophiesReward += vipBonusTrophies;
                        player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                        Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в соло (всего: {player.Avatar.VipTotalBonusReceived})");
                    }
                    message.TrophiesReward = trophiesReward;
                    if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                    {
                        if (tokensReward * 2 <= player.Home.TokenDoublers)
                        {
                            player.Home.TokenDoublers -= tokensReward;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = player.Home.TokenDoublers;
                            message.TokensReward *= 2;
                        }
                        else
                        {
                            message.TokensReward += player.Home.TokenDoublers;
                            player.Home.TokenDoublers = 0;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = 0;
                        }
                    }
                    message.TokensReward = tokensReward;
                    player.Avatar.AddTokens(tokensReward);
                    if (player.Home != null) player.Home.TokenReward += tokensReward;
                    player.Avatar.SoloWins += 1;
                    if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                    {
                        if (player.Avatar.EventTokenCap < 200)
                        {
                            player.Avatar.AddStarPoints(10);
                            player.Avatar.EventTokenCap += 10;
                        }
                    }
                    if (hero != null) hero.AddTrophies(message.TrophiesReward);
                    if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                }
            }
            else if (m_gameModeVariation == 28)
            {
                int brawlerTrophies = 0;
                if (hero != null)
                {
                    brawlerTrophies = hero.Trophies;
                }

                int[] Trophies = new int[10];
                if (brawlerTrophies >= 0 && brawlerTrophies <= 49)
                {
                    Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 50 && brawlerTrophies <= 99)
                {
                    Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 100 && brawlerTrophies <= 199)
                {
                    Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 200 && brawlerTrophies <= 299)
                {
                    Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 300 && brawlerTrophies <= 399)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 400 && brawlerTrophies <= 499)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 500 && brawlerTrophies <= 599)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 600 && brawlerTrophies <= 699)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 700 && brawlerTrophies <= 799)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 800 && brawlerTrophies <= 899)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 900 && brawlerTrophies <= 999)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 1000 && brawlerTrophies <= 1099)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 1100 && brawlerTrophies <= 1199)
                {
                     Trophies = new[] { 93, 72, 53, 34, 29, 20, 10, -21, -21, -21 };
                }
                else if (brawlerTrophies >= 1200)
                {
                     Trophies = new[] { 72, 51, 32, 21, 13, 7, -21, -43, -53, -87 };
                }
                rank = GetTeamRank(player.TeamIndex);
                
                int vipBonusMastery = player.Avatar.VipBonusMastery;
                int masteryGained = MasteryReward(brawlerTrophies);
                if (vipBonusMastery > 0)
                {
                    masteryGained += vipBonusMastery;
                    player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                    Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                }
                message.MasteryGained = masteryGained;
                if (player.Avatar.GetHero(player.CharacterId) != null)
                    player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;
                
                message.IsPvP = BattleWithTrophies;
                message.GameMode = 2;
                message.Result = rank;
                message.Players = new List<BattlePlayer>();
                message.Players.Add(player);
                message.OwnPlayer = player;
                if (hero != null && hero.Trophies >= hero.HighestTrophies)
                {
                    hero.HighestTrophies = hero.Trophies;
                }
                if (!BattleWithTrophies)
                {
                    message.BattleWithoutTrophies = true;
                }

                int tokensReward = 40 / rank;
                if (BattleWithTrophies) message.TokensReward = tokensReward;
                if (rank == 1 && BattleWithTrophies)
                {
                    player.Avatar.Wins += 1;
                    player.Avatar.WinStreak += 1;
                    if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                    message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                    if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                    switch (player.Avatar.Wins)
                    {
                        case 1:
                        case 4:
                        case 8:
                            if (player.Home != null)
                            {
                                player.Home.DropsCount++;
                                if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                            }
                            break;
                        default:
                            if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                            break;
                    }
                    int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                    int trophiesReward = Trophies[rank - 1] + message.WinstreakTrophies;
                    if (vipBonusTrophies > 0 && !IsRanked && !IsPiggy)
                    {
                        trophiesReward += vipBonusTrophies;
                        player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                        Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков (всего: {player.Avatar.VipTotalBonusReceived})");
                    }
                    message.TrophiesReward = trophiesReward;
                    if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                    {
                        if (tokensReward * 2 <= player.Home.TokenDoublers)
                        {
                            player.Home.TokenDoublers -= tokensReward;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = player.Home.TokenDoublers;
                            message.TokensReward *= 2;
                        }
                        else
                        {
                            message.TokensReward += player.Home.TokenDoublers;
                            player.Home.TokenDoublers = 0;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = 0;
                        }
                    }
                    message.TokensReward = tokensReward;
                    player.Avatar.AddTokens(tokensReward);
                    if (player.Home != null) player.Home.TokenReward += tokensReward;
                    player.Avatar.SoloWins += 1;
                    if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                    {
                        if (player.Avatar.EventTokenCap < 200)
                        {
                            player.Avatar.AddStarPoints(10);
                            player.Avatar.EventTokenCap += 10;
                        }
                    }
                    if (hero != null) hero.AddTrophies(message.TrophiesReward);
                    if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + message.TrophiesReward, 0);
                }
            }
            if (IsSecret)
            {
                message.GameMode = 1;
                message.IsPvP = BattleWithTrophies;
                message.Players = m_players;
                message.OwnPlayer = player;

                if (m_winnerTeam == -1 && BattleWithTrophies)
                {
                    message.Result = 2;
                    message.TrophiesReward = 0;
                }
                int brawlerTrophies = 0;
                if (hero != null)
                {
                    brawlerTrophies = hero.Trophies;
                }
                int winTrophies = 46;
                int loseTrophies = -8;

                if (isVictory && BattleWithTrophies)
                {
                    player.Avatar.Wins += 1;
                    switch (player.Avatar.Wins)
                    {
                        case 1:
                        case 4:
                        case 8:
                            if (player.Home != null)
                            {
                                player.Home.DropsCount++;
                                if (player.Home.TwoDropsEvent) player.Home.DropsCount++;
                            }
                            break;
                        default:
                            if (player.Home != null) player.Home.ExecuteLobbyDrop = false;
                            break;
                    }
                    player.Avatar.WinStreak += 1;
                    if (player.Avatar.MaxWinstreak < player.Avatar.WinStreak) player.Avatar.MaxWinstreak = player.Avatar.WinStreak;
                    message.WinstreakTrophies = player.Avatar.WinStreak - 1;
                    if (message.WinstreakTrophies > 5) message.WinstreakTrophies = 5;
                    
                    int vipBonusMastery = player.Avatar.VipBonusMastery;
                    int masteryGained = MasteryReward(brawlerTrophies);
                    if (vipBonusMastery > 0)
                    {
                        masteryGained += vipBonusMastery;
                        player.Avatar.VipMasteryTotalBonusReceived += vipBonusMastery;
                        Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус к мастерству +{vipBonusMastery} (всего: {player.Avatar.VipMasteryTotalBonusReceived})");
                    }
                    message.MasteryGained = masteryGained;
                    if (player.Avatar.GetHero(player.CharacterId) != null)
                        player.Avatar.GetHero(player.CharacterId).MasteryPoints += masteryGained;

                    message.Result = 0;

                    int vipBonusTrophies = player.Avatar.VipBonusTrophies;
                    int trophiesReward = winTrophies + message.WinstreakTrophies;
                    if (vipBonusTrophies > 0)
                    {
                        trophiesReward += vipBonusTrophies;
                        player.Avatar.VipTotalBonusReceived += vipBonusTrophies;
                        Console.WriteLine($"[VIP] Игрок {player.Avatar.Name} получил VIP бонус +{vipBonusTrophies} кубков в секретном режиме");
                    }
                    message.TrophiesReward = trophiesReward;
                    trophiesReward += player.Kills;
                    trophiesReward -= player.Deaths;
                    if (hero != null) hero.AddTrophies(trophiesReward);
                    int tokensReward = 10;
                    if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                    {
                        if (tokensReward * 2 <= player.Home.TokenDoublers)
                        {
                            player.Home.TokenDoublers -= tokensReward;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = player.Home.TokenDoublers;
                            message.TokensReward *= 2;
                        }
                        else
                        {
                            message.TokensReward += player.Home.TokenDoublers;
                            player.Home.TokenDoublers = 0;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = 0;
                        }
                    }
                    message.TokensReward = tokensReward;
                    player.Avatar.AddTokens(tokensReward);
                    player.Avatar.TrioWins++;
                    if (TimerMath(new DateTime(2005, 9, 3, 12, 0, 0), new DateTime(2025, 11, 1, 12, 0, 0)) > 0)
                    {
                        if (player.Avatar.EventTokenCap < 200)
                        {
                            player.Avatar.AddStarPoints(10);
                            player.Avatar.EventTokenCap += 10;
                        }
                    }
                    message.Plus = player.Kills;
                    message.Minus = player.Deaths;
                    if (player.Home != null) player.Home.TokenReward += tokensReward;
                    if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                }
                else if (m_winnerTeam != -1 && BattleWithTrophies)
                {
                    message.Result = 1;
                    message.TokensReward = 10;

                    int trophiesReward = loseTrophies + message.WinstreakTrophies;
                    if (hero != null && hero.Trophies < -trophiesReward) trophiesReward = -hero.Trophies;
                    message.TrophiesReward = trophiesReward;
                    trophiesReward += player.Kills;
                    trophiesReward -= player.Deaths;
                    if (hero != null) hero.AddTrophies(trophiesReward);
                    int tokensReward = 10;
                    if (BattleWithTrophies && player.Home != null && player.Home.TokenDoublers > 0 && tokensReward > 0)
                    {
                        if (tokensReward * 2 <= player.Home.TokenDoublers)
                        {
                            player.Home.TokenDoublers -= tokensReward;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = player.Home.TokenDoublers;
                            message.TokensReward *= 2;
                        }
                        else
                        {
                            message.TokensReward += player.Home.TokenDoublers;
                            player.Home.TokenDoublers = 0;
                            message.TokenDoublers = tokensReward;
                            message.TokenDoublersRemaining = 0;
                        }
                    }
                    message.TokensReward = tokensReward;
                    player.Avatar.AddTokens(tokensReward);
                    player.Avatar.WinStreak = 0;
                    message.Plus = player.Kills;
                    message.Minus = player.Deaths;
                    if (player.Home != null) player.Home.TokenReward += tokensReward;
                    if (player.Home != null) player.Home.TrophiesReward += LogicMath.Max(player.Home.TrophiesReward + trophiesReward, 0);
                }
                else
                    message.Result = 2;
            }
            bool serverEventVictory = m_gameModeVariation == 6 || m_gameModeVariation == 9 || m_gameModeVariation == 28
                ? rank == 1
                : isVictory;
            int serverEventTrophyBonus = ApplyServerWinEventRewards(player, serverEventVictory);
            message.TrophiesReward += serverEventTrophyBonus;
            int newTrophies = hero != null ? hero.HighestTrophies : 0;
            if (hero != null && BattleWithTrophies && player.Home != null)
            {
                for (int i = 0; i < 34; i++)
                {
                    MilestoneData m = DataTables.Get(DataType.Milestone).GetDataByGlobalId<MilestoneData>(GlobalId.CreateGlobalId(39, i));
                    int progress = m.ProgressStart + m.Progress;
                    if (oldTrophies < progress && newTrophies >= progress)
                    {
                        message.MilestoneId = i;
                        player.Home.BlingsReward += m.SecondaryLvlUpRewardCount;
                        player.Avatar.AddBlings(m.SecondaryLvlUpRewardCount);
                        message.TokensReward += m.PrimaryLvlUpRewardCount;
                        player.Avatar.AddTokens(m.PrimaryLvlUpRewardCount);
                        player.Home.TokenReward += m.PrimaryLvlUpRewardCount;
                    }
                }
            }
            var temp = new List<BattleLogPlayerEntry>();
            foreach (BattlePlayer plr in _playersSnapshot)
            {
                if (plr == null) continue;
                if (plr.Avatar == null)
                {
                    var LOGBOT = new BattleLogPlayerEntry
                    {
                        Data = plr.DisplayData,
                        Brawler = plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0,
                        BrawlerPowerLVL = plr.HeroPowerLevel,
                        IsStarPlayer = false,
                        AccountId = -1,
                        Index = plr.TeamIndex,
                        Trophies = new Random().Next(0, 1000)
                    };
                    temp.Add(LOGBOT);
                    continue;
                }
                var log = new BattleLogPlayerEntry
                {
                    Data = plr.DisplayData,
                    Brawler = plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0,
                    BrawlerPowerLVL = plr.HeroPowerLevel,
                    IsStarPlayer = false,
                    AccountId = plr.AccountId,
                    Index = plr.TeamIndex,
                    Trophies = plr.Avatar.GetHero(plr.CharacterIds != null && plr.CharacterIds.Length > 0 ? plr.CharacterIds[0] : 0) != null ? plr.Avatar.GetHero(plr.CharacterIds[0]).Trophies : 0
                };
                temp.Add(log);
            }
            int logresult = message.Result;
            if (m_gameModeVariation == 6) logresult = player.BattleRoyaleRank;
            if (m_gameModeVariation == 9) logresult = _playersAliveForDuo;
            if (player.Home != null)
            {
                player.Home.BattleLogs.Add(new BattleLogEntry
                {
                    CreationDate = DateTime.Now,
                    Type = 1,
                    Result = message.TrophiesReward,
                    Ticks = GetTicksGone() / 20,
                    BattleWithoutTrophies = false,
                    Location = Location.GetGlobalId(),
                    ResultType = logresult,
                    PlayerEntries = temp,
                    IternalId = player.Home.BattleLogs.GetIndex() + 1

                });
            }
            if (player.GameListener == null) return;
            if (IsChampionship)
            {
                message.BattleWithoutTrophies = false;
                message.IsPvP = false;
                message.var = ChampieData.ChallengeType;
                message.Step = player.Avatar.HomeMode.Home.ChallengeWins;
                message.end = player.Avatar.HomeMode.Home.ChallengeWins == ChampieWinCount();
                if (player.Home != null && player.Home.ChallengeWins >= ChampieWinCount())
                {
                    player.Home.NotificationFactory.Add(new()
                    {
                        Id = 63,
                        gemOffer = new(ChampieData.GemOfferType,
                        ChampieData.GemOfferCount,
                        ChampieData.GemOfferData1,
                        ChampieData.GemOfferData2,
                        ChampieData.GemOfferExtra),
                        Victory = player.Home.ChallengeWins,
                        ChallengeName = ChampieData.Title,
                        ChallengeType = ChampieData.ChallengeType
                    });
                }
            }
            if (m_gameModeVariation == 9)
            {
                message.Players.Clear();
                foreach (BattlePlayer p in GetPlayersByTeamInSnapshot(player.TeamIndex))
                    if (p != null) message.Players.Add(p);
            }
            message.Winstreak = player.Avatar.WinStreak;

            player.GameListener.SendTCPMessage(message);

            if (player.Avatar != null && player.Avatar.TeamId > 0) LogicServerListener.Instance.UpdateTeam(player.Avatar.TeamId);
        }
        public int GetTeamScore(int team)
        {
            int score = 0;
            foreach (BattlePlayer player in m_players)
            {
                if (player != null && player.TeamIndex == team) score += player.GetScore();
            }
            return score;
        }
        private int CalculateIsRoundOver()
        {
            if (GetAliveCharactersByTeam(0).Count <= 0 && GetAliveCharactersByTeam(1).Count > 0) return 1;
            if (GetAliveCharactersByTeam(1).Count <= 0 && GetAliveCharactersByTeam(0).Count > 0) return 0;
            if (RoundTicks > 4095) return 2;
            return -1;
        }
        private bool CalculateIsGameOver()
        {
            switch (m_gameModeVariation)
            {
                case 20:
                    for (int i = 0; i < 2; i++)
                    {
                        if (TScore[i] == 2)
                        {
                            m_winnerTeam = i;
                            foreach (Character character in m_gameObjectManager.GetCharacters())
                            {
                                if (character != null) character.StopAI = true;
                            }
                            if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                            _SetStopTicks = true;
                            if (!_pinsReseted)
                            {
                                foreach (BattlePlayer player in m_players)
                                {
                                    if (player != null) player.LastPinUseTicks = -999;
                                }
                                _pinsReseted = true;
                            }
                            if (GetTicksGone() == _StopTicks) return true;
                        }
                    }
                    break;
                case 0:
                    if (GetTeamScore(0) > GetTeamScore(1) && GetTeamScore(0) >= 10)
                    {
                        if (m_gemGrabCountdown == 0)
                        {
                            m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                        }
                        else if (GetTicksGone() > m_gemGrabCountdown)
                        {
                            m_winnerTeam = 0;
                            foreach (Character character in m_gameObjectManager.GetCharacters())
                            {
                                if (character != null) character.StopAI = true;
                            }
                            if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                            _SetStopTicks = true;
                            if (!_pinsReseted)
                            {
                                foreach (BattlePlayer player in m_players)
                                {
                                    if (player != null) player.LastPinUseTicks = -999;
                                }
                                _pinsReseted = true;
                            }
                            if (GetTicksGone() == _StopTicks) return true;
                        }
                    }
                    else if (GetTeamScore(0) < GetTeamScore(1) && GetTeamScore(1) >= 10)
                    {
                        if (m_gemGrabCountdown == 0)
                        {
                            m_gemGrabCountdown = GetTicksGone() + 20 * 17;
                        }
                        else if (GetTicksGone() > m_gemGrabCountdown)
                        {
                            m_winnerTeam = 1;
                            foreach (Character character in m_gameObjectManager.GetCharacters())
                            {
                                if (character != null) character.StopAI = true;
                            }
                            if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                            _SetStopTicks = true;
                            if (!_pinsReseted)
                            {
                                foreach (BattlePlayer player in m_players)
                                {
                                    if (player != null) player.LastPinUseTicks = -999;
                                }
                                _pinsReseted = true;
                            }
                            if (GetTicksGone() == _StopTicks) return true;
                        }
                    }
                    else
                    {
                        m_gemGrabCountdown = 0;
                    }
                    break;
                case 3:
                    if (GetTeamScore(0) >= 20)
                    {
                        m_winnerTeam = 0;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    else if (GetTeamScore(1) >= 20)
                    {
                        m_winnerTeam = 1;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 25:
                    if (GetTeamScore(0) >= 10)
                    {
                        m_winnerTeam = 0;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    else if (GetTeamScore(1) >= 10)
                    {
                        m_winnerTeam = 1;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 27:
                    if (GetTeamScore(0) >= 8)
                    {
                        m_winnerTeam = 0;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    else if (GetTeamScore(1) >= 8)
                    {
                        m_winnerTeam = 1;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 9:
                    if (GetAliveTeams() <= 1)
                    {
                        m_winnerTeam = 1;
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 60;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 17:
                    if (Zone1Score >= 100)
                    {
                        m_winnerTeam = 0;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    else if (Zone2Score >= 100)
                    {
                        m_winnerTeam = 1;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 6:
                    if (m_playersAlive <= 1)
                    {
                        m_winnerTeam = 1;
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 60;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 19:
                    int Base1Alive = 0;
                    int Base2Alive = 0;
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character == null) continue;
                        if (!character.CharacterData.IsHero()) continue;
                        if (!character.IsAlive()) continue;
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam1King()) Base1Alive = character.GetHitpointPercentage();
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam2King()) Base2Alive = character.GetHitpointPercentage();
                    }
                    if (Base1Alive <= 0 || Base2Alive <= 0)
                    {
                        if (Base1Alive <= 0)
                        {
                            m_winnerTeam = 1;
                        }
                        else if (Base2Alive <= 0)
                        {
                            m_winnerTeam = 0;
                        }
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
                case 28:

                    for (int i = 0; i < 10; i++)
                    {
                        if (GetTeamScore(i) >= 6)
                        {
                            m_winnerTeam = 1;
                            foreach (Character character in m_gameObjectManager.GetCharacters())
                            {
                                if (character != null) character.StopAI = true;
                            }
                            if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                            _SetStopTicks = true;
                            if (!_pinsReseted)
                            {
                                foreach (BattlePlayer player in m_players)
                                {
                                    if (player != null) player.LastPinUseTicks = -999;
                                }
                                _pinsReseted = true;
                            }
                            if (GetTicksGone() == _StopTicks) return true;
                        }
                    }
                    break;
                case 5:
                    if (GetTeamScore(0) >= 2)
                    {
                        CanReset = false;
                        m_winnerTeam = 0;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    else if (GetTeamScore(1) >= 2)
                    {
                        m_winnerTeam = 1;
                        CanReset = false;
                        foreach (Character character in m_gameObjectManager.GetCharacters())
                        {
                            if (character != null) character.StopAI = true;
                        }
                        if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                        _SetStopTicks = true;
                        if (!_pinsReseted)
                        {
                            foreach (BattlePlayer player in m_players)
                            {
                                if (player != null) player.LastPinUseTicks = -999;
                            }
                            _pinsReseted = true;
                        }
                        if (GetTicksGone() == _StopTicks) return true;
                    }
                    break;
            }

            if (GameModeUtil.HasTwoBases(m_gameModeVariation))
            {
                int Base1Alive = 0;
                int Base2Alive = 0;
                foreach (Character character in m_gameObjectManager.GetCharacters())
                {
                    if (character == null) continue;
                    if (character.CharacterData.IsBase())
                    {
                        if (character.GetIndex() / 16 == 0) Base1Alive = character.GetHitpointPercentage();
                        else Base2Alive = character.GetHitpointPercentage();
                    }
                }
                if (Base1Alive <= 0 || Base2Alive <= 0)
                {
                    if (Base1Alive <= 0)
                    {
                        m_winnerTeam = 1;
                    }
                    else if (Base2Alive <= 0)
                    {
                        m_winnerTeam = 0;
                    }
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character != null) character.StopAI = true;
                    }
                    if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                    _SetStopTicks = true;
                    if (!_pinsReseted)
                    {
                        foreach (BattlePlayer player in m_players)
                        {
                            if (player != null) player.LastPinUseTicks = -999;
                        }
                        _pinsReseted = true;
                    }
                    if (GetTicksGone() == _StopTicks) return true;
                }
            }
            if (GetTicksGone() >= GameModeUtil.GetBattleTicks(m_gameModeVariation) - 1)
            {
                if (GameModeUtil.HasTwoBases(m_gameModeVariation))
                {
                    int Base1Alive = 0;
                    int Base2Alive = 0;
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character == null) continue;
                        if (character.CharacterData.IsBase())
                        {
                            if (character.GetIndex() / 16 == 0) Base1Alive = character.GetHitpointPercentage();
                            else Base2Alive = character.GetHitpointPercentage();
                        }
                    }
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character != null) character.StopAI = true;
                    }
                    if (Base1Alive > Base2Alive) m_winnerTeam = 0;
                    else if (Base1Alive < Base2Alive) m_winnerTeam = 1;
                    else m_winnerTeam = -1;
                }
                foreach (Character character in m_gameObjectManager.GetCharacters())
                {
                    if (character != null) character.StopAI = true;
                }
                if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                _SetStopTicks = true;
                if (!_pinsReseted)
                {
                    foreach (BattlePlayer player in m_players)
                    {
                        if (player != null) player.LastPinUseTicks = -999;
                    }
                    _pinsReseted = true;
                }
                if (GetTicksGone() == _StopTicks) return true;
            }
            if (m_gameModeVariation == 5 && GetTicksGone() >= 3200 - 1)
            {
                if (GetTeamScore(0) == GetTeamScore(1) && GetTileMap() != null && !GetTileMap().IsDestroyed) GetTileMap().DestroyAll();
                else if (GetTeamScore(0) != GetTeamScore(1))
                {
                    int tmp = 0;
                    if (GetTeamScore(0) > GetTeamScore(1)) tmp = 0;
                    else if (GetTeamScore(0) < GetTeamScore(1)) tmp = 1;
                    m_winnerTeam = tmp;
                    CanReset = false;
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character != null) character.StopAI = true;
                    }
                    if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                    _SetStopTicks = true;
                    if (!_pinsReseted)
                    {
                        foreach (BattlePlayer player in m_players)
                        {
                            if (player != null) player.LastPinUseTicks = -999;
                        }
                        _pinsReseted = true;
                    }
                    if (GetTicksGone() == _StopTicks) return true;
                }

                if (GetTicksGone() >= 4400 - 1)
                {
                    CanReset = false;
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character != null) character.StopAI = true;
                    }
                    if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                    _SetStopTicks = true;
                    if (!_pinsReseted)
                    {
                        foreach (BattlePlayer player in m_players)
                        {
                            if (player != null) player.LastPinUseTicks = -999;
                        }
                        _pinsReseted = true;
                    }
                    if (GetTicksGone() == _StopTicks) return true;
                }
            }
            if (GetTicksGone() >= GameModeUtil.GetBattleTicks(m_gameModeVariation) - 1)
            {
                if (m_gameModeVariation == 19)
                {
                    int Base1Alive = 0;
                    int Base2Alive = 0;
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character == null) continue;
                        if (!character.CharacterData.IsHero()) continue;
                        if (!character.IsAlive()) continue;
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam1King()) Base1Alive = character.GetHitpointPercentage();
                        if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetTeam2King()) Base2Alive = character.GetHitpointPercentage();
                    }
                    foreach (Character character in m_gameObjectManager.GetCharacters())
                    {
                        if (character != null) character.StopAI = true;
                    }
                    if (Base1Alive > Base2Alive) m_winnerTeam = 0;
                    else if (Base1Alive < Base2Alive) m_winnerTeam = 1;
                    else m_winnerTeam = -1;
                }
                if (m_gameModeVariation == 27)
                {
                    if (GetTeamScore(0) > GetTeamScore(1)) m_winnerTeam = 0;
                    else if (GetTeamScore(0) < GetTeamScore(1)) m_winnerTeam = 1;
                    else m_winnerTeam = -1;
                }
                if (m_gameModeVariation == 3)
                {
                    if (GetTeamScore(0) > GetTeamScore(1)) m_winnerTeam = 0;
                    else if (GetTeamScore(0) < GetTeamScore(1)) m_winnerTeam = 1;
                    else if (GetTeamScore(0) == GetTeamScore(1)) goto LABEL_1;
                    else m_winnerTeam = -1;
                    LABEL_1:
                    m_winnerTeam = GetWinnerTeamWithBlueStarWhenDrawInBounty();
                }
                foreach (Character character in m_gameObjectManager.GetCharacters())
                {
                    if (character != null) character.StopAI = true;
                }
                if (!_SetStopTicks || _StopTicks == 0) _StopTicks = GetTicksGone() + 65;
                _SetStopTicks = true;
                if (!_pinsReseted)
                {
                    foreach (BattlePlayer player in m_players)
                    {
                        if (player != null) player.LastPinUseTicks = -999;
                    }
                    _pinsReseted = true;
                }
                if (GetTicksGone() == _StopTicks) return true;
            }

            if (GetTicksGone() > 120)
            {
                if (GameModeUtil.HasTwoTeams(m_gameModeVariation) && m_gameModeVariation != 3)
                {
                    if (PlayerIndexesForTeam1.Count > 3 || PlayerIndexesForTeam2.Count > 3)
                    {
                        m_winnerTeam = -1;
                        return true;
                    }
                }
                if (m_gameModeVariation == 3 && HasEventModifier(15))
                {
                    if (PlayerIndexesForTeam1.Count > 1 || PlayerIndexesForTeam2.Count > 1)
                    {
                        m_winnerTeam = -1;
                        return true;
                    }
                }

            }
            return false;
        }

        public int GetWinnerTeamWithBlueStarWhenDrawInBounty()
        {
            foreach (BattlePlayer LAYER in m_players)
            {
                if (LAYER != null && LAYER.HasStar) return LAYER.TeamIndex;
            }
            return -1;
        }
        public int GetAliveTeams()
        {
            List<int> teams = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                if (GetAliveCharactersByTeam(i).Count >= 1 && !teams.Contains(i)) teams.Add(i);
            }
            return teams.Count;
        }
        public List<int> GetAliveTeamsList()
        {
            List<int> teams = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                if (GetAliveCharactersByTeam(i).Count >= 1 && !teams.Contains(i)) teams.Add(i);
            }
            return teams;
        }
        public List<Character> GetAliveCharactersByTeam(int teamIndex)
        {
            if (m_gameObjectManager == null) return new List<Character>();
            List<Character> teamCharacters = new List<Character>();

            foreach (Character character in m_gameObjectManager.GetCharacters())
            {
                if (character == null) continue;
                BattlePlayer player = character.GetPlayer();
                if (player != null && player.TeamIndex == teamIndex)
                {
                    if (player.IsAlive) teamCharacters.Add(character);
                }
            }

            return teamCharacters;
        }
        public Character GetTeammate(int teamindex, int playerindx)
        {
            foreach (Character plr in GetCharactersByTeam(teamindex))
            {
                if (plr != null && plr.GetPlayer() != null && plr.GetPlayer().PlayerIndex != playerindx) return plr;
            }
            return null;
        }
        public bool IsTeammateAlive(int teamindex, int playerindx)
        {
            foreach (Character plr in GetCharactersByTeam(teamindex))
            {
                if (plr != null && plr.GetPlayer() != null && plr.GetPlayer().PlayerIndex != playerindx && plr.IsAlive()) return true;
            }
            return false;
        }
        public List<int> GetPlayerIndexesByTeam(int teamIndex)
        {
            List<int> teamCharacters = new List<int>();

            foreach (BattlePlayer player in m_players)
            {
                if (player != null && player.TeamIndex == teamIndex)
                {
                    teamCharacters.Add(player.PlayerIndex);
                }
            }

            return teamCharacters;
        }


        public List<Character> GetCharactersByTeam(int teamIndex)
        {
            if (m_gameObjectManager == null) return new List<Character>();
            List<Character> teamCharacters = new List<Character>();

            foreach (Character character in m_gameObjectManager.GetCharacters())
            {
                if (character == null) continue;
                BattlePlayer player = character.GetPlayer();
                if (player != null && player.TeamIndex == teamIndex)
                {
                    teamCharacters.Add(character);
                }
            }
            return teamCharacters;
        }
        public List<BattlePlayer> GetPlayersByTeam(int team)
        {
            List<BattlePlayer> teamCharacters = new List<BattlePlayer>();

            foreach (BattlePlayer bplr in m_players)
            {
                if (bplr != null && bplr.TeamIndex == team)
                {
                    teamCharacters.Add(bplr);
                }
            }
            return teamCharacters;
        }
        public List<BattlePlayer> GetPlayersByTeamInSnapshot(int team)
        {
            List<BattlePlayer> teamCharacters = new List<BattlePlayer>();

            foreach (BattlePlayer bplr in _playersSnapshot)
            {
                if (bplr != null && bplr.TeamIndex == team)
                {
                    teamCharacters.Add(bplr);
                }
            }
            return teamCharacters;
        }

        public BattlePlayer GetTeammateByPlayer(int teamindex, int playerindx)
        {
            foreach (BattlePlayer plr in GetPlayersByTeam(teamindex))
            {
                if (plr != null && plr.PlayerIndex != playerindx) return plr;
            }
            return null;
        }
        public bool ResetCarryableState;
        public void ResetCarryable()
        {
            if (Carryable == null) return;
            
            try
            {
                Carryable.CarringCharacter = null;
                Carryable.StopKicking();
                Carryable.CauseDamage(null, 99999, 99999, false, null, false, false, -1, -1, false, true);
                Carryable = null;
                foreach (Character cha in m_gameObjectManager.GetCharacters())
                {
                    if (cha != null)
                    {
                        cha.CarryablePersonImunTicks = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ResetCarryable] Error: {ex.Message}");
            }
        }
        public int KnockoutTicks;
        public void TickTimers()
        {
            if (m_gameModeVariation == 9) _playersAliveForDuo = GetAliveTeams();
            if (TmpBrawlballTick > 1) TmpBrawlballTick--;
            if (TmpBrawlballTick == 1) ResetCarryable();
            if (m_gameModeVariation == 5 && GoalTicks > 0) GoalTicks = Math.Max(0, GoalTicks - 1);
            if (KnockoutTicks > 0) KnockoutTicks = Math.Max(0, KnockoutTicks - 1);
            if (GoalTicks <= 1 && ToGoalTeam != -1) ResetRoundLaserBall(GetGameModeVariation() == 5);
            if (KnockoutTicks == 1) ResetRoundLaserBall(false);
            if (GoalTicks <= 1 && ToGoalTeam != -1) ToGoalTeam = -1;
            SpikeTileTick = Math.Min(SpikeTileTick + 1, int.MaxValue);
            if (m_gameModeVariation == 20)
            {
                if (CalculateIsRoundOver() != -1 && !nokbool)
                {
                    int res = CalculateIsRoundOver();
                    nokbool = true;
                    KnockoutTicks = 3 * 20;
                    if (RoundCount >= 0 && RoundCount < RoundWins.Length) RoundWins[RoundCount] = res;
                    RoundCount++;
                    if ((res == 0 || res == 1) && res >= 0 && res < TScore.Count) TScore[res]++;
                }
                RoundTicks++;
            }
            IsSpecialTrophyMode = EventModifiers.Contains(15);
        }
        private void Tick()
        {
            try
            {
                m_gameObjectManager.Tick();
                m_tileMap.Tick(m_gameObjectManager);
                TickSpawnHeroes();
                TickPetrols();
                TickModifier();
                UpdatePlayerStatus();
                if (m_gameModeVariation == 27) TickInvasion();
                if (m_gameModeVariation == 8) TicKCoopEvents();
                TickTimers();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Tick] Error: {ex.Message}");
            }
        }
        private void TicKCoopEvents()
        {
            int currentRound = CoopRound + 1;
            int ticksForRound = CoopTickPerRound.ContainsKey(currentRound) ? CoopTickPerRound[currentRound] : 140;
            
            if (GetTicksGone() >= CoopRoundTick + ticksForRound)
            {
                CoopRoundTick = GetTicksGone();
                CoopRound++;
                CoopSpawnBots();
            }
        }

        private void CoopSpawnBots()
        {
            Random rand = new Random();
            int mapWidth = GetTileMap().LogicWidth;
            int mapHeight = GetTileMap().LogicHeight;
            
            int meleeCount = 2 + CoopRound;
            if (meleeCount > 8) meleeCount = 8;
            
            for (int i = 0; i < meleeCount; i++)
            {
                Character bot = new Character(DataTables.GetCharacterByName("InvasionMeleeEnemy"));
                if (bot != null)
                {
                    int spawnX = rand.Next(300, mapWidth - 300);
                    int spawnY = rand.Next(300, mapHeight - 300);
                    bot.SetPosition(spawnX, spawnY, 0);
                    bot.SetIndex(16);
                    GetGameObjectManager().AddGameObject(bot);
                }
            }

            if (CoopRound >= 3)
            {
                int rangedCount = 1 + (CoopRound / 3);
                if (rangedCount > 4) rangedCount = 4;
                
                for (int i = 0; i < rangedCount; i++)
                {
                    Character bot = new Character(DataTables.GetCharacterByName("InvasionRangedEnemy"));
                    if (bot != null)
                    {
                        int spawnX = rand.Next(300, mapWidth - 300);
                        int spawnY = rand.Next(300, mapHeight - 300);
                        bot.SetPosition(spawnX, spawnY, 0);
                        bot.SetIndex(16);
                        GetGameObjectManager().AddGameObject(bot);
                    }
                }
            }

            if (CoopRound >= 5)
            {
                int fastCount = CoopRound / 5;
                if (fastCount > 3) fastCount = 3;
                
                for (int i = 0; i < fastCount; i++)
                {
                    Character bot = new Character(DataTables.GetCharacterByName("InvasionFastMeleeEnemy"));
                    if (bot != null)
                    {
                        int spawnX = rand.Next(300, mapWidth - 300);
                        int spawnY = rand.Next(300, mapHeight - 300);
                        bot.SetPosition(spawnX, spawnY, 0);
                        bot.SetIndex(16);
                        GetGameObjectManager().AddGameObject(bot);
                    }
                }
            }

            if (CoopRound == 5 || CoopRound == 10 || CoopRound == 15 || CoopRound == 20)
            {
                Character boss = new Character(DataTables.GetCharacterByName("InvasionBossEnemy"));
                if (boss != null)
                {
                    int spawnX = rand.Next(300, mapWidth - 300);
                    int spawnY = rand.Next(300, mapHeight - 300);
                    boss.SetPosition(spawnX, spawnY, 0);
                    boss.SetIndex(16);
                    GetGameObjectManager().AddGameObject(boss);
                }
            }
        }

        private void TickSpawnEventStuffDelayed()
        {

            if (m_gameModeVariation == 6 && (TimerMath(new DateTime(2024, 11, 9, 12, 0, 0), new DateTime(2034, 11, 16, 12, 0, 0)) != -1))
            {
                Random rand = new Random();
                int x_spawn = 0;
                int y_spawn = 0;
                if (GetTicksGone() == 0)
                {
                    for (int x = 0; x < 7; x++)
                    {
                        rand = new Random();
                        x_spawn = rand.Next(900, 17100);
                        y_spawn = rand.Next(900, 17100);
                        Tile tile1 = m_tileMap.GetTile(((x_spawn) / 300), ((y_spawn) / 300), true);

                        if ((tile1.Data.BlocksMovement && !tile1.IsDestructed()))
                        {
                            Console.WriteLine("NotOk");
                        }
                        else
                        {
                            Item Money = new Item(DataTables.Get(18).GetData<ItemData>("Speed"));
                            Money.SetPosition(x_spawn, y_spawn, 0);
                            m_gameObjectManager.AddGameObject(Money);
                        }
                    }
                }
                if (GetTicksGone() % 500 == 0)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        rand = new Random();
                        x_spawn = rand.Next(900, 17100);
                        y_spawn = rand.Next(900, 17100);
                        Tile tile1 = m_tileMap.GetTile(((x_spawn) / 300), ((y_spawn) / 300), true);

                        if ((tile1.Data.BlocksMovement && !tile1.IsDestructed()))
                        {
                            Console.WriteLine("NotOk");
                        }
                        else
                        {
                            Item Money = new Item(DataTables.Get(18).GetData<ItemData>("Speed"));
                            Money.SetPosition(x_spawn, y_spawn, 0);
                            m_gameObjectManager.AddGameObject(Money);
                        }
                    }
                }
            }

            if (m_gameModeVariation != 6 && (TimerMath(new DateTime(2024, 11, 9, 12, 0, 0), new DateTime(2034, 11, 16, 12, 0, 0)) != -1))
            {
                Random rand = new Random();
                int x_spawn = 0;
                int y_spawn = 0;
                if (GetTicksGone() == 0)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        rand = new Random();
                        x_spawn = rand.Next(600, 5700);
                        y_spawn = rand.Next(600, 9400);
                        Tile tile1 = m_tileMap.GetTile(((x_spawn) / 300), ((y_spawn) / 300), true);

                        if ((tile1.Data.BlocksMovement && !tile1.IsDestructed()))
                        {
                            Console.WriteLine("NotOk");
                        }
                        else
                        {
                            Item Money = new Item(DataTables.Get(18).GetData<ItemData>("Speed"));
                            Money.SetPosition(x_spawn, y_spawn, 0);
                            m_gameObjectManager.AddGameObject(Money);
                        }
                    }
                }
                if (GetTicksGone() % 500 == 0)
                {
                    for (int x = 0; x < 2; x++)
                    {
                        rand = new Random();
                        x_spawn = rand.Next(600, 5700);
                        y_spawn = rand.Next(600, 9400);
                        Tile tile1 = m_tileMap.GetTile(((x_spawn) / 300), ((y_spawn) / 300), true);

                        if ((tile1.Data.BlocksMovement && !tile1.IsDestructed()))
                        {
                            Console.WriteLine("NotOk");
                        }
                        else
                        {
                            Item Money = new Item(DataTables.Get(18).GetData<ItemData>("Speed"));
                            Money.SetPosition(x_spawn, y_spawn, 0);
                            m_gameObjectManager.AddGameObject(Money);
                        }
                    }
                }
            }

        }
        private int EventModifierLastTick;
        private void TickModifier()
        {
            if (HasEventModifier(3) && GetTicksGone() > m_ticksSinceLastExplosion + ExplosionInterval)
            {
                AreaEffect area = new AreaEffect(DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("ExplosionSpawn"));
                if (area != null)
                {
                    area.SetPosition(new Random().Next(300, GetTileMap().LogicWidth - 300), new Random().Next(300, GetTileMap().LogicHeight - 300), 0);
                    area.SetIndex(16);
                    area.m_ticksElapsed = GetTicksGone();
                    m_gameObjectManager.AddGameObject(area);
                    area.Trigger();
                }

                m_ticksSinceLastExplosion = GetTicksGone();
            }
            if (HasEventModifier(18) && GetTicksGone() > (EventModifierLastTick + 5 * 20))
            {
                Item eventItem = new Item(DataTables.GetItemByName("EventCollectToken"));
                if (eventItem != null)
                {
                    eventItem.SetPosition(new Random().Next(300, GetTileMap().LogicWidth - 300), new Random().Next(300, GetTileMap().LogicHeight - 300), 0);
                    eventItem.SetIndex(-16);
                    m_gameObjectManager.AddGameObject(eventItem);
                }

                EventModifierLastTick = GetTicksGone();
            }
        }
        public void SetRandomPosition(GameObject obj)
        {
            if (obj != null)
                obj.SetPosition(new Random().Next(300, GetTileMap().LogicWidth - 300), new Random().Next(300, GetTileMap().LogicHeight - 300), 0);
        }
        private int lastSpawnTick = -1;
        private void TickInvasion()
        {
            int _firstSpawnTick = 150;
            int _otherSpawnTick = 235;

            int ticksGone = GetTicksGone();

            if (ticksGone == _firstSpawnTick ||
               (ticksGone > _firstSpawnTick && (ticksGone - lastSpawnTick) >= _otherSpawnTick))
            {
                AreaEffectData invasionData = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("InvasionSpawnIncoming");
                if (invasionData != null && m_tileMap.InvasionBotSpawn != null && m_tileMap.InvasionBotSpawn.Count > 0)
                {
                    AreaEffect area = new AreaEffect(invasionData);
                    int randomIndex = new Random().Next(m_tileMap.InvasionBotSpawn.Count);
                    area.SetPosition(m_tileMap.InvasionBotSpawn[randomIndex].X + 150, m_tileMap.InvasionBotSpawn[randomIndex].Y + 150, 0);
                    area.SetIndex(-16);
                    area.m_ticksElapsed = GetTicksGone();
                    m_gameObjectManager.AddGameObject(area);
                    area.Trigger();
                    lastSpawnTick = ticksGone;
                }
            }

            if (ticksGone == 1500)
            {
                AreaEffectData invasionData = DataTables.Get(DataType.AreaEffect).GetData<AreaEffectData>("InvasionSpawnIncoming");
                if (invasionData != null && m_tileMap.InvasionBotSpawn != null && m_tileMap.InvasionBotSpawn.Count > 0)
                {
                    AreaEffect area = new AreaEffect(invasionData);
                    int randomIndex = new Random().Next(m_tileMap.InvasionBotSpawn.Count);
                    area.SetPosition(m_tileMap.InvasionBotSpawn[randomIndex].X + 150, m_tileMap.InvasionBotSpawn[randomIndex].Y + 150, 0);
                    area.SetIndex(-16);
                    area.m_ticksElapsed = GetTicksGone();
                    m_gameObjectManager.AddGameObject(area);
                    area.Trigger();
                    lastSpawnTick = ticksGone;
                }
            }
        }
        public void TickPortals(BattlePlayer owner)
        {
            if (owner == null) return;
            
            Item Portal1 = owner.Portal1;
            Item Portal2 = owner.Portal2;

            if (Portal1 != null)
            {
                if (Portal1.SpawnTick < 10) Portal1.PortalClosed = true;
                else Portal1.PortalClosed = false;
            }

            if (Portal2 != null)
            {
                if (Portal2.SpawnTick < 10) Portal2.PortalClosed = false;
                else Portal2.PortalClosed = false;
            }

            if (Portal1 == null || Portal2 == null) return;

            var plrsInv1 = m_gameObjectManager.GetGameObjects()
            .Where(g => g != null && g.GetObjectType() == 0)
            .Cast<Character>()
            .Where(c => c != null && c.CharacterData.IsHero())
            .Where(c => Portal1.GetPosition().GetDistance(c.GetPosition()) <= 300)
            .ToList();

            var plrsInv2 = m_gameObjectManager.GetGameObjects()
            .Where(g => g != null && g.GetObjectType() == 0)
            .Cast<Character>()
            .Where(c => c != null && c.CharacterData.IsHero())
            .Where(c => Portal2.GetPosition().GetDistance(c.GetPosition()) <= 300)
            .ToList();


            if (plrsInv1.Count > 0 || plrsInv2.Count > 0)
            {
                Portal1.PortalArrive = true;
                Portal2.PortalArrive = true;

            }
            else
            {
                Portal1.PortalArrive = false;
                Portal2.PortalArrive = false;
            }

            if (plrsInv1.Count > 0 || plrsInv2.Count > 0)
            {
                foreach (GameObject gameObject in GetGameObjectManager().GetGameObjects())
                {
                    if (gameObject == null) continue;
                    if (gameObject.GetObjectType() != 0) continue;
                    Character c = (Character)gameObject;
                    if (!c.CharacterData.IsHero()) continue;
                    if (Portal1.GetPosition().GetDistance(c.GetPosition()) <= 300)
                    {
                        c.TriggerBlink(Portal2.GetX(), Portal2.GetY(), DataTables.GetAreaEffectByName("DoorManPortalArrive"), DataTables.GetAreaEffectByName("DoorManPortalArrive"), 0, 0);
                    }

                    if (Portal2.GetPosition().GetDistance(c.GetPosition()) <= 300)
                    {
                        c.TriggerBlink(Portal1.GetX(), Portal1.GetY(), DataTables.GetAreaEffectByName("DoorManPortalArrive"), DataTables.GetAreaEffectByName("DoorManPortalArrive"), 0, 0);
                    }
                }
            }
        }

        private void TickPetrols()
        {
            var currentPetrols = new List<Petrol>(m_gameObjectManager.Petrols);
            var toRemove = new List<Petrol>();

            foreach (var petrol in currentPetrols)
            {
                if (petrol == null) continue;
                if (!m_gameObjectManager.Petrols.Contains(petrol))
                    continue;

                if (petrol.Tick(m_gameObjectManager))
                {
                    toRemove.Add(petrol);
                    foreach (var other in currentPetrols)
                    {
                        if (other != null && other.TeamIndex == petrol.TeamIndex
                            && LogicMath.Abs(other.X - petrol.X) <= 1
                            && LogicMath.Abs(other.Y - petrol.Y) <= 1)
                        {
                            other.Ignite(m_gameObjectManager, petrol.Damage);
                        }
                    }
                }
            }
            foreach (var p in toRemove)
            {
                m_gameObjectManager.Petrols.Remove(p);
            }
        }

                public void UpdatePlayerStatus()
        {
            foreach (BattlePlayer player in m_players)
            {
                if (player == null) continue;
                
                if (m_gameObjectManager.GetGameObjectByID(player.OwnObjectId) == null && player.IsAlive && m_gameObjectManager.AddObjects.ToList().Find(obj => obj != null && obj.GetGlobalID() == player.OwnObjectId) == null)
                {
                    player.IsAlive = false;
                    player.DeathTick = GetTicksGone();
                }

                Character character = GetCharacterSafe(player.OwnObjectId);
                player.UpdateOverCharge();
                if (character != null)
                {
                    Accessory v9 = player.Accessory;
                    if (v9 != null) v9.UpdateAccessory(character);

                    // ЗАМОРОЗКА ХОДЬБЫ БОТОВ (Они остаются смертными)
                    //if (player.IsBot() == 1)
                    //{
                        //character.MoveTo(0, character.GetX(), character.GetY(), 0, 0, 0, 0);
                    //}
                }
            }
        }

        public List<(int TeamIndex, int Score)> GetSortedTeams()
        {
            List<(int TeamIndex, int Score)> teams = new List<(int, int)>();

            for (int i = 0; i <= 10; i++)
            {
                teams.Add((i, GetTeamScore(i)));
            }

            return teams.OrderByDescending(t => t.Score).ToList();
        }
        public int GetTeamRank(int teamId)
        {
            var sortedTeams = GetSortedTeams();

            for (int i = 0; i < sortedTeams.Count; i++)
            {
                if (sortedTeams[i].TeamIndex == teamId)
                    return i + 1;
            }

            return -1;
        }

        public int GetTopTeamIndex()
        {
            var teams = new List<(int TeamIndex, int Score)>();
            for (int i = 1; i <= 10; i++)
            {
                teams.Add((i, GetTeamScore(i)));
            }

            var topTeam = teams.OrderByDescending(t => t.Score).FirstOrDefault();
            return topTeam.TeamIndex;
        }
        public Character GetTopByCharacter()
        {
            foreach (var character in m_gameObjectManager.GetCharacters())
            {
                if (character == null) continue;
                if (character.GetPlayer() != null && character.GetPlayer().TeamIndex == GetTopTeamIndex())
                {
                    return character;
                }
            }

            return null;
        }

        public void AddScoreTo1Zone() { Zone1Score++; }
        public void AddScoreTo2Zone() { Zone2Score++; }

        private void SendVisionUpdateToPlayers()
        {
            int tick = GetTicksGone();
            int viewersCount = m_spectators.Count;
            bool isBrawlTV = IsBrawlTV;
            var players = _playersSnapshot;
            int playerCount = players.Count;

            if (playerCount <= 6)
            {
                for (int i = 0; i < playerCount; i++)
                {
                    var player = players[i];
                    if (player == null) continue;
                    var listener = player.GameListener;
                    if (listener == null) continue;

                    BitStream visionBitStream = new BitStream(512);
                    m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);

                    listener.SendMessage(new VisionUpdateMessage
                    {
                        Tick = tick,
                        HandledInputs = player.LastHandledInput,
                        Viewers = viewersCount,
                        VisionBitStream = visionBitStream,
                        IsBrawlTV = isBrawlTV
                    });
                }
            }
            else
            {
                Parallel.For(0, playerCount, i =>
                {
                    var player = players[i];
                    if (player == null) return;
                    var listener = player.GameListener;
                    if (listener == null) return;

                    BitStream visionBitStream = new BitStream(512);
                    m_gameObjectManager.Encode(visionBitStream, m_tileMap, player.OwnObjectId, player.PlayerIndex, player.TeamIndex);

                    listener.SendMessage(new VisionUpdateMessage
                    {
                        Tick = tick,
                        HandledInputs = player.LastHandledInput,
                        Viewers = viewersCount,
                        VisionBitStream = visionBitStream,
                        IsBrawlTV = isBrawlTV
                    });
                });
            }

            if (viewersCount == 0) return;

            BitStream spectateStream = new BitStream(512);
            m_gameObjectManager.Encode(spectateStream, m_tileMap, 0, -1, 99);

            var spectatorMessage = new VisionUpdateMessage
            {
                Tick = tick,
                Viewers = viewersCount,
                VisionBitStream = spectateStream,
                IsBrawlTV = isBrawlTV
            };

            foreach (var gameListener in m_spectators.Values)
            {
                if (gameListener != null)
                {
                    spectatorMessage.HandledInputs = gameListener.HandledInputs;
                    gameListener.SendTCPMessage(spectatorMessage);
                }
            }
        }

        public void ErrorEnd(int id = 43)
        {
            Parallel.ForEach(m_players, player =>
            {
                if (player == null) return;
                if (player.GameListener == null)
                    return;

                try
                {
                    ServerErrorMessage error = new ServerErrorMessage();
                    error.id = id;
                    player.GameListener.SendTCPMessage(error);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"idk but battleserver cannot send error lol bcs {ex}");
                }
            });
        }
        
        public Character GetCharacterSafe(int globalId)
        {
            try
            {
                GameObject obj = m_gameObjectManager.GetGameObjectByID(globalId);
                if (obj == null) return null;
                if (obj.GetObjectType() != 0) return null;
                return (Character)obj;
            }
            catch
            {
                return null;
            }
        }
        
        public bool IsCarryableValid()
        {
            if (Carryable == null) return false;
            if (!Carryable.IsAlive()) return false;
            if (!IsInPlayArea(Carryable.GetX(), Carryable.GetY())) return false;
            return true;
        }
        
        public long Id { get; set; }
    }
}
