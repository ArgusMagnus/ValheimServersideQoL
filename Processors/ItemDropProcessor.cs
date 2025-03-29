using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ItemDropProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
	{
        if (zdo.PrefabInfo.ItemDrop is null || !Config.Containers.AutoPickup.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (zdo.PrefabInfo.Piece is not null && zdo.Vars.GetPiece())
            return true; // ignore placed items (such as feasts)

        if (zdo.PrefabInfo.EggGrow is not null && zdo.Vars.GetGrowStart() > 0)
            return true;

        if (!CheckMinDistance(peers, zdo, Config.Containers.AutoPickupMinPlayerDistance.Value))
			return false; // player to close

		var shared = zdo.PrefabInfo.ItemDrop.m_itemData.m_shared;
        if (!SharedProcessorState.ContainersByItemName.TryGetValue(shared, out var containers))
            return false;

        HashSet<Vector2i>? usedSlots = null;
        ItemDrop.ItemData? item = null;

        foreach (var containerZdo in containers)
        {
            if (!containerZdo.IsValid() || containerZdo.PrefabInfo is not { Container: not null, Piece: not null, PieceTable: not null })
            {
                containers.Remove(containerZdo);
                continue;
            }

            if (Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > Config.Containers.AutoPickupRange.Value * Config.Containers.AutoPickupRange.Value)
                continue;

            if (containerZdo.Vars.GetInUse() || !CheckMinDistance(peers, containerZdo))
                continue; // in use or player to close

            if (item is null)
            {
                item = new() { m_shared = shared };
                PrivateAccessor.LoadFromZDO(item, zdo);
            }

            var stack = item.m_stack;
            usedSlots ??= new();
            usedSlots.Clear();

            ItemDrop.ItemData? containerItem = null;
            foreach (var slot in containerZdo.Inventory!.Items)
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
                containers.Remove(containerZdo);
                if (containers is { Count: 0 })
                    SharedProcessorState.ContainersByItemName.TryRemove(item.m_shared, out _);
                continue;
            }

            for (var emptySlots = containerZdo.Inventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
            {
                var amount = Math.Min(stack, item.m_shared.m_maxStackSize);

                var slot = containerItem.Clone();
                slot.m_stack = amount;
                slot.m_gridPos.x = -1;
                for (int x = 0; x < containerZdo.Inventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                {
                    for (int y = 0; y < containerZdo.Inventory.Inventory.GetHeight(); y++)
                    {
                        if (usedSlots.Add(new(x, y)))
                        {
                            (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                            break;
                        }
                    }
                }
                containerZdo.Inventory.Items.Add(slot);
                stack -= amount;
            }

            if (stack != item.m_stack)
            {
                containerZdo.Inventory.Save();
                (item.m_stack, stack) = (stack, item.m_stack);
                zdo.ClaimOwnershipInternal();
                ItemDrop.SaveToZDO(item, zdo);
                RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{containerZdo.PrefabInfo.Piece!.m_name}: $msg_added {item.m_shared.m_name} {stack}x");
            }

            if (item.m_stack is 0)
                break;
        }

        if (item?.m_stack is 0)
            DestroyZdo = true;

        return false;
    }
}
