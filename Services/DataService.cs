using Unsheathed.Interfaces;
using Unsheathed.Resources;


using Unsheathed.Utilities;
using ProjectM;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Entities;
using VampireCommandFramework;
using static Unsheathed.Services.ConfigService;
using static Unsheathed.Services.ConfigService.ConfigInitialization;
using static Unsheathed.Services.DataService.PlayerDictionaries;




namespace Unsheathed.Services;
internal static class DataService
{













    public static class PlayerDictionaries
    {
        // exoform data



        // prestige data

        





        public static class PlayerPersistence
        {
            static readonly JsonSerializerOptions _jsonOptions = new()
            {
                WriteIndented = true,
                IncludeFields = true
            };

            static readonly Dictionary<string, string> _filePaths = new()
            {

            };

            static void LoadData<T>(ref List<List<float>> dataStructure, string key)
            {
                string path = _filePaths[key];
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose();
                    dataStructure = [];
                    // Core.Log.LogInfo($"{key} file created...");

                    return;
                }
                try
                {
                    string json = File.ReadAllText(path);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        dataStructure = [];
                    }
                    else
                    {
                        var data = JsonSerializer.Deserialize<List<List<float>>>(json, _jsonOptions);
                        dataStructure = data ?? [];
                    }
                }
                catch (IOException ex)
                {
                    Core.Log.LogWarning($"Failed to read {key} data from file: {ex.Message}");
                }
            }
            static void LoadData<T>(ref List<ulong> dataStructure, string key)
            {
                string path = _filePaths[key];
                if (!File.Exists(path))
                {
                    File.Create(path).Dispose();
                    dataStructure = [];
                    // Core.Log.LogInfo($"{key} file created...");

                    return;
                }
                try
                {
                    string json = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        dataStructure = [];
                    }
                    else
                    {
                        var data = JsonSerializer.Deserialize<List<ulong>>(json, _jsonOptions);
                        dataStructure = data ?? [];
                    }
                }
                catch (IOException ex)
                {
                    Core.Log.LogWarning($"Failed to read {key} data from file: {ex.Message}");
                }
            }
            static void SaveData<T>(ConcurrentDictionary<ulong, T> data, string key)
            {
                string path = _filePaths[key];
                try
                {
                    string json = JsonSerializer.Serialize(data, _jsonOptions);
                    File.WriteAllText(path, json);
                }
                catch (IOException ex)
                {
                    Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Core.Log.LogWarning($"JSON serialization error when saving {key} data: {ex.Message}");
                }
            }
            static void SaveData<T>(List<List<float>> data, string key)
            {
                string path = _filePaths[key];

                try
                {
                    string json = JsonSerializer.Serialize(data, _jsonOptions);
                    File.WriteAllText(path, json);
                }
                catch (IOException ex)
                {
                    Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Core.Log.LogWarning($"JSON serialization error when saving {key} data: {ex.Message}");
                }
            }
            static void SaveData<T>(List<ulong> data, string key)
            {
                string path = _filePaths[key];

                try
                {
                    string json = JsonSerializer.Serialize(data, _jsonOptions);
                    File.WriteAllText(path, json);
                }
                catch (IOException ex)
                {
                    Core.Log.LogWarning($"Failed to write {key} data to file: {ex.Message}");
                }
                catch (JsonException ex)
                {
                    Core.Log.LogWarning($"JSON serialization error saving {key} - {ex.Message}");
                }
            }


            public static class PlayerBoolsManager
            {
                static string GetFilePath(ulong steamId) => Path.Combine(DirectoryPaths[9], $"{steamId}_player_bools.json");
                public static void SavePlayerBools(ulong steamId, Dictionary<string, bool> preferences)
                {
                    string filePath = GetFilePath(steamId);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string jsonString = JsonSerializer.Serialize(preferences, options);

                    File.WriteAllText(filePath, jsonString);
                }
                public static Dictionary<string, bool> LoadPlayerBools(ulong steamId)
                {
                    string filePath = GetFilePath(steamId);

                    if (!File.Exists(filePath))
                        return [];

                    string jsonString = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<Dictionary<string, bool>>(jsonString);
                }
               
            }
        }
    }
}

            /*
            static readonly Dictionary<WeaponType, PrefabGUID> _sanguineWeapons = new()
        {
            { WeaponType.Sword, new PrefabGUID(-774462329) },
            { WeaponType.Axe, new PrefabGUID(-2044057823) },
            { WeaponType.Mace, new PrefabGUID(-126076280) },
            { WeaponType.Spear, new PrefabGUID(-850142339) },
            { WeaponType.Crossbow, new PrefabGUID(1389040540) },
            { WeaponType.GreatSword, new PrefabGUID(147836723) },
            { WeaponType.Slashers, new PrefabGUID(1322545846) },
            { WeaponType.Pistols, new PrefabGUID(1071656850) },
            { WeaponType.Reaper, new PrefabGUID(-2053917766) },
            { WeaponType.Longbow, new PrefabGUID(1860352606) },
            { WeaponType.Whip, new PrefabGUID(-655095317) },
            { WeaponType.TwinBlades, new PrefabGUID(-297349982) },
            { WeaponType.Daggers, new PrefabGUID(1031107636) },
            { WeaponType.Claws, new PrefabGUID(-1777908217) }
        };




        }
    }
}
       /*
       
        public class StatMods
        {
            public int StatMod { get; set; }
            public float Value { get; set; }
        }

       
  
      
    }


    */