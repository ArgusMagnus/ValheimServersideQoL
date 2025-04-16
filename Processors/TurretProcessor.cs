﻿using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TurretProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override async ValueTask<bool> ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Turret is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var fields = zdo.Fields<Turret>();
        if (!Config.Turrets.DontTargetPlayers.Value)
            fields.Reset(x => x.m_targetPlayers);
        else if (fields.SetIfChanged(x => x.m_targetPlayers, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(x => x.m_targetTamed);
        else if (fields.SetIfChanged(x => x.m_targetTamed, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(x => x.m_targetTamedConfig);
        else if (fields.SetIfChanged(x => x.m_targetTamedConfig, false))
            RecreateZdo = true;

        /// <see cref="Turret.RPC_AddAmmo"/>
        if (!Config.Turrets.LoadFromContainers.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!CheckMinDistance(peers, zdo))
            return false;

        var maxLoaded = fields.GetInt(x => x.m_maxAmmo);
        var currentAmmo = zdo.Vars.GetAmmo();
        var maxAdd = maxLoaded - currentAmmo;
        if (maxAdd < maxLoaded / 2)
            return currentAmmo > 0;

        var allowedAmmoDropPrefabName = currentAmmo > 0 ? zdo.Vars.GetAmmoType() : null;
        ItemDrop.ItemData? allowedAmmo = null;

        var addedAmmo = 0;
        
        foreach (var ammoItem in zdo.PrefabInfo.Turret.Value.Turret.m_allowedAmmo.Select(x => x.m_ammo))
        {
            if (!string.IsNullOrEmpty(allowedAmmoDropPrefabName) && ammoItem.name != allowedAmmoDropPrefabName)
                continue;

            if (!Instance<ContainerProcessor>().ContainersByItemName.TryGetValue(ammoItem.m_itemData.m_shared, out var containers))
                continue;

            List<ItemDrop.ItemData>? removeSlots = null;
            foreach (var containerZdo in containers)
            {
                if (!containerZdo.IsValid() || containerZdo.PrefabInfo.Container is null)
                {
                    containers.Remove(containerZdo);
                    continue;
                }

                if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > Config.Turrets.LoadFromContainersRange.Value)
                    continue;

                if (containerZdo.Vars.GetInUse() || !CheckMinDistance(peers, containerZdo))
                    continue; // in use or player to close

                var inventory = await containerZdo.GetInventory();
                removeSlots?.Clear();
                var addAmmo = 0;
                var found = false;
                foreach (var slot in inventory.Items.Where(x => new ItemKey(x) == ammoItem.m_itemData).OrderBy(x => x.m_stack))
                {
                    found = found || slot is { m_stack: > 0 };
                    var take = Math.Min(maxAdd, slot.m_stack);
                    if (take is 0)
                        continue;

                    allowedAmmoDropPrefabName = ammoItem.name;
                    allowedAmmo = ammoItem.m_itemData;

                    addAmmo += take;
                    slot.m_stack -= take;
                    if (slot.m_stack is 0)
                        (removeSlots ??= new()).Add(slot);

                    maxAdd -= take;
                    if (maxAdd is 0)
                        break;
                }

                if (addAmmo is 0)
                {
                    if (!found)
                    {
                        containers.Remove(containerZdo);
                        if (containers is { Count: 0 })
                            Instance<ContainerProcessor>().ContainersByItemName.TryRemove(ammoItem.m_itemData.m_shared, out _);
                    }
                    continue;
                }

                if (removeSlots is { Count: > 0 })
                {
                    foreach (var remove in removeSlots)
                        inventory.Items.Remove(remove);

                    if (inventory.Items is { Count: 0 })
                    {
                        containers.Remove(containerZdo);
                        if (containers is { Count: 0 })
                            Instance<ContainerProcessor>().ContainersByItemName.TryRemove(ammoItem.m_itemData.m_shared, out _);
                    }
                }

                currentAmmo += addAmmo;
                zdo.Vars.SetAmmo(currentAmmo);
                zdo.Vars.SetAmmoType(allowedAmmoDropPrefabName!);

                inventory.Save();

                addedAmmo += addAmmo;

                if (maxAdd is 0)
                    break;
            }
        }

        if (addedAmmo is not 0)
            RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Turret.Value.Piece.m_name}: $msg_added {allowedAmmo!.m_shared.m_name} {addedAmmo}x");
        //else
        //    RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, "$msg_noturretammo");

        return currentAmmo > 0;
    }
}
