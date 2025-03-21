using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class TurretProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo is not { Turret: not null, Piece: not null, PieceTable: not null })
            return false;

        var fields = zdo.Fields<Turret>();
        if (fields.GetBool(x => x.m_targetPlayers) != !Config.Turrets.DontTargetPlayers.Value)
        {
            fields.Set(x => x.m_targetPlayers, !Config.Turrets.DontTargetPlayers.Value);
            recreate = true;
        }
        if (fields.GetBool(x => x.m_targetTamed) != !Config.Turrets.DontTargetTames.Value)
        {
            fields.Set(x => x.m_targetTamed, !Config.Turrets.DontTargetTames.Value);
            recreate = true;
        }
        if (fields.GetBool(x => x.m_targetTamedConfig) != !Config.Turrets.DontTargetTames.Value)
        {
            fields.Set(x => x.m_targetTamedConfig, !Config.Turrets.DontTargetTames.Value);
            recreate = true;
        }

        /// <see cref="Turret.RPC_AddAmmo"/>
        if (!Config.Turrets.LoadFromContainers.Value)
            return true;

        if (!CheckMinDistance(peers, zdo))
            return false;

        var maxLoaded = fields.GetInt(x => x.m_maxAmmo);
        var currentAmmo = zdo.GetInt(ZDOVars.s_ammo);
        var maxAdd = maxLoaded - currentAmmo;
        if (maxAdd < maxLoaded / 2)
            return false;

        var allowedAmmoDropPrefabName = currentAmmo > 0 ? zdo.GetString(ZDOVars.s_ammoType) : null;
        ItemDrop.ItemData? allowedAmmo = null;

        var addedAmmo = 0;
        
        foreach (var ammoItem in zdo.PrefabInfo.Turret.m_allowedAmmo.Select(x => x.m_ammo))
        {
            if (!string.IsNullOrEmpty(allowedAmmoDropPrefabName) && ammoItem.name != allowedAmmoDropPrefabName)
                continue;

            if (!SharedProcessorState.ContainersByItemName.TryGetValue(ammoItem.m_itemData.m_shared, out var containers))
                continue;

            List<ItemDrop.ItemData>? removeSlots = null;
            foreach (var containerZdo in containers)
            {
                if (!containerZdo.IsValid() || containerZdo.PrefabInfo is not { Container: not null, Piece: not null, PieceTable: not null })
                {
                    containers.Remove(containerZdo);
                    continue;
                }

                if (Utils.DistanceXZ(zdo.GetPosition(), containerZdo.GetPosition()) > Config.Turrets.LoadFromContainersRange.Value)
                    continue;

                if (containerZdo.GetBool(ZDOVars.s_inUse) || !CheckMinDistance(peers, containerZdo))
                    continue; // in use or player to close

                removeSlots?.Clear();
                var addAmmo = 0;
                foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == ammoItem.m_itemData).OrderBy(x => x.m_stack))
                {
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
                    containers.Remove(containerZdo);
                    if (containers is { Count: 0 })
                        SharedProcessorState.ContainersByItemName.TryRemove(ammoItem.m_itemData.m_shared, out _);
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
                            SharedProcessorState.ContainersByItemName.TryRemove(ammoItem.m_itemData.m_shared, out _);
                    }
                }

                currentAmmo += addAmmo;
                zdo.Set(ZDOVars.s_ammo, currentAmmo);
                zdo.Set(ZDOVars.s_ammoType, allowedAmmoDropPrefabName);

                containerZdo.Inventory.Save();

                addedAmmo += addAmmo;

                if (maxAdd is 0)
                    break;
            }
        }

        if (addedAmmo is not 0)
            RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Piece.m_name}: $msg_added {allowedAmmo!.m_shared.m_name} {addedAmmo}x");
        //else
        //    RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, "$msg_noturretammo");

        return false;
    }
}
