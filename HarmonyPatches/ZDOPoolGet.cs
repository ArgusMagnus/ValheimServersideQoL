using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace Valheim.ServersideQoL.HarmonyPatches;

[HarmonyPatch(typeof(ZDOPool), "Get")]
public static class ZDOPoolGet
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var targetMethod = typeof(ZDO).GetConstructor(Type.EmptyTypes) ?? throw new ArgumentNullException();
        var replacementMethod = typeof(ExtendedZDO).GetConstructor(Type.EmptyTypes) ?? throw new ArgumentNullException();

        var success = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == targetMethod)
            {
                instruction.operand = replacementMethod;
                success = true;
            }
            yield return instruction;
        }

        if (!success)
            throw new Exception($"HarmonyPatch {nameof(ZDOPoolGet)}.{nameof(Transpiler)} failed");
    }
}