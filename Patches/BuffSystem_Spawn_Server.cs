using System;
using System.Collections;                 // IEnumerator
using System.Collections.Generic;         // Dictionary, HashSet
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unsheathed;
using Unsheathed.Resources;
using Unsheathed.Services;

namespace Unsheathed.Patches
{
    [HarmonyPatch(typeof(ProjectM.BuffSystem_Spawn_Server), "OnUpdate")]
    public static class BuffSystem_Spawn_Server_OnUpdate_Patch
    {
        // --- Known buff GUIDs used by the allow-list builder ---
        static readonly PrefabGUID VeilStormASBuff = new PrefabGUID(-1515928707);

        static readonly Dictionary<string, PrefabGUID> EquipBuffByKey =
            new Dictionary<string, PrefabGUID>(StringComparer.OrdinalIgnoreCase)
            {
                ["Axe"]        = PrefabGUIDs.EquipBuff_Weapon_Axe_Base,
                ["Sword"]      = PrefabGUIDs.EquipBuff_Weapon_Sword_Base,
                ["Greatsword"] = PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base,
                ["TwinBlades"] = PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base,
                ["Slashers"]   = PrefabGUIDs.EquipBuff_Weapon_Slashers_Base,
                ["Daggers"]    = PrefabGUIDs.EquipBuff_Weapon_Daggers_Base,
                ["Mace"]       = PrefabGUIDs.EquipBuff_Weapon_Mace_Base,
                ["Reaper"]     = PrefabGUIDs.EquipBuff_Weapon_Reaper_Base,
                ["Spear"]      = PrefabGUIDs.EquipBuff_Weapon_Spear_Base,
                ["Whip"]       = PrefabGUIDs.EquipBuff_Weapon_Whip_Base,
                ["Claws"]      = PrefabGUIDs.EquipBuff_Weapon_Claws_Base,
                ["Longbow"]    = PrefabGUIDs.EquipBuff_Weapon_Longbow_Base,
                ["Crossbow"]   = PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base,
                ["Pistols"]    = PrefabGUIDs.EquipBuff_Weapon_Pistols_Base,
                ["FishingPole"]= PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base,
            };

        static readonly HashSet<int> _uncapBuffHashes = new HashSet<int>();
        static bool _allowlistBuilt;

        // Per-character uncap state (overlap-safe)
        class UncapInfo { public VampireAttributeCaps Caps; public int RefCount; public bool HadCaps; }
        static readonly Dictionary<Entity, UncapInfo> _uncap = new Dictionary<Entity, UncapInfo>();

        // optional one-time diag
        const bool EnableCapsDumpOnce = false;
        static bool _dumpedCaps;

        /// <summary>Rebuild the uncap allow-list from Spirit_Loadout (plus VeilStorm). Call after Spirit_ApplyAllConfigured().</summary>
        public static void RebuildUncapAllowlistFromLoadout()
        {
            _allowlistBuilt = false;
            EnsureAllowlistBuilt();
        }

        public static void InvalidateUncapAllowlist() => _allowlistBuilt = false;

        public static void AddUncapBuffGuid(PrefabGUID guid)
        {
            _uncapBuffHashes.Add(guid.GuidHash);
            Plugin.LogInstance.LogInfo($"[UncapAllowlist] Added {guid.GuidHash} at runtime.");
        }

        static void EnsureAllowlistBuilt()
        {
            if (_allowlistBuilt) return;

            _uncapBuffHashes.Clear();
            _uncapBuffHashes.Add(VeilStormASBuff.GuidHash); // always include VeilStorm

            if (ConfigService.ConfigInitialization.FinalConfigValues.TryGetValue("Spirit_Loadout", out var obj))
            {
                var loadout = Convert.ToString(obj) ?? string.Empty;
                var keys = loadout.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                foreach (var key in keys)
                    if (EquipBuffByKey.TryGetValue(key, out var equipBuff))
                        _uncapBuffHashes.Add(equipBuff.GuidHash);
            }

            _allowlistBuilt = true;
            Plugin.LogInstance.LogInfo($"[UncapAllowlist] Built from Spirit_Loadout: count={_uncapBuffHashes.Count}");
        }

        [HarmonyPrefix]
        static void Prefix(ProjectM.BuffSystem_Spawn_Server __instance)
        {
            EnsureAllowlistBuilt(); // <- you were missing this

            var em = __instance.EntityManager;
            var q  = __instance._Query;

            var entities = q.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var e = entities[i];
                    if (!em.Exists(e) || !em.HasComponent<PrefabGUID>(e)) continue;

                    var guid = em.GetComponentData<PrefabGUID>(e);
                    var h    = guid.GuidHash;

                    // 1) VeilStorm AS: edit stats (now or next frame) and strip spawn listeners next frame
                    if (h == VeilStormASBuff.GuidHash)
                    {
                        const float primaryAS = 3.0f;  // stay within net rails
                        const float abilityAS = 9.0f;  // stay within net rails

                        bool patchedNow = false;
                        if (em.HasBuffer<ModifyUnitStatBuff_DOTS>(e))
                        {
                            var mods = em.GetBuffer<ModifyUnitStatBuff_DOTS>(e);
                            AppendOrUpdate(ref mods, UnitStatType.PrimaryAttackSpeed, primaryAS);
                            AppendOrUpdate(ref mods, UnitStatType.AbilityAttackSpeed, abilityAS);
                            patchedNow = true;

                            Core.StartCoroutine(RemoveSpawnScriptsNextFrame(e));
                        }
                        else
                        {
                            Core.StartCoroutine(PatchVeilStormNextFrame(e, primaryAS, abilityAS, alsoStripSpawn: true));
                        }

                        Plugin.LogInstance.LogInfo($"[BuffSystem_Spawn_Server] AS mods for {h} on {e.Index} (now={patchedNow}).");
                    }

                    // 2) Any allow-listed buff (equip or VeilStorm): remove caps while present (ref-counted)
                    if (_uncapBuffHashes.Contains(h) && em.HasComponent<EntityOwner>(e))
                    {
                        var owner = em.GetComponentData<EntityOwner>(e).Owner;
                        if (owner != Entity.Null && em.Exists(owner) && em.HasComponent<PlayerCharacter>(owner))
                        {
                            if (_uncap.TryGetValue(owner, out var info))
                            {
                                info.RefCount++;
                                _uncap[owner] = info;
                                Plugin.LogInstance.LogInfo($"[UncapWhileBuff] ++ref on {owner.Index} -> {info.RefCount}");
                            }
                            else
                            {
                                bool hadCaps = em.HasComponent<VampireAttributeCaps>(owner);
                                var savedCaps = hadCaps ? em.GetComponentData<VampireAttributeCaps>(owner) : default;

                                if (hadCaps)
                                {
                                    em.RemoveComponent<VampireAttributeCaps>(owner);
                                    Plugin.LogInstance.LogInfo($"[UncapWhileBuff] Removed VampireAttributeCaps on char {owner.Index}.");
                                }

                                _uncap[owner] = new UncapInfo { Caps = savedCaps, RefCount = 1, HadCaps = hadCaps };
                            }

                            // watch THIS buff; when it ends, decrement & maybe restore
                            Core.StartCoroutine(RestoreCapsWhenBuffEnds(owner, guid));

                            // prevent double-strip: only strip here for equip buffs (VeilStorm already scheduled)
                            if (h != VeilStormASBuff.GuidHash)
                                Core.StartCoroutine(RemoveSpawnScriptsNextFrame(e));

                            if (EnableCapsDumpOnce && !_dumpedCaps)
                            {
                                _dumpedCaps = true;
                                DumpCapSourcesOnOwner(em, owner);
                            }
                        }
                    }
                }
            }
            finally
            {
                entities.Dispose();
            }
        }

        // === helpers ===

        static IEnumerator PatchVeilStormNextFrame(Entity buff, float primaryAS, float abilityAS, bool alsoStripSpawn)
        {
            var em = Core.EntityManager;
            yield return null; // allow BuffSystem to finish adding components

            if (!em.Exists(buff)) yield break;

            DynamicBuffer<ModifyUnitStatBuff_DOTS> mods =
                em.HasBuffer<ModifyUnitStatBuff_DOTS>(buff) ? em.GetBuffer<ModifyUnitStatBuff_DOTS>(buff)
                                                            : em.AddBuffer<ModifyUnitStatBuff_DOTS>(buff);

            AppendOrUpdate(ref mods, UnitStatType.PrimaryAttackSpeed, primaryAS);
            AppendOrUpdate(ref mods, UnitStatType.AbilityAttackSpeed, abilityAS);

            if (alsoStripSpawn)
                Core.StartCoroutine(RemoveSpawnScriptsNextFrame(buff));

            Plugin.LogInstance.LogInfo($"[PatchNextFrame] Patched AS on buff {buff.Index} (primary={primaryAS}, ability={abilityAS}).");
        }

        static IEnumerator RemoveSpawnScriptsNextFrame(Entity buff)
        {
            var em = Core.EntityManager;
            yield return null; // ensure original spawn scripts ran once

            if (!em.Exists(buff)) yield break;

            if (em.HasComponent<ScriptSpawn>(buff)) em.RemoveComponent<ScriptSpawn>(buff);
            if (em.HasComponent<CreateGameplayEventsOnSpawn>(buff)) em.RemoveComponent<CreateGameplayEventsOnSpawn>(buff);
            if (em.HasComponent<GameplayEventListeners>(buff))      em.RemoveComponent<GameplayEventListeners>(buff);
            if (em.HasComponent<RemoveBuffOnGameplayEvent>(buff))   em.RemoveComponent<RemoveBuffOnGameplayEvent>(buff);
            if (em.HasComponent<RemoveBuffOnGameplayEventEntry>(buff)) em.RemoveComponent<RemoveBuffOnGameplayEventEntry>(buff);
            if (em.HasComponent<DestroyOnGameplayEvent>(buff))      em.RemoveComponent<DestroyOnGameplayEvent>(buff);
            if (em.HasComponent<SpawnStructure_WeakenState_DataShared>(buff)) em.RemoveComponent<SpawnStructure_WeakenState_DataShared>(buff);

            Plugin.LogInstance.LogInfo($"[StripSpawn] Removed ScriptSpawn (+extras) from buff {buff.Index}.");
        }

        static void AppendOrUpdate(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buf, UnitStatType type, float value)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                var m = buf[i];
                if (m.StatType != type) continue;

                m.Value = value;
                m.ModificationType = ModificationType.Multiply;
                m.Modifier = 1f;
                m.AttributeCapType = AttributeCapType.Uncapped;

                buf[i] = m;
                Plugin.LogInstance.LogInfo($"[AppendOrUpdate:UPDATED] stat={m.StatType} val={m.Value} cap={m.AttributeCapType} id={m.Id}");
                return;
            }

            var added = new ModifyUnitStatBuff_DOTS
            {
                StatType = type,
                Value = value,
                ModificationType = ModificationType.Multiply,
                Modifier = 1f,
                AttributeCapType = AttributeCapType.Uncapped,
                Id = ModificationId.NewId(0)
            };
            buf.Add(added);
            Plugin.LogInstance.LogInfo($"[AppendOrUpdate:ADDED] stat={added.StatType} val={added.Value} cap={added.AttributeCapType} id={added.Id}");
        }

        static IEnumerator RestoreCapsWhenBuffEnds(Entity owner, PrefabGUID buffGuid)
        {
            var em = Core.EntityManager;

            while (true)
            {
                if (!em.Exists(owner)) yield break;
                if (!BuffUtility.HasBuff(em, owner, buffGuid)) break;
                yield return null;
            }

            if (!em.Exists(owner)) yield break;
            if (!_uncap.TryGetValue(owner, out var info)) yield break;

            info.RefCount--;
            Plugin.LogInstance.LogInfo($"[UncapWhileBuff] --ref on {owner.Index} -> {info.RefCount}");
            if (info.RefCount > 0) { _uncap[owner] = info; yield break; }

            _uncap.Remove(owner);
            if (info.HadCaps)
            {
                if (!em.HasComponent<VampireAttributeCaps>(owner))
                    em.AddComponentData(owner, info.Caps);
                else
                    em.SetComponentData(owner, info.Caps);

                Plugin.LogInstance.LogInfo($"[UncapWhileBuff] Restored VampireAttributeCaps on char {owner.Index}.");
            }
            else
            {
                if (em.HasComponent<VampireAttributeCaps>(owner))
                    em.RemoveComponent<VampireAttributeCaps>(owner);
            }
        }

        static void DumpCapSourcesOnOwner(EntityManager em, Entity owner)
        {
            if (!em.Exists(owner)) return;

            Plugin.LogInstance.LogInfo($"[CapsDump] Owner={owner.Index} hasCaps={em.HasComponent<VampireAttributeCaps>(owner)}");

            if (em.HasBuffer<BuffBuffer>(owner))
            {
                var buffs = em.GetBuffer<BuffBuffer>(owner);
                for (int i = 0; i < buffs.Length; i++)
                {
                    var be = buffs[i].Entity;
                    if (!em.Exists(be)) continue;

                    bool hasCaps  = em.HasComponent<VampireAttributeCaps>(be);
                    bool hasSpawn = em.HasComponent<ScriptSpawn>(be);
                    if (hasCaps || hasSpawn)
                    {
                        var p = buffs[i].PrefabGuid;
                        Plugin.LogInstance.LogInfo($"[CapsDump] BuffEntity={be.Index} guid={p.GuidHash} caps={hasCaps} spawn={hasSpawn}");
                    }
                }
            }
        }
    }
}
