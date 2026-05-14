using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using Valheim.ServersideQoL.Processors;

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

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        //foreach (var instruction in instructions)
        //{
        //    Main.Instance.Logger.DevLog($"{instruction.opcode}: {instruction.operand}");
        //    yield return instruction;
        //}

        var listCtor = typeof(List<string>).GetConstructor([typeof(IEnumerable<string>)]);
        var method = ((Delegate)ModfiyGlobalKeys).Method;

        return new CodeMatcher().Start().Insert(instructions).Start()
            .MatchForward(false, new CodeMatch(new CodeInstruction(OpCodes.Newobj, listCtor)))
            .Advance(1)
            .Insert(
              // Load "peer" argument
              new CodeInstruction(OpCodes.Ldarg_1),
              new CodeInstruction(OpCodes.Call, method)
            )
            .ThrowIfInvalid($"Failed to apply patch {nameof(ZoneSystemSendGlobalKeys)}.{nameof(Transpiler)}")
            .InstructionEnumeration();

        static List<string> ModfiyGlobalKeys(List<string> globalKeys, long peer)
        {
            if (Processor.Instance<PlayerProcessor>().GetPeerInfo(peer) is not { } peerInfo)
                return globalKeys;

            foreach (var (key, add) in peerInfo.GlobalKeyModifications)
            {
                if (!add)
                    globalKeys.Remove(key);
                else if (!globalKeys.Contains(key))
                    globalKeys.Add(key);
            }
            return globalKeys;
        }
    }
}
