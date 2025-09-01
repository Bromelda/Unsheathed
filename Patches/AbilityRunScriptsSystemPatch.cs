using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unsheathed.Resources;
using Unsheathed.Services;
using Unsheathed.Utilities;





namespace Unsheathed.Patches;



[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _weapons = ConfigService.WeaponsSystem;

    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> WeaponsSpells => _weaponsSpells;
    static readonly Dictionary<PrefabGUID, int> _weaponsSpells = new Dictionary<PrefabGUID, int>();





   
    // Track what we set during a cast so we can restore precisely
    static readonly Dictionary<Entity, float> _prevSpeedDuringCast = new();








    static readonly Dictionary<PrefabGUID, (float Mult, PrefabGUID RequiredEquipBuff)> _spiritSpeedByGroup = new();
    public static void AddWeaponSlotSpeed(PrefabGUID group, float multiplier, PrefabGUID requiredEquipBuff)
    {
        if (multiplier > 0f) _spiritSpeedByGroup[group] = (multiplier, requiredEquipBuff);
    }






   

  


























































    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
        if (!ConfigService.SpiritArsenal || !ConfigService.WeaponsSystem) return;

        var castStartedEvents = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
        var preCastFinishedEvents = __instance._OnPreCastFinishedQuery.ToEntityArray(Allocator.Temp);
        var postCastEndedEvents = __instance._OnPostCastEndedQuery.ToEntityArray(Allocator.Temp);
        var castEndedEvents = __instance._OnCastEndedQuery.ToEntityArray(Allocator.Temp);
        var interruptedEvents = __instance._OnInterruptedQuery.ToEntityArray(Allocator.Temp);

        try
        {
            var em = __instance.EntityManager;
            Unsheathed.Services.SpiritEquipCapWatcher.Tick(em);

            HandleCastStarted(em, castStartedEvents);          // AbilityCastStartedEvent  :contentReference[oaicite:4]{index=4}
            HandlePreCastFinished(em, preCastFinishedEvents);  // AbilityPreCastFinishedEvent  :contentReference[oaicite:5]{index=5}
            HandlePostCastEnded(em, postCastEndedEvents);      // AbilityPostCastEndedEvent  :contentReference[oaicite:6]{index=6}
            HandleCastEnded(em, castEndedEvents);              // AbilityCastEndedEvent  :contentReference[oaicite:7]{index=7}
            HandleInterrupted(em, interruptedEvents);          // AbilityInterruptedEvent  :contentReference[oaicite:8]{index=8}
        }
        finally
        {
            castStartedEvents.Dispose();
            preCastFinishedEvents.Dispose();
            postCastEndedEvents.Dispose();
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
            if (!em.Exists(ev.Character) || !ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();
            if (!_spiritSpeedByGroup.TryGetValue(groupGuid, out var entry)) continue;

            // Cast started: apply speed + lift caps (via service)
            if (entry.RequiredEquipBuff.GuidHash == 0 || ev.Character.HasBuff(entry.RequiredEquipBuff))
            {
                ApplyCastSpeed(em, ev.Character, entry.Mult);
                AttackSpeedCapService.TryLiftCaps(em, ev.Character);
            }
        }
    }

    static void HandlePreCastFinished(EntityManager em, NativeArray<Entity> events)
    {
        // (intentionally empty for now)
    }

    // In HandlePostCastEnded(.), remove the temporary speed and restore caps too (idempotent).
    static void HandlePostCastEnded(EntityManager em, NativeArray<Entity> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityPostCastEndedEvent>(e)) continue;

            var ev = em.GetComponentData<AbilityPostCastEndedEvent>(e);
            if (ev.AbilityGroup == Entity.Null || ev.Character == Entity.Null) continue;
            if (!em.Exists(ev.Character) || !ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();

            // If this group was one we boosted, clean up speed + caps here as well.
            if (_spiritSpeedByGroup.ContainsKey(groupGuid))
            {
                RestoreCastSpeed(em, ev.Character);
                AttackSpeedCapService.RestoreCaps(em, ev.Character);
            }

            // Apply your cooldown tweak (existing logic)
            if (_weaponsSpells.TryGetValue(groupGuid, out var idx))
            {
                float cooldown = idx == 0 ? Weapon_COOLDOWN_FACTOR : (idx + 1) * Weapon_COOLDOWN_FACTOR;
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
            if (ev.AbilityGroup == Entity.Null || ev.Character == Entity.Null) continue;
            if (!em.Exists(ev.Character) || !ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();
            if (!_spiritSpeedByGroup.ContainsKey(groupGuid)) continue;

            // Cast ended / interrupted / post-cast: restore speed + caps (via service)
            RestoreCastSpeed(em, ev.Character);
            AttackSpeedCapService.RestoreCaps(em, ev.Character);
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
            if (!em.Exists(ev.Character) || !ev.Character.IsPlayer()) continue;

            var groupGuid = ev.AbilityGroup.GetPrefabGuid();
            if (!_spiritSpeedByGroup.ContainsKey(groupGuid)) continue;

            // Cast ended / interrupted / post-cast: restore speed + caps (via service)
            RestoreCastSpeed(em, ev.Character);
            AttackSpeedCapService.RestoreCaps(em, ev.Character);
        }
    }

    // -- unchanged helper API below --

    public static void AddWeaponsSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponsSpells.TryAdd(prefabGuid, spellIndex);
    }

    static ModifyUnitStatBuff_DOTS MakeSpeed(UnitStatType stat, float mult) => new ModifyUnitStatBuff_DOTS
    {
        AttributeCapType = AttributeCapType.Uncapped,
        StatType = stat,
        Value = mult,
        ModificationType = ModificationType.Multiply,
        Modifier = 1,
        Id = ModificationId.NewId(0)
    };

    // We store & restore what was there; if nothing prior, we just remove our buff at the end.
    static void ApplyCastSpeed(EntityManager em, Entity character, float mult)
    {
        // Capture prior (if we already applied this in a nested way, do nothing)
        if (!_prevSpeedDuringCast.ContainsKey(character))
        {
            float prior = 1f;
            if (character.TryGetBuff(Buffs.BonusPlayerStatsBuff, out var buffEntity) &&
                em.HasBuffer<ModifyUnitStatBuff_DOTS>(buffEntity))
            {
                var buf = em.GetBuffer<ModifyUnitStatBuff_DOTS>(buffEntity);
                for (int k = 0; k < buf.Length; k++)
                {
                    ref var b = ref buf.ElementAt(k);
                    if (b.StatType == UnitStatType.AbilityAttackSpeed && b.ModificationType == ModificationType.Multiply)
                    {
                        prior = b.Value; break;
                    }
                }
            }
            _prevSpeedDuringCast[character] = prior;
        }

        // Ensure buff exists
        if (character.TryApplyAndGetBuff(Buffs.BonusPlayerStatsBuff, out var bonusBuff))
        {
            var mods = em.HasBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff)
                ? em.GetBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff)
                : em.AddBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff);

            // Single-source container: clear then write our two speed entries
            mods.Clear();
            mods.Add(MakeSpeed(UnitStatType.PrimaryAttackSpeed, mult));
            mods.Add(MakeSpeed(UnitStatType.AbilityAttackSpeed, mult));
        }
    }

    static void RestoreCastSpeed(EntityManager em, Entity character)
    {
        if (!character.TryGetBuff(Buffs.BonusPlayerStatsBuff, out var bonusBuff)) { _prevSpeedDuringCast.Remove(character); return; }

        var hadPrev = _prevSpeedDuringCast.Remove(character, out var prior);
        var mods = em.HasBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff)
            ? em.GetBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff)
            : em.AddBuffer<ModifyUnitStatBuff_DOTS>(bonusBuff);

        mods.Clear();

        if (hadPrev && prior > 0f && Math.Abs(prior - 1f) > 0.0001f)
        {
            // restore prior (if it looked like a real multiplier)
            mods.Add(MakeSpeed(UnitStatType.PrimaryAttackSpeed, prior));
            mods.Add(MakeSpeed(UnitStatType.AbilityAttackSpeed, prior));
        }
        else
        {
            // nothing to restore; drop the helper buff
            bonusBuff.Destroy();
        }
    }




}