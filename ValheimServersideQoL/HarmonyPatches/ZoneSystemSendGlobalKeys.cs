using HarmonyLib;

namespace Valheim.ServersideQoL.HarmonyPatches;

[HarmonyPatch(typeof(ZoneSystem), "SendGlobalKeys")]
public static class ZoneSystemSendGlobalKeys
{
    public static event Action? GlobalKeysChanged;
    public static event Action? GlobalKeyValuesChanged;
    static readonly Dictionary<string, string> __prevKeys = [];

    public static void Prefix(ZoneSystem __instance, long peer)
    {
        if (peer != ZRoutedRpc.Everybody)
            return;

        var changed = false;

        if (GlobalKeysChanged is not null && !__prevKeys.Keys.SequenceEqual(__instance.m_globalKeysValues.Keys))
        {
            changed = true;
            Main.Instance.Logger.DevLog($"Invoking {nameof(GlobalKeysChanged)} event");
            GlobalKeysChanged();
        }

        if (GlobalKeyValuesChanged is not null && !__prevKeys.SequenceEqual(__instance.m_globalKeysValues))
        {
            changed = true;
            Main.Instance.Logger.DevLog($"Invoking {nameof(GlobalKeyValuesChanged)} event");
            GlobalKeyValuesChanged();
        }

        if (!changed)
            return;
        
        __prevKeys.Clear();
        foreach (var (key, value) in __instance.m_globalKeysValues)
            __prevKeys.Add(key, value);
    }
}
