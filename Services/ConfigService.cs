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
    
   

    // ==== SPIRIT — FISHING POLE ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_FishingPole",
        "primary=AB_Fishing_AbilityGroup;q=AB_Bandit_Fisherman_SpinAttack_AbilityGroup;e=AB_Bandit_Fisherman_FishHook_AbilityGroup;copy=true,true,true",
        "FishingPole Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_FishingPole",
        "-1,-5,-12",
        "Run script indices for FishingPole Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   
    // ==== SPIRIT — DAGGERS ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Daggers",
        "primary=1837385563;q=1971375367;e=211628325;copy=true,true,true",

        "Daggers Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Daggers",
        "-1,5,12",
        "Run script indices for Daggers Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — REAPER ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Reaper",
        "primary=948587795;q=-808864212;e=1691254929;copy=true,true,true",
        "Reaper Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Reaper",
        "6,5,5",
        "Run script indices for Reaper Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — MACE ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Mace",
        "primary=722599012;q=-1172099204;e=1926314891;copy=true,true,true",
        "Mace Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Mace",
        "-1,-1,5",
        "Run script indices for Mace Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — SWORD ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Sword",
        "primary=-2097352908;q=532210332;e=-1161896955;copy=true,true,true",
        "Sword Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Sword",
        "-1,8,15",
        "Run script indices for Sword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   
    // ==== SPIRIT — GREATSWORD ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_GreatSword",
        "primary=-1428882023;q=-328302080;e=-2126197617;copy=true,true,true",
        "GreatSword Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_GreatSword",
        "-1,-1,5",
        "Run script indices for GreatSword Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — SPEAR ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Spear",
        "primary=1142040823;q=381862924;e=1166337981;copy=true,true,true",
        "Spear Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Spear",
        "-1,2,11",
        "Run script indices for Spear Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — TWINBLADES ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_TwinBlades",
        "primary=298784800;q=-2023636973;e=-1357375516;copy=true,true,true",
        "TwinBlades Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_TwinBlades",
        "-1,-1,6",
        "Run script indices for TwinBlades Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — SLASHERS ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Slashers",
        "primary=-186690512;q=-198012170;e=2019689688;copy=true,true,true",
        "Slashers Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Slashers",
        "-1,5,5",
        "Run script indices for Slashers Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
    

    // ==== SPIRIT — WHIP ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Whip",
        "primary=112329675;q=-1111373807;e=2130985273;copy=true,true,true",
        "Whip Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Whip",
        "5,2,4",
        "Run script indices for Whip Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — PISTOLS ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Pistols",
        "primary=1622839653;q=-1884688827;e=-526118698;copy=true,true,true",
        "Pistols Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Pistols",
        "0,5,11",
        "Run script indices for Pistols Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — CROSSBOW ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Crossbow",
        "primary=1232856473;q=76767983;e=-444905742;copy=true,true,true",
        "Crossbow Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Crossbow",
        "10,5,9",
        "Run script indices for Crossbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
  
    // ==== SPIRIT — LONGBOW ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Longbow",
        "primary=617823552;q=766284586;e=501615608;copy=true,true,true",
        "Longbow Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Longbow",
        "-1,5,7",
        "Run script indices for Longbow Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),
   

    // ==== SPIRIT — CLAWS ====
    new ConfigEntryDefinition(
        "General", "Spirit_Groups_Claws",
        "primary=1180130515;q=-1085783726;e=-2096054164;copy=true,true,true",
        "Claws Prefab ability Format"),
    new ConfigEntryDefinition(
        "General", "Spirit_Scripts_Claws",
        "-1,5,5",
        "Run script indices for Claws Spirit abilities as 'P,Q,E'. Use -1 to skip a slot."),




   // ==== SPIRIT BUFFS — PER-WEAPON ====
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_FishingPole",
    "primary=;q=;e=",
    "Buffs to apply during casts for FishingPole (primary/q/e). Empty disables."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Daggers",
    "primary=-1515928707;q=-1515928707;e=-1515928707",
    "Buffs to apply during casts for Daggers (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Reaper",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Reaper (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Mace",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Mace (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Sword",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Sword (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_GreatSword",
    "primary=AB_GoldGolem_Enrage_Buff;q=AB_GoldGolem_Enrage_Buff;e=AB_GoldGolem_Enrage_Buff",
    "Buffs to apply during casts for GreatSword (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Spear",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Spear (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_TwinBlades",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for TwinBlades (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Slashers",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Slashers (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Whip",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Whip (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Pistols",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Pistols (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Crossbow",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Crossbow (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Longbow",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Longbow (primary/q/e)."),
new ConfigEntryDefinition(
    "General", "Spirit_Buffs_Claws",
    "primary=AB_GoldGolem_Enrage_Buff;q=;e=",
    "Buffs to apply during casts for Claws (primary/q/e).")

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
