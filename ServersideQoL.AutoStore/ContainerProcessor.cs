namespace ServersideQoL.AutoStore;

[Processor("e1c6ea7a-996b-4aad-8595-af86f02fe25b")]
[RunBefore<ContainerRegistryProcessor>]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  readonly Dictionary<ItemDataKey, int> _stackPerItem = [];

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
  {
    if (!Config.Instance.AutoSort.Value)
      return ProcessResult.UnregisterProcessor;

    var state = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo);

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
}
