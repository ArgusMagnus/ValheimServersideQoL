using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ContainerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo is not { Container: not null, Piece: not null, PieceTable: not null })
            return false;

        if (zdo.GetLong(ZDOVars.s_creator) is 0)
            return false; // Not sure if necessary. Are there non-player built pieces which are part of a PieceTable?

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
                recreate = true;
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

        if (zdo.GetBool(ZDOVars.s_inUse))
            return true; // in use or player to close

        if (inventory is { Items: { Count: 0 } })
            return true;

        if ((width, height) != (desiredWidth, desiredHeight))
        {
            recreate = true;
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
                    recreate = false;
            }
            if (recreate)
            {
                fields.Set(x => x.m_width, width = desiredWidth);
                fields.Set(x => x.m_height, height = desiredHeight);
            }
        }

        var changed = false;
        var x = 0;
        var y = 0;
        ItemDrop.ItemData? lastPartialSlot = null;
        foreach (var item in inventory.Items
            .OrderBy(x => x.IsEquipable() ? 0 : 1)
            .ThenBy(x => x.m_shared.m_name)
            .ThenByDescending(x => x.m_stack))
        {
            var set = SharedProcessorState.ContainersByItemName.GetOrAdd(item.m_shared, static _ => new());
            set.Add(zdo);
            if (!Config.Containers.AutoSort.Value && !recreate)
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
            if (item.m_stack < item.m_shared.m_maxStackSize)
                lastPartialSlot = item;

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

        if (!changed)
            return true;

        for (int i = inventory.Items.Count - 1; i >= 0; i--)
        {
            if (inventory.Items[i].m_stack is 0)
                inventory.Items.RemoveAt(i);
        }

        inventory.Save();
        RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Piece!.m_name} sorted");
        return true;
    }
}
