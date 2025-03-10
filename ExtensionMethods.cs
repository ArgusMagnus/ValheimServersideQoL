using UnityEngine;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static void Update(this Inventory inventory, ZDO zdo)
    {
        var inventoryData = zdo.GetString(ZDOVars.s_items);
        if (string.IsNullOrEmpty(inventoryData))
            inventory.GetAllItems().Clear();
        else
            inventory.Load(new(inventoryData));
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
