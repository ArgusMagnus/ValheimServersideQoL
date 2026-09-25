using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.AutoProcess;

[Processor("4d9ff5be-1a2e-469e-8046-addadfd58bab")]
[RunAfter<ContainerRegistryProcessor>]
public sealed class FermenterProcessor : Processor<FermenterProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Fermenter Fermenter) : ProcessorPrefabInfo;

  SectorDictionary<HashSet<ServersideQoLZDO>>? _fermenters;
  SectorDictionary<HashSet<ServersideQoLZDO>>? _containers;
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    if (Config.Instance.FeedFromContainers.Value && (Config.Instance.FeedFermenters.Value || Config.Instance.ExtractFermenters.Value))
    {
      _fermenters = new(Mathf.Max(Config.Instance.FeedFromContainersRange.Value, Config.Instance.FeedFromContainersMaxRange.Value));
      _containers = Instance<ContainerRegistryProcessor>().GetContainers(_fermenters.SectorWidth);
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(_fermenters.SectorWidth);
      Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
    }
    else
    {
      _fermenters = null;
      _containers = null;
      _containersByItemName = null;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (_fermenters is null || _containers is null || _containersByItemName is null)
      return ProcessResult.UnregisterProcessor;

    _fermenters.TryAdd(zdo);

    if (!CheckMinDistance(peers, zdo, Config.Instance.FeedFromContainersMinPlayerDistance.Value))
      return ScheduleReprocessing();

    var content = zdo.Vars.GetContent();
    if (content is 0)
      return Config.Instance.FeedFermenters.Value ? Feed(zdo, peers, prefabInfo) : default;

    return Config.Instance.ExtractFermenters.Value ? Extract(zdo, peers, prefabInfo, content) : default;
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

  /// <see cref="Fermenter.RPC_Tap"/> <see cref="Fermenter.DelayedTap"/>
  ProcessResult Extract(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo, int content)
  {
    var startTime = zdo.Vars.GetStartTime();
    if (startTime.Ticks is 0)
      return default;

    var remaining = zdo.Fields<Fermenter>().GetFloat(static () => x => x.m_fermentationDuration) - (float)(ZNet.instance.GetTime() - startTime).TotalSeconds;
    if (remaining >= 0f)
      return ScheduleReprocessing(remaining + 1f);

    if (prefabInfo.Fermenter.m_conversion.FirstOrDefault(x => x.m_from.gameObject.name.GetStableHashCode() == content) is not { } conversion)
      return default;

    var output = conversion.m_to.m_itemData;
    var itemKey = new ItemDataKey(output);
    var maxStackSize = output.m_shared.m_maxStackSize;
    var result = ProcessResult.Default;
    var extracted = false;
    HashSet<Vector2i>? usedSlots = null;
    List<ServersideQoLZDO>? toRemove = null;

    // pass 0: containers which already hold the item, pass 1: all other containers
    for (var pass = 0; pass < 2 && !extracted; pass++)
    {
      foreach (var containers in _containers!.EnumerateAdjacent(zdo.ZDO.GetPosition()))
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
          (usedSlots ??= []).Clear();
          var hasItem = false;
          var capacity = inventory.Inventory.GetEmptySlots() * maxStackSize;
          foreach (var slot in inventory.Items)
          {
            usedSlots.Add(slot.m_gridPos);
            if (new ItemDataKey(slot) != itemKey)
              continue;

            hasItem = true;
            capacity += Math.Max(0, slot.m_shared.m_maxStackSize - slot.m_stack);
          }

          if (hasItem != (pass is 0) || capacity < conversion.m_producedItems)
            continue;

          if (!containerZdo.IsOwnerOrUnassigned())
          {
            result |= ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
            continue;
          }

          var amount = conversion.m_producedItems;
          foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == itemKey))
          {
            var add = Math.Min(amount, slot.m_shared.m_maxStackSize - slot.m_stack);
            if (add <= 0)
              continue;

            slot.m_stack += add;
            amount -= add;
            if (amount is 0)
              break;
          }

          while (amount > 0)
          {
            var slot = output.Clone();
            slot.m_stack = Math.Min(amount, maxStackSize);
            slot.m_dropPrefab = conversion.m_to.gameObject;
            slot.m_gridPos.x = -1;
            for (int x = 0; x < inventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
            {
              for (int y = 0; y < inventory.Inventory.GetHeight(); y++)
              {
                if (usedSlots.Add(new(x, y)))
                {
                  (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                  break;
                }
              }
            }
            inventory.Items.Add(slot);
            amount -= slot.m_stack;
          }

          inventory.Save();

          zdo.ReleaseOwnership();
          zdo.Vars.SetContent(0);
          zdo.Vars.SetStartTime(default);

          ShowMessage(peers, zdo,
              Config.Instance.Localization.Value.FormatProductExtracted(prefabInfo.Fermenter.m_name, output.m_shared.m_name, conversion.m_producedItems),
              Config.Instance.FermenterExtractedMessageType.Value);

          extracted = true;
          break;
        }

        if (toRemove is not null)
        {
          foreach (var containerZdo in toRemove)
            containers.Remove(containerZdo);
        }

        if (extracted)
          break;
      }
    }

    return result;
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
