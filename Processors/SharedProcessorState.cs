using BepInEx.Configuration;
using System.Collections.Concurrent;
using System.Reflection;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

record InventoryEx(Inventory Inventory)
{
    public uint DataRevision { get; set; }
}

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
    public IReadOnlyDictionary<int, PrefabInfo> PrefabInfo { get; } = new Dictionary<int, PrefabInfo>();
    public ConcurrentHashSet<ZDOID> Ships { get; } = new();
    public ConcurrentDictionary<ZDOID, uint> DataRevisions { get; } = new();

    public ConcurrentDictionary<SharedItemDataKey, ConcurrentDictionary<ZDOID, InventoryEx>> ContainersByItemName { get; } = new();
    public ConcurrentDictionary<string, ConcurrentHashSet<ZDOID>> FollowingTamesByPlayerName { get; } = new();

    public void Initialize(ModConfig cfg)
    {
        var dict = (IDictionary<int, PrefabInfo>)PrefabInfo;
        dict.Clear();
        Ships.Clear();

        List<HashSet<Type>> requiredTypes = new();
        foreach (var sectionProperty in cfg.GetType().GetProperties().Where(x => x.PropertyType.IsClass))
        {
            object? section = null;
            IEnumerable<RequiredPrefabsAttribute>? classAttr = null;
            foreach (var keyProperty in sectionProperty.PropertyType.GetProperties())
            {
                section ??= sectionProperty.GetValue(cfg);

                var attrs = keyProperty.GetCustomAttributes<RequiredPrefabsAttribute>();

                switch (keyProperty.GetValue(section))
                {
                    default:
                        if (attrs.Any())
                            throw new Exception($"{nameof(RequiredPrefabsAttribute)} only supported on classes and properties of type {nameof(ConfigEntry<bool>)}/{nameof(ConfigEntry<bool>)}");
                        continue;

                    case ConfigEntry<bool> boolProp:
                        if (!boolProp.Value)
                            continue;
                        break;

                    case ConfigEntry<float> floatProp:
                        if (float.IsNaN(floatProp.Value))
                            continue;
                        break;
                }

                classAttr ??= sectionProperty.PropertyType.GetCustomAttributes<RequiredPrefabsAttribute>();

                foreach (var attr in attrs)
                {
                    var types = attr.Prefabs.ToHashSet();
                    if (!requiredTypes.Any(x => x.SequenceEqual(types)))
                        requiredTypes.Add(types);
                }
            }

            foreach (var attr in classAttr ?? [])
            {
                if (attr.Prefabs.ToHashSet() is { } classTypes && !requiredTypes.Any(x => x.SequenceEqual(classTypes)))
                    requiredTypes.Add(classTypes);
            }
        }

        if (requiredTypes is { Count: > 0 })
        {
            var needsShips = false;
            foreach (var prefab in ZNetScene.instance.m_prefabs)
            {
                Dictionary<Type, Component>? components = null;
                foreach (var requiredTypeList in requiredTypes)
                {
                    var prefabs = requiredTypeList.Select(x => (Type: x, Component: prefab.GetComponent(x))).Where(x => x.Component is not null).ToList();
                    if (prefabs.Count != requiredTypeList.Count)
                        continue;
                    foreach (var (type, component) in prefabs)
                    {
                        components ??= new();
                        if (!components.ContainsKey(type))
                            components.Add(type, component);
                    }
                }
                if (components is not null)
                {
                    var prefabInfo = new PrefabInfo(prefab, components);
                    dict.Add(prefab.name.GetStableHashCode(), prefabInfo);
                    needsShips = needsShips || prefabInfo.Ship is not null;
                }
            }

            if (needsShips)
            {
                foreach (var zdo in PrivateAccessor.GetZDOManObjectsByID(ZDOMan.instance).Values.Where(x => PrefabInfo.TryGetValue(x.GetPrefab(), out var info) && info.Ship is not null))
                    Ships.Add(zdo.m_uid);
            }
        }
    }
}
