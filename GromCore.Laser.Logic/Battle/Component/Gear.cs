namespace GromCore.Laser.Logic.Battle.Structures
{
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using GromCore.Laser.Logic.Battle.Component;
    using GromCore.Laser.Logic.Battle.Level;
    using GromCore.Laser.Logic.Battle.Objects;
    using GromCore.Laser.Logic.Battle.Structures;
    using GromCore.Laser.Logic.Data;
    using GromCore.Laser.Logic.Data.Helper;

    using GromCore.Laser.Logic.Util;
    using GromCore.Laser.Titan.DataStream;
    using GromCore.Laser.Titan.Debug;
    using GromCore.Laser.Titan.Math;

    public class Gear
    {
        public GearData GearData;
        public bool IsActive;
        public int ShieldAmount;
        public Gear(GearData gearData)
        {
            GearData=gearData;
            IsActive = false;
            if (GearData != null && GearData.LogicType == 4 ) ShieldAmount = 600;

        }
        public void Tick(Character character)
        {
            IsActive = false;
            switch (GearData.LogicType)
            {
                case 0:
                    if (character.IsInBush())
                    {
                        character.GiveSpeedFasterBuff(character.CharacterData.Speed/5, 2, false);
                        IsActive = true;
                    }
                    break;
                case 2:
                    if (character.GetHitpointPercentage() < 50)
                    {
                        character.GiveDamageBuff(GearData.ModifierValue, 2);
                        //DamageBoost damageBoost = new DamageBoost(1, GearData.DamageBoost);
                        //character.AddBuff(damageBoost);
                        IsActive = true;
                    }
                    break;
                case 4:
                    if (character.GetHitpointPercentage() == 100)
                    {
                        int TicksSinceFullHealth = character.TicksGone - character.m_lastSelfHealTick;
                        if (TicksSinceFullHealth > 0 && TicksSinceFullHealth % 10 == 0)
                        {
                            ShieldAmount = LogicMath.Min(GearData.ModifierValue, ShieldAmount + GearData.ModifierValue / 20);
                        }
                    }
                    break;
            }
        }
        public void OnBoost()
        {
            //if(GearData.LogicType==)
            IsActive = true;
        }
        public void Encode(BitStream stream)
        {
            stream.WriteBoolean(IsActive);
            if (GearData.LogicType == 4) stream.WritePositiveIntMax1023(ShieldAmount);
        }
    }
}
