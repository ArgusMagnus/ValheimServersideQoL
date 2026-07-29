using UnityEngine;

namespace ServersideQoL.AutoStore;

[Processor("5f86a765-e449-4047-afc8-a63e4d681a48")]
[RunAfter<ContainerRegistryProcessor>]
[RunAfter<TameableRegistryProcessor>]
public sealed class ItemDropProcessor : Processor<ItemDropProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(ItemDrop ItemDrop, Piece? Piece, EggGrow? EggGrow, ZSyncTransform? ZSyncTransform) : ProcessorPrefabInfo;

  readonly Dictionary<ServersideQoLZDO, DateTimeOffset> _eggDropTime = [];
  SectorDictionary<HashSet<ServersideQoLZDO>>? _itemDrops;
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    if (Config.Instance.AutoPickup.Value)
    {
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(Mathf.Max(Config.Instance.AutoPickupRange.Value, Config.Instance.AutoPickupMaxRange ?? 0));
      _itemDrops = new(_containersByItemName.SectorWidth);
      Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
    }
    else
    {
      _containersByItemName = null;
      _itemDrops = null;
    }

    _eggDropTime.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_containersByItemName is null || _itemDrops is null)
      return ProcessResult.UnregisterProcessor;
    if (prefabInfo.Piece is not null && zdo.Vars.GetPiece())
      return ProcessResult.UnregisterProcessor; // ignore placed items (such as feasts)

    if (prefabInfo.EggGrow is not null)
    {
      if (zdo.Vars.GetGrowStart() > 0)
        return default;

      var delay = 2 * prefabInfo.EggGrow.m_updateInterval + 2;
      if (!_eggDropTime.TryGetValue(zdo, out var dropTime))
      {
        _eggDropTime.Add(zdo, DateTimeOffset.UtcNow);
        zdo.Destroyed += x => _eggDropTime.Remove(x);
        zdo.DelaySchedulingFor(delay);
        return ProcessResult.ScheduleReprocessing;
      }
      delay -= (float)(DateTimeOffset.UtcNow - dropTime).TotalSeconds;
      if (delay > 0)
      {
        zdo.DelaySchedulingFor(delay);
        return ProcessResult.ScheduleReprocessing;
      }
    }

    if (!CheckMinDistance(peers, zdo, Config.Instance.AutoPickupMinPlayerDistance.Value))
      return ProcessResult.ScheduleReprocessing; // player to close

    var shared = prefabInfo.ItemDrop.m_itemData.m_shared;
    var requestOwn = false;
    var excludeFodderCheckComplete = !Config.Instance.AutoPickupExcludeFodder.Value;
    HashSet<Vector2i>? usedSlots = null;
    List<ServersideQoLZDO>? toRemove = null;
    ItemDrop.ItemData? item = null;

    foreach (var containers in _containersByItemName.EnumerateAdjacent((zdo.ZDO.GetPosition(), shared)))
    {
      if (containers.Count > 0 && !excludeFodderCheckComplete)
      {
        excludeFodderCheckComplete = true;
        foreach (var tameables in Instance<TameableRegistryProcessor>().Tameables.EnumerateAdjacent(zdo.ZDO.GetPosition()))
        {
          foreach (var tameableZdo in tameables)
          {
            if (Instance<TameableRegistryProcessor>().GetState(tameableZdo) is not { } tameState)
              continue;

            /// <see cref="MonsterAI.CanConsume(ItemDrop.ItemData)"/>
            if (!tameState.PrefabInfo.MonsterAI.m_consumeItems.Any(x => x.m_itemData.m_shared.m_name == shared.m_name))
              continue;
            var rangeSqr = tameState.PrefabInfo.MonsterAI.m_consumeSearchRange;
            rangeSqr *= rangeSqr;
            if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), tameableZdo.ZDO.GetPosition()) < rangeSqr)
            {
              if (prefabInfo.ZSyncTransform is not null && zdo.GetTimeSinceSpawned() < TimeSpan.FromSeconds(10))
                return ProcessResult.ScheduleReprocessing;

              ProcessResult result = ProcessResult.UnregisterProcessor;
              var fields = zdo.Fields<ItemDrop>();
              if (fields.UpdateValue(static () => x => x.m_autoPickup, false))
                result = ProcessResult.RecreateZDO;
              if (fields.UpdateValue(static () => x => x.m_autoDestroy, false))
                result = ProcessResult.RecreateZDO;
              return result;
            }
          }
        }
      }

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

        var pickupRangeSqr = containerState.PickupRange ?? Config.Instance.AutoPickupRange.Value;
        pickupRangeSqr *= pickupRangeSqr;

        if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > pickupRangeSqr)
          continue;

        if (item is null)
        {
          item = new() { m_shared = shared };
          ItemDrop.LoadFromZDO(item, zdo.ZDO);
        }

        var stack = item.m_stack;
        (usedSlots ??= []).Clear();

        var requestContainerOwn = false;

        ItemDrop.ItemData? containerItem = null;
        var inventory = containerState.GetInventory();
        foreach (var slot in inventory.Items)
        {
          usedSlots.Add(slot.m_gridPos);
          if (new ItemDataKey(item) != slot)
            continue;

          containerItem ??= slot;

          var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
          if (maxAmount <= 0)
            continue;

          if (Config.Instance.AutoPickupRequestOwnership.Value && !zdo.IsOwnerOrUnassigned())
            requestOwn = true;
          if (!containerZdo.IsOwnerOrUnassigned())
            requestContainerOwn = true;
          if (requestOwn || requestContainerOwn)
            break;

          var amount = Math.Min(stack, maxAmount);
          slot.m_stack += amount;
          stack -= amount;
          if (stack is 0)
            break;
        }

        if (containerItem is null)
        {
          (toRemove ??= []).Add(containerZdo);
          continue;
        }

        for (var emptySlots = inventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
        {
          if (Config.Instance.AutoPickupRequestOwnership.Value && !zdo.IsOwnerOrUnassigned())
            requestOwn = true;
          if (!containerZdo.IsOwnerOrUnassigned())
            requestContainerOwn = true;
          if (requestOwn || requestContainerOwn)
            break;

          var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

          var slot = containerItem.Clone();
          slot.m_stack = amount;
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
          stack -= amount;
        }

        if (requestOwn || requestContainerOwn)
        {
          if (requestContainerOwn)
            Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, 0);
          continue;
        }

        if (stack != item.m_stack)
        {
          inventory.Save();
          (item.m_stack, stack) = (stack, item.m_stack);
          ItemDrop.SaveToZDO(item, zdo.ZDO);
          ShowMessage(peers, containerZdo,
              Config.Instance.Localization.Value.FormatAutoPickup(containerState.PrefabInfo.Container.m_name, item.m_shared.m_name, stack),
              Config.Instance.PickedUpMessageType.Value);
        }

        if (item.m_stack is 0)
          break;
      }

      if (toRemove is not null)
      {
        foreach (var containerZdo in toRemove)
          containers.Remove(containerZdo);
      }

      if (item?.m_stack is 0)
        return ProcessResult.DestroyZDO;

      if (requestOwn)
      {
        RPC.RequestOwn(zdo);
        return ProcessResult.ScheduleReprocessing | ProcessResult.SkipOtherProcessors;
      }
    }

    _itemDrops.TryAdd(zdo);
    return default;
  }

  void OnContainerChanged(ServersideQoLZDO containerZdo, ContainerState containerState)
  {
    if (_itemDrops is null)
    {
      Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
      return;
    }

    if (containerState.GetInventory().Items.Count is 0)
      return;

    var rangeSqr = containerState.PickupRange ?? Config.Instance.AutoPickupRange.Value;
    rangeSqr *= rangeSqr;
    if (rangeSqr is 0f)
      return;

    foreach (var itemDrops in _itemDrops.EnumerateAdjacent(containerZdo.ZDO.GetPosition()))
    {
      foreach (var zdo in itemDrops)
      {
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) <= rangeSqr)
          ScheduleReprocessing(zdo);
      }
    }
  }
}
