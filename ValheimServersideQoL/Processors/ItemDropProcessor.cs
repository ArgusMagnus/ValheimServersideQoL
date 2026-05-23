using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class ItemDropProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("0c2236b3-e371-4bb9-9724-e1eb83a9679e");

    readonly Dictionary<ExtendedZDO, DateTimeOffset> _eggDropTime = [];
    readonly SectorDictionary<HashSet<ExtendedZDO>> _itemDrops = new(1);
    readonly SectorDictionary<ExtendedZDO> _crates = new(4);
    float _maxPickupRange;

    readonly record struct PositionKey(int X, int Z)
    {
        public PositionKey(Vector3 pos, float range)
            : this(Mathf.RoundToInt(pos.x / range), Mathf.RoundToInt(pos.z / range)) { }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        _maxPickupRange = 0;
        Instance<ContainerProcessor>().ContainerChanged -= OnContainerChanged;
        if (Config.Containers.AutoPickup.Value)
        {
            Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
            _maxPickupRange = Mathf.Max(Config.Containers.AutoPickupRange.Value, Config.Containers.AutoPickupMaxRange.Value);
            _itemDrops.Reset(_maxPickupRange);
        }

        if (!firstTime)
            return;

        _eggDropTime.Clear();
        _itemDrops.Clear();
    }

    void OnContainerChanged(ExtendedZDO containerZdo)
    {
        var inventory = containerZdo.GetInventory();
        if (inventory.Items.Count is 0)
            return;

        var rangeSqr = inventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
        rangeSqr *= rangeSqr;

        foreach (var itemDrops in _itemDrops.EnumerateAdjacent(containerZdo.GetPosition()))
        {
            foreach (var zdo in itemDrops)
            {
                if (Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) <= rangeSqr)
                    zdo.ResetProcessorDataRevision(this);
            }
        }
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        base.PreProcessCore(peers);
        _crates.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;

        if (zdo.PrefabInfo.ItemDrop is null || (Config.TrophySpawner.Enable.Value && Instance<TrophyProcessor>().IsAttracting(zdo)))
            return false;

        if (zdo.PrefabInfo.ItemDrop.Value.Piece.Value is not null && zdo.Vars.GetPiece())
            return false; // ignore placed items (such as feasts)

        ItemDrop.ItemData? item = null;

        if (_maxPickupRange > 0)
        {
            UnregisterZdoProcessor = false;

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
            var requestOwn = false;
            var excludeFodderCheckComplete = !Config.Containers.AutoPickupExcludeFodder.Value;
            HashSet<Vector2i>? usedSlots = null;
            List<ExtendedZDO>? toRemove = null;

            foreach (var containers in Instance<ContainerProcessor>().ContainersByItemName.EnumerateAdjacent((zdo.GetPosition(), shared)))
            {
                if (containers.Count > 0 && !excludeFodderCheckComplete)
                {
                    excludeFodderCheckComplete = true;
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
                            return false;
                        }
                    }
                }

                toRemove?.Clear();

                foreach (var containerZdo in containers)
                {
                    if (containerZdo.PrefabInfo.Container is null)
                    {
                        (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                        continue; // in use or player to close

                    var inventory = containerZdo.GetInventory();
                    var pickupRangeSqr = inventory.PickupRange ?? Config.Containers.AutoPickupRange.Value;
                    pickupRangeSqr *= pickupRangeSqr;

                    if (pickupRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > pickupRangeSqr)
                        continue;

                    if (item is null)
                    {
                        item = new() { m_shared = shared };
                        ItemDrop.LoadFromZDO(item, zdo);
                    }

                    var stack = item.m_stack;
                    (usedSlots ??= []).Clear();

                    var requestContainerOwn = false;

                    ItemDrop.ItemData? containerItem = null;
                    foreach (var slot in inventory.Items)
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
                        (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    for (var emptySlots = inventory.Inventory.GetEmptySlots(); stack > 0 && emptySlots > 0; emptySlots--)
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
                        for (int x = 0; x < inventory.Inventory.GetWidth() && slot.m_gridPos.x < 0; x++)
                        {
                            for (int y = 0; y < inventory.Inventory.GetHeight(); y++)
                            {
                                if (usedSlots.Add(new(x, y)))
                                {
                                    (slot.m_gridPos.x, slot.m_gridPos.y) = (x, y);
                                    break;
                                }
                            }
                        }
                        inventory.Items.Add(slot);
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
                        inventory.Save();
                        (item.m_stack, stack) = (stack, item.m_stack);
                        ItemDrop.SaveToZDO(item, zdo);
                        ShowMessage(peers, containerZdo,
                            Config.Localization.Containers.FormatAutoPickup(containerZdo.PrefabInfo.Container.Value.Container.m_name, item.m_shared.m_name, stack),
                            Config.Containers.PickedUpMessageType.Value);
                    }

                    if (item.m_stack is 0)
                        break;
                }

                if (toRemove is not null)
                {
                    foreach (var containerZdo in toRemove)
                        containers.Remove(containerZdo);
                }

                if (item?.m_stack is 0)
                {
                    DestroyZdo = true;
                    return false;
                }

                if (requestOwn)
                {
                    RPC.RequestOwn(zdo);
                    return true;
                }
            }

            _itemDrops.TryAdd(zdo);
        }

        if (Config.World.MakeAllItemsFloat.Value && zdo.PrefabInfo.ItemDrop is { Floating.Value: null, Fish.Value: null })
        {
            UnregisterZdoProcessor = false;

            if (zdo.GetPosition() is { y: < ZoneSystem.c_WaterLevel }  &&
                zdo.Vars.GetSpawnTime() < ZNet.instance.GetTime().AddSeconds(-2) &&
                GetHeight(zdo.GetPosition()) is < ZoneSystem.c_WaterLevel - 2)
            {
                var pos = zdo.GetPosition();
                pos.y += 1;
                var crate = GetCrate(pos, zdo.GetRotation());

                if (item is null)
                {
                    item = new() { m_shared = zdo.PrefabInfo.ItemDrop.Value.ItemDrop.m_itemData.m_shared };
                    ItemDrop.LoadFromZDO(item, zdo);
                }

                var inventory = crate.GetInventory();

                foreach (var slot in inventory.Items)
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
                    item.m_dropPrefab = zdo.PrefabInfo.ItemDrop.Value.ItemDrop.gameObject;
                    inventory.Items.Add(item);
                    var (width, height) = PlayerProcessor.GetBackpackSize(inventory.Items.Count);
                    crate.Fields<Container>()
                        .Set(static () => x => x.m_width, width)
                        .Set(static () => x => x.m_height, height);

                    Logger.DevLog($"Putting {item.m_dropPrefab.name} in crate ({width}x{height}): stack={item.m_stack}, pos={item.m_gridPos}, items={inventory.Items.Count}");

                    using var enumerator = inventory.Items.GetEnumerator();
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            if (!enumerator.MoveNext())
                                break;
                            Logger.DevLog($"Setting pos of {enumerator.Current.m_dropPrefab.name} to {x},{y}");
                            enumerator.Current.m_gridPos = new(x, y);
                        }
                    }
                }

                crate.ClaimOwnershipInternal();
                inventory.Save();
                crate.SetOwnerInternal(zdo.GetOwner());
                DestroyZdo = true;
                return false;
            }
        }

        return true;
    }

    ExtendedZDO GetCrate(Vector3 pos, Quaternion rot)
    {
        if (!_crates.TryGetValue(pos, out var crate))
        {
            _crates.Add(pos, crate = PlaceObject(pos, Prefabs.CargoCrate, rot));
            //PlacedObjects.Remove(crate); // remove exclusive access
            crate.Vars.SetCreator(0);
            Logger.DevLog($"Created crate for item drop at {crate.GetPosition()}");
        }
        return crate;
    }
}
