using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class MisterProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly Dictionary<ExtendedZDO, float> _misters = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _misters.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Mister is null)
            return false;

        var fields = zdo.Fields<Mister>();
        switch (Config.World.RemoveMistlandsMist.Value)
        {
            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.Never:
                if (fields.ResetIfChanged(x => x.m_radius))
                    RecreateZdo = true;
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.Always:
                if (fields.SetIfChanged(x => x.m_radius, float.MinValue))
                    RecreateZdo = true;
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.AfterQueenKilled:
                if (ZoneSystem.instance.GetGlobalKey("defeated_queen"))
                {
                    if (fields.SetIfChanged(x => x.m_radius, float.MinValue))
                        RecreateZdo = true;
                }
                else
                {
                    UnregisterZdoProcessor = false;
                    if (fields.ResetIfChanged(x => x.m_radius))
                        RecreateZdo = true;
                }
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.InsideShield:
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

                if (zdo.PrefabInfo.Mister.m_radius > maxRadius)
                {
                    if (fields.SetIfChanged(x => x.m_radius, maxRadius))
                        RecreateZdo = true;
                }
                else
                {
                    if (fields.ResetIfChanged(x => x.m_radius))
                        RecreateZdo = true;
                }
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.DynamicTimeBased:
                UnregisterZdoProcessor = false;
                var p = zdo.GetPosition();
                var f = 0.5f + 0.5f * Mathf.Sin(((float)ZNet.instance.GetTimeSeconds() + p.x + p.y + p.z) / Mathf.PI * 0.25f);
                var radius = Mathf.Round(f * zdo.PrefabInfo.Mister.m_radius);
                if (!_misters.TryGetValue(zdo, out var oldRadius) || radius != oldRadius)
                {
                    _misters[zdo] = radius;
                    if (fields.SetIfChanged(x => x.m_radius, Mathf.Round(radius)))
                        RecreateZdo = true;
                }
                break;
        }

#if DEBUG
        if (fields.ResetIfChanged(x => x.m_height))
            RecreateZdo = true;
#endif

        return false;
    }
}
