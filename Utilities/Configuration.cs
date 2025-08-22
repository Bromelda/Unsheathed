
using Unsheathed.Patches;
using Unsheathed.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

using BepInEx;

using Unsheathed.Resources; // for PrefabGUIDs names


using ProjectM;
using Stunlock.Core;

namespace Unsheathed.Utilities;
internal static class Configuration
{

    public static List<int> ParseIntegersFromString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }

        return [.. configString.Split(',').Select(int.Parse)];
    }
    public static List<T> ParseEnumsFromString<T>(string configString) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(configString))
            return [];

        List<T> result = [];

        foreach (var part in configString.Split(','))
        {
            if (Enum.TryParse<T>(part.Trim(), ignoreCase: true, out var value))
            {
                result.Add(value);
            }
        }

        return result;
    }


   


    // === Spirit config support ===
    public struct SpiritSlots
    {
        public PrefabGUID Primary;
        public PrefabGUID Q;
        public PrefabGUID E;
        public bool CopyP;
        public bool CopyQ;
        public bool CopyE;
    }

    static readonly Lazy<Dictionary<string, PrefabGUID>> _prefabMap = new(() =>
    {
        var dict = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);

        // Reflect all public static PrefabGUID fields on PrefabGUIDs
        try
        {
            foreach (var f in typeof(PrefabGUIDs).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (f.FieldType == typeof(PrefabGUID))
                {
                    var val = (PrefabGUID)f.GetValue(null);
                    dict[f.Name] = val;
                }
            }
        }
        catch (Exception ex)
        {
            global::Unsheathed.Core.Log.LogWarning($"[Spirit] PrefabGUIDs reflection failed: {ex.Message}");
        }

        // Optional: merge PrefabIndex.json if present next to the plugin config
        try
        {
            var path = Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME, "PrefabIndex.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var tmp = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                if (tmp != null)
                {
                    foreach (var kv in tmp)
                        if (!dict.ContainsKey(kv.Key))
                            dict[kv.Key] = new PrefabGUID(kv.Value);
                }
            }
        }
        catch (Exception ex)
        {
            global::Unsheathed.Core.Log.LogWarning($"[Spirit] PrefabIndex.json load failed (optional): {ex.Message}");
        }

        return dict;
    });

    static bool TryParsePrefab(string token, out PrefabGUID guid)
    {
        guid = default;
        if (string.IsNullOrWhiteSpace(token)) return false;

        // raw int?
        if (int.TryParse(token.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
        {
            guid = new PrefabGUID(i);
            return true;
        }

        // name lookup
        if (_prefabMap.Value.TryGetValue(token.Trim(), out guid)) return true;

        global::Unsheathed.Core.Log.LogWarning($"[Spirit] Unknown prefab token '{token}'.");
        return false;
    }

    static bool TryParseSpiritGroups(string text, out SpiritSlots slots)
    {
        slots = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        // primary=...;q=...;e=...;copy=true,true,true
        var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string p = null, q = null, e = null, copy = null;

        foreach (var part in parts)
        {
            var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2) continue;

            switch (kv[0].ToLowerInvariant())
            {
                case "primary": p = kv[1]; break;
                case "q": q = kv[1]; break;
                case "e": e = kv[1]; break;
                case "copy": copy = kv[1]; break;
            }
        }

        if (!TryParsePrefab(p, out slots.Primary)) return false;
        if (!TryParsePrefab(q, out slots.Q)) return false;
        if (!TryParsePrefab(e, out slots.E)) return false;

        slots.CopyP = slots.CopyQ = slots.CopyE = true; // default copy flags
        if (!string.IsNullOrWhiteSpace(copy))
        {
            var c = copy.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (c.Length == 3)
            {
                bool.TryParse(c[0], out slots.CopyP);
                bool.TryParse(c[1], out slots.CopyQ);
                bool.TryParse(c[2], out slots.CopyE);
            }
        }

        return true;
    }

    public static bool TryGetSpiritGroups(string weaponKey, out SpiritSlots slots)
    {
        slots = default;
        if (string.IsNullOrWhiteSpace(weaponKey)) return false;

        // Respect optional loadout filter
        if (Services.ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue("Spirit_Loadout", out var loadoutObj))
        {
            var loadoutStr = Convert.ToString(loadoutObj) ?? "";
            if (!string.IsNullOrWhiteSpace(loadoutStr))
            {
                var list = loadoutStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (!list.Any(x => string.Equals(x, weaponKey, StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
        }

        string key = $"Spirit_Groups_{weaponKey}";
        if (!Services.ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(key, out var obj)) return false;

        var text = Convert.ToString(obj) ?? "";
        return TryParseSpiritGroups(text, out slots);
    }
    // === /Spirit config support ===
    // Parses: [General] Spirit_Scripts_<Weapon> = "P,Q,E"
    // Returns false if key missing/blank/malformed. Use -1 to skip a slot.
    public static bool TryGetSpiritScriptIndices(string weaponKey, out int p, out int q, out int e)
    {
        p = q = e = -1;
        if (string.IsNullOrWhiteSpace(weaponKey)) return false;

        if (!Services.ConfigService.ConfigInitialization.FinalConfigValues
                .TryGetValue($"Spirit_Scripts_{weaponKey}", out var obj))
            return false;

        var text = Convert.ToString(obj) ?? "";
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3) return false;

        // allow negatives to mean "skip"
        bool ok = int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out p)
               &  int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out q)
               &  int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out e);
        return ok;
    }

}
    

    

