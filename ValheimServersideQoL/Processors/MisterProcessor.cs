using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class MisterProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("bc1174a9-43c5-4594-8754-bc059bbee284");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Mister is null)
            return false;

        var fields = zdo.Fields<Mister>();
        switch (Config.World.RemoveMistlandsMist.Value)
        {
            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.Never:
                if (fields.UpdateResetValue(static () => x => x.m_radius))
                    RecreateZdo = true;
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.Always:
                if (fields.UpdateValue(static () => x => x.m_radius, float.MinValue))
                    RecreateZdo = true;
                break;

            case ModConfig.WorldConfig.RemoveMistlandsMistOptions.AfterQueenKilled:
                if (ZoneSystem.instance.GetGlobalKey("defeated_queen"))
                {
                    if (fields.UpdateValue(static () => x => x.m_radius, float.MinValue))
                        RecreateZdo = true;
                }
                else
                {
                    UnregisterZdoProcessor = false;
                    if (fields.UpdateResetValue(static () => x => x.m_radius))
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
                    if (fields.UpdateValue(static () => x => x.m_radius, maxRadius))
                        RecreateZdo = true;
                }
                else
                {
                    if (fields.UpdateResetValue(static () => x => x.m_radius))
                        RecreateZdo = true;
                }
                break;
        }

#if DEBUG
        if (fields.UpdateResetValue(static () => x => x.m_height))
            RecreateZdo = true;
#endif

        return false;
    }
}
