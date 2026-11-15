using ServersideQoL.Processors;
using UnityEngine;
using static ItemSets;

namespace ServersideQoL.Recycler;

[Processor(Id)]
[RunBefore<IncineratorRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorRegistryProcessor.PrefabInfo>
{
  public const string Id = "1076579e-2a9c-45db-a85c-949f89ea266c";

  readonly Dictionary<ItemDrop, ItemDrop.ItemData> _ingredients = [];

  protected override void Initialize()
  {
    _ingredients.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, IncineratorRegistryProcessor.PrefabInfo prefabInfo)
  {
    _ingredients.Clear();
    foreach (var item in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
    {
      if (ObjectDB.instance.GetRecipe(item) is not { m_requireOnlyOneIngredient: false } recipe)
        continue;

      var found = false;
      for (int quality = 1; quality <= item.m_quality; quality++)
      {
        for (int i = 0; i < recipe.m_resources.Length; i++)
        {
          if (recipe.m_resources[i] is not { m_resItem: not null } res)
            continue;
          if (res.m_upgraderResource != quality > item.m_shared.m_maxQuality)
            continue;
          if (!_ingredients.TryGetValue(res.m_resItem, out var ingredient))
            _ingredients.Add(res.m_resItem, ingredient = res.m_resItem.m_itemData.Clone());
          ingredient.m_stack += res.GetAmount(quality);
          found = true;
        }
      }

      if (found)
        Instance<IncineratorRegistryProcessor>().ReserveItem(item);
    }

    var ratio = Config.Instance.ReturnPercentage.Value * 0.01f;
    foreach (var item in _ingredients.Values)
    {
      var avgStack = item.m_stack * ratio;
      item.m_stack = Mathf.FloorToInt(avgStack);
      if (avgStack > item.m_stack && UnityEngine.Random.value <= (avgStack - item.m_stack))
        item.m_stack++;
      Instance<IncineratorRegistryProcessor>().AddConversionResult(item);
    }

    return default;
  }
}
