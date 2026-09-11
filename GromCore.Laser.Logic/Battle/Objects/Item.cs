namespace GromCore.Laser.Logic.Battle.Objects
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using Masuda.Net.Models;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Math;

    public class Item : GameObject
    {


        public ItemData ItemData;

        public int SpawningTick;
        public int SpawnTick; // tick when item spawned
        public int PickUpTimerDefault;
        public int PickUpTimer;
        public int PickUpStartX;
        public int PickUpStartY;
        public int PickUpZ;
        public int PickUpStartZ;
        public Character Picker;
        public bool PickedUp;
        public int MoveTimer;
        public int MoveTimerDefault;
        public int TargetX;
        public int TargetY;
        public int SpawnedTick;
        public int Damage;
        public Character Owner;
        public SprayData SprayData;
        public Character AttachedCharacter;
        public int AttachTicks;
        public int SilenceDamage;
        public int LastAttachDamageTicks = 20;

        public bool IsHealing = false;
        public bool IsVisible;
        public int LastTick;
        public int IsTriggeredMine;
        public int TriggerTicks;
        public bool TriggerTicksSet;

        public bool PortalArrive;
        public bool PortalClosed;
        public int PortalTicks;
        public Item TwinPortal;

        private int _jumpTime;
        private int _jumpDuration = 10;
        private int _jumpHeight = 500; 
        private bool _isJumping = false;
        private int _startX, _startY, _startZ;
        private int _targetX, _targetY, _targetZ;
        private int TeleportType;
        private int StateTick;
        public int TeleportIternalId { get; private set; }
        private int TramplineState;
        private int TramplineStateTick;
        private int LastTramplineStateTick;
        private bool LastTramplineStateTickHasSet;
        private bool TramplineStartSet;
        public Item(ItemData itemData) : base(itemData)
        {
            ItemData = itemData;
            SpawningTick = 1;
            PickUpTimerDefault = 250;
            MoveTimer = 750;
            MoveTimerDefault = 750;
        }

        public override void Tick()
        {
            SpawnTick++;

            if(ItemData.Name == "BattleRoyaleBuff" || ItemData.Name == "Point" || ItemData.Name == "Scrap")
            {
                TileMap tileMap = GetBattle().GetTileMap();
                if(tileMap.GetTile(GetX(), GetY()) != null && tileMap.GetTile(GetX(),GetY()).Data.BlocksMovement)
                {
                    LogicVector2 vec = new LogicVector2(GetX(), GetY());
                    GamePlayUtil.GetClosestPassableSpot(1, GetX(), GetY(), 10, tileMap, vec, false);
                    SetPosition(vec.X, vec.Y, 0);
                }
            }

            if (_isJumping)
            {
                _jumpTime++;
                float progress = (float)_jumpTime / _jumpDuration;

                if (progress >= 1f)
                {
                    SetPosition(_targetX, _targetY, _targetZ);
                    _isJumping = false;
                    return;
                }

                SetPosition(_startX + (int)((_targetX - _startX) * progress), _startY + (int)((_targetY - _startY) * progress), _startZ + (int)(_jumpHeight * 4 * progress * (1 - progress)));
            }
            if (ItemData.Name.StartsWith("SpringBoard"))
            {
                int Angle = GamePlayUtil.GetNearestAngle(GamePlayUtil.GetAngleFromString(ItemData.Name));
                int SVO = 3500;
                if (ItemData.Name.EndsWith("_Gale")) SVO = 1850;
                var playersInZone = PROJECTGROM_ONTOP.GetGameObjects()
                    .Where(g => g.GetObjectType() == 0)
                    .Cast<Character>()
                    .Where(c => c.CharacterData.IsHero())
                    .Where(c => Position.GetDistance(c.GetPosition()) <= 300)
                    .ToList();
                if (playersInZone.Count > 0) TramplineStartSet = true;
                if (TramplineStartSet)
                {
                    TramplineStateTick++;
                    TramplineState = 1;
                    if (TramplineStateTick > 40)
                    {
                        if(TramplineStateTick == 41)
                        {
                            foreach (GameObject gameObject in PROJECTGROM_ONTOP.GetGameObjects())
                            {
                                if (gameObject.GetObjectType() != 0) continue;
                                Character c = (Character)gameObject;
                                if (!c.CharacterData.IsHero()) continue;
                                if (Position.GetDistance(c.GetPosition()) > 500) continue;
                                
                                c.TriggerCharge(c.GetX() - LogicMath.GetRotatedX(SVO, 0, Angle), 
                                    c.GetY() - LogicMath.GetRotatedY(SVO, 0, Angle), 2750, 6, 0,
                                    0, false);
                                //c.TriggerPushback(c.GetX() - LogicMath.GetRotatedX(500, 0, c.MoveAngle), c.GetY() - LogicMath.GetRotatedY(500, 0, c.MoveAngle), 500, true, 0, false, true, false, true, true, false, false, 0);
                            }
                        }
                        TramplineState = 2;
                    }
                    if (TramplineStateTick > 60)
                    {
                        TramplineStateTick = 0;
                        TramplineState = 0;
                        TramplineStartSet = false;
                    }
                }
            }
            if (ItemData.Name == "Healing")
            {
                var playersInZone = PROJECTGROM_ONTOP.GetGameObjects()
                    .Where(g => g.GetObjectType() == 0)
                    .Cast<Character>()
                    .Where(c => c.CharacterData.IsHero())
                    .Where(c => Position.GetDistance(c.GetPosition()) <= 300)
                    .ToList();

                foreach (GameObject gameObject in PROJECTGROM_ONTOP.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    Character c = (Character)gameObject;
                    if (!c.CharacterData.IsHero()) continue;
                    if (Position.GetDistance(c.GetPosition()) > 300) continue;
                    if (GetBattle().GetTicksGone() >= LastTick + 25) c.Heal(0, 500, true, null);
                    else return;
                    if (IsHealing == false) IsHealing = true;
                    LastTick = GetBattle().GetTicksGone();
                }
                if (playersInZone.Count <= 0) IsHealing = false;
            }
            if(ItemData.Name == "WindWall")
            {
                foreach (GameObject gameObject in PROJECTGROM_ONTOP.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    Character c = (Character)gameObject;
                    if (!c.CharacterData.IsHero()) continue;
                    if (Position.GetDistance(c.GetPosition()) > 500) continue;
                    if (c.GetIndex() / 16 == GetIndex() / 16) continue;
                    c.TriggerPushback(c.GetX() + LogicMath.GetRotatedX(500, 0, c.MoveAngle),c.GetY() + LogicMath.GetRotatedY(500, 0, c.MoveAngle), 50, true, 0, false, true, false, true, true, false, false, 0);
                }
                if (SpawnTick > 100) PROJECTGROM_ONTOP.RemoveGameObject(this);
            }

            if (SpawnTick > 22 && ItemData.Name.StartsWith("StickyBomb"))
            {
                SpawnTick--;
                PROJECTGROM_ONTOP.RemoveGameObject(this);
                if (Owner == null) return;
                AreaEffect v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(DataTables.GetAreaEffectByName(ItemData.TriggerAreaEffect).Name ?? "StickyBombClusterExplosion"));
                v224.SetSource(Owner, 1);
                v224.SetPosition(GetX(), GetY(), 0);
                v224.SetIndex(Owner.GetIndex());
                v224.Damage = Damage;
                PROJECTGROM_ONTOP.AddGameObject(v224);
                v224.Trigger();
            }

            if (ItemData.Name.StartsWith("StickyBomb") && AttachedCharacter != null)
            {
                SetPosition(AttachedCharacter.GetX(), AttachedCharacter.GetY(), AttachedCharacter.GetZ());
            }

            if (SpawnTick > 22 && ItemData.Name.EndsWith("BombUlti"))
            {
                SpawnTick--;
                PROJECTGROM_ONTOP.RemoveGameObject(this);
                if (Owner == null) return;
                AreaEffect v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(DataTables.GetAreaEffectByName(ItemData.TriggerAreaEffect).Name ?? "StickyBombUltiExplosion"));
                v224.SetSource(Owner, 1);
                v224.SetPosition(GetX(), GetY(), 0);
                v224.SetIndex(Owner.GetIndex());
                PROJECTGROM_ONTOP.AddGameObject(v224);
                v224.Trigger();
            }

            if (ItemData.Name.EndsWith("BombUlti") && AttachedCharacter != null)
            {
                SetPosition(AttachedCharacter.GetX(), AttachedCharacter.GetY(), AttachedCharacter.GetZ());
            }

            if (SpawnTick > 22 && ItemData.Name.StartsWith("StickyBomb"))
            {
                SpawnTick--;
                PROJECTGROM_ONTOP.RemoveGameObject(this);
                if (Owner == null) return;
                AreaEffect v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(DataTables.GetAreaEffectByName(ItemData.TriggerAreaEffect).Name ?? "StickyBombClusterExplosion"));
                v224.SetSource(Owner, 1);
                v224.SetPosition(GetX(), GetY(), 0);
                v224.SetIndex(Owner.GetIndex());
                PROJECTGROM_ONTOP.AddGameObject(v224);
                v224.Trigger();
            }

            if (ItemData.Name == "SilencerHugger" || ItemData.ParentItemForSkin == "SilencerHugger")
            {
                if (AttachedCharacter != null && AttachedCharacter.Silence) SetPosition(AttachedCharacter.GetX(), AttachedCharacter.GetY(), AttachedCharacter.GetZ());
                if (AttachedCharacter != null && AttachedCharacter.SilenceTick < TicksGone) PROJECTGROM_ONTOP.RemoveGameObject(this);
                if (AttachedCharacter != null && !AttachedCharacter.IsAlive()) PROJECTGROM_ONTOP.RemoveGameObject(this);
            }
            if (SpawnTick > 20 && ItemData.Name.StartsWith("Piper"))
            {
                SpawnTick--;
                PROJECTGROM_ONTOP.RemoveGameObject(this);
                if (Owner == null) return;
                AreaEffect v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(DataTables.GetAreaEffectByName(ItemData.TriggerAreaEffect).Name ?? "Piper_def_ulti"));
                v224.SetPosition(GetX(), GetY(), 0);
                v224.SetIndex(Owner.GetIndex());
                v224.Damage = 900;
                v224.NormalDMG = 0;
                PROJECTGROM_ONTOP.AddGameObject(v224);

                v224.Trigger();
            }

            if (ItemData.Name == "Portal")
            {
                GetBattle().TickPortals(Owner.GetPlayer());
                

                /*
             foreach (GameObject gameObject in PROJECTGROM_ONTOP.GetGameObjects())
             {
                 if (gameObject.GetObjectType() != 0) continue;
                 Character c = (Character)gameObject;
                 if (!c.CharacterData.IsHero()) continue;
                 if (Position.GetDistance(c.GetPosition()) > 300)
                 {
                     c.PortalTicks = 0;
                 }
                 if (GetBattle().GetTicksGone() >= c.PortalTicks + (20 * 4) && LastTick + 30 <= TicksGone)
                 {
                     c.TriggerBlink(Owner.GetPlayer().Portal2.GetX(), Owner.GetPlayer().Portal2.GetY(), DataTables.GetAreaEffectByName("DoorManPortalArrive"), DataTables.GetAreaEffectByName("DoorManPortalArrive"), 0, 0);
                     LastTick = TicksGone;
                 }
                 else break; ;
                 c.PortalTicks = 0;
             }
*/

                // 2 * 20

            }
            if (ItemData.Name == "Teleport1" ||
    ItemData.Name == "Teleport2" ||
    ItemData.Name == "Teleport3" ||
    ItemData.Name == "Teleport4")
            {
                if(TeleportIternalId == 0) TeleportIternalId = ItemData.Value;
                if (StateTick > 0) StateTick--;
                if (TeleportType == 2 && StateTick <= 1) TeleportType = 0;
                Item pair = TileMap.GetTeleportPairLocation(TeleportIternalId, GetGameObjectManager(), GetGlobalID());
                if (pair == null) return;
                foreach (GameObject gameObject in PROJECTGROM_ONTOP.GetGameObjects())
                {
                    if (gameObject.GetObjectType() != 0) continue;
                    Character c = (Character)gameObject;
                    if (!c.CharacterData.IsHero()) continue;
                    if (Position.GetDistance(c.GetPosition()) > 300 && TeleportType == 0)
                    {
                        TeleportType = 1;
                        StateTick = 2 * 20;
                    }
                    if (TeleportType == 1 && StateTick <= 1 && Position.GetDistance(c.GetPosition()) < 300)
                    {
                        c.TriggerBlink(pair.GetX(), pair.GetY(), DataTables.GetAreaEffectByName("TeleportTileArrive"), DataTables.GetAreaEffectByName("TeleportTileLeave"), 0, 0);
                        c.TeleportCooldownTicks = 7 * 20;
                        TeleportType = 2;
                        StateTick = 7 * 20;
                    }
                    else break;
                    //c.PortalTicks = 0;
                }
            }

            if (ItemData.Name == "Mine" || ItemData.Name == "ElectroTrap" || ItemData.ParentItemForSkin == "Mine")
            {
                if (SpawnTick >= 20 && IsTriggeredMine <= 0) IsVisible = false;
                else IsVisible = true;
                if (SpawnTick >= 20)
                {
                    var playersInZone = PROJECTGROM_ONTOP.GetGameObjects()
                    .Where(g => g.GetObjectType() == 0)
                    .Cast<Character>()
                    .Where(c => c.CharacterData.IsHero())
                    .Where(c => Position.GetDistance(c.GetPosition()) <= 600)
                    .Where(c => c.GetIndex() / 16 != GetIndex() / 16)
                    .ToList();

                    if (playersInZone.Count >= 1 && !TriggerTicksSet)
                    {
                        IsTriggeredMine = 100;
                        TriggerTicks = TicksGone + 22;
                        TriggerTicksSet = true;
                        IsVisible = true;
                    }
                    if (IsTriggeredMine > 0 && TriggerTicks <= TicksGone)
                    {
                        SpawnTick--;
                        PROJECTGROM_ONTOP.RemoveGameObject(this);
                        AreaEffect v224 = GameObjectFactory.CreateGameObjectByData(DataTables.GetAreaEffectByName(DataTables.GetAreaEffectByName(ItemData.TriggerAreaEffect).Name ?? "Bo_def_ulti2_area"));
                        v224.SetPosition(GetX(), GetY(), 0);
                        v224.SetIndex(GetIndex());
                        v224.SetSource(Owner,1);
                        v224.Damage = 900;
                        v224.NormalDMG = 0;
                        PROJECTGROM_ONTOP.AddGameObject(v224);
                        v224.Trigger();
                    }
                }
            }

            if (ItemData.Name == "ButtonHold" || ItemData.Name == "ButtonPress")
            {
                foreach (Character character in PROJECTGROM_ONTOP.GetCharacters())
                {
                    if (character.GetPosition().GetDistance(Position) <= 150)
                    {
                        GetBattle().StoryMode.DoorTest = true;
                        break;
                    }
                }
            }

            if ((ItemData.Name == "DoorHorizontal" || ItemData.Name == "DoorVertical")&& !GetBattle().StoryMode.DoorTest)
            {
                foreach (Character character in PROJECTGROM_ONTOP.GetCharacters())
                {
                    if (character.GetPosition().GetDistance(Position) <= 150&&character==GetBattle().StoryMode.PlayerCharacter)
                    {
                        GetBattle().StoryMode.N("???,???????????\n??????<cFF0000>??�?</c>?????,\n??????????");
                        break;
                    }
                }
            }


            if(ItemData.Name == "BattleRoyaleBuff")
            {
                SetPosition(GetX(), GetY(), GetZ());
                PickUpStartX = GetX();
                PickUpStartY = GetY();
                PickUpStartZ = 1000 * GetZ();
                int angle = GetBattle().GetRandomInt(360);
                TargetX = GetX() + LogicMath.GetRotatedX(600, 0, angle);
                TargetY = GetY() + LogicMath.GetRotatedY(600, 0, angle);
            }
            if (ItemData.Name == "OrbSpawner")
            {
                if (SpawningTick == 1) SpawningTick = 40;
                if (TicksGone < SpawningTick) goto LABEL_235;
                Item v269 = GameObjectFactory.CreateGameObjectByData(DataTables.GetItemByName("Point"));
                v269.SetPosition(GetX(), GetY(), GetZ());
                v269.PickUpStartX = GetX();
                v269.PickUpStartY = GetY();
                v269.PickUpStartZ = 1000 * GetZ();
                int angle = GetBattle().GetRandomInt(360);
                v269.TargetX = GetX() + LogicMath.GetRotatedX(600, 0, angle);
                v269.TargetY = GetY() + LogicMath.GetRotatedY(600, 0, angle);
                PROJECTGROM_ONTOP.AddGameObject(v269);
                SpawningTick = TicksGone + 140;
                SpawnedTick = TicksGone;
            }
            if (ItemData.Name == "TrainSpawner")
            {
               // if (SpawningTick == 1) SpawningTick = 40;
                //if (TicksGone < SpawningTick) goto LABEL_235;
                // SpawnTrain();
                //PROJECTGROM_ONTOP.AddGameObject(v269);
                //SpawningTick = TicksGone + 140;
            }
            if (PickedUp)
            {
                if (PickUpTimer < 1) goto LABEL_235;
                if (Picker != null)
                {
                    if (!Picker.IsAlive())
                    {
                        PickedUp = false;
                        goto LABEL_235;
                    }
                }
                PickUpTimer -= 50;
                int v196 = LogicMath.Min(1000, 1000 - 1000 * PickUpTimer / PickUpTimerDefault);
                int v200 = PickUpStartZ + (v196 - 1000) * v196 * (-1) + (PickUpZ - PickUpStartZ) * v196 / 1000;
                SetPosition((Picker.GetX() * v196 + PickUpStartX * (1000 - v196)) / 1000,
                        (Picker.GetY() * v196 + PickUpStartY * (1000 - v196)) / 1000,
                        v200 / 1000);
                if (PickUpTimer <= 0)
                {
                    if (Picker.IsAlive())
                    {
                        Picker.ApplyItem(this);
                    }
                }
                goto LABEL_235;
            }
            MoveTimer -= 50;
            if (MoveTimer < 0) MoveTimer = 0;
            if (TargetX == 0) goto LABEL_263;
            int v187 = LogicMath.Min(1000, 1000 - 1000 * MoveTimer / MoveTimerDefault);
            if (ItemData.Name == "Point")
            {
                if (v187 > 700)
                {
                    int v186 = 700;
                    int v189 = 300;
                    int v206 = v187 - v186;
                    int v211 = TargetX - PickUpStartX;
                    int v212 = TargetY - PickUpStartY;
                    int v213 = (TargetX - PickUpStartX) * v186;
                    int v214 = (PickUpStartX * v189 + TargetX * v186);
                    int v215 = (PickUpZ + v206 * (-6) * (v206 - v189));
                    int v216 = (PickUpStartY * v189 + TargetY * v186);
                    int v217 = ((v214 / 1000) * (v189 - v206) + (v213 / 1000 + PickUpStartX + v211 * v189 / 2000) * v206) / v189;
                    int v218 = ((v216 / 1000) * (v189 - v206) + (v212 * v186 / 1000 + PickUpStartY + v212 * v189 / 2000) * v206) / v189;
                    int v219 = v215 / 1000;
                    SetPosition(v217, v218, LogicMath.Clamp(v219, 0, 3000));
                }
                else
                {
                    int v186 = 700;
                    int v206 = v187 - 700;
                    int v220 = (PickUpStartY * (1000 - v187) + TargetY * v187);
                    int v221 = (PickUpStartZ + (-6) * v187 * v206 + (PickUpZ - PickUpStartZ) * v187 / v186);
                    int v218 = v220 / 1000;
                    int v217 = (PickUpStartX * (1000 - v187) + TargetX * v187) / 1000;
                    int v219 = v221 / 1000;
                    SetPosition(v217, v218, LogicMath.Clamp(v219, 0, 3000));
                }
            }

        LABEL_263:
            ;
        LABEL_235:
            ;
        }

        public void SetCharacter(Character character)
        {
            Owner = character;
        }
        public void PickUp(Character character)
        {
            PickedUp = true;
            Picker = character;

            PickUpTimer = PickUpTimerDefault;
            PickUpStartX = GetX();
            PickUpStartY = GetY();
            PickUpZ = 20000;
        }

        public void SetAngle(int angle)
        {
            ;
        }

        public void DisableAppearAnimation()
        {
            ;
        }
        public override bool ShouldDestruct()
        {
            return PickedUp && PickUpTimer <= 0;
        }

        public bool CanBePickedUp(Character a1)
        {
            if (ItemData.CanBePickedUp && !PickedUp)
            {
                switch (ItemData.Name)
                {
                    case "BattleRoyaleBuff":
                    case "Money":
                    case "Speed":
                    case "Scrap":
                    case "Point":
                    case "EventCollectToken":
                        return true;
                    case "DamageAndSpeed":
                        return true;
                    case "RuffsBuff":
                        if (a1.GetIndex() / 16 == GetIndex() / 16&& !a1.HasBuff(13)) return true;
                        return false;
                    case "Health":
                        if (
                            a1.GetIndex() / 16 == GetIndex() / 16&& 
                        a1.CharacterData.Name == "Bulletstorm") return true;
                        return false;
                    case "SoulCollectorSoul":
                        return a1.GetIndex() / 16 == GetIndex() / 16 && a1.GetHitpointPercentage() < 100;
                    case "WeaponThrowerWeapon":
                        return a1.GetIndex() == GetIndex();
                }

                switch (ItemData.ParentItemForSkin)
                {
                    case "WeaponThrowerWeapon":
                        return a1.GetIndex() == GetIndex();
                }

            }
            return false;
        }

        public override void Encode(BitStream bitStream, bool isOwnObject, int OwnObjectIndex, int visionTeam)
        {
            base.Encode(bitStream, isOwnObject, visionTeam);
            {
                bitStream.WritePositiveInt(10, 4);
                if (ItemData.Name == "ConductorSign")
                {
                    bitStream.WriteBoolean(true); // ������ ��� �������� � �� 
                    bitStream.WriteBoolean(true); // ������ ��� �������� ������ ���� ( ������ ���������� ����� ����� ��)
                    bitStream.WriteBoolean(false); // ��� ����� �� ������
                    bitStream.WriteBoolean(false); // ����� �������� ����� ��

                    bitStream.WriteBoolean(false); // �� � ����� �� ����� ��� ���


                    bitStream.WritePositiveIntMax511(SpawnTick); // ���� � ���������� �����
                }
                if (ItemData.Name == "SilencerHugger" || ItemData.ParentItemForSkin == "SilencerHugger")
                {
                    bitStream.WriteBoolean(false);
                }
                if (ItemData.Name == "SilencerHuggerMine")
                {
                    bitStream.WritePositiveIntMax4095(0);
                }
                if (ItemData.Name == "Healing")
                {
                    bitStream.WriteBoolean(IsHealing);
                }
                if(ItemData.Name == "ClusterMine")
                {
                    if (bitStream.WriteBoolean(true))
                    {
                        bitStream.WritePositiveIntMax3(0);
                        bitStream.WritePositiveIntMax63(0);
                    }
                }
                if (ItemData.Name == "Portal")
                {
                    if (bitStream.WriteBoolean(true))
                    {
                        bitStream.WriteBoolean(PortalArrive); // when brwaler on item
                        bitStream.WriteBoolean(PortalClosed); // ������ ��� ������(��� ���������)

                        bitStream.WritePositiveIntMax63(0);
                    }
                }

                if (ItemData.Name == "StickyBomb" || ItemData.Name == "StickyBombUlti" || ItemData.ParentItemForSkin == "StickyBomb" || ItemData.ParentItemForSkin == "StickyBombUlti")
                {
                    bitStream.WritePositiveIntMax127(0);
                }
                if (ItemData.Name == "Mine" || ItemData.Name == "ElectroTrap" || ItemData.ParentItemForSkin == "Mine")
                {
                    bitStream.WriteBoolean(IsVisible); // seen to others
                    bitStream.WritePositiveIntMax2047(IsTriggeredMine); // ticks 
                }

                if (ItemData.Name.StartsWith("SpringBoard"))
                {
                    if (bitStream.WriteBoolean(true)) // is rabotaet vro
                    {
                        bitStream.WritePositiveIntMax3(TramplineState);
                        bitStream.WritePositiveIntMax63(TramplineStateTick);
                        // ����� ���� ��������� ��� �� ����
                    }
                }
                
                if(ItemData.GetInstanceId() == 54)
                {
                    if(bitStream.WriteBoolean(true))
                    {
                        bitStream.WritePositiveIntMax3(0);
                        bitStream.WritePositiveIntMax63(62);
                    }
                }
   
                if (ItemData.Name == "Teleport1" ||
                    ItemData.Name == "Teleport2" ||
                    ItemData.Name == "Teleport3" ||
                    ItemData.Name == "Teleport4")
                {
                    if (bitStream.WriteBoolean(true)) // is rabotaet vro
                    {
                        bitStream.WritePositiveIntMax3(TeleportType); // TeleportType
                        bitStream.WritePositiveIntMax63(StateTick); //Ticks
                    }

                }
                if (ItemData.Name == "OrbSpawner" || ItemData.Name == "TrainSpawner")
                {
                    bitStream.WritePositiveIntMax16383(SpawningTick); // Spawn Train
                    bitStream.WritePositiveIntMax16383(SpawnedTick);
                    if(ItemData.Name == "TrainSpawner") bitStream.WritePositiveIntMax16383(0); // OpenExit
                }

                if (ItemData.Name.StartsWith("Piper"))
                {
                    bitStream.WritePositiveIntMax4095(4000);
                }

                if (ItemData.Name == "Spray")
                {
                    bitStream.WriteIntMax511(SprayData.GetInstanceId()); // spray reference
                    if (bitStream.WriteBoolean(true))
                    {
                        bitStream.WritePositiveIntMax3(1);
                        bitStream.WritePositiveIntMax63(0);
                    }
                }
                if (ItemData.Name == "DoorHorizontal" || ItemData.Name == "DoorVertical")
                {
                    bitStream.WriteBoolean(GetBattle().StoryMode.DoorTest);
                    bitStream.WritePositiveIntMax127(1);
                }
                if (ItemData.Name == "ButtonHold" || ItemData.Name == "ButtonPress")
                {
                    bitStream.WritePositiveIntMax127(1);
                }
            }
        }
        public void SetFromToPosition(int X, int Y, int Z, int TX, int TY, int TZ)
        {
            SetPosition(X, Y, Z);
            PickUpStartX = X;
            PickUpStartY = Y;
            PickUpStartZ = Z;
            TargetX = TX;
            TargetY = TY;
            PickUpZ = TZ;
        }

        public void StartJumpEffect(int fromX, int fromY, int fromZ, int toX, int toY, int toZ)
        {
            _startX = fromX;
            _startY = fromY;
            _startZ = fromZ;
            _targetX = toX;
            _targetY = toY;
            _targetZ = toZ;
            _jumpTime = 0;
            _isJumping = true;
            SetPosition(fromX, fromY, fromZ);
        }
        public override int GetObjectType()
        {
            return 3;
        }
    }
}
