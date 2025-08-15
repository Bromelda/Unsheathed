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

   

    const float Spell_COOLDOWN_FACTOR = 8f;
    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> ClassSpells => _classSpells;
    static readonly Dictionary<PrefabGUID, int> _classSpells = [];

    public static IReadOnlyDictionary<PrefabGUID, int> WeaponAbility => _weaponAbility;
    static readonly Dictionary<PrefabGUID, int> _weaponAbility = [];

    static readonly PrefabGUID _useWaypointAbilityGroup = PrefabGUIDs.AB_Interact_UseWaypoint_AbilityGroup;
    static readonly PrefabGUID _useCastleWaypointAbilityGroup = PrefabGUIDs.AB_Interact_UseWaypoint_Castle_AbilityGroup;
    static readonly PrefabGUID _vanishBuff = Buffs.VanishBuff;


    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
       

        // NativeArray<Entity> entities = __instance._OnCastEndedQuery.ToEntityArray(Allocator.Temp);
        NativeArray<AbilityPostCastEndedEvent> postCastEndedEvents = __instance._OnPostCastEndedQuery.ToComponentDataArray<AbilityPostCastEndedEvent>(Allocator.Temp);

        try
        {
            foreach (AbilityPostCastEndedEvent postCastEndedEvent in postCastEndedEvents)
            {
                if (postCastEndedEvent.AbilityGroup.Has<VBloodAbilityData>()) continue;
                else if (postCastEndedEvent.Character.IsPlayer())
                {
                    PrefabGUID prefabGuid = postCastEndedEvent.AbilityGroup.GetPrefabGuid();
            
               if (WeaponAbility.ContainsKey(prefabGuid))
                    {
                        float cooldown = WeaponAbility[prefabGuid].Equals(0) ? Weapon_COOLDOWN_FACTOR : (WeaponAbility[prefabGuid] + 1) * Weapon_COOLDOWN_FACTOR;
                        ServerGameManager.SetAbilityGroupCooldown(postCastEndedEvent.Character, prefabGuid, cooldown);
                    }
                }
            }
        }
        finally
        {
            postCastEndedEvents.Dispose();
        }
    }

   
    public static void AddWeaponAbility(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponAbility.TryAdd(prefabGuid, spellIndex);
    }

   
}