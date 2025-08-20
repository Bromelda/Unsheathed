using Bloodcraft.Services;
using HarmonyLib;
using UnityEngine;

namespace Bloodcraft.Patches;

[HarmonyPatch]
internal class DebugLoggerPatch
{
    public static bool _initialized = false;
   

    /*
    const string TYPE_INDEX_ERROR = "typeIndexInArchetype was -1";

    [HarmonyPatch(typeof(Debug), nameof(Debug.Log), new Type[] { typeof(Il2CppSystem.Object) })] // don't use preview feature here or github workflow gets mad
    [HarmonyPrefix]
    static bool LogPrefix(Il2CppSystem.Object message)
    {
        if (!_initialized) return true;
        else if (!_familiars) return true;

        string stringMessage = message.ToString();
        if (stringMessage.Contains(TYPE_INDEX_ERROR))
        {
            return false;
        }

        return true;
    }
    */
}
