using System.Diagnostics;
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

    static readonly Dictionary<int, PrefabInfo?> __prefabInfo = [];

    static readonly IReadOnlyList<Type> __componentTypes = [.. typeof(PrefabInfo).GetProperties()
        .Select(static x => x.PropertyType).Where(static x => typeof(MonoBehaviour).IsAssignableFrom(x))];

    static readonly IReadOnlyList<IReadOnlyList<(Type Type, bool Optional)>> __componentTypeCombinations = [.. typeof(PrefabInfo).GetProperties()
        .Select(static x => x.PropertyType)
        .Where(static x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == typeof(Nullable<>) && x.GenericTypeArguments[0].IsConstructedGenericType)
        .Select(static x => x.GenericTypeArguments[0])
        .Where(static x => x.IsConstructedGenericType && typeof(ITuple).IsAssignableFrom(x))
        .Select(static x =>
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
        .Where(static x => x is not null)];

    static IReadOnlyDictionary<string, Character>? __characterByTrophy;
    public static IReadOnlyDictionary<string, Character> CharacterByTrophy => __characterByTrophy ??= new Func<IReadOnlyDictionary<string, Character>>(static () =>
    {
        Dictionary<string, Character> result = new();
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab.GetComponent<Character>() is not { m_boss: false } character || prefab.GetComponent<CharacterDrop>() is not { } characterDrop)
                continue;

            var drop = characterDrop.m_drops.Select(static x => x.m_prefab?.GetComponent<ItemDrop>()).FirstOrDefault(static x => x is { m_itemData: { m_shared: { m_itemType: ItemDrop.ItemData.ItemType.Trophy } } });
            if (drop is null)
                continue;

            //Main.Instance.Logger.LogWarning($"{prefab.name}: {drop.name}");
            if (!result.TryGetValue(drop.name, out var otherCharacterPrefab))
                result.Add(drop.name, character);
            else if (drop.name.Contains(prefab.name) || prefab.name.Length < otherCharacterPrefab.name.Length)
                result[drop.name] = character;
        }
        return result;
    }).Invoke();

    static IReadOnlyDictionary<Heightmap.Biome, Character>? __bossesByBiome;
    public static IReadOnlyDictionary<Heightmap.Biome, Character> BossesByBiome => __bossesByBiome ??= new Func<IReadOnlyDictionary<Heightmap.Biome, Character>>(static () =>
    {
        var bosses = new Dictionary<Heightmap.Biome, Character>();
        foreach (var includeDungeons in (IEnumerable<bool>)[false, true])
        {
            foreach (var location in ZoneSystem.instance.m_locations)
            {
                if (!location.m_enable || !location.m_prioritized || location.m_biome is Heightmap.Biome.None or Heightmap.Biome.All or Heightmap.Biome.Ocean)
                    continue;

                if (bosses.ContainsKey(location.m_biome))
                    continue;

                location.m_prefab.Load();
                var bowl = location.m_prefab.Asset.GetComponentInChildren<OfferingBowl>();
                if (includeDungeons && bowl is null && location.m_prefab.Asset.GetComponentInChildren<DungeonGenerator>() is { } dungeonGen)
                {
                    foreach (var roomRef in dungeonGen.GetAvailableRoomPrefabs())
                    {
                        roomRef.Load();
                        var room = roomRef.Asset.GetComponent<Room>();
                        bowl = room.GetComponentInChildren<OfferingBowl>();
                        if (bowl is not null)
                            break;
                    }
                }
                if (bowl is not null)
                    bosses.Add(location.m_biome, bowl.m_bossPrefab.GetComponent<Character>());
            }
        }
        return bosses;
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
                    (components ??= []).Add(componentType, component);
            }
            foreach (var componentTypeCombinations in __componentTypeCombinations)
            {
                var componentCombinations = componentTypeCombinations
                    .Select(x => (x.Type, x.Optional, Component: (components?.TryGetValue(x.Type, out var c) ?? false) ? c : GetComponent(prefab, x.Type, availableComponents)))
                    .ToList();
                if (componentCombinations.Count > 0 && !componentCombinations.Any(static x => x.Component is null && !x.Optional))
                {
                    components ??= [];
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
