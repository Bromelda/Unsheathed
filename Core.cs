
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;

using Bloodcraft.Patches;
using Bloodcraft.Resources;
using Bloodcraft.Services;





using Bloodcraft.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Bloodcraft.Utilities.EntityQueries;
using ComponentType = Unity.Entities.ComponentType;

namespace Bloodcraft;
internal static class Core
{
    public static World Server { get; } = GetServerWorld() ?? throw new Exception("There is no Server world!");
    public static EntityManager EntityManager => Server.EntityManager;
    public static ServerGameManager ServerGameManager => SystemService.ServerScriptMapper.GetServerGameManager();
    public static SystemService SystemService { get; } = new(Server);
    public static ServerGameBalanceSettings ServerGameBalanceSettings { get; set; }
    public static double ServerTime => ServerGameManager.ServerTime;
    public static double DeltaTime => ServerGameManager.DeltaTime;
    public static ManualLogSource Log => Plugin.LogInstance;

    public static void ApplyEquipBuff(Entity entity, int groupGuid, int slot = 1)
    {
        var buffer = entity.ReadBuffer<ReplaceAbilityOnSlotBuff>();
        ReplaceAbilityOnSlotBuff buff = new()
        {
            Slot = slot,
            NewGroupId = new PrefabGUID(groupGuid),
            CopyCooldown = true,
            Priority = 0,
        };
        buffer.Add(buff);
    }
    static MonoBehaviour _monoBehaviour;

    static readonly List<PrefabGUID> _returnBuffs =
    [
        PrefabGUIDs.Buff_Shared_Return,
        PrefabGUIDs.Buff_Shared_Return_NoInvulernable,
        PrefabGUIDs.Buff_Vampire_BloodKnight_Return,
        PrefabGUIDs.Buff_Vampire_Dracula_Return,
        PrefabGUIDs.Buff_Dracula_Return,
        PrefabGUIDs.Buff_WerewolfChieftain_Return,
        PrefabGUIDs.Buff_Werewolf_Return,
        PrefabGUIDs.Buff_Monster_Return,
        PrefabGUIDs.Buff_Purifier_Return,
        PrefabGUIDs.Buff_Blackfang_Morgana_Return,
        PrefabGUIDs.Buff_ChurchOfLight_Paladin_Return,
        PrefabGUIDs.Buff_Gloomrot_Voltage_Return,
        PrefabGUIDs.Buff_Militia_Fabian_Return
    ];

   

   

   
    static readonly bool _classes = ConfigService.ClassSystem;
  
   
   
    

  
    public static byte[] NEW_SHARED_KEY { get; set; }

    public static bool _initialized = false;
    public static void Initialize()
    {
        if (_initialized) return;

        NEW_SHARED_KEY = Convert.FromBase64String(SecretManager.GetNewSharedKey());
        // string hexString = SecretManager.GetNewSharedKey();
        // NEW_SHARED_KEY = [..Enumerable.Range(0, hexString.Length / 2).Select(i => Convert.ToByte(hexString.Substring(i * 2, 2), 16))];

        if (!ComponentRegistry._initialized) ComponentRegistry.Initialize();

     
        _ = new LocalizationService();
       

      

       

       

        if (ConfigService.ClassSystem)
        {
            // Configuration.InitializeClassPassiveBuffs();
            Configuration.GetClassSpellCooldowns();
           
        }

       

      

       

        

       
           

       
        ModifyPrefabs();
      

        try
        {
            ServerGameBalanceSettings = ServerGameBalanceSettings.Get(SystemService.ServerGameSettingsSystem._ServerBalanceSettings);
           
        }
        catch (Exception e)
        {
            Log.LogWarning($"Error getting attribute soft caps: {e}");
        }

       

        _initialized = true;
        DebugLoggerPatch._initialized = true;
    }
    static World GetServerWorld()
    {
        return World.s_AllWorlds.ToArray().FirstOrDefault(world => world.Name == "Server");
    }
    static MonoBehaviour GetOrCreateMonoBehaviour()
    {
        return _monoBehaviour ??= CreateMonoBehaviour();
    }
    static MonoBehaviour CreateMonoBehaviour()
    {
        MonoBehaviour monoBehaviour = new GameObject(MyPluginInfo.PLUGIN_NAME).AddComponent<IgnorePhysicsDebugSystem>();
        UnityEngine.Object.DontDestroyOnLoad(monoBehaviour.gameObject);
        return monoBehaviour;
    }
    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        return GetOrCreateMonoBehaviour().StartCoroutine(routine.WrapToIl2Cpp());
    }
    public static void StopCoroutine(Coroutine routine)
    {
        GetOrCreateMonoBehaviour().StopCoroutine(routine);
    }
    public static void RunDelayed(Action action, float delay = 0.25f)
    {
        RunDelayedRoutine(delay, action).Run();
    }
    public static void Delay(this Action action, float delay)
    {
        RunDelayedRoutine(delay, action).Run();
    }
    static IEnumerator RunDelayedRoutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
    public static void DelayCall(float delay, Delegate method, params object[] args)
    {
        DelayedRoutine(delay, method, args).Run();
    }

    private static IEnumerator DelayedRoutine(float delay, Delegate method, object[] args)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        else
            yield return null;

        method.DynamicInvoke(args);
    }
    public static AddItemSettings GetAddItemSettings()
    {
        AddItemSettings addItemSettings = new()
        {
            EntityManager = EntityManager,
            DropRemainder = true,
            ItemDataMap = ServerGameManager.ItemLookupMap,
            EquipIfPossible = true
        };

        return addItemSettings;
    }
    
   
    static void ModifyPrefabs()
    {
       

       

       

        

       

            

        if (ConfigService.TwilightArsenal)
        {

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_FishingPole_T01, out Entity prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base, out Entity buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Fishing_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
           



                    // weaponQ (Right click) - slot 1

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {

                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Bandit_Fisherman_SpinAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Bandit_Fisherman_SpinAttack_AbilityGroup, 0); // 0 = spell index



                    // WeaponE - slot 2

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_Fisherman_FishHook_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Bandit_Fisherman_FishHook_AbilityGroup, 0); // 0 = spell index

                }
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Daggers_Legendary_T06, out  prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Daggers_Base;
                });
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Daggers_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
               
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Undead_Priest_Elite_Projectile_Hard_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                

                    // weaponQ (Right click) - slot 1
               
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Undead_BishopOfDeath_CorpseExplosion_Hard_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_BishopOfDeath_CorpseExplosion_Hard_AbilityGroup, 5); // 0 = spell index



                    // WeaponE - slot 2

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_Leader_AreaAttack_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_Leader_AreaAttack_Group, 12); // 0 = spell index

                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Reaper_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Reaper_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Reaper_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
               
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Undead_Leader_SpinningDash_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_Leader_SpinningDash_Group, 6); // 0 = spell index


                    // weaponQ (Right click) - slot 1

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_IceRanger_LurkerSpikes_Split_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_IceRanger_LurkerSpikes_Split_AbilityGroup, 5); // 0 = spell index


                    // WeaponE - slot 2

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_IceRanger_IceNova_Large_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_IceRanger_IceNova_Large_AbilityGroup, 5); // 0 = spell index

                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Mace_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Mace_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Mace_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                  
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Vampire_Mace_Primary_MeleeAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });


                    // weaponQ (Right click) - slot 1

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_ChurchOfLight_Paladin_Dash_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_ChurchOfLight_Paladin_Dash_AbilityGroup, 7); // 0 = spell index


                    // WeaponE - slot 2
                    
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Paladin_HolyNuke_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Paladin_HolyNuke_AbilityGroup, 5); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Sword_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Sword_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Sword_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
          
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_RazorParchment_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   


                    // weaponQ (Right click) - slot 1

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_InkFuel_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Militia_Scribe_InkFuel_AbilityGroup, 8); // 0 = spell index


                    // WeaponE - slot 2

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_CursedSmith_Summon_WeaponSword_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_CursedSmith_Summon_WeaponSword_AbilityGroup, 15); // 0 = spell index

                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_GreatSword_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 01
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Vampire_GreatSword_Primary_Moving_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_HighLord_SwordPrimary_MeleeAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
         
                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup, 5); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Spear_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Spear_Base;
                });
            }
            

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Spear_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Blackfang_Viper_StepThrow_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Blackfang_Viper_JavelinRain_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Blackfang_Viper_JavelinRain_AbilityGroup, 2); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_CursedSmith_FloatingSpear_SpearThrust_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_CursedSmith_FloatingSpear_SpearThrust_AbilityGroup, 11); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_TwinBlades_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Vampire_TwinBlades_Primary_MeleeAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Undead_ArenaChampion_Windslash_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                  

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_ArenaChampion_CounterStrike_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Undead_ArenaChampion_CounterStrike_AbilityGroup, 6); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Slashers_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Slashers_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Slashers_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_RangedAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_CuttingParchment02_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Militia_Scribe_CuttingParchment02_AbilityGroup, 5); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_RazorParchment_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Militia_Scribe_RazorParchment_AbilityGroup, 5); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Whip_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Whip_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Whip_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_CastleMan_SpinShield_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_CastleMan_SpinShield_AbilityGroup, 5); // 0 = spell index

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_AbilityGroup, 2); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_Foreman_ThrowNet_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Bandit_Foreman_ThrowNet_Group, 4); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Pistols_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Pistols_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Pistols_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Revolvers4_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_VHunter_Jade_Revolvers4_Group, 1); // 0 = spell index

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Snipe_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_VHunter_Jade_Snipe_Group, 5); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_DisablingShot_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_VHunter_Jade_DisablingShot_Group, 11); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Crossbow_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Militia_BombThrow_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Militia_BombThrow_AbilityGroup, 2); // 0 = spell index

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_BlastVault_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_VHunter_Jade_BlastVault_Group, 7); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_ClusterBombThrow_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Bandit_ClusterBombThrow_AbilityGroup, 7); // 0 = spell index
                }
            }
            
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Longbow_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Longbow_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Longbow_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Militia_LightArrow_UnsteadyShot_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Bandit_FrostArrow_RainOfArrows_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Bandit_FrostArrow_RainOfArrows_AbilityGroup, 5); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Stealth_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_VHunter_Jade_Stealth_Group, 7); // 0 = spell indexv
                }
            }
            
             
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Claws_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Claws_Base;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Claws_Base, out buffEntity))
            {
                if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                {
                    buffer.Clear();

                    // PRIMARY (Left click) - slot 0
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 0,
                        NewGroupId = PrefabGUIDs.AB_Vampire_Claws_Primary_MeleeAttack_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Prog_HomingNova_Group    ,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Prog_HomingNova_Group    , 5); // 0 = spell index

                    // WeaponE - slot 2
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Blackfang_Striker_FistBlock_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    AbilityRunScriptsSystemPatch.AddWeaponAbility(PrefabGUIDs.AB_Blackfang_Striker_FistBlock_AbilityGroup, 5); // 0 = spell index
                }
            }
            
            // add more custom weapons

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Axe_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) => equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_DualHammers_Ability03);
            }

            /*
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Reaper_T09_ShadowMatter, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability03;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_TwinBlades_T09_ShadowMatter, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability03;
                });
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_GreatSword_T09_ShadowMatter, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) =>
                {
                    equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Pollaxe_Ability03;
                });
            }
            */
        }
    }

   
   
    static bool IsWeaponPrimaryProjectile(string prefabName, WeaponType weaponType)
    {
        return prefabName.ContainsAll([weaponType.ToString(), "Primary", "Projectile"]);
    }
    public static void DumpEntity(this Entity entity, World world)
    {
        Il2CppSystem.Text.StringBuilder sb = new();

        try
        {
            EntityDebuggingUtility.DumpEntity(world, entity, true, sb);
            Log.LogInfo($"Entity Dump:\n{sb.ToString()}");
        }
        catch (Exception e)
        {
            Log.LogWarning($"Error dumping entity: {e.Message}");
        }
    }
}
public struct NativeAccessor<T>(NativeArray<T> array) : IDisposable where T : unmanaged
{
    NativeArray<T> _array = array;
    public T this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }
    public int Length => _array.Length;
    public NativeArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();
    public void Dispose() => _array.Dispose();
}
