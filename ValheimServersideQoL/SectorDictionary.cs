using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL;

sealed class SectorDictionary<TValue>(float sectorWidth) : IDictionary<Vector3, TValue>, IReadOnlyDictionary<Vector3, TValue>
{
    float _scale = sectorWidth > 0 ? 1f / sectorWidth : throw new ArgumentOutOfRangeException(nameof(sectorWidth));
    readonly Dictionary<(int, int), TValue> _sections = [];

    public void Reset(float newSectorWidth)
    {
        if (newSectorWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(sectorWidth));
        _scale = 1f / newSectorWidth;
        _sections.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    (int, int) GetKey(Vector3 pos)
        => (Mathf.RoundToInt(pos.x * _scale), Mathf.RoundToInt(pos.z * _scale));

    ICollection<Vector3> IDictionary<Vector3, TValue>.Keys => throw new NotSupportedException();
    IEnumerable<Vector3> IReadOnlyDictionary<Vector3, TValue>.Keys => throw new NotSupportedException();

    public Dictionary<(int, int), TValue>.ValueCollection Values => _sections.Values;
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
        if (_sections.TryGetValue((x, y), out value))
            return true;
        if (!includeAdjacent)
            return false;
        if (_sections.TryGetValue((x - 1, y - 1), out value))
            return true;
        if (_sections.TryGetValue((x, y - 1), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y - 1), out value))
            return true;
        if (_sections.TryGetValue((x - 1, y), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y), out value))
            return true;
        if (_sections.TryGetValue((x - 1, y + 1), out value))
            return true;
        if (_sections.TryGetValue((x, y + 1), out value))
            return true;
        if (_sections.TryGetValue((x + 1, y + 1), out value))
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

    public struct AdjacentEnumerator(Dictionary<(int, int), TValue> dict, (int x, int y) key) : IEnumerator<TValue>
    {
        readonly int _x = key.x;
        readonly int _y = key.y;
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
                        if (dict.TryGetValue((_x, _y), out value))
                            break;
                        continue;
                    case 1:
                        if (dict.TryGetValue((_x - 1, _y - 1), out value))
                            break;
                        continue;
                    case 2:
                        if (dict.TryGetValue((_x, _y - 1), out value))
                            break;
                        continue;
                    case 3:
                        if (dict.TryGetValue((_x + 1, _y - 1), out value))
                            break;
                        continue;
                    case 4:
                        if (dict.TryGetValue((_x - 1, _y), out value))
                            break;
                        continue;
                    case 5:
                        if (dict.TryGetValue((_x + 1, _y), out value))
                            break;
                        continue;
                    case 6:
                        if (dict.TryGetValue((_x - 1, _y + 1), out value))
                            break;
                        continue;
                    case 7:
                        if (dict.TryGetValue((_x, _y + 1), out value))
                            break;
                        continue;
                    case 8:
                        if (dict.TryGetValue((_x + 1, _y + 1), out value))
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

static class SectorDictionary
{
    public static TValue GetOrAdd<TValue>(this SectorDictionary<TValue> @this, Vector3 key)
        where TValue : new()
    {
        if (!@this.TryGetValue(key, out var value))
            @this.Add(key, value = new());
        return value;
    }

    public static bool TryAdd(this SectorDictionary<HashSet<ExtendedZDO>> @this, Vector3 key, ExtendedZDO zdo)
    {
        var set = @this.GetOrAdd(key);
        if (!set.Add(zdo))
            return false;
        zdo.Destroyed += x => set.Remove(x);
        return true;
    }

    public static bool TryAdd(this SectorDictionary<HashSet<ExtendedZDO>> @this, ExtendedZDO zdo)
        => @this.TryAdd(zdo.GetPosition(), zdo);

    public static bool TryAdd<TValue>(this SectorDictionary<HashSet<TValue>> @this, Vector3 key, TValue value)
        => @this.GetOrAdd(key).Add(value);

    public static void Add<TCollection, TValue>(this SectorDictionary<TCollection> @this, Vector3 key, TValue value)
        where TCollection : class, ICollection<TValue>, new()
        => @this.GetOrAdd(key).Add(value);

    public static bool Remove<TCollection, TValue>(this SectorDictionary<TCollection> @this, Vector3 key, TValue value)
        where TCollection : class, ICollection<TValue>
    {
        if (@this.TryGetValue(key, out var collection))
            return collection.Remove(value);
        return false;
    }

    public static bool Remove<TCollection>(this SectorDictionary<TCollection> @this, ExtendedZDO zdo)
        where TCollection : class, ICollection<ExtendedZDO>
        => @this.Remove(zdo.GetPosition(), zdo);

    //public static Enumerator<TEnumerable, TValue> EnumerateAdjacent<TEnumerable, TValue>(this SectorDictionary<TEnumerable> @this, Vector3 key)
    //    where TEnumerable : class, IEnumerable<TValue>
    //    => new(@this.EnumerateAdjacent(key));

    //public struct Enumerator<TEnumerable, TValue>(SectorDictionary<TEnumerable>.AdjacentEnumerator enumerator) : IEnumerator<TValue>
    //    where TEnumerable : class, IEnumerable<TValue>
    //{
    //    SectorDictionary<TEnumerable>.AdjacentEnumerator _sectorEnumerator = enumerator;
    //    IEnumerator<TValue>? _enumerator;

    //    public readonly Enumerator<TEnumerable, TValue> GetEnumerator() => this;
    //    public readonly TValue Current => _enumerator is null ? default! : _enumerator.Current;
    //    readonly object IEnumerator.Current => Current!;

    //    public readonly void Dispose() => _sectorEnumerator.Dispose();

    //    public bool MoveNext()
    //    {
    //        if (_enumerator is not null && _enumerator.MoveNext())
    //            return true;

    //        if (!_sectorEnumerator.MoveNext())
    //            return false;
    //        _enumerator = _sectorEnumerator.Current.GetEnumerator();
    //        return _enumerator.MoveNext();
    //    }

    //    public void Reset()
    //    {
    //        _sectorEnumerator.Reset();
    //        _enumerator = null;
    //    }
    //}
}