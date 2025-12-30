using System.Reflection;
using UnityEngine;
using Valheim.ZDOExtender;

namespace Valheim.ServersideQoL;

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

    static class InstanceCache<T>
        where T : Processor, new()
    {
        public static readonly T Instance = new();
    }

    public static T Instance<T>()
        where T : Processor, new()
        => InstanceCache<T>.Instance;
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