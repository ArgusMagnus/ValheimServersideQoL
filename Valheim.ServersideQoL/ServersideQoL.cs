using BepInEx;
using System.Reflection;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(ZDOExtender.ZDOExtender.PluginGuid, ZDOExtender.ZDOExtender.PluginVersion)]
public sealed class ServersideQoL : BaseUnityPlugin
{
    public const string PluginName = nameof(ServersideQoL);
    public const string PluginGuid = $"argusmagnus.{PluginName}";
    public const string PluginVersion = "0.0.1";

    readonly ExtendedZDOInterface<IZDOWithProcessors> _extendedZDOInterface = ZDOExtender.ZDOExtender.AddInterface<IZDOWithProcessors>();
    static Dictionary<Type, Processor>? _processors = [];
    public static IReadOnlyList<Processor> Processors => field ??= new Func<IReadOnlyList<Processor>>(static () =>
    {
        var processors = _processors!;
        _processors = null;
        return [.. processors.OrderByDescending(static x => x.Key.GetCustomAttribute<ProcessorAttribute>()?.Priority ?? 0).Select(static x => x.Value)];
    }).Invoke();

    public static void AddProcessor<T>()
        where T : Processor, new()
    {
        if (_processors is null)
            throw new InvalidOperationException("Processor registration is closed.");

        var type = typeof(T);
        if (_processors.ContainsKey(type))
            return;

        var processor = Processor.Instance<T>();
        processor.ValidateProcessorInternal();
        _processors.Add(type, processor);
    }
}
