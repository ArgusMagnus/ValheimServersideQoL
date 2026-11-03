using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.Utilities;

public readonly ref struct Location : IDisposable
{
  public ZoneSystem.ZoneLocation ZoneLocation { get; }
  readonly Vector3 _pos;
  readonly Quaternion _rot;
  public bool IsValid => ZoneLocation is { m_prefab.IsValid: true };
  public GameObject? Prefab { get; }

  public Location(ZoneSystem.ZoneLocation loc)
  {
    ZoneLocation = loc;
    if (!ZoneLocation.m_prefab.IsValid)
    {
      ServersideQoLPlugin.Logger.LogWarning($"Tried to load invalid location asset: {ZoneLocation.m_prefabName} / {ZoneLocation.m_prefab.m_assetID}");
      return;
    }

    if (!ZoneLocation.m_prefab.IsLoaded)
    {
      if (!ZoneLocation.m_prefab.IsLoading)
        ZoneLocation.m_prefab.LoadAsync();
      return;
    }

    Prefab = ZoneLocation.m_prefab.Asset;
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

  public RandomSpawnEnumerable GetRandomSpawns(ServersideQoLZDO zdo)
    => new(in this, zdo);

  public void GetRandomSpawns(ServersideQoLZDO zdo, ICollection<RandomSpawn> collection)
  {
    foreach (var rs in GetRandomSpawns(zdo))
      collection.Add(rs);
  }

  public ComponentEnumerable<T> GetComponents<T>(ServersideQoLZDO zdo)
    where T : MonoBehaviour
    => new(in this, zdo);

  public ref struct RandomSpawnEnumerator : IEnumerator<RandomSpawn>
  {
    readonly ServersideQoLZDO _zdo;
    readonly GameObject? _prefab;
    RandomSpawn? _current;
    public readonly RandomSpawn Current => _current!;
    readonly object IEnumerator.Current => _current!;

    RandomSpawn[]? _randomSpawns;
    UnityEngine.Random.State _randomState;
    global::Location? _loc;
    int _idx = -1;

    public RandomSpawnEnumerator(ref readonly Location location, ServersideQoLZDO zdo)
      : this(location.Prefab, zdo) { }

    internal RandomSpawnEnumerator(GameObject? location, ServersideQoLZDO zdo)
    {
      _zdo = zdo.AssertAndReturn<LocationProxy>();
      _prefab = location;
    }

    [MemberNotNullWhen(true, nameof(_current))]
    public bool MoveNext()
    {
      if (_prefab is null)
        return false;

      _randomSpawns ??= Utils.GetEnabledComponentsInChildren<RandomSpawn>(_prefab);
      if (_idx < 0)
      {
        _randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(_zdo.Vars.GetSeed());
      }

      _current = null;
      while (_current is null && ++_idx < _randomSpawns.Length)
      {
        var rs = _randomSpawns[_idx];
        var pos = rs.gameObject.transform.position;
        pos = _zdo.ZDO.GetPosition() + _zdo.ZDO.GetRotation() * pos;
        rs.Prepare();
        rs.Randomize(pos, _loc ??= _prefab.GetComponent<global::Location>());
        if (rs.gameObject.activeSelf)
          _current = rs;
        rs.Reset();
        rs.GetComponent<ZNetView>()?.gameObject.SetActive(true);
      }

      if (_current is not null)
        return true;

      UnityEngine.Random.state = _randomState;
      return false;
    }

    public void Dispose()
    {
      if (_idx < 0)
        return;
      UnityEngine.Random.state = _randomState;
      _idx = -1;
      _current = null;
    }

    public void Reset() => Dispose();
  }

  public readonly ref struct RandomSpawnEnumerable(scoped ref readonly Location location, ServersideQoLZDO zdo) //: IEnumerable<RandomSpawn>
  {
    readonly GameObject? _prefab = location.Prefab;
    public RandomSpawnEnumerator GetEnumerator() => new(_prefab, zdo);
    //IEnumerator<RandomSpawn> IEnumerable<RandomSpawn>.GetEnumerator() => GetEnumerator();
    //IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo<TCollection>(TCollection collection)
      where TCollection : ICollection<RandomSpawn>
    {
      foreach (var randomSpawn in this)
        collection.Add(randomSpawn);
    }
  }

  public ref struct ComponentEnumerator<T> : IEnumerator<T>
    where T : MonoBehaviour
  {
    readonly ServersideQoLZDO _zdo;
    readonly GameObject? _prefab;
    T? _current;
    public readonly T Current => _current!;
    readonly object IEnumerator.Current => _current!;

    T[]? _components;
    HashSet<RandomSpawn>? _randomSpawns;
    int _idx = -1;

    public ComponentEnumerator(ref readonly Location location, ServersideQoLZDO zdo)
      : this(location.Prefab, zdo) { }

    internal ComponentEnumerator(GameObject? location, ServersideQoLZDO zdo)
    {
      _zdo = zdo.AssertAndReturn<LocationProxy>();
      _prefab = location;
    }

    [MemberNotNullWhen(true, nameof(_current))]
    public bool MoveNext()
    {
      if (_prefab is null)
        return false;

      _components ??= Utils.GetEnabledComponentsInChildren<T>(_prefab);
      _current = null;
      while (_current is null && ++_idx < _components.Length)
      {
        if (_components[_idx] is not { } component)
          continue;

        if (component.GetComponent<RandomSpawn>() is { } randomSpawn)
        {
          if (_randomSpawns is null)
          {
            _randomSpawns = [];
            using var enumerator = new RandomSpawnEnumerator(_prefab, _zdo);
            while (enumerator.MoveNext())
              _randomSpawns.Add(enumerator.Current);
          }
          if (!_randomSpawns.Contains(randomSpawn))
            continue;
        }

        _current = component;
      }

      return _current is not null;
    }

    readonly void IDisposable.Dispose() { }

    public void Reset()
    {
      _idx = -1;
      _current = null;
    }
  }

  public readonly ref struct ComponentEnumerable<T>(scoped ref readonly Location location, ServersideQoLZDO zdo) //: IEnumerable<T>
    where T : MonoBehaviour
  {
    readonly GameObject? _prefab = location.Prefab;
    public ComponentEnumerator<T> GetEnumerator() => new(_prefab, zdo);
    //IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    //IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void CopyTo<TCollection>(TCollection collection)
      where TCollection : ICollection<T>
    {
      foreach (var component in this)
        collection.Add(component);
    }
  }
}
