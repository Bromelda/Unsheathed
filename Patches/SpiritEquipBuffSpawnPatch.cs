using System;
using System.Collections;                  // IEnumerator
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Scripting;         // ScriptSpawn, DestroyOnGameplayEvent
using ProjectM.Scripting;                  // CreateGameplayEventsOnSpawn
using ProjectM.Shared;                     // BuffUtility, UnitStatType
using Stunlock.Core;                       // PrefabGUID
using Unity.Collections;                   // Allocator
using Unity.Entities;                      // Entity, EntityManager
using Unsheathed;                          // Core, Plugin
using Unsheathed.Resources;                 // PrefabGUIDs
using Unsheathed.Services;                 // AttackSpeedCapService
using Unsheathed.Utilities;               // Buffs

namespace Unsheathed.Patches
{
    [HarmonyPatch(typeof(BuffSystem_Spawn_Server), nameof(BuffSystem_Spawn_Server.OnUpdate))]
    static class SpiritEquipBuffSpawnPatch
    {
        // Veil of Storms "attack-speed" buff GUID
        private static readonly PrefabGUID VeilStormASBuff = new PrefabGUID(-1515928707); // AB_Vampire_VeilOfStorm_Buff_AttackSpeed

        // Tunables (stay within net-rails)
        private const float PrimaryAS = 3.0f;
        private const float AbilityAS = 9.0f;

        [HarmonyPriority(Priority.Last)]
        static void Prefix(BuffSystem_Spawn_Server __instance)
        {
            var em = __instance.EntityManager;
            // Use the system's provided query (no ad-hoc queries)
            var spawned = __instance.__query_401358634_0.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < spawned.Length; i++)
                {
                    var buffEntity = spawned[i];
                    if (!em.Exists(buffEntity) || !em.HasComponent<EntityOwner>(buffEntity) || !em.HasComponent<PrefabGUID>(buffEntity))
                        continue;

                    var owner = em.GetComponentData<EntityOwner>(buffEntity).Owner;
                    if (owner == Entity.Null || !em.Exists(owner) || !em.HasComponent<PlayerCharacter>(owner))
                        continue;

                    var guid = em.GetComponentData<PrefabGUID>(buffEntity);
                    int h = guid.GuidHash;

                    // 1) Spirit equip buffs: lift caps when these spawn.
                    if (IsSpiritEquipBuff(guid))
                    {
                        AttackSpeedCapService.TryLiftCaps(em, owner);
                        continue; // equip-buffs don't need AS edits here
                    }

                    // 2) VeilStorm + your custom bonus buff(s): apply AS edits and uncap while active.
                    if (h == VeilStormASBuff.GuidHash || h == Buffs.BonusStatsBuff.GuidHash || h == Buffs.BonusPlayerStatsBuff.GuidHash)
                    {
                        bool patchedNow = false;
                        if (em.HasBuffer<ModifyUnitStatBuff_DOTS>(buffEntity))
                        {
                            var mods = em.GetBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
                            AppendOrUpdate(ref mods, UnitStatType.PrimaryAttackSpeed, PrimaryAS);
                            AppendOrUpdate(ref mods, UnitStatType.AbilityAttackSpeed, AbilityAS);
                            patchedNow = true;

                            // Strip spawn listeners next frame (after originals run once)
                            Core.StartCoroutine(RemoveSpawnScriptsNextFrame(buffEntity));
                        }
                        else
                        {
                            // Ensure buffer exists and patch next frame
                            Core.StartCoroutine(PatchNextFrame(buffEntity, PrimaryAS, AbilityAS, alsoStripSpawn: true));
                        }

                        Plugin.LogInstance.LogInfo($"[SpiritEquipBuffSpawnPatch] AS mods for {h} on buff {buffEntity.Index} (now={patchedNow}).");

                        // Lift caps while the buff is active, then restore on end (service is ref-counted & idempotent)
                        AttackSpeedCapService.TryLiftCaps(em, owner);
                        Core.StartCoroutine(RestoreCapsWhenBuffEnds(owner, guid));
                    }
                }
            }
            finally { spawned.Dispose(); }
        }

        // === Helpers ===
        static bool IsSpiritEquipBuff(PrefabGUID g)
        {
            // Weapon equip-buffs only (no AS edits here; those are handled during casting in other systems)
            return g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Sword_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Slashers_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Daggers_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Mace_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Reaper_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Spear_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Whip_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Claws_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Pistols_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_Longbow_Base.GuidHash
                 || g.GuidHash == PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base.GuidHash;
        }

        private static void AppendOrUpdate(ref DynamicBuffer<ModifyUnitStatBuff_DOTS> buf, UnitStatType type, float value)
        {
            for (int i = 0; i < buf.Length; i++)
            {
                var m = buf[i];
                if (m.StatType != type) continue;

                m.Value = value;
                m.ModificationType = ModificationType.Multiply;
                m.Modifier = 1f;
                m.AttributeCapType = AttributeCapType.Uncapped;
                // keep existing m.Id
                buf[i] = m;
                Plugin.LogInstance.LogInfo($"[AppendOrUpdate:UPDATED] {type} val={m.Value} cap={m.AttributeCapType} id={m.Id}");
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
            Plugin.LogInstance.LogInfo($"[AppendOrUpdate:ADDED] {type} val={added.Value} cap={added.AttributeCapType} id={added.Id}");
        }

        private static IEnumerator PatchNextFrame(Entity buff, float primaryAS, float abilityAS, bool alsoStripSpawn)
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

            Plugin.LogInstance.LogInfo($"[PatchNextFrame] Patched buff {buff.Index} (primary={primaryAS}, ability={abilityAS}).");
        }

        private static IEnumerator RemoveSpawnScriptsNextFrame(Entity buff)
        {
            var em = Core.EntityManager;
            yield return null; // ensure original spawn scripts ran once

            if (!em.Exists(buff)) yield break;

            if (em.HasComponent<ScriptSpawn>(buff)) em.RemoveComponent<ScriptSpawn>(buff);
            if (em.HasComponent<CreateGameplayEventsOnSpawn>(buff)) em.RemoveComponent<CreateGameplayEventsOnSpawn>(buff);
            if (em.HasComponent<GameplayEventListeners>(buff)) em.RemoveComponent<GameplayEventListeners>(buff);
            if (em.HasComponent<RemoveBuffOnGameplayEvent>(buff)) em.RemoveComponent<RemoveBuffOnGameplayEvent>(buff);
            if (em.HasComponent<RemoveBuffOnGameplayEventEntry>(buff)) em.RemoveComponent<RemoveBuffOnGameplayEventEntry>(buff);
            if (em.HasComponent<DestroyOnGameplayEvent>(buff)) em.RemoveComponent<DestroyOnGameplayEvent>(buff);

            Plugin.LogInstance.LogInfo($"[StripSpawn] Removed ScriptSpawn (+listeners) from buff {buff.Index}.");
        }

        private static IEnumerator RestoreCapsWhenBuffEnds(Entity owner, PrefabGUID buffGuid)
        {
            var em = Core.EntityManager;

            // wait until the buff is gone
            while (true)
            {
                if (!em.Exists(owner)) yield break;
                if (!BuffUtility.HasBuff(em, owner, buffGuid)) break;
                yield return null;
            }

            if (em.Exists(owner))
                AttackSpeedCapService.RestoreCaps(em, owner);
        }
    }
}



