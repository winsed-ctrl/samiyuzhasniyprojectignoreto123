namespace GromCore.Laser.Logic.Battle.Objects
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Helper;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;

    public class PROJECTGROM_ONTOP
    {
        public ConcurrentQueue<GameObject> AddObjects { get; }
        public ConcurrentQueue<GameObject> RemoveObjects { get; }
        public List<GameObject> GameObjects => _gameObjects;
        public List<Petrol> Petrols { get; } = new();
        public int Ended = -1;

        private readonly BattleMode _battle;
        private BattleMode Battle;
        private int _objectCounterInternal;
        public int ObjectCounter
        {
            get => _objectCounterInternal;
            set => _objectCounterInternal = value;
        }

        private readonly List<GameObject> _gameObjects = new(512);
        private Character[] _cachedCharacters = Array.Empty<Character>();
        private Projectile[] _cachedProjectiles = Array.Empty<Projectile>();
        private Item[] _cachedItems = Array.Empty<Item>();
        private AreaEffect[] _cachedAreaEffects = Array.Empty<AreaEffect>();
        private bool _typeCachesDirty = true;

        public PROJECTGROM_ONTOP(BattleMode battle)
        {
            _battle = battle;
            Battle = battle;
            AddObjects = new ConcurrentQueue<GameObject>();
            RemoveObjects = new ConcurrentQueue<GameObject>();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureTypeCaches()
        {
            if (!_typeCachesDirty) return;
            RebuildTypeCaches();
        }

        private void RebuildTypeCaches()
        {
            var chars = new List<Character>();
            var projs = new List<Projectile>();
            var items = new List<Item>();
            var areas = new List<AreaEffect>();

            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                {
                    switch (obj)
                    {
                        case Character c: chars.Add(c); break;
                        case Projectile p: projs.Add(p); break;
                        case Item i: items.Add(i); break;
                        case AreaEffect a: areas.Add(a); break;
                    }
                }
            }

            _cachedCharacters = chars.ToArray();
            _cachedProjectiles = projs.ToArray();
            _cachedItems = items.ToArray();
            _cachedAreaEffects = areas.ToArray();
            _typeCachesDirty = false;
        }

        public GameObject[] GetGameObjects()
        {
            lock (_gameObjects) return _gameObjects.ToArray();
        }

        public Projectile[] GetProjectiles()
        {
            EnsureTypeCaches();
            return _cachedProjectiles;
        }

        public Character[] GetCharacters()
        {
            EnsureTypeCaches();
            var list = new List<Character>(_cachedCharacters);
            foreach (var obj in AddObjects)
                if (obj is Character c) list.Add(c);
            return list.ToArray();
        }

        public Item[] GetItems()
        {
            EnsureTypeCaches();
            var list = new List<Item>(_cachedItems);
            foreach (var obj in AddObjects)
                if (obj is Item i) list.Add(i);
            return list.ToArray();
        }

        public AreaEffect[] GetAreaEffects()
        {
            EnsureTypeCaches();
            var list = new List<AreaEffect>(_cachedAreaEffects);
            foreach (var obj in AddObjects)
                if (obj is AreaEffect a) list.Add(a);
            return list.ToArray();
        }

        public Projectile[] GetSpecificProjectiles(GameObject gameObject)
        {
            if (gameObject == null) return Array.Empty<Projectile>();
            var pos = gameObject.GetPosition();
            var result = new List<Projectile>();
            EnsureTypeCaches();
            foreach (var proj in _cachedProjectiles)
            {
                if (proj.GetIndex() / 16 != gameObject.GetIndex() / 16 &&
                    proj.GetPosition().GetDistance(pos) <= 600)
                {
                    result.Add(proj);
                }
            }
            return result.ToArray();
        }

        public BattleMode GetBattle() => _battle;

        public void PreTick()
        {
            lock (_gameObjects)
            {
                while (RemoveObjects.TryDequeue(out var obj))
                {
                    _gameObjects.Remove(obj);
                }
                for (int i = _gameObjects.Count - 1; i >= 0; i--)
                {
                    var obj = _gameObjects[i];
                    if (obj == null)
                    {
                        _gameObjects.RemoveAt(i);
                        continue;
                    }

                    if (obj.ShouldDestruct())
                    {
                        obj.OnDestruct();
                        _gameObjects.RemoveAt(i);
                        _typeCachesDirty = true;
                    }
                    else
                    {
                        obj.ResetEventsOnTick();
                    }
                }
                while (AddObjects.TryDequeue(out var obj))
                {
                    if (obj != null)
                    {
                        _gameObjects.Add(obj);
                        _typeCachesDirty = true;
                    }
                }
            }
        }

        public void Tick()
        {
            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                {
                    if (obj != null) obj.Tick();
                }
            }
        }

        public void AddGameObject(GameObject gameObject)
        {
            if (gameObject != null)
            {
                gameObject.AttachGameObjectManager(this, GlobalId.CreateGlobalId(gameObject.GetObjectType(), _objectCounterInternal++));
                AddObjects.Enqueue(gameObject);
            }
        }

        public void RemoveGameObject(GameObject gameObject)
        {
            if (gameObject != null)
                RemoveObjects.Enqueue(gameObject);
           
        }

        public GameObject GetGameObjectByID(int globalId)
        {
            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                    if (obj != null && obj.GetGlobalID() == globalId)
                        return obj;
            }

            foreach (var obj in AddObjects)
                if (obj != null && obj.GetGlobalID() == globalId)
                    return obj;

            return null;
        }

        public GameObject[] GetGameObjectsByID(int globalId)
        {
            var result = new List<GameObject>();
            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                    if (obj != null && obj.GetGlobalID() == globalId)
                        result.Add(obj);
            }
            foreach (var obj in AddObjects)
                if (obj != null && obj.GetGlobalID() == globalId)
                    result.Add(obj);
            return [.. result];
        }

        public List<GameObject> GetGameObjectByCloneOfObjectGlobalId(int globalId)
        {
            var result = new List<GameObject>();
            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                    if (obj != null && obj.GetCloneOfObjectGlobalId() == globalId)
                        result.Add(obj);
            }
            return result;
        }

        public Character FindObject(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            EnsureTypeCaches();
            foreach (var character in _cachedCharacters)
                if (character.CharacterData?.Name == name)
                    return character;
            return null;
        }

        public Character GetCharacterByPlayer(BattlePlayer player)
        {
            if (player == null) return null;
            EnsureTypeCaches();
            foreach (var character in _cachedCharacters)
                if (character.GetPlayer() == player)
                    return character;
            return null;
        }

        public List<GameObject> GetVisibleGameObjects(int teamIndex, int ownObjectGlobalId)
        {
            var objects = new List<GameObject>();
            var character = GetGameObjectByID(ownObjectGlobalId) as Character;
            bool isInRealm = character?.IsInRealm ?? false;
            int ticks = _battle?.GetTicksGone() ?? 0;

            lock (_gameObjects)
            {
                foreach (var obj in _gameObjects)
                {
                    if (obj == null) continue;
                    if (obj.IsInRealm != isInRealm) continue;
                    if (obj.GetFadeCounter() > 0 || (obj.GetIndex() / 16 == teamIndex && _battle?.StoryMode == null))
                        objects.Add(obj);
                }
            }

            foreach (var obj in AddObjects)
            {
                if (obj == null) continue;
                if (obj.IsInRealm != isInRealm) continue;
                if (obj.GetFadeCounter() > 0 || obj.GetIndex() / 16 == teamIndex)
                    objects.Add(obj);
            }

            if (character != null)
            {
                int charX = character.GetX();
                int charY = character.GetY();

                objects.RemoveAll(obj =>
                {
                    if (obj == null || obj.GetGlobalID() == character.GetGlobalID()) return false;
                    if (obj != null && Battle.GetGameModeVariation() == 9 && obj.GetIndex() / 16 == teamIndex && obj.GetObjectType() == 0) return false;
                    if (character.GetPlayer() == null || character.GetPlayer().Bot > 0) return false;
                    if (obj.GetObjectType() == 2) return false;

                    int dx = Math.Abs(obj.GetX() - charX);
                    int dy = Math.Abs(obj.GetY() - charY);
                    return ticks > 100 && (dx > 5000 || dy > 5000);
                });

                objects.RemoveAll(obj =>
                {
                    if (obj is not Item item) return false;
                    if (!item.IsVisible && item.GetTeamIndex() != teamIndex &&
                        (item.ItemData?.Name == "Mine" || item.ItemData?.ParentItemForSkin == "Mine"))
                        return true;
                    return false;
                });
            }

            return objects;
        }

        private int[] CalculateRounds(int own)
        {
            int[] p = { _battle.RoundWins[0], _battle.RoundWins[1], _battle.RoundWins[2] };
            if (own == 1)
            {
                if (_battle.RoundWins[0] == 0) p[0] = 1;
                if (_battle.RoundWins[1] == 0) p[0] = 1;
                if (_battle.RoundWins[2] == 0) p[0] = 1;
            }
            return p;
        }
        public void Encode(BitStream bitStream, TileMap tileMap, int ownObjectGlobalId, int playerIndex, int teamIndex)
        {
            BattlePlayer[] players = Battle.GetPlayers();
            List<GameObject> visibleGameObjects = GetVisibleGameObjects(teamIndex, ownObjectGlobalId);
            bool ShouldDestroyGhostUlti = false;
            GameObject obj = GetGameObjectByID(ownObjectGlobalId);
            BattlePlayer plr = null;
            if (obj != null)
            {
                Character character = (Character)obj;
                if (character != null && character.ChargeUp > 0 && character.CharacterData.Name == "Ghost") ShouldDestroyGhostUlti = true;
                else ShouldDestroyGhostUlti = false;
                if (character.GetPlayer() != null) plr = character.GetPlayer();
            }

            int GameModeVariation = Battle.GetGameModeVariation();
            bitStream.WritePositiveIntMax2097151(ownObjectGlobalId);
            bitStream.WritePositiveIntMax2097151(0);
            if (bitStream.WriteBoolean(false))
            {
                int unsingnedInt = 1;
                bitStream.WritePositiveIntMax63(unsingnedInt);
                if (unsingnedInt > 0)
                    bitStream.WritePositiveIntMax63(15);
            }

            if (bitStream.WriteBoolean(GetBattle().HasEventModifier(18)))
                bitStream.WritePositiveIntMax255(plr != null ? LogicMath.Clamp(plr.EventCollectTokens, 0, 255) : 0);

            switch (GameModeVariation)
            {
                case 0:
                case 33:
                    bitStream.WritePositiveVIntMax65535(Battle.GetGemGrabCountdown());
                    break;
                case 17:
                    bitStream.WritePositiveIntMax7(1);
                    bitStream.WritePositiveIntMax127(GetBattle().GetZone1Score());
                    bitStream.WritePositiveIntMax127(GetBattle().GetZone2Score());
                    break;
                case 19:
                    bitStream.WritePositiveIntMax7(GetBattle().GetTeam1King());
                    bitStream.WritePositiveIntMax7(GetBattle().GetTeam2King());
                    break;
                case 20:
                case 24:
                case 35: // it from v54 btw
                    bitStream.WritePositiveIntMax4095(!GetBattle().nokbool ? GetBattle().RoundTicks : 4095); // round ticks
                    bitStream.WriteBoolean(GetBattle().nokbool);
                    bitStream.WriteBoolean(GetBattle().nokbool);

                    bitStream.WriteBoolean(GetBattle().nokbool);
                    bitStream.WriteBoolean(GetBattle().nokbool);
                    bitStream.WriteBoolean(GetBattle().nokbool);

                    bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[0]);
                    bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[1]);
                    bitStream.WritePositiveIntMax255(CalculateRounds(teamIndex)[2]);
                    break;

            }

            bitStream.WriteBoolean(GetBattle().IsPlayFieldMirroredForPlayer(teamIndex));
            bitStream.WriteIntMax15(Battle.m_winnerTeam == -1 ? -1 : Battle.m_winnerTeam == teamIndex ? 0 : Battle.m_winnerTeam == -1 ? 2 : 1);


            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);
            bitStream.WriteBoolean(false);//????

            if (tileMap.Width < 22)
            {
                bitStream.WritePositiveIntMax31(0);
                bitStream.WritePositiveIntMax63(0);
                bitStream.WritePositiveIntMax31(LogicMath.Clamp(tileMap.Width, 0, tileMap.Width - 1));
            }
            else
            {
                bitStream.WritePositiveIntMax63(0);
                bitStream.WritePositiveIntMax63(0);
                bitStream.WritePositiveIntMax63(LogicMath.Clamp(tileMap.Width, 0, tileMap.Width - 1));
            }
            bitStream.WritePositiveIntMax63(LogicMath.Clamp(tileMap.Height, 0, tileMap.Height - 1));

            for (int i = 0; i < tileMap.Width; i++)
            {
                for (int j = 0; j < tileMap.Height; j++)
                {
                    var tile = tileMap.GetTile(i, j, true, true);
                    if (tile.Data.RespawnSeconds > 0 || tile.Data.Name.StartsWith("Indestructible") || tile.Code == 'J' || tile.Data.IsDestructible || tile.Data.RestoreAfterDynamicOverlap)
                    {
                        bitStream.WriteBoolean(ShouldDestroyGhostUlti || (tile.IsDestructed() && tileMap.GetTile(i, j, true).IsDestructed()));
                        if (GameModeUtil.HasBigMap(GameModeVariation) && false) bitStream.WriteBoolean(false);
                    }
                }
            }


            bitStream.WritePositiveVIntMax65535OftenZero(tileMap.NewTiles.Count);
            {
                for (int i = 0; i < tileMap.NewTiles.Count; i++)
                {
                    bitStream.WritePositiveIntMax4095(tileMap.NewTiles[i].TileX + tileMap.Width * tileMap.NewTiles[i].TileY);
                    bitStream.WritePositiveIntMax15(tileMap.NewTiles[i].Data.DynamicCode);
                    if (false) bitStream.WriteBoolean(false);
                    int idk = 0;
                    if (idk == 0xC)
                        bitStream.WritePositiveIntMax3(0);
                }
            }
            bitStream.WritePositiveVIntMax65535OftenZero(0);
            //bitStream.WritePositiveIntMax255(200);
            //bitStream.WritePositiveIntMax255(200);
            //bitStream.WritePositiveIntMax255(200);
            //bitStream.WritePositiveIntMax255(200);

            bitStream.WritePositiveVIntMax65535OftenZero(Petrols.Count);
            if (Petrols.Count >= 1)
            {
                for (int i = 0; i < Petrols.Count; i++)
                {
                    bitStream.WritePositiveIntMax63(Petrols[i].X);
                    bitStream.WritePositiveIntMax63(Petrols[i].Y);
                    bitStream.WriteIntMax15(Petrols[i].TeamIndex);
                    bitStream.WriteIntMax15(Petrols[i].PlayerIndex);
                }
            }
            for (int i = 0; i < players.Length; i++)
            {
                if (i == playerIndex)
                {
                    
                    bitStream.WritePositiveIntMax4095(players[i].GetUltiCharge());
                    bitStream.WritePositiveIntMax4095(players[i].GetOverCharge()); 
                    bitStream.WriteBoolean(false);
                    bitStream.WriteBoolean(false);
                    bitStream.WritePositiveVIntMax255OftenZero(players[i].Kills);
                    if (bitStream.WriteBoolean(players[i].Vibrate))
                    {
                        bitStream.WritePositiveIntMax3(1);
                        bitStream.WritePositiveIntMax16383(GetBattle().GetTicksGone());
                    }
                    if (players[i].Vibrate) players[i].Vibrate = false;
                }
                bitStream.WritePositiveIntMax3(0);
                if (bitStream.WriteBoolean(false)) ByteStreamHelper.WriteDataReference(bitStream, ownObjectGlobalId);
                if (players[i].Accessory != null)
                {
                    players[i].Accessory.Encode(bitStream, i == playerIndex);
                }
                bitStream.WriteBoolean(GameModeVariation == 20 && obj!=null); 

                bitStream.WriteBoolean(players[i].HasUlti());
                if (bitStream.WriteBoolean(players[i].TemporaryTeamOverride != -1 && players[i].ControlCharacter != null))
                {
                    bitStream.WriteIntMax15(players[i].TemporaryTeamOverride);
                    bitStream.WriteIntMax15(players[i].ControlCharacter.GetIndex());
                }

                if (bitStream.WriteBoolean(players[i].ControlledCharacter != null))
                {
                    bitStream.WritePositiveIntMax31(GlobalId.GetClassId(players[i].ControlledCharacter.GetGlobalID()));
                    bitStream.WritePositiveVIntMax65535(GlobalId.GetInstanceId(players[i].ControlledCharacter.GetGlobalID()));
                }
                switch (GameModeVariation)
                {
                    case 6:
                        bitStream.WritePositiveIntMax15(0);
                        break;
                    case 3:
                        bitStream.WriteBoolean(players[i].HasStar);
                        break;
                }

                if (players[i].Accessory != null)
                {
                    bitStream.WritePositiveVIntMax255OftenZero(players[i].Accessory.IsActive == true ? 1 : 0);
                }
                else
                {
                    bitStream.WritePositiveVIntMax255OftenZero(0);
                }

                bitStream.WriteBoolean(!players[i].CanUsePin(GetBattle().GetTicksGone())); // cant use pin
                bitStream.WriteBoolean(players[i].IsUsingSpray(GetBattle().GetTicksGone())); // cant use spray

                bitStream.WriteBoolean(players[i].IsUsingPin(GetBattle().GetTicksGone()));
                if (players[i].IsUsingPin(GetBattle().GetTicksGone()))
                {
                    bitStream.WriteIntMax7(players[i].PinIndex);
                    bitStream.WritePositiveIntMax16383(GetBattle().GetTicksGone() - 1);
                }

            }
            bitStream.WritePositiveVIntMax65535OftenZero(0);
            bitStream.WritePositiveVIntMax255OftenZero(0); // ïîâîðîò êóäà òî
            if (((2129332653 >> GameModeVariation) & 1) != 0)
                bitStream.WritePositiveVIntMax65535OftenZero(0);

            if (GameModeUtil.PONOSXMOPSER(GameModeVariation))
            {
                bitStream.WriteBoolean(!(GetBattle().GetGoalTeam() != -1) || GetBattle()._SetStopTicks);
                bitStream.WriteIntMax1(GetBattle().GetGoalTeam() != -1 ? GetBattle().GetGoalTeam() : 0);
            }

            switch (GameModeVariation)
            {
                case 6:
                    bitStream.WritePositiveIntMax15(Battle.GetPlayersAliveCountForBattleRoyale());
                    break;
                case 16:

                    bitStream.WritePositiveIntMax8191(0);
                    bitStream.WritePositiveIntMax8191(2);

                    break;
                case 9:
                    bitStream.WritePositiveIntMax7(Battle.GetAliveTeams());
                    break;
                default:
                    if (GameModeVariation == 999 || GameModeVariation == 8 ||
                        GameModeUtil.HasTwoBases(GameModeVariation))
                    {

                        int hp1 = 0;
                        int hp2 = 0;
                        foreach (Character character in GetCharacters())
                        {
                            if (!character.CharacterData.IsBase()) continue;
                            if (!character.IsAlive()) continue;
                            if (character.GetIndex() / 16 == 0) hp1 = character.GetHitpointPercentage();
                            else hp2 = character.GetHitpointPercentage();
                        }
                        bitStream.WritePositiveIntMax127(hp1);
                        if (GameModeVariation == 11) // siege
                        {
                            bitStream.WritePositiveIntMax127(hp2);
                            bitStream.WritePositiveIntMax255(0);
                            bitStream.WritePositiveIntMax255(0);
                            bitStream.WritePositiveIntMax31(0);
                            bitStream.WritePositiveIntMax31(0);
                            bitStream.WritePositiveIntMax63(0); // HIDWORD(v403)
                            bitStream.WritePositiveIntMax63(0);
                            bitStream.WriteBoolean(false); // LogicBattleModeServer::isRoboWarsNoRobos
                        }
                        else
                        {
                            bitStream.WritePositiveIntMax127(GameModeVariation == 8 ? GetBattle().CoopRound : hp2); // robot âàâå if 8 
                            bitStream.WriteBoolean(GameModeVariation == 8 ? GetBattle().CoopRound is 5 or 10 or 15 or 20 : true); // LogicBattleModeServer::isRoboWarsNoRobos
                        }

                    }
                    else
                    {
                        switch (GameModeVariation)
                        {
                            case 19:
                                int hp1 = 0;
                                int hp2 = 0;
                                foreach (Character character in GetCharacters())
                                {
                                    if (!character.CharacterData.IsHero()) continue;
                                    if (!character.IsAlive()) continue;
                                    if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetBattle().GetTeam1King()) hp1 = character.GetHitpointPercentage();
                                    if (character.GetPlayer() != null && character.GetPlayer().PlayerIndex == GetBattle().GetTeam2King()) hp2 = character.GetHitpointPercentage();
                                }
                                bitStream.WritePositiveIntMax127(hp1);
                                bitStream.WritePositiveIntMax127(hp2);
                                break;
                            case 26:
                            case 21:
                                bitStream.WritePositiveIntMax127(127);
                                bitStream.WritePositiveIntMax127(127);
                                break;
                            case 27:
                                bitStream.WritePositiveIntMax7(0);
                                break;
                            case 14:
                                bitStream.WritePositiveIntMax127(2);
                                bitStream.WritePositiveIntMax16383(2);
                                break;
                            case 13:
                                GameObject player = GetGameObjectByID(ownObjectGlobalId);
                                int dmg = 0;
                                if (player != null) dmg = player.GetPlayer().GetDps();
                                bitStream.WritePositiveIntMax131071(dmg);
                                break;
                            case 10:
                                bitStream.WritePositiveIntMax127(2);
                                break;
                            case 18:
                            case 7:
                                bitStream.WritePositiveIntMax127(0);
                                if (GameModeVariation == 18) bitStream.WritePositiveIntMax127(0);
                                break;
                            case 29: // dont work, idk
                                if (bitStream.WriteBoolean(false))
                                {
                                    bitStream.WritePositiveIntMax262143(9999);
                                    bitStream.WritePositiveIntMax262143(9999);
                                    bitStream.WritePositiveIntMax127(2);
                                }
                                bitStream.WritePositiveIntMax31(0);
                                break;
                            case 28:
                                if (bitStream.WriteBoolean(true))
                                {
                                    Character top = GetBattle().GetTopByCharacter();
                                    int index, x, y;
                                    if (top != null) { index = GetBattle().GetTopByCharacter().GetIndex(); x = GetBattle().GetTopByCharacter().GetX(); y = GetBattle().GetTopByCharacter().GetY(); }
                                    else { index = 0; x = 0; y = 0; }
                                    bitStream.WriteIntMax15(index); // maybe BestPlayerIndex???
                                    bitStream.WritePositiveIntMax262143(LogicMath.Clamp(x, 0, 262143)); // BestPlayer X
                                    bitStream.WritePositiveIntMax262143(LogicMath.Clamp(y, 0, 262143)); // BestPlayer Y
                                }
                                break;
                        }
                    }

                    break;

            }




            for (int i = 0; i < players.Length; i++)
            {

                bitStream.WriteBoolean(true);
                switch (GameModeVariation)
                {
                    case 14:
                        bitStream.WritePositiveIntMax524287(1);
                        break;
                    case 15:
                        bitStream.WritePositiveIntMax134217727(players[i].AllStarsEarned);
                        break;
                    case 28:
                        bitStream.WritePositiveIntMax15(players[i].GetScore());
                        bitStream.WritePositiveIntMax4095(0);
                        break;
                    default:
                        bitStream.WritePositiveVIntMax255(players[i].GetScore());
                        break;
                }


                if (bitStream.WriteBoolean(players[i].KillList.Count > 0))
                {

                    bitStream.WritePositiveIntMax15(players[i].KillList.Count);
                    if (players[i].KillList.Count > 0)
                    {
                        for (int j = 0; j < players[i].KillList.Count; j++)
                        {
                            bitStream.WritePositiveIntMax15(players[i].KillList[j].PlayerIndex);
                            bitStream.WriteIntMax7(GameModeVariation == 3 ? players[i].KillList[j].BountyStarsEarned : 0);
                        }
                    }

                }
            }
            bitStream.WritePositiveVIntMax65535(visibleGameObjects.Count);

            foreach (GameObject gameObject in visibleGameObjects)
            {
                ByteStreamHelper.WriteDataReference(bitStream, gameObject.GetDataId());
                //Debugger.Print(gameObject.GetDataId() + "");
            }

            foreach (GameObject gameObject in visibleGameObjects)
            {
                bitStream.WritePositiveVIntMax65535(GlobalId.GetInstanceId(gameObject.GetGlobalID())); // 0x2381b4
            }
            bitStream.WriteBoolean(false);
            foreach (GameObject gameObject in visibleGameObjects)
            {
                gameObject.Encode(bitStream, gameObject.GetGlobalID() == ownObjectGlobalId, playerIndex + teamIndex * 16, teamIndex);
                //bitStream.WritePositiveVIntMax65535OftenZero(0);

                //Debugger.Print("e:"+gameObject.GetDataId());
            }
            if (GameModeVariation == 7)
            {
                if (bitStream.WriteBoolean(true))
                {
                    bitStream.WritePositiveIntMax32767(10000);
                    bitStream.WritePositiveIntMax65535(10000);
                }
            }
        }
    }
}
