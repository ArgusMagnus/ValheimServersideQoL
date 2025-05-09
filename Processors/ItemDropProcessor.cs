using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class ItemDropProcessor : Processor
{
    readonly Dictionary<ExtendedZDO, DateTimeOffset> _eggDropTime = new();
    readonly List<ExtendedZDO> _itemDrops = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
    }

    void OnEggDropDestroyed(ExtendedZDO zdo)
    {
        _eggDropTime.Remove(zdo);
    }

    void OnContainerChanged(ExtendedZDO containerZdo)
    {
        if (containerZdo.Inventory.Items.Count is 0)
            return;

        foreach (var zdo in _itemDrops)
        {
            if (Vector3.Distance(zdo.GetPosition(), containerZdo.GetPosition()) <= Config.Containers.AutoPickupRange.Value)
                zdo.ResetProcessorDataRevision(this);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
	{
        if (zdo.PrefabInfo.ItemDrop is null || !Config.Containers.AutoPickup.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (zdo.PrefabInfo.ItemDrop.Value.Piece.Value is not null && zdo.Vars.GetPiece())
            return true; // ignore placed items (such as feasts)

        if (zdo.PrefabInfo.EggGrow is not null)
        {
            if (zdo.Vars.GetGrowStart() > 0)
                return true;

            if (!_eggDropTime.TryGetValue(zdo, out var dropTime))
            {
                _eggDropTime.Add(zdo, DateTimeOffset.UtcNow);
                zdo.Destroyed += OnEggDropDestroyed;
                return false;
            }
            if (DateTimeOffset.UtcNow - dropTime < TimeSpan.FromSeconds(2 * zdo.PrefabInfo.EggGrow.m_updateInterval + 2))
                return false;
            _eggDropTime.Remove(zdo);
            zdo.Destroyed -= OnEggDropDestroyed;
        }

        if (!CheckMinDistance(peers, zdo, Config.Containers.AutoPickupMinPlayerDistance.Value))
			return false; // player to close

		var shared = zdo.PrefabInfo.ItemDrop.Value.ItemDrop.m_itemData.m_shared;
        if (!Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(shared, out var containers))
            return false;

        if (Config.Containers.AutoPickupExcludeFodder.Value)
        {
            foreach (var tameZdo in Instance<TameableProcessor>().Tames)
            {
                if (tameZdo.PrefabInfo.Tameable is null)
                    continue;

                /// <see cref="MonsterAI.CanConsume(ItemDrop.ItemData)"/>
                if (!tameZdo.PrefabInfo.Tameable.Value.MonsterAI.m_consumeItems.Any(x => x.m_itemData.m_shared.m_name == shared.m_name))
                    continue;
                var rangeSqr = tameZdo.PrefabInfo.Tameable.Value.MonsterAI.m_consumeSearchRange;
                rangeSqr *= rangeSqr;
                if (Utils.DistanceSqr(zdo.GetPosition(), tameZdo.GetPosition()) < rangeSqr)
                    return false;
            }
        }

        HashSet<Vector2i>? usedSlots = null;
        ItemDrop.ItemData? item = null;

        foreach (var containerZdo in containers)
        {
            if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
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
                    Instance<ContainerProcessor>().ContainersByItemName.TryRemove(item.m_shared, out _);
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
                RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{containerZdo.PrefabInfo.Container.Value.Piece.m_name}: $msg_added {item.m_shared.m_name} {stack}x");
            }

            if (item.m_stack is 0)
                break;
        }

        if (item?.m_stack is 0)
            DestroyZdo = true;
        else if (!_itemDrops.Contains(zdo))
        {
            _itemDrops.Add(zdo);
            zdo.Destroyed += x => _itemDrops.Remove(x);
        }

        return true;
    }
}
