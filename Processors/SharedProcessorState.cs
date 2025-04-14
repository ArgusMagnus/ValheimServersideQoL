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

sealed class SharedProcessorState
{
    public IReadOnlyCollection<PieceTable> PieceTables { get; }
    public IReadOnlyDictionary<string, PieceTable> PieceTablesByPiece { get; }
    public IReadOnlyDictionary<int, PrefabInfo> PrefabInfo { get; }
    public IReadOnlyDictionary<SharedItemDataKey, string> CharacterByTrophy { get; }

    public SharedProcessorState()
    {
        HashSet<PieceTable> pieceTables = [];
        Dictionary<string, PieceTable> pieceTablesByPiece = [];
        Dictionary<SharedItemDataKey, string> characterByTrophy = [];
        Dictionary<int, PrefabInfo> prefabInfo = [];

        foreach (var item in ObjectDB.instance.m_items)
        {
            if (item.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_buildPieces is { } table && pieceTables.Add(table))
            {
                foreach (var piece in table.m_pieces)
                {
                    if (!pieceTablesByPiece.ContainsKey(piece.name))
                        pieceTablesByPiece.Add(piece.name, table);
                }
            }
        }

        var componentTypes = GetComponentTypes();
        var componentTypeCombinations = GetComponentTypeCombinations();

        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab.GetComponent<Character>() is { m_boss: false } && prefab.GetComponent<CharacterDrop>() is { } characterDrop)
            {
                var drop = characterDrop.m_drops.Select(x => x.m_prefab?.GetComponent<ItemDrop>()).FirstOrDefault(x => x is { m_itemData: { m_shared: { m_itemType: ItemDrop.ItemData.ItemType.Trophy } } });
                if (drop is not null)
                {
                    //Main.Instance.Logger.LogWarning($"{prefab.name}: {drop.name}");
                    if (!characterByTrophy.TryGetValue(drop.m_itemData.m_shared, out var otherCharacterPrefab))
                        characterByTrophy.Add(drop.m_itemData.m_shared, prefab.name);
                    else if (drop.name.Contains(prefab.name) || prefab.name.Length < otherCharacterPrefab.Length)
                        characterByTrophy[drop.m_itemData.m_shared] = prefab.name;
                }
            }

            if (GetPrefabInfo(prefab, componentTypes, componentTypeCombinations, pieceTablesByPiece) is { } info)
                prefabInfo.Add(prefab.name.GetStableHashCode(), info);
        }
        PieceTables = pieceTables;
        PieceTablesByPiece = pieceTablesByPiece;
        CharacterByTrophy = characterByTrophy;
        PrefabInfo = prefabInfo;
    }

    static IReadOnlyList<Type> GetComponentTypes() => [.. typeof(PrefabInfo).GetProperties()
        .Select(x => x.PropertyType).Where(x => typeof(MonoBehaviour).IsAssignableFrom(x))];

    static IReadOnlyList<IReadOnlyList<(Type Type, bool Optional)>> GetComponentTypeCombinations() => [.. typeof(PrefabInfo).GetProperties()
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


    static ConcurrentHashSet<ExtendedZDO> GetShips()
    {
        var ships = new ConcurrentHashSet<ExtendedZDO>();
        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.PrefabInfo.Ship is not null))
            ships.Add(zdo);
        return ships;
    }

    static PrefabInfo? GetPrefabInfo(GameObject prefab, IReadOnlyList<Type> componentTypes, IReadOnlyList<IReadOnlyList<(Type Type, bool Optional)>> componentTypeCombinationsDict, IReadOnlyDictionary<string, PieceTable> pieceTablesByPiece)
    {
        Dictionary<Type, MonoBehaviour>? components = null;
        foreach (var componentType in componentTypes)
        {
            var component = GetComponent(prefab, componentType, pieceTablesByPiece);
            if (component is not null)
                (components ??= new()).Add(componentType, component);
        }
        foreach (var componentTypeCombinations in componentTypeCombinationsDict)
        {
            var componentCombinations = componentTypeCombinations
                .Select(x => (x.Type, x.Optional, Component: (components?.TryGetValue(x.Type, out var c) ?? false) ? c : GetComponent(prefab, x.Type, pieceTablesByPiece)))
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

        static MonoBehaviour? GetComponent(GameObject prefab, Type componentType, IReadOnlyDictionary<string, PieceTable> pieceTablesByPiece)
        {
            if (componentType == typeof(PieceTable))
                return pieceTablesByPiece.TryGetValue(prefab.name, out var c) ? c : null;
            return (MonoBehaviour?)prefab.GetComponent<ZNetView>()?.gameObject.GetComponentInChildren(componentType);

            //if (componentType == typeof(PieceTable))
            //    return PieceTablesByPiece.TryGetValue(prefab.name, out var c) ? c : null;
            //if ((prefab.GetComponent(componentType) ?? prefab.GetComponentInChildren(componentType)) is MonoBehaviour component)
            //    return component;
            //return null;
        }
    }
}
