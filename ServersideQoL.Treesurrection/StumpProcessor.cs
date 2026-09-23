using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.Treesurrection;

[Processor(Id)]
public sealed class StumpProcessor : Processor<StumpProcessor.PrefabInfo>
{
  public const string Id = "a17d6420-25d7-48bd-98ae-ca291083cfaa";
  readonly List<ZDO> _sectorObjects = [];
  static readonly ServerVar<long> __invulnerableUntilTicks = TreesurrectionPlugin.RegisterServerVar<long>("InvulnerableUntilTicks");

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    ProcessResult result;
    if (prefabInfo.IsStump)
    {
      zdo.Destroyed += OnStumpDestroyed;
      result = ProcessResult.UnregisterProcessor;
    }
    else if (PlacedObjects.Contains(zdo))
    {
      var invulnerableUntil = new DateTimeOffset(__invulnerableUntilTicks.Get(zdo), default);
      var delay = (float)(invulnerableUntil - DateTimeOffset.UtcNow).TotalSeconds;
      if (delay > 0)
        result = ScheduleReprocessing(delay);
      else
      {
        if (zdo.Vars.GetHealth() < 0)
          zdo.Vars.SetHealth(prefabInfo.Destructible.m_health);
        result = ProcessResult.UnregisterProcessor;
      }
    }
    else
    {
      Logger.DevLog($"Unexpected prefab: {prefabInfo.PrefabInfo.PrefabName}");
      result = ProcessResult.UnregisterProcessor;
    }

    return result;
  }

  void OnStumpDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo) is not { IsStump: true } prefabInfo)
      return;
    if (UnityEngine.Random.Range(0, 100) >= Config.Instance.SaplingDropChance.Value)
      return;

    var sapling = prefabInfo.SaplingsByTree.Values.First()!;
    if (prefabInfo.SaplingsByTree.Count > 1)
    {
      ZDOMan.instance.FindSectorObjects(zdo.ZDO.GetSector(), ZNet.instance.GetSyncedSimulationDistance(), _sectorObjects);
      foreach (var tmpZdo in _sectorObjects)
      {
        if (GetPrefabInfo(tmpZdo.GetPrefab()).GetComponent<TreeBase>() is { } tmpTree && prefabInfo.SaplingsByTree.TryGetValue(tmpTree, out sapling))
          break;
      }
      _sectorObjects.Clear();
    }

    var saplingZdo = PlaceObject(zdo.ZDO.GetPosition(), sapling.name.GetStableHashCode(), zdo.ZDO.GetRotation(), CreatorMarkers.ProcessorOwned);
    saplingZdo.Fields<Plant>()
      .Set(static () => x => x.m_growRadius, 0)
      .Set(static () => x => x.m_destroyIfCantGrow, false)
      .Set(static () => x => x.m_needCultivatedGround, false)
      .Set(static () => x => x.m_growTime, sapling.m_growTime * Config.Instance.GrowingTimeMultiplier.Value)
      .Set(static () => x => x.m_growTimeMax, sapling.m_growTimeMax * Config.Instance.GrowingTimeMultiplier.Value);
    __invulnerableUntilTicks.Set(saplingZdo, DateTimeOffset.UtcNow.AddSeconds(Config.Instance.SaplingInvulnerabilitySeconds.Value).Ticks);
  }

  public sealed record PrefabInfo(Destructible Destructible) : ProcessorPrefabInfo
  {
    static Dictionary<GameObject, HashSet<TreeBase>>? __treesByStump;
    static Dictionary<TreeBase, Plant>? __saplingByTree;
    static HashSet<Plant>? __saplings;

    [MemberNotNullWhen(true, nameof(SaplingsByTree)), MemberNotNullWhen(false, nameof(Sapling))]
    public bool IsStump { get; private set; }
    public IReadOnlyDictionary<TreeBase, Plant>? SaplingsByTree { get; private set; } = default!;

    public Plant? Sapling { get; private set; }

    public override bool IsValid
    {
      get
      {
        if (PrefabInfo.Prefab is null)
          return false;

        if (__treesByStump is null || __saplingByTree is null || __saplings is null)
          Initialize();

        if (PrefabInfo.GetComponent<Plant>() is { } sapling && __saplings.Contains(sapling))
        {
          Sapling = sapling;
          return true;
        }

        if (!__treesByStump.TryGetValue(PrefabInfo.Prefab, out var trees))
          return false;

        Dictionary<TreeBase, Plant> saplingsByTree = new(trees.Count);
        foreach (var tree in trees)
        {
          if (__saplingByTree.TryGetValue(tree, out sapling))
            saplingsByTree.Add(tree, sapling);
        }
        if (saplingsByTree.Count is 0)
          return false;
        SaplingsByTree = saplingsByTree;
        IsStump = true;
        return true;

        [MemberNotNull(nameof(__treesByStump), nameof(__saplingByTree), nameof(__saplings))]
        static void Initialize()
        {
          __treesByStump = [];
          __saplingByTree = [];
          __saplings = [];
          foreach (var go in ZNetScene.instance.m_prefabs)
          {
            if (go.GetComponentInChildren<TreeBase>() is { m_stubPrefab: not null } treeBase)
            {
              if (!__treesByStump.TryGetValue(treeBase.m_stubPrefab, out var set))
                __treesByStump.Add(treeBase.m_stubPrefab, set = []);
              set.Add(treeBase);
            }
            else if (go.GetComponentInChildren<Plant>() is { m_grownPrefabs.Length: > 0 } plant)
            {
              foreach (var go2 in plant.m_grownPrefabs)
              {
                if (go2.GetComponentInChildren<TreeBase>() is not { m_stubPrefab: not null } treeBase2)
                  continue;
                __saplingByTree.Add(treeBase2, plant);
                __saplings.Add(plant);
              }
            }
          }
        }
      }
    }
  }
}
