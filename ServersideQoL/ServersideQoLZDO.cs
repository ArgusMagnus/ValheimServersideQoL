using System.Diagnostics;
using UnityEngine;

namespace ServersideQoL;

public sealed partial class ServersideQoLZDO(ZDO zdo) : IEquatable<ServersideQoLZDO>
{
  static readonly Dictionary<int, IReadOnlyList<Processor>> __processors = [];
  static readonly ZPackage __pkg = new();
  static readonly Stack<Dictionary<Type, object>> __componentFieldAccessorCache = [];
  static bool _onDestroyedSubscribed;

  public ZDO ZDO { get; } = zdo;
  public PrefabInfo? PrefabInfo
  {
    get;
    internal set
    {
      if (ComponentFieldAccessors is { } componentFieldAccessors)
      {
        componentFieldAccessors.Clear();
        __componentFieldAccessorCache.Push(componentFieldAccessors);
      }

      field = value;
      Processors = value?.EnabledProcessors ?? [];
      HasProcessors = Processors.Count is not 0;
      ExclusivityCheckDone = false;
      _hasFields = default;
      ComponentFieldAccessors = default;
      ScheduleBefore = float.NaN;
      _destroyed = default;
#if DEBUG
      Debug = default;
#endif
    }
  }

  Action<ServersideQoLZDO>? _destroyed;
  public event Action<ServersideQoLZDO>? Destroyed
  {
    add
    {
      if (!_onDestroyedSubscribed)
      {
        ZDOMan.instance.m_onZDODestroyed += OnDestroyed;
        _onDestroyedSubscribed = true;
      }
      _destroyed += value;
    }
    remove => _destroyed -= value;
  }

  public ZDOVars Vars => new(ZDO);

  public TPrefabInfo? GetProcessorPrefabInfo<TPrefabInfo>()
      where TPrefabInfo : notnull, ProcessorPrefabInfo
      => PrefabInfo?.GetExtension<IProcessorPrefabInfo<TPrefabInfo>>().PrefabInfo;

  internal bool HasProcessors { get; private set; }
  internal IReadOnlyList<Processor> Processors { get; private set; } = [];
  internal bool ExclusivityCheckDone { get; set; }
  bool? _hasFields;
  static readonly int __hasFieldsHash = ZNetView.CustomFieldsStr.GetStableHashCode();
  public bool HasFields => _hasFields ??= ZDO.GetBool(__hasFieldsHash);
  internal Dictionary<Type, object>? ComponentFieldAccessors { get; private set; }
  internal float ScheduleBefore { get; set; } = float.NaN;

#if DEBUG
  public int Debug { get; set; }
#endif

  /// <summary>
  /// Delays rescheduling for max <paramref name="delayInSeconds"/>.
  /// The lowest delay wins if this method is called multiple times.
  /// Changed instances (owner/data revision changes) are always processed immediatly and are not affected by this delay.
  /// </summary>
  public void DelaySchedulingFor(float delayInSeconds)
  {
    var scheduleBefore = Time.realtimeSinceStartup + delayInSeconds;
    if (!(scheduleBefore > ScheduleBefore))
      ScheduleBefore = scheduleBefore;
  }

  internal void SetHasFields()
  {
    if (_hasFields is not true)
      ZDO.Set(__hasFieldsHash, (_hasFields = true).Value);
  }

  int _prevPrefab = -1;
  internal bool UpdatePrefab()
  {
    var prefab = ZDO.GetPrefab();
    if (prefab == _prevPrefab)
      return false;
    _prevPrefab = prefab;
    return true;
  }

  ushort _prevOwnerRev = ushort.MaxValue;
  uint _prevDataRev = uint.MaxValue;
  internal bool UpdateOwnerAndDataRevisions()
  {
    if ((ZDO.OwnerRevision, ZDO.DataRevision) == (_prevOwnerRev, _prevDataRev))
      return false;
    (_prevOwnerRev, _prevDataRev) = (ZDO.OwnerRevision, ZDO.DataRevision);
    return true;
  }

  static void OnDestroyed(ZDO zdo)
    => zdo.ServersideQoLZDO._destroyed?.Invoke(zdo.ServersideQoLZDO);

  internal void Unregister(IReadOnlyList<Processor> processors)
  {
    static IReadOnlyList<Processor> UnregisterCore(IReadOnlyList<Processor> processors, IReadOnlyList<Processor> zdoProcessors)
    {
      if (zdoProcessors.Count is 0)
        return zdoProcessors;

      var hash = 0;
      foreach (var processor in zdoProcessors.Enumerate())
      {
        var keep = true;
        foreach (var remove in processors.Enumerate())
        {
          if (ReferenceEquals(processor, remove))
          {
            keep = false;
            break;
          }
        }
        if (keep)
          hash = (hash, processor.GetType()).GetHashCode();
      }

      if (!__processors.TryGetValue(hash, out var newProcessors))
      {
        var list = new List<Processor>();
        __processors.Add(hash, newProcessors = list);
        foreach (var processor in zdoProcessors.Enumerate())
        {
          var keep = true;
          foreach (var remove in processors.Enumerate())
          {
            if (ReferenceEquals(processor, remove))
            {
              keep = false;
              break;
            }
          }
          if (keep)
            list.Add(processor);
        }
      }
      return newProcessors;
    }

    Processors = UnregisterCore(processors, Processors ?? []);
    HasProcessors = Processors.Count is not 0;
  }

  //internal void Reregister(IReadOnlyList<Processor> processors)
  //{
  //    static IReadOnlyList<Processor> ReregisterCore(IReadOnlyList<Processor> processors, IReadOnlyList<Processor> zdoProcessors, IReadOnlyList<Processor> allProcessors)
  //    {

  //    }


  //    // does this implementation make sense?
  //    var extZdo = GetExtension<IServersideQoLZDO>();
  //    var zdoProcessors = Processors ?? [];
  //    var allProcessors = PrefabInfo?.EnabledProcessors ?? [];
  //    var unregister = new List<Processor>(allProcessors.Count);
  //    foreach (var processor in allProcessors.AsEnumerable())
  //    {
  //        var found = false;
  //        foreach (var keep in processors.AsEnumerable())
  //        {
  //            if (ReferenceEquals(processor, keep))
  //            {
  //                found = true;
  //                break;
  //            }
  //        }
  //        if (found)
  //            continue;

  //        foreach (var keep in zdoProcessors.AsEnumerable())
  //        {
  //            if (ReferenceEquals(processor, keep))
  //            {
  //                found = true;
  //                break;
  //            }
  //        }

  //        if (!found)
  //            unregister.Add(processor);
  //    }
  //    Ungregister(unregister);
  //}

  public void UnregisterAllExcept(Processor keep)
  {
    static IReadOnlyList<Processor> UnregisterAllExceptCore(Processor keep, IReadOnlyList<Processor> zdoProcessors)
    {
      if (!zdoProcessors.Contains(keep))
        return [];
      var hash = (0, keep.GetType()).GetHashCode();
      if (!__processors.TryGetValue(hash, out var processors))
        __processors.Add(hash, processors = [keep]);
      return processors;
    }

    Processors = UnregisterAllExceptCore(keep, Processors ?? []);
    HasProcessors = Processors.Count is not 0;
  }

  public void UnregisterAll()
  {
    Processors = [];
    HasProcessors = false;
  }

  public void ReregisterAll()
  {
    Processors = PrefabInfo?.EnabledProcessors ?? [];
    HasProcessors = Processors.Count is not 0;
  }

  public ComponentFieldAccessor<TComponent> Fields<TComponent>() where TComponent : MonoBehaviour
  {
    if (ComponentFieldAccessors is not { } accessors || !accessors.TryGetValue(typeof(TComponent), out var accessorObj))
    {
      if (PrefabInfo?.Components is not { } components || !components.TryGetValue(typeof(TComponent), out var componentList))
        throw new KeyNotFoundException(typeof(TComponent).FullName);

      accessorObj = new ComponentFieldAccessor<TComponent>(this, (TComponent)componentList[0]);

      if (!__componentFieldAccessorCache.TryPop(out accessors))
        accessors = [];
      accessors.Add(typeof(TComponent), accessorObj);
    }
    return (ComponentFieldAccessor<TComponent>)accessorObj;
  }

  public void Destroy()
  {
    ClaimOwnershipInternal();
    ZDOMan.instance.DestroyZDO(ZDO);
  }

  public ServersideQoLZDO CreateClone()
  {
    var prefab = ZDO.GetPrefab();
    var pos = ZDO.GetPosition();
    var owner = ZDO.GetOwner();
    __pkg.Clear();
    ZDO.Serialize(__pkg);
    __pkg.Size(); // force flush

    var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
    __pkg.SetPos(0);
    zdo.Deserialize(__pkg);
    zdo.SetOwnerInternal(owner);
    return zdo.ServersideQoLZDO;
  }

  public ServersideQoLZDO Recreate()
  {
    var zdo = CreateClone();

    // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
    //_addData?.Recreated?.Invoke(this, zdo);

    if (PrefabInfo is { ReleaseOwnershipOnRecreate: true })
      zdo.ReleaseOwnershipInternal(); // required for physics to work again

    Destroy();
    return zdo;
  }

  public TimeSpan GetTimeSinceSpawned() => ZNet.instance.GetTime() - Vars.GetSpawnTime();

  public void ClaimOwnership() => ZDO.SetOwner(ZDOMan.GetSessionID());
  public void ClaimOwnershipInternal() => ZDO.SetOwnerInternal(ZDOMan.GetSessionID());
  public void ReleaseOwnership() => ZDO.SetOwner(0);
  public void ReleaseOwnershipInternal() => ZDO.SetOwnerInternal(0);

  public bool IsOwnerOrUnassigned() => !ZDO.HasOwner() || ZDO.IsOwner();

  public void SetModAsCreator(Processor.CreatorMarkers marker = Processor.CreatorMarkers.None) => Vars.SetCreator((long)ServersideQoLPlugin.PluginGuidHash | (long)((ulong)marker << 32));
  public bool IsModCreator(out Processor.CreatorMarkers marker)
  {
    marker = Processor.CreatorMarkers.None;
    if ((int)Vars.GetCreator() != ServersideQoLPlugin.PluginGuidHash)
      return false;
    marker = (Processor.CreatorMarkers)((ulong)Vars.GetCreator() >> 32);
    return true;
  }
  public bool IsModCreator() => IsModCreator(out _);

  public bool IsAnyCloserThan(IReadOnlyList<Peer> peers, float distance)
  {
    distance *= distance;
    var pos = ZDO.GetPosition();
    foreach (var peer in peers.Enumerate())
    {
      if (Utils.DistanceSqr(peer.RefPos, pos) < distance)
        return true;
    }
    return false;
  }

  public bool Equals(ServersideQoLZDO? other) => ZDO.Equals(other?.ZDO);
  public override bool Equals(object obj) => Equals(obj as ServersideQoLZDO);
  public override int GetHashCode() => ZDO.GetHashCode();

  [Conditional("DEBUG")]
  public void AssertIs<T>() where T : MonoBehaviour
      => System.Diagnostics.Debug.Assert(PrefabInfo?.Prefab.GetComponentInChildren<T>() is not null);

  [Conditional("DEBUG")]
  public void AssertIsAll<T1, T2>() where T1 : MonoBehaviour where T2 : MonoBehaviour
      => System.Diagnostics.Debug.Assert(PrefabInfo?.Prefab is { } prefab &&
          prefab.GetComponentInChildren<T1>() is not null &&
          prefab.GetComponentInChildren<T2>() is not null);
}
