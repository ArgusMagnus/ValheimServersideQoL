using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace Valheim.ServersideQoL.HarmonyPatches;

[HarmonyPatch(typeof(ZDOPool), "Get")]
public static class ZDOPoolGet
{
    static readonly ConstructorInfo __targetMethod = typeof(ZDO).GetConstructor(Type.EmptyTypes) ?? throw new ArgumentNullException();
    static readonly ConstructorInfo __replacementMethod = typeof(ExtendedZDO).GetConstructor(Type.EmptyTypes) ?? throw new ArgumentNullException();

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var success = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == __targetMethod)
            {
                instruction.operand = __replacementMethod;
                success = true;
            }
            yield return instruction;
        }

        if (!success)
            throw new Exception($"HarmonyPatch {nameof(ZDOPoolGet)}.{nameof(Transpiler)} failed");
    }
}