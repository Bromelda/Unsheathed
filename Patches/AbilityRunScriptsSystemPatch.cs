using Bloodcraft.Resources;
using Bloodcraft.Services;
using Bloodcraft.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;


namespace Bloodcraft.Patches;

[HarmonyPatch]
internal static class AbilityRunScriptsSystemPatch
{
    static ServerGameManager ServerGameManager => Core.ServerGameManager;

    static readonly bool _classes = ConfigService.ClassSystem;
   

    const float Spell_COOLDOWN_FACTOR = 8f;
    const float Weapon_COOLDOWN_FACTOR = 1f;
    public static IReadOnlyDictionary<PrefabGUID, int> ClassSpells => _classSpells;
    static readonly Dictionary<PrefabGUID, int> _classSpells = [];

    public static IReadOnlyDictionary<PrefabGUID, int> WeaponAbility => _weaponAbility;
    static readonly Dictionary<PrefabGUID, int> _weaponAbility = [];

   


    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(AbilityRunScriptsSystem __instance)
    {
        if (!Core._initialized) return;
        else if (!_classes) return;

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

                   
                    
                    if (ClassSpells.ContainsKey(prefabGuid))
                    {
                        float cooldown = ClassSpells[prefabGuid].Equals(0) ? Spell_COOLDOWN_FACTOR : (ClassSpells[prefabGuid] + 1) * Spell_COOLDOWN_FACTOR;
                        ServerGameManager.SetAbilityGroupCooldown(postCastEndedEvent.Character, prefabGuid, cooldown);
                    }
                    else if (WeaponAbility.ContainsKey(prefabGuid))
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

    
    public static void AddClassSpell(PrefabGUID prefabGuid, int spellIndex)
    {
        _classSpells.TryAdd(prefabGuid, spellIndex);
    }
    public static void AddWeaponAbility(PrefabGUID prefabGuid, int spellIndex)
    {
        _weaponAbility.TryAdd(prefabGuid, spellIndex);
    }

   
}