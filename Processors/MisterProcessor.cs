using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class MisterProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
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
                foreach (var shieldGenerator in Instance<ShieldGeneratorProcessor>().ShieldGenerators)
                {
                    if (!(shieldGenerator.Vars.GetFuel() > 0))
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
        }

#if DEBUG
        if (fields.ResetIfChanged(x => x.m_height))
            RecreateZdo = true;
#endif

        return false;
    }
}
