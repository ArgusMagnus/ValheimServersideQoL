using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ItemDropProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    public override void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.ItemDrop is null || !Config.Containers.AutoPickup.Value)
            return;

        if (prefabInfo.Piece is not null && zdo.GetBool(ZDOVars.s_piece))
            return; // ignore placed items (such as feasts)

        if (!CheckMinDistance(peers, zdo, Config.Containers.AutoPickupMinPlayerDistance.Value))
            return; // player to close

        var shared = ZNetScene.instance.GetPrefab(zdo.GetPrefab()).GetComponent<ItemDrop>().m_itemData.m_shared;
        if (!SharedState.ContainersByItemName.TryGetValue(shared, out var dict))
            return;

        HashSet<Vector2i>? usedSlots = null;
        ItemDrop.ItemData? item = null;

        foreach (var (containerZdoId, inventory) in dict.Select(x => (x.Key, x.Value)))
        {
            if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
            {
                dict.TryRemove(containerZdoId, out _);
                continue;
            }

            if (!SharedState.DataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                continue;

            if (Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > Config.Containers.AutoPickupRange.Value * Config.Containers.AutoPickupRange.Value)
                continue;

            if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                continue; // in use or player to close

            inventory.Update(containerZdo);

            if (item is null)
            {
                item = new() { m_shared = shared };
                PrivateAccessor.LoadFromZDO(item, zdo);
            }

            var stack = item.m_stack;
            usedSlots ??= new();
            usedSlots.Clear();

            ItemDrop.ItemData? containerItem = null;
            foreach (var slot in inventory.GetAllItems())
            {
                usedSlots.Add(slot.m_gridPos);
                if (new ItemKey(item) != slot)
                    continue;

                containerItem ??= slot;

                var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                if (maxAmount <= 0)
                    continue;

                var amount = Math.Min(stack, maxAmount);
                slot.m_stack += amount;
                stack -= amount;
                if (stack is 0)
                    break;
            }

            if (containerItem is null)
            {
                dict.TryRemove(containerZdoId, out _);
                if (dict is { Count: 0 })
                    SharedState.ContainersByItemName.TryRemove(item.m_shared, out _);
                continue;
            }

            if (!ReferenceEquals(inventory.GetAllItems(), inventory.GetAllItems()))
                throw new Exception("Algorithm assumption violated");

            for (var emptySlots = inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
            {
                var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

                var slot = containerItem.Clone();
                slot.m_stack = amount;
                slot.m_gridPos.x = -1;
                for (int x = 0; x < inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                {
                    for (int y = 0; y < inventory.GetHeight(); y++)
                    {
                        if (usedSlots.Add(new(x, y)))
                        {
                            (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                            break;
                        }
                    }
                }
                inventory.GetAllItems().Add(slot);
                stack -= amount;
            }

            if (stack != item.m_stack)
            {
                var pkg = new ZPackage();
                inventory.Save(pkg);
                containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                SharedState.DataRevisions[containerZdo.m_uid] = containerZdo.DataRevision;
                (item.m_stack, stack) = (stack, item.m_stack);
                zdo.SetOwner(ZDOMan.GetSessionID());
                ItemDrop.SaveToZDO(item, zdo);
                Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{SharedState.PrefabInfo[containerZdo.GetPrefab()].Piece!.m_name}: $msg_added {item.m_shared.m_name} {stack}x");
            }

            if (item.m_stack is 0)
                break;
        }

        if (item?.m_stack is 0)
        {
            zdo.SetOwner(ZDOMan.GetSessionID());
            ZDOMan.instance.DestroyZDO(zdo);
        }
    }
}
