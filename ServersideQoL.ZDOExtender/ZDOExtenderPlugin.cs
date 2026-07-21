using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace ServersideQoL.ZDOExtender;

public interface IZDOInterfaceCollection
{
    IZDOInterfaceCollection Add<T>() where T : class, IExtendedZDO;
}

partial class ZDOExtenderPlugin : BaseUnityPlugin
{
    internal static Harmony HarmonyInstance { get; } = new(PluginGuid);
    internal static ZDOExtenderPlugin Instance { get; private set; } = default!;
    internal new ManualLogSource Logger => base.Logger;

#pragma warning disable CS0618 // Type or member is obsolete
    static TypeExtensionBuilder<IExtendedZDO, ExtendedZDO>? __zdoTypeBuilder = new();
#pragma warning restore CS0618 // Type or member is obsolete
    static ConstructorInfo? __dynamicZdoCtor;

    static Action<IZDOInterfaceCollection>? _registerInterfaces;
    public static event Action<IZDOInterfaceCollection>? RegisterInterfaces
    {
        add
        {
            if (__zdoTypeBuilder is null)
                throw new InvalidOperationException("Cannot register interfaces after ZDO type has been created.");
            _registerInterfaces += value;
        }
        remove => _registerInterfaces -= value;
    }

    public ZDOExtenderPlugin()
    {
        Instance = this;
    }

    partial void OnAwake()
    {
        var znetStart = typeof(ZNet).GetMethod("Start", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new ArgumentNullException();
        var znetStartPrefix = ((Delegate)ZNetStartPrefix).Method;
        HarmonyInstance.Patch(znetStart, prefix: new(znetStartPrefix));
    }

    static void ZNetStartPrefix()
    {
        if (__zdoTypeBuilder is null || _registerInterfaces is null)
            return;

        var collection = new ZDOInterfaceCollection();
        foreach (var del in _registerInterfaces.GetInvocationList().Cast<Action<IZDOInterfaceCollection>>())
        {
            try { del(collection); }
            catch (Exception ex)
            {
                Instance.Logger.LogError($"Exception in {nameof(RegisterInterfaces)} handler {del.Method.DeclaringType?.FullName}.{del.Method.Name}: {ex}");
            }
        }

        _registerInterfaces = null;

        if (!__zdoTypeBuilder.HasInterfaces)
        {
            __zdoTypeBuilder = null;
            return;
        }

        __dynamicZdoCtor = __zdoTypeBuilder.GetConstructorInfo();
        __zdoTypeBuilder = null;
        var zdoPoolGetMethod = typeof(ZDOPool).GetMethod("Get", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new ArgumentNullException();
        var zdoPoolGetTranspiler = ((Delegate)ZDOPoolGetTranspiler).Method;
        HarmonyInstance.Patch(zdoPoolGetMethod, transpiler: new(zdoPoolGetTranspiler));
        return;

        static IEnumerable<CodeInstruction> ZDOPoolGetTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var originalCtor = typeof(ZDO).GetConstructor(Type.EmptyTypes) ?? throw new ArgumentNullException();
            var newCtor = __dynamicZdoCtor ?? throw new ArgumentNullException();
            var success = false;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Newobj && (ConstructorInfo)instruction.operand == originalCtor)
                {
                    instruction.operand = newCtor;
                    success = true;
                }
                yield return instruction;
            }

            if (!success)
                throw new Exception($"HarmonyPatch {nameof(ZDOPoolGetTranspiler)} failed");
        }
    }

    sealed class ZDOInterfaceCollection : IZDOInterfaceCollection
    {
        public IZDOInterfaceCollection Add<T>() where T : class, IExtendedZDO
        {
            __zdoTypeBuilder!.AddInterface<T>();
            return this;
        }
    }
}