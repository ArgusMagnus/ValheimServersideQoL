namespace Valheim.ServersideQoL.Processors;

sealed class HumanoidLevelProcessor : Processor
{
    readonly IReadOnlyList<int> _statusEffects = [
        //SEMan.s_statusEffectBurning, SEMan.s_statusEffectLightning, SEMan.s_statusEffectPoison,
        SEMan.s_statusEffectSpirit];
    sealed class HumanoidState(int statusEffect)
    {
        public int StatusEffect { get; } = statusEffect;
        public TimeSpan Duration { get; } = TimeSpan.FromSeconds(ObjectDB.instance.GetStatusEffect(statusEffect).m_ttl - 0.2f);
        public DateTimeOffset LastApplied { get; set; }
    }
    readonly Dictionary<ExtendedZDO, HumanoidState> _states = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _states.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Humanoid is null)
            return false;

        var level = zdo.Vars.GetLevel();
        if (level <= 3)
            return false;

        UnregisterZdoProcessor = false;
        if (!_states.TryGetValue(zdo, out var state))
        {
            var fields = zdo.Fields<Humanoid>();
            if (!Config.Creatures.ShowHigherLevelStars.Value)
                fields.Reset(x => x.m_name);
            else if (fields.SetIfChanged(x => x.m_name, $"<line-height=150%><voffset=-2em>{zdo.PrefabInfo.Humanoid.Value.Humanoid.m_name}<size=70%><br><color=yellow>{string.Concat(Enumerable.Repeat("⭐", level - 1))}</color></size></voffset></line-height>"))
                RecreateZdo = true;

            if (!RecreateZdo)
            {
                var tamed = zdo.Vars.GetTamed();
                if ((tamed && Config.Creatures.ShowHigherLevelAura.Value.HasFlag(ModConfig.CreaturesConfig.ShowHigherLevelAuraOptions.Tamed)) ||
                    (!tamed && Config.Creatures.ShowHigherLevelAura.Value.HasFlag(ModConfig.CreaturesConfig.ShowHigherLevelAuraOptions.Wild)))
                {
                    _states.Add(zdo, state = new(_statusEffects[UnityEngine.Random.Range(0, _statusEffects.Count)]));
                    zdo.Destroyed += x => _states.Remove(x);
                }
            }
        }

        if (state is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (DateTimeOffset.UtcNow - state.LastApplied > state.Duration)
        {
            state.LastApplied = DateTimeOffset.UtcNow;
            RPC.AddStatusEffect(zdo, state.StatusEffect, true);
        }

        return false;
    }
}