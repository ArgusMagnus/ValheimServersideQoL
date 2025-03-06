using System.Linq.Expressions;

namespace Valheim.ServersideQoL;

static class PrivateAccessor
{
    static readonly Action<ItemDrop.ItemData, ZDO> __loadFromZDO = Expression.Lambda<Action<ItemDrop.ItemData, ZDO>>(
        Expression.Call(
            typeof(ItemDrop).GetMethod("LoadFromZDO", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static),
            Expression.Parameter(typeof(ItemDrop.ItemData)) is var par1 ? par1 : throw new Exception(),
            Expression.Parameter(typeof(ZDO)) is var par2 ? par2 : throw new Exception()),
        par1, par2).Compile();

    /// <summary>
    /// Calls <see cref="ItemDrop.LoadFromZDO(ItemDrop.ItemData, ZDO)"/>
    /// </summary>
    /// <param name="itemData"></param>
    /// <param name="zdo"></param>
    public static void LoadFromZDO(ItemDrop.ItemData itemData, ZDO zdo) => __loadFromZDO(itemData, zdo);
}
