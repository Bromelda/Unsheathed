// File: MuteAbilityBarSharedLogs_IL2CPP_Reflected.cs
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// Alias static logger so we don't clash with BasePlugin.Log
using BELogger = BepInEx.Logging.Logger;

[BepInPlugin("unsheathed.mute.abilitybarshared", "Mute AbilityBar_Shared Log Spam", "1.1.0")]
public class MuteAbilityBarSharedLogs : BasePlugin
{
    private ManualLogSource _unityProxy;

    // Matches the noisy Unity error:
    // "Clearing entity ... which is a modification source ... ProjectM.AbilityBar_Shared"
    private static readonly Regex SuppressError = new(
        @"Clearing entity .* which is a modification source .*ProjectM\.AbilityBar_Shared",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Matches the follow-up info spam:
    // "Patched modifiable value ... ProjectM.AbilityBar_Shared"
    private static readonly Regex SuppressInfo = new(
        @"Patched modifiable value .*ProjectM\.AbilityBar_Shared",
        RegexOptions.Compiled | RegexOptions.Singleline);

    // Keep delegates alive so GC doesn't unhook them
    private Delegate _cbMain;
    private Delegate _cbThreaded;

    public override void Load()
    {
        // 1) Remove BepInEx' built-in Unity log source so those logs don't bypass our filter.
        var unitySrc = BELogger.Sources.FirstOrDefault(s => s.SourceName == "Unity");
        if (unitySrc != null) BELogger.Sources.Remove(unitySrc);

        // 2) Create our own "Unity" source so output still looks familiar.
        _unityProxy = BELogger.CreateLogSource("Unity");

        // 3) Hook Unity's log events *via reflection* (no direct symbol refs).
        TryHookUnityLogEvent("logMessageReceived", out _cbMain);
        TryHookUnityLogEvent("logMessageReceivedThreaded", out _cbThreaded);

        Log.LogInfo("[MuteAbilityBarSharedLogs] Muting 'Clearing entity … modification source …' and 'Patched modifiable value …' for ProjectM.AbilityBar_Shared.");
    }

    private void TryHookUnityLogEvent(string eventName, out Delegate delOut)
    {
        delOut = null;
        try
        {
            var appType = typeof(Application);
            var evt = appType.GetEvent(eventName, BindingFlags.Static | BindingFlags.Public);
            if (evt == null)
            {
                Log.LogWarning($"[MuteAbilityBarSharedLogs] Unity event '{eventName}' not found; skipping.");
                return;
            }

            // The event type is UnityEngine.Application+LogCallback with signature (string, string, LogType)
            var cbType = evt.EventHandlerType;
            var handler = typeof(MuteAbilityBarSharedLogs).GetMethod(
                nameof(OnUnityLog), BindingFlags.Instance | BindingFlags.NonPublic);

            var del = Delegate.CreateDelegate(cbType, this, handler!);
            evt.AddEventHandler(null, del);
            delOut = del;

            Log.LogInfo($"[MuteAbilityBarSharedLogs] Hooked Unity '{eventName}'.");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"[MuteAbilityBarSharedLogs] Failed to hook '{eventName}': {ex.Message}");
        }
    }

    // MUST match Unity's LogCallback signature
    // void (string condition, string stackTrace, LogType type)
    private void OnUnityLog(string condition, string stackTrace, LogType type)
    {
        // 1) Hide the specific Unity error spam
        if ((type == LogType.Error || type == LogType.Exception) && SuppressError.IsMatch(condition))
            return;

        // 2) Hide the specific follow-up info ("Message") spam
        if (type == LogType.Log && SuppressInfo.IsMatch(condition))
            return;

        // Forward everything else unchanged
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                _unityProxy.LogError(condition);
                break;
            case LogType.Warning:
                _unityProxy.LogWarning(condition);
                break;
            default:
                _unityProxy.LogInfo(condition);
                break;
        }
    }
}





