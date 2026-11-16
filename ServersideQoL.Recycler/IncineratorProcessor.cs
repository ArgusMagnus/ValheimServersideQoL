using ServersideQoL.Processors;

namespace ServersideQoL.Recycler;

[Processor(Id)]
[RunBefore<IncineratorRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorRegistryProcessor.PrefabInfo>
{
  public const string Id = "1076579e-2a9c-45db-a85c-949f89ea266c";

  readonly Dictionary<(ItemDrop, int), ItemDrop.ItemData> _ingredients = [];

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, IncineratorRegistryProcessor.PrefabInfo prefabInfo)
  {
    _ingredients.Clear();
    foreach (var item in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
    {
      if (item.GetIngredients() is { } ingredients)
      {
        Instance<IncineratorRegistryProcessor>().ReserveItem(item);
        foreach (var ingredient in ingredients.Enumerate())
        {
          var itemDrop = ingredient.m_dropPrefab.GetComponent<ItemDrop>();
          if (_ingredients.TryGetValue((itemDrop, ingredient.m_quality), out var ingr))
            ingr.m_stack += ingredient.m_stack;
          else
            _ingredients.Add((itemDrop, ingredient.m_quality), ingredient);
        }
      }
      else
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
            if (!_ingredients.TryGetValue((res.m_resItem, res.m_resItem.m_itemData.m_quality), out var ingredient))
            {
              _ingredients.Add((res.m_resItem, res.m_resItem.m_itemData.m_quality), ingredient = res.m_resItem.m_itemData.Clone());
              ingredient.m_stack = 0;
            }
            ingredient.m_stack += res.GetAmount(quality);
            found = true;
          }
        }

        if (found)
          Instance<IncineratorRegistryProcessor>().ReserveItem(item);
      }
    }

    if (_ingredients.Count is 0)
      return default;

    var ratio = Config.Instance.ReturnPercentage.Value * 0.01f;
    foreach (var item in _ingredients.Values)
    {
      var avgStack = item.m_stack * ratio;
      item.m_stack = (int)avgStack;
      if (avgStack > item.m_stack && UnityEngine.Random.value <= (avgStack - item.m_stack))
        item.m_stack++;
      Instance<IncineratorRegistryProcessor>().AddConversionResult(item
#if !DEBUG
        , false
#endif
        );
    }

    return default;
  }
}
