
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
using System.Reflection;

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

             if (!TryResolvePrefab(p, out slots.Primary)) return false;
             if (!TryResolvePrefab(q, out slots.Q)) return false;
             if (!TryResolvePrefab(e, out slots.E)) return false;

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























        // -------- Prefab name/int -> PrefabGUID map + resolver --------
        static readonly Lazy<Dictionary<string, PrefabGUID>> _prefabMap
            = new(() =>
            {
                var dict = new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase);

                // 1) Reflect your generated constants in Unsheathed.Resources.PrefabGUIDs
                try
                {
                    var flags = BindingFlags.Public | BindingFlags.Static;
                    foreach (var f in typeof(PrefabGUIDs).GetFields(flags))
                    {
                        if (f.FieldType == typeof(PrefabGUID))
                        {
                            var name = f.Name;
                            var guid = (PrefabGUID)f.GetValue(null);
                            if (!string.IsNullOrWhiteSpace(name))
                                dict[name] = guid;
                        }
                    }
                }
                catch { /* keep going; reflection is best-effort */ }

                // 2) Optionally merge PrefabIndex.json if present
                try
                {
                    var candidates = new[]
            {
            Path.Combine(Paths.ConfigPath, "PrefabIndex.json"),
            Path.Combine(Paths.PluginPath, "PrefabIndex.json"),
                };
                    var path = candidates.FirstOrDefault(File.Exists);
                    if (path != null)
                    {
                        var json = File.ReadAllText(path);
                        // The JSON is a simple string->int map
                        var map = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                        if (map != null)
                        {
                            foreach (var kv in map)
                            {
                                var k = kv.Key?.Trim();
                                if (string.IsNullOrEmpty(k)) continue;
                                if (k.StartsWith("!")) continue; // treat "! ..." entries as comments
                                dict[k] = new PrefabGUID(kv.Value);
                            }
                        }
                    }
                }
                catch { /* ignore, stay robust */ }

                return dict;
            });

        public static bool TryResolvePrefab(string nameOrId, out PrefabGUID guid)
        {
            guid = default;
            if (string.IsNullOrWhiteSpace(nameOrId)) return false;

            var s = nameOrId.Trim();

            // A) direct name lookup (case-insensitive)
            if (_prefabMap.Value.TryGetValue(s, out guid))
                return true;

            // B) allow "Namespace.Class.Field" by taking last segment
            var lastDot = s.LastIndexOf('.');
            if (lastDot >= 0)
            {
                var tail = s[(lastDot + 1)..];
                if (_prefabMap.Value.TryGetValue(tail, out guid))
                    return true;
            }

            // C) numeric id (decimal or 0xHEX), supports negative values
            //    examples: "123456789", "-123", "0x7F3A112B"
            if (TryParseIntFlexible(s, out var id))
            {
                guid = new PrefabGUID(id);
                return true;
            }

            return false;
        }

        static bool TryParseIntFlexible(string s, out int value)
        {
            // hex?
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && int.TryParse(s[2..],
                System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out value))
                return true;

            // decimal (allow leading + or -)
            return int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        public static bool TryGetSpiritSpeeds(string weaponKey, out float p, out float q, out float e)
        {
            p = q = e = 0f;
            var key = $"Spirit_Speeds_{weaponKey}";
            if (!Services.ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue(key, out var raw)) return false;

            var s = Convert.ToString(raw) ?? "";
            var parts = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            float Parse(int i) => (i < parts.Length && float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) ? f : 0f;

            p = Parse(0); q = Parse(1); e = Parse(2);
            return (p > 0f) || (q > 0f) || (e > 0f);
        }

       










    }
}






