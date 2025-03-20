using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class SmelterProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
	{
		if (!Config.Smelters.FeedFromContainers.Value || zdo.PrefabInfo.Smelter is null)
			return false;

		if (!CheckMinDistance(peers, zdo))
			return false; // player to close

		/// <see cref="Smelter.OnAddFuel"/>
		{
            var maxFuel = zdo.Fields<Smelter>().GetInt(x => x.m_maxFuel);
            var currentFuel = zdo.GetFloat(ZDOVars.s_fuel);
            var maxFuelAdd = (int)(maxFuel - currentFuel);
            if (maxFuelAdd > maxFuel / 2)
            {
                var fuelItem = zdo.PrefabInfo.Smelter.m_fuelItem.m_itemData;
                var addedFuel = 0;
                if (SharedProcessorState.ContainersByItemName.TryGetValue(fuelItem.m_shared, out var containers))
                {
                    List<ItemDrop.ItemData>? removeSlots = null;
                    foreach (var containerZdo in containers)
                    {
                        if (!containerZdo.IsValid() || containerZdo.PrefabInfo is not { Container: not null, Piece: not null, PieceTable: not null })
                        {
                            containers.Remove(containerZdo);
                            continue;
                        }

                        if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                            continue;

                        if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                            continue; // in use or player to close

                        removeSlots?.Clear();
                        float addFuel = 0;
                        var leave = Config.Smelters.FeedFromContainersLeaveAtLeastFuel.Value;
                        foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == fuelItem).OrderBy(x => x.m_stack))
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
                            containers.Remove(containerZdo);
                            if (containers is { Count: 0 })
                                SharedProcessorState.ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
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
                                    SharedProcessorState.ContainersByItemName.TryRemove(fuelItem.m_shared, out _);
                            }
                        }

                        zdo.Set(ZDOVars.s_fuel, currentFuel + addFuel);

                        containerZdo.Inventory.Save();

                        addedFuel += (int)addFuel;

                        if (maxFuelAdd is 0)
                            break;
                    }
                }

                if (addedFuel is not 0)
                    Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Piece?.m_name ?? zdo.PrefabInfo.Smelter.m_name}: $msg_added {fuelItem.m_shared.m_name} {addedFuel}x");
            }
        }

        /// <see cref="Smelter.OnAddOre"/> <see cref="Smelter.QueueOre"/>
        {
            int maxOre = zdo.Fields<Smelter>().GetInt(x => x.m_maxOre);
            var currentOre = zdo.GetInt(ZDOVars.s_queued);
            var maxOreAdd = maxOre - zdo.GetInt(ZDOVars.s_queued);
            if (maxOreAdd > maxOre / 2)
            {
                foreach (var conversion in zdo.PrefabInfo.Smelter.m_conversion)
                {
                    var oreItem = conversion.m_from.m_itemData;
                    var addedOre = 0;
                    if (SharedProcessorState.ContainersByItemName.TryGetValue(oreItem.m_shared, out var containers))
                    {
                        List<ItemDrop.ItemData>? removeSlots = null;
                        foreach (var containerZdo in containers)
                        {
                            if (!containerZdo.IsValid() || containerZdo.PrefabInfo is not { Container: not null, Piece: not null, PieceTable: not null })
                            {
                                containers.Remove(containerZdo);
                                continue;
                            }

                            if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > 4)
                                continue;

                            if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                                continue; // in use or player to close

                            removeSlots?.Clear();
                            int addOre = 0;
                            var leave = Config.Smelters.FeedFromContainersLeaveAtLeastOre.Value;
                            foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == oreItem).OrderBy(x => x.m_stack))
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
                                containers.Remove(containerZdo);
                                if (containers is { Count: 0 })
                                    SharedProcessorState.ContainersByItemName.TryRemove(oreItem.m_shared, out _);
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
                                        SharedProcessorState.ContainersByItemName.TryRemove(oreItem.m_shared, out _);
                                }
                            }

                            zdo.SetOwner(ZDOMan.GetSessionID());
                            for (int i = 0; i < addOre; i++)
                                zdo.Set($"item{currentOre + i}", conversion.m_from.gameObject.name);
                            zdo.Set(ZDOVars.s_queued, currentOre + addOre);

                            containerZdo.Inventory.Save();

                            addedOre += addOre;

                            if (maxOreAdd is 0)
                                break;
                        }
                    }

                    if (addedOre is not 0)
                        Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Piece?.m_name ?? zdo.PrefabInfo.Smelter.m_name}: $msg_added {oreItem.m_shared.m_name} {addedOre}x");
                }
            }
        }

        return false;
    }
}