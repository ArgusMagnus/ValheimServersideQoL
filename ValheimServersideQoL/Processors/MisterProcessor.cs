using UnityEngine;
using Valheim.ServersideQoL.HarmonyPatches;
using static Valheim.ServersideQoL.ModConfigBase.WorldConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class MisterProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("bc1174a9-43c5-4594-8754-bc059bbee284");

    const string QueenDefeatedKey = "defeated_queen";
    bool _queenDefeated;
    readonly SectorDictionary<HashSet<ExtendedZDO>> _misters = new((ZoneSystem.instance.GetActiveArea() * 2 + 1) * ZoneSystem.c_ZoneSize);
    const bool MistWeather = true;
    bool _updateMistWeather;
    DateTimeOffset _nextWeatherUpdate;

    static float MistAtPosition(Vector3 pos)
    {
        const float Zones = 20;
        const float Hours = 1f;
        const float MaxSpatialOffset = 0;
        const float ClearThreshold = 0.1f;
        const float DenseThreshold = 0.5f;

        const float SpatialScale = 1 / (Zones * ZoneSystem.c_ZoneSize);
        var local = Mathf.PerlinNoise(pos.x * SpatialScale, pos.z * SpatialScale);

        const float TemporalScale = 1 / Hours;
        const float HoursPerSecond = 1f / (TimeSpan.TicksPerHour / TimeSpan.TicksPerSecond);
        var factor = Mathf.PerlinNoise1D(((float)(ZNet.instance.GetTimeSeconds() * HoursPerSecond) + local * MaxSpatialOffset) * TemporalScale);
        return Mathf.InverseLerp(ClearThreshold, DenseThreshold, factor);
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _queenDefeated = ZoneSystem.instance.GetGlobalKey(QueenDefeatedKey);
        _misters.Clear();
        _updateMistWeather = false;

        ZoneSystemSendGlobalKeys.GlobalKeysChanged -= OnGlobalKeysChanged;
        if (!_queenDefeated)
            ZoneSystemSendGlobalKeys.GlobalKeysChanged += OnGlobalKeysChanged;

        Instance<ShieldGeneratorProcessor>().ShieldGeneratorChanged -= OnShieldGeneratorChanged;
        if (Config.World.RemoveMistlandsMist.Value is RemoveMistlandsMistOptions.InsideShield)
            Instance<ShieldGeneratorProcessor>().ShieldGeneratorChanged += OnShieldGeneratorChanged;
    }

    void OnGlobalKeysChanged()
    {
        _queenDefeated = ZoneSystem.instance.GetGlobalKey(QueenDefeatedKey);
        if (!_queenDefeated)
            return;
        Instance<ShieldGeneratorProcessor>().ShieldGeneratorChanged -= OnShieldGeneratorChanged;
        foreach (var zdo in _misters.Values.SelectMany(static x => x))
            zdo.ResetProcessorDataRevision(this);
        _misters.Clear();
    }

    void OnShieldGeneratorChanged(ExtendedZDO shieldGenerator, bool hasFuel)
    {
        if (!hasFuel)
            return;

        foreach (var misters in _misters.EnumerateAdjacent(shieldGenerator.GetPosition()))
        {
            foreach (var zdo in misters)
                zdo.ResetProcessorDataRevision(this);
        }
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        base.PreProcessCore(peers);
        if (MistWeather)
        {
            var now = DateTimeOffset.UtcNow;
            _updateMistWeather = _nextWeatherUpdate < now;
            if (_updateMistWeather)
                _nextWeatherUpdate = now.AddSeconds(2);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Mister is null)
            return false;

        var radius = zdo.PrefabInfo.Mister.m_radius;
        var fields = zdo.Fields<Mister>();
        switch (Config.World.RemoveMistlandsMist.Value)
        {
            case RemoveMistlandsMistOptions.Never:
                break;

            case RemoveMistlandsMistOptions.Always:
                if (fields.UpdateValue(static () => x => x.m_radius, float.MinValue))
                    RecreateZdo = true;
                return false;

            case RemoveMistlandsMistOptions.AfterQueenKilled:
                if (_queenDefeated)
                {
                    if (fields.UpdateValue(static () => x => x.m_radius, float.MinValue))
                        RecreateZdo = true;
                    return false;
                }
                else
                {
                    _misters.TryAdd(zdo);
                    break;
                }

            case RemoveMistlandsMistOptions.InsideShield:
                UnregisterZdoProcessor = false;
                var maxRadius = float.PositiveInfinity;
                var range = Mathf.Max(ParticleMist.instance.m_localRange, ParticleMist.instance.m_distantMaxRange);
                foreach (var (shieldGenerator, hasFuel) in Instance<ShieldGeneratorProcessor>().ShieldGenerators)
                {
                    if (!hasFuel)
                        continue;
                    var dist = Vector3.Distance(shieldGenerator.GetPosition(), zdo.GetPosition());
                    maxRadius = Mathf.Min(maxRadius, dist - shieldGenerator.PrefabInfo.ShieldGenerator!.m_maxShieldRadius - range);
                }

                if (radius > maxRadius)
                    radius = maxRadius;
                else
                    _misters.TryAdd(zdo);
                break;
        }

        if (!MistWeather)
        {
            if (fields.UpdateValue(static () => x => x.m_radius, radius))
                RecreateZdo = true;
            return true;

        }

        UnregisterZdoProcessor = false;
        if (!_updateMistWeather)
            return false;

        var factor = MistAtPosition(zdo.GetPosition());
        var r = radius * factor;
        if (Mathf.Abs(r - fields.GetFloat(static () => x => x.m_radius)) > 1 && fields.UpdateValue(static () => x => x.m_radius, r))
        {
            RecreateZdo = true;
            Logger.DevLog($"Recreating {zdo.PrefabInfo.PrefabName} at {zdo.GetPosition()}: f={factor} r={r} radius={radius}");
        }
        return false;
    }
}
