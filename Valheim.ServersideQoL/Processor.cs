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
    [Flags]
    internal protected enum ProcessResult
    {
        Default = 0,
        WaitForZDORevisionChange = 1 << 0,
        UnregisterProcessor = 1 << 1,
        DestroyZDO = 1 << 2,
        RecreateZDO = 1 << 3
    }

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

    internal abstract ProcessResult Process(IReadOnlyList<Peer> peers, ZDO zdo);
    internal protected abstract void Initialize(bool firstTime);
    protected virtual void PreProcess(PeersEnumerable peers) { }
    internal void PreProcessInternal(PeersEnumerable peers) => PreProcess(peers);
    internal protected bool ClaimExclusive(ZDO zdo) => throw new NotImplementedException();
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
        public IReadOnlyList<Peer> Peers { get; private set; } = default!;
        public ZDO ZDO { get; private set; } = default!;
        public TPrefabInfo PrefabInfo { get; private set; } = default!;

        internal void Update(IReadOnlyList<Peer> peers, ZDO zdo, TPrefabInfo prefabInfo)
        {
            Peers = peers;
            ZDO = zdo;
            PrefabInfo = prefabInfo;
        }
    }

    readonly ConstructorInfo? _prefabInfoCtor;
    readonly ParameterInfo[]? _prefabInfoCtorParameters;
    readonly Dictionary<int, TPrefabInfo?> _prefabInfoByHash = [];
    readonly Inputs _inputs = new();

    protected Processor()
    {
        if (typeof(TPrefabInfo).GetConstructors() is { Length: 1 } ctors)
        {
            _prefabInfoCtor = ctors[0];
            _prefabInfoCtorParameters = _prefabInfoCtor.GetParameters();
            ZDOExtender.ZDOExtender.AddInterface<IZDOWithPrefabInfo>();
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

    internal override ProcessResult Process(IReadOnlyList<Peer> peers, ZDO zdo)
    {
        var extZDO = zdo.GetExtension<IZDOWithPrefabInfo>();
        if (extZDO.PrefabInfo is not { } prefabInfo)
            prefabInfo = GetPrefabInfo(extZDO);

        var result = ProcessResult.Default;
        if (prefabInfo is null)
            result |= ProcessResult.UnregisterProcessor;
        else
        {
            _inputs.Update(peers, zdo, prefabInfo);
            result |= Process(_inputs);
        }
        return result;
    }

    protected abstract ProcessResult Process(Inputs inputs);

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
        var any = false;
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
            any = true;
        }
        if (!any)
            return default;
        var prefabInfo = (TPrefabInfo)_prefabInfoCtor!.Invoke(args);
        prefabInfo.PrefabHash = prefabHash;
        prefabInfo.PrefabName = prefab.name;
        prefabInfo.Components = componentDict;
        return prefabInfo;
    }
}