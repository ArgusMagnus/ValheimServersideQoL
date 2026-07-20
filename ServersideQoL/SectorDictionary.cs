using ServersideQoL.ZDOExtender;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ServersideQoL;

public sealed class SectorDictionary<TValue>(float sectorWidth) : IDictionary<Vector3, TValue>, IReadOnlyDictionary<Vector3, TValue>
{
    public readonly record struct Key(int X, int Z);
    public float SectorWidth { get; } = sectorWidth;
    readonly float _scale = sectorWidth > 0 ? 1f / sectorWidth : throw new ArgumentOutOfRangeException(nameof(sectorWidth));
    readonly Dictionary<Key, TValue> _sections = [];

    //public void Reset(float newSectorWidth)
    //{
    //    if (newSectorWidth <= 0)
    //        throw new ArgumentOutOfRangeException(nameof(newSectorWidth));
    //    SectorWidth = newSectorWidth;
    //    _scale = 1f / newSectorWidth;
    //    _sections.Clear();
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Key GetKey(Vector3 pos)
        => new(Mathf.RoundToInt(pos.x * _scale), Mathf.RoundToInt(pos.z * _scale));

    ICollection<Vector3> IDictionary<Vector3, TValue>.Keys => throw new NotSupportedException();
    IEnumerable<Vector3> IReadOnlyDictionary<Vector3, TValue>.Keys => throw new NotSupportedException();

    public Dictionary<Key, TValue>.ValueCollection Values => _sections.Values;
    ICollection<TValue> IDictionary<Vector3, TValue>.Values => _sections.Values;
    IEnumerable<TValue> IReadOnlyDictionary<Vector3, TValue>.Values => _sections.Values;

    public int Count => _sections.Count;
    bool ICollection<KeyValuePair<Vector3, TValue>>.IsReadOnly => false;

    public TValue this[Vector3 key] { get => _sections[GetKey(key)]; set => _sections[GetKey(key)] = value; }
    public void Add(Vector3 key, TValue value) => _sections.Add(GetKey(key), value);
    bool IDictionary<Vector3, TValue>.ContainsKey(Vector3 key) => _sections.ContainsKey(GetKey(key));
    bool IReadOnlyDictionary<Vector3, TValue>.ContainsKey(Vector3 key) => _sections.ContainsKey(GetKey(key));
    bool IDictionary<Vector3, TValue>.Remove(Vector3 key) => _sections.Remove(GetKey(key));
    public bool TryGetValue(Vector3 key, out TValue value) => _sections.TryGetValue(GetKey(key), out value);
    public void Clear() => _sections.Clear();

    public bool TryGetValue(Vector3 key, bool includeAdjacent, out TValue value)
    {
        var (x, y) = GetKey(key);
        if (_sections.TryGetValue(new(x, y), out value))
            return true;
        if (!includeAdjacent)
            return false;
        if (_sections.TryGetValue(new(x - 1, y - 1), out value))
            return true;
        if (_sections.TryGetValue(new(x, y - 1), out value))
            return true;
        if (_sections.TryGetValue(new(x + 1, y - 1), out value))
            return true;
        if (_sections.TryGetValue(new(x - 1, y), out value))
            return true;
        if (_sections.TryGetValue(new(x + 1, y), out value))
            return true;
        if (_sections.TryGetValue(new(x - 1, y + 1), out value))
            return true;
        if (_sections.TryGetValue(new(x, y + 1), out value))
            return true;
        if (_sections.TryGetValue(new(x + 1, y + 1), out value))
            return true;
        return false;
    }

    void ICollection<KeyValuePair<Vector3, TValue>>.Add(KeyValuePair<Vector3, TValue> item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<Vector3, TValue>>.Contains(KeyValuePair<Vector3, TValue> item)
    {
        throw new NotSupportedException();
    }

    void ICollection<KeyValuePair<Vector3, TValue>>.CopyTo(KeyValuePair<Vector3, TValue>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<Vector3, TValue>>.Remove(KeyValuePair<Vector3, TValue> item)
    {
        throw new NotSupportedException();
    }

    IEnumerator<KeyValuePair<Vector3, TValue>> IEnumerable<KeyValuePair<Vector3, TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    public AdjacentEnumerator EnumerateAdjacent(Vector3 key)
        => new(_sections, GetKey(key));

    public struct AdjacentEnumerator(Dictionary<Key, TValue> dict, Key key) : IEnumerator<TValue>
    {
        readonly int _x = key.X;
        readonly int _z = key.Z;
        int _idx = -1;

        public readonly AdjacentEnumerator GetEnumerator() => this;

        public TValue Current { get; private set; } = default!;
        readonly object IEnumerator.Current => Current!;

        public readonly void Dispose() { }

        public bool MoveNext()
        {
            TValue value;
            while (true)
            {
                switch (++_idx)
                {
                    case 0:
                        if (dict.TryGetValue(new(_x, _z), out value))
                            break;
                        continue;
                    case 1:
                        if (dict.TryGetValue(new(_x - 1, _z - 1), out value))
                            break;
                        continue;
                    case 2:
                        if (dict.TryGetValue(new(_x, _z - 1), out value))
                            break;
                        continue;
                    case 3:
                        if (dict.TryGetValue(new(_x + 1, _z - 1), out value))
                            break;
                        continue;
                    case 4:
                        if (dict.TryGetValue(new(_x - 1, _z), out value))
                            break;
                        continue;
                    case 5:
                        if (dict.TryGetValue(new(_x + 1, _z), out value))
                            break;
                        continue;
                    case 6:
                        if (dict.TryGetValue(new(_x - 1, _z + 1), out value))
                            break;
                        continue;
                    case 7:
                        if (dict.TryGetValue(new(_x, _z + 1), out value))
                            break;
                        continue;
                    case 8:
                        if (dict.TryGetValue(new(_x + 1, _z + 1), out value))
                            break;
                        continue;
                    default:
                        Current = default!;
                        return false;
                }
                Current = value;
                return true;
            }
        }

        public void Reset() => _idx = -1;
    }
}

public sealed class SectorDictionary<TKey, TValue>(float sectorWidth) : IDictionary<(Vector3, TKey), TValue>, IReadOnlyDictionary<(Vector3, TKey), TValue>
        where TKey : notnull
{
    public float SectorWidth { get; } = sectorWidth;
    readonly float _scale = sectorWidth > 0 ? 1f / sectorWidth : throw new ArgumentOutOfRangeException(nameof(sectorWidth));
    readonly Dictionary<(int, int, TKey), TValue> _sections = [];

    //public void Reset(float newSectorWidth)
    //{
    //    if (newSectorWidth <= 0)
    //        throw new ArgumentOutOfRangeException(nameof(newSectorWidth));
    //    SectorWidth = newSectorWidth;
    //    _scale = 1f / newSectorWidth;
    //    _sections.Clear();
    //}

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    (int, int, TKey) GetKey((Vector3 pos, TKey key) key)
        => (Mathf.RoundToInt(key.pos.x * _scale), Mathf.RoundToInt(key.pos.z * _scale), key.key);

    ICollection<(Vector3, TKey)> IDictionary<(Vector3, TKey), TValue>.Keys => throw new NotSupportedException();
    IEnumerable<(Vector3, TKey)> IReadOnlyDictionary<(Vector3, TKey), TValue>.Keys => throw new NotSupportedException();

    public Dictionary<(int, int, TKey), TValue>.ValueCollection Values => _sections.Values;
    ICollection<TValue> IDictionary<(Vector3, TKey), TValue>.Values => _sections.Values;
    IEnumerable<TValue> IReadOnlyDictionary<(Vector3, TKey), TValue>.Values => _sections.Values;

    public int Count => _sections.Count;
    bool ICollection<KeyValuePair<(Vector3, TKey), TValue>>.IsReadOnly => false;

    public TValue this[(Vector3, TKey) key] { get => _sections[GetKey(key)]; set => _sections[GetKey(key)] = value; }
    public void Add((Vector3, TKey) key, TValue value) => _sections.Add(GetKey(key), value);
    bool IDictionary<(Vector3, TKey), TValue>.ContainsKey((Vector3, TKey) key) => _sections.ContainsKey(GetKey(key));
    bool IReadOnlyDictionary<(Vector3, TKey), TValue>.ContainsKey((Vector3, TKey) key) => _sections.ContainsKey(GetKey(key));
    bool IDictionary<(Vector3, TKey), TValue>.Remove((Vector3, TKey) key) => _sections.Remove(GetKey(key));
    public bool TryGetValue((Vector3, TKey) key, out TValue value) => _sections.TryGetValue(GetKey(key), out value);
    public void Clear() => _sections.Clear();

    public bool TryGetValue((Vector3, TKey) key, bool includeAdjacent, out TValue value)
    {
        var (x, y, k) = GetKey(key);
        if (_sections.TryGetValue((x, y, k), out value))
            return true;
        if (!includeAdjacent)
            return false;
        if (_sections.TryGetValue((x - 1, y - 1, k), out value))
            return true;
        if (_sections.TryGetValue((x, y - 1, k), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y - 1, k), out value))
            return true;
        if (_sections.TryGetValue((x - 1, y, k), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y, k), out value))
            return true;
        if (_sections.TryGetValue((x - 1, y + 1, k), out value))
            return true;
        if (_sections.TryGetValue((x, y + 1, k), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y + 1, k), out value))
            return true;
        return false;
    }

    void ICollection<KeyValuePair<(Vector3, TKey), TValue>>.Add(KeyValuePair<(Vector3, TKey), TValue> item)
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<(Vector3, TKey), TValue>>.Contains(KeyValuePair<(Vector3, TKey), TValue> item)
    {
        throw new NotSupportedException();
    }

    void ICollection<KeyValuePair<(Vector3, TKey), TValue>>.CopyTo(KeyValuePair<(Vector3, TKey), TValue>[] array, int arrayIndex)
    {
        throw new NotSupportedException();
    }

    bool ICollection<KeyValuePair<(Vector3, TKey), TValue>>.Remove(KeyValuePair<(Vector3, TKey), TValue> item)
    {
        throw new NotSupportedException();
    }

    IEnumerator<KeyValuePair<(Vector3, TKey), TValue>> IEnumerable<KeyValuePair<(Vector3, TKey), TValue>>.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotSupportedException();
    }

    public AdjacentEnumerator EnumerateAdjacent((Vector3, TKey) key)
        => new(_sections, GetKey(key));

    public struct AdjacentEnumerator(Dictionary<(int, int, TKey), TValue> dict, (int x, int y, TKey k) key) : IEnumerator<TValue>
    {
        readonly int _x = key.x;
        readonly int _y = key.y;
        readonly TKey _k = key.k;
        int _idx = -1;

        public readonly AdjacentEnumerator GetEnumerator() => this;

        public TValue Current { get; private set; } = default!;
        readonly object IEnumerator.Current => Current!;

        public readonly void Dispose() { }

        public bool MoveNext()
        {
            TValue value;
            while (true)
            {
                switch (++_idx)
                {
                    case 0:
                        if (dict.TryGetValue((_x, _y, _k), out value))
                            break;
                        continue;
                    case 1:
                        if (dict.TryGetValue((_x - 1, _y - 1, _k), out value))
                            break;
                        continue;
                    case 2:
                        if (dict.TryGetValue((_x, _y - 1, _k), out value))
                            break;
                        continue;
                    case 3:
                        if (dict.TryGetValue((_x + 1, _y - 1, _k), out value))
                            break;
                        continue;
                    case 4:
                        if (dict.TryGetValue((_x - 1, _y, _k), out value))
                            break;
                        continue;
                    case 5:
                        if (dict.TryGetValue((_x + 1, _y, _k), out value))
                            break;
                        continue;
                    case 6:
                        if (dict.TryGetValue((_x - 1, _y + 1, _k), out value))
                            break;
                        continue;
                    case 7:
                        if (dict.TryGetValue((_x, _y + 1, _k), out value))
                            break;
                        continue;
                    case 8:
                        if (dict.TryGetValue((_x + 1, _y + 1, _k), out value))
                            break;
                        continue;
                    default:
                        Current = default!;
                        return false;
                }
                Current = value;
                return true;
            }
        }

        public void Reset() => _idx = -1;
    }
}

public static class SectorDictionary
{
    public static TValue GetOrAdd<TValue>(this SectorDictionary<TValue> @this, Vector3 key)
        where TValue : new()
    {
        if (!@this.TryGetValue(key, out var value))
            @this.Add(key, value = new());
        return value;
    }

    public static bool TryAdd(this SectorDictionary<HashSet<ZDO>> @this, Vector3 key, ZDO zdo, bool autoRemoveOnDestroyed = true)
    {
        var set = @this.GetOrAdd(key);
        if (!set.Add(zdo))
            return false;
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => set.Remove(x);
        return true;
    }

    public static bool TryAdd(this SectorDictionary<HashSet<ZDO>> @this, ZDO zdo, bool autoRemoveOnDestroyed = true)
        => @this.TryAdd(zdo.GetPosition(), zdo, autoRemoveOnDestroyed);

    public static bool TryAdd<TValue>(this SectorDictionary<HashSet<TValue>> @this, Vector3 key, TValue value)
        => @this.GetOrAdd(key).Add(value);

    public static void Add<TCollection, TValue>(this SectorDictionary<TCollection> @this, Vector3 key, TValue value)
        where TCollection : class, ICollection<TValue>, new()
        => @this.GetOrAdd(key).Add(value);

    public static void Add<TCollection>(this SectorDictionary<TCollection> @this, Vector3 key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TCollection : class, ICollection<ZDO>, new()
    {
        var collection = @this.GetOrAdd(key);
        collection.Add(zdo);
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => collection.Remove(x);
    }

    public static void Add<TCollection>(this SectorDictionary<TCollection> @this, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TCollection : class, ICollection<ZDO>, new()
        => @this.Add(zdo.GetPosition(), zdo, autoRemoveOnDestroyed);

    public static bool TryAdd<TCollection>(this SectorDictionary<TCollection> @this, Vector3 key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TCollection : class, ICollection<ZDO>, new()
    {
        var collection = @this.GetOrAdd(key);
        if (collection.Contains(zdo))
            return false;
        collection.Add(zdo);
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => collection.Remove(x);
        return true;
    }

    public static bool TryAdd<TCollection>(this SectorDictionary<TCollection> @this, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TCollection : class, ICollection<ZDO>, new()
        => @this.TryAdd(zdo.GetPosition(), zdo, autoRemoveOnDestroyed);

    public static bool Remove<TCollection, TValue>(this SectorDictionary<TCollection> @this, Vector3 key, TValue value)
        where TCollection : class, ICollection<TValue>
    {
        if (@this.TryGetValue(key, out var collection) && collection.Remove(value))
        {
            if (collection.Count is 0)
                @this.Remove(key, out _);
            return true;
        }
        return false;
    }

    public static bool Remove<TCollection>(this SectorDictionary<TCollection> @this, ZDO zdo)
        where TCollection : class, ICollection<ZDO>
        => @this.Remove(zdo.GetPosition(), zdo);

    public static TValue GetOrAdd<TKey, TValue>(this SectorDictionary<TKey, TValue> @this, (Vector3, TKey) key)
        where TKey : notnull
        where TValue : new()
    {
        if (!@this.TryGetValue(key, out var value))
            @this.Add(key, value = new());
        return value;
    }

    public static bool TryAdd<TKey>(this SectorDictionary<TKey, HashSet<ZDO>> @this, (Vector3, TKey) key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
    {
        var set = @this.GetOrAdd(key);
        if (!set.Add(zdo))
            return false;
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => set.Remove(x);
        return true;
    }

    public static bool TryAdd<TKey>(this SectorDictionary<TKey, HashSet<ZDO>> @this, TKey key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
        => @this.TryAdd((zdo.GetPosition(), key), zdo, autoRemoveOnDestroyed);

    public static bool TryAdd<TKey, TValue>(this SectorDictionary<TKey, HashSet<TValue>> @this, (Vector3, TKey) key, TValue value)
        where TKey : notnull
        => @this.GetOrAdd(key).Add(value);

    public static void Add<TKey, TCollection, TValue>(this SectorDictionary<TKey, TCollection> @this, (Vector3, TKey) key, TValue value)
        where TKey : notnull
        where TCollection : class, ICollection<TValue>, new()
        => @this.GetOrAdd(key).Add(value);

    public static void Add<TKey, TCollection>(this SectorDictionary<TKey, TCollection> @this, (Vector3, TKey) key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
        where TCollection : class, ICollection<ZDO>, new()
    {
        var collection = @this.GetOrAdd(key);
        collection.Add(zdo);
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => collection.Remove(x);
    }

    public static void Add<TKey, TCollection>(this SectorDictionary<TKey, TCollection> @this, TKey key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
        where TCollection : class, ICollection<ZDO>, new()
        => @this.Add((zdo.GetPosition(), key), zdo, autoRemoveOnDestroyed);

    public static bool TryAdd<TKey, TCollection>(this SectorDictionary<TKey, TCollection> @this, (Vector3, TKey) key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
        where TCollection : class, ICollection<ZDO>, new()
    {
        var collection = @this.GetOrAdd(key);
        if (collection.Contains(zdo))
            return false;
        collection.Add(zdo);
        if (autoRemoveOnDestroyed)
            zdo.GetExtension<IExtendedZDO>().Destroyed += x => collection.Remove(x);
        return true;
    }

    public static bool TryAdd<TKey, TCollection>(this SectorDictionary<TKey, TCollection> @this, TKey key, ZDO zdo, bool autoRemoveOnDestroyed = true)
        where TKey : notnull
        where TCollection : class, ICollection<ZDO>, new()
        => @this.TryAdd((zdo.GetPosition(), key), zdo, autoRemoveOnDestroyed);

    public static bool Remove<TKey, TCollection, TValue>(this SectorDictionary<TKey, TCollection> @this, (Vector3, TKey) key, TValue value)
        where TKey : notnull
        where TCollection : class, ICollection<TValue>
    {
        if (@this.TryGetValue(key, out var collection) && collection.Remove(value))
        {
            if (collection.Count is 0)
                @this.Remove(key, out _);
            return true;
        }
        return false;
    }

    public static bool Remove<TKey, TCollection>(this SectorDictionary<TKey, TCollection> @this, TKey key, ZDO zdo)
        where TKey : notnull
        where TCollection : class, ICollection<ZDO>
        => @this.Remove((zdo.GetPosition(), key), zdo);
}