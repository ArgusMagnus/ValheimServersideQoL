using System.Reflection;
using UnityEngine;
using Valheim.ZDOExtender;

namespace ServersideQoL;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ProcessorAttribute(string id) : Attribute
{
    public Guid Id { get; } = new(id);

    /// <summary>
    /// True if the processor should run cyclically,
    /// false if the processor should only run when the data or owner revision of a <see cref="ZDO"/> changes
    /// </summary>
    public bool Cyclic { get; init; }

    /// <summary>
    /// <see cref="Id"/>s of the processors this processor must run before.
    /// </summary>
    public Guid[] RunBeforeIds { get; private init; } = [];

    /// <inheritdoc cref="RunBeforeIds"/>
    public string[] RunBefore { get; init => RunBeforeIds = [.. (field = value).Select(static x => new Guid(x))]; } = [];

    /// <summary>
    /// <see cref="Id"/>s of the processors this processor must run after.
    /// </summary>
    public Guid[] RunAfterIds { get; private init; } = [];

    /// <inheritdoc cref="RunAfterIds"/>
    public string[] RunAfter { get; init => RunAfterIds = [.. (field = value).Select(static x => new Guid(x))]; } = [];

    /// <summary>
    /// True to run this processor only if other processors depend on it via <see cref="RunBefore"/>/<see cref="RunAfter"/>
    /// </summary>
    public bool OnlyWhenDependedOn { get; init; }

    /// <summary>
    /// Priority of the processor if no other constraints apply. Processors with lower priority are run first.
    /// </summary>
    public int Priority { get; init; } = 0;
}

public abstract class Processor
{
    internal ProcessorAttribute Attribute { get; }
    internal IServersideQoLPlugin Plugin { get; set; } = default!;

    protected readonly HashSet<ZDO> PlacedObjects = [];

    static bool __initialized;
    static ZDO? _dataZDO;

    static bool __enableProcessingTimeMonitoring;
    public double ProcessingTimeSeconds { get; private set; }
    public double TotalProcessingTimeSeconds { get; private set; }

    private protected Processor()
    {
        Attribute = GetType().GetCustomAttribute<ProcessorAttribute>() ?? throw new Exception($"Required {nameof(ProcessorAttribute)} missing on type {GetType().FullName}");
    }

    private protected abstract void ValidateProcessor();
    internal void ValidateProcessorInternal() => ValidateProcessor();

    private protected abstract bool InitializePrefabInfo(PrefabInfo prefabInfo);
    internal bool InitializePrefabInfoInternal(PrefabInfo prefabInfo) => InitializePrefabInfo(prefabInfo);

    private protected abstract void AddPrefabInfoInterface(TypeExtensionBuilder<IPrefabInfo, PrefabInfo> prefabInfoBuilder);
    internal void AddPrefabInfoInterfaceInternal(TypeExtensionBuilder<IPrefabInfo, PrefabInfo> prefabInfoBuilder) => AddPrefabInfoInterface(prefabInfoBuilder);

    static class InstanceCache<T>
        where T : Processor, new()
    {
        public static readonly T Instance = new();
    }

    public static T Instance<T>()
        where T : Processor, new()
        => InstanceCache<T>.Instance;

    internal protected virtual void Initialize(bool firstTime)
    {
        __enableProcessingTimeMonitoring = Config.Instance.DiagnosticLogs.Value;
        //__teleportableItems = null;
        //ZoneSystemSendGlobalKeys.GlobalKeysChanged -= UpdateTeleportableItems;

        if (!firstTime)
        {
            __initialized = false;
            return;
        }

        if (__initialized)
            return;
        __initialized = true;
        _dataZDO = null;

        foreach (var zdo in ZDOMan.instance.GetObjects())
        {
            if (!zdo.IsModCreator(out var marker))
                continue;

            if (marker is 0)
            {
                zdo.Destroy();
                continue;
            }

            if ((marker & CreatorMarkers.DataZDO) is not 0)
            {
                if (_dataZDO is null)
                    _dataZDO = zdo;
                else
                {
                    ServersideQoL.Logger.LogError("More then one DataZDO found, destroying the second one");
                    zdo.Destroy();
                }
            }
            if ((marker & CreatorMarkers.ProcessorOwned) is not 0)
            {
                if (ServersideQoL.Processors.TryGetValue(zdo.Vars.GetProcessorId(), out var processor))
                    processor.PlacedObjects.Add(zdo);
            }
        }
    }

    protected static ZDO DataZDO
    {
        get
        {
            if (_dataZDO is null)
            {
                _dataZDO = ZDOMan.instance.CreateNewZDO(new(WorldGenerator.waterEdge * 10, -1000f, WorldGenerator.waterEdge * 10), Prefabs.Sconce);
                _dataZDO.SetPrefab(Prefabs.Sconce);
                _dataZDO.Persistent = true;
                _dataZDO.Distant = false;
                _dataZDO.Type = ZDO.ObjectType.Default;
                _dataZDO.SetModAsCreator(CreatorMarkers.DataZDO);
                _dataZDO.Vars.SetHealth(-1);
                _dataZDO.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, false);
                _dataZDO.Fields<WearNTear>().Set(static () => x => x.m_noRoofWear, false).Set(static () => x => x.m_noSupportWear, false).Set(static () => x => x.m_health, -1);
                _dataZDO.UnregisterAll();
            }
            return _dataZDO;
        }
    }

    private protected abstract ProcessResult Process(IReadOnlyList<Peer> peers, ZDO zdo);
    protected virtual void PreProcess(PeersEnumerable peers) { }

    internal ProcessResult ProcessInternal(IReadOnlyList<Peer> peers, ZDO zdo)
    {
        if (!__enableProcessingTimeMonitoring)
            return Process(peers, zdo);

        var start = Time.realtimeSinceStartupAsDouble;
        var result = Process(peers, zdo);
        ProcessingTimeSeconds += Time.realtimeSinceStartupAsDouble - start;
        return result;
    }

    internal void PreProcessInternal(PeersEnumerable peers)
    {
        if (!__enableProcessingTimeMonitoring)
            PreProcess(peers);
        else
        {
            TotalProcessingTimeSeconds += ProcessingTimeSeconds;
            var start = Time.realtimeSinceStartupAsDouble;
            PreProcess(peers);
            ProcessingTimeSeconds = Time.realtimeSinceStartupAsDouble - start;
        }
    }

    internal protected bool ClaimExclusive(ZDO zdo) => PlacedObjects.Contains(zdo);

    protected ZDO PlaceObject(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None)
        => PlaceObject(pos, prefab, Quaternion.Euler(0, rot, 0), marker);

    protected ZDO PlaceObject(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None)
    {
        var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
        PlacedObjects.Add(zdo);

        zdo.SetPrefab(prefab);
        zdo.Persistent = true;
        zdo.Distant = false;
        zdo.Type = ZDO.ObjectType.Default;
        zdo.SetRotation(rot);
        zdo.SetModAsCreator(marker);
        zdo.Vars.SetHealth(-1);
        if (marker.HasFlag(CreatorMarkers.ProcessorOwned))
            zdo.Vars.SetProcessorId(Attribute.Id);

        return zdo;
    }

    protected ZDO PlacePiece(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None)
        => PlacePiece(pos, prefab, Quaternion.Euler(0, rot, 0), marker);

    protected ZDO PlacePiece(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None)
    {
        var zdo = PlaceObject(pos, prefab, rot, marker);
        zdo.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, false);
        zdo.Fields<WearNTear>()
            .Set(static () => x => x.m_noRoofWear, false)
            .Set(static () => x => x.m_noSupportWear, false)
            .Set(static () => x => x.m_health, -1);
        return zdo;
    }

    protected ZDO RecreatePiece(ZDO zdo)
    {
        if (!PlacedObjects.Remove(zdo))
            throw new ArgumentException();
        PlacedObjects.Add(zdo = zdo.Recreate());
        return zdo;
    }

    protected void DestroyObject(ZDO zdo)
    {
        if (!PlacedObjects.Remove(zdo))
            throw new ArgumentException();
        zdo.Destroy();
    }

    [Flags]
    internal protected enum ProcessResult
    {
        Default = 0,
        WaitForZDORevisionChange = 1 << 0,
        UnregisterProcessor = 1 << 1,
        DestroyZDO = 1 << 2,
        RecreateZDO = 1 << 3
    }

    [Flags]
    public enum CreatorMarkers : uint
    {
        None = 0,
        DataZDO = 1u << 0,
        ProcessorOwned = 1u << 1,
        //Persistent = 1u << 2
    }

    private protected interface IProcessorPrefabInfo<TPrefabInfo> : IPrefabInfo
        where TPrefabInfo : ProcessorPrefabInfo
    {
        TPrefabInfo? PrefabInfo { get; set; }
    }
}

public abstract class Processor<TPrefabInfo> : Processor
    where TPrefabInfo : ProcessorPrefabInfo
{

    readonly ConstructorInfo? _prefabInfoCtor;
    readonly ParameterInfo[]? _prefabInfoCtorParameters;
    //readonly Dictionary<int, TPrefabInfo?> _prefabInfoByHash = [];

    protected Processor()
    {
        if (typeof(TPrefabInfo).GetConstructors() is { Length: 1 } ctors)
        {
            _prefabInfoCtor = ctors[0];
            _prefabInfoCtorParameters = _prefabInfoCtor.GetParameters();
        }
    }

    private protected override void AddPrefabInfoInterface(TypeExtensionBuilder<IPrefabInfo, PrefabInfo> prefabInfoBuilder)
        => prefabInfoBuilder.AddInterface<IProcessorPrefabInfo<TPrefabInfo>>();

    private protected override bool InitializePrefabInfo(PrefabInfo prefabInfo)
    {
        var ext = prefabInfo.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>();
        ext.PrefabInfo ??= GetProcessorPrefabInfo(prefabInfo);
        return ext.PrefabInfo is not null;
    }

    protected TPrefabInfo? GetPrefabInfo(ZDO zdo)
        => zdo.GetExtension<IServersideQoLZDO>().PrefabInfo?.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>().PrefabInfo;

    TPrefabInfo? GetProcessorPrefabInfo(PrefabInfo prefabInfo)
    {
        var prefab = prefabInfo.Prefab;
        var components = prefabInfo.Components;
        var args = new object?[_prefabInfoCtorParameters!.Length];
        var any = false;
        for (int i = 0; i < _prefabInfoCtorParameters.Length; i++)
        {
            var par = _prefabInfoCtorParameters[i];
            if (!components.TryGetValue(par.ParameterType, out var component))
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
        return (TPrefabInfo)_prefabInfoCtor!.Invoke(args);
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

    private protected sealed override ProcessResult Process(IReadOnlyList<Peer> peers, ZDO zdo)
    {
        // todo: we should already have IServersideQoLZDO when this method is called
        var extZDO = zdo.GetExtension<IServersideQoLZDO>();
        var result = ProcessResult.Default;
        if (extZDO.PrefabInfo?.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>().PrefabInfo is not { } prefabInfo)
            result |= ProcessResult.UnregisterProcessor;
        else
            result |= Process(zdo, peers, prefabInfo);
        return result;
    }

    protected abstract ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, TPrefabInfo prefabInfo);
}