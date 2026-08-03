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
  public GameObject? Prefab { get; private set; }
  public int PrefabHash { get; private set; }
  public string PrefabName { get; private set; } = default!;
  public IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>> Components { get; private set; } = default!;
  public bool ReleaseOwnershipOnRecreate { get; private set; }
  internal List<Processor> AvailableProcessors { get; } = [];
  internal List<Processor> EnabledProcessors { get; } = [];

  internal void Init(GameObject prefab, int prefabHash, string prefabName, IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>>? components)
  {
    Prefab = prefab;
    PrefabHash = prefabHash;
    PrefabName = prefabName;
    Components = components is { Count: > 0 } ? components : EmptyReadOnlyCollections<Type, IReadOnlyList<MonoBehaviour>>.Dictionary;
    ReleaseOwnershipOnRecreate =
      Components.TryGetValue(typeof(ZSyncTransform), out var list) &&
      list.Cast<ZSyncTransform>().Any(static x => x.m_syncPosition || x.m_syncRotation || x.m_syncBodyVelocity);
  }

  public bool HasComponent<T>() where T : MonoBehaviour => Components.ContainsKey(typeof(T));
  public T? GetComponent<T>() where T : MonoBehaviour => Components.TryGetValue(typeof(T), out var list) ? (T)list[0] : null;
  public T GetRequiredComponent<T>() where T : MonoBehaviour => (T)Components[typeof(T)][0];
}

public abstract record ProcessorPrefabInfo
{
  public virtual bool IsValid => true;
  public PrefabInfo PrefabInfo { get; internal set; } = default!;
}

public sealed record ProcessorPrefabInfo<T>(T Component) : ProcessorPrefabInfo;

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
