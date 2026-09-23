using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.AutoExtract;

[Processor("9d9305cb-0a0c-47b0-aa3d-edec9873a506")]
[RunAfter<ContainerRegistryProcessor>]
public sealed class ExtractProcessor : Processor<ExtractProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Beehive? Beehive, SapCollector? SapCollector) : ProcessorPrefabInfo;

  SectorDictionary<HashSet<ServersideQoLZDO>>? _stations;
  SectorDictionary<HashSet<ServersideQoLZDO>>? _containers;

  protected override void Initialize()
  {
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    _stations = new(Mathf.Max(Config.Instance.ExtractRange.Value, Config.Instance.AutoPickupMaxRange ?? 0));
    _containers = Instance<ContainerRegistryProcessor>().GetContainers(_stations.SectorWidth);
    Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (_stations is null || _containers is null)
      return ProcessResult.UnregisterProcessor;

    if (!(prefabInfo.Beehive is not null ? Config.Instance.ExtractBeehives.Value : Config.Instance.ExtractSapCollectors.Value))
      return ProcessResult.UnregisterProcessor;

    _stations.TryAdd(zdo);

    // GetLevel defaults to 1, so pass 0
    var level = zdo.Vars.GetLevel(0);
    if (level <= 0)
      return default;

    if (!CheckMinDistance(peers, zdo, Config.Instance.ExtractMinPlayerDistance.Value))
      return ScheduleReprocessing();

    var (stationName, itemDrop) = prefabInfo.Beehive is not null ?
        (prefabInfo.Beehive.m_name, prefabInfo.Beehive.m_honeyItem) :
        (prefabInfo.SapCollector!.m_name, prefabInfo.SapCollector.m_spawnItem);

    /// <see cref="Beehive.RPC_Extract"/> <see cref="SapCollector.RPC_Extract"/>
    var perUnit = Game.instance.ScaleDrops(itemDrop.m_itemData, 1);
    var itemKey = new ItemDataKey(itemDrop.m_itemData);
    var maxStackSize = itemDrop.m_itemData.m_shared.m_maxStackSize;
    var extracted = 0;
    var result = ProcessResult.Default;
    HashSet<Vector2i>? usedSlots = null;
    List<ServersideQoLZDO>? toRemove = null;

    // pass 0: containers which already hold the item, pass 1: all other containers
    for (var pass = 0; pass < 2 && extracted < level; pass++)
    {
      foreach (var containers in _containers.EnumerateAdjacent(zdo.ZDO.GetPosition()))
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

          var rangeSqr = containerState.AutoStorePickupRange ?? Config.Instance.ExtractRange.Value;
          rangeSqr *= rangeSqr;
          if (rangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > rangeSqr)
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

          if (hasItem != (pass is 0))
            continue;

          var units = Math.Min(level - extracted, capacity / perUnit);
          if (units is 0)
            continue;

          if (!containerZdo.IsOwnerOrUnassigned())
          {
            result |= ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
            continue;
          }

          var amount = units * perUnit;
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
            var slot = itemDrop.m_itemData.Clone();
            slot.m_stack = Math.Min(amount, maxStackSize);
            slot.m_dropPrefab = itemDrop.gameObject;
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

          extracted += units;
          zdo.ReleaseOwnership();
          zdo.Vars.SetLevel(level - extracted);

          if (extracted == level)
            break;
        }

        if (toRemove is not null)
        {
          foreach (var containerZdo in toRemove)
            containers.Remove(containerZdo);
        }

        if (extracted == level)
          break;
      }
    }

    if (extracted is not 0)
    {
      ShowMessage(peers, zdo,
          Config.Instance.Localization.Value.FormatExtracted(stationName, itemDrop.m_itemData.m_shared.m_name, extracted * perUnit),
          Config.Instance.ExtractedMessageType.Value);
    }

    return result;
  }

  void OnContainerChanged(ServersideQoLZDO containerZdo, ContainerState state)
  {
    if (_stations is null)
      throw new Exception("bug");

    var rangeSqr = state.AutoStorePickupRange ?? Config.Instance.ExtractRange.Value;
    rangeSqr *= rangeSqr;
    if (rangeSqr is 0f)
      return;

    foreach (var stations in _stations.EnumerateAdjacent(containerZdo.ZDO.GetPosition()))
    {
      foreach (var zdo in stations)
      {
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) <= rangeSqr)
          ScheduleReprocessing(zdo);
      }
    }
  }
}
