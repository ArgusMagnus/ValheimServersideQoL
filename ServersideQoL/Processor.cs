using ServersideQoL.ZDOExtender;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

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
  /// True to run this processor only if other processors depend on it via <see cref="RunBefore"/>/<see cref="RunAfter"/>
  /// </summary>
  public bool OnlyWhenDependedOn { get; init; }

  /// <summary>
  /// Priority of the processor if no other constraints apply. Processors with lower priority are run first.
  /// </summary>
  public int Priority { get; init; } = 0;
}

public abstract class ProcessorDependencyAttribute : Attribute
{
  public Guid ProcessorId { get; }

  /// <summary>
  /// If true, processors with this dependency will be dropped if the processor with Id = <see cref="ProcessorId"/> is not found or was dropped itself.
  /// </summary>
  public bool Required { get; init; }

  /// <summary>
  /// If true, processors with this dependency will be run before the processor with Id = <see cref="ProcessorId"/>, otherwise they will be run after. 
  /// </summary>
  public bool RunBefore { get; }

  ProcessorDependencyAttribute(Guid processorId, bool required, bool runBefore)
  {
    ProcessorId = processorId;
    Required = required;
    RunBefore = runBefore;
  }

  private protected ProcessorDependencyAttribute(string processorId, bool runBefore)
      : this(new Guid(processorId), false, runBefore) { }

  private protected ProcessorDependencyAttribute(Type processorType, bool runBefore)
      : this(processorType.GetCustomAttribute<ProcessorAttribute>()?.Id ?? default, true, runBefore) { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RunBeforeAttribute(string processorId) : ProcessorDependencyAttribute(processorId, true);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RunBeforeAttribute<T>() : ProcessorDependencyAttribute(typeof(T), true) where T : Processor, new();

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RunAfterAttribute(string processorId) : ProcessorDependencyAttribute(processorId, false);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class RunAfterAttribute<T>() : ProcessorDependencyAttribute(typeof(T), false) where T : Processor, new();

public abstract class Processor
{
  internal ProcessorAttribute Attribute { get; }
  internal IServersideQoLPlugin Plugin { get; private set; } = default!;
  protected Logger Logger { get; private set; } = default!;

  protected readonly HashSet<ZDO> PlacedObjects = [];

  static ZDO? __dataZDO;

  static bool __enableProcessingTimeMonitoring;
  public double ProcessingTimeSeconds { get; private set; }
  public double TotalProcessingTimeSeconds { get; private set; }

  private protected Processor()
  {
    Attribute = GetType().GetCustomAttribute<ProcessorAttribute>() ?? throw new Exception($"Required {nameof(ProcessorAttribute)} missing on type {GetType().FullName}");
  }

  internal void Init(IServersideQoLPlugin plugin, Logger logger)
  {
    Plugin = plugin;
    Logger = logger;
    ValidateProcessor();
  }

  private protected abstract void ValidateProcessor();

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

  internal static void StaticInitialize()
  {
    __enableProcessingTimeMonitoring = Config.Instance.DiagnosticLogs.Value;
    //__teleportableItems = null;
    //ZoneSystemSendGlobalKeys.GlobalKeysChanged -= UpdateTeleportableItems;

    __dataZDO = null;

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
        if (__dataZDO is null)
          __dataZDO = zdo;
        else
        {
          ServersideQoLPlugin.Logger.LogError("More then one DataZDO found, destroying the second one");
          zdo.Destroy();
        }
      }
      if ((marker & CreatorMarkers.ProcessorOwned) is not 0)
      {
        if (ServersideQoLPlugin.Processors.TryGetValue(zdo.Vars.GetProcessorId(), out var processor))
          processor.PlacedObjects.Add(zdo);
      }
    }
  }

  internal protected virtual void Initialize() { }

  protected static ZDO DataZDO
  {
    get
    {
      if (__dataZDO is null)
      {
        __dataZDO = ZDOMan.instance.CreateNewZDO(new(WorldGenerator.waterEdge * 10, -1000f, WorldGenerator.waterEdge * 10), Prefabs.Sconce);
        __dataZDO.SetPrefab(Prefabs.Sconce);
        __dataZDO.Persistent = true;
        __dataZDO.Distant = false;
        __dataZDO.Type = ZDO.ObjectType.Default;
        __dataZDO.SetModAsCreator(CreatorMarkers.DataZDO);
        __dataZDO.Vars.SetHealth(-1);
        __dataZDO.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, false);
        __dataZDO.Fields<WearNTear>().Set(static () => x => x.m_noRoofWear, false).Set(static () => x => x.m_noSupportWear, false).Set(static () => x => x.m_health, -1);
        __dataZDO.UnregisterAll();
      }
      return __dataZDO;
    }
  }

  protected void ScheduleReprocessing(ZDO zdo)
  {
    zdo.GetExtension<IServersideQoLZDO>().ResetProcessorDataRevision(this);
    ServersideQoLPlugin.Instance.ScheduleReprocessing(zdo);
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

  //protected bool CheckMinDistance(IReadOnlyList<Peer> peers, ZDO zdo)
  //    => CheckMinDistance(peers, zdo, Config.Instance.MinPlayerDistance.Value);

  protected static bool CheckMinDistance(IReadOnlyList<Peer> peers, ZDO zdo, float minDistance)
  {
    minDistance *= minDistance;
    foreach (var peer in peers.AsEnumerable())
    {
      if (Utils.DistanceSqr(peer.m_refPos, zdo.GetPosition()) < minDistance)
        return false;
    }
    return true;
  }

  protected static ZDO Spawn(int prefab, Vector3 pos, Quaternion rot, long owner = 0)
  {
    var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
    zdo.SetPrefab(prefab);
    zdo.Persistent = true;
    zdo.Distant = false;
    zdo.Type = ZDO.ObjectType.Default;
    zdo.SetRotation(rot);

    zdo.SetOwnerInternal(owner);
    return zdo;
  }

  protected ZDO PlaceObject(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None, long owner = 0)
      => PlaceObject(pos, prefab, Quaternion.Euler(0, rot, 0), marker, owner);

  protected ZDO PlaceObject(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None, long owner = 0)
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

    zdo.SetOwnerInternal(owner);

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

  protected TPrefabInfo? GetPrefabInfo<TPrefabInfo>(ZDO zdo)
      where TPrefabInfo : ProcessorPrefabInfo
      => zdo.GetExtension<IServersideQoLZDO>().PrefabInfo?.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>().PrefabInfo;

  [Flags]
  internal protected enum ProcessResult
  {
    Default = 0,
    WaitForZDORevisionChange = 1 << 0,
    UnregisterProcessor = 1 << 1,
    DestroyZDO = 1 << 2,
    RecreateZDO = 1 << 3,
    SkipOtherProcessors = 1 << 4,
    ScheduleReprocessing = 1 << 5
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

  protected static void ShowMessage(IEnumerable<Peer> peers, Vector3 pos, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
  {
    //Main.Instance.Logger.DevLog($"ShowMessage: {message}", LogLevel.Info);
    switch (type)
    {
      case MessageTypes.TopLeftNear:
      case MessageTypes.CenterNear:
      case MessageTypes.InWorld:
        peers = peers.Where(x => Vector3.Distance(x.m_refPos, pos) <= DamageText.instance.m_maxTextDistance);
        break;

      case MessageTypes.TopLeftFar:
      case MessageTypes.CenterFar:
        peers = peers.Where(x => Vector3.Distance(x.m_refPos, pos) <= Config.Instance.FarMessageRange.Value);
        break;

      default:
        return;
    }

    if (type is MessageTypes.InWorld)
      RPC.ShowInWorldText(peers.Select(static x => x.m_uid), inWorldTextType, pos, message.RemoveRichTextTags());
    else
    {
      var msgType = type is MessageTypes.TopLeftNear or MessageTypes.TopLeftFar ? MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center;
      foreach (var peer in peers)
        RPC.ShowMessage(peer.m_uid, msgType, message);
    }
  }

  protected static void ShowMessage(IEnumerable<Peer> peers, ZDO zdo, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
      => ShowMessage(peers, zdo.GetPosition(), message, type, inWorldTextType);


  [Conditional("DEBUG")]
  protected static void DevShowMessage(ZDO zdo, string message, DamageText.TextType type = DamageText.TextType.Normal, [CallerFilePath] string callerFile = default!, [CallerLineNumber] int callerLineNo = default)
  {
    RPC.ShowInWorldText([0], type, zdo.GetPosition(), $"{Path.GetFileNameWithoutExtension(callerFile)} L{callerLineNo}: {message}");
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
      => GetPrefabInfo<TPrefabInfo>(zdo);

  TPrefabInfo? GetProcessorPrefabInfo(PrefabInfo prefabInfo)
  {
    var prefab = prefabInfo.Prefab;
    var components = prefabInfo.Components;
    var args = new object?[_prefabInfoCtorParameters!.Length];
    var any = false;
    MethodInfo? createListDef = null;
    List<Type>? warn = null;
    for (int i = 0; i < _prefabInfoCtorParameters.Length; i++)
    {
      var par = _prefabInfoCtorParameters[i];
      var type = par.ParameterType;
      var assignList = false;

      if (type.IsGenericType && type.GetGenericArguments() is { Length: 1 } genericTypes && type.IsAssignableFrom(typeof(IReadOnlyList<>).MakeGenericType(genericTypes)))
      {
        type = genericTypes[0];
        assignList = true;
      }

      if (!components.TryGetValue(type, out var list))
      {
        if (par.CustomAttributes.Any(static x => x.AttributeType.FullName is "System.Runtime.CompilerServices.NullableAttribute"))
          continue;
        return default;
      }

      if (assignList)
        args[i] = (createListDef ??= ((Delegate)CreateList<MonoBehaviour>).Method.GetGenericMethodDefinition()).MakeGenericMethod(type).Invoke(null, [list]);
      else
      {
        args[i] = list[0];
        if (list.Count is not 0)
          (warn ??= []).Add(type);
      }

      any = true;
    }
    if (!any)
    {
      if (warn is not null)
        Logger.LogWarning($"{typeof(TPrefabInfo).FullName} has the following property types which are not lists but have multiple components in the prefab: {string.Join(", ", warn.Select(static x => x.FullName))}. Only the first component will be used.");
      return default;
    }
    return (TPrefabInfo)_prefabInfoCtor!.Invoke(args);

    static IReadOnlyList<T> CreateList<T>(IReadOnlyList<MonoBehaviour> list)
        where T : MonoBehaviour
        => [.. list.Cast<T>()];
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
