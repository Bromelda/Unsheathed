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





   



    static readonly Lazy<float> _maxHealth = new(() => GetConfigValue<float>("MaxHealth"));
    public static float MaxHealth => _maxHealth.Value;

    static readonly Lazy<float> _movementSpeed = new(() => GetConfigValue<float>("MovementSpeed"));
    public static float MovementSpeed => _movementSpeed.Value;

    static readonly Lazy<float> _primaryAttackSpeed = new(() => GetConfigValue<float>("PrimaryAttackSpeed"));
    public static float PrimaryAttackSpeed => _primaryAttackSpeed.Value;

    static readonly Lazy<float> _physicalLifeLeech = new(() => GetConfigValue<float>("PhysicalLifeLeech"));
    public static float PhysicalLifeLeech => _physicalLifeLeech.Value;

    static readonly Lazy<float> _spellLifeLeech = new(() => GetConfigValue<float>("SpellLifeLeech"));
    public static float SpellLifeLeech => _spellLifeLeech.Value;

    static readonly Lazy<float> _primaryLifeLeech = new(() => GetConfigValue<float>("PrimaryLifeLeech"));
    public static float PrimaryLifeLeech => _primaryLifeLeech.Value;

    static readonly Lazy<float> _physicalPower = new(() => GetConfigValue<float>("PhysicalPower"));
    public static float PhysicalPower => _physicalPower.Value;

    static readonly Lazy<float> _spellPower = new(() => GetConfigValue<float>("SpellPower"));
    public static float SpellPower => _spellPower.Value;

    static readonly Lazy<float> _physicalCritChance = new(() => GetConfigValue<float>("PhysicalCritChance"));
    public static float PhysicalCritChance => _physicalCritChance.Value;

    static readonly Lazy<float> _physicalCritDamage = new(() => GetConfigValue<float>("PhysicalCritDamage"));
    public static float PhysicalCritDamage => _physicalCritDamage.Value;

    static readonly Lazy<float> _spellCritChance = new(() => GetConfigValue<float>("SpellCritChance"));
    public static float SpellCritChance => _spellCritChance.Value;

    static readonly Lazy<float> _spellCritDamage = new(() => GetConfigValue<float>("SpellCritDamage"));
    public static float SpellCritDamage => _spellCritDamage.Value;


    static readonly Lazy<bool> _bloodQualityBonus = new(() => GetConfigValue<bool>("BloodQualityBonus"));
    public static bool BloodQualityBonus => _bloodQualityBonus.Value;





    static readonly Lazy<float> _healingReceived = new(() => GetConfigValue<float>("HealingReceived"));
    public static float HealingReceived => _healingReceived.Value;

    static readonly Lazy<float> _damageReduction = new(() => GetConfigValue<float>("DamageReduction"));
    public static float DamageReduction => _damageReduction.Value;

    static readonly Lazy<float> _physicalResistance = new(() => GetConfigValue<float>("PhysicalResistance"));
    public static float PhysicalResistance => _physicalResistance.Value;

    static readonly Lazy<float> _spellResistance = new(() => GetConfigValue<float>("SpellResistance"));
    public static float SpellResistance => _spellResistance.Value;

    static readonly Lazy<float> _resourceYield = new(() => GetConfigValue<float>("ResourceYield"));
    public static float ResourceYield => _resourceYield.Value;

    static readonly Lazy<float> _reducedBloodDrain = new(() => GetConfigValue<float>("ReducedBloodDrain"));
    public static float ReducedBloodDrain => _reducedBloodDrain.Value;

    static readonly Lazy<float> _spellCooldownRecoveryRate = new(() => GetConfigValue<float>("SpellCooldownRecoveryRate"));
    public static float SpellCooldownRecoveryRate => _spellCooldownRecoveryRate.Value;

    static readonly Lazy<float> _weaponCooldownRecoveryRate = new(() => GetConfigValue<float>("WeaponCooldownRecoveryRate"));
    public static float WeaponCooldownRecoveryRate => _weaponCooldownRecoveryRate.Value;

    static readonly Lazy<float> _ultimateCooldownRecoveryRate = new(() => GetConfigValue<float>("UltimateCooldownRecoveryRate"));
    public static float UltimateCooldownRecoveryRate => _ultimateCooldownRecoveryRate.Value;

    static readonly Lazy<float> _minionDamage = new(() => GetConfigValue<float>("MinionDamage"));
    public static float MinionDamage => _minionDamage.Value;

    static readonly Lazy<float> _abilityAttackSpeed = new(() => GetConfigValue<float>("AbilityAttackSpeed"));
    public static float AbilityAttackSpeed => _abilityAttackSpeed.Value;

    static readonly Lazy<float> _corruptionDamageReduction = new(() => GetConfigValue<float>("CorruptionDamageReduction"));
    public static float CorruptionDamageReduction => _corruptionDamageReduction.Value;



    static readonly Lazy<bool> _weaponsSystem = new(() => GetConfigValue<bool>("WeaponsSystem"));
 
   public static bool WeaponsSystem => true;










    public static class ConfigInitialization
    {
        static readonly Regex _regex = new(@"^\[(.+)\]$");

        public static readonly Dictionary<string, object> FinalConfigValues = [];

        static readonly Lazy<List<string>> _directoryPaths = new(() =>
        {
            return
            [
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME),                                     // 0
           
            Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PlayerBools")                     // 9
          
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
        public static readonly List<ConfigEntryDefinition> ConfigEntries =
        [
            new ConfigEntryDefinition("General", "LanguageLocalization", "English", "The language localization for prefabs displayed to users. English by default. Options: Brazilian, English, French, German, Hungarian, Italian, Japanese, Koreana, Latam, Polish, Russian, SimplifiedChinese, Spanish, TraditionalChinese, Thai, Turkish, Vietnamese"),

            new ConfigEntryDefinition("General", "SpiritArsenal", true, "Enable or disable experimental ability replacements on shadow weapons."),
            // --- Spirit Arsenal: data-driven overrides ---
            new ConfigEntryDefinition("General","Spirit_Loadout","FishingPole,Daggers,Reaper,Mace,Sword,GreatSword,Spear,TwinBlades,Slashers,Whip,Pistols,Crossbow,Longbow,Claws", "FishingPole,Daggers,Reaper,Mace,Sword,GreatSword,Spear,TwinBlades,Slashers,Whip,Pistols,Crossbow,Longbow,Claws."),
                // e.g. "FishingPole,Daggers,Reaper,Mace,Sword,GreatSword,Spear,TwinBlades,Slashers,Whip,Pistols,Crossbow,Longbow,Claws."    "Comma-separated weapon keys to override (case-insensitive). If empty, uses hardcoded defaults."


// "For each weapon, one string bundle: "primary=<PrefabName|int>;q=<PrefabName|int>;e=<PrefabName|int>;copy=<P,Q,E>."
new ConfigEntryDefinition("General",
    "Spirit_Groups_FishingPole",
    " primary=AB_Fishing_AbilityGroup;q=AB_Bandit_Fisherman_SpinAttack_AbilityGroup;e=AB_Bandit_Fisherman_FishHook_AbilityGroup;copy=true,true,true",
     "FishingPole Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Daggers","primary=1837385563;q=1971375367;e=211628325;copy=true,true,true",
    "Daggers Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Reaper",
    "primary=948587795;q=-808864212;e=1691254929;copy=true,true,true",
    "Reaper Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Mace",
    "primary=722599012;q=-1172099204;e=1926314891;copy=true,true,true",
    "Mace Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Sword",
    "primary=-2097352908;q=532210332;e=-1161896955;copy=true,true,true",
    "Sword Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_GreatSword",
    "primary=-1428882023;q=-328302080;e=-2126197617;copy=true,true,true",
    "GreatSword Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Spear",
    "primary=1142040823;q=381862924;e=1166337981;copy=true,true,true", 
    "Spear Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_TwinBlades",
    "primary=298784800;q=-2023636973;e=-1357375516 ;copy=true,true,true", 
    "TwinBlades Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Slashers",
    "primary=-186690512;q=-198012170;e=2019689688;copy=true,true,true", 
    "Slashers Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Whip",
    "primary=112329675;q=-1111373807;e=2130985273;copy=true,true,true",
    "Whip Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Pistols",
    "primary=1622839653;q=-1884688827;e=-526118698;copy=true,true,true",  
    "Pistols Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Crossbow",
    "primary=1232856473;q=76767983;e=-444905742;copy=true,true,true",  
    "Crossbow Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Longbow",
    "primary=617823552;q=766284586;e=501615608;copy=true,true,true", 
    "Longbow Prefab ability Format "),

new ConfigEntryDefinition("General",
    "Spirit_Groups_Claws",
    "primary=1180130515;q=-1085783726;e=-2096054164;copy=true,true,true", 
    "Claws Prefab ability Format "),

            // --- Spirit: ability run script indices per weapon ---
// Format: "P,Q,E" (integers). Example: "1,5,11". Use -1 to skip registering a script for that slot.
// Leave empty to keep your current defaults.
new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_FishingPole",
    "-1,-5,-12", // default script indices for P,Q,E; use -1 to skip
    "Run script indices for FishingPole Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),
new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Daggers",
    "-1,5,12",
    "Run script indices for Daggers Spirit abilities as 'P,Q,E' (e.g., '2,7,7'). Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Reaper",
    "6,5,5",
    "Run script indices for Reaper Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Mace",
    "-1,-1,5",
    "Run script indices for Mace Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Sword",
    "-1,8,15",
    "Run script indices for Sword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_GreatSword",
    "-1,-1,5",
    "Run script indices for GreatSword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Spear",
    "-1,2,11",
    "Run script indices for Spear Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_TwinBlades",
    "-1,-1,6",
    "Run script indices for TwinBlades Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Slashers",
    "-1,5,5",
    "Run script indices for Slashers Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Whip",
    "5,2,4",
    "Run script indices for Whip Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Pistols",
    "0,5,11",
    "Run script indices for Pistols Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Crossbow",
    "3,7,7",
    "Run script indices for Crossbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),
   

new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Longbow",
    "-1,5,7",
    "Run script indices for Longbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
),

            new ConfigEntryDefinition(
    "General",
    "Spirit_Scripts_Claws",
    "-1,5,5",
    "Run script indices for Claws Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."
)




           
            /*
            new ConfigEntryDefinition("Expertise", "MaxHealth", 250f, "The base cap for maximum health."),
            new ConfigEntryDefinition("Expertise", "MovementSpeed", 0.25f, "The base cap for movement speed."),
            new ConfigEntryDefinition("Expertise", "PrimaryAttackSpeed", 0.10f, "The base cap for primary attack speed."),
            new ConfigEntryDefinition("Expertise", "PhysicalLifeLeech", 0.10f, "The base cap for physical life leech."),
            new ConfigEntryDefinition("Expertise", "SpellLifeLeech", 0.10f, "The base cap for spell life leech."),
            new ConfigEntryDefinition("Expertise", "PrimaryLifeLeech", 0.15f, "The base cap for primary life leech."),
            new ConfigEntryDefinition("Expertise", "PhysicalPower", 20f, "The base cap for physical power."),
            new ConfigEntryDefinition("Expertise", "SpellPower", 10f, "The base cap for spell power."),
            new ConfigEntryDefinition("Expertise", "PhysicalCritChance", 0.10f, "The base cap for physical critical strike chance."),
            new ConfigEntryDefinition("Expertise", "PhysicalCritDamage", 0.50f, "The base cap for physical critical strike damage."),
            new ConfigEntryDefinition("Expertise", "SpellCritChance", 0.10f, "The base cap for spell critical strike chance."),
            new ConfigEntryDefinition("Expertise", "SpellCritDamage", 0.50f, "The base cap for spell critical strike damage."),


            new ConfigEntryDefinition("Legacies", "HealingReceived", 0.15f, "The base cap for healing received."),
            new ConfigEntryDefinition("Legacies", "DamageReduction", 0.05f, "The base cap for damage reduction."),
            new ConfigEntryDefinition("Legacies", "PhysicalResistance", 0.10f, "The base cap for physical resistance."),
            new ConfigEntryDefinition("Legacies", "SpellResistance", 0.10f, "The base cap for spell resistance."),
            new ConfigEntryDefinition("Legacies", "ResourceYield", 0.25f, "The base cap for resource yield."),
            new ConfigEntryDefinition("Legacies", "ReducedBloodDrain", 0.5f, "The base cap for reduced blood drain."),
            new ConfigEntryDefinition("Legacies", "SpellCooldownRecoveryRate", 0.10f, "The base cap for spell cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "WeaponCooldownRecoveryRate", 0.10f, "The base cap for weapon cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "UltimateCooldownRecoveryRate", 0.20f, "The base cap for ultimate cooldown recovery rate."),
            new ConfigEntryDefinition("Legacies", "MinionDamage", 0.25f, "The base cap for minion damage."),
            new ConfigEntryDefinition("Legacies", "AbilityAttackSpeed", 0.10f, "The base cap for ability attack speed."),
            new ConfigEntryDefinition("Legacies", "CorruptionDamageReduction", 0.10f, "The base cap for corruption damage reduction."),
            */

           

          
           

            

        ];
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
                        List<string> sectionKeys = [..sectionLines
                            .Where(line => line.Contains('='))
                            .Select(line => line.Split('=')[0].Trim())];

                        // Create a dictionary of default values directly from ConfigEntries
                        Dictionary<string, object> defaultValuesMap = ConfigEntries
                            .Where(entry => entry.Section == section)
                            .ToDictionary(entry => entry.Key, entry => entry.DefaultValue);

                        int keyIndex = 0;
                        bool previousLineSkipped = false; // Track whether the last line was skipped

                        foreach (var line in sectionLines)
                        {
                            if (line.Contains('='))
                            {
                                string key = line.Split('=')[0].Trim();

                                // Skip obsolete keys that are not in ConfigEntries
                                if (!defaultValuesMap.ContainsKey(key))
                                {
                                    Core.Log.LogWarning($"Skipping obsolete config entry: {key}");
                                    previousLineSkipped = true;
                                    continue;
                                }
                            }

                            // Prevent consecutive blank lines by skipping extra ones
                            if (string.IsNullOrWhiteSpace(line) && previousLineSkipped)
                            {
                                continue;
                            }

                            if (line.Contains(DEFAULT_VALUE_LINE))
                            {
                                ConfigEntryDefinition entry = ConfigEntries.FirstOrDefault(e => e.Key == sectionKeys[keyIndex] && e.Section == section);

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

                            previousLineSkipped = false; // Reset flag since we wrote a valid line
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
