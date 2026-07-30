using System.Diagnostics;
using UnityEngine;

namespace ServersideQoL;

public interface IPrefabInfo
{
  int PrefabHash { get; }
  string PrefabName { get; }
  IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>> Components { get; }
}

[DebuggerDisplay($"{{{nameof(PrefabName)}}}: Processors: {{{nameof(EnabledProcessors)}.{nameof(EnabledProcessors.Count)}}}")]
public abstract class PrefabInfo : IPrefabInfo
{
  public GameObject Prefab { get; private set; } = default!;
  public int PrefabHash { get; private set; }
  public string PrefabName { get; private set; } = default!;
  public IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>> Components { get; private set; } = default!;
  public IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>> BaseComponents { get; private set; } = default!;
  public bool ReleaseOwnershipOnRecreate { get; private set; }
  internal List<Processor> AvailableProcessors { get; } = [];
  internal List<Processor> EnabledProcessors { get; } = [];

  internal void Init(GameObject prefab, int prefabHash, string prefabName, IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>>? components, IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>>? baseComponents)
  {
    Prefab = prefab;
    PrefabHash = prefabHash;
    PrefabName = prefabName;
    Components = components is { Count: > 0 } ? components : EmptyReadOnlyCollections<Type, IReadOnlyList<MonoBehaviour>>.Dictionary;
    BaseComponents = baseComponents is { Count: > 0 } ? baseComponents : EmptyReadOnlyCollections<Type, IReadOnlyList<MonoBehaviour>>.Dictionary;
    ReleaseOwnershipOnRecreate =
      (Components.TryGetValue(typeof(ZSyncTransform), out var list) || BaseComponents.TryGetValue(typeof(ZSyncTransform), out list))
      && list.Cast<ZSyncTransform>().Any(static x => x.m_syncPosition || x.m_syncRotation || x.m_syncBodyVelocity);
  }
}

public abstract record ProcessorPrefabInfo
{
  // Only simple types that are very unlikely to change in the future should be defined here,
  // because modifying these types potentially affects multiple processors.
  public static class Shared
  {
    public sealed record SignPrefabInfo(Sign Sign) : ProcessorPrefabInfo;
  }
}

interface IProcessorPrefabInfo<TPrefabInfo> : IPrefabInfo
    where TPrefabInfo : ProcessorPrefabInfo
{
  TPrefabInfo? PrefabInfo { get; set; }
}

static class PrefabInfoExtensions
{
  public static T GetExtension<T>(this IPrefabInfo prefabInfo)
      where T : class, IPrefabInfo
      => (T)prefabInfo;
}
