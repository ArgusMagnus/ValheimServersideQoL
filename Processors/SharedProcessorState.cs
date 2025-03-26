using BepInEx.Configuration;
using System.Collections.Concurrent;
using System.Reflection;
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
    public static IReadOnlyDictionary<int, PrefabInfo> PrefabInfo { get; } = new Dictionary<int, PrefabInfo>();
    public static ConcurrentHashSet<ExtendedZDO> Ships { get; } = new();

    public static ConcurrentDictionary<SharedItemDataKey, ConcurrentHashSet<ExtendedZDO>> ContainersByItemName { get; } = new();
    public static ConcurrentDictionary<string, ConcurrentHashSet<ZDOID>> FollowingTamesByPlayerName { get; } = new();

    public static void Initialize(ModConfig cfg)
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
        }

        var dict = (IDictionary<int, PrefabInfo>)PrefabInfo;
        dict.Clear();
        Ships.Clear();

        var componentTypes = typeof(PrefabInfo).GetProperties().Select(x => x.PropertyType).Where(x => typeof(MonoBehaviour).IsAssignableFrom(x)).ToList();

        var needsShips = false;
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            Dictionary<Type, MonoBehaviour>? components = null;
            foreach (var componentType in componentTypes)
            {
                var component = (prefab.GetComponent(componentType) ?? prefab.GetComponentInChildren(componentType)) as MonoBehaviour;
                if (component is not null)
                    (components ??= new()).Add(componentType, component);
            }
            if (components is not null)
            {
                PieceTablesByPiece.TryGetValue(prefab.name, out var pieceTable);
                var prefabInfo = new PrefabInfo(components, pieceTable);
                dict.Add(prefab.name.GetStableHashCode(), prefabInfo);
                needsShips = needsShips || prefabInfo.Ship is not null;
            }
        }

        if (needsShips)
        {
            foreach (var zdo in PrivateAccessor.GetZDOManObjectsByID(ZDOMan.instance).Values.Cast<ExtendedZDO>().Where(x => x.PrefabInfo.Ship is not null))
                Ships.Add(zdo);
        }
    }
}
