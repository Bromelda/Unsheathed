using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unsheathed.Resources;
using Unsheathed.Services;
using Unsheathed.Utilities;
using System.Reflection;





namespace Unsheathed.Patches;



[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _weapons = ConfigService.WeaponsSystem;

    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> WeaponsSpells => _weaponsSpells;
    static readonly Dictionary<PrefabGUID, int> _weaponsSpells = new Dictionary<PrefabGUID, int>();










    // Ability group -> Buff prefab to apply during cast
    static readonly Dictionary<PrefabGUID, PrefabGUID> _slotBuffs = new();

    public static void RegisterWeaponSlotBuff(PrefabGUID abilityGroup, PrefabGUID buffPrefab)
    {
        if (abilityGroup.GuidHash != 0 && buffPrefab.GuidHash != 0)
            _slotBuffs[abilityGroup] = buffPrefab;
    }


    // Duration (seconds) that slot 0/1/4 cast-buffs should persist after the cast starts.
    // Set to <= 0 to revert to the old "remove at endcast" behavior.
    static readonly float SlotBuffDurationSeconds = 1.0f;










































    static void CastLog(string msg)
    {
        if (!ConfigService.Debug_CastTweaks) return;
        Plugin.LogInstance.LogInfo(msg);
    }





    struct CastTimeOverride
    {
        public PrefabGUID Cast;
        public float CastTime;
        public float? PostCastTime;
    }

    static readonly Lazy<Dictionary<PrefabGUID, CastTimeOverride>> _castOverrides =
        new(() =>
        {
            var merged = new Dictionary<PrefabGUID, CastTimeOverride>();

            void Merge(string raw)
            {
                foreach (var kv in ParseCastTimeOverrides(raw))
                    merged[kv.Key] = kv.Value; // later merges win on duplicates
            }

            // 1) optional global/misc first
            Merge(ConfigService.CastTimeOverrides);

            // 2) per-weapon buckets (add/remove to match your keys)
            Merge(ConfigService.CastTimeOverrides_FishingPole);
            Merge(ConfigService.CastTimeOverrides_Dagger);
            Merge(ConfigService.CastTimeOverrides_Reaper);
            Merge(ConfigService.CastTimeOverrides_Mace);
            Merge(ConfigService.CastTimeOverrides_Sword);
            Merge(ConfigService.CastTimeOverrides_Greatsword);
            Merge(ConfigService.CastTimeOverrides_Spear);
            Merge(ConfigService.CastTimeOverrides_TwinBlades);
            Merge(ConfigService.CastTimeOverrides_Slashers);
            Merge(ConfigService.CastTimeOverrides_Whip);
            Merge(ConfigService.CastTimeOverrides_Pistols);
            Merge(ConfigService.CastTimeOverrides_Crossbow);
            Merge(ConfigService.CastTimeOverrides_Bow);
            Merge(ConfigService.CastTimeOverrides_Claws);
            Merge(ConfigService.CastTimeOverrides_Axe);
            // (if you add FishingPole, etc., merge them here too)

            CastLog($"[CastTimeTweaks] Merged {merged.Count} cast-time override(s) from all buckets.");
            return merged;

        });
    // Put this in AbilityRunScriptsSystemPatch.cs (below _castOverrides)
    static Dictionary<PrefabGUID, CastTimeOverride> ParseCastTimeOverrides(string raw)
    {
        var map = new Dictionary<PrefabGUID, CastTimeOverride>();
        if (string.IsNullOrWhiteSpace(raw)) return map;

        // split by ; | or newline
        var entries = raw.Split(new[] { ';', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var entry in entries)
        {
            // <Group>=<Cast>,cast=<s>[,post=<s>]
            var line = entry.Trim();
            if (line.Length == 0) continue;
            line = line.Trim('"').Trim();
            if (line.EndsWith(",")) line = line[..^1];

            var parts = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            // left side must contain Group=Cast
            var kv = parts[0].Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (kv.Length != 2)
            {
                Plugin.LogInstance.LogWarning($"[CastTimeTweaks] Bad entry '{entry}'");
                continue;
            }

            var groupToken = kv[0].Trim();
            var castToken = kv[1].Trim();

            // parse times
            float? castTime = null;
            float? postTime = null;
            for (int i = 1; i < parts.Length; i++)
            {
                var p = parts[i].Trim();
                if (p.StartsWith("cast=", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(p.AsSpan(5), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var ct))
                        castTime = ct;
                }
                else if (p.StartsWith("post=", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(p.AsSpan(5), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var pt))
                        postTime = pt;
                }
            }

            if (castTime == null)
            {
                CastLog($"[CastTimeTweaks] Loaded {map.Count} cast-time override(s) from config.");

            }

            var groupGuid = ResolvePrefabGUID(groupToken);
            var castGuid = ResolvePrefabGUID(castToken);

            map[groupGuid] = new CastTimeOverride
            {
                Cast         = castGuid,
                CastTime     = MathF.Max(0f, castTime.Value),
                PostCastTime = postTime.HasValue ? MathF.Max(0f, postTime.Value) : (float?)null
            };
        }

        CastLog($"[CastTimeTweaks] Loaded {map.Count} cast-time override(s) from config.");
        return map;
    }

    // Keep this helper with it
    static PrefabGUID ResolvePrefabGUID(string token)
    {
        token = token.Trim();

        // numeric
        if (int.TryParse(token, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out var id))
            return new PrefabGUID(id);

        // symbolic PrefabGUIDs.AB_* field
        var f = typeof(PrefabGUIDs).GetField(token, BindingFlags.Public | BindingFlags.Static);
        if (f != null && f.FieldType == typeof(PrefabGUID))
            return (PrefabGUID)f.GetValue(null);

        throw new FormatException($"Could not resolve PrefabGUID from '{token}' (not an int, and not a PrefabGUIDs field).");
    }








    // Scan the owner's AttachedBuffer for the spawned cast entity and write back
    static bool TryModifyPlayerAbilityCastEntity(EntityManager em, Entity owner, PrefabGUID castGuid, float newCastTime, float? newPostCastTime = null)
    {
        if (!em.Exists(owner) || !em.HasBuffer<AttachedBuffer>(owner)) return false;

        var attached = em.GetBuffer<AttachedBuffer>(owner).AsNativeArray();
        for (int i = 0; i < attached.Length; i++)
        {
            var e = attached[i].Entity;
            var guid = attached[i].PrefabGuid;

            if (!guid.Equals(castGuid)) continue;
            if (!em.Exists(e)) continue;
            if (!em.HasComponent<AbilityCastTimeData>(e)) continue;

            var data = em.GetComponentData<AbilityCastTimeData>(e);

            // Set MaxCastTime
            TrySetModFloatField(ref data, "MaxCastTime", newCastTime);

            // Also set PostCastTime (if provided; pass same as cast to lock them together)
            if (newPostCastTime.HasValue)
                TrySetModFloatField(ref data, "PostCastTime", newPostCastTime.Value);

            em.SetComponentData(e, data);

            CastLog($"[CastTimeTweaks] owner={owner.Index} cast={guid.GuidHash} MaxCastTime={newCastTime}" +
 (newPostCastTime.HasValue ? $" PostCastTime={newPostCastTime.Value}" : ""));
            return true;
        }
        return false;
    }









    static readonly string[] CastTimeFieldCandidates =
 {
    "MaxCastTime", "CastTime", "CastDuration", "Duration", "TimeToComplete", "Time", "Value", "PostCastTime"
};

    static bool TrySetCastTimeField(ref AbilityCastTimeData data, float value)
    {
        var t = typeof(AbilityCastTimeData);

        for (int i = 0; i < CastTimeFieldCandidates.Length; i++)
        {
            var name = CastTimeFieldCandidates[i];
            var fi = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi == null) continue;

            // Case A: plain float field
            if (fi.FieldType == typeof(float))
            {
                object boxed = data;              // box the struct
                fi.SetValue(boxed, value);        // set value on boxed copy
                data = (AbilityCastTimeData)boxed; // unbox back
                Plugin.LogInstance.LogInfo($"[CastTimeTweaks] Set AbilityCastTimeData.{name} (float) = {value}");
                return true;
            }

            // Case B: ModifiableFloat (or similar) container
            if (fi.FieldType.Name == "ModifiableFloat" || fi.FieldType.FullName?.EndsWith(".ModifiableFloat") == true)
            {
                object outerBoxed = data;                          // box parent struct
                object modf = fi.GetValue(outerBoxed);       // get boxed ModifiableFloat

                if (TrySetModifiableFloat(ref modf, value))
                {
                    // put the modified ModifiableFloat back into the parent
                    fi.SetValue(outerBoxed, modf);
                    data = (AbilityCastTimeData)outerBoxed;        // unbox parent back
                    Plugin.LogInstance.LogInfo($"[CastTimeTweaks] Set AbilityCastTimeData.{name} (ModifiableFloat) = {value}");
                    return true;
                }
                else
                {
                    CastLog($"[CastTimeTweaks] Could not set inner fields on {name} (ModifiableFloat).");
                }
            }
        }

        return false;
    }

    // Tweaks any common float slots inside ProjectM.ModifiableFloat
    static bool TrySetModFloatField(ref AbilityCastTimeData data, string fieldName, float value)
    {
        var t = typeof(AbilityCastTimeData);
        var fi = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fi == null) return false;

        // A) plain float field
        if (fi.FieldType == typeof(float))
        {
            object boxed = data;
            fi.SetValue(boxed, value);
            data = (AbilityCastTimeData)boxed;
            CastLog($"[CastTimeTweaks] {fieldName} (float) = {value}");
            return true;
        }

        // B) ModifiableFloat (e.g., ProjectM.ModifiableFloat)
        if (fi.FieldType.Name == "ModifiableFloat" || fi.FieldType.FullName?.EndsWith(".ModifiableFloat") == true)
        {
            object parent = data;                // box parent struct
            object mf = fi.GetValue(parent); // boxed ModifiableFloat

            if (TrySetModifiableFloat(ref mf, value))
            {
                fi.SetValue(parent, mf);         // write back the modified ModifiableFloat
                data = (AbilityCastTimeData)parent;
                CastLog($"[CastTimeTweaks] {fieldName} (ModifiableFloat) = {value}");
                return true;
            }
        }

        return false;
    }

    static bool TrySetModifiableFloat(ref object boxedModifiableFloat, float value)
    {
        var mt = boxedModifiableFloat.GetType();
        bool touched = false;

        // Prefer BaseValue/Value if present
        var baseF = mt.GetField("BaseValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (baseF != null && baseF.FieldType == typeof(float))
        {
            baseF.SetValue(boxedModifiableFloat, value);
            touched = true;
            CastLog($"    -> {mt.Name}.BaseValue = {value}");
        }

        var valF = mt.GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (valF != null && valF.FieldType == typeof(float))
        {
            valF.SetValue(boxedModifiableFloat, value);
            touched = true;
            CastLog($"    -> {mt.Name}.Value = {value}");
        }

        // Fallback: any float *Value field
        if (!touched)
        {
            foreach (var f in mt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(float) && f.Name.IndexOf("Value", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    f.SetValue(boxedModifiableFloat, value);
                    touched = true;
                      CastLog($"    -> {mt.Name}.Value = {value}");
                }
            }
        }

        // Last resort: first float field
        if (!touched)
        {
            foreach (var f in mt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.FieldType == typeof(float))
                {
                    f.SetValue(boxedModifiableFloat, value);
                    touched = true;
                    CastLog($"    -> {mt.Name}.{f.Name} = {value} (fallback)");
                    break;
                }
            }
        }
        return touched;
    }

    // (Optional) deeper dump to see inside ModifiableFloat
    static void DumpAbilityCastTimeData(in AbilityCastTimeData data)
    {
        var t = typeof(AbilityCastTimeData);
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var f in fields)
        {
            object parentBox = data;
            object v = f.GetValue(parentBox);

            if (v == null)
            {
                CastLog($"[AbilityCastTimeData] {f.FieldType.Name} {f.Name} = <null>");
                continue;
            }

            if (f.FieldType.Name == "ModifiableFloat" || f.FieldType.FullName?.EndsWith(".ModifiableFloat") == true)
            {
                CastLog($"[AbilityCastTimeData] {f.FieldType.Name} {f.Name}:");
                var mt = v.GetType();
                var mf = mt.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var inner in mf)
                {
                    if (inner.FieldType == typeof(float))
                    {
                        CastLog($"    float {inner.Name} = {inner.GetValue(v)}");
                    }
                    else
                    {
                        // print a hint without spamming huge objects
                        if (inner.FieldType.IsValueType || inner.FieldType == typeof(string))
                            CastLog($"    {inner.FieldType.Name} {inner.Name} = {inner.GetValue(v)}");
                        else
                            CastLog($"    {inner.FieldType.Name} {inner.Name} = <{inner.FieldType.Name}>");
                    }
                }
            }
            else
            {
                CastLog($"[AbilityCastTimeData] {f.FieldType.Name} {f.Name} = {v}");
            }
        }
    }



















































    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        // Use your repo’s init guard (you can swap to GameSystems.Initialized if you actually have that symbol)
        if (!Core._initialized) return;
        if (!ConfigService.SpiritArsenal || !ConfigService.WeaponsSystem) return;

        var castStartedEvents = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);       // First event
        var preCastFinishedEvents = __instance._OnPreCastFinishedQuery.ToEntityArray(Allocator.Temp);   // Second event
        var postCastEndedEvents = __instance._OnPostCastEndedQuery.ToEntityArray(Allocator.Temp);     // Third event
        var interruptedEvents = __instance._OnInterruptedQuery.ToEntityArray(Allocator.Temp);
        var postCastFinishedEvents = __instance._OnPostCastFinishedQuery.ToEntityArray(Allocator.Temp);
        var castEndedEvents = __instance._OnCastEndedQuery.ToEntityArray(Allocator.Temp);
        try
        {
            var em = __instance.EntityManager;
            HandleCastStarted(em, castStartedEvents);
            HandlePreCastFinished(em, preCastFinishedEvents);
            HandlePostCastEnded(em, postCastEndedEvents);    // keep your cooldown logic here
           
            HandleCastEnded(em, castEndedEvents);               // …remove here if you want longest window
            HandleInterrupted(em, interruptedEvents);
        }
        finally
        {
            castStartedEvents.Dispose();
            preCastFinishedEvents.Dispose();
            postCastEndedEvents.Dispose();
            postCastFinishedEvents.Dispose();
            castEndedEvents.Dispose();
            interruptedEvents.Dispose();
        }
    }

    // --- Handlers (add/extend as you implement your per-ability/per-weapon logic) ---

    static void HandleCastStarted(EntityManager em, NativeArray<Entity> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityCastStartedEvent>(e)) continue;

            var ev = em.GetComponentData<AbilityCastStartedEvent>(e);
            if (ev.AbilityGroup == Entity.Null || ev.Character == Entity.Null) continue;
            if (!em.Exists(ev.AbilityGroup) || !em.Exists(ev.Character)) continue;
            if (em.HasComponent<VBloodAbilityData>(ev.AbilityGroup)) continue;
            if (!ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();

            var map = _castOverrides.Value;
            if (!map.TryGetValue(groupGuid, out var cfg)) continue;

            // modify the spawned _Cast entity on the owner
            if (TryModifyPlayerAbilityCastEntity(em, ev.Character, cfg.Cast, cfg.CastTime, cfg.PostCastTime))
            {
                CastLog($"[CastTimeTweaks] Applied Cast={cfg.Cast.GuidHash} cast={cfg.CastTime}" +
                (cfg.PostCastTime.HasValue ? $" post={cfg.PostCastTime.Value}" : "") +
                    $" for group={groupGuid.GuidHash} owner={ev.Character.Index}");

                if (_slotBuffs.TryGetValue(groupGuid, out var buffGuid))
                {
                    if (SlotBuffDurationSeconds > 0f)
                        ev.Character.TryApplyBuffWithLifeTimeDestroy(buffGuid, SlotBuffDurationSeconds);
                     else
                        ev.Character.TryApplyBuffWithLifeTimeNone(buffGuid);
                }
            }
        }
    }
    

    static void HandlePreCastFinished(EntityManager em, NativeArray<Entity> events)
    {
       
        
    }


    // In HandlePostCastEnded(...), remove the temporary speed:
    static void HandlePostCastEnded(EntityManager em, NativeArray<Entity> events)
    {
        // keep your existing cooldown work:
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityPostCastEndedEvent>(e)) continue;

            var ev = em.GetComponentData<AbilityPostCastEndedEvent>(e);
            if (ev.AbilityGroup == Entity.Null || ev.Character == Entity.Null) continue;
            if (!em.Exists(ev.AbilityGroup) || !em.Exists(ev.Character)) continue;
            if (em.HasComponent<VBloodAbilityData>(ev.AbilityGroup)) continue;
            if (!ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();

            // your original cooldown tweak
            if (WeaponsSpells != null && WeaponsSpells.ContainsKey(groupGuid))
            {
                float cooldown = WeaponsSpells[groupGuid] == 0 ? 1f : (WeaponsSpells[groupGuid] + 1) * 1f;
                ServerGameManager.SetAbilityGroupCooldown(ev.Character, groupGuid, cooldown);
            }

           
            }
        }



    static void HandleCastEnded(EntityManager em, NativeArray<Entity> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityCastEndedEvent>(e)) continue;

            var ev = em.GetComponentData<AbilityCastEndedEvent>(e);
            if (ev.Character == Entity.Null || ev.AbilityGroup == Entity.Null) continue;
            if (!em.Exists(ev.Character) || !em.Exists(ev.AbilityGroup)) continue;
            if (!ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();
            if (SlotBuffDurationSeconds <= 0f &&
 _slotBuffs.TryGetValue(groupGuid, out var buffGuid))
            {
                ev.Character.TryRemoveBuff(buffGuid);
            }
        }
    }


        // Also clear on interrupt:
        static void HandleInterrupted(EntityManager em, NativeArray<Entity> events)
        {
            for (int i = 0; i < events.Length; i++)
            {
                var e = events[i];
                if (!em.Exists(e) || !em.HasComponent<AbilityInterruptedEvent>(e)) continue;

                var ev = em.GetComponentData<AbilityInterruptedEvent>(e);
                if (ev.AbilityGroup == Entity.Null || ev.Character == Entity.Null) continue;
                if (!em.Exists(ev.AbilityGroup) || !em.Exists(ev.Character)) continue;
                if (!ev.Character.IsPlayer()) continue;

                var groupGuid = ev.AbilityGroup.GetPrefabGuid();
                if (SlotBuffDurationSeconds <= 0f &&
     _slotBuffs.TryGetValue(groupGuid, out var buffGuid))
                {
                    ev.Character.TryRemoveBuff(buffGuid);
                }
            }
        }
    
    


    public static void AddWeaponsSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponsSpells.TryAdd(prefabGuid, spellIndex);
    }
   


}