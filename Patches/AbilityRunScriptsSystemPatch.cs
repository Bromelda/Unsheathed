using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unsheathed.Resources;
using Unsheathed.Services;
using Unsheathed.Utilities;
using System.Diagnostics;
using UnityEngine.TextCore.Text;
using static ProjectM.Sequencer.FullscreenEffectSystem;
using System;




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
            if (_slotBuffs.TryGetValue(groupGuid, out var buffGuid))
            {
                // Persist through the cast; we’ll remove manually
                ev.Character.TryApplyBuffWithLifeTimeNone(buffGuid); // uses your helper
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
            if (_slotBuffs.TryGetValue(groupGuid, out var buffGuid)) ev.Character.TryRemoveBuff(buffGuid);
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
            if (_slotBuffs.TryGetValue(groupGuid, out var buffGuid))
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