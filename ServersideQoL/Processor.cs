using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ProcessorAttribute(string id) : Attribute
{
  public Guid Id { get; } = new(id);

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
  public bool? RunBefore { get; }

  ProcessorDependencyAttribute(Guid processorId, bool required, bool? runBefore)
  {
    ProcessorId = processorId;
    Required = required;
    RunBefore = runBefore;
  }

  private protected ProcessorDependencyAttribute(string processorId, bool? runBefore)
      : this(new Guid(processorId), false, runBefore) { }

  private protected ProcessorDependencyAttribute(Type processorType, bool? runBefore)
      : this(processorType.GetCustomAttribute<ProcessorAttribute>()?.Id ?? default, true, runBefore) { }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DependsOnAttribute(string processorId) : ProcessorDependencyAttribute(processorId, null);

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class DependsOnAttribute<T>() : ProcessorDependencyAttribute(typeof(T), null) where T : Processor, new();

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

  protected HashSet<ServersideQoLZDO> PlacedObjects { get; } = [];

  static ServersideQoLZDO? __dataZDO;

  static bool __enableProcessingTimeMonitoring;
  public double ProcessingTimeSeconds { get; private set; }
  public double TotalProcessingTimeSeconds { get; private set; }

#if DEBUG
  private protected static readonly Dictionary<int, Type> __prefabInfoTypes = [];
#endif

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

    foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
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
        if (ServersideQoLPlugin.Instance.Processors.TryGetValue(GetProcessorId(zdo), out var processor))
        {
          processor.PlacedObjects.Add(zdo);
          zdo.Destroyed += processor.OnPlacedObjectDestroyed;
        }
      }
    }
  }

  internal static void StaticPreProcess(PeersEnumerable peers)
  {
    HeightmapUtils.CleanUp(peers);
  }

  protected static IReadOnlyDictionary<string, PieceTable> PieceTablesByPieceName => ServersideQoLPlugin.Instance.PieceTablesByPieceName;

  internal protected virtual void Initialize() { }


  static int __processorId = ServersideQoLPlugin.RegisterServerVar("ProcessorId");
  public static Guid GetProcessorId(ServersideQoLZDO zdo, Guid defaultValue = default) => zdo.ZDO.GetByteArray(__processorId, []) is { Length: > 0 } arr ? new(arr) : defaultValue;
  public static void SetProcessorId(ServersideQoLZDO zdo, Guid value) => zdo.ZDO.Set(__processorId, value == default ? [] : value.ToByteArray());

  protected static IReadOnlyDictionary<Heightmap.Biome, Character> BossesByBiome => field ??= new Func<IReadOnlyDictionary<Heightmap.Biome, Character>>(static () =>
  {
    var bosses = new Dictionary<Heightmap.Biome, Character>();
    foreach (var includeDungeons in (IEnumerable<bool>)[false, true])
    {
      foreach (var location in ZoneSystem.instance.m_locations)
      {
        if (!location.m_enable || !location.m_prioritized || location.m_biome is Heightmap.Biome.None or Heightmap.Biome.All or Heightmap.Biome.Ocean)
          continue;

        if (bosses.ContainsKey(location.m_biome))
          continue;

        try { location.m_prefab.Load(); }
        catch (Exception ex)
        {
          ServersideQoLPlugin.Logger.LogWarning($"Loading location asset {location.m_prefabName} failed: {ex}");
          continue;
        }

        if (location.m_prefab.Asset is not { } asset)
        {
          ServersideQoLPlugin.Logger.LogWarning($"Loading location asset {location.m_prefabName} failed");
          continue;
        }

        var bowl = asset.GetComponentInChildren<OfferingBowl>();
        if (includeDungeons && bowl is null && asset.GetComponentInChildren<DungeonGenerator>() is { } dungeonGen)
        {
          foreach (var roomRef in dungeonGen.GetAvailableRoomPrefabs())
          {
            roomRef.Load();
            var room = roomRef.Asset.GetComponent<Room>();
            bowl = room.GetComponentInChildren<OfferingBowl>();
            if (bowl is not null)
              break;
          }
        }
        if (bowl is not null)
          bosses.Add(location.m_biome, bowl.m_bossPrefab.GetComponent<Character>());
      }
    }
    return bosses;
  }).Invoke();

  protected static ServersideQoLZDO DataZDO
  {
    get
    {
      if (__dataZDO is null)
      {
        __dataZDO = ZDOMan.instance.CreateNewZDO(new(WorldGenerator.waterEdge * 10, -1000f, WorldGenerator.waterEdge * 10), Prefabs.Sconce).ServersideQoLZDO;
        __dataZDO.ZDO.SetPrefab(Prefabs.Sconce);
        __dataZDO.ZDO.Persistent = true;
        __dataZDO.ZDO.Distant = false;
        __dataZDO.ZDO.Type = ZDO.ObjectType.Default;
        __dataZDO.SetModAsCreator(CreatorMarkers.DataZDO);
        __dataZDO.Vars.SetHealth(-1);
        __dataZDO.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, false);
        __dataZDO.Fields<WearNTear>().Set(static () => x => x.m_noRoofWear, false).Set(static () => x => x.m_noSupportWear, false).Set(static () => x => x.m_health, -1);
        __dataZDO.UnregisterAll();
      }
      return __dataZDO;
    }
  }

  protected void ScheduleReprocessing(ServersideQoLZDO zdo)
    => ServersideQoLPlugin.Instance.ScheduleReprocessing(zdo);

  protected void ScheduleReprocessing(ServersideQoLZDO zdo, float delayInSeconds)
    => ServersideQoLPlugin.Instance.ScheduleReprocessing(zdo, delayInSeconds);

  private protected abstract ProcessResult Process(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo);
  protected virtual void PreProcess(PeersEnumerable peers) { }

  internal ProcessResult ProcessInternal(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo)
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

  internal protected bool ClaimExclusive(ServersideQoLZDO zdo) => PlacedObjects.Contains(zdo);

  //protected bool CheckMinDistance(IReadOnlyList<Peer> peers, ZDO zdo)
  //    => CheckMinDistance(peers, zdo, Config.Instance.MinPlayerDistance.Value);

  protected static bool CheckMinDistance(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo, float minDistance)
  {
    minDistance *= minDistance;
    foreach (var peer in peers.AsEnumerable())
    {
      if (Utils.DistanceSqr(peer.RefPos, zdo.ZDO.GetPosition()) < minDistance)
        return false;
    }
    return true;
  }

  protected static ServersideQoLZDO Spawn(int prefab, Vector3 pos, Quaternion rot, long owner = 0)
  {
    var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
    zdo.SetPrefab(prefab);
    zdo.Persistent = true;
    zdo.Distant = false;
    zdo.Type = ZDO.ObjectType.Default;
    zdo.SetRotation(rot);

    zdo.SetOwnerInternal(owner);

    zdo.ServersideQoLZDO.PrefabInfo = ServersideQoLPlugin.Instance.GetPrefabInfo(prefab);
    return zdo.ServersideQoLZDO;
  }

  void OnPlacedObjectDestroyed(ServersideQoLZDO zdo) => PlacedObjects.Remove(zdo);

  protected ServersideQoLZDO PlaceObject(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None, long owner = 0)
      => PlaceObject(pos, prefab, Quaternion.Euler(0, rot, 0), marker, owner);

  protected ServersideQoLZDO PlaceObject(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None, long owner = 0)
  {
    var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab).ServersideQoLZDO;
    zdo.ZDO.SetPrefab(prefab);
    zdo.ZDO.Persistent = true;
    zdo.ZDO.Distant = false;
    zdo.ZDO.Type = ZDO.ObjectType.Default;
    zdo.ZDO.SetRotation(rot);
    zdo.SetModAsCreator(marker);
    zdo.Vars.SetHealth(-1);
    if (marker.HasFlag(CreatorMarkers.ProcessorOwned))
      SetProcessorId(zdo, Attribute.Id);

    zdo.ZDO.SetOwnerInternal(owner);

    zdo.PrefabInfo = ServersideQoLPlugin.Instance.GetPrefabInfo(prefab);
    PlacedObjects.Add(zdo);
    zdo.Destroyed += OnPlacedObjectDestroyed;
    return zdo;
  }

  protected ServersideQoLZDO PlacePiece(Vector3 pos, int prefab, float rot, CreatorMarkers marker = CreatorMarkers.None)
      => PlacePiece(pos, prefab, Quaternion.Euler(0, rot, 0), marker);

  protected ServersideQoLZDO PlacePiece(Vector3 pos, int prefab, Quaternion rot, CreatorMarkers marker = CreatorMarkers.None)
  {
    var zdo = PlaceObject(pos, prefab, rot, marker);
    zdo.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, false);
    zdo.Fields<WearNTear>()
        .Set(static () => x => x.m_noRoofWear, false)
        .Set(static () => x => x.m_noSupportWear, false)
        .Set(static () => x => x.m_health, -1);
    return zdo;
  }

  protected ServersideQoLZDO RecreatePiece(ServersideQoLZDO zdo)
  {
    if (!PlacedObjects.Remove(zdo))
      throw new ArgumentException();
    zdo.Destroyed -= OnPlacedObjectDestroyed;
    PlacedObjects.Add(zdo = zdo.Recreate());
    zdo.Destroyed += OnPlacedObjectDestroyed;
    return zdo;
  }

  protected void DestroyObject(ServersideQoLZDO zdo)
  {
    if (!PlacedObjects.Remove(zdo))
      throw new ArgumentException();
    zdo.Destroy();
  }

  protected static Heightmap GetHeightmap(Vector3 pos) => Heightmap.FindHeightmap(pos) ?? HeightmapUtils.CreateHeightmap(pos);
  protected static Heightmap.Biome GetBiome(Vector3 pos) => GetHeightmap(pos).GetBiome(pos);
  protected static float GetHeight(Vector3 pos) => GetHeightmap(pos).GetHeight(pos);

  protected static string ConvertToRegexPattern(string searchPattern)
  {
    searchPattern = Regex.Escape(searchPattern);
    searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
    return $"(?i)^{searchPattern}$";
  }

  [Flags]
  internal protected enum ProcessResult
  {
    Default = 0,
    //WaitForZDORevisionChange = 1 << 0, not needed as long as no cyclic processors exist
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

  protected static void ShowMessage(IEnumerable<Peer> peers, Vector3 pos, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
  {
    //Main.Instance.Logger.DevLog($"ShowMessage: {message}", LogLevel.Info);
    switch (type)
    {
      case MessageTypes.TopLeftNear:
      case MessageTypes.CenterNear:
      case MessageTypes.InWorld:
        peers = peers.Where(x => Vector3.Distance(x.RefPos, pos) <= DamageText.instance.m_maxTextDistance);
        break;

      case MessageTypes.TopLeftFar:
      case MessageTypes.CenterFar:
        peers = peers.Where(x => Vector3.Distance(x.RefPos, pos) <= Config.Instance.FarMessageRange.Value);
        break;

      default:
        return;
    }

    if (type is MessageTypes.InWorld)
      RPC.ShowInWorldText(peers.Select(static x => x.ZNetPeer.m_uid), inWorldTextType, pos, message.RemoveRichTextTags());
    else
    {
      var msgType = type is MessageTypes.TopLeftNear or MessageTypes.TopLeftFar ? MessageHud.MessageType.TopLeft : MessageHud.MessageType.Center;
      foreach (var peer in peers)
        RPC.ShowMessage(peer.ZNetPeer.m_uid, msgType, message);
    }
  }

  protected static void ShowMessage(IEnumerable<Peer> peers, ServersideQoLZDO zdo, string message, MessageTypes type, DamageText.TextType inWorldTextType = DamageText.TextType.Normal)
      => ShowMessage(peers, zdo.ZDO.GetPosition(), message, type, inWorldTextType);


  [Conditional("DEBUG")]
  protected static void DevShowMessage(ServersideQoLZDO zdo, string message, DamageText.TextType type = DamageText.TextType.Normal, [CallerFilePath] string callerFile = default!, [CallerLineNumber] int callerLineNo = default)
  {
    RPC.ShowInWorldText([0], type, zdo.ZDO.GetPosition(), $"{Path.GetFileNameWithoutExtension(callerFile)} L{callerLineNo}: {message}");
  }

  static class HeightmapUtils
  {
    static readonly List<(Vector2s ZoneId, GameObject Root)> _zoneRoots = [];

    public static Heightmap CreateHeightmap(Vector2s zoneId)
    {
      var zonePos = ZoneSystem.GetZonePos(zoneId);
      //Main.Instance.Logger.DevLog($"Creating hmap for {zoneId}");
      var root = UnityEngine.Object.Instantiate(ZoneSystem.instance.m_zonePrefab, zonePos, Quaternion.identity);
      _zoneRoots.Add((zoneId, root));
      return root.GetComponentInChildren<Heightmap>();
    }

    public static Heightmap CreateHeightmap(Vector3 refPos) => CreateHeightmap(ZoneSystem.GetZone(refPos));

    static DateTimeOffset __nextCleanup;

    public static void CleanUp(PeersEnumerable peers)
    {
      if (__nextCleanup > DateTimeOffset.UtcNow)
        return;

      for (int i = _zoneRoots.Count - 1; i >= 0; i--)
      {
        var (zone, root) = _zoneRoots[i];
        if (peers.Any(x => ZNetScene.InActiveArea(zone, x.RefPos)))
          continue;

        //Main.Instance.Logger.DevLog($"Destroying hmap for {zone}");
        _zoneRoots.RemoveAt(i);
        UnityEngine.Object.Destroy(root);
      }

      __nextCleanup = DateTimeOffset.UtcNow.AddSeconds(2);
    }
  }
}

public abstract class Processor<TPrefabInfo> : Processor
    where TPrefabInfo : notnull, ProcessorPrefabInfo
{

  readonly ConstructorInfo? _prefabInfoCtor;
  readonly ParameterInfo[]? _prefabInfoCtorParameters;
  readonly bool?[]? _prefabInfoCtorParametersNullable;

  protected Processor()
  {
    if (typeof(TPrefabInfo).GetConstructors() is { Length: 1 } ctors)
    {
      _prefabInfoCtor = ctors[0];
      _prefabInfoCtorParameters = _prefabInfoCtor.GetParameters();
      _prefabInfoCtorParametersNullable = new bool?[_prefabInfoCtorParameters.Length];
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

  protected TPrefabInfo? GetPrefabInfo(ServersideQoLZDO zdo)
      => zdo.GetProcessorPrefabInfo<TPrefabInfo>();

  TPrefabInfo? GetProcessorPrefabInfo(PrefabInfo prefabInfo)
  {
    if (_prefabInfoCtor is null || _prefabInfoCtorParameters is null || _prefabInfoCtorParametersNullable is null)
      throw new InvalidOperationException();

    if (_prefabInfoCtorParameters.Length is 0)
      return (TPrefabInfo)_prefabInfoCtor.Invoke([]);

    var prefab = prefabInfo.Prefab;
    var components = prefabInfo.Components;
    var args = new object?[_prefabInfoCtorParameters.Length];
    var any = false;
    MethodInfo? createListDef = null;
    List<Type>? warn = null;
    bool? defaultNullable = null;
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
        if (_prefabInfoCtorParametersNullable[i] is not { } nullable)
          _prefabInfoCtorParametersNullable[i] = nullable = IsNullable(par, ref defaultNullable);

        if (nullable)
          continue;
        return default;
      }

      if (assignList)
        args[i] = (createListDef ??= ((Delegate)CreateList<MonoBehaviour>).Method.GetGenericMethodDefinition()).MakeGenericMethod(type).Invoke(null, [list]);
      else
      {
        args[i] = list[0];
        if (list.Count is not 1)
          (warn ??= []).Add(type);
      }

      any = true;
    }

    if (!any)
      return default;

    //Logger.DevLog($"Instantiating {typeof(TPrefabInfo).FullName} for {prefabInfo.PrefabName}...");

    if (warn is not null)
      Logger.LogWarning($"{typeof(TPrefabInfo).FullName} has the following property types which are not lists but have multiple components in the prefab: {string.Join(", ", warn.Select(static x => x.FullName))}. Only the first component will be used.");

#if DEBUG
    var set = new HashSet<(Type, bool)>(_prefabInfoCtorParameters.Length);
    for (int i = 0; i < _prefabInfoCtorParameters.Length; i++)
      set.Add((_prefabInfoCtorParameters[i].ParameterType, _prefabInfoCtorParametersNullable[i] ??= IsNullable(_prefabInfoCtorParameters[i], ref defaultNullable)));

    var hash = 0;
    foreach (var item in set)
      hash = (hash, item).GetHashCode();

    if (!__prefabInfoTypes.TryGetValue(hash, out var otherType))
      __prefabInfoTypes.Add(hash, typeof(TPrefabInfo));
    else if (otherType != typeof(TPrefabInfo))
      Logger.LogWarning($"{typeof(TPrefabInfo).FullName} and {otherType.FullName} use the same parameters, consider using the same type");
#endif

    return (TPrefabInfo)_prefabInfoCtor.Invoke(args);

    static IReadOnlyList<T> CreateList<T>(IReadOnlyList<MonoBehaviour> list)
        where T : MonoBehaviour
        => [.. list.Cast<T>()];
  }

  bool IsNullable(ParameterInfo par, ref bool? defaultNullable)
  {
    if (par.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName is "System.Runtime.CompilerServices.NullableAttribute") is { } attr)
      return (byte)attr.ConstructorArguments[0].Value is 2;

    if (defaultNullable is null)
    {
      const string AttrName = "System.Runtime.CompilerServices.NullableContextAttribute";
      var contextAttr = _prefabInfoCtor!.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName is AttrName)
        ?? _prefabInfoCtor.DeclaringType.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName is AttrName)
        ?? _prefabInfoCtor.DeclaringType.Assembly.CustomAttributes.FirstOrDefault(static x => x.AttributeType.FullName is AttrName);
      if (contextAttr is null)
        defaultNullable = false;
      else
        defaultNullable = (byte)contextAttr.ConstructorArguments[0].Value is 2;
    }
    return defaultNullable.Value;
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

  private protected sealed override ProcessResult Process(IReadOnlyList<Peer> peers, ServersideQoLZDO zdo)
  {
    var result = ProcessResult.Default;
    if (zdo.PrefabInfo?.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>().PrefabInfo is not { } prefabInfo)
      result |= ProcessResult.UnregisterProcessor;
    else
    {
      result |= Process(zdo, peers, prefabInfo);
      //Logger.DevLog($"{GetType().Name} result: {result}");
    }
    return result;
  }

  protected abstract ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, TPrefabInfo prefabInfo);
}
