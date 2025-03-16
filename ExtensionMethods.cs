using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static void Update(this InventoryEx inventory, ExtendedZDO zdo)
    {
        if (inventory.DataRevision == zdo.DataRevision)
            return;
        var inventoryData = zdo.GetString(ZDOVars.s_items);
        if (string.IsNullOrEmpty(inventoryData))
            inventory.Inventory.GetAllItems().Clear();
        else
            inventory.Inventory.Load(new(inventoryData));
        inventory.DataRevision = zdo.DataRevision;
    }

    public static void Save(this InventoryEx inventory, ExtendedZDO zdo)
    {
        var pkg = new ZPackage();
        inventory.Inventory.Save(pkg);
        zdo.Set(ZDOVars.s_items, pkg.GetBase64());
        inventory.DataRevision = zdo.DataRevision;
    }
}