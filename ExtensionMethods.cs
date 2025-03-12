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

    public static void Save(this InventoryEx inventory, ZDO zdo)
    {
        var pkg = new ZPackage();
        inventory.Inventory.Save(pkg);
        zdo.Set(ZDOVars.s_items, pkg.GetBase64());
        inventory.DataRevision = zdo.DataRevision;
    }

    public static ZDOComponentFieldAccessor<TComponent> Fields<TComponent>(this ZDO zdo, TComponent? component = default)
        where TComponent : MonoBehaviour
        => new(zdo, component);

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

    public static void ClaimOwnership(this ZDO zdo) => zdo.SetOwner(ZDOMan.GetSessionID());
    public static void ClaimOwnershipInternal(this ZDO zdo) => zdo.SetOwnerInternal(ZDOMan.GetSessionID());
}