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

  extension(ItemDrop.ItemData itemData)
  {
    public bool IsItem => itemData.m_shared.IsItem;
  }

  extension(ItemDrop itemDrop)
  {
    public bool IsItem => itemDrop.m_itemData.IsItem;
  }
}
