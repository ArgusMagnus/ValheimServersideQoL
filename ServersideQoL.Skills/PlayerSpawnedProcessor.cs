using ServersideQoL.Processors;
using UnityEngine;
using static Skills;

namespace ServersideQoL.Skills;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
public sealed class PlayerSpawnedProcessor : Processor<PlayerSpawnedProcessor.PrefabInfo>
{
  public const string Id = "b41745c8-07ee-4ae1-897f-68391c136102";

  public sealed record PrefabInfo(Humanoid Humanoid, MonsterAI MonsterAI, Tameable? Tameable) : ProcessorPrefabInfo
  {
    public override bool IsValid => Humanoid.m_faction is Character.Faction.Players or Character.Faction.PlayerSpawned;
  }

  bool _enabled;

  readonly Dictionary<string, SpawnInfo> _spawnInfo = [];
  readonly Dictionary<int, List<ServersideQoLZDO>> _spawnedByPrefab = [];
  readonly Dictionary<ServersideQoLZDO, SpawnedState> _spawnedStates = [];
  PlayerState? _lastSummoningPlayer;

  protected override void Initialize()
  {
    Instance<PlayerRegistryProcessor>().EnableSkillLevelEstimation(_enabled =
      Config.Instance.BloodMagic.SummonsLevelUpEnabled ||
      Config.Instance.BloodMagic.MakeSummonsFriendlyEnabled ||
      Config.Instance.BloodMagic.MakeSummonsTolerateLavaEnabled ||
      Config.Instance.BloodMagic.SummonsHPRegenMultiplierEnabled ||
      Config.Instance.BloodMagic.SummonsSpeedMultiplierEnabled);

    foreach (var list in _spawnedByPrefab.Values)
      list.Clear();

    _spawnedStates.Clear();
    _lastSummoningPlayer = null;
    
    RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger,
        Config.Instance.BloodMagic.AllowReplacementSummonMinSkill.Value <= 100 || Config.Instance.BloodMagic.MakeSummonsFriendlyEnabled);

    if (_spawnInfo.Count is 0)
    {
      if (Config.Instance.BloodMagic.AllowReplacementSummonMinSkill.Value <= 100 || Config.Instance.BloodMagic.MakeSummonsFriendlyEnabled)
      {
        foreach (var item in ObjectDB.instance.m_items.Select(static x => x.GetComponent<ItemDrop>()))
        {
          var attack = item.m_itemData.m_shared.m_attack;
          if (attack.m_attackProjectile?.GetComponent<SpawnAbility>() is not { } spawnAbility)
            continue;

          Dictionary<int, List<ServersideQoLZDO>> dict = [];
          foreach (var prefab in spawnAbility.m_spawnPrefab)
          {
            if (prefab.GetComponent<Humanoid>() is not { m_faction: Character.Faction.Players or Character.Faction.PlayerSpawned } humanoid)
              continue;

            var hash = prefab.name.GetStableHashCode();
            if (!_spawnedByPrefab.TryGetValue(hash, out var list))
              _spawnedByPrefab.Add(hash, list = []);
            dict.Add(hash, list);
          }
          if (dict.Count > 0)
            _spawnInfo.Add(attack.m_attackAnimation, new(spawnAbility.m_maxSpawned, spawnAbility.m_maxSummonReached, dict));
        }

        foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
        {
          if (!_spawnedByPrefab.TryGetValue(zdo.ZDO.GetPrefab(), out var list) || list.Contains(zdo))
            continue;
          list.Add(zdo);
          zdo.Destroyed += x => list.Remove(x);
        }

        foreach (var list in _spawnedByPrefab.Values)
          SortBySpawnTime(list);
      }
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!_spawnedStates.TryGetValue(zdo, out var state))
    {
      _spawnedStates.Add(zdo, state = new());
      zdo.Destroyed += x => _spawnedStates.Remove(x);

      if (prefabInfo.Tameable is null)
      {
        if (!_spawnedByPrefab.TryGetValue(zdo.ZDO.GetPrefab(), out var list))
          _spawnedByPrefab.Add(zdo.ZDO.GetPrefab(), list = []);
        if (!list.Contains(zdo))
        {
          list.Add(zdo);
          SortBySpawnTime(list);
          zdo.Destroyed += x => list.Remove(x);
        }
      }
    }

    if (state.Summoner is null)
    {
      var playerName = zdo.Vars.GetFollow();
      if (!string.IsNullOrEmpty(playerName))
        state.Summoner = Instance<PlayerRegistryProcessor>().PlayerStates.FirstOrDefault(x => x.PlayerName == playerName);
      state.Summoner ??= _lastSummoningPlayer;
      if (state.Summoner is not null && string.IsNullOrEmpty(playerName))
        zdo.Vars.SetFollow(playerName = state.Summoner.PlayerName);
    }

    var tamed = zdo.Vars.GetTamed();

    var result = EvaluateChances(zdo, state, tamed, prefabInfo);

    var follow = tamed && Config.Instance.BloodMagic.MakeFriendlySummonsFollow.Value;
    if (follow && prefabInfo.Tameable is null)
    {
      var cfg = Config.Instance.Advanced.Value.BloodMagic.FollowSummoners;
      var fields = zdo.Fields<MonsterAI>();
      if (fields.UpdateValue(static () => x => x.m_randomMoveInterval, cfg.MoveInterval))
        result |= ProcessResult.RecreateZDO;
      if (fields.UpdateValue(static () => x => x.m_randomMoveRange, cfg.MaxDistance))
        result |= ProcessResult.RecreateZDO;

      result &= ~ProcessResult.UnregisterProcessor;

      if (state.Summoner is not null &&
          DateTimeOffset.UtcNow is { } now && now > state.NextPatrolPointUpdate &&
          Utils.DistanceXZ(zdo.ZDO.GetPosition(), state.Summoner.ZDO.ZDO.GetPosition()) > cfg.MaxDistance / 2)
      {
        state.NextPatrolPointUpdate = now.AddSeconds(cfg.MoveInterval / 2);
        var rev = zdo.ZDO.DataRevision;
        zdo.Vars.SetSpawnPoint(state.Summoner.ZDO.ZDO.GetPosition());
        zdo.Vars.SetPatrol(true);
        zdo.Vars.SetPatrolPoint(state.Summoner.ZDO.ZDO.GetPosition());
        if (rev != zdo.ZDO.DataRevision) // values changed
          zdo.ZDO.DataRevision += 100;
      }
    }

    return result;
  }

  static void SortBySpawnTime(List<ServersideQoLZDO> list)
  {
    list.Sort(static (a, b) => Math.Sign(a.Vars.GetSpawnTime().Ticks - b.Vars.GetSpawnTime().Ticks));
  }

  ProcessResult EvaluateChances(ServersideQoLZDO zdo, SpawnedState state, bool tamed, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;

    if (state.ChancesEvaluated)
      return result;
    state.ChancesEvaluated = true;
    if (state.Summoner is null)
      return result;

    var randomState = UnityEngine.Random.state;
    UnityEngine.Random.InitState(zdo.Vars.GetSeed());
    try
    {
      float? skill = null;
      if (!tamed && Config.Instance.BloodMagic.MakeSummonsFriendlyEnabled)
      {
        if (skill is null)
        {
          skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
          if (float.IsNaN(skill.Value))
            skill = 0;
        }
        var makeFriendlyChance = Mathf.RoundToInt(Utils.Lerp(Config.Instance.BloodMagic.MakeSummonsFriendlyChanceAtMinSkill.Value, Config.Instance.BloodMagic.MakeSummonsFriendlyChanceAtMaxSkill.Value, skill.Value));

        if (makeFriendlyChance >= 0 && UnityEngine.Random.Range(0, 100) <= makeFriendlyChance)
        {
          result &= ~ProcessResult.UnregisterProcessor;
          RPC.SetTamed(zdo, true);
          zdo.Vars.SetTamed(true);
        }
      }

      if (Config.Instance.BloodMagic.SummonsLevelUpEnabled)
      {
        if (skill is null)
        {
          skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
          if (float.IsNaN(skill.Value))
            skill = 0;
        }
        var levelUpChance = Mathf.RoundToInt(Utils.Lerp(Config.Instance.BloodMagic.SummonsLevelUpChanceAtMinSkill.Value, Config.Instance.BloodMagic.SummonsLevelUpChanceAtMaxSkill.Value, skill.Value));

        var level = 1;
        while (level < Config.Instance.BloodMagic.SummonsMaxLevel.Value && UnityEngine.Random.Range(0f, 100f) <= levelUpChance)
          level++;
        if (level != zdo.Vars.GetLevel())
        {
          zdo.Vars.SetLevel(level);
          result |= ProcessResult.RecreateZDO;
        }
      }

      if (Config.Instance.BloodMagic.MakeSummonsTolerateLavaEnabled)
      {
        if (skill is null)
        {
          skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
          if (float.IsNaN(skill.Value))
            skill = 0;
        }
        var tolerateLavaChance = Mathf.RoundToInt(Utils.Lerp(Config.Instance.BloodMagic.MakeSummonsTolerateLavaChanceAtMinSkill.Value, Config.Instance.BloodMagic.MakeSummonsTolerateLavaChanceAtMaxSkill.Value, skill.Value));

        if (!prefabInfo.Humanoid.m_tolerateFire &&
            zdo.Fields<Humanoid>().UpdateValue(static () => x => x.m_tolerateFire, UnityEngine.Random.Range(0, 100) <= tolerateLavaChance))
        {
          result |= ProcessResult.RecreateZDO;
        }
      }

      if (Config.Instance.BloodMagic.SummonsHPRegenMultiplierEnabled)
      {
        if (skill is null)
        {
          skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
          if (float.IsNaN(skill.Value))
            skill = 0;
        }
        var hpRegenMultiplier = Utils.Lerp(Config.Instance.BloodMagic.SummonsHPRegenMultiplierAtMinSkill.Value, Config.Instance.BloodMagic.SummonsHPRegenMultiplierAtMaxSkill.Value, skill.Value);

        if (zdo.Fields<Humanoid>().UpdateValue(static () => x => x.m_regenAllHPTime, prefabInfo.Humanoid.m_regenAllHPTime / hpRegenMultiplier))
          result |= ProcessResult.RecreateZDO;
        if (prefabInfo.Tameable is not null && zdo.Fields<Tameable>().UpdateValue(static () => x => x.m_fedDuration, float.PositiveInfinity))
          result |= ProcessResult.RecreateZDO;
      }

      if (Config.Instance.BloodMagic.SummonsSpeedMultiplierEnabled)
      {
        if (skill is null)
        {
          skill = state.Summoner.GetEstimatedSkillLevel(SkillType.BloodMagic);
          if (float.IsNaN(skill.Value))
            skill = 0;
        }
        var speedMultiplier = Utils.Lerp(Config.Instance.BloodMagic.SummonsSpeedMultiplierAtMinSkill.Value, Config.Instance.BloodMagic.SummonsSpeedMultiplierAtMaxSkill.Value, skill.Value);
        var fields = zdo.Fields<Humanoid>();
        if (fields.UpdateValue(static () => x => x.m_speed, prefabInfo.Humanoid.m_speed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_crouchSpeed, prefabInfo.Humanoid.m_crouchSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_flyFastSpeed, prefabInfo.Humanoid.m_flyFastSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_flySlowSpeed, prefabInfo.Humanoid.m_flySlowSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        //if (fields.UpdateValue(static () => x => x.m_flyTurnSpeed, prefabInfo.Humanoid.m_flyTurnSpeed * speedMultiplier))
        //    result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_groundTiltSpeed, prefabInfo.Humanoid.m_groundTiltSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_runSpeed, prefabInfo.Humanoid.m_runSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        //if (fields.UpdateValue(static () => x => x.m_runTurnSpeed, prefabInfo.Humanoid.m_runTurnSpeed * speedMultiplier))
        //    result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_swimSpeed, prefabInfo.Humanoid.m_swimSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
        //if (fields.UpdateValue(static () => x => x.m_swimTurnSpeed, prefabInfo.Humanoid.m_swimTurnSpeed * speedMultiplier))
        //    result |= ProcessResult.RecreateZDO;
        //if (fields.UpdateValue(static () => x => x.m_turnSpeed, prefabInfo.Humanoid.m_turnSpeed * speedMultiplier))
        //    result |= ProcessResult.RecreateZDO;
        if (fields.UpdateValue(static () => x => x.m_walkSpeed, prefabInfo.Humanoid.m_walkSpeed * speedMultiplier))
          result |= ProcessResult.RecreateZDO;
      }
    }
    finally { UnityEngine.Random.state = randomState; }

    return result;
  }

  /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
  void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
  {
    if (!_spawnInfo.TryGetValue(name, out var spawnInfo) || (_lastSummoningPlayer = Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID)) is null)
      return;

    if (!(Config.Instance.BloodMagic.AllowReplacementSummonMinSkill.Value <= _lastSummoningPlayer.GetEstimatedSkillLevel(SkillType.BloodMagic)))
      return;

    foreach (var list in spawnInfo.SpawnedByPrefab.Values)
    {
      if (list.Count < spawnInfo.MaxSpawned)
        continue;

      if (list[0].ZDO.GetOwner() == data.m_senderPeerID &&
          ZNetScene.InActiveArea(list[0].ZDO.GetSector(), _lastSummoningPlayer.ZDO.ZDO.GetSector()))
      {
        RPC.Damage(list[0], new(float.MaxValue) { m_attacker = _lastSummoningPlayer.ZDO.ZDO.m_uid });
      }
      else
      {
        list[0].Destroy(); // does not show death animation, but is faster and therefore more reliable
      }
      RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, spawnInfo.MaxSummonReached);
    }
  }

  sealed record SpawnInfo(int MaxSpawned, string MaxSummonReached, Dictionary<int, List<ServersideQoLZDO>> SpawnedByPrefab);

  sealed class SpawnedState
  {
    public PlayerState? Summoner { get; set; }
    public DateTimeOffset NextPatrolPointUpdate { get; set; }
    public bool ChancesEvaluated { get; set; }
  }
}
