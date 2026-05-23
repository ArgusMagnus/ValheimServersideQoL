namespace Valheim.ServersideQoL.Processors;

sealed class TurretProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("4b69158e-1790-40be-8dd1-5d1d57197bba");

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

        var feedRangeSqr = containerZdo.Inventory.FeedRange ?? Config.Turrets.LoadFromContainersRange.Value;
        feedRangeSqr *= feedRangeSqr;
        if (feedRangeSqr is 0f)
            return;

        foreach (var zdo in _turrets)
        {
            if (Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) <= feedRangeSqr && zdo.Vars.GetAmmo() is 0)
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
            fields.Reset(static () => x => x.m_targetPlayers);
        else if (fields.UpdateValue(static () => x => x.m_targetPlayers, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(static () => x => x.m_targetTamed);
        else if (fields.UpdateValue(static () => x => x.m_targetTamed, false))
            RecreateZdo = true;

        if (!Config.Turrets.DontTargetTames.Value)
            fields.Reset(static () => x => x.m_targetTamedConfig);
        else if (fields.UpdateValue(static () => x => x.m_targetTamedConfig, false))
            RecreateZdo = true;

#if DEBUG
        ////var ammoType = "StaffFireball";
        //var ammoType = "StaffIceShards"; // sometimes hits turret
        ////var ammoType = "StaffClusterbomb";
        ////var ammoType = "StaffGreenRoots"; // crashes client
        ////var ammoType = "StaffLightning"; // always hits turret
        ////var ammoType = "BombBlob_Poison";
        ////var ammoType = "DvergerStaffIce";
        //if (ZNetScene.instance.GetPrefab(ammoType)?.GetComponent<ItemDrop>() is not { } item)
        //{
        //    Logger.DevLog($"Item {ammoType} not found in ZNetScene");
        //    return false;
        //}

        //var attack = item.m_itemData.m_shared.m_attack;
        //if (attack.m_attackProjectile is not { } projectile || attack.m_projectileVel <= 0)
        //{
        //    Logger.DevLog($"Item {item.name} has no valid attack projectile");
        //    return false;
        //}

        //var eitr = attack.m_attackEitr;
        //if (eitr is 0)
        //    eitr = attack.m_reloadEitrDrain;
        //if (eitr is 0)
        //{
        //    eitr = 30f;
        //    //Logger.DevLog($"Item {item.name} has no attack eitr");
        //    //return false;
        //}

        //if (fields.UpdateValue(static () => x => x.m_returnAmmoOnDestroy, false))
        //    RecreateZdo = true;
        //if (fields.UpdateValue(static () => x => x.m_maxAmmo, 0))
        //    RecreateZdo = true;
        //if (fields.UpdateValue(static () => x => x.m_defaultAmmo, item))
        //    RecreateZdo = true;

        //var cooldown = zdo.PrefabInfo.Turret.Value.Turret.m_attackCooldown * eitr / 30f;
        //if (fields.UpdateValue(static () => x => x.m_attackCooldown, cooldown))
        //    RecreateZdo = true;

        //return false;
#endif

        /// <see cref="Turret.RPC_AddAmmo"/>
        if (!Config.Turrets.LoadFromContainers.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!CheckMinDistance(peers, zdo))
            return false;

        var maxLoaded = fields.GetInt(static () => x => x.m_maxAmmo);
        var currentAmmo = zdo.Vars.GetAmmo();
        var maxAdd = maxLoaded - currentAmmo;
        if (maxAdd < maxLoaded / 2)
            return currentAmmo > 0;

        var allowedAmmoDropPrefabName = currentAmmo > 0 ? zdo.Vars.GetAmmoType() : null;
        ItemDrop.ItemData? allowedAmmo = null;

        var addedAmmo = 0;
        List<ExtendedZDO>? toRemove = null;
        List<ItemDrop.ItemData>? removeSlots = null;

        foreach (var ammoItem in zdo.PrefabInfo.Turret.Value.Turret.m_allowedAmmo.Select(static x => x.m_ammo))
        {
            if (!string.IsNullOrEmpty(allowedAmmoDropPrefabName) && ammoItem.name != allowedAmmoDropPrefabName)
                continue;

            foreach (var containers in Instance<ContainerProcessor>().ContainersByItemName.EnumerateAdjacent((zdo.GetPosition(), ammoItem.m_itemData.m_shared)))
            {
                toRemove?.Clear();
                foreach (var containerZdo in containers)
                {
                    if (containerZdo.PrefabInfo.Container is null)
                    {
                        (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    var feedRangeSqr = containerZdo.Inventory.FeedRange ?? Config.Turrets.LoadFromContainersRange.Value;
                    feedRangeSqr *= feedRangeSqr;
                    if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.GetPosition(), containerZdo.GetPosition()) > feedRangeSqr)
                        continue;

                    if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                        continue; // in use or player to close

                    removeSlots?.Clear();
                    var addAmmo = 0;
                    var found = false;
                    var requestOwn = false;
                    foreach (var slot in containerZdo.Inventory.Items.Where(x => new ItemDataKey(x) == ammoItem.m_itemData).OrderBy(static x => x.m_stack))
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
                            (toRemove ??= []).Add(containerZdo);
                        continue;
                    }

                    if (removeSlots is { Count: > 0 })
                    {
                        foreach (var remove in removeSlots)
                            containerZdo.Inventory.Items.Remove(remove);

                        if (containerZdo.Inventory.Items is { Count: 0 })
                            (toRemove ??= []).Add(containerZdo);
                    }

                    currentAmmo += addAmmo;
                    zdo.Vars.SetAmmo(currentAmmo);
                    zdo.Vars.SetAmmoType(allowedAmmoDropPrefabName!);

                    containerZdo.Inventory.Save();

                    addedAmmo += addAmmo;

                    if (maxAdd is 0)
                        break;
                }

                if (toRemove is not null)
                {
                    foreach (var containerZdo in toRemove)
                        containers.Remove(containerZdo);
                }

                if (maxAdd is 0)
                    break;
            }

            if (maxAdd is 0)
                break;
        }

        if (addedAmmo is not 0)
            ShowMessage(peers, zdo, Config.Localization.Turrets.FormatAmmoAdded(zdo.PrefabInfo.Turret.Value.Piece.m_name, allowedAmmo!.m_shared.m_name, addedAmmo), Config.Turrets.AmmoAddedMessageType.Value);
        else if (currentAmmo is 0)
            ShowMessage(peers, zdo, Config.Localization.Turrets.NoAmmoFound, Config.Turrets.NoAmmoMessageType.Value, DamageText.TextType.Bonus);

        if (!_turrets.Contains(zdo))
        {
            _turrets.Add(zdo);
            zdo.Destroyed += x => _turrets.Remove(x);
        }

        return true;
        //return currentAmmo > 0;
    }
}
