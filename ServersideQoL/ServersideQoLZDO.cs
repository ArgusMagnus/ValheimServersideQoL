using System.Diagnostics;
using UnityEngine;

namespace ServersideQoL;

public sealed partial class ServersideQoLZDO(ZDO zdo)
{
  static readonly Dictionary<int, IReadOnlyList<Processor>> __processors = [];
  static readonly ZPackage __pkg = new();
  static readonly Stack<Dictionary<Processor, (uint, uint)>> __dataRevCache = [];
  static readonly Stack<Dictionary<Type, object>> __componentFieldAccessorCache = [];
  static bool _onDestroyedSubscribed;

  public ZDO ZDO { get; } = zdo;
  public PrefabInfo? PrefabInfo
  {
    get;
    internal set
    {
      if (ProcessorDataRevisions is { } dataRevisions)
      {
        dataRevisions.Clear();
        __dataRevCache.Push(dataRevisions);
      }

      if (ComponentFieldAccessors is { } componentFieldAccessors)
      {
        componentFieldAccessors.Clear();
        __componentFieldAccessorCache.Push(componentFieldAccessors);
      }

      field = value;
      Processors = value?.EnabledProcessors ?? [];
      HasProcessors = Processors.Count is not 0;
      CyclicProcessors = value?.EnabledCyclicProcessors ?? [];
      HasCyclicProcessors = CyclicProcessors.Count is not 0;
      ProcessorDataRevisions = default;
      HasFields = default;
      ComponentFieldAccessors = default;
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

  internal bool HasProcessors { get; set; }
  internal IReadOnlyList<Processor> Processors { get; set; } = [];
  internal bool HasCyclicProcessors { get; set; }
  internal IReadOnlyList<Processor> CyclicProcessors { get; set; } = [];
  internal Dictionary<Processor, (uint Data, uint Owner)>? ProcessorDataRevisions { get; set; }
  internal bool HasFields { get; set; }
  internal Dictionary<Type, object>? ComponentFieldAccessors { get; set; }

  int _prevPrefab;

  internal bool UpdatePrefab()
  {
    var prefab = ZDO.GetPrefab();
    if (prefab == _prevPrefab)
      return false;
    _prevPrefab = prefab;
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
    CyclicProcessors = UnregisterCore(processors, CyclicProcessors ?? []);
    HasCyclicProcessors = CyclicProcessors.Count is not 0;

    if (ProcessorDataRevisions is { } dataRevisions)
    {
      foreach (var processor in processors.Enumerate())
        dataRevisions.Remove(processor);
    }
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
      var hash = (0, keep.GetType()).GetHashCode();
      if (!__processors.TryGetValue(hash, out var processors))
        __processors.Add(hash, processors = [keep]);
      return processors;
    }

    Processors = UnregisterAllExceptCore(keep, Processors ?? []);
    HasProcessors = Processors.Count is not 0;
    CyclicProcessors = UnregisterAllExceptCore(keep, CyclicProcessors ?? []);
    HasCyclicProcessors = CyclicProcessors.Count is not 0;

    if (HasProcessors && ProcessorDataRevisions is { } dataRevisions)
    {
      foreach (var processor in Processors.Enumerate())
      {
        if (!ReferenceEquals(processor, keep))
          dataRevisions.Remove(processor);
      }
    }
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
    CyclicProcessors = PrefabInfo?.EnabledCyclicProcessors ?? [];
    HasCyclicProcessors = CyclicProcessors.Count is not 0;
  }

  internal void UpdateProcessorDataRevision(Processor processor, bool onlyExisting = false)
  {
    if (ProcessorDataRevisions is not { } dataRevisions)
    {
      if (onlyExisting)
        return;
      if (!__dataRevCache.TryPop(out dataRevisions))
        dataRevisions = [];
      ProcessorDataRevisions = dataRevisions;
    }

    if (onlyExisting)
      dataRevisions.TryAdd(processor, (ZDO.DataRevision, ZDO.OwnerRevision));
    else
      dataRevisions[processor] = (ZDO.DataRevision, ZDO.OwnerRevision);
  }

  internal void ResetProcessorDataRevision(Processor processor)
      => ProcessorDataRevisions?.Remove(processor);

  internal bool CheckProcessorDataRevisionChanged(Processor processor)
  {
    var dataRevisions = ProcessorDataRevisions;
    if (dataRevisions is null || !dataRevisions.TryGetValue(processor, out var revision) || revision != (ZDO.DataRevision, ZDO.OwnerRevision))
      return true;
    return false;
  }

  public ComponentFieldAccessor<TComponent> Fields<TComponent>() where TComponent : MonoBehaviour
  {
    if (ComponentFieldAccessors is not { } accessors || !accessors.TryGetValue(typeof(TComponent), out var accessorObj))
    {
      if (PrefabInfo?.Components is not { } components || !components.TryGetValue(typeof(TComponent), out var componentList))
        throw new KeyNotFoundException();

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
      if (Utils.DistanceSqr(peer.m_refPos, pos) < distance)
        return true;
    }
    return false;
  }

  [Conditional("DEBUG")]
  public void AssertIs<T>() where T : MonoBehaviour
      => System.Diagnostics.Debug.Assert(PrefabInfo?.Prefab.GetComponentInChildren<T>() is not null);

  [Conditional("DEBUG")]
  public void AssertIsAll<T1, T2>() where T1 : MonoBehaviour where T2 : MonoBehaviour
      => System.Diagnostics.Debug.Assert(PrefabInfo?.Prefab is { } prefab &&
          prefab.GetComponentInChildren<T1>() is not null &&
          prefab.GetComponentInChildren<T2>() is not null);
}
