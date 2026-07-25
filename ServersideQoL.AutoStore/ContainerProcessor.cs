using UnityEngine;
using static Version;

namespace ServersideQoL.AutoStore;

[Processor("e1c6ea7a-996b-4aad-8595-af86f02fe25b")]
[RunBefore<ContainerRegistryProcessor>]
[DependsOn<PlayerRegistryProcessor>]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  readonly Dictionary<ItemDataKey, int> _stackPerItem = [];
  readonly Dictionary<ServersideQoLZDO, StackContainerState> _stackContainers = [];
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;
  SectorDictionary<HashSet<ServersideQoLZDO>>? _containers;

  protected override void Initialize()
  {
    Instance<PlayerRegistryProcessor>().EmoteDetected -= OnPlayerEmoteDetected;

    if (Config.Instance.StackInventoryIntoContainersEmote.Value is ConfigBase.DisabledEmote)
    {
      _containersByItemName = null;
      _containers = null;
    }
    else
    {
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(Mathf.Max(Config.Instance.AutoPickupRange.Value, Config.Instance.AutoPickupMaxRange ?? 0));
      _containers = Instance<ContainerRegistryProcessor>().GetContainers(_containersByItemName.SectorWidth);
      Instance<PlayerRegistryProcessor>().EmoteDetected += OnPlayerEmoteDetected;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
  {
    if (!Config.Instance.AutoSort.Value)
      return ProcessResult.UnregisterProcessor;

    var state = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo);

    if (_stackContainers.TryGetValue(zdo, out var stackContainerState))
      return ProcessStackContainer(zdo, peers, state, stackContainerState);

    var changed = false;
    ItemDrop.ItemData? lastPartialSlot = null;
    var inventory = state.GetInventory();
    _stackPerItem.Clear();
    foreach (var item in inventory.Items
        .OrderBy(static x => x.IsEquipable() ? 0 : 1)
        .ThenBy(static x => x.m_shared.m_name)
        .ThenByDescending(static x => x.m_stack))
    {
      if (lastPartialSlot is not null && new ItemDataKey(item) == lastPartialSlot)
      {
        changed = true;
        if (!zdo.IsOwnerOrUnassigned())
          break;
        else
        {
          var diff = Math.Min(item.m_stack, lastPartialSlot.m_shared.m_maxStackSize - lastPartialSlot.m_stack);
          lastPartialSlot.m_stack += diff;
          item.m_stack -= diff;
        }
      }

      if (item.m_stack is 0)
        continue;

      if (!_stackPerItem.TryGetValue(item, out var stackCount))
        stackCount = 0;
      _stackPerItem[item] = stackCount + 1;

      if (item.m_stack < item.m_shared.m_maxStackSize)
        lastPartialSlot = item;
    }

    if (changed && zdo.IsOwnerOrUnassigned())
    {
      for (int i = inventory.Items.Count - 1; i >= 0; i--)
      {
        if (inventory.Items[i].m_stack is 0)
          inventory.Items.RemoveAt(i);
      }
    }

    if (_stackPerItem.Count > 0)
    {
      var fields = zdo.Fields<Container>();
      var width = fields.GetInt(static () => x => x.m_width);
      var height = fields.GetInt(static () => x => x.m_height);

      if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / width)) <= height)
      {
        var x = -1;
        var y = 0;
        ItemDataKey? lastKey = null;
        foreach (var item in inventory.Items
            .OrderBy(static x => x.IsEquipable() ? 0 : 1)
            .ThenBy(static x => x.m_shared.m_name)
            .ThenByDescending(static x => x.m_stack))
        {
          if (++x >= width || (lastKey.HasValue && lastKey != item))
          {
            x = 0;
            y++;
          }
          if (item.m_gridPos.x != x || item.m_gridPos.y != y)
          {
            changed = true;
            if (zdo.IsOwnerOrUnassigned())
              item.m_gridPos = new(x, y);
          }
          lastKey = item;
        }
      }
      else if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / height)) <= width)
      {
        var x = 0;
        var y = height;
        ItemDataKey? lastKey = null;
        foreach (var item in inventory.Items
            .OrderBy(static x => x.IsEquipable() ? 0 : 1)
            .ThenBy(static x => x.m_shared.m_name)
            .ThenByDescending(static x => x.m_stack))
        {
          if (--y < 0 || (lastKey.HasValue && lastKey != item))
          {
            y = height - 1;
            x++;
          }
          if (item.m_gridPos.x != x || item.m_gridPos.y != y)
          {
            changed = true;
            if (zdo.IsOwnerOrUnassigned())
              item.m_gridPos = new(x, y);
          }
          lastKey = item;
        }
      }
      else
      {
        var x = 0;
        var y = 0;
        foreach (var item in inventory.Items
            .OrderBy(static x => x.IsEquipable() ? 0 : 1)
            .ThenBy(static x => x.m_shared.m_name)
            .ThenByDescending(static x => x.m_stack))
        {
          if (item.m_gridPos.x != x || item.m_gridPos.y != y)
          {
            changed = true;
            if (zdo.IsOwnerOrUnassigned())
              item.m_gridPos = new(x, y);
          }
          if (++x >= width)
          {
            x = 0;
            y++;
          }
        }
      }
    }

    if (changed)
    {
      if (!zdo.IsOwnerOrUnassigned())
      {
        Instance<ContainerRegistryProcessor>().RequestOwnership(zdo, zdo.Vars.GetCreator(), state);
      }
      else if (changed)
      {
        inventory.Save();
        ShowMessage(peers, zdo, Config.Instance.Localization.Value.FormatContainerSorted(prefabInfo.Container.m_name), Config.Instance.SortedMessageType.Value);
      }
    }

    return default;
  }

  void OnPlayerEmoteDetected(ServersideQoLZDO zdo, PlayerState state, Emotes emote)
  {
    if (Config.Instance.StackInventoryIntoContainersEmote.Value is not ConfigBase.AnyEmote && Config.Instance.StackInventoryIntoContainersEmote.Value != emote)
      return;

    List<ServersideQoLZDO>? toRemove = null;
    Dictionary<SharedItemDataKey, ItemDrop.ItemData>? items = null;

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

        var pickupRangeSqr = containerState.PickupRange ?? Config.Instance.AutoPickupRange.Value;
        pickupRangeSqr *= pickupRangeSqr;

        if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > pickupRangeSqr)
          continue;

        if (containerState.PrefabInfo.Container.m_privacy is Container.PrivacySetting.Private && containerZdo.Vars.GetCreator() != zdo.Vars.GetPlayerID())
          continue; // private container

        var containerInventory = containerState.GetInventory();
        foreach (var item in containerInventory.Items)
          (items ??= []).TryAdd(item.m_shared, item);
      }

      if (toRemove is not null)
      {
        foreach (var containerZdo in toRemove)
          containers.Remove(containerZdo);
      }
    }

    if (items is not null)
    {
      var container = PlacePiece(zdo.ZDO.GetPosition() with { y = -1000 }, Prefabs.WoodChest, 0);
      var h = Math.Max(4, items.Count);
      container.Fields<Container>()
          .Set(static () => x => x.m_width, 8)
          .Set(static () => x => x.m_height, h);
      int y = 0;
      var inventory = Instance<ContainerRegistryProcessor>().GetState(container)!.GetInventory();
      foreach (var item in items.Values)
      {
        var clone = item.Clone();
        clone.m_stack = 1;
        clone.m_gridPos = new(0, y++);
        inventory.Items.Add(clone);
      }
      inventory.Save();
      container.ZDO.SetOwnerInternal(zdo.ZDO.GetOwner());
      _stackContainers.Add(container, new(zdo));
      container.Destroyed += OnStackContainerDestroyed;
      RPC.StackResponse(container, true);
    }
  }

  void OnStackContainerDestroyed(ServersideQoLZDO zdo) => _stackContainers.Remove(zdo);

  ProcessResult ProcessStackContainer(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerState state, StackContainerState stackContainerState)
  {
    var inventory = state.GetInventory();
    if (inventory.Items.Count is 0)
      return ProcessResult.DestroyZDO;
    else if (stackContainerState.Stacked)
    {
      if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
        RPC.TakeAllResponse(zdo, true);
      else if (MoveItems(zdo, state, stackContainerState, peers))
      {
        zdo.Destroyed -= OnStackContainerDestroyed;
        _stackContainers.Remove(zdo);
        if (inventory.Items.Count is 0)
          return ProcessResult.DestroyZDO;

        _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
        zdo.Destroyed += OnStackContainerDestroyed;
        // stackContainerState.RemoveAfter = DateTimeOffset.UtcNow;
      }
      return default;
    }
    else if (inventory.Items.Any(static x => x is { m_gridPos.x: > 0 } or { m_stack: > 1 }))
    {
      for (int i = inventory.Items.Count - 1; i >= 0; i--)
      {
        var item = inventory.Items[i];
        if (item.m_gridPos.x is not 0)
          continue;
        if (--item.m_stack is 0)
          inventory.Items.RemoveAt(i);
      }
      inventory.Save();
      stackContainerState.Stacked = true;
      stackContainerState.RemoveAfter = DateTimeOffset.UtcNow.AddSeconds(Config.Instance.StackInventoryIntoContainersReturnDelay.Value);
      zdo.Destroyed -= OnStackContainerDestroyed;
      _stackContainers.Remove(zdo);
      _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
      zdo.Destroyed += OnStackContainerDestroyed;
    }
    else if (stackContainerState.RemoveAfter < DateTimeOffset.UtcNow)
    {
      return ProcessResult.DestroyZDO;
    }
    else
    {
      RPC.StackResponse(zdo, true);
    }
    return default;
  }

  bool MoveItems(ServersideQoLZDO zdo, ContainerState state, StackContainerState stackContainerState, IEnumerable<Peer> peers)
  {
    var changed = false;
    HashSet<Vector2i>? usedSlots = null;
    List<ServersideQoLZDO>? toRemove = null;
    var inventory = state.GetInventory();
    for (int i = inventory.Items.Count - 1; i >= 0; i--)
    {
      var item = inventory.Items[i];
      foreach (var containers in _containersByItemName!.EnumerateAdjacent((stackContainerState.PlayerZDO.ZDO.GetPosition(), item.m_shared)))
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

          var pickupRangeSqr = containerState.PickupRange ?? Config.Instance.AutoPickupRange.Value;
          pickupRangeSqr *= pickupRangeSqr;

          if (pickupRangeSqr is 0f || Utils.DistanceSqr(stackContainerState.PlayerZDO.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > pickupRangeSqr)
            continue;

          var stack = item.m_stack;
          usedSlots ??= [];
          usedSlots.Clear();

          var requestContainerOwn = false;

          var containerInventory = containerState.GetInventory();
          ItemDrop.ItemData? containerItem = null;
          foreach (var slot in containerInventory.Items)
          {
            usedSlots.Add(slot.m_gridPos);
            if (new ItemDataKey(item) != slot)
              continue;

            containerItem ??= slot;

            var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
            if (maxAmount <= 0)
              continue;

            if (!containerZdo.IsOwnerOrUnassigned())
            {
              requestContainerOwn = true;
              break;
            }

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

          for (var emptySlots = containerInventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
          {
            if (!containerZdo.IsOwnerOrUnassigned())
              requestContainerOwn = true;
            if (requestContainerOwn)
              break;

            var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

            var slot = containerItem.Clone();
            slot.m_stack = amount;
            slot.m_gridPos.x = -1;
            for (int x = 0; x < containerInventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
            {
              for (int y = 0; y < containerInventory.Inventory.GetHeight(); y++)
              {
                if (usedSlots.Add(new(x, y)))
                {
                  (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                  break;
                }
              }
            }
            containerInventory.Items.Add(slot);
            stack -= amount;
          }

          if (requestContainerOwn)
          {
            Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, stackContainerState.PlayerZDO.Vars.GetPlayerID());
            continue;
          }

          if (stack != item.m_stack)
          {
            containerInventory.Save();
            (item.m_stack, stack) = (stack, item.m_stack);
            changed = true;
            //ShowMessage(peers, containerZdo,
            //    Config.Instance.Localization.Value.FormatAutoPickup(containerState.PrefabInfo.Container.m_name, item.m_shared.m_name, stack),
            //    Config.Instance.PickedUpMessageType.Value);
          }

          if (item.m_stack is 0)
          {
            inventory.Items.RemoveAt(i);
            break;
          }
        }

        if (toRemove is not null)
        {
          foreach (var containerZdo in toRemove)
            containers.Remove(containerZdo);
        }

        if (item.m_stack is 0)
          break;
      }
    }

    if (changed)
      inventory.Save();
    return changed;
  }

  sealed record StackContainerState(ServersideQoLZDO PlayerZDO)
  {
    public DateTimeOffset RemoveAfter { get; set; } = DateTimeOffset.UtcNow.AddSeconds(20);
    public bool Stacked { get; set; }
  }
}
