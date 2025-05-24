using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class SmelterProcessor : Processor
{
    readonly List<ExtendedZDO> _smelters = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
    }

    void OnContainerChanged(ExtendedZDO containerZdo)
    {
        if (containerZdo.Inventory.Items.Count is 0)
            return;

        foreach (var zdo in _smelters)
        {
            if (Vector3.Distance(zdo.GetPosition(), containerZdo.GetPosition()) <= Config.Smelters.FeedFromContainersRange.Value)
                zdo.ResetProcessorDataRevision(this);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
	{
        if (zdo.PrefabInfo is not { Smelter: not null } and not { ShieldGenerator: not null})
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (Config.Smelters.CapacityMultiplier.Value is 1f)
        {
            if (zdo.PrefabInfo.Smelter is not null)
                zdo.Fields<Smelter>().Reset(x => x.m_maxFuel).Reset(x => x.m_maxOre);
            else
                zdo.Fields<ShieldGenerator>().Reset(x => x.m_maxFuel);
        }
        else
        {
            if (zdo.PrefabInfo.Smelter is null)
                RecreateZdo = zdo.Fields<ShieldGenerator>().SetIfChanged(x => x.m_maxFuel, Mathf.RoundToInt(Config.Smelters.CapacityMultiplier.Value * zdo.PrefabInfo.ShieldGenerator!.m_maxFuel));
            else
            {
                if (zdo.Fields<Smelter>().SetIfChanged(x => x.m_maxFuel, Mathf.RoundToInt(Config.Smelters.CapacityMultiplier.Value * zdo.PrefabInfo.Smelter.m_maxFuel)))
                    RecreateZdo = true;
                if (zdo.Fields<Smelter>().SetIfChanged(x => x.m_maxOre, Mathf.RoundToInt(Config.Smelters.CapacityMultiplier.Value * zdo.PrefabInfo.Smelter.m_maxOre)))
                    RecreateZdo = true;
            }
        }

        if (!Config.Smelters.FeedFromContainers.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!CheckMinDistance(peers, zdo))
            return false; // player to close

		/// <see cref="Smelter.OnAddFuel"/>
		{
            var maxFuel = zdo.PrefabInfo.Smelter is not null ? zdo.Fields<Smelter>().GetInt(x => x.m_maxFuel) : zdo.Fields<ShieldGenerator>().GetInt(x => x.m_maxFuel);
            var currentFuel = zdo.Vars.GetFuel();
            var maxFuelAdd = (int)(maxFuel - currentFuel);
            if (maxFuelAdd > maxFuel / 2)
            {
                foreach (var fuelItem in zdo.PrefabInfo.ShieldGenerator?.m_fuelItems.Select(x => x.m_itemData) ?? [zdo.PrefabInfo.Smelter!.m_fuelItem.m_itemData])
                {
                    var addedFuel = 0;
                    if (Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(fuelItem.m_shared, out var containers))
                    {
                        List<ItemDrop.ItemData>? removeSlots = null;
                        foreach (var containerZdo in containers)
                        {
                            if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
                            {
                                containers.Remove(containerZdo);
                                continue;
                            }

                            if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                                continue; // in use or player to close

                            var feedRangeSqr = containerZdo.Inventory.FeedRange ?? Config.Smelters.FeedFromContainersRange.Value;
                            feedRangeSqr *= feedRangeSqr;
                            if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > feedRangeSqr)
                                continue;

                            removeSlots?.Clear();
                            var addFuel = 0;
                            var leave = Config.Smelters.FeedFromContainersLeaveAtLeastFuel.Value;
                            var found = false;
                            var requestOwn = false;
                            foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == fuelItem).OrderBy(x => x.m_stack))
                            {
                                found = found || slot is { m_stack: > 0 };
                                var take = Math.Min(maxFuelAdd, slot.m_stack);
                                var leaveDiff = Math.Min(take, leave);
                                leave -= leaveDiff;
                                take -= leaveDiff;
                                if (take is 0)
                                    continue;
                                else if (!containerZdo.IsOwner())
                                {
                                    requestOwn = true;
                                    break;
                                }

                                addFuel += take;
                                slot.m_stack -= take;
                                if (slot.m_stack is 0)
                                    (removeSlots ??= new()).Add(slot);

                                maxFuelAdd -= take;
                                if (maxFuelAdd is 0)
                                    break;
                            }

                            if (requestOwn)
                            {
                                Instance<ContainerProcessor>().RequestOwnership(containerZdo, 0);
                                continue;
                            }

                            if (addFuel is 0)
                            {
                                if (!found)
                                {
                                    containers.Remove(containerZdo);
                                    if (containers is { Count: 0 })
                                        Instance<ContainerProcessor>().ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
                                }
                                continue;
                            }

                            if (removeSlots is { Count: > 0 })
                            {
                                foreach (var remove in removeSlots)
                                    containerZdo.Inventory.Items.Remove(remove);

                                if (containerZdo.Inventory.Items is { Count: 0 })
                                {
                                    containers.Remove(containerZdo);
                                    if (containers is { Count: 0 })
                                        Instance<ContainerProcessor>().ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
                                }
                            }

                            zdo.ClaimOwnership();
                            currentFuel += addFuel;
                            zdo.Vars.SetFuel(currentFuel);
                            containerZdo.Inventory.Save();

                            addedFuel += addFuel;

                            if (maxFuelAdd is 0)
                                break;
                        }
                        if (maxFuelAdd is 0)
                            break;
                    }
                    if (addedFuel is not 0)
                        ShowMessage(peers, zdo, $"{zdo.PrefabInfo.Smelter?.m_name ?? zdo.PrefabInfo.ShieldGenerator!.m_name}: $msg_added {fuelItem.m_shared.m_name} {addedFuel}x", Config.Smelters.OreOrFuelAddedMessageType.Value);
                }
            }
        }

        /// <see cref="Smelter.OnAddOre"/> <see cref="Smelter.QueueOre"/>
        if (zdo.PrefabInfo.Smelter is not null)
        {
            int maxOre = zdo.Fields<Smelter>().GetInt(x => x.m_maxOre);
            var currentOre = zdo.Vars.GetQueued();
            var maxOreAdd = maxOre - currentOre;
            if (maxOreAdd > maxOre / 2)
            {
                foreach (var conversion in zdo.PrefabInfo.Smelter.m_conversion)
                {
                    var oreItem = conversion.m_from.m_itemData;
                    var addedOre = 0;
                    if (Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(oreItem.m_shared, out var containers))
                    {
                        List<ItemDrop.ItemData>? removeSlots = null;
                        foreach (var containerZdo in containers)
                        {
                            if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
                            {
                                containers.Remove(containerZdo);
                                continue;
                            }

                            if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                                continue; // in use or player to close

                            var feedRangeSqr = containerZdo.Inventory.FeedRange ?? Config.Smelters.FeedFromContainersRange.Value;
                            feedRangeSqr *= feedRangeSqr;
                            if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > feedRangeSqr)
                                continue;

                            removeSlots?.Clear();
                            int addOre = 0;
                            var leave = Config.Smelters.FeedFromContainersLeaveAtLeastOre.Value;
                            var found = false;
                            var requestOwn = false;
                            foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == oreItem).OrderBy(x => x.m_stack))
                            {
                                found = found || slot is { m_stack: > 0 };
                                var take = Math.Min(maxOreAdd, slot.m_stack);
                                var leaveDiff = Math.Min(take, leave);
                                leave -= leaveDiff;
                                take -= leaveDiff;
                                if (take is 0)
                                    continue;
                                else if (!containerZdo.IsOwner())
                                {
                                    requestOwn = true;
                                    break;
                                }

                                addOre += take;
                                slot.m_stack -= take;
                                if (slot.m_stack is 0)
                                    (removeSlots ??= new()).Add(slot);

                                maxOreAdd -= take;
                                if (maxOreAdd is 0)
                                    break;
                            }

                            if (requestOwn)
                            {
                                Instance<ContainerProcessor>().RequestOwnership(containerZdo, 0);
                                continue;
                            }

                            if (addOre is 0)
                            {
                                if (!found)
                                {
                                    containers.Remove(containerZdo);
                                    if (containers is { Count: 0 })
                                        Instance<ContainerProcessor>().ContainersByItemName.TryRemove(oreItem.m_shared, out _);
                                }
                                continue;
                            }

                            if (removeSlots is { Count: > 0 })
                            {
                                foreach (var remove in removeSlots)
                                    containerZdo.Inventory.Items.Remove(remove);

                                if (containerZdo.Inventory.Items is { Count: 0 })
                                {
                                    containers.Remove(containerZdo);
                                    if (containers is { Count: 0 })
                                        Instance<ContainerProcessor>().ContainersByItemName.TryRemove(oreItem.m_shared, out _);
                                }
                            }

                            zdo.ClaimOwnership();
                            for (int i = 0; i < addOre; i++)
                                zdo.Vars.SetItem(currentOre + i, conversion.m_from.gameObject.name);
                            currentOre += addOre;
                            zdo.Vars.SetQueued(currentOre);

                            containerZdo.Inventory.Save();

                            addedOre += addOre;

                            if (maxOreAdd is 0)
                                break;
                        }
                    }

                    if (addedOre is not 0)
                        ShowMessage(peers, zdo, $"{zdo.PrefabInfo.Smelter.m_name}: $msg_added {oreItem.m_shared.m_name} {addedOre}x", Config.Smelters.OreOrFuelAddedMessageType.Value);
                }
            }
        }

        if (!_smelters.Contains(zdo))
        {
            _smelters.Add(zdo);
            zdo.Destroyed += x => _smelters.Remove(x);
        }

        return true;
    }
}