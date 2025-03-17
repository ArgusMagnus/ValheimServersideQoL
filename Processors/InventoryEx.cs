namespace Valheim.ServersideQoL.Processors;

record InventoryEx(Inventory Inventory)
{
    public uint DataRevision { get; set; }

    public void Update(ExtendedZDO zdo)
    {
        if (DataRevision == zdo.DataRevision)
            return;
        var inventoryData = zdo.GetString(ZDOVars.s_items);
        if (string.IsNullOrEmpty(inventoryData))
            Inventory.GetAllItems().Clear();
        else
            Inventory.Load(new(inventoryData));
        DataRevision = zdo.DataRevision;
    }

    public void Save(ExtendedZDO zdo)
    {
        var pkg = new ZPackage();
        Inventory.Save(pkg);
        var dataRevision = zdo.DataRevision;
        zdo.Set(ZDOVars.s_items, pkg.GetBase64());
        if (dataRevision == zdo.DataRevision)
            return; // no change

        // moving ZDO are constantly updated, so we need to get ahead for our changes to stick.
        // Not sure about the increment value though...
        if (zdo.PrefabInfo.ZSyncTransform is not null)
            zdo.DataRevision += 120;
        
        DataRevision = zdo.DataRevision;
    }
}
