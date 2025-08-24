using Unsheathed.Resources;
using Unsheathed.Services;
using Unsheathed.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using System.Collections.Generic;


namespace Unsheathed.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _weapons = ConfigService.WeaponsSystem;

    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> WeaponsSpells => _weaponsSpells;
    static readonly Dictionary<PrefabGUID, int> _weaponsSpells = new Dictionary<PrefabGUID, int>();












    // Which stat to touch for a given group (Primary uses PrimaryAttackSpeed; Q/E use AbilityAttackSpeed)
    public enum SlotKind : byte { Primary, Q, E }

    // Map SpiritArsenal ability group -> slot
    static readonly Dictionary<PrefabGUID, SlotKind> _groupSlot = new();

    // Per-group speed multiplier (e.g., 1.0 = no change, 1.8 = 80% faster)
    static readonly Dictionary<PrefabGUID, float> _groupMultiplier = new();

    // Backup of original values per character so we can restore exactly
    struct AbilityBarBackup { public float Ability; public float Primary; }
    static readonly Dictionary<Entity, AbilityBarBackup> _abilityBarBackup = new();

    // API you call from Core/Spirit setup:
    public static void RegisterGroupSlot(PrefabGUID group, SlotKind slot) => _groupSlot[group] = slot;
    public static void SetGroupSpeedMultiplier(PrefabGUID group, float multiplier)
    {
        // clamp to sane bounds to avoid silly values
        if (multiplier < 0.5f) multiplier = 0.5f;
        else if (multiplier > 3.0f) multiplier = 3.0f;
        _groupMultiplier[group] = multiplier;
    }

    static void ApplyAbilityBarOverrideForSlot(EntityManager em, Entity character, SlotKind slot, float multiplier)
    {
        if (character == Entity.Null || !em.Exists(character)) return;
        if (!em.HasComponent<AbilityBar_Shared>(character)) return;

        var ab = em.GetComponentData<AbilityBar_Shared>(character);

        // Save originals once; presence in the dict means "override active"
        if (!_abilityBarBackup.ContainsKey(character))
        {
            _abilityBarBackup[character] = new AbilityBarBackup
            {
                Ability = ab.AbilityAttackSpeed._Value,
                Primary = ab.PrimaryAttackSpeed._Value
            };
        }

        var orig = _abilityBarBackup[character];

        // Touch only the relevant stat for the slot being cast
        switch (slot)
        {
            case SlotKind.Primary:
                ab.PrimaryAttackSpeed._Value = orig.Primary * multiplier;
                break;
            case SlotKind.Q:
            case SlotKind.E:
                ab.AbilityAttackSpeed._Value = orig.Ability * multiplier;
                break;
        }

        em.SetComponentData(character, ab);
    }

    static void RestoreAbilityBar(EntityManager em, Entity character)
    {
        if (character == Entity.Null || !em.Exists(character)) return;
        if (_abilityBarBackup.TryGetValue(character, out var orig) && em.HasComponent<AbilityBar_Shared>(character))
        {
            var ab = em.GetComponentData<AbilityBar_Shared>(character);
            ab.AbilityAttackSpeed._Value = orig.Ability;
            ab.PrimaryAttackSpeed._Value = orig.Primary;
            em.SetComponentData(character, ab);
        }
        _abilityBarBackup.Remove(character);
    }



























    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        // Use your repo’s init guard (you can swap to GameSystems.Initialized if you actually have that symbol)
        if (!Core._initialized) return;

        var castStartedEvents = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);       // First event
        var preCastFinishedEvents = __instance._OnPreCastFinishedQuery.ToEntityArray(Allocator.Temp);   // Second event
        var postCastEndedEvents = __instance._OnPostCastEndedQuery.ToEntityArray(Allocator.Temp);     // Third event
        var interruptedEvents = __instance._OnInterruptedQuery.ToEntityArray(Allocator.Temp);

        try
        {
            var em = __instance.EntityManager;

            HandleCastStarted(em, castStartedEvents);
            HandlePreCastFinished(em, preCastFinishedEvents);
            HandlePostCastEnded(em, postCastEndedEvents);   // keeps your cooldown behavior
            HandleInterrupted(em, interruptedEvents);
        }
        finally
        {
            castStartedEvents.Dispose();
            preCastFinishedEvents.Dispose();
            postCastEndedEvents.Dispose();
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
            if (ev.Character == Entity.Null || ev.AbilityGroup == Entity.Null) continue;
            if (!em.Exists(ev.Character) || !em.Exists(ev.AbilityGroup)) continue;
            if (!ev.Character.IsPlayer()) continue;

            var group = ev.AbilityGroup.GetPrefabGuid();

            // Only affect your custom SpiritArsenal abilities you registered:
            if (_groupSlot.TryGetValue(group, out var slot) && _groupMultiplier.TryGetValue(group, out var mult))

            {
                Core.Log.LogInfo($"[SpiritSpeed] Apply {mult:0.00} to {slot} for group {group.GuidHash} on {ev.Character.Index}");


                // Guard to avoid double-applying if multiple cast-start events fire
                if (!_abilityBarBackup.ContainsKey(ev.Character))
                    ApplyAbilityBarOverrideForSlot(em, ev.Character, slot, mult);
            }
        }
    }


    static void HandlePreCastFinished(EntityManager em, NativeArray<Entity> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityPreCastFinishedEvent>(e)) continue;
            var ev = em.GetComponentData<AbilityPreCastFinishedEvent>(e);
            if (ev.Character == Entity.Null || !em.Exists(ev.Character)) continue;

            RestoreAbilityBar(em, ev.Character);
        }
    }

    static void HandleInterrupted(EntityManager em, NativeArray<Entity> events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            var e = events[i];
            if (!em.Exists(e) || !em.HasComponent<AbilityInterruptedEvent>(e)) continue;
            var ev = em.GetComponentData<AbilityInterruptedEvent>(e);
            if (ev.Character == Entity.Null || !em.Exists(ev.Character)) continue;

            RestoreAbilityBar(em, ev.Character);
        }
    }


    static void HandlePostCastEnded(EntityManager em, NativeArray<Entity> events)
    {
        // Preserves your original Spirit cooldown tweak per ability group
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

            // Assumes these already exist in your class:
            //   public static IReadOnlyDictionary<PrefabGUID,int> WeaponsSpells => _weaponsSpells;
            //   static readonly Dictionary<PrefabGUID,int> _weaponsSpells = ...;
            //   const float Weapon_COOLDOWN_FACTOR = 1f; (or your actual value)
            if (WeaponsSpells != null && WeaponsSpells.ContainsKey(groupGuid))
            {
                float cooldown = WeaponsSpells[groupGuid] == 0
                    ? Weapon_COOLDOWN_FACTOR
                    : (WeaponsSpells[groupGuid] + 1) * Weapon_COOLDOWN_FACTOR;

                ServerGameManager.SetAbilityGroupCooldown(ev.Character, groupGuid, cooldown);
            }
        }
    }


    public static void AddWeaponsSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponsSpells.TryAdd(prefabGuid, spellIndex);
    }
   


}