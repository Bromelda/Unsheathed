using Unsheathed.Resources;
using Unsheathed.Services;
using Unsheathed.Utilities;
using HarmonyLib;
using ProjectM;
using ProjectM.Behaviours;
using ProjectM.Gameplay.Systems;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;



namespace Unsheathed.Patches;

[HarmonyPatch]
internal static class StatChangeSystemPatch
{
    static EntityManager EntityManager => Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;

    static readonly GameModeType _gameMode = SystemService.ServerGameSettingsSystem._Settings.GameModeType;

    static readonly Random _random = new();
    public static IReadOnlyDictionary<ulong, DateTime> LastDamageTime => _lastDamageTime;
    static readonly ConcurrentDictionary<ulong, DateTime> _lastDamageTime = [];
    /*
    static readonly HashSet<PrefabGUID> _shinyOnHitDebuffs = new()
    {
        { Buffs.VampireIgniteDebuff},
        { Buffs.VampireStaticDebuff},
        { Buffs.VampireLeechDebuff},
        { Buffs.VampireWeakenDebuff},
        { Buffs.VampireChillDebuff},
        { Buffs.VampireCondemnDebuff}
    };
    */
    static readonly HashSet<PrefabGUID> _ignoredSources = new()
    {
        { Buffs.GarlicDebuff },
        { Buffs.SilverDebuff },
        { Buffs.HolyDebuff },
        { Buffs.DivineDebuff }
    };
    /*
    static readonly PrefabGUID _slashersMeleeHit03 = PrefabGUIDs.AB_Vampire_Slashers_Primary_MeleeAttack_Hit03;
    static readonly PrefabGUID _twinBladesSweepingStrikeHit = PrefabGUIDs.AB_Vampire_TwinBlades_SweepingStrike_Hit;
    static readonly PrefabGUID _vargulfBleedBuff = Buffs.VargulfBleedBuff;
    static readonly PrefabGUID _iceShieldBuff = PrefabGUIDs.Frost_Vampire_Buff_IceShield_SpellMod;
    */
   

  

    static readonly PrefabGUID _activeCharmedHumanBuff = Buffs.ActiveCharmedHumanBuff;
    /*
    [HarmonyPatch(typeof(StatChangeSystem), nameof(StatChangeSystem.OnUpdate))]
    [HarmonyPrefix]
    static void OnUpdatePrefix(StatChangeSystem __instance)
    {
        if (!Core._initialized) return;

        // NativeArray<Entity> entities = __instance._DamageTakenEventQuery.ToEntityArray(Allocator.Temp);
        // NativeArray<DamageTakenEvent> damageTakenEvents = __instance._DamageTakenEventQuery.ToComponentDataArray<DamageTakenEvent>(Allocator.Temp);

        
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Core.Log.LogWarning($"[StatChangeSystem] Exception: {e}");
        }
        finally
        {
            // entities.Dispose();
            // damageTakenEvents.Dispose();
        }
    }
*/
    public static void RemoveOnItemPickup(ulong steamId)
    {
        _lastDamageTime.TryRemove(steamId, out DateTime _);
    }
    static bool IsValidTarget(Entity entity)
    {
        return entity.Has<Movement>() && entity.Has<Health>() && !entity.IsPlayer();
    }
   
            
   }
    


