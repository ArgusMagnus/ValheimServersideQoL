using BepInEx.Logging;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentHashSet<ExtendedZDO> _shieldGenerators = new();
    readonly ConcurrentDictionary<ExtendedZDO, ExtendedZDO> _roofs = new();

    public override void Initialize()
    {
        base.Initialize();
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    { 
        _shieldGenerators.Remove(zdo);
        if (_roofs.TryRemove(zdo, out var roof))
            DestroyPiece(roof);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.ShieldGenerator is not null)
            _shieldGenerators.Add(zdo);

        if (zdo.PrefabInfo.Fireplace is null)
            return false;

        var fields = zdo.Fields<Fireplace>();
        if (!Config.Fireplaces.MakeToggleable.Value)
            fields.Reset(x => x.m_canTurnOff);
        else if (fields.SetIfChanged(x => x.m_canTurnOff, true))
            RecreateZdo = true;

        if (!Config.Fireplaces.InfiniteFuel.Value)
            fields.Reset(x => x.m_secPerFuel).Reset(x => x.m_canRefill);
        else
        {
            if (fields.SetIfChanged(x => x.m_secPerFuel, 0))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_canRefill, false))
                RecreateZdo = true;
            zdo.Vars.SetFuel(fields.GetFloat(x => x.m_maxFuel));
        }

        var ignoreRain = Config.Fireplaces.IgnoreRain.Value switch
        {
            ModConfig.FireplacesConfig.IgnoreRainOptions.Never => false,
            ModConfig.FireplacesConfig.IgnoreRainOptions.Always => true,
            ModConfig.FireplacesConfig.IgnoreRainOptions.InsideShield => _shieldGenerators
                .Any(x => x.Vars.GetFuel() > 0 && Vector3.Distance(x.GetPosition(), zdo.GetPosition()) < x.Fields<ShieldGenerator>().GetFloat(x => x.m_maxShieldRadius)),
            _ => false
        };

        const float offset = -100;
        if (ignoreRain)
        {
            if (fields.SetIfChanged(x => x.m_coverCheckOffset, offset))
                RecreateZdo = true;
            if (!RecreateZdo && !_roofs.ContainsKey(zdo))
            {
                var pos = zdo.GetPosition();
                pos.y += offset + zdo.PrefabInfo.Fireplace.m_coverCheckOffset + 0.5f + 2;
                _roofs.TryAdd(zdo, PlacePiece(pos, Prefabs.GraustenFloor4x4, 0));
            }
        }
        else
        {
            if (fields.ResetIfChanged(x => x.m_coverCheckOffset))
                RecreateZdo = true;
            if (_roofs.TryRemove(zdo, out var roof))
                DestroyPiece(roof);
        }

        if (Config.Fireplaces.IgnoreRain.Value is ModConfig.FireplacesConfig.IgnoreRainOptions.InsideShield)
        {
            UnregisterZdoProcessor = false;
            return false;
        }

        return true;
    }
}
