using UnityEngine;

namespace ServersideQoL;

public static class Extensions
{
  public static float GetHeight(this Heightmap hmap, Vector3 pos)
  {
    hmap.WorldToVertex(pos, out var x, out var y);
    return hmap.GetHeight(x, y);
  }
}
