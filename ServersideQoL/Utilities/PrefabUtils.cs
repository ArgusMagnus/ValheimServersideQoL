using UnityEngine;

namespace ServersideQoL.Utilities;

public static class PrefabUtils
{
  static readonly Dictionary<GameObject, Bounds?> __boundBoxCorners = [];

  public static Bounds? GetBoundingBox(this GameObject gameObject)
  {
    if (!__boundBoxCorners.TryGetValue(gameObject, out var bounds))
    {
      var root = gameObject.transform;
      var (minX, maxX, minY, maxY, minZ, maxZ) = (float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity, float.PositiveInfinity, float.NegativeInfinity);
      foreach (var meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(true))
      {
        if (meshFilter.sharedMesh is null)
          continue;

        var b = meshFilter.sharedMesh.bounds;
        for (var i = 0; i < 8; i++)
        {
          var corner = b.center + Vector3.Scale(b.extents, new((i & 1) is 0 ? -1 : 1, (i & 2) is 0 ? -1 : 1, (i & 4) is 0 ? -1 : 1));
          corner = root.InverseTransformPoint(meshFilter.transform.TransformPoint(corner));
          minX = Mathf.Min(minX, corner.x);
          maxX = Mathf.Max(maxX, corner.x);
          minY = Mathf.Min(minY, corner.y);
          maxY = Mathf.Max(maxY, corner.y);
          minZ = Mathf.Min(minZ, corner.z);
          maxZ = Mathf.Max(maxZ, corner.z);
        }
      }

      if (!float.IsInfinity(minX))
      {
        bounds = new(
          center: 0.5f * new Vector3(minX + maxX, minY + maxY, minZ + maxZ),
          size: new(maxX - minX, maxY - minY, maxZ - minZ));
      }
      __boundBoxCorners.Add(gameObject, bounds);
    }
    return bounds;
  }
}
