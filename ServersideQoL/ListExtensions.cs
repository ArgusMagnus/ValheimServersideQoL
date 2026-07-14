using System.Collections;

namespace Valheim.ServersideQoL;

public static class ListExtensions
{
    public static ListEnumerable<T> AsEnumerable<T>(this IReadOnlyList<T> list) => new(list);
    public static IEnumerable<T> AsBoxedEnumerable<T>(this IReadOnlyList<T> list) => Enumerable.AsEnumerable(list);

    public readonly struct ListEnumerable<T>(IReadOnlyList<T> list)
    {
        readonly IReadOnlyList<T> _list = list;

        public Enumerator GetEnumerator() => new(_list);

        public struct Enumerator(IReadOnlyList<T> list) : IEnumerator<T>
        {
            readonly IReadOnlyList<T> _list = list;
            readonly int _count = list.Count;
            int _index = -1;

            public T Current { get; private set; } = default!;
            readonly object? IEnumerator.Current => Current;

            public void Dispose() => Current = default!;

            public bool MoveNext()
            {
                if (++_index < _count)
                {
                    Current = _list[_index];
                    return true;
                }
                Current = default!;
                return false;
            }

            public void Reset() => _index = -1;
        }
    }
}
