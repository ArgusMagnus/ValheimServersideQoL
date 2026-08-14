using UnityEngine;
using static Skills;

namespace ServersideQoL.Skills;

[Processor("df8713af-a556-4351-9726-5826e791314f")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class MineRockProcessor : Processor<MineRockProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(MineRock5 MineRock5) : ProcessorPrefabInfo;

  readonly List<(int Idx, float Health)> _notDestroyedIndices = [];

  protected override void Initialize()
  {
    Instance<PlayerRegistryProcessor>().EnableSkillLevelEstimation(Config.Instance.PickaxeRockCollapseEnabled);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!Config.Instance.PickaxeRockCollapseEnabled)
      return ProcessResult.UnregisterProcessor;

    if (zdo.Vars.GetHealthString() is not { Length: > 0 } healthData)
      return default;

    var minDist = ZoneSystem.c_ZoneSizeHalf;
    PlayerState? playerState = null;
    var skill = float.NaN;
    foreach (var state in Instance<PlayerRegistryProcessor>().PlayerStates)
    {
      if (state.LastUsedItem is not { m_itemData.m_shared.m_skillType: SkillType.Pickaxes })
        continue;
      if (float.IsNaN(skill = state.GetEstimatedSkillLevel( SkillType.Pickaxes)))
        continue;
      var dist = Vector3.Distance(state.ZDO.ZDO.GetPosition(), zdo.ZDO.GetPosition());
      if (dist >= minDist)
        continue;
      playerState = state;
      minDist = dist;
    }

    if (playerState is null)
      return default;

    if (float.IsNaN(skill))
      skill = 0;
    var threshold = Utils.Lerp(Config.Instance.PickaxeRockCollapseThresholdAtMinSkill.Value, Config.Instance.PickaxeRockCollapseThresholdAtMaxSkill.Value, skill);
    threshold /= 100;
    if (threshold >= 1)
      return default;
    var destroy = threshold <= 0;
    if (!destroy)
    {
      /// <see cref="MineRock5.LoadHealth"/>
      SingletonCache<ZPackage>.Instance.Load(Convert.FromBase64String(healthData));
      var count = SingletonCache<ZPackage>.Instance.ReadInt();
      _notDestroyedIndices.Clear();
      for (int i = 0; i < count; i++)
      {
        var health = SingletonCache<ZPackage>.Instance.ReadSingle();
        if (health > 0)
          _notDestroyedIndices.Add((i, health));
      }

      var destroyed = (float)(count - _notDestroyedIndices.Count) / count;
      destroy = destroyed >= threshold;
    }

    if (destroy)
    {
      var hit = new HitData();
      foreach (var (idx, health) in _notDestroyedIndices)
      {
        /// <see cref="MineRock5.CheckSupport"/>
        hit.m_damage.m_damage = health;
        hit.m_toolTier = short.MaxValue;
        hit.m_hitType = HitData.HitType.Structural;
        RPC.DamageMineRock5(zdo, hit, idx);
      }
    }
    return default;
  }
}
