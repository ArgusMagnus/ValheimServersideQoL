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
  internal readonly List<Processor> AvailableProcessors = [];
  internal readonly List<Processor> EnabledProcessors = [];
  internal readonly List<Processor> EnabledCyclicProcessors = [];

  internal void Init(GameObject prefab, int prefabHash, string prefabName, IReadOnlyDictionary<Type, IReadOnlyList<MonoBehaviour>> components)
  {
    Prefab = prefab;
    PrefabHash = prefabHash;
    PrefabName = prefabName;
    Components = components;
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
