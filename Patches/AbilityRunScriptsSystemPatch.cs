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
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_weapons) return;

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


                if (WeaponsSpells.ContainsKey(prefabGuid))
                    {
                        float cooldown = WeaponsSpells[prefabGuid].Equals(0) ? Weapon_COOLDOWN_FACTOR : (WeaponsSpells[prefabGuid] + 1) * Weapon_COOLDOWN_FACTOR;
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


    public static void AddWeaponsSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponsSpells.TryAdd(prefabGuid, spellIndex);
    }
   


}