using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.AutoStore;

[Processor(Id)]
[RunBefore<ContainerRegistryProcessor>]
[DependsOn<PlayerRegistryProcessor>]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  public const string Id = "e1c6ea7a-996b-4aad-8595-af86f02fe25b";
  readonly Dictionary<ItemDataKey, int> _stackPerItem = [];
  readonly Dictionary<ServersideQoLZDO, StackContainerState> _stackContainers = [];
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;
  SectorDictionary<HashSet<ServersideQoLZDO>>? _containers;
  readonly HashSet<ItemDrop.ItemData.ItemType> _excludedTypes = [];
  int _effectPrefab;

  internal void SpawnModifiedEffect(ServersideQoLZDO container)
  {
    container.AssertIs<Container>();
    if (_effectPrefab is 0)
      return;
    var zdo = Spawn(_effectPrefab, container.ZDO.GetPosition(), container.ZDO.GetRotation());
    if (Config.Instance.SuppressContainerModifiedEffectSound.Value && GetPrefabInfo(zdo).HasComponent<ZSFX>())
      zdo.Fields<ZSFX>().Set(static () => x => x.m_playOnAwake, false);
  }

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
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(Mathf.Max(Config.Instance.AutoPickupRange.Value, Config.Instance.AutoPickupMaxRange.Value));
      _containers = Instance<ContainerRegistryProcessor>().GetContainers(_containersByItemName.SectorWidth);
      Instance<PlayerRegistryProcessor>().EmoteDetected += OnPlayerEmoteDetected;
    }

    Config.Instance.StackInventoryIntoContainersExcludeItemTypes.SettingChanged -= UpdateExcludedTypes;
    UpdateExcludedTypes(null, null);
    Config.Instance.StackInventoryIntoContainersExcludeItemTypes.SettingChanged += UpdateExcludedTypes;

    void UpdateExcludedTypes(object? sender, EventArgs? args)
    {
      _excludedTypes.Clear();
      foreach (var type in Config.Instance.StackInventoryIntoContainersExcludeItemTypes.Value.Items)
        _excludedTypes.Add(type);
    }

    Config.Instance.Advanced.ValueChanged -= UpdateEffectPrefab;
    UpdateEffectPrefab(Config.Instance.Advanced);
    Config.Instance.Advanced.ValueChanged += UpdateEffectPrefab;

    void UpdateEffectPrefab(ConfigBase.YamlConfigEntry<Config.AdvancedConfig> sender)
    {
      _effectPrefab = 0;
      var prefabName = sender.Value.ContainerModifiedEffectPrefabName.Trim();
      if (string.IsNullOrEmpty(prefabName))
        return;
      var hash = prefabName.GetStableHashCode();
      if (ZNetScene.instance.GetPrefab(hash)?.GetComponent<TimedDestruction>() is null)
        Logger.LogWarning($"Prefab '{prefabName}' does not have the {nameof(TimedDestruction)} component and is not suitable as effect");
      else
      _effectPrefab = hash;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
  {
    if (_stackContainers.TryGetValue(zdo, out var stackContainerState))
      return ProcessStackContainer(zdo, peers, Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container), stackContainerState);

    if (!Config.Instance.AutoSort.Value)
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetInUse())
      return default;

    var state = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);

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
        return ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(state, zdo.Vars.GetCreator()));

      inventory.Save();
      ShowMessage(peers, zdo, Config.Instance.Localization.Value.FormatContainerSorted(prefabInfo.Container.m_name), Config.Instance.SortedMessageType.Value);
    }

    return default;
  }

  void OnPlayerEmoteDetected(PlayerState state, Emotes emote)
  {
    if (Config.Instance.StackInventoryIntoContainersEmote.Value is not ConfigBase.AnyEmote && Config.Instance.StackInventoryIntoContainersEmote.Value != emote)
      return;

    List<ServersideQoLZDO>? toRemove = null;
    Dictionary<SharedItemDataKey, ItemDrop.ItemData>? items = null;

    foreach (var containers in _containers!.EnumerateAdjacent(state.ZDO.ZDO.GetPosition()))
    {
      toRemove?.Clear();
      foreach (var containerZdo in containers)
      {
        if (Instance<ContainerRegistryProcessor>().GetState(containerZdo) is not { } containerState)
        {
          (toRemove ??= []).Add(containerZdo);
          continue;
        }

        var pickupRangeSqr = containerState.AutoStorePickupRange ?? Config.Instance.AutoPickupRange.Value;
        pickupRangeSqr *= pickupRangeSqr;

        if (pickupRangeSqr is 0f || Utils.DistanceSqr(state.ZDO.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > pickupRangeSqr)
          continue;

        if (containerState.Container.m_privacy is Container.PrivacySetting.Private && containerZdo.Vars.GetCreator() != state.ZDO.Vars.GetPlayerID())
          continue; // private container

        var containerInventory = containerState.GetInventory();
        foreach (var item in containerInventory.Items)
        {
          if (!_excludedTypes.Contains(item.m_shared.m_itemType))
            (items ??= []).TryAdd(item.m_shared, item);
        }
      }

      if (toRemove is not null)
      {
        foreach (var containerZdo in toRemove)
          containers.Remove(containerZdo);
      }
    }

    if (items is not null)
    {
      var container = PlacePiece(state.ZDO.ZDO.GetPosition() with { y = -1000 }, Prefabs.PrivateChest, 0);
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
      container.ZDO.SetOwnerInternal(state.ZDO.ZDO.GetOwner());
      _stackContainers.Add(container, new(state.ZDO));
      container.Destroyed += OnStackContainerDestroyed;
      container.RPC.Container.StackResponse(true);
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
      if (stackContainerState.RemoveAfter < Timestamp.Now)
        zdo.RPC.Container.TakeAllResponse(true);
      else if (MoveItems(zdo, peers, state, stackContainerState))
      {
        zdo.Destroyed -= OnStackContainerDestroyed;
        _stackContainers.Remove(zdo);
        if (inventory.Items.Count is 0)
          return ProcessResult.DestroyZDO;

        _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
        zdo.Destroyed += OnStackContainerDestroyed;
        stackContainerState.RemoveAfter = Timestamp.Now.AddSeconds(0.5f);
      }
      return ScheduleReprocessing(Config.Instance.Advanced.Value.ProcessingDelays.StackContainerWhenMovingItems);
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
      stackContainerState.RemoveAfter = Timestamp.Now.AddSeconds(Config.Instance.StackInventoryIntoContainersReturnDelay.Value);
      zdo.Destroyed -= OnStackContainerDestroyed;
      _stackContainers.Remove(zdo);
      _stackContainers.Add(zdo = RecreatePiece(zdo), stackContainerState);
      zdo.Destroyed += OnStackContainerDestroyed;
    }
    else if (stackContainerState.RemoveAfter < Timestamp.Now)
    {
      return ProcessResult.DestroyZDO;
    }
    else
    {
      zdo.RPC.Container.StackResponse(true);
    }
    return default;
  }

  bool MoveItems(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerState state, StackContainerState stackContainerState)
  {
    HashSet<Vector2i>? usedSlots = null;
    List<ServersideQoLZDO>? toRemove = null;
    HashSet<ServersideQoLZDO>? modified = null;
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

          var pickupRangeSqr = containerState.AutoStorePickupRange ?? Config.Instance.AutoPickupRange.Value;
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
            Instance<ContainerRegistryProcessor>().RequestOwnership(containerState, stackContainerState.PlayerZDO.Vars.GetPlayerID());
            continue;
          }

          if (stack != item.m_stack)
          {
            containerInventory.Save();
            (item.m_stack, stack) = (stack, item.m_stack);
            (modified ??= []).Add(containerZdo);
            ShowMessage(peers, containerZdo,
                Config.Instance.Localization.Value.FormatStacked(containerState.Container.m_name, item.m_shared.m_name, stack),
                //Config.Instance.StackInventoryIntoContainersMessageType.Value);
                Config.Instance.PickedUpMessageType.Value);
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

    if (modified is not null)
    {
      inventory.Save();
      if (_effectPrefab is not 0)
      {
        foreach (var container in modified)
          SpawnModifiedEffect(container);
      }
      return true;
    }
    return false;
  }

  sealed record StackContainerState(ServersideQoLZDO PlayerZDO)
  {
    public Timestamp RemoveAfter { get; set; } = Timestamp.Now.AddSeconds(4);
    public bool Stacked { get; set; }
  }
}
