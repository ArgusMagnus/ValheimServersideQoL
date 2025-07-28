namespace Valheim.ServersideQoL.Processors;

sealed class CreatureLevelUpProcessor : Processor
{
    readonly Dictionary<Heightmap.Biome, int> _levelIncreasePerBiome = [];
    readonly List<string> _missingBossKeys = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        InitializeLevelIncreasePerBiome();
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        base.PreProcessCore(peers);

        if (_missingBossKeys.Any(static x => ZoneSystem.instance.GetGlobalKey(x)))
            InitializeLevelIncreasePerBiome();
    }

    void InitializeLevelIncreasePerBiome()
    {
        _levelIncreasePerBiome.Clear();
        _missingBossKeys.Clear();
        if (Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            var increase = 0;
            foreach (var (biome, boss) in SharedProcessorState.BossesByBiome.OrderBy(static x => x.Value.m_health))
            {
                if (!ZoneSystem.instance.GetGlobalKey(boss.m_defeatSetGlobalKey))
                    _missingBossKeys.Add(boss.m_defeatSetGlobalKey);
                else
                    increase += Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value;
                _levelIncreasePerBiome.Add(biome, increase);
            }
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo is { CreatureSpawner: null } and { Humanoid: null })
            return false;

        var increase = Config.Creatures.MaxLevelIncrease.Value;
        if (Config.Creatures.MaxLevelIncreasePerDefeatedBoss.Value > 0)
        {
            var biome = WorldGenerator.instance.GetBiome(zdo.GetPosition());
            if (_levelIncreasePerBiome.TryGetValue(biome, out var value))
                increase += value;
        }

        switch (zdo.PrefabInfo)
        {
            case { CreatureSpawner: not null }:

                var fields = zdo.Fields<CreatureSpawner>();

#if DEBUG
                if (fields.ResetIfChanged(static  x => x.m_respawnTimeMinuts))
                    RecreateZdo = true;
#endif

                if (zdo.PrefabInfo.CreatureSpawner.m_respawnTimeMinuts <= 0 && fields.SetIfChanged(static x => x.m_respawnTimeMinuts, Config.Creatures.RespawnOneTimeSpawnsAfter.Value))
                    RecreateZdo = true;

                var maxLevel = zdo.PrefabInfo.CreatureSpawner.m_maxLevel + increase;
                if (fields.SetIfChanged(static x => x.m_maxLevel, maxLevel))
                {
                    Logger.DevLog($"{zdo.PrefabInfo.PrefabName}: Set max level to {maxLevel} (+{increase})");
                    RecreateZdo = true;
                }

                var steps = maxLevel - zdo.PrefabInfo.CreatureSpawner.m_minLevel;
                if (Config.Creatures.MaxLevelChance.Value is 0 || steps is 0)
                {
                    if (fields.ResetIfChanged(static x => x.m_levelupChance))
                        RecreateZdo = true;
                }
                else
                {
                    var chance = Config.Creatures.MaxLevelChance.Value / 100.0;
                    chance = Math.Pow(chance, 1.0 / steps);
                    chance *= 100.0;
                    Logger.DevLog($"{zdo.PrefabInfo.PrefabName}: Set level up chance to {chance}");
                    if (fields.SetIfChanged(static x => x.m_levelupChance, (float)chance))
                        RecreateZdo = true;
                }
                break;

            //case { Humanoid: not null }:

            //    if (false) // todo: check if spawned by creaturespawner
            //        return false;

            //    var initialLevel = zdo.Vars.GetInitialLevel();
            //    if (initialLevel is 0)
            //    {

            //        zdo.Vars.SetInitialLevel(initialLevel);
            //    }

            //    if (zdo.PrefabInfo.Humanoid is { Humanoid.m_boss: true } && !Config.Creatures.LevelUpBosses.Value)
            //        increase = 0;

            //    break;
        }

        return false;
    }
}
