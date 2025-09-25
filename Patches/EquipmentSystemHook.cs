using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unsheathed.Utilities; // for GetPrefabName(), TryApplyBuff/TryRemoveBuff if you use these helpers

namespace Unsheathed.Patches
{
    internal static class LegendaryBuffs
    {
        static PrefabGUID ChooseBuffForWeapon(string prefabName)
        {
            var n = prefabName ?? string.Empty;

            if (n.Contains("Sword")) return Buffs.ShroudBuff;
            if (n.Contains("GreatSword")) return Buffs.HolyBeamPowerBuff;
            if (n.Contains("Axe")) return Buffs.PvECombatBuff;
            if (n.Contains("Mace")) return Buffs.HolyBubbleBuff;
            if (n.Contains("Spear")) return Buffs.PhasingBuff;
            if (n.Contains("Reaper")) return Buffs.StormShieldPrimaryBuff;
            if (n.Contains("Slashers")) return Buffs.TauntEmoteBuff;
            if (n.Contains("Daggers")) return Buffs.InteractModeBuff;
            if (n.Contains("Claws")) return Buffs.ActiveCharmedHumanBuff;
            if (n.Contains("TwinBlades")) return Buffs.WranglerPotionBuff;
            if (n.Contains("Whip")) return Buffs.StormShieldSecondaryBuff;
            if (n.Contains("Crossbow")) return Buffs.StandardWerewolfBuff;
            if (n.Contains("Longbow")) return Buffs.VanishBuff;
            if (n.Contains("Pistols")) return Buffs.StormShieldTertiaryBuff;

            return Buffs.CombatStanceBuff; // fallback
        }

        public static void OnEquip(EntityManager em, Entity playerChar, Entity weaponEntity, PrefabGUID weaponGuid)
        {
            var name = weaponGuid.GetPrefabName();
            var buff = ChooseBuffForWeapon(name);
            playerChar.TryApplyBuff(buff);
        }

        public static void OnUnequip(EntityManager em, Entity playerChar, Entity weaponEntity, PrefabGUID weaponGuid)
        {
            var name = weaponGuid.GetPrefabName();
            var buff = ChooseBuffForWeapon(name);
            playerChar.TryRemoveBuff(buff);
        }
    }

    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    internal static class WeaponLevelSystem_Spawn_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(WeaponLevelSystem_Spawn __instance)
        {
            var em = __instance.EntityManager;
            var entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var weaponEntity = entities[i];

                    // Owner must exist and be a PlayerCharacter (no User component needed)
                    if (!em.TryGetComponentData<EntityOwner>(weaponEntity, out var owner) ||
                        owner.Owner == Entity.Null ||
                        !em.HasComponent<PlayerCharacter>(owner.Owner))
                        continue;

                    if (!em.HasComponent<PrefabGUID>(weaponEntity)) continue;
                    var weaponGuid = em.GetComponentData<PrefabGUID>(weaponEntity);

                    var name = weaponGuid.GetPrefabName();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!name.Contains("Legendary_T06")) continue;

                    LegendaryBuffs.OnEquip(em, owner.Owner, weaponEntity, weaponGuid);
                }
            }
            finally { entities.Dispose(); }
        }
    }

    [HarmonyPatch(typeof(WeaponLevelSystem_Destroy), nameof(WeaponLevelSystem_Destroy.OnUpdate))]
    internal static class WeaponLevelSystem_Destroy_Patch
    {
        [HarmonyPostfix]
        private static void Postfix(WeaponLevelSystem_Destroy __instance)
        {
            var em = __instance.EntityManager;
            var entities = __instance.__query_1111682408_0.ToEntityArray(Allocator.Temp);
            try
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var weaponEntity = entities[i];

                    if (!em.TryGetComponentData<EntityOwner>(weaponEntity, out var owner) ||
                        owner.Owner == Entity.Null ||
                        !em.HasComponent<PlayerCharacter>(owner.Owner))
                        continue;

                    if (!em.HasComponent<PrefabGUID>(weaponEntity)) continue;
                    var weaponGuid = em.GetComponentData<PrefabGUID>(weaponEntity);

                    var name = weaponGuid.GetPrefabName();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (!name.Contains("Legendary_T06")) continue;

                    LegendaryBuffs.OnUnequip(em, owner.Owner, weaponEntity, weaponGuid);
                }
            }
            finally { entities.Dispose(); }
        }
    }
}
