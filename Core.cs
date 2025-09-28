
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Unsheathed.Patches;
using Unsheathed.Resources;
using Unsheathed.Services;
using System;
using System.Collections.Generic;
using System.Linq;





using Unsheathed.Utilities;
using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.Physics;
using ProjectM.Scripting;
using Stunlock.Core;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static Unsheathed.Utilities.EntityQueries;
using ComponentType = Unity.Entities.ComponentType;

namespace Unsheathed;
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


   



    // === Spirit config overrides (single source of truth) ===
    static void Spirit_ApplyAllConfigured()
    {
        // Map every Spirit-able weapon here:
        Spirit_Apply("Pistols", PrefabGUIDs.EquipBuff_Weapon_Pistols_Base);
        Spirit_Apply("Crossbow", PrefabGUIDs.EquipBuff_Weapon_Crossbow_Base);
        Spirit_Apply("Longbow", PrefabGUIDs.EquipBuff_Weapon_Longbow_Base);
        Spirit_Apply("Sword", PrefabGUIDs.EquipBuff_Weapon_Sword_Base);
        Spirit_Apply("GreatSword", PrefabGUIDs.EquipBuff_Weapon_GreatSword_Base);
        Spirit_Apply("TwinBlades", PrefabGUIDs.EquipBuff_Weapon_TwinBlades_Base);
        Spirit_Apply("Slashers", PrefabGUIDs.EquipBuff_Weapon_Slashers_Base);
        Spirit_Apply("Daggers", PrefabGUIDs.EquipBuff_Weapon_Daggers_Base);
        Spirit_Apply("Mace", PrefabGUIDs.EquipBuff_Weapon_Mace_Base);
        Spirit_Apply("Reaper", PrefabGUIDs.EquipBuff_Weapon_Reaper_Base);
        Spirit_Apply("Spear", PrefabGUIDs.EquipBuff_Weapon_Spear_Base);
        Spirit_Apply("Whip", PrefabGUIDs.EquipBuff_Weapon_Whip_Base);
        Spirit_Apply("Claws", PrefabGUIDs.EquipBuff_Weapon_Claws_Base);
        Spirit_Apply("FishingPole", PrefabGUIDs.EquipBuff_Weapon_FishingPole_Base);
    }

    static void Spirit_Apply(string weaponKey, PrefabGUID equipBuffGuid)
    {
        // Only act if config provides values (and if weaponKey is in Spirit_
        //
        //
        // , when specified)
        if (!Unsheathed.Utilities.Configuration.TryGetSpiritGroups(weaponKey, out var s))
            return;

        if (!SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(equipBuffGuid, out var buffEntity))
            return;

        if (!buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
        {
            Log.LogWarning($"[Spirit] {weaponKey}: ReplaceAbilityOnSlotBuff buffer not found on equip buff entity.");
            if (!SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(equipBuffGuid, out  buffEntity))
            {
                Log.LogWarning($"[Spirit] {weaponKey}: equip buff entity not found for {equipBuffGuid.GuidHash}");
                return;
            }

            return;

        }

        buffer.Clear();

        // Slot indices: 0 = Primary (LMB), 1 = Q (RMB), 4 = E
        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 0, NewGroupId = s.Primary, CopyCooldown = s.CopyP, Priority = 0 });
        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 1, NewGroupId = s.Q, CopyCooldown = s.CopyQ, Priority = 0 });
        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 4, NewGroupId = s.E, CopyCooldown = s.CopyE, Priority = 0 });


        if (Unsheathed.Utilities.Configuration.TryGetSpiritScriptIndices(weaponKey, out var ip, out var iq, out var ie))
        {
            if (ip >= 0) AbilityRunScriptsSystemPatch.AddWeaponsSpell(s.Primary, ip);
            if (iq >= 0) AbilityRunScriptsSystemPatch.AddWeaponsSpell(s.Q, iq);
            if (ie >= 0) AbilityRunScriptsSystemPatch.AddWeaponsSpell(s.E, ie);

            Log.LogInfo($"[Spirit] Script indices for {weaponKey} = {ip},{iq},{ie}");
        }
        // After scripts indices block in Spirit_Apply(...)
        if (Unsheathed.Utilities.Configuration.TryGetSpiritBuffs(weaponKey, out var b))
        {
            // tie buffs to the exact ability groups we just placed on this equip buff
            if (b.HasPrimary) AbilityRunScriptsSystemPatch.RegisterWeaponSlotBuff(s.Primary, b.Primary);
            if (b.HasQ) AbilityRunScriptsSystemPatch.RegisterWeaponSlotBuff(s.Q, b.Q);
            if (b.HasE) AbilityRunScriptsSystemPatch.RegisterWeaponSlotBuff(s.E, b.E);

            Log.LogInfo($"[Spirit] Buffs for {weaponKey} " +
                        $"(P={(b.HasPrimary ? b.Primary.GuidHash : 0)}, " +
                        $"Q={(b.HasQ ? b.Q.GuidHash : 0)}, " +
                        $"E={(b.HasE ? b.E.GuidHash : 0)})");
        }

      
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






    static readonly bool _weapons = ConfigService.WeaponsSystem;






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








        if (ConfigService.WeaponsSystem)
        {
            // Configuration.InitializeWeaponsPassiveBuffs();
           

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




        if (ConfigService.SpiritArsenal)
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
                   



                    // WeaponE - slot 4

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_Fisherman_FishHook_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   

                }
            }

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Daggers_Legendary_T06, out prefabEntity))
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
                   



                    // WeaponE - slot 4

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_Leader_AreaAttack_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   

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
                   

                    // weaponQ (Right click) - slot 1

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_IceRanger_LurkerSpikes_Split_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   


                    // WeaponE - slot 4

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_IceRanger_IceNova_Large_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    

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
                   


                    // WeaponE - slot 4

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Paladin_HolyNuke_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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
                   


                    // WeaponE - slot 4

                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_CursedSmith_Summon_WeaponSword_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   

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

                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_HighLord_SwordDashCleave_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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
                   

                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_CursedSmith_FloatingSpear_SpearThrust_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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


                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Undead_ArenaChampion_CounterStrike_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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


                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Militia_Scribe_RazorParchment_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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
                   

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_Lucie_PlayerAbility_WondrousHealingPotion_Throw_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                  

                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_Foreman_ThrowNet_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
                }
            }

            // Map the legendary item to its equip buff (unchanged)
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Pistols_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData eq) => eq.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_Pistols_Base);
            }

            // Apply from config if available; else keep existing defaults below
            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.EquipBuff_Weapon_Pistols_Base, out buffEntity))
            {
               
                {
                    // --- your existing default block (unchanged) ---
                    if (buffEntity.TryGetBuffer<ReplaceAbilityOnSlotBuff>(out var buffer))
                    {
                        buffer.Clear();

                        // PRIMARY (LMB) slot 0
                        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 0, NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Revolvers4_Group, CopyCooldown = true, Priority = 0 });
                       

                        // weaponQ (RMB) slot 1
                        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 1, NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Snipe_Group, CopyCooldown = true, Priority = 0 });
                       

                        // WeaponE slot 4
                        buffer.Add(new ReplaceAbilityOnSlotBuff { Slot = 4, NewGroupId = PrefabGUIDs.AB_VHunter_Jade_DisablingShot_Group, CopyCooldown = true, Priority = 0 });
                        
                    }
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
                   

                    // weaponQ (Right click) - slot 1
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 1,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_BlastVault_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   

                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Bandit_ClusterBombThrow_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
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
                    

                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_VHunter_Jade_Stealth_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                    
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
                        NewGroupId = PrefabGUIDs.AB_Prog_HomingNova_Group,
                        CopyCooldown = true,
                        Priority = 0
                    });
                  
                    
                    // WeaponE - slot 4
                    buffer.Add(new ReplaceAbilityOnSlotBuff
                    {
                        Slot = 4,
                        NewGroupId = PrefabGUIDs.AB_Blackfang_Striker_FistBlock_AbilityGroup,
                        CopyCooldown = true,
                        Priority = 0
                    });
                   
                }
            }

            // add more custom weapons

            if (SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(PrefabGUIDs.Item_Weapon_Axe_Legendary_T06, out prefabEntity))
            {
                prefabEntity.With((ref EquippableData equippableData) => equippableData.BuffGuid = PrefabGUIDs.EquipBuff_Weapon_DualHammers_Ability03);
            }
        

            Spirit_ApplyAllConfigured();
          
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
