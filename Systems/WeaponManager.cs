using ProjectM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Entities;
using static Unsheathed.Services.Systems.WeaponManager.WeaponStats;

namespace Unsheathed.Services.Systems
{
    internal class WeaponManager
    {


        public static class WeaponStats
        {
            public enum WeaponStatType : int
            {
                MaxHealth = 0,
                MovementSpeed = 1,
                PrimaryAttackSpeed = 2,
                AbilityAttackSpeed = 33,
                PhysicalLifeLeech = 3,
                SpellLifeLeech = 4,
                PrimaryLifeLeech = 5,
                PhysicalPower = 6,
                SpellPower = 7,
                PhysicalCritChance = 8,
                PhysicalCritDamage = 9,
                SpellCritChance = 10,
                SpellCritDamage = 11
            }
            public enum WeaponType
            {
                Item_Weapon_Axe_Legendary_T06,
                Item_Weapon_Claws_Legendary_T06,
                Item_Weapon_Crossbow_Legendary_T06,
                Item_Weapon_Daggers_Legendary_T06,
                Item_Weapon_GreatSword_Legendary_T06,
                Item_Weapon_Longbow_Legendary_T06,
                Item_Weapon_Mace_Legendary_T06,
                Item_Weapon_Pistols_Legendary_T06,
                Item_Weapon_Reaper_Legendary_T06,
                Item_Weapon_Slashers_Legendary_T06,
                Item_Weapon_Spear_Legendary_T06,
                Item_Weapon_Sword_Legendary_T06,
                Item_Weapon_TwinBlades_Legendary_T06,
                Item_Weapon_Whip_Legendary_T06

            }
            public static IReadOnlyDictionary<WeaponStatType, string> WeaponStatFormats => _weaponStatFormats;
            static readonly Dictionary<WeaponStatType, string> _weaponStatFormats = new()
        {
            { WeaponStatType.MaxHealth, "integer" },
            { WeaponStatType.MovementSpeed, "decimal" },
            { WeaponStatType.PrimaryAttackSpeed, "percentage" },
            { WeaponStatType.AbilityAttackSpeed, "percentage" },
            { WeaponStatType.PhysicalLifeLeech, "percentage" },
            { WeaponStatType.SpellLifeLeech, "percentage" },
            { WeaponStatType.PrimaryLifeLeech, "percentage" },
            { WeaponStatType.PhysicalPower, "integer" },
            { WeaponStatType.SpellPower, "integer" },
            { WeaponStatType.PhysicalCritChance, "percentage" },
            { WeaponStatType.PhysicalCritDamage, "percentage" },
            { WeaponStatType.SpellCritChance, "percentage" },
            { WeaponStatType.SpellCritDamage, "percentage" }
        };
            public static IReadOnlyDictionary<WeaponStatType, UnitStatType> WeaponStatTypes => _weaponStatTypes;
            static readonly Dictionary<WeaponStatType, UnitStatType> _weaponStatTypes = new()
        {
            { WeaponStatType.MaxHealth, UnitStatType.MaxHealth },
            { WeaponStatType.MovementSpeed, UnitStatType.MovementSpeed },
            { WeaponStatType.PrimaryAttackSpeed, UnitStatType.PrimaryAttackSpeed },
             { WeaponStatType.AbilityAttackSpeed, UnitStatType.AbilityAttackSpeed },
            { WeaponStatType.PhysicalLifeLeech, UnitStatType.PhysicalLifeLeech },
            { WeaponStatType.SpellLifeLeech, UnitStatType.SpellLifeLeech },
            { WeaponStatType.PrimaryLifeLeech, UnitStatType.PrimaryLifeLeech },
            { WeaponStatType.PhysicalPower, UnitStatType.PhysicalPower },
            { WeaponStatType.SpellPower, UnitStatType.SpellPower },
            { WeaponStatType.PhysicalCritChance, UnitStatType.PhysicalCriticalStrikeChance },
            { WeaponStatType.PhysicalCritDamage, UnitStatType.PhysicalCriticalStrikeDamage },
            { WeaponStatType.SpellCritChance, UnitStatType.SpellCriticalStrikeChance },
            { WeaponStatType.SpellCritDamage, UnitStatType.SpellCriticalStrikeDamage },
        };
          




            /*public enum UnsheathedStatType : int
            {
                PhysicalPower = 0,
                ResourcePower = 1,
                SiegePower = 2,
                ResourceYield = 3,
                MaxHealth = 4,
                MovementSpeed = 5,
                CooldownRecoveryRate = 7,
                PhysicalResistance = 8,
                FireResistance = 9,
                HolyResistance = 10,
                SilverResistance = 11,
                SunChargeTime = 12,
                SunResistance = 19,
                GarlicResistance = 20,
                Vision = 22,
                SpellResistance = 23,
                Radial_SpellResistance = 24,
                SpellPower = 25,
                PassiveHealthRegen = 26,
                PhysicalLifeLeech = 27,
                SpellLifeLeech = 28,
                PhysicalCriticalStrikeChance = 29,
                PhysicalCriticalStrikeDamage = 30,
                SpellCriticalStrikeChance = 31,
                SpellCriticalStrikeDamage = 32,
                AbilityAttackSpeed = 33,
                DamageVsUndeads = 38,
                DamageVsHumans = 39,
                DamageVsDemons = 40,
                DamageVsMechanical = 41,
                DamageVsBeasts = 42,
                DamageVsCastleObjects = 43,
                DamageVsVampires = 44,
                ResistVsUndeads = 45,
                ResistVsHumans = 46,
                ResistVsDemons = 47,
                ResistVsMechanical = 48,
                ResistVsBeasts = 49,
                ResistVsCastleObjects = 50,
                ResistVsVampires = 51,
                DamageVsWood = 52,
                DamageVsMineral = 53,
                DamageVsVegetation = 54,
                DamageVsLightArmor = 55,
                DamageVsVBloods = 56,
                DamageVsMagic = 57,
                ReducedResourceDurabilityLoss = 58,
                PrimaryAttackSpeed = 59,
                ImmuneToHazards = 60,
                PrimaryLifeLeech = 61,
                HealthRecovery = 62,
                PrimaryCooldownModifier = 63,
                FallGravity = 64,
                PvPResilience = 65,
                BloodDrain = 66,
                BonusPhysicalPower = 67,
                BonusSpellPower = 68,
                CCReduction = 69,
                SpellCooldownRecoveryRate = 70,
                WeaponCooldownRecoveryRate = 71,
                UltimateCooldownRecoveryRate = 72,
                MinionDamage = 73,
                DamageReduction = 74,
                HealingReceived = 75,
                IncreasedShieldEfficiency = 76,
                BloodEfficiency = 77,
                InventorySlots = 78,
                SilverCoinResistance = 79,
                TravelCooldownRecoveryRate = 80,
                ReducedBloodDrain = 81,
                BonusMaxHealth = 82,
                BonusMovementSpeed = 83,
                BonusShapeshiftMovementSpeed = 84,
                BonusMountMovementSpeed = 85,
                UltimateEfficiency = 86,
                SpellFreeCast = 88,
                WeaponFreeCast = 89,
                WeaponSkillPower = 90,
                FeedCooldownRecoveryRate = 91,
                BloodMendHealEfficiency = 92,
                DemountProtection = 93,
                BloodDrainMultiplier = 94,
                CorruptionDamageReduction = 95
            */






        }
    }

}


       












    

