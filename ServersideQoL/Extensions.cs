using UnityEngine;

namespace ServersideQoL;

public static class Extensions
{
  public static float GetHeight(this Heightmap hmap, Vector3 pos)
  {
    hmap.WorldToVertex(pos, out var x, out var y);
    return hmap.GetHeight(x, y);
  }

  extension(ZoneSystem zoneSystem)
  {
    /// <see cref="ZNetScene.InActiveArea(Vector2s, Vector2s)"/>
    public int ActiveArea => zoneSystem.m_activeArea - 1;
    public int LoadedArea => zoneSystem.m_activeArea;
  }
}
