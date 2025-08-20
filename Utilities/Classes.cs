
using Bloodcraft.Patches;
using Bloodcraft.Services;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Text.RegularExpressions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using VampireCommandFramework;




using static Bloodcraft.Utilities.EntityQueries;


namespace Bloodcraft.Utilities;
internal static class Classes
{
    static EntityManager EntityManager => Core.EntityManager;

    static EntityManager _entityManagerRef = Core.EntityManager;
    static ServerGameManager ServerGameManager => Core.ServerGameManager;
    static SystemService SystemService => Core.SystemService;
    static PrefabCollectionSystem PrefabCollectionSystem => SystemService.PrefabCollectionSystem;
    static ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem => SystemService.ActivateVBloodAbilitySystem;
    static ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem => SystemService.ReplaceAbilityOnSlotSystem;

    static readonly WaitForSeconds _longDelay = new(10f);
    static readonly Regex _classNameRegex = new("(?<!^)([A-Z])");

    static readonly PrefabGUID _vBloodAbilityBuff = Buffs.VBloodAbilityReplaceBuff;

    const string NO_NAME = "No Name";
    const string PRIMARY_ATTACK = "Primary Attack";
    static NativeParallelHashMap<PrefabGUID, ItemData> ItemLookup => SystemService.GameDataSystem.ItemHashLookupMap;
    static PrefabLookupMap _prefabLookupMap = PrefabCollectionSystem._PrefabLookupMap;

   

    

   

    static readonly int[] _typeIndices = [0, 1];

   

    
   

   

   

   
   
   
   

    

   
  
  
    static void RemoveNPCSpell(Entity character)
    {
        Entity buffEntity = Entity.Null;
        var buffer = character.ReadBuffer<BuffBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            BuffBuffer item = buffer[i];
            if (item.PrefabGuid.GetPrefabName().StartsWith("EquipBuff_Weapon"))
            {
                buffEntity = item.Entity;
                break;
            }
        }

        var replaceBuffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        int toRemove = -1;

        for (int i = 0; i < replaceBuffer.Length; i++)
        {
            ReplaceAbilityOnSlotBuff item = replaceBuffer[i];
            if (item.Slot == 3)
            {
                toRemove = i;
                break;
            }
        }

        if (toRemove >= 0 && toRemove < replaceBuffer.Length) replaceBuffer.RemoveAt(toRemove);

        ServerGameManager.ModifyAbilityGroupOnSlot(buffEntity, character, 3, PrefabGUID.Empty);
    }
    static void HandleNPCSpell(Entity character, PrefabGUID spellPrefabGUID)
    {
        Entity buffEntity = Entity.Null;
        var buffer = character.ReadBuffer<BuffBuffer>();

        for (int i = 0; i < buffer.Length; i++)
        {
            BuffBuffer item = buffer[i];
            if (item.PrefabGuid.GetPrefabName().StartsWith("EquipBuff_Weapon"))
            {
                buffEntity = item.Entity;
                break;
            }
        }

        var replaceBuffer = buffEntity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        int toRemove = -1;

        for (int i = 0; i < replaceBuffer.Length; i++)
        {
            ReplaceAbilityOnSlotBuff item = replaceBuffer[i];
            if (item.Slot == 3)
            {
                toRemove = i;
                break;
            }
        }

        if (toRemove >= 0 && toRemove < replaceBuffer.Length) replaceBuffer.RemoveAt(toRemove);

        ReplaceAbilityOnSlotBuff buff = new()
        {
            Slot = 3,
            NewGroupId = spellPrefabGUID,
            CopyCooldown = true,
            Priority = 0,
        };

        replaceBuffer.Add(buff);
        ServerGameManager.ModifyAbilityGroupOnSlot(buffEntity, character, 3, spellPrefabGUID);
    }
   
    static string GetClassSpellName(PrefabGUID prefabGuid)
    {
        string prefabName = prefabGuid.GetLocalizedName();
        if (string.IsNullOrEmpty(prefabName) || prefabName.Equals(NO_NAME) || prefabName.Equals(PRIMARY_ATTACK)) prefabName = prefabGuid.GetPrefabName();

        int prefabIndex = prefabName.IndexOf("PrefabGuid");
        if (prefabIndex > 0) prefabName = prefabName[..prefabIndex].TrimEnd();

        return prefabName;
    }
   

       
   
   
   
    public static string FormatModifyUnitStatBuffer(Entity buffEntity)
    {
        if (buffEntity.TryGetBuffer<ModifyUnitStatBuff_DOTS>(out var buffer) && !buffer.IsEmpty)
        {
            List<string> formattedStats = [];

            foreach (ModifyUnitStatBuff_DOTS modifyUnitStatBuff in buffer)
            {
                string statName = modifyUnitStatBuff.StatType.ToString();
                float value = modifyUnitStatBuff.Value;
                string formattedValue;

               
                {
                   
                }
           
                {
                    formattedValue = FormatPercentStatValue(value);
                }

                string colorizedStat = $"<color=#00FFFF>{statName}</color>: <color=white>{formattedValue}</color>";
                formattedStats.Add(colorizedStat);
            }

            return string.Join(", ", formattedStats);
        }

        else return string.Empty;
    }
    static string FormatPercentStatValue(float value)
    {
        return (value * 100).ToString("F0") + "%";
    }
    
    
}
