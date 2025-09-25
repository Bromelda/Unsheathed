
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx;
using ProjectM;
using Stunlock.Core;
using Unsheathed.Resources;   // PrefabGUIDs names
using Unsheathed.Services;    // ConfigService

namespace Unsheathed.Utilities
{
    // Make it public so Core can call into it
    public static class Configuration
    {
        // -------- Small helpers --------

        public static List<int> ParseIntegersFromString(string configString)
        {
            var list = new List<int>();
            if (string.IsNullOrWhiteSpace(configString)) return list;
            foreach (var part in configString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                    list.Add(v);
            }
            return list;
        }

        public static List<T> ParseEnumsFromString<T>(string configString) where T : struct, Enum
        {
            var list = new List<T>();
            if (string.IsNullOrWhiteSpace(configString)) return list;

            foreach (var part in configString.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<T>(part, true, out var v))
                    list.Add(v);
            }
            return list;
        }

        // -------- Prefab name/int -> PrefabGUID map --------

        static readonly Lazy<Dictionary<string, PrefabGUID>> _prefabMap = new Lazy<Dictionary<string, PrefabGUID>>(() =>
        {
            var dict = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);

            // Reflect public static PrefabGUID fields on PrefabGUIDs
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
                Core.Log.LogWarning($"[Spirit] PrefabGUIDs reflection failed: {ex.Message}");
            }

            // Optional: merge PrefabIndex.json from the plugin’s config folder
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
                Core.Log.LogWarning($"[Spirit] PrefabIndex.json load failed (optional): {ex.Message}");
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

            Core.Log.LogWarning($"[Spirit] Unknown prefab token '{token}'.");
            return false;
        }








        // -------- Spirit buff config (per-weapon Primary/Q/E) --------
        public struct SpiritBuffSlots
        {
            public PrefabGUID Primary;
            public PrefabGUID Q;
            public PrefabGUID E;
            public bool HasPrimary;
            public bool HasQ;
            public bool HasE;
        }

        static bool TryParseSpiritBuffs(string text, out SpiritBuffSlots slots)
        {
            slots = default;
            if (string.IsNullOrWhiteSpace(text)) return false;

            // primary=...;q=...;e=...
            var parts = text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string p = null, q = null, e = null;

            foreach (var part in parts)
            {
                var kv = part.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2) continue;

                switch (kv[0].ToLowerInvariant())
                {
                    case "primary": p = kv[1]; break;
                    case "q": q = kv[1]; break;
                    case "e": e = kv[1]; break;
                }
            }

            // tokens are optional: set Has* only if they parse
            if (!string.IsNullOrWhiteSpace(p) && TryParsePrefab(p, out slots.Primary)) slots.HasPrimary = true;
            if (!string.IsNullOrWhiteSpace(q) && TryParsePrefab(q, out slots.Q)) slots.HasQ       = true;
            if (!string.IsNullOrWhiteSpace(e) && TryParsePrefab(e, out slots.E)) slots.HasE       = true;

            return slots.HasPrimary || slots.HasQ || slots.HasE;
        }

        public static bool TryGetSpiritBuffs(string weaponKey, out SpiritBuffSlots slots)
        {
            slots = default;
            if (string.IsNullOrWhiteSpace(weaponKey)) return false;

            // Honor Spirit_Loadout just like groups do
            if (ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue("Spirit_Loadout", out var loadoutObj))
            {
                var loadoutStr = Convert.ToString(loadoutObj) ?? "";
                if (!string.IsNullOrWhiteSpace(loadoutStr))
                {
                    var list = loadoutStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (!list.Any(x => string.Equals(x, weaponKey, StringComparison.OrdinalIgnoreCase)))
                        return false;
                }
            }

            var key = $"Spirit_Buffs_{weaponKey}";
            if (!ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(key, out var obj)) return false;
            var text = Convert.ToString(obj) ?? "";
            return TryParseSpiritBuffs(text, out slots);
        }



































        // -------- Spirit group config (per-weapon Primary/Q/E) --------

        public struct SpiritSlots
        {
            public PrefabGUID Primary;
            public PrefabGUID Q;
            public PrefabGUID E;
            public bool CopyP;
            public bool CopyQ;
            public bool CopyE;
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

            slots.CopyP = slots.CopyQ = slots.CopyE = true; // defaults
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

            // Optional: filter by Spirit_Loadout list
            if (ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue("Spirit_Loadout", out var loadoutObj))
            {
                var loadoutStr = Convert.ToString(loadoutObj) ?? "";
                if (!string.IsNullOrWhiteSpace(loadoutStr))
                {
                    var list = loadoutStr.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    if (!list.Any(x => string.Equals(x, weaponKey, StringComparison.OrdinalIgnoreCase)))
                        return false;
                }
            }

            var key = $"Spirit_Groups_{weaponKey}";
            if (!ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(key, out var obj)) return false;

            var text = Convert.ToString(obj) ?? "";
            return TryParseSpiritGroups(text, out slots);
        }

        // Parses: [General] Spirit_Scripts_<Weapon> = "P,Q,E"
        // Returns false if key missing/blank/malformed. Use -1 to skip a slot.
        public static bool TryGetSpiritScriptIndices(string weaponKey, out int p, out int q, out int e)
        {
            p = q = e = -1;
            if (string.IsNullOrWhiteSpace(weaponKey)) return false;

            if (!ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue($"Spirit_Scripts_{weaponKey}", out var obj))
                return false;

            var text = Convert.ToString(obj) ?? "";
            if (string.IsNullOrWhiteSpace(text)) return false;

            var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            var ok =
                int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out p) &
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out q) &
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out e);

            return ok;
        }

       
        }
    }






