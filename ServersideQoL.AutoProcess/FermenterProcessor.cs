using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.AutoProcess;

[Processor("4d9ff5be-1a2e-469e-8046-addadfd58bab")]
[RunAfter<ContainerRegistryProcessor>]
public sealed class FermenterProcessor : Processor<FermenterProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Fermenter Fermenter) : ProcessorPrefabInfo;

  const float TapRetryDelay = 10f;

  SectorDictionary<HashSet<ServersideQoLZDO>>? _fermenters;
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    if (Config.Instance.FeedFromContainers.Value && Config.Instance.FeedFermenters.Value)
    {
      _fermenters = new(Mathf.Max(Config.Instance.FeedFromContainersRange.Value, Config.Instance.FeedFromContainersMaxRange.Value));
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(_fermenters.SectorWidth);
      Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
    }
    else
    {
      _fermenters = null;
      _containersByItemName = null;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (_fermenters is null && !Config.Instance.TapFermenters.Value)
      return ProcessResult.UnregisterProcessor;

    _fermenters?.TryAdd(zdo);

    if (!CheckMinDistance(peers, zdo, Config.Instance.FeedFromContainersMinPlayerDistance.Value))
      return ScheduleReprocessing();

    if (zdo.Vars.GetContent() is 0)
      return _fermenters is not null ? Feed(zdo, peers, prefabInfo) : default;

    return Config.Instance.TapFermenters.Value ? Tap(zdo) : default;
  }

  /// <see cref="Fermenter.RPC_AddItem"/>
  ProcessResult Feed(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.Default;
    var added = false;
    List<ServersideQoLZDO>? toRemove = null;

    foreach (var conversion in prefabInfo.Fermenter.m_conversion)
    {
      var baseItem = conversion.m_from.m_itemData;
      foreach (var containers in _containersByItemName!.EnumerateAdjacent((zdo.ZDO.GetPosition(), baseItem.m_shared)))
      {
        toRemove?.Clear();
        foreach (var containerZdo in containers)
        {
          if (Instance<ContainerRegistryProcessor>().GetState(containerZdo) is not { } containerState)
          {
            (toRemove ??= []).Add(containerZdo);
            continue;
          }

          if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
            continue; // in use or player to close

          var feedRangeSqr = containerState.AutoProcessFeedRange ?? Config.Instance.FeedFromContainersRange.Value;
          feedRangeSqr *= feedRangeSqr;
          if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > feedRangeSqr)
            continue;

          var inventory = containerState.GetInventory();
          var leave = Config.Instance.FeedFromContainersLeaveAtLeastFermentable.Value;
          ItemDrop.ItemData? take = null;
          var found = false;
          foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == baseItem).OrderBy(static x => x.m_stack))
          {
            found = found || slot is { m_stack: > 0 };
            var reserved = Math.Min(slot.m_stack, leave);
            leave -= reserved;
            if (slot.m_stack > reserved)
            {
              take = slot;
              break;
            }
          }

          if (take is null)
          {
            if (!found)
              (toRemove ??= []).Add(containerZdo);
            continue;
          }

          if (!containerZdo.IsOwnerOrUnassigned())
          {
            result |= ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
            continue;
          }

          take.m_stack--;
          if (take.m_stack is 0)
          {
            inventory.Items.Remove(take);

            if (inventory.Items is { Count: 0 })
              (toRemove ??= []).Add(containerZdo);
          }

          zdo.ReleaseOwnership();
          zdo.Vars.SetContent(conversion.m_from.gameObject.name.GetStableHashCode());
          zdo.Vars.SetStartTime(ZNet.instance.GetTime());
          inventory.Save();

          ShowMessage(peers, zdo,
              Config.Instance.Localization.Value.FormatOreAdded(prefabInfo.Fermenter.m_name, baseItem.m_shared.m_name, 1),
              Config.Instance.OreOrFuelAddedMessageType.Value);

          added = true;
          break;
        }

        if (toRemove is not null)
        {
          foreach (var containerZdo in toRemove)
            containers.Remove(containerZdo);
        }

        if (added)
          break;
      }

      if (added)
        break;
    }

    return result;
  }

  /// <see cref="Fermenter.RPC_Tap"/>
  ProcessResult Tap(ServersideQoLZDO zdo)
  {
    var startTime = zdo.Vars.GetStartTime();
    if (startTime.Ticks is 0)
      return default;

    var remaining = zdo.Fields<Fermenter>().GetFloat(static () => x => x.m_fermentationDuration) - (float)(ZNet.instance.GetTime() - startTime).TotalSeconds;
    if (remaining >= 0f)
      return ScheduleReprocessing(remaining + 1f);

    // Only the owning client can tap. It drops the products at the tap like tapping by hand,
    // clears the content and thereby triggers reprocessing. Retry in case the client refuses (e.g. exposed fermenter)
    if (!zdo.IsOwnerOrUnassigned())
      zdo.RPC.Fermenter.Tap();

    return ScheduleReprocessing(TapRetryDelay);
  }

  void OnContainerChanged(ServersideQoLZDO containerZdo, ContainerState state)
  {
    if (_fermenters is null)
      throw new Exception("bug");

    var feedRangeSqr = state.AutoProcessFeedRange ?? Config.Instance.FeedFromContainersRange.Value;
    feedRangeSqr *= feedRangeSqr;
    if (feedRangeSqr is 0f)
      return;

    foreach (var fermenters in _fermenters.EnumerateAdjacent(containerZdo.ZDO.GetPosition()))
    {
      foreach (var zdo in fermenters)
      {
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) <= feedRangeSqr)
          ScheduleReprocessing(zdo);
      }
    }
  }
}
