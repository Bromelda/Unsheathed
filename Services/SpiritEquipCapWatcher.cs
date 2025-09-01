using ProjectM;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unsheathed.Resources;

namespace Unsheathed.Services
{
    public static class SpiritEquipCapWatcher
    {
        static readonly PrefabGUID[] EquipBuffs =
        {
            PrefabGUIDs.EquipBuff_Weapon_Sword_Base,
            PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base,
            PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base,
            PrefabGUIDs.EquipBuff_Weapon_Slashers_Base,
            PrefabGUIDs.EquipBuff_Weapon_Daggers_Base,
            PrefabGUIDs.EquipBuff_Weapon_Mace_Base,
            PrefabGUIDs.EquipBuff_Weapon_Reaper_Base,
            PrefabGUIDs.EquipBuff_Weapon_Spear_Base,
            PrefabGUIDs.EquipBuff_Weapon_Whip_Base,
            PrefabGUIDs.EquipBuff_Weapon_Claws_Base,
            PrefabGUIDs.EquipBuff_Weapon_Pistols_Base,
            PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base,
            PrefabGUIDs.EquipBuff_Weapon_Longbow_Base,
            PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base,
        };

        public static void Tick(EntityManager em)
        {
            List<Entity> active = AttackSpeedCapService.GetActiveSnapshot();
            for (int i = 0; i < active.Count; i++)
            {
                var ch = active[i];
                if (!em.Exists(ch) || !em.HasComponent<PlayerCharacter>(ch))
                {
                    AttackSpeedCapService.RestoreCaps(em, ch);
                    continue;
                }

                bool hasSpiritEquipBuff = false;
                for (int k = 0; k < EquipBuffs.Length; k++)
                {
                    if (BuffUtility.HasBuff(em, ch, EquipBuffs[k])) { hasSpiritEquipBuff = true; break; }
                }

                if (!hasSpiritEquipBuff)
                {
                    AttackSpeedCapService.RestoreCaps(em, ch);
                }
            }
        }
    }
}
