using UnityEngine;

namespace ServersideQoL.MoreVile;

[Processor(Id)]
public sealed class VileProcessor : Processor<VileProcessor.PrefabInfo>
{
  public const string Id = "a8c4a115-80bb-4288-82d6-6e08a1c57910";
  const string VilePrefabName = "Unbjorn";
  static readonly int __vilePrefab = VilePrefabName.GetStableHashCode();

  /// <summary>
  /// Viles which have been seen for the first time more than this long after they spawned
  /// (e.g. existing Viles when the mod is installed) are not multiplied.
  /// </summary>
  static readonly TimeSpan __maxSpawnAge = TimeSpan.FromSeconds(60);

  static readonly ServerVar<bool> __processedVar = MoreVilePlugin.RegisterServerVar<bool>("Processed");
  readonly List<ZDO> _sectorObjects = [];

  public sealed record PrefabInfo(Character Character) : ProcessorPrefabInfo
  {
    public override bool IsValid => PrefabInfo.PrefabHash == __vilePrefab;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (__processedVar.Get(zdo))
      return ProcessResult.UnregisterProcessor;
    __processedVar.Set(zdo, true);

    // Event creatures despawn when the event ends, additional Viles would not
    if (zdo.Vars.GetTamed() || zdo.Vars.GetEventCreature())
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetSpawnTime() != default && zdo.GetTimeSinceSpawned() > __maxSpawnAge)
      return ProcessResult.UnregisterProcessor;

    var extra = Config.Instance.SpawnMultiplier.Value - 1f;
    var count = (int)extra;
    if (UnityEngine.Random.value < extra - count)
      count++;
    if (count <= 0)
      return ProcessResult.UnregisterProcessor;

    if (Config.Instance.MaxNearby.Value > 0)
    {
      ZDOMan.instance.FindSectorObjects(zdo.ZDO.GetSector(), 1, _sectorObjects);
      var nearby = _sectorObjects.Count(static x => x.GetPrefab() == __vilePrefab);
      _sectorObjects.Clear();
      count = Math.Min(count, Config.Instance.MaxNearby.Value - nearby);
      if (count <= 0)
        return ProcessResult.UnregisterProcessor;
    }

    var origin = zdo.ZDO.GetPosition();
    var level = Config.Instance.CopyLevel.Value ? zdo.Vars.GetLevel() : 1;
    for (int i = 0; i < count; i++)
    {
      var offset = UnityEngine.Random.insideUnitCircle * Config.Instance.SpawnRadius.Value;
      var pos = origin + new Vector3(offset.x, 0, offset.y);
      // Max: don't spawn below the terrain, but keep the original height in dungeons/on structures
      pos.y = Mathf.Max(origin.y, GetHeight(pos)) + 0.5f;

      var vile = Spawn(__vilePrefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
      __processedVar.Set(vile, true);
      if (level > 1)
        vile.Vars.SetLevel(level);
    }

    Logger.DevLog($"Spawned {count} additional {VilePrefabName} at {origin}");
    return ProcessResult.UnregisterProcessor;
  }
}
