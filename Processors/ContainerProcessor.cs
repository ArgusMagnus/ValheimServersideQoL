using BepInEx.Logging;
using System.Drawing;

namespace Valheim.ServersideQoL.Processors;

sealed class ContainerProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo is not { Container: not null, Piece: not null })
            return false;

        if (zdo.GetLong(ZDOVars.s_creator) is 0)
            return false; // ignore non-player-built chests (such as TreasureChest_*)

        if (!CheckMinDistance(peers, zdo))
            return false;

        if (zdo.GetBool(ZDOVars.s_inUse))
            return true; // in use or player to close

        var data = zdo.GetString(ZDOVars.s_items);
        if (string.IsNullOrEmpty(data))
            return true;

        /// <see cref="Container.Load"/>
        /// <see cref="Container.Save"/>
        var fields = zdo.Fields<Container>();
        var width = fields.GetInt(x => x.m_width);
        var height = fields.GetInt(x => x.m_height);
        InventoryEx inventory = new(new(zdo.PrefabInfo.Container.m_name, zdo.PrefabInfo.Container.m_bkg, width, height)) { DataRevision = zdo.DataRevision };
        inventory.Inventory.Load(new(data));
        var changed = false;
        var x = 0;
        var y = 0;

        ItemDrop.ItemData? lastPartialSlot = null;
        var items = inventory.Inventory.GetAllItems();
        foreach (var item in items
            .OrderBy(x => x.IsEquipable() ? 0 : 1)
            .ThenBy(x => x.m_shared.m_name)
            .ThenByDescending(x => x.m_stack))
        {
            var dict = SharedProcessorState.ContainersByItemName.GetOrAdd(item.m_shared, static _ => new());
            dict[zdo.m_uid] = inventory;
            if (!Config.Containers.AutoSort.Value)
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

        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (items[i].m_stack is 0)
                items.RemoveAt(i);
        }

        inventory.Save(zdo);
        Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Piece.m_name} sorted");
        return true;
    }
}
