using System.Collections;
using System.Collections.Concurrent;

namespace TestMod;

sealed class ConcurrentHashSet<T> : ICollection<T>, IReadOnlyCollection<T>
    where T : notnull
{
    readonly ConcurrentDictionary<T, object?> _dict = new();

    public bool Add(T value) => _dict.TryAdd(value, null);
    public bool Remove(T value) => _dict.TryRemove(value, out _);

    public int Count => _dict.Count;

    bool ICollection<T>.IsReadOnly => false;

    void ICollection<T>.Add(T item)
    {
        if (!Add(item))
            throw new InvalidOperationException();
    }

    public void Clear() => _dict.Clear();

    public bool Contains(T item) => _dict.ContainsKey(item);

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        throw new NotImplementedException();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => _dict.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _dict.Keys.GetEnumerator();
}
