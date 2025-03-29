using BepInEx.Configuration;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

record struct ItemKey(string Name, int Quality, int Variant)
{
    public static implicit operator ItemKey(ItemDrop.ItemData data) => new(data);
    public ItemKey(ItemDrop.ItemData data) : this(data.m_shared.m_name, data.m_quality, data.m_variant) { }
}

record struct SharedItemDataKey(string Name)
{
    public static implicit operator SharedItemDataKey(ItemDrop.ItemData.SharedData data) => new(data.m_name);
}

static class SharedProcessorState
{
    public static IReadOnlyCollection<PieceTable> PieceTables { get; } = new HashSet<PieceTable>();
    public static IReadOnlyDictionary<string, PieceTable> PieceTablesByPiece { get; } = new Dictionary<string, PieceTable>();
    public static ConcurrentHashSet<ExtendedZDO> Ships { get; } = new();

    public static ConcurrentDictionary<SharedItemDataKey, ConcurrentHashSet<ExtendedZDO>> ContainersByItemName { get; } = new();
    public static ConcurrentDictionary<string, ConcurrentHashSet<ZDOID>> FollowingTamesByPlayerName { get; } = new();

    static readonly Dictionary<int, PrefabInfo?> __prefabInfo = new();
    static readonly IReadOnlyList<Type> __componentTypes = typeof(PrefabInfo).GetProperties()
        .Select(x => x.PropertyType).Where(x => typeof(MonoBehaviour).IsAssignableFrom(x)).ToList();

    static readonly IReadOnlyList<IReadOnlyList<(Type Type, bool Optional)>> __componentTypeCombinations = typeof(PrefabInfo).GetProperties()
        .Select(x => x.PropertyType)
        .Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>) && x.GenericTypeArguments[0].IsConstructedGenericType)
        .Select(x => x.GenericTypeArguments[0])
        .Where(x => x.IsConstructedGenericType && typeof(ITuple).IsAssignableFrom(x))
        .Select(x =>
        {
            List<(Type Type, bool Optional)>? list = new(x.GenericTypeArguments.Length);
            foreach (var type in x.GenericTypeArguments)
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    list.Add((type, false));
                else if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(PrefabInfo.Optional<>))
                    list.Add((type.GenericTypeArguments[0], true));
                else
                {
                    list = null;
                    break;
                }
            }
            return list!;
        })
        .Where(x => x is not null)
        .ToList();

    public static void Initialize()
    {
        if (PieceTables is { Count: 0})
        {
            var pieceTables = (HashSet<PieceTable>)PieceTables;
            var pieceTablesByPiece = (IDictionary<string, PieceTable>)PieceTablesByPiece;
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                var table = prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
                if (table is null || !pieceTables.Add(table))
                    continue;

                foreach (var piece in table.m_pieces)
                    pieceTablesByPiece.Add(piece.name, table);
            }

            foreach (var zdo in PrivateAccessor.GetZDOManObjectsByID(ZDOMan.instance).Values.Cast<ExtendedZDO>().Where(x => x.PrefabInfo.Ship is not null))
                Ships.Add(zdo);
        }
    }

    public static PrefabInfo? GetPrefabInfo(int hash)
    {
        if (!__prefabInfo.TryGetValue(hash, out var prefabInfo))
        {
            var prefab = ZNetScene.instance.GetPrefab(hash);
            if (prefab is null)
                return null;

            prefabInfo = Get(hash, prefab);
            __prefabInfo.Add(hash, prefabInfo);
        }
        return prefabInfo;

        static PrefabInfo? Get(int hash, GameObject prefab)
        {
            Dictionary<Type, MonoBehaviour>? components = null;
            foreach (var componentType in __componentTypes)
            {
                var component = GetComponent(prefab, componentType);
                if (component is not null)
                    (components ??= new()).Add(componentType, component);
            }
            foreach (var componentTypeCombinations in __componentTypeCombinations)
            {
                var componentCombinations = componentTypeCombinations
                    .Select(x => (x.Type, x.Optional, Component: (components?.TryGetValue(x.Type, out var c) ?? false) ? c : GetComponent(prefab, x.Type)))
                    .ToList();
                if (componentCombinations.Count > 0 && !componentCombinations.Any(x => x.Component is null && !x.Optional))
                {
                    components ??= new();
                    foreach (var (type, _, component) in componentCombinations)
                    {
                        if (component is not null && !components.ContainsKey(type))
                            components.Add(type, component);
                    }
                }
            }
            if (components is null)
                return null;
            return new(components);
        }

        static MonoBehaviour? GetComponent(GameObject prefab, Type componentType)
        {
            if (componentType == typeof(PieceTable))
                return PieceTablesByPiece.TryGetValue(prefab.name, out var c) ? c : null;
            return (prefab.GetComponent(componentType) ?? prefab.GetComponentInChildren(componentType)) as MonoBehaviour;
        }
    }
}
