using BepInEx.Logging;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL.Processors;

sealed class ContainerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly Dictionary<ItemKey, int> _stackPerItem = new();

    public ConcurrentDictionary<SharedItemDataKey, ConcurrentHashSet<ExtendedZDO>> ContainersByItemName { get; } = new();

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Container is null || zdo.Vars.GetCreator() is 0)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var fields = zdo.Fields<Container>();
        var inventory = zdo.Inventory!;
        var width = inventory.Inventory.GetWidth();
        var height = inventory.Inventory.GetHeight();
        if (Config.Containers.ContainerSizes.TryGetValue(zdo.GetPrefab(), out var sizeCfg)
            && sizeCfg.Value.Split(['x'], 2) is { Length: 2 } parts
            && int.TryParse(parts[0], out var desiredWidth)
            && int.TryParse(parts[1], out var desiredHeight)
            && (width, height) != (desiredWidth, desiredHeight))
        {
            if (zdo.Inventory is { Items: { Count: 0 } })
            {
                fields.Set(x => x.m_width, width = desiredWidth);
                fields.Set(x => x.m_height, height = desiredHeight);
                RecreateZdo = true;
                return false;
            }
        }
        else
        {
            desiredWidth = width;
            desiredHeight = height;
        }

        if (!CheckMinDistance(peers, zdo))
            return false;

        if (zdo.Vars.GetInUse())
            return true; // in use or player to close

        if (inventory is { Items: { Count: 0 } })
            return true;

        if ((width, height) != (desiredWidth, desiredHeight))
        {
            RecreateZdo = true;
            if (inventory.Items.Count > desiredWidth * desiredHeight)
            {
                var found = false;
                for (var h = desiredHeight; !found && h <= height; h++)
                {
                    for (var w = desiredWidth; !found && w <= width; w++)
                    {
                        if (inventory.Items.Count <= w * h)
                        {
                            found = true;
                            (desiredWidth, desiredHeight) = (w, h);
                        }
                    }
                }

                if (!found || (width, height) == (desiredWidth, desiredHeight))
                    RecreateZdo = false;
            }
            if (RecreateZdo)
            {
                fields.Set(x => x.m_width, width = desiredWidth);
                fields.Set(x => x.m_height, height = desiredHeight);
            }
        }

        var changed = false;
        ItemDrop.ItemData? lastPartialSlot = null;
        _stackPerItem.Clear();
        foreach (var item in inventory.Items
            .OrderBy(x => x.IsEquipable() ? 0 : 1)
            .ThenBy(x => x.m_shared.m_name)
            .ThenByDescending(x => x.m_stack))
        {
            if (zdo.PrefabInfo.Container.Value.Container.m_privacy is not Container.PrivacySetting.Private)
            {
                var set = ContainersByItemName.GetOrAdd(item.m_shared, static _ => new());
                set.Add(zdo);
            }
            if (!Config.Containers.AutoSort.Value && !RecreateZdo)
                continue;

            if (lastPartialSlot is not null && new ItemKey(item) == lastPartialSlot)
            {
                var diff = Math.Min(item.m_stack, lastPartialSlot.m_shared.m_maxStackSize - lastPartialSlot.m_stack);
                lastPartialSlot.m_stack += diff;
                item.m_stack -= diff;
                changed = true;
            }

            if (item.m_stack is 0)
                continue;

            if (!_stackPerItem.TryGetValue(item, out var stackCount))
                stackCount = 0;
            _stackPerItem[item] = stackCount + 1;

            if (item.m_stack < item.m_shared.m_maxStackSize)
                lastPartialSlot = item;
        }

        if (changed)
        {
            for (int i = inventory.Items.Count - 1; i >= 0; i--)
            {
                if (inventory.Items[i].m_stack is 0)
                    inventory.Items.RemoveAt(i);
            }
        }

        if (_stackPerItem.Count > 0)
        {
            if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / width)) <= height)
            {
                var x = -1;
                var y = 0;
                ItemKey? lastKey = null;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (++x >= width || (lastKey.HasValue && lastKey != item))
                    {
                        x = 0;
                        y++;
                    }
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        item.m_gridPos.x = x;
                        item.m_gridPos.y = y;
                        changed = true;
                    }
                    lastKey = item;
                }
            }
            else if (_stackPerItem.Values.Sum(x => (int)Math.Ceiling((double)x / height)) <= width)
            {
                var x = 0;
                var y = height;
                ItemKey? lastKey = null;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (--y < 0 || (lastKey.HasValue && lastKey != item))
                    {
                        y = height - 1;
                        x++;
                    }
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        item.m_gridPos.x = x;
                        item.m_gridPos.y = y;
                        changed = true;
                    }
                    lastKey = item;
                }
            }
            else
            {
                var x = 0;
                var y = 0;
                foreach (var item in inventory.Items
                    .OrderBy(x => x.IsEquipable() ? 0 : 1)
                    .ThenBy(x => x.m_shared.m_name)
                    .ThenByDescending(x => x.m_stack))
                {
                    if (item.m_gridPos.x != x || item.m_gridPos.y != y)
                    {
                        item.m_gridPos.x = x;
                        item.m_gridPos.y = y;
                        changed = true;
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
            inventory.Save();
            RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Container.Value.Piece.m_name} sorted");
        }

        return true;
    }
}
