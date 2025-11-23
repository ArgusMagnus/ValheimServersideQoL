using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class MineRockProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("12157bfa-d940-45b9-b6c8-eb0460c3c053");
    readonly ZPackage _pkg = new();
    readonly List<(int Idx, float Health)> _notDestroyedIndices = [];

    bool _canCollapseBasedOnSkill;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _canCollapseBasedOnSkill = Config.Skills.PickaxeRockCollapseEnabled;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.MineRock5 is null ||!_canCollapseBasedOnSkill)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (zdo.Vars.GetHealthString() is not { Length: > 0 } healthData)
            return true;

        var minDist = ZoneSystem.c_ZoneHalfSize;
        PlayerProcessor.IPeerInfo? info = null;
        var skill = float.NaN;
        foreach (var peerInfo in Instance<PlayerProcessor>().PeerInfos)
        {
            if (peerInfo.LastUsedItem is not { m_itemData.m_shared.m_skillType: Skills.SkillType.Pickaxes })
                continue;
            if (float.IsNaN(skill = peerInfo.GetEstimatedSkillLevel(Skills.SkillType.Pickaxes)))
                continue;
            var dist = Vector3.Distance(peerInfo.PlayerZDO.GetPosition(), zdo.GetPosition());
            if (dist >= minDist)
                continue;
            info = peerInfo;
            minDist = dist;
        }

        if (info is null)
            return true;

        if (float.IsNaN(skill))
            skill = 0;
        var threshold = Utils.Lerp(Config.Skills.PickaxeRockCollapseThresholdAtMinSkill.Value, Config.Skills.PickaxeRockCollapseThresholdAtMaxSkill.Value, skill);
        threshold /= 100;
        if (threshold >= 1)
            return true;
        var destroy = threshold <= 0;
        if (!destroy)
        {
            /// <see cref="MineRock5.LoadHealth"/>
            _pkg.Load(Convert.FromBase64String(healthData));
            var count = _pkg.ReadInt();
            _notDestroyedIndices.Clear();
            for (int i = 0; i < count; i++)
            {
                var health = _pkg.ReadSingle();
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
        return true;
    }
}
