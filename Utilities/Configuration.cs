
using Bloodcraft.Patches;
using Bloodcraft.Services;


using ProjectM;
using Stunlock.Core;

namespace Bloodcraft.Utilities;
internal static class Configuration
{
   
    public static List<int> ParseIntegersFromString(string configString)
    {
        if (string.IsNullOrEmpty(configString))
        {
            return [];
        }

        return [..configString.Split(',').Select(int.Parse)];
    }
    public static List<T> ParseEnumsFromString<T>(string configString) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(configString))
            return [];

        List<T> result = [];

        foreach (var part in configString.Split(','))
        {
            if (Enum.TryParse<T>(part.Trim(), ignoreCase: true, out var value))
            {
                result.Add(value);
            }
        }

        return result;
    }
   
   
    public static void GetClassSpellCooldowns()
    {
       
        {
            

           
            {
               
            }
        }
    }

    /*
    public static void InitializeClassPassiveBuffs()
    {
        foreach (var keyValuePair in Classes.ClassBuffMap)
        {
            HashSet<PrefabGUID> buffPrefabs = [..ParseIntegersFromString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            List<PrefabGUID> orderedBuffs = [..ParseIntegersFromString(keyValuePair.Value).Select(buffPrefab => new PrefabGUID(buffPrefab))];

            UpdateBuffsBufferDestroyPatch.ClassBuffsSet.TryAdd(keyValuePair.Key, buffPrefabs);
            UpdateBuffsBufferDestroyPatch.ClassBuffsOrdered.Add(keyValuePair.Key, orderedBuffs);
        }
    }
    */
}
