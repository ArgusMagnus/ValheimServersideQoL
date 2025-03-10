using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ContainerProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo is not { Container: not null, Piece: not null })
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && zdo.DataRevision == dataRevision)
            return;

        if (zdo.GetLong(ZDOVars.s_creator) is 0)
            return; // ignore non-player-built chests (such as TreasureChest_*)

        if (zdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, zdo))
            return; // in use or player to close

        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;

        var data = zdo.GetString(ZDOVars.s_items);
        if (string.IsNullOrEmpty(data))
            return;

        /// <see cref="Container.Load"/>
        /// <see cref="Container.Save"/>
        var width = prefabInfo.Container.m_width;
        var height = prefabInfo.Container.m_height;
        if (zdo.Fields<Container>().GetHasFields())
        {
            width = zdo.Fields<Container>().GetInt(x => x.m_width, width);
            height = zdo.Fields<Container>().GetInt(x => x.m_height, height);
        }
        InventoryEx inventory = new(new(prefabInfo.Container.m_name, prefabInfo.Container.m_bkg, width, height));
        inventory.Inventory.Load(new(data));
        var changed = false;
        var x = 0;
        var y = 0;
        foreach (var item in inventory.Inventory.GetAllItems()
            .OrderBy(x => x.IsEquipable() ? 0 : 1)
            .ThenBy(x => x.m_shared.m_name)
            .ThenByDescending(x => x.m_stack))
        {
            var dict = SharedState.ContainersByItemName.GetOrAdd(item.m_shared, static _ => new());
            dict[zdo.m_uid] = inventory;
            if (!Config.Containers.AutoSort.Value)
                continue;

            // todo: merge stacks

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

        if (changed)
        {
            var pkg = new ZPackage();
            inventory.Inventory.Save(pkg);
            data = pkg.GetBase64();
            zdo.Set(ZDOVars.s_items, data);
            SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
            Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece.m_name} sorted");
        }
        inventory.DataRevision = zdo.DataRevision;
    }
}