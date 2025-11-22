using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class ItemDropProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("0c2236b3-e371-4bb9-9724-e1eb83a9679e");

    readonly Dictionary<ExtendedZDO, DateTimeOffset> _eggDropTime = [];
    readonly List<ExtendedZDO> _itemDrops = [];
    readonly Dictionary<Vector2i, ExtendedZDO> _crates = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _eggDropTime.Clear();
        _itemDrops.Clear();
        Instance<ContainerProcessor>().ContainerChanged -= OnContainerChanged;
        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
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

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.ItemDrop is null || !Config.Containers.AutoPickup.Value || (Config.TrophySpawner.Enable.Value && Instance<TrophyProcessor>().IsAttracting(zdo)))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (zdo.PrefabInfo.ItemDrop.Value.Piece.Value is not null && zdo.Vars.GetPiece())
        {
            UnregisterZdoProcessor = true;
            return false; // ignore placed items (such as feasts)
        }

        if (zdo.PrefabInfo.EggGrow is not null)
        {
            if (zdo.Vars.GetGrowStart() > 0)
                return true;

            if (!_eggDropTime.TryGetValue(zdo, out var dropTime))
            {
                _eggDropTime.Add(zdo, DateTimeOffset.UtcNow);
                zdo.Destroyed += x => _eggDropTime.Remove(x);
                return false;
            }
            if (DateTimeOffset.UtcNow - dropTime < TimeSpan.FromSeconds(2 * zdo.PrefabInfo.EggGrow.m_updateInterval + 2))
                return false;
        }

        if (!CheckMinDistance(peers, zdo, Config.Containers.AutoPickupMinPlayerDistance.Value))
			return false; // player to close

        var shared = zdo.PrefabInfo.ItemDrop.Value.ItemDrop.m_itemData.m_shared;
        ItemDrop.ItemData? item = null;
        var requestOwn = false;

        if (Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(shared, out var containers))
        {
            if (Config.Containers.AutoPickupExcludeFodder.Value)
            {
                foreach (var tameState in Instance<TameableProcessor>().Tames)
                {
                    if (tameState.ZDO.PrefabInfo.Tameable is null)
                        continue;

                    /// <see cref="MonsterAI.CanConsume(ItemDrop.ItemData)"/>
                    if (!tameState.ZDO.PrefabInfo.Tameable.Value.MonsterAI.m_consumeItems.Any(x => x.m_itemData.m_shared.m_name == shared.m_name))
                        continue;
                    var rangeSqr = tameState.ZDO.PrefabInfo.Tameable.Value.MonsterAI.m_consumeSearchRange;
                    rangeSqr *= rangeSqr;
                    if (Utils.DistanceSqr(zdo.GetPosition(), tameState.ZDO.GetPosition()) < rangeSqr)
                    {
                        if (zdo.PrefabInfo.ItemDrop is { ZSyncTransform.Value: not null } && zdo.GetTimeSinceSpawned() < TimeSpan.FromSeconds(10))
                            return false;

                        UnregisterZdoProcessor = true;
                        var fields = zdo.Fields<ItemDrop>();
                        if (fields.UpdateValue(static () => x => x.m_autoPickup, false))
                            RecreateZdo = true;
                        if (fields.UpdateValue(static () => x => x.m_autoDestroy, false))
                            RecreateZdo = true;
                        if (RecreateZdo)
                            zdo.ReleaseOwnershipInternal();
                        return false;
                    }
                }
            }

            HashSet<Vector2i>? usedSlots = null;

            foreach (var containerZdo in containers)
            {
                if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
                {
                    containers.Remove(containerZdo);
                    continue;
                }

                if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                    continue; // in use or player to close

                var pickupRangeSqr = containerZdo.Inventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                pickupRangeSqr *= pickupRangeSqr;

                if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                    continue;

                if (item is null)
                {
                    item = new() { m_shared = shared };
                    PrivateAccessor.LoadFromZDO(item, zdo);
                }

                var stack = item.m_stack;
                usedSlots ??= [];
                usedSlots.Clear();

                var requestContainerOwn = false;

                ItemDrop.ItemData? containerItem = null;
                foreach (var slot in containerZdo.Inventory.Items)
                {
                    usedSlots.Add(slot.m_gridPos);
                    if (new ItemDataKey(item) != slot)
                        continue;

                    containerItem ??= slot;

                    var maxAmount = slot.m_shared.m_maxStackSize - slot.m_stack;
                    if (maxAmount <= 0)
                        continue;

                    if (Config.Containers.AutoPickupRequestOwnership.Value && !zdo.IsOwnerOrUnassigned())
                        requestOwn = true;
                    if (!containerZdo.IsOwnerOrUnassigned())
                        requestContainerOwn = true;
                    if (requestOwn || requestContainerOwn)
                        break;

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
                    if (Config.Containers.AutoPickupRequestOwnership.Value && !zdo.IsOwnerOrUnassigned())
                        requestOwn = true;
                    if (!containerZdo.IsOwnerOrUnassigned())
                        requestContainerOwn = true;
                    if (requestOwn || requestContainerOwn)
                        break;

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

                if (requestOwn || requestContainerOwn)
                {
                    if (requestContainerOwn)
                        Instance<ContainerProcessor>().RequestOwnership(containerZdo, 0);
                    continue;
                }

                if (stack != item.m_stack)
                {
                    containerZdo.Inventory.Save();
                    (item.m_stack, stack) = (stack, item.m_stack);
                    ItemDrop.SaveToZDO(item, zdo);
                    ShowMessage(peers, containerZdo,
                        Config.Localization.Containers.FormatAutoPickup(containerZdo.PrefabInfo.Container.Value.Container.m_name, item.m_shared.m_name, stack),
                        Config.Containers.PickedUpMessageType.Value);
                }

                if (item.m_stack is 0)
                    break;
            }

            if (item?.m_stack is 0)
            {
                DestroyZdo = true;
                return false;
            }
            
            if (!_itemDrops.Contains(zdo))
            {
                _itemDrops.Add(zdo);
                zdo.Destroyed += x => _itemDrops.Remove(x);
            }

            if (requestOwn)
            {
                RPC.RequestOwn(zdo);
                return true;
            }
        }

        if (zdo.PrefabInfo.ItemDrop is { Floating.Value: null, Fish.Value: null } &&
            Config.World.MakeAllItemsFloat.Value &&
            zdo.Vars.GetSpawnTime() < ZNet.instance.GetTime().AddSeconds(-2) &&
            GetHeight(zdo.GetPosition()) is < ZoneSystem.c_WaterLevel - 2 && zdo.GetPosition() is { y: < ZoneSystem.c_WaterLevel })
        {
            var crate = GetCrate(zdo.GetPosition(), zdo.GetRotation());

            if (item is null)
            {
                item = new() { m_shared = shared };
                PrivateAccessor.LoadFromZDO(item, zdo);
            }

            item.m_dropPrefab = zdo.PrefabInfo.ItemDrop.Value.ItemDrop.gameObject;

            foreach (var slot in crate.Inventory.Items)
            {
                if (ItemDataKeyComparer.Instance.Equals(slot, item))
                {
                    var transfer = Math.Min(item.m_stack, slot.m_shared.m_maxStackSize - slot.m_stack);
                    slot.m_stack += transfer;
                    item.m_stack -= transfer;
                    if (item.m_stack is 0)
                        break;
                }
            }

            if (item.m_stack > 0)
            {
                crate.Inventory.Items.Add(item);
                var (width, height) = PlayerProcessor.GetBackpackSize(crate.Inventory.Items.Count);
                var fields = crate.Fields<Container>();
                var changed = (
                    fields.UpdateValue(static () => x => x.m_width, width),
                    fields.UpdateValue(static () => x => x.m_height, height)) != (false, false);
                if (changed)
                {
                    using var enumerator = crate.Inventory.Items.GetEnumerator();
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            if (!enumerator.MoveNext())
                                break;
                            enumerator.Current.m_gridPos = new(x, y);
                        }
                    }

                }
            }

            crate.Inventory.Save();
            DestroyZdo = true;
            return false;
        }

        return true;
    }

    ExtendedZDO GetCrate(Vector3 pos, Quaternion rot)
    {
        // round to 4 meters
        var key = new Vector2i(Mathf.FloorToInt(pos.x / 4), Mathf.FloorToInt(pos.z / 4));
        if (!_crates.TryGetValue(key, out var crate) || !crate.IsOwnerOrUnassigned())
        {
            if (crate is not null)
                _crates.Remove(key);
            _crates.Add(key, crate = PlaceObject(pos, Prefabs.CargoCrate, rot));
            PlacedObjects.Remove(crate); // remove exclusive access
            crate.Destroyed += _ => _crates.Remove(key);
            crate.Vars.SetCreator(0);
        }
        return crate;
    }
}
