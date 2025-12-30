using BepInEx;
using System.Reflection;
using UnityEngine;
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

        var processor = new T();
        processor.ValidateProcessorInternal();
        _processors.Add(type, processor);
    }
}

interface IZDOWithProcessors : IExtendedZDO
{
    public bool HasNoProcessors { get; set; }
    public IReadOnlyList<Processor> Processors { get; set; }
    public Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
}

public abstract record PrefabInfoBase
{
    public int PrefabHash { get; internal set; }
    public string PrefabName { get; internal set; } = default!;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class ProcessorAttribute : Attribute
{
    public required int Priority { get; init; }
}

public abstract class Processor
{
    private protected Processor() { }

    internal void ValidateProcessorInternal() => ValidateProcessor();

    private protected abstract void ValidateProcessor();
}

public abstract class Processor<TPrefabInfo> : Processor
    where TPrefabInfo : PrefabInfoBase
{
    interface IZDOWithPrefabInfo : IExtendedZDO
    {
        TPrefabInfo? PrefabInfo { get; set; }
    }

    protected sealed class Inputs
    {
        public ZDO ZDO { get; private set; } = default!;
        public TPrefabInfo PrefabInfo { get; private set; } = default!;

        internal void Update(ZDO zdo, TPrefabInfo prefabInfo)
        {
            ZDO = zdo;
            PrefabInfo = prefabInfo;
        } 
    }

    protected sealed class Outputs
    {
        public bool UnregisterProcessor { get; set; }

        internal void Reset()
        {
            UnregisterProcessor = false;
        }
    }

    readonly ExtendedZDOInterface<IZDOWithPrefabInfo> _extendedZDOInterface = ZDOExtender.ZDOExtender.AddInterface<IZDOWithPrefabInfo>();
    readonly ConstructorInfo? _prefabInfoCtor;
    readonly ParameterInfo[]? _prefabInfoCtorParameters;
    readonly Dictionary<int, TPrefabInfo?> _prefabInfoByHash = [];
    readonly Inputs _inputs = new();
    readonly Outputs _outputs = new();

    protected Processor()
    {
        if (typeof(TPrefabInfo).GetConstructors() is { Length: 1 } ctors)
        {
            _prefabInfoCtor = ctors[0];
            _prefabInfoCtorParameters = _prefabInfoCtor.GetParameters();
        }
    }

    private protected override void ValidateProcessor()
    {
        if (_prefabInfoCtor is null)
            throw new ArgumentException($"Cannot use {GetType().FullName} with {typeof(TPrefabInfo).FullName}: type must have exactly one constructor.");
        foreach (var par in _prefabInfoCtorParameters!)
        {
            if (!par.ParameterType.IsSubclassOf(typeof(MonoBehaviour)))
                throw new ArgumentException($"Cannot use {GetType().FullName} with {typeof(TPrefabInfo).FullName}: constructor parameter '{par.Name}' is not a {nameof(MonoBehaviour)}.");
        }
    }

    internal void Process(ZDO zdo)
    {
        var extZDO = zdo.GetExtension<IZDOWithPrefabInfo>();
        if (extZDO.PrefabInfo is not { } prefabInfo)
            prefabInfo = GetPrefabInfo(extZDO);

        _outputs.Reset();

        if (prefabInfo is null)
            _outputs.UnregisterProcessor = true;
        else
        {
            _inputs.Update(zdo, prefabInfo);
            Process(_inputs, _outputs);
        }
    }

    protected abstract void Process(Inputs inputs, Outputs outputs);

    TPrefabInfo? GetPrefabInfo(IZDOWithPrefabInfo extZDO)
    {
        var prefabHash = extZDO.ZDO.GetPrefab();
        if (!_prefabInfoByHash.TryGetValue(prefabHash, out var prefabInfo))
        {
            prefabInfo = GetPrefabInfo(prefabHash);
            _prefabInfoByHash.Add(prefabHash, prefabInfo);
        }
        if (prefabInfo is not null)
        {
            extZDO.PrefabInfo = prefabInfo;
            extZDO.PrefabChanged += OnPrefabChanged;

            static void OnPrefabChanged(ZDO zdo, int oldPrefab, int newPrefab)
            {
                var extZDO = zdo.GetExtension<IZDOWithPrefabInfo>();
                extZDO.PrefabInfo = default;
                extZDO.PrefabChanged -= OnPrefabChanged;
            }
        }
        return prefabInfo;
    }

    TPrefabInfo? GetPrefabInfo(int prefabHash)
    {
        if (ZNetScene.instance.GetPrefab(prefabHash) is not { } prefab)
            return default;

        if (prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>() is not { } availableComponents)
            return default;

        var componentDict = availableComponents.ToDictionary(static c => c.GetType());
        var args = new object?[_prefabInfoCtorParameters!.Length];
        for (int i = 0; i < _prefabInfoCtorParameters.Length; i++)
        {
            var par = _prefabInfoCtorParameters[i];
            if (!componentDict.TryGetValue(par.ParameterType, out var component))
            {
                if (par.CustomAttributes.Any(static x => x.AttributeType.FullName is "System.Runtime.CompilerServices.NullableAttribute"))
                    continue;
                return default;
            }
            args[i] = component;
        }
        var prefabInfo = (TPrefabInfo)_prefabInfoCtor!.Invoke(args);
        prefabInfo.PrefabHash = prefabHash;
        prefabInfo.PrefabName = prefab.name;
        return prefabInfo;
    }
}