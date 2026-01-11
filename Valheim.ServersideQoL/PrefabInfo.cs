using UnityEngine;

namespace Valheim.ServersideQoL;

public interface IPrefabInfo
{
    int PrefabHash { get; }
    string PrefabName { get; }
    IReadOnlyDictionary<Type, MonoBehaviour>? Components { get; }
}

public abstract class PrefabInfo : IPrefabInfo
{
    public GameObject Prefab { get; internal set; } = default!;
    public int PrefabHash { get; internal set; }
    public string PrefabName { get; internal set; } = default!;
    public IReadOnlyDictionary<Type, MonoBehaviour> Components { get; internal set; } = default!;
    internal readonly List<Processor> AvailableProcessors = [];
    internal readonly List<Processor> EnabledProcessors = [];
}

public abstract record ProcessorPrefabInfo;

public static class PrefabInfoExtensions
{
    public static T GetExtension<T>(this IPrefabInfo prefabInfo)
        where T : class, IPrefabInfo
        => (T)prefabInfo;
}