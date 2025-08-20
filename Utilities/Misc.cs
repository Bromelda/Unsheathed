using Bloodcraft.Resources;
using Bloodcraft.Services;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Diagnostics;
using System.Text;
using Unity.Entities;
using VampireCommandFramework;



namespace Bloodcraft.Utilities;
internal static class Misc
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static GameDataSystem GameDataSystem => SystemService.GameDataSystem;

    static readonly Random _random = new();
    const string STAT_MOD = "StatMod";
    public enum SpellSchool : int
    {
        Shadow = 0,
        Blood = 1,
        Chaos = 2,
        Unholy = 3,
        Illusion = 4,
        Frost = 5,
        Storm = 6
    }
    public class BiDictionary<T1, T2> : IEnumerable<KeyValuePair<T1, T2>> // kind of weird but idk it can stay for now
    {
        readonly Dictionary<T1, T2> _forward = [];
        readonly Dictionary<T2, T1> _reverse = [];
        public BiDictionary() { }
        public BiDictionary(IEnumerable<KeyValuePair<T1, T2>> pairs)
        {
            foreach (var (key, value) in pairs)
            {
                Add(key, value);
            }
        }
        public void Add(T1 key, T2 value)
        {
            _forward[key] = value;
            _reverse[value] = key;
        }
        public T2 this[T1 key] => _forward[key];
        public T1 this[T2 key] => _reverse[key];
        public bool TryGetByFirst(T1 key, out T2 value) => _forward.TryGetValue(key, out value);
        public bool TryGetBySecond(T2 key, out T1 value) => _reverse.TryGetValue(key, out value);
        public IEnumerable<T1> Keys => _forward.Keys;
        public IEnumerable<T2> Values => _forward.Values;
        public IEnumerator<KeyValuePair<T1, T2>> GetEnumerator() => _forward.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class SpellSchoolInfusionMap
    {
        public static readonly BiDictionary<SpellSchool, PrefabGUID> SpellSchoolInfusions = [];
        static SpellSchoolInfusionMap()
        {
            SpellSchoolInfusions.Add(SpellSchool.Blood, PrefabGUIDs.SpellMod_Weapon_BloodInfused);
            SpellSchoolInfusions.Add(SpellSchool.Chaos, PrefabGUIDs.SpellMod_Weapon_ChaosInfused);
            SpellSchoolInfusions.Add(SpellSchool.Shadow, PrefabGUIDs.SpellMod_Weapon_UndeadInfused);
            SpellSchoolInfusions.Add(SpellSchool.Illusion, PrefabGUIDs.SpellMod_Weapon_IllusionInfused);
            SpellSchoolInfusions.Add(SpellSchool.Frost, PrefabGUIDs.SpellMod_Weapon_FrostInfused);
            SpellSchoolInfusions.Add(SpellSchool.Storm, PrefabGUIDs.SpellMod_Weapon_StormInfused);
        }
    }

  
   

   

    

   
   
    public static string FormatTimespan(TimeSpan timeSpan)
    {
        string timeString = timeSpan.ToString(@"mm\:ss");
        return timeString;
    }
    public static string FormatPercentStatValue(float value)
    {
        string bonusString = (value * 100).ToString("F0") + "%";
        return bonusString;
    }

    static readonly Dictionary<PrefabGUID, float> _statModPresetValues = new()
    {
        { new(-1545133628), 0.25f },
        { new(1448170922), 0.15f },
        { new(-1700712765), 0.25f },
        { new(523084427), 0.15f },
        { new(1179205309), 0.15f },
        { new(-2004879548), 0.10f },
        { new(539854831), 0.15f },
        { new(-1274939577), 0.10f },
        { new(1032018140), 0.15f },
        { new(1842448780), 0.15f }
    };
    public static bool TryGetStatTypeFromPrefabName(PrefabGUID prefabGuid, float originalValue, out UnitStatType statType, out float resolvedValue)
    {
        statType = default;
        resolvedValue = originalValue;

        string rawPrefabString = prefabGuid.GetPrefabName();
        string baseName = rawPrefabString.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (baseName is null) return false;

        baseName = baseName.Replace("StatMod_", "", StringComparison.CurrentCultureIgnoreCase)
                           .Replace("Unique_", "", StringComparison.CurrentCultureIgnoreCase);

        string[] tierSuffixes = ["_Low", "_Mid", "_High"];
        foreach (var suffix in tierSuffixes)
        {
            if (baseName.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase))
            {
                baseName = baseName[..^suffix.Length];
                break;
            }
        }

        if (_statModPresetValues.TryGetValue(prefabGuid, out float presetValue))
        {
            resolvedValue = presetValue;
        }

        if (Enum.TryParse(baseName, ignoreCase: true, out statType)) return true;

        switch (baseName.ToLowerInvariant())
        {
            case "attackspeed":
                statType = UnitStatType.PrimaryAttackSpeed;
                return true;
            case "criticalstrikephysical":
                statType = UnitStatType.PhysicalCriticalStrikeChance;
                return true;
            case "criticalstrikephysicalpower":
                statType = UnitStatType.PhysicalCriticalStrikeDamage;
                return true;
            case "criticalstrikespellpower":
                statType = UnitStatType.SpellCriticalStrikeDamage;
                return true;
            case "criticalstrikespells":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "criticalstrikespell":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "spellcooldownreduction":
                statType = UnitStatType.SpellCooldownRecoveryRate;
                return true;
            case "weaponcooldownreduction":
                statType = UnitStatType.WeaponCooldownRecoveryRate;
                return true;
            case "spellleech":
                statType = UnitStatType.SpellLifeLeech;
                return true;
        }

        Core.Log.LogWarning($"Unmapped stat mod prefab: '{rawPrefabString}' → parsed '{baseName}'");
        return false;
    }

    /*
    public static bool TryGetStatTypeFromPrefabName(string rawPrefabString, out UnitStatType statType)
    {
        statType = default;

        if (string.IsNullOrWhiteSpace(rawPrefabString))
            return false;

        // Step 1: Extract the filename portion (remove 'PrefabGuid(...)' etc.)
        string baseName = rawPrefabString.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (baseName is null)
            return false;

        // Step 2: Strip known prefixes and suffixes
        baseName = baseName.Replace("StatMod_", "", StringComparison.CurrentCultureIgnoreCase)
                           .Replace("Unique_", "", StringComparison.CurrentCultureIgnoreCase);

        // Remove suffixes like "_Low", "_Mid", "_High"
        string[] tierSuffixes = ["_Low", "_Mid", "_High"];
        foreach (var suffix in tierSuffixes)
        {
            if (baseName.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase))
            {
                baseName = baseName[..^suffix.Length];
                break;
            }
        }

        // Step 3: Try to parse directly
        if (Enum.TryParse(baseName, ignoreCase: true, out statType))
            return true;

        // Step 4: Fallback alias matching
        switch (baseName.ToLowerInvariant())
        {
            case "attackspeed":
                statType = UnitStatType.PrimaryAttackSpeed;
                return true;
            case "criticalstrikephysical":
                statType = UnitStatType.PhysicalCriticalStrikeChance;
                return true;
            case "criticalstrikephysicalpower":
                statType = UnitStatType.PhysicalCriticalStrikeDamage;
                return true;
            case "criticalstrikespellpower":
                statType = UnitStatType.SpellCriticalStrikeDamage;
                return true;
            case "criticalstrikespells":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "criticalstrikespell":
                statType = UnitStatType.SpellCriticalStrikeChance;
                return true;
            case "spellcooldownreduction":
                statType = UnitStatType.SpellCooldownRecoveryRate;
                return true;
            case "weaponcooldownreduction":
                statType = UnitStatType.WeaponCooldownRecoveryRate;
                return true;
            case "spellleech":
                statType = UnitStatType.SpellLifeLeech;
                return true;
            case "vampiredamage":
                statType = UnitStatType.DamageVsVampires;
                return true;
            default:
                Core.Log.LogWarning($"Unmapped stat mod prefab! ('{rawPrefabString}' → parsed '{baseName}')");
                return false;
        }
    }
    */
  
    public static void ReplySCTDetails(ChatCommandContext ctx)
    {
        ulong steamId = ctx.User.PlatformId;

        StringBuilder sb = new();
        sb.AppendLine("<color=#FFC0CB>SCT Options</color>:");

      
       
        {
            

           
            {
                
            }
        }

        LocalizationService.HandleReply(ctx, sb.ToString());
    }
    public static void GiveOrDropItem(User user, Entity playerCharacter, PrefabGUID itemType, int amount)
    {
        var itemDataHashMap = GameDataSystem.ItemHashLookupMap;
        bool hasSpace = InventoryUtilities.HasFreeStackSpaceOfType(EntityManager, playerCharacter, itemDataHashMap, itemType, amount);

        if (hasSpace && ServerGameManager.TryAddInventoryItem(playerCharacter, itemType, amount))
        {
            string message = "Your bag feels slightly heavier...";
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
        else
        {
            string message = "Something fell out of your bag!";
            InventoryUtilitiesServer.CreateDropItem(EntityManager, playerCharacter, itemType, amount, new Entity()); // does this create multiple drops to account for excessive stacks? noting for later
            LocalizationService.HandleServerReply(EntityManager, user, message);
        }
    }
    public static bool RollForChance(float chance)
    {
        return _random.NextDouble() < chance;
    }
    public static class Performance
    {
        static readonly Stopwatch _stopwatch = new();
        static string _label = "";
        static long _totalElapsedTicks = 0;
        public static void Start(string label)
        {
            _label = label;
            _stopwatch.Restart();
            Core.Log.LogInfo($"[TIMER] Start - {_label}");
        }
        public static void Stop()
        {
            _stopwatch.Stop();

            long elapsedTicks = _stopwatch.ElapsedTicks;
            _totalElapsedTicks += elapsedTicks;

            double elapsedMilliseconds = _stopwatch.Elapsed.TotalMilliseconds;

            Core.Log.LogInfo($"[TIMER] Stop - {_label} ({_totalElapsedTicks}t | {elapsedMilliseconds:F3}ms)");

            // Reset
            _totalElapsedTicks = 0;
        }
    }

   
}
