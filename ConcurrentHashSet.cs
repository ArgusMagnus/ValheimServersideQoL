using System.Collections;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL;

sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
        where T : notnull
{
    readonly ConcurrentDictionary<T, object?> _dict;

    public ConcurrentHashSet()
        => _dict = new();
    public ConcurrentHashSet(IEnumerable<T> collection)
        => _dict = new(collection.Select(x => new KeyValuePair<T, object?>(x, default)));
    public ConcurrentHashSet(IEqualityComparer<T> comparer)
        => _dict = new(comparer);
    public ConcurrentHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
        => _dict = new(collection.Select(x => new KeyValuePair<T, object?>(x, default)), comparer);
    public ConcurrentHashSet(int concurrencyLevel, int capacity)
        => _dict = new(concurrencyLevel, capacity);
    public ConcurrentHashSet(int concurrencyLevel, IEnumerable<T> collection, IEqualityComparer<T> comparer)
        => _dict = new(concurrencyLevel, collection.Select(x => new KeyValuePair<T, object?>(x, default)), comparer);
    public ConcurrentHashSet(int concurrencyLevel, int capacity, IEqualityComparer<T> comparer)
        => _dict = new(concurrencyLevel, capacity, comparer);

    public int Count => _dict.Count;
    public bool IsEmpty => _dict.IsEmpty;
    bool ICollection<T>.IsReadOnly => ((ICollection<KeyValuePair<T, byte>>)_dict).IsReadOnly;

    public bool Add(T item) => _dict.TryAdd(item, default);
    public bool Remove(T item) => _dict.TryRemove(item, out var tmp);
    public bool Contains(T item) => _dict.ContainsKey(item);
    public void Clear() => _dict.Clear();

    void ICollection<T>.Add(T item) => Add(item);

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        foreach (var value in _dict.Keys)
            array[arrayIndex++] = value;
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _dict.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dict.Keys.GetEnumerator();
}
