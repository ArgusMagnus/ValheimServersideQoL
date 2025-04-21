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
    sealed record PieceTableInfo(HashSet<PieceTable> PieceTables, Dictionary<string, PieceTable> PieceTablesByName);
    static PieceTableInfo? __pieceTables;
    public static IReadOnlyCollection<PieceTable> PieceTables => (__pieceTables ??= GetPieceTableInfo()).PieceTables;
    public static IReadOnlyDictionary<string, PieceTable> PieceTablesByPiece => (__pieceTables ??= GetPieceTableInfo()).PieceTablesByName;

    static ConcurrentHashSet<ExtendedZDO>? __ships;
    public static ConcurrentHashSet<ExtendedZDO> Ships => __ships ??= GetShips();

    static readonly Dictionary<int, PrefabInfo?> __prefabInfo = [];

    static readonly IReadOnlyList<Type> __componentTypes = [.. typeof(PrefabInfo).GetProperties()
        .Select(x => x.PropertyType).Where(x => typeof(MonoBehaviour).IsAssignableFrom(x))];

    static readonly IReadOnlyList<IReadOnlyList<(Type Type, bool Optional)>> __componentTypeCombinations = [.. typeof(PrefabInfo).GetProperties()
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
        .Where(x => x is not null)];

    static IReadOnlyDictionary<SharedItemDataKey, string>? __characterByTrophy;
    public static IReadOnlyDictionary<SharedItemDataKey, string> CharacterByTrophy => __characterByTrophy ??= new Func<IReadOnlyDictionary<SharedItemDataKey, string>>(static () =>
    {
        Dictionary<SharedItemDataKey, string> result = new();
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab.GetComponent<Character>() is not { m_boss: false } || prefab.GetComponent<CharacterDrop>() is not { } characterDrop)
                continue;

            var drop = characterDrop.m_drops.Select(x => x.m_prefab?.GetComponent<ItemDrop>()).FirstOrDefault(x => x is { m_itemData: { m_shared: { m_itemType: ItemDrop.ItemData.ItemType.Trophy } } });
            if (drop is null)
                continue;

            //Main.Instance.Logger.LogWarning($"{prefab.name}: {drop.name}");
            if (!result.TryGetValue(drop.m_itemData.m_shared, out var otherCharacterPrefab))
                result.Add(drop.m_itemData.m_shared, prefab.name);
            else if (drop.name.Contains(prefab.name) || prefab.name.Length < otherCharacterPrefab.Length)
                result[drop.m_itemData.m_shared] = prefab.name;
        }
        return result;
    }).Invoke();

    static PieceTableInfo GetPieceTableInfo()
    {
        var result = new PieceTableInfo(new(), new());
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            var table = prefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces;
            if (table is null || !result.PieceTables.Add(table))
                continue;

            foreach (var piece in table.m_pieces)
            {
                if (!result.PieceTablesByName.ContainsKey(piece.name))
                    result.PieceTablesByName.Add(piece.name, table);
            }
        }
        return result;
    }

    static ConcurrentHashSet<ExtendedZDO> GetShips()
    {
        var ships = new ConcurrentHashSet<ExtendedZDO>();
        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.PrefabInfo.Ship is not null))
            ships.Add(zdo);
        return ships;
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
            if (prefab.GetComponent<ZNetView>()?.gameObject.GetComponentsInChildren<MonoBehaviour>() is not { } availableComponents)
                return null;

            Dictionary<Type, MonoBehaviour>? components = null;
            foreach (var componentType in __componentTypes)
            {
                var component = GetComponent(prefab, componentType, availableComponents);
                if (component is not null)
                    (components ??= new()).Add(componentType, component);
            }
            foreach (var componentTypeCombinations in __componentTypeCombinations)
            {
                var componentCombinations = componentTypeCombinations
                    .Select(x => (x.Type, x.Optional, Component: (components?.TryGetValue(x.Type, out var c) ?? false) ? c : GetComponent(prefab, x.Type, availableComponents)))
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
            return new(prefab, components);
        }

        static MonoBehaviour? GetComponent(GameObject prefab, Type componentType, IReadOnlyList<MonoBehaviour> availableComponents)
        {
            if (componentType == typeof(PieceTable))
                return PieceTablesByPiece.TryGetValue(prefab.name, out var c) ? c : null;
            return availableComponents.FirstOrDefault(x => x.GetType() == componentType);

            //if (componentType == typeof(PieceTable))
            //    return PieceTablesByPiece.TryGetValue(prefab.name, out var c) ? c : null;
            //if ((prefab.GetComponent(componentType) ?? prefab.GetComponentInChildren(componentType)) is MonoBehaviour component)
            //    return component;
            //return null;
        }
    }
}
