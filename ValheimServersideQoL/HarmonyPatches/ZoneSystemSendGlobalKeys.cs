using HarmonyLib;

namespace Valheim.ServersideQoL.HarmonyPatches;

[HarmonyPatch(typeof(ZoneSystem), "SendGlobalKeys")]
public static class ZoneSystemSendGlobalKeys
{
    public static event Action? GlobalKeysChanged;
    static readonly Dictionary<string, string> __prevKeys = [];

    public static void Prefix(ZoneSystem __instance, long peer)
    {
        if (peer != ZRoutedRpc.Everybody || GlobalKeysChanged is null || __prevKeys.SequenceEqual(__instance.m_globalKeysValues))
            return;

        Main.Instance.Logger.DevLog("Invoking GlobalKeysChanged event");
        __prevKeys.Clear();
        foreach (var (key, value) in __instance.m_globalKeysValues)
            __prevKeys.Add(key, value);
        GlobalKeysChanged();
    }
}
