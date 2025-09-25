using BepInEx.Configuration;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using static Unity.Physics.ConvexHull;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;




namespace Unsheathed.Services;
internal static class ConfigService
{
    static readonly Lazy<string> _languageLocalization = new(() => GetConfigValue<string>("LanguageLocalization"));
    public static string LanguageLocalization => _languageLocalization.Value;



    static readonly Lazy<bool> _spiritArsenal = new(() => GetConfigValue<bool>("SpiritArsenal"));
    public static bool SpiritArsenal => _spiritArsenal.Value;

    static readonly Lazy<bool> _weaponsSystem = new(() => GetConfigValue<bool>("WeaponsSystem"));
   public static bool WeaponsSystem => true;

    // was: public static bool Debug_CastTweaks = false;
    public static bool Debug_CastTweaks { get; private set; } = false;

    public static bool Debug_Buffs => GetConfigValue<bool>("Debug_Buffs");




    // Accessors
    static readonly Lazy<string> _ctoFishingPole = new(() => GetConfigValue<string>("CastTimeOverrides_FishingPole"));
    static readonly Lazy<string> _ctoDagger = new(() => GetConfigValue<string>("CastTimeOverrides_Dagger"));
    static readonly Lazy<string> _ctoReaper = new(() => GetConfigValue<string>("CastTimeOverrides_Reaper"));
    static readonly Lazy<string> _ctoMace = new(() => GetConfigValue<string>("CastTimeOverrides_Mace"));
    static readonly Lazy<string> _ctoSword = new(() => GetConfigValue<string>("CastTimeOverrides_Sword"));
    static readonly Lazy<string> _ctoGreatsword = new(() => GetConfigValue<string>("CastTimeOverrides_Greatsword"));
    static readonly Lazy<string> _ctoSpear = new(() => GetConfigValue<string>("CastTimeOverrides_Spear"));
    static readonly Lazy<string> _ctoTwinBlades = new(() => GetConfigValue<string>("CastTimeOverrides_TwinBlades"));
    static readonly Lazy<string> _ctoSlashers = new(() => GetConfigValue<string>("CastTimeOverrides_Slashers"));
    static readonly Lazy<string> _ctoWhip = new(() => GetConfigValue<string>("CastTimeOverrides_Whip"));
    static readonly Lazy<string> _ctoPistols = new(() => GetConfigValue<string>("CastTimeOverrides_Pistols"));
    static readonly Lazy<string> _ctoCrossbow = new(() => GetConfigValue<string>("CastTimeOverrides_Crossbow"));
    static readonly Lazy<string> _ctoBow = new(() => GetConfigValue<string>("CastTimeOverrides_Bow"));
    static readonly Lazy<string> _ctoClaws = new(() => GetConfigValue<string>("CastTimeOverrides_Claws"));
    static readonly Lazy<string> _ctoAxe = new(() => GetConfigValue<string>("CastTimeOverrides_Axe"));

    public static string CastTimeOverrides_FishingPole => _ctoFishingPole.Value;
    public static string CastTimeOverrides_Dagger => _ctoDagger.Value;
    public static string CastTimeOverrides_Reaper => _ctoReaper.Value;
    public static string CastTimeOverrides_Mace => _ctoMace.Value;
    public static string CastTimeOverrides_Sword => _ctoSword.Value;
    public static string CastTimeOverrides_Greatsword => _ctoGreatsword.Value;
    public static string CastTimeOverrides_Spear => _ctoSpear.Value;
    public static string CastTimeOverrides_TwinBlades => _ctoTwinBlades.Value;
    public static string CastTimeOverrides_Slashers => _ctoSlashers.Value;
    public static string CastTimeOverrides_Whip => _ctoWhip.Value;
    public static string CastTimeOverrides_Pistols => _ctoPistols.Value;
    public static string CastTimeOverrides_Crossbow => _ctoCrossbow.Value;
    public static string CastTimeOverrides_Bow => _ctoBow.Value;
    public static string CastTimeOverrides_Claws => _ctoClaws.Value;
    public static string CastTimeOverrides_Axe => _ctoAxe.Value;


    static readonly Lazy<string> _ctoGlobal =
    new(() => GetConfigValue<string>("CastTimeOverrides"));


    public static string CastTimeOverrides => _ctoGlobal.Value;

    // (optional alias, if you ever referred to CastTimeOverrides_Global)
    public static string CastTimeOverrides_Global => _ctoGlobal.Value;


















    public static class ConfigInitialization
    {
        static readonly Regex _regex = new(@"^\[(.+)\]$");

        public static readonly Dictionary<string, object> FinalConfigValues = [];

        static readonly Lazy<List<string>> _directoryPaths = new(() =>
        {
            return
            [
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME),                                    

            ];
        });


        public static List<string> DirectoryPaths => _directoryPaths.Value;

        public static readonly List<string> SectionOrder =
        [
            "General",
            "Weapons"
        ];
        public class ConfigEntryDefinition(string section, string key, object defaultValue, string description)
        {
            public string Section { get; } = section;
            public string Key { get; } = key;
            public object DefaultValue { get; } = defaultValue;
            public string Description { get; } = description;
        }
        public static readonly List<ConfigEntryDefinition> ConfigEntries = new List<ConfigEntryDefinition>
{





    // ==== Core ====
    new ConfigEntryDefinition(
        "General", "LanguageLocalization", "English",
        "The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese"),
    new ConfigEntryDefinition(
        "General", "SpiritArsenal", true,
        "Enable or disable experimental ability replacements on shadow weapons."),

    // ==== SPIRIT (GLOBAL) ====
    new ConfigEntryDefinition(
        "General", "Spirit_Loadout",
        "FishingPole,Daggers,Reaper,Mace,Sword,GreatSword,Spear,TwinBlades,Slashers,Whip,Pistols,Crossbow,Longbow,Claws",
        "Comma-separated weapon keys to override (case-insensitive). If empty, uses hardcoded defaults."),


    new ConfigEntryDefinition(
    "General", "Debug_CastTweaks", false,
    "If true, logs CastTimeTweaks merge/apply messages to the console."),

    new ConfigEntryDefinition(
    "Debug",                       // section
    "Debug_Buffs",                 // key
    "false",                       // default
    "Enable logging for buff-related systems & patches"),

   
    // (optional global bucket)
new ConfigEntryDefinition("Weapons","CastTimeOverrides","", "Global/misc overrides"),






    // ==== SPIRIT — FISHING POLE ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_FishingPole",
        "primary=AB_Fishing_AbilityGroup;q=AB_Bandit_Fisherman_SpinAttack_AbilityGroup;e=AB_Bandit_Fisherman_FishHook_AbilityGroup;copy=true,true,true",
        "FishingPole Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_FishingPole",
        "-1,-5,-12",
        "Run script indices for FishingPole Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_FishingPole",
    "primary=;q=-822514423;e=-822514423",
    "Buffs to apply during casts for FishingPole (primary/q/e). Empty disables."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_FishingPole","\"AB_Bandit_Fisherman_SpinAttack_AbilityGroup=AB_Bandit_Fisherman_SpinAttack_Cast,cast=0.05,post=0.05\" | \"AB_Bandit_Fisherman_FishHook_AbilityGroup=AB_Bandit_Fisherman_FishHook_Cast,cast=0.05,post=0.05\"", "FishingPole-only overrides"),






    // ==== SPIRIT — DAGGERS ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Daggers",
        "primary=AB_HighLord_UnholySkill_AbilityGroup;   q=AB_Undead_BishopOfDeath_CorpseExplosion_Hard_AbilityGroup;   e=AB_Undead_Leader_AreaAttack_Group;copy=true,true,true",

        "Daggers Prefab ability Format"),
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Daggers",
        "-1,5,12",
        "Run script indices for Daggers Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Daggers",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Daggers (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Dagger","\"AB_HighLord_UnholySkill_AbilityGroup=AB_HighLord_UnholySkill_Cast,cast=0.05,post=0.05\" | \"AB_Undead_BishopOfDeath_CorpseExplosion_Hard_AbilityGroup=AB_Undead_BishopOfDeath_CorpseExplosion_Hard_Cast,cast=0.05,post=0.05\" | \"AB_Undead_Leader_AreaAttack_Group=AB_Undead_Leader_AreaAttack_Cast,cast=0.05,post=0.05\"", "Dagger-only overrides"),






    // ==== SPIRIT — REAPER ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Reaper",
        "primary=AB_ArchMage_CrystalLance_Charged_AbilityGroup;   q=AB_IceRanger_LurkerSpikes_Split_AbilityGroup;   e=AB_IceRanger_IceNova_Large_AbilityGroup;copy=true,true,true",
        "Reaper Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Reaper",
        "6,5,5",
        "Run script indices for Reaper Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Reaper",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Reaper (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Reaper","\"AB_ArchMage_CrystalLance_Charged_AbilityGroup=AB_ArchMage_CrystalLance_Charged_Cast,cast=0.05,post=0.05\" | \"AB_IceRanger_LurkerSpikes_Split_AbilityGroup=AB_IceRanger_LurkerSpikes_Split_Cast,cast=0.05,post=0.05\" | \"AB_IceRanger_IceNova_Large_AbilityGroup=AB_IceRanger_IceNova_Large_Cast,cast=0.05,post=0.0.05\"", "Reaper-only overrides"),






    // ==== SPIRIT — MACE ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Mace",
        "primary=722599012;   q=AB_ChurchOfLight_Paladin_Hard_Dash_AbilityGroup;   e=AB_Paladin_HolyNuke_AbilityGroup;copy=true,true,true",
        "Mace Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weaponsv", "Spirit_Scripts_Mace",
        "-1,-1,5",
        "Run script indices for Mace Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Mace",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Mace (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Mace","\"  AB_ChurchOfLight_Paladin_Hard_Dash_AbilityGroup=AB_ChurchOfLight_Paladin_Hard_Dash_Cast,cast=0.05,post=0.05\" | \"AB_Paladin_HolyNuke_AbilityGroup=AB_Paladin_HolyNuke_Cast,cast=0.05,post=0.05\"", "Mace-only overrides"),






    // ==== SPIRIT — SWORD ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Sword",
        "primary=-2097352908;   q=AB_Vampire_Dracula_SwordThrow_Abilitygroup;   e=AB_Vampire_Dracula_EtherialSword_Abilitygroup;copy=true,true,true",
        "Sword Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Sword",
        "-1,8,15",
        "Run script indices for Sword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Sword",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Sword (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Sword","\"AB_Vampire_Dracula_SwordThrow_Abilitygroup=AB_Vampire_Dracula_SwordThrow_Cast,cast=0.05,post=0.05\" | \"AB_Vampire_Dracula_EtherialSword_Abilitygroup=AB_Vampire_Dracula_EtherialSword_Cast,cast=0.05,post=0.05\"", "Sword-only overrides"),






    // ==== SPIRIT — GREATSWORD ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_GreatSword",
        "primary=-1428882023;   q=AB_HighLord_SwordPrimary_MeleeAttack_AbilityGroup;   e=AB_HighLord_SwordDashCleave_AbilityGroup;copy=true,true,true",
        "GreatSword Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_GreatSword",
        "-1,-1,5",
        "Run script indices for GreatSword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_GreatSword",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for GreatSword (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Greatsword","\"AB_HighLord_SwordPrimary_MeleeAttack_AbilityGroup=AB_HighLord_SwordPrimary_MeleeAttack_Cast01,cast=0.05,post=0.05\" | \"AB_HighLord_SwordPrimary_MeleeAttack_AbilityGroup=AB_HighLord_SwordPrimary_MeleeAttack_Cast02,cast=0.05,post=0.05\" | \"AB_HighLord_SwordDashCleave_AbilityGroup=AB_HighLord_SwordDashCleave_DashStrike_Cast,cast=0.05,post=0.05\"", "Greatsword-only overrides"),






    // ==== SPIRIT — SPEAR ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Spear",
        "primary=1142040823;   q=AB_Blackfang_Viper_JavelinRain_AbilityGroup;   e=AB_ChurchOfLight_Overseer_SpinAttack_AbilityGroup;copy=true,true,true",
        "Spear Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Spear",
        "-1,2,11",
        "Run script indices for Spear Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Spear",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Spear (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Spear","\"AB_Blackfang_Viper_JavelinRain_AbilityGroup=AB_Blackfang_Viper_JavelinRain_Cast,cast=0.05,post=0.05\" | \"AB_ChurchOfLight_Overseer_SpinAttack_AbilityGroup=AB_ChurchOfLight_Overseer_SpinAttack_Cast,cast=0.05,post=0\" | \"AB_ChurchOfLight_Overseer_PiercingCharge_AbilityGroup=AB_ChurchOfLight_Overseer_PiercingCharge_Cast,cast=0.05,post=0.05\"", "Spear-only overrides"),






    // ==== SPIRIT — TWINBLADES ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_TwinBlades",
        "primary=298784800;   q=AB_Undead_ArenaChampion_Windslash_AbilityGroup;   e=AB_Undead_ArenaChampion_CounterStrike_AbilityGroup;copy=true,true,true",
        "TwinBlades Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_TwinBlades",
        "-1,-1,6",
        "Run script indices for TwinBlades Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_TwinBlades",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for TwinBlades (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_TwinBlades","\"AB_Undead_ArenaChampion_Windslash_AbilityGroup=AB_Undead_ArenaChampion_Windslash_Cast01,cast=0.05,post=0.05\" | \"AB_Undead_ArenaChampion_Windslash_AbilityGroup=AB_Undead_ArenaChampion_Windslash_Cast02,cast=0.05,post=0\" | \"AB_Undead_ArenaChampion_CounterStrike_AbilityGroup=AB_Undead_ArenaChampion_CounterStrike_Cast,cast=0.05,post=0.05\"", "TwinBlades-only overrides"),






    // ==== SPIRIT — SLASHERS ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Slashers",
        "primary=-186690512;   q=AB_Blackfang_Livith_CuttingWind_AbilityGroup;   e=AB_Blackfang_Livith_SlicingDash_AbilityGroup;copy=true,true,true",
        "Slashers Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Slashers",
        "-1,5,5",
        "Run script indices for Slashers Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

    new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Slashers",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Slashers (primary/q/e)."),

    new ConfigEntryDefinition("Weapons","CastTimeOverrides_Slashers","\"AB_Blackfang_Livith_CuttingWind_AbilityGroup=AB_Blackfang_Livith_CuttingWind_Cast01,cast=0.05,post=0.05\" | \"AB_Blackfang_Livith_CuttingWind_AbilityGroup=AB_Blackfang_Livith_CuttingWind_Cast02,cast=0.05,post=0\" | \"AB_Blackfang_Livith_SlicingDash_AbilityGroup=AB_Blackfang_Livith_SlicingDash_Cast01,cast=0.05,post=0\"", "Slashers-only overrides"),






    // ==== SPIRIT — WHIP ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Whip",
        "primary=112329675;   q=AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_AbilityGroup;   e=AB_Gloomrot_RailgunSergeant_LightningWall_AbilityGroup;copy=true,true,true",
        "Whip Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Whip",
        "5,2,4",
        "Run script indices for Whip Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

   new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Whip",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Whip (primary/q/e)."),

   new ConfigEntryDefinition("Weapons","CastTimeOverrides_Whip","\"AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_AbilityGroup=AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_Cast,cast=0.05,post=0.05\" | \"AB_Gloomrot_RailgunSergeant_LightningWall_AbilityGroup=AB_Gloomrot_RailgunSergeant_LightningWall_Cast,cast=0.05,post=0.05\"", "Whip-only overrides"),






    // ==== SPIRIT — PISTOLS ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Pistols",
        "primary=AB_Rifleman_Projectile_Group;   q=AB_VHunter_Jade_Snipe_Group;   e=AB_VHunter_Jade_DisablingShot_Group;copy=true,true,true",
        "Pistols Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Pistols",
        "0,5,11",
        "Run script indices for Pistols Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

    new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Pistols",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Pistols (primary/q/e)."),

    new ConfigEntryDefinition("Weapons","CastTimeOverrides_Pistols","\"AB_Rifleman_Projectile_Group=AB_Rifleman_Projectile_Cast,cast=0.05,post=0.05\" | \"AB_VHunter_Jade_Snipe_Group=AB_VHunter_Jade_Snipe_Cast,cast=0.05,post=0.05\" | \"AB_VHunter_Jade_DisablingShot_Group=AB_VHunter_Jade_DisablingShot_Cast,cast=0.05,post=0.05\"", "Pistols-only overrides"),






    // ==== SPIRIT — CROSSBOW ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Crossbow",
        "primary=AB_Militia_BombThrow_AbilityGroup;   q=AB_VHunter_Jade_BlastVault_Group;   e=AB_Bandit_ClusterBombThrow_AbilityGroup;copy=true,true,true",
        "Crossbow Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Crossbow",
        "10,5,9",
        "Run script indices for Crossbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

  new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Crossbow",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Crossbow (primary/q/e)."),

  new ConfigEntryDefinition("Weapons","CastTimeOverrides_Crossbow","\"AB_Militia_BombThrow_AbilityGroup=AB_Militia_BombThrow_Cast,cast=0.05,post=0.05\" | \"AB_VHunter_Jade_BlastVault_Group=AB_VHunter_Jade_BlastVault_Cast,cast=0.05,post=0.05\" | \"AB_Bandit_ClusterBombThrow_AbilityGroup=AB_Bandit_ClusterBombThrow_Cast,cast=0.05,post=0.05\"", "Crossbow-only overrides"),






    // ==== SPIRIT — LONGBOW ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Longbow",
        "primary=AB_Militia_LightArrow_UnsteadyQuickShot_Group;   q=AB_Bandit_FrostArrow_RainOfArrows_Hard_AbilityGroup;   e=AB_Bandit_Deadeye_Chaosbarrage_Hard_Group;copy=true,true,true",
        "Longbow Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Longbow",
        "-1,5,7",
        "Run script indices for Longbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

    new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Longbow",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Longbow (primary/q/e)."),

    new ConfigEntryDefinition("Weapons","CastTimeOverrides_Bow","\"AB_Militia_LightArrow_UnsteadyQuickShot_Group=AB_Militia_LightArrow_UnsteadyQuickShot_Cast,cast=0.05,post=0.05\" | \"AB_Bandit_FrostArrow_RainOfArrows_Hard_AbilityGroup=AB_Bandit_FrostArrow_RainOfArrows_Hard_Cast,cast=0.05,post=0.05\" | \"AB_Bandit_Deadeye_Chaosbarrage_Hard_Group=AB_Bandit_Deadeye_Chaosbarrage_Hard_Cast,cast=0.05,post=0.05\"", "Bow-only overrides"),






    // ==== SPIRIT — CLAWS ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Claws",
        "primary=1180130515;   q=AB_Blackfang_Striker_FistBlock_AbilityGroup;   e=AB_Prog_HomingNova_Group;copy=true,true,true",
        "Claws Prefab ability Format"),

    new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Claws",
        "-1,5,5",
        "Run script indices for Claws Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

    new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Claws",
    "primary=;   q=-822514423;   e=-822514423",
    "Buffs to apply during casts for Claws (primary/q/e)."),

    new ConfigEntryDefinition("Weapons","CastTimeOverrides_Claws","\"AB_Blackfang_Striker_FistBlock_AbilityGroup=AB_Blackfang_Striker_FistBlock_Cast,cast=0.05,post=0.05\" | \"AB_Prog_HomingNova_Group=AB_Prog_HomingNova_Cast,cast=0.05,post=0.05\"", "Claws-only overrides"),






     // ==== SPIRIT — AXE ====
    new ConfigEntryDefinition(
        "Weapons", "Spirit_Groups_Axe",
        "primary=;   q=;   e=;copy=true,true,true",
        "Axe Prefab ability Format"),
  new ConfigEntryDefinition(
        "Weapons", "Spirit_Scripts_Axe",
        "",
        "Run script indices for Axe Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),

    new ConfigEntryDefinition(
    "Weapons", "Spirit_Buffs_Axe",
    "primary=;   q=-;   e=",
    "Buffs to apply during casts for Axe (primary/q/e)."),

    new ConfigEntryDefinition("Weapons","CastTimeOverrides_Axe","", "Axe-only overrides") // no Spirit_Groups_Axe in your file





        };

















            

        
        public static void InitializeConfig()
        {
            foreach (string path in DirectoryPaths)
            {
                CreateDirectory(path);
            }

            var oldConfigFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
            Dictionary<string, string> oldConfigValues = [];

            if (File.Exists(oldConfigFile))
            {
                string[] oldConfigLines = File.ReadAllLines(oldConfigFile);
                foreach (var line in oldConfigLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var configKey = keyValue[0].Trim();
                        var configValue = keyValue[1].Trim();
                        oldConfigValues[configKey] = configValue;
                    }
                }
            }

            foreach (ConfigEntryDefinition entry in ConfigEntries)
            {
                // Get the type of DefaultValue
                Type entryType = entry.DefaultValue.GetType();

                // Reflect on the nested ConfigInitialization class within ConfigService
                Type nestedClassType = typeof(ConfigService).GetNestedType("ConfigInitialization", BindingFlags.Static | BindingFlags.Public);

                // Use reflection to call InitConfigEntry with the appropriate type
                MethodInfo method = nestedClassType.GetMethod("InitConfigEntry", BindingFlags.Static | BindingFlags.NonPublic);
                MethodInfo generic = method.MakeGenericMethod(entryType);

                // Check if the old config has the key
                if (oldConfigValues.TryGetValue(entry.Key, out var oldValue))
                {
                    // Convert the old value to the correct type

                    try
                    {
                        object convertedValue;

                        if (entryType == typeof(float))
                        {
                            convertedValue = float.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else if (entryType == typeof(double))
                        {
                            convertedValue = double.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else if (entryType == typeof(decimal))
                        {
                            convertedValue = decimal.Parse(oldValue, CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            convertedValue = Convert.ChangeType(oldValue, entryType);
                        }

                        var configEntry = generic.Invoke(null, [entry.Section, entry.Key, convertedValue, entry.Description]);
                        UpdateConfigProperty(entry.Key, configEntry);

                        object valueProp = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);
                        if (valueProp != null)
                        {
                            FinalConfigValues[entry.Key] = valueProp;
                        }
                        else
                        {
                            Core.Log.LogError($"Failed to get value property for {entry.Key}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.LogInstance.LogError($"Failed to convert old config value for {entry.Key}: {ex.Message}");
                    }
                }
                else
                {
                    // Use default value if key is not in the old config
                    var configEntry = generic.Invoke(null, [entry.Section, entry.Key, entry.DefaultValue, entry.Description]);
                    UpdateConfigProperty(entry.Key, configEntry);

                    object valueProp = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);
                    if (valueProp != null)
                    {
                        FinalConfigValues[entry.Key] = valueProp;
                    }
                    else
                    {
                        Core.Log.LogError($"Failed to get value property for {entry.Key}");
                    }
                }
            }

            var configFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");
            if (File.Exists(configFile)) OrganizeConfig(configFile);
        }
        static void UpdateConfigProperty(string key, object configEntry)
        {
            PropertyInfo propertyInfo = typeof(ConfigService).GetProperty(key, BindingFlags.Static | BindingFlags.Public);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                object value = configEntry.GetType().GetProperty("Value")?.GetValue(configEntry);

                if (value != null)
                {
                    propertyInfo.SetValue(null, Convert.ChangeType(value, propertyInfo.PropertyType));
                }
                else
                {
                    throw new Exception($"Value property on configEntry is null for key {key}.");
                }
            }
        }
        static ConfigEntry<T> InitConfigEntry<T>(string section, string key, T defaultValue, string description)
        {
            // Bind the configuration entry with the default value in the new section
            var entry = Plugin.Instance.Config.Bind(section, key, defaultValue, description);

            // Define the path to the configuration file
            var configFile = Path.Combine(BepInEx.Paths.ConfigPath, $"{MyPluginInfo.PLUGIN_GUID}.cfg");

            // Ensure the configuration file is only loaded if it exists
            if (File.Exists(configFile))
            {
                string[] configLines = File.ReadAllLines(configFile);
                //Plugin.LogInstance.LogInfo(configLines);
                foreach (var line in configLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }

                    var keyValue = line.Split('=');
                    if (keyValue.Length == 2)
                    {
                        var configKey = keyValue[0].Trim();
                        var configValue = keyValue[1].Trim();

                        // Check if the key matches the provided key
                        if (configKey.Equals(key, StringComparison.CurrentCultureIgnoreCase))
                        {
                            // Try to convert the string value to the expected type
                            try
                            {
                                object convertedValue;

                                Type t = typeof(T);

                                if (t == typeof(float))
                                {
                                    convertedValue = float.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(double))
                                {
                                    convertedValue = double.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(decimal))
                                {
                                    convertedValue = decimal.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(int))
                                {
                                    convertedValue = int.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(uint))
                                {
                                    convertedValue = uint.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(long))
                                {
                                    convertedValue = long.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(ulong))
                                {
                                    convertedValue = ulong.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(short))
                                {
                                    convertedValue = short.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(ushort))
                                {
                                    convertedValue = ushort.Parse(configValue, CultureInfo.InvariantCulture);
                                }
                                else if (t == typeof(bool))
                                {
                                    convertedValue = bool.Parse(configValue);
                                }
                                else if (t == typeof(string))
                                {
                                    convertedValue = configValue;
                                }
                                else
                                {
                                    // Handle other types or throw an exception
                                    throw new NotSupportedException($"Type {t} is not supported");
                                }

                                entry.Value = (T)convertedValue;
                            }
                            catch (Exception ex)
                            {
                                Plugin.LogInstance.LogError($"Failed to convert config value for {key}: {ex.Message}");
                            }

                            break;
                        }
                    }
                }
            }

            return entry;
        }
        static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        const string DEFAULT_VALUE_LINE = "# Default value: ";
        static void OrganizeConfig(string configFile)
        {
            try
            {
                Dictionary<string, List<string>> OrderedSections = [];
                string currentSection = "";

                string[] lines = File.ReadAllLines(configFile);
                string[] fileHeader = lines[0..3];

                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();
                    var match = _regex.Match(trimmedLine);

                    if (match.Success)
                    {
                        currentSection = match.Groups[1].Value;
                        if (!OrderedSections.ContainsKey(currentSection))
                        {
                            OrderedSections[currentSection] = [];
                        }
                    }
                    else if (SectionOrder.Contains(currentSection))
                    {
                        OrderedSections[currentSection].Add(trimmedLine);
                    }
                }

                using StreamWriter writer = new(configFile, false);

                foreach (var header in fileHeader)
                {
                    writer.WriteLine(header);
                }

                foreach (var section in SectionOrder)
                {
                    if (OrderedSections.ContainsKey(section))
                    {
                        writer.WriteLine($"[{section}]");

                        List<string> sectionLines = OrderedSections[section];

                        // Extract keys from the config file
                        List<string> sectionKeys = sectionLines
                     .Where(line => line.Contains('=') && !line.TrimStart().StartsWith("#"))
                     .Select(line => line.Split('=')[0].Trim())
                    .ToList();

                        // Create a dictionary of default values directly from ConfigEntries
                        Dictionary<string, object> defaultValuesMap = ConfigEntries
                            .Where(entry => entry.Section == section)
                            .ToDictionary(entry => entry.Key, entry => entry.DefaultValue);

                        int keyIndex = 0;
                        bool previousLineSkipped = false; // Track whether the last line was skipped

                        foreach (var line in sectionLines)
                        {
                            var trimmed = line.Trim();

                            // if previous line (a key) was skipped as obsolete, also skip its "# Default value: ..." line
                            if (previousLineSkipped && trimmed.StartsWith(DEFAULT_VALUE_LINE))
                            {
                                previousLineSkipped = false;
                                continue;
                            }

                            // pass through regular comments (but not the default-value marker we handle above)
                            if (trimmed.StartsWith("#") && !trimmed.StartsWith(DEFAULT_VALUE_LINE))
                            {
                                writer.WriteLine(line);
                                previousLineSkipped = false;
                                continue;
                            }

                            if (line.Contains('='))
                            {
                                string key = line.Split('=')[0].Trim();

                                // ignore commented key-like lines just in case
                                if (key.StartsWith("#")) { writer.WriteLine(line); previousLineSkipped = false; continue; }

                                if (!defaultValuesMap.ContainsKey(key))
                                {
                                    Core.Log.LogWarning($"Skipping obsolete config entry: {key}");
                                    previousLineSkipped = true;
                                    continue;
                                }
                            }

                            if (line.Contains(DEFAULT_VALUE_LINE))
                            {
                                // guard against out-of-range and missing entry
                                if (keyIndex < sectionKeys.Count)
                                {
                                    var entry = ConfigEntries.FirstOrDefault(e => e.Key == sectionKeys[keyIndex] && e.Section == section);
                                    if (entry != null)
                                    {
                                        writer.WriteLine(DEFAULT_VALUE_LINE + entry.DefaultValue.ToString());
                                        keyIndex++;
                                    }
                                    else
                                    {
                                        Core.Log.LogWarning($"Config entry for key '{sectionKeys[keyIndex]}' not found in ConfigEntries!");
                                        writer.WriteLine(line);
                                    }
                                }
                                else
                                {
                                    writer.WriteLine(line);
                                }
                                previousLineSkipped = false;
                                continue;
                            }

                            // existing blank-line squashing can remain as-is
                            writer.WriteLine(line);
                            previousLineSkipped = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Core.Log.LogError($"Failed to clean and organize config file: {ex.Message}");
            }
        }
    }
    static T GetConfigValue<T>(string key)
    {
        if (ConfigInitialization.FinalConfigValues.TryGetValue(key, out var val))
        {
            return (T)Convert.ChangeType(val, typeof(T));
        }

        var entry = ConfigInitialization.ConfigEntries.FirstOrDefault(e => e.Key == key);
        return entry == null ? throw new InvalidOperationException($"Config entry for key '{key}' not found.") : (T)entry.DefaultValue;
    }
}
