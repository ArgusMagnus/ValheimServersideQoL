using UnityEngine;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static void Update(this InventoryEx inventory, ZDO zdo)
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

    public static ZDOComponentFieldAccessor<TComponent> Fields<TComponent>(this ZDO zdo)
        where TComponent : Component
        => new(zdo);

    public static ZDO Recreate(this ZDO zdo)
    {
        var prefab = zdo.GetPrefab();
        var pos = zdo.GetPosition();
        var owner = zdo.GetOwner();
        var pkg = new ZPackage();
        zdo.Serialize(pkg);

        zdo.SetOwnerInternal(ZDOMan.GetSessionID());
        ZDOMan.instance.DestroyZDO(zdo);
        zdo = ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.Deserialize(new(pkg.GetArray()));
        zdo.SetOwnerInternal(owner);
        return zdo;
    }
}
