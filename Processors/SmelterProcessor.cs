using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class SmelterProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (!Config.Smelters.FeedFromContainers.Value || prefabInfo.Smelter is null)
            return;

        if (!CheckMinDistance(peers, zdo))
            return; // player to close

        var hasFields = zdo.Fields<Smelter>().GetHasFields();

        /// <see cref="Smelter.OnAddFuel"/>
        {
            int maxFuel = prefabInfo.Smelter.m_maxFuel;
            if (hasFields)
                maxFuel = zdo.Fields<Smelter>().GetInt(x => x.m_maxFuel, maxFuel);
            var currentFuel = zdo.GetFloat(ZDOVars.s_fuel);
            var maxFuelAdd = (int)(maxFuel - currentFuel);
            if (maxFuelAdd > maxFuel / 2)
            {
                var fuelItem = prefabInfo.Smelter.m_fuelItem.m_itemData;
                var addedFuel = 0;
                if (SharedState.ContainersByItemName.TryGetValue(fuelItem.m_shared, out var containers))
                {
                    List<ItemDrop.ItemData>? removeSlots = null;
                    foreach (var (containerZdoId, inventory) in containers.Select(x => (x.Key, x.Value)))
                    {
                        if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                        {
                            containers.TryRemove(containerZdoId, out _);
                            continue;
                        }

                        if (!SharedState.DataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                            continue;

                        if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                            continue;

                        if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                            continue; // in use or player to close

                        inventory.Update(containerZdo);

                        removeSlots?.Clear();
                        float addFuel = 0;
                        var leave = Config.Smelters.FeedFromContainersLeaveAtLeastFuel.Value;
                        foreach (var slot in inventory.Inventory.GetAllItems().Where(x => new ItemKey(x) == fuelItem).OrderBy(x => x.m_stack))
                        {
                            var take = Math.Min(maxFuelAdd, slot.m_stack);
                            var leaveDiff = Math.Min(take, leave);
                            leave -= leaveDiff;
                            take -= leaveDiff;
                            if (take is 0)
                                continue;

                            addFuel += take;
                            slot.m_stack -= take;
                            if (slot.m_stack is 0)
                                (removeSlots ??= new()).Add(slot);

                            maxFuelAdd -= take;
                            if (maxFuelAdd is 0)
                                break;
                        }

                        if (addFuel is 0)
                        {
                            containers.TryRemove(containerZdoId, out _);
                            if (containers is { Count: 0 })
                                SharedState.ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
                            continue;
                        }

                        if (removeSlots is { Count: > 0 })
                        {
                            if (!ReferenceEquals(inventory.Inventory.GetAllItems(), inventory.Inventory.GetAllItems()))
                                throw new Exception("Algorithm assumption violated");
                            foreach (var remove in removeSlots)
                                inventory.Inventory.GetAllItems().Remove(remove);

                            if (inventory.Inventory.GetAllItems() is { Count: 0 })
                            {
                                containers.TryRemove(containerZdoId, out _);
                                if (containers is { Count: 0 })
                                    SharedState.ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
                            }
                        }

                        zdo.Set(ZDOVars.s_fuel, currentFuel + addFuel);

                        var pkg = new ZPackage();
                        inventory.Inventory.Save(pkg);
                        containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                        SharedState.DataRevisions[containerZdo.m_uid] = inventory.DataRevision = containerZdo.DataRevision;

                        addedFuel += (int)addFuel;

                        if (maxFuelAdd is 0)
                            break;
                    }
                }

                if (addedFuel is not 0)
                    Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name}: $msg_added {fuelItem.m_shared.m_name} {addedFuel}x");
            }
        }

        /// <see cref="Smelter.OnAddOre"/> <see cref="Smelter.QueueOre"/>
        {
            int maxOre = prefabInfo.Smelter.m_maxOre;
            if (hasFields)
                maxOre = zdo.Fields<Smelter>().GetInt(x => x.m_maxOre, maxOre);
            var currentOre = zdo.GetInt(ZDOVars.s_queued);
            var maxOreAdd = maxOre - zdo.GetInt(ZDOVars.s_queued);
            if (maxOreAdd > maxOre / 2)
            {
                foreach (var conversion in prefabInfo.Smelter.m_conversion)
                {
                    var oreItem = conversion.m_from.m_itemData;
                    var addedOre = 0;
                    if (SharedState.ContainersByItemName.TryGetValue(oreItem.m_shared, out var containers))
                    {
                        List<ItemDrop.ItemData>? removeSlots = null;
                        foreach (var (containerZdoId, inventory) in containers.Select(x => (x.Key, x.Value)))
                        {
                            if (ZDOMan.instance.GetZDO(containerZdoId) is not { } containerZdo)
                            {
                                containers.TryRemove(containerZdoId, out _);
                                continue;
                            }

                            if (!SharedState.DataRevisions.TryGetValue(containerZdoId, out var containerDataRevision) || containerZdo.DataRevision != containerDataRevision)
                                continue;

                            if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                                continue;

                            if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                                continue; // in use or player to close

                            inventory.Update(containerZdo);

                            removeSlots?.Clear();
                            int addOre = 0;
                            var leave = Config.Smelters.FeedFromContainersLeaveAtLeastOre.Value;
                            foreach (var slot in inventory.Inventory.GetAllItems().Where(x => new ItemKey(x) == oreItem).OrderBy(x => x.m_stack))
                            {
                                var take = Math.Min(maxOreAdd, slot.m_stack);
                                var leaveDiff = Math.Min(take, leave);
                                leave -= leaveDiff;
                                take -= leaveDiff;
                                if (take is 0)
                                    continue;

                                addOre += take;
                                slot.m_stack -= take;
                                if (slot.m_stack is 0)
                                    (removeSlots ??= new()).Add(slot);

                                maxOreAdd -= take;
                                if (maxOreAdd is 0)
                                    break;
                            }

                            if (addOre is 0)
                            {
                                containers.TryRemove(containerZdoId, out _);
                                if (containers is { Count: 0 })
                                    SharedState.ContainersByItemName.TryRemove(oreItem.m_shared, out _);
                                continue;
                            }

                            if (removeSlots is { Count: > 0 })
                            {
                                if (!ReferenceEquals(inventory.Inventory.GetAllItems(), inventory.Inventory.GetAllItems()))
                                    throw new Exception("Algorithm assumption violated");
                                foreach (var remove in removeSlots)
                                    inventory.Inventory.GetAllItems().Remove(remove);

                                if (inventory.Inventory.GetAllItems() is { Count: 0 })
                                {
                                    containers.TryRemove(containerZdoId, out _);
                                    if (containers is { Count: 0 })
                                        SharedState.ContainersByItemName.TryRemove(oreItem.m_shared, out _);
                                }
                            }

                            zdo.SetOwner(ZDOMan.GetSessionID());
                            for (int i = 0; i < addOre; i++)
                                zdo.Set($"item{currentOre + i}", conversion.m_from.gameObject.name);
                            zdo.Set(ZDOVars.s_queued, currentOre + addOre);

                            var pkg = new ZPackage();
                            inventory.Inventory.Save(pkg);
                            containerZdo.Set(ZDOVars.s_items, pkg.GetBase64());
                            SharedState.DataRevisions[containerZdo.m_uid] = inventory.DataRevision = containerZdo.DataRevision;

                            addedOre += addOre;

                            if (maxOreAdd is 0)
                                break;
                        }
                    }

                    if (addedOre is not 0)
                        Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{prefabInfo.Piece?.m_name ?? prefabInfo.Smelter.m_name}: $msg_added {oreItem.m_shared.m_name} {addedOre}x");
                }
            }
        }
    }
}