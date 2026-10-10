using UnityEngine;

namespace ServersideQoL.PrefabConfigurator;

[Processor(Id,
  Priority = int.MinValue)] // Run before every other processor to allow the others to overwrite the values set by this processor
[RunBefore<PrefabProcessor>]
public sealed class CraftingStationProcessor : Processor<CraftingStationProcessor.PrefabInfo>
{
  public const string Id = "9d781632-9856-4af3-ac08-4c17610fe268";
  public sealed record PrefabInfo(CraftingStation? Station) : ProcessorPrefabInfo;

  /// <summary>Vanilla collider dimensions, captured lazily so a <c>-1</c> config value restores the original exclusion range.</summary>
  readonly Dictionary<Collider, (float Radius, float Height)> _defaultDimensions = [];

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.IsModCreator())
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.UnregisterProcessor;
    var prefabHash = zdo.ZDO.GetPrefab();

    var stations = Config.Instance.CraftingStations;
    if (!stations.BuildRanges.TryGetValue(prefabHash, out var buildRangeEntry)
      && !stations.EnemySpawnRanges.ContainsKey(prefabHash))
      return ProcessResult.UnregisterProcessor;

    var buildRange = buildRangeEntry?.Value ?? -1f;

    var fields = zdo.Fields<CraftingStation>();
    if (buildRange < 0)
    {
      /// -1 skips the station: leave the build range field untouched and restore vanilla exclusion colliders.
      fields.Reset(static () => x => x.m_rangeBuild);
      UpdateExclusion(prefabInfo, null);
      return result;
    }

    if (fields.UpdateValue(static () => x => x.m_rangeBuild, buildRange))
      result |= ProcessResult.RecreateZDO;

    if (stations.EnemySpawnRanges.TryGetValue(prefabHash, out var exclusionEntry))
      UpdateExclusion(prefabInfo, exclusionEntry.Value >= 0 ? exclusionEntry.Value : buildRange);

    return result;
  }

  /// <summary>Applies <paramref name="radius"/> to the prefab's PlayerBase exclusion colliders, or restores vanilla dimensions when <c>null</c>.</summary>
  void UpdateExclusion(PrefabInfo prefabInfo, float? radius)
  {
    /// PlayerBase effect areas sit on inactive children of the prefab (e.g. a <see cref="CapsuleCollider"/> on workbench/forge)
    foreach (var effectArea in prefabInfo.PrefabInfo.Prefab.GetComponentsInChildren<EffectArea>(true))
    {
      if ((effectArea.m_type & EffectArea.Type.PlayerBase) is 0
        || effectArea.GetComponent<Collider>() is not { } collider)
        continue;
      if (!_defaultDimensions.TryGetValue(collider, out var vanilla))
        _defaultDimensions.Add(collider, vanilla = collider switch
        {
          SphereCollider s => (s.radius, 0f),
          CapsuleCollider c => (c.radius, c.height),
          _ => (0f, 0f)
        });
      var effectiveRadius = radius ?? vanilla.Radius;
      switch (collider)
      {
        case SphereCollider s:
          if (s.radius != effectiveRadius)
          {
            Logger.LogDiagnosticInfo($"{prefabInfo.PrefabInfo.PrefabName}: PlayerBase exclusion radius {s.radius} -> {effectiveRadius}");
            s.radius = effectiveRadius;
          }
          break;
        case CapsuleCollider c:
          if (c.radius != effectiveRadius)
          {
            Logger.LogDiagnosticInfo($"{prefabInfo.PrefabInfo.PrefabName}: PlayerBase exclusion radius {c.radius} -> {effectiveRadius}");
            c.radius = effectiveRadius;
            c.height = effectiveRadius > 0 ? vanilla.Height : 0;
          }
          break;
      }
    }
  }
}
