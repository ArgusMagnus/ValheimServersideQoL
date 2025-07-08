using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TurretProcessor : Processor
{
    readonly List<ExtendedZDO> _turrets = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _turrets.Clear();
        Instance<ContainerProcessor>().ContainerChanged -= OnContainerChanged;
        Instance<ContainerProcessor>().ContainerChanged += OnContainerChanged;
    }

    void OnContainerChanged(ExtendedZDO containerZdo)
    {
        if (containerZdo.Inventory.Items.Count is 0)
            return;

        foreach (var zdo in _turrets)
        {
            if (Vector3.Distance(zdo.GetPosition(), containerZdo.GetPosition()) <= Config.Smelters.FeedFromContainersRange.Value && zdo.Vars.GetAmmo() is 0)
                zdo.ResetProcessorDataRevision(this);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.Turret is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var fields = zdo.Fields<Turret>();
        if (!Config.Turrets.DontTargetPlayers.Value)
            fields.Reset(static x => x.m_targetPlayers);
        else if (fields.SetIfChanged(static x => x.m_targetPlayers, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(static x => x.m_targetTamed);
        else if (fields.SetIfChanged(static x => x.m_targetTamed, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(static x => x.m_targetTamedConfig);
        else if (fields.SetIfChanged(static x => x.m_targetTamedConfig, false))
            RecreateZdo = true;

        /// <see cref="Turret.RPC_AddAmmo"/>
        if (!Config.Turrets.LoadFromContainers.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!CheckMinDistance(peers, zdo))
            return false;

        var maxLoaded = fields.GetInt(static x => x.m_maxAmmo);
        var currentAmmo = zdo.Vars.GetAmmo();
        var maxAdd = maxLoaded - currentAmmo;
        if (maxAdd < maxLoaded / 2)
            return currentAmmo > 0;

        var allowedAmmoDropPrefabName = currentAmmo > 0 ? zdo.Vars.GetAmmoType() : null;
        ItemDrop.ItemData? allowedAmmo = null;

        var addedAmmo = 0;
        
        foreach (var ammoItem in zdo.PrefabInfo.Turret.Value.Turret.m_allowedAmmo.Select(static x => x.m_ammo))
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

                if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                    continue; // in use or player to close

                removeSlots?.Clear();
                var addAmmo = 0;
                var found = false;
                var requestOwn = false;
                foreach (var slot in containerZdo.Inventory!.Items.Where(x => new ItemKey(x) == ammoItem.m_itemData).OrderBy(static x => x.m_stack))
                {
                    found = found || slot is { m_stack: > 0 };
                    var take = Math.Min(maxAdd, slot.m_stack);
                    if (take is 0)
                        continue;
                    else if (!containerZdo.IsOwnerOrUnassigned())
                    {
                        requestOwn = true;
                        break;
                    }

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

                if (requestOwn)
                {
                    Instance<ContainerProcessor>().RequestOwnership(containerZdo, 0);
                    continue;
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
                        containerZdo.Inventory.Items.Remove(remove);

                    if (containerZdo.Inventory.Items is { Count: 0 })
                    {
                        containers.Remove(containerZdo);
                        if (containers is { Count: 0 })
                            Instance<ContainerProcessor>().ContainersByItemName.TryRemove(ammoItem.m_itemData.m_shared, out _);
                    }
                }

                currentAmmo += addAmmo;
                zdo.Vars.SetAmmo(currentAmmo);
                zdo.Vars.SetAmmoType(allowedAmmoDropPrefabName!);

                containerZdo.Inventory.Save();

                addedAmmo += addAmmo;

                if (maxAdd is 0)
                    break;
            }
        }

        if (addedAmmo is not 0)
            ShowMessage(peers, zdo, $"{zdo.PrefabInfo.Turret.Value.Piece.m_name}: $msg_added {allowedAmmo!.m_shared.m_name} {addedAmmo}x", Config.Turrets.AmmoAddedMessageType.Value);
        else
            ShowMessage(peers, zdo, "<color=red>$msg_noturretammo", Config.Turrets.NoAmmoMessageType.Value);

        if (!_turrets.Contains(zdo))
        {
            _turrets.Add(zdo);
            zdo.Destroyed += x => _turrets.Remove(x);
        }

        return true;
        //return currentAmmo > 0;
    }
}
