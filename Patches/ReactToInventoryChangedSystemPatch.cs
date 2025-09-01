using HarmonyLib;
using ProjectM;
using ProjectM.Scripting;
using Unity.Collections;
using Unity.Entities;
using Unsheathed.Services;

namespace Unsheathed.Patches
{
    [HarmonyPatch]
    internal static class ReactToInventoryChangedSystemPatch
    {
        static ServerGameManager ServerGameManager => Core.ServerGameManager;
        static SystemService SystemService => Core.SystemService;

        [HarmonyPatch(typeof(ReactToInventoryChangedSystem), nameof(ReactToInventoryChangedSystem.OnUpdate))]
        [HarmonyPrefix]
        static void OnUpdatePrefix(ReactToInventoryChangedSystem __instance)
        {
            // Use the system's predefined query and fetch EVENT ENTITIES
            // (mirrors your AbilityRunScriptsSystem patch style).
            var em = __instance.EntityManager;
            NativeArray<Entity> events = __instance.EntityQueries[0].ToEntityArray(Allocator.Temp);

            try
            {
                for (int i = 0; i < events.Length; i++)
                {
                    var e = events[i];
                    if (!em.Exists(e) || !em.HasComponent<InventoryChangedEvent>(e)) continue;

                    var ev = em.GetComponentData<InventoryChangedEvent>(e);
                    if (ev.ChangeType != InventoryChangedEventType.Obtained) continue;

                    var inventory = ev.InventoryEntity;
                    if (!em.Exists(inventory)) continue;

                    // Be safe with component access on referenced entities
                    if (em.HasComponent<InventoryConnection>(inventory))
                    {
                        var conn = em.GetComponentData<InventoryConnection>(inventory);
                        // TODO: Handle your “obtained” path here.
                        // Example: identify owner/character, check item, apply logic, etc.
                        // Core.Log is your centralized logger.
                        Core.Log.LogInfo($"[ReactToInventoryChangedSystemPatch] Obtained -> inv={inventory.Index}, evt={e.Index}");
                    }
                }
            }
            finally
            {
                // Always dispose native arrays in finally (your project’s ECS rule of thumb).
                events.Dispose();
            }
        }
    }
}















