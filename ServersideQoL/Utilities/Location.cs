using UnityEngine;

namespace ServersideQoL.Utilities;

public readonly struct Location : IDisposable
{
  readonly ZoneSystem.ZoneLocation? _loc;
  readonly Vector3 _pos;
  readonly Quaternion _rot;
  public bool IsValid => _loc?.m_prefab.IsValid ?? false;
  public GameObject? Prefab { get; }

  public Location(ZoneSystem.ZoneLocation loc)
  {
    _loc = loc;
    if (!_loc.m_prefab.IsValid)
    {
      ServersideQoLPlugin.Logger.LogWarning($"Tried to load invalid location asset: {_loc.m_prefabName} / {_loc.m_prefab.m_assetID}");
      return;
    }

    if (!_loc.m_prefab.IsLoaded)
    {
      if (!_loc.m_prefab.IsLoading)
        _loc.m_prefab.LoadAsync();
      return;
    }

    Prefab = _loc.m_prefab.Asset;
    _pos = Prefab.gameObject.transform.position;
    _rot = Prefab.gameObject.transform.rotation;
    Prefab.gameObject.transform.position = Vector3.zero;
    Prefab.gameObject.transform.rotation = Quaternion.identity;
  }

  public void Dispose()
  {
    Prefab?.gameObject.transform.position = _pos;
    Prefab?.gameObject.transform.rotation = _rot;
  }
}
