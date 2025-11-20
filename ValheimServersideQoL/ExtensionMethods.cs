using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL;

static class ExtensionMethods
{
    public static ExtendedZDO? GetExtendedZDO(this ZDOMan instance, ZDOID id) => (ExtendedZDO?)instance.GetZDO(id);

    /// <see cref="ZNetScene.InActiveArea(Vector2i, Vector2i)"/>
    public static int GetActiveArea(this ZoneSystem instance) => instance.m_activeArea - 1;
    public static int GetLoadedArea(this ZoneSystem instance) => instance.m_activeArea;

    public static float GetHeight(this Heightmap hmap, Vector3 pos)
    {
        hmap.WorldToVertex(pos, out var x, out var y);
        return hmap.GetHeight(x, y);
    }

    public static ConfigEntry<T> BindEx<T>(this ConfigFile config, string section, T defaultValue, string description, [CallerMemberName]string key = default!)
        => config.Bind(section, key, defaultValue, description);

    public static ConfigEntry<T> BindEx<T>(this ConfigFile config, string section, T defaultValue, string description, AcceptableValueBase? acceptableValues, [CallerMemberName] string key = default!)
        => config.Bind(section, key, defaultValue, new ConfigDescription(description, acceptableValues));

    [Conditional("DEBUG")]
    public static void AssertIs<T>(this ExtendedZDO zdo) where T : MonoBehaviour
    {
#if DEBUG
        if (zdo.PrefabInfo.Prefab.GetComponentInChildren<T>() is null)
            throw new ArgumentException();
#endif
    }

    public static void InsertSorted<T>(this List<T> list, T item) where T : IComparable<T>
    {
        var index = list.BinarySearch(item);
        if (index < 0)
            index = ~index; // bitwise complement = insertion point
        list.Insert(index, item);
    }

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