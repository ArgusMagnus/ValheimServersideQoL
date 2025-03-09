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
}
