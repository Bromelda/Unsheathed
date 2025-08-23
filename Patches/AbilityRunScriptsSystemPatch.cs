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



namespace Unsheathed.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _weapons = ConfigService.WeaponsSystem;


 
    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> WeaponsSpells => _weaponsSpells;
    static readonly Dictionary<PrefabGUID, int> _weaponsSpells = [];






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
            HandleCastFinished(em, preCastFinishedEvents);
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
        // TODO: your “on cast start” logic
        // Pattern:
        // for (int i=0;i<events.Length;i++) { var e=events[i];
        //   if (!em.Exists(e) || !em.HasComponent<AbilityCastStartedEvent>(e)) continue;
        //   var ev = em.GetComponentData<AbilityCastStartedEvent>(e);
        //   // ev.Character, ev.AbilityGroup ...
        // }
    }

    static void HandleCastFinished(EntityManager em, NativeArray<Entity> events)
    {
        // TODO: your “pre-cast finished” logic
        // Use AbilityPreCastFinishedEvent
    }

    static void HandleInterrupted(EntityManager em, NativeArray<Entity> events)
    {
        // TODO: your “on interrupt” cleanup logic
        // Use AbilityInterruptedEvent
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