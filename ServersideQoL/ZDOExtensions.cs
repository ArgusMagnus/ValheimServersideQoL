using ServersideQoL.ZDOExtender;
using System.Diagnostics;
using UnityEngine;

namespace ServersideQoL;

public static partial class ZDOExtensions
{
  static readonly Dictionary<int, IReadOnlyList<Processor>> __processors = [];
  static readonly ZPackage __pkg = new();
  static readonly Stack<Dictionary<Processor, (uint, uint)>> __dataRevCache = [];
  static readonly Stack<Dictionary<Type, object>> __componentFieldAccessorCache = [];

  extension(IServersideQoLZDO @this)
  {
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

      @this.Processors = UnregisterCore(processors, @this.Processors ?? []);
      @this.HasNoProcessors = @this.Processors.Count is 0;
      @this.CyclicProcessors = UnregisterCore(processors, @this.CyclicProcessors ?? []);
      @this.HasNoCyclicProcessors = @this.CyclicProcessors.Count is 0;

      if (@this.ProcessorDataRevisions is { } dataRevisions)
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
    //    var extZdo = @this.GetExtension<IServersideQoLZDO>();
    //    var zdoProcessors = extZdo.Processors ?? [];
    //    var allProcessors = extZdo.PrefabInfo?.EnabledProcessors ?? [];
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
    //    @this.Ungregister(unregister);
    //}

    internal void UnregisterAllExcept(Processor keep)
    {
      static IReadOnlyList<Processor> UnregisterAllExceptCore(Processor keep, IReadOnlyList<Processor> zdoProcessors)
      {
        var hash = (0, keep.GetType()).GetHashCode();
        if (!__processors.TryGetValue(hash, out var processors))
          __processors.Add(hash, processors = [keep]);
        return processors;
      }

      @this.Processors = UnregisterAllExceptCore(keep, @this.Processors ?? []);
      @this.HasNoProcessors = @this.Processors.Count is 0;
      @this.CyclicProcessors = UnregisterAllExceptCore(keep, @this.CyclicProcessors ?? []);
      @this.HasNoCyclicProcessors = @this.CyclicProcessors.Count is 0;

      if (!@this.HasNoProcessors && @this.ProcessorDataRevisions is { } dataRevisions)
      {
        foreach (var processor in @this.Processors.Enumerate())
        {
          if (!ReferenceEquals(processor, keep))
            dataRevisions.Remove(processor);
        }
      }
    }

    internal void UnregisterAll()
    {
      @this.Processors = [];
      @this.HasNoProcessors = true;
    }

    internal void ReregisterAll()
    {
      @this.Processors = @this.PrefabInfo?.EnabledProcessors ?? [];
      @this.HasNoProcessors = @this.Processors.Count is 0;
      @this.CyclicProcessors = @this.PrefabInfo?.EnabledCyclicProcessors ?? [];
      @this.HasNoCyclicProcessors = @this.CyclicProcessors.Count is 0;
    }

    internal void UpdateProcessorDataRevision(Processor processor, bool onlyExisting = false)
    {
      if (@this.ProcessorDataRevisions is not { } dataRevisions)
      {
        if (onlyExisting)
          return;
        if (!__dataRevCache.TryPop(out dataRevisions))
          dataRevisions = [];
        @this.ProcessorDataRevisions = dataRevisions;
      }

      if (onlyExisting)
        dataRevisions.TryAdd(processor, (@this.ZDO.DataRevision, @this.ZDO.OwnerRevision));
      else
        dataRevisions[processor] = (@this.ZDO.DataRevision, @this.ZDO.OwnerRevision);
    }

    internal void ResetProcessorDataRevision(Processor processor)
        => @this.ProcessorDataRevisions?.Remove(processor);

    internal bool CheckProcessorDataRevisionChanged(Processor processor)
    {
      var dataRevisions = @this.ProcessorDataRevisions;
      if (dataRevisions is null || !dataRevisions.TryGetValue(processor, out var revision) || revision != (@this.ZDO.DataRevision, @this.ZDO.OwnerRevision))
        return true;
      return false;
    }
  }

  extension(ZDO @this)
  {
    public PrefabInfo? PrefabInfo
    {
      get => @this.GetExtension<IServersideQoLZDO>().PrefabInfo;
      internal set
      {
        var extZdo = @this.GetExtension<IServersideQoLZDO>();

        if (extZdo.ProcessorDataRevisions is { } dataRevisions)
        {
          dataRevisions.Clear();
          __dataRevCache.Push(dataRevisions);
        }

        if (extZdo.ComponentFieldAccessors is { } componentFieldAccessors)
        {
          componentFieldAccessors.Clear();
          __componentFieldAccessorCache.Push(componentFieldAccessors);
        }

        extZdo.PrefabInfo = value;
        extZdo.Processors = value?.EnabledProcessors ?? [];
        extZdo.HasNoProcessors = extZdo.Processors.Count is 0;
        extZdo.CyclicProcessors = value?.EnabledCyclicProcessors ?? [];
        extZdo.HasNoCyclicProcessors = extZdo.CyclicProcessors.Count is 0;
        extZdo.ProcessorDataRevisions = default;
        extZdo.HasFields = default;
        extZdo.ComponentFieldAccessors = default;
      }
    }

    //public ListExtensions.ListEnumerable<Processor> Processors => (@this.GetExtension<IServersideQoLZDO>().Processors ?? []).AsEnumerable();
    //public ListExtensions.ListEnumerable<Processor> CyclicProcessors => (@this.GetExtension<IServersideQoLZDO>().CyclicProcessors ?? []).AsEnumerable();

    public void Unregister(IReadOnlyList<Processor> processors)
        => @this.GetExtension<IServersideQoLZDO>().Unregister(processors);

    //public void Reregister(IReadOnlyList<Processor> processors)
    //{
    //    static IReadOnlyList<Processor> ReregisterCore(IReadOnlyList<Processor> processors, IReadOnlyList<Processor> zdoProcessors, IReadOnlyList<Processor> allProcessors)
    //    {

    //    }


    //    // does this implementation make sense?
    //    var extZdo = @this.GetExtension<IServersideQoLZDO>();
    //    var zdoProcessors = extZdo.Processors ?? [];
    //    var allProcessors = extZdo.PrefabInfo?.EnabledProcessors ?? [];
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
    //    @this.Ungregister(unregister);
    //}

    public void UnregisterAllExcept(Processor keep)
        => @this.GetExtension<IServersideQoLZDO>().UnregisterAllExcept(keep);

    public void UnregisterAll()
        => @this.GetExtension<IServersideQoLZDO>().UnregisterAll();

    public void ReregisterAll()
        => @this.GetExtension<IServersideQoLZDO>().ReregisterAll();

    //public void UpdateProcessorDataRevision(Processor processor)
    //    => @this.GetExtension<IServersideQoLZDO>().UpdateProcessorDataRevision(processor);

    //public void ResetProcessorDataRevision(Processor processor)
    //    => @this.GetExtension<IServersideQoLZDO>().ResetProcessorDataRevision(processor);

    //public bool CheckProcessorDataRevisionChanged(Processor processor)
    //    => @this.GetExtension<IServersideQoLZDO>().CheckProcessorDataRevisionChanged(processor);

    public void Destroy()
    {
      @this.ClaimOwnershipInternal();
      ZDOMan.instance.DestroyZDO(@this);
    }

    public ZDO CreateClone()
    {
      var prefab = @this.GetPrefab();
      var pos = @this.GetPosition();
      var owner = @this.GetOwner();
      __pkg.Clear();
      @this.Serialize(__pkg);
      __pkg.Size(); // force flush

      var zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
      __pkg.SetPos(0);
      zdo.Deserialize(__pkg);
      zdo.SetOwnerInternal(owner);
      return zdo;
    }

    public ZDO Recreate()
    {
      var zdo = @this.CreateClone();

      // Call before Destroy and thus before ZDOMan.instance.m_onZDODestroyed
      //_addData?.Recreated?.Invoke(this, zdo);

      @this.Destroy();
      return zdo;
    }

    public TimeSpan GetTimeSinceSpawned() => ZNet.instance.GetTime() - @this.Vars.GetSpawnTime();

    public void ClaimOwnership() => @this.SetOwner(ZDOMan.GetSessionID());
    public void ClaimOwnershipInternal() => @this.SetOwnerInternal(ZDOMan.GetSessionID());
    public void ReleaseOwnership() => @this.SetOwner(0);
    public void ReleaseOwnershipInternal() => @this.SetOwnerInternal(0);

    public bool IsOwnerOrUnassigned() => !@this.HasOwner() || @this.IsOwner();

    public void SetModAsCreator(Processor.CreatorMarkers marker = Processor.CreatorMarkers.None) => @this.Vars.SetCreator((long)ServersideQoLPlugin.PluginGuidHash | (long)((ulong)marker << 32));
    public bool IsModCreator(out Processor.CreatorMarkers marker)
    {
      marker = Processor.CreatorMarkers.None;
      if ((int)@this.Vars.GetCreator() != ServersideQoLPlugin.PluginGuidHash)
        return false;
      marker = (Processor.CreatorMarkers)((ulong)@this.Vars.GetCreator() >> 32);
      return true;
    }
    public bool IsModCreator() => @this.IsModCreator(out _);

    public bool IsAnyCloserThan(IReadOnlyList<Peer> peers, float distance)
    {
      distance *= distance;
      var pos = @this.GetPosition();
      foreach (var peer in peers.Enumerate())
      {
        if (Utils.DistanceSqr(peer.m_refPos, pos) < distance)
          return true;
      }
      return false;
    }

    [Conditional("DEBUG")]
    public void AssertIs<T>() where T : MonoBehaviour
        => System.Diagnostics.Debug.Assert(@this.PrefabInfo?.Prefab.GetComponentInChildren<T>() is not null);

    [Conditional("DEBUG")]
    public void AssertIsAll<T1, T2>() where T1 : MonoBehaviour where T2 : MonoBehaviour
        => System.Diagnostics.Debug.Assert(@this.PrefabInfo?.Prefab is { } prefab &&
            prefab.GetComponentInChildren<T1>() is not null &&
            prefab.GetComponentInChildren<T2>() is not null);

  }
}
