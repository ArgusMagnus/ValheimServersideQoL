using Mono.Cecil.Cil;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL;

public static class Extensions
{
  public static float GetHeight(this Heightmap hmap, Vector3 pos)
  {
    hmap.WorldToVertex(pos, out var x, out var y);
    return hmap.GetHeight(x, y);
  }

  extension(ZNetScene instance)
  {
    public IEnumerable<ZNetView> ZNetViews => instance.m_prefabs
        .Select(static x =>
        {
          if (x.GetComponent<ZNetView>() is not { } zNetView)
          {
            ServersideQoLPlugin.Logger.LogWarning($"GameObject '{x.name}' was added to ZNetScene.instance.m_prefabs by another mod but is missing the {nameof(ZNetView)} component");
            return null!;
          }
          return zNetView;
        })
        .Where(static x => x is not null);
  }

  extension(ItemDrop.ItemData.SharedData sharedData)
  {
    public bool IsItem => sharedData.m_icons is { Length: > 0 };
  }

  const string IngredientDataKey = "SQoL:Ingredients";

  extension(ItemDrop.ItemData itemData)
  {
    public bool IsItem => itemData.m_shared.IsItem;

    public void SaveIngredients(params ReadOnlySpan<ItemDrop.ItemData> items)
    {
      var zpkg = SingletonCache<ZPackage>.Instance;
      zpkg.Clear();
      zpkg.Write((int)Version.c_ItemDataVersion);
      zpkg.WriteNumItems(items.Length);
      foreach (var item in items)
        item.Save(zpkg);
      itemData.m_customData[IngredientDataKey] = Convert.ToBase64String(zpkg.GetCompressed());
    }

    public IReadOnlyList<ItemDrop.ItemData>? GetIngredients()
    {
      if (!itemData.m_customData.TryGetValue(IngredientDataKey, out var str))
        return null;

      var zpkg = SingletonCache<ZPackage>.Instance;
      zpkg.Load(Convert.FromBase64String(str));
      zpkg.Decompress();
      var version = (Version.Item)zpkg.ReadInt();
      var count = zpkg.ReadNumItems();
      var result = new ItemDrop.ItemData[count];
      for (int i = 0; i < count; i++)
      {
        var (prefab, item) = ItemDrop.ItemData.Load(zpkg, version);
        item.m_dropPrefab = ZNetScene.instance.GetPrefab(prefab);
        item.m_shared = item.m_dropPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
        result[i] = item;
      }

      return result;
    }
  }

  extension(ItemDrop itemDrop)
  {
    public bool IsItem => itemDrop.m_itemData.IsItem;
  }

  extension(Instruction instruction)
  {
    public void MakeNoOp()
    {
      instruction.OpCode = OpCodes.Nop;
      instruction.Operand = null;
    }
  }
}
