using BepInEx.Logging;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentHashSet<ExtendedZDO> _shieldGenerators = new();
    readonly ConcurrentDictionary<ExtendedZDO, IEnumerable<ExtendedZDO>> _enclosure = new();

    public override void Initialize()
    {
        base.Initialize();
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    { 
        _shieldGenerators.Remove(zdo);
        if (_enclosure.TryRemove(zdo, out var enclosures))
        {
            foreach (var piece in enclosures)
                DestroyPiece(piece);
        }
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
            /// <see cref="Fireplace.CheckWet"/>
            if (fields.SetIfChanged(x => x.m_coverCheckOffset, offset))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_disableCoverCheck, true))
                RecreateZdo = true;
            if (!RecreateZdo && !_enclosure.ContainsKey(zdo))
                _enclosure.TryAdd(zdo, [.. PlaceEnclosure(zdo.GetPosition(), offset, zdo.PrefabInfo.Fireplace.m_coverCheckOffset)]);
        }
        else
        {
            if (fields.ResetIfChanged(x => x.m_coverCheckOffset))
                RecreateZdo = true;
            if (fields.ResetIfChanged(x => x.m_disableCoverCheck))
                RecreateZdo = true;
            if (_enclosure.TryRemove(zdo, out var enclosures))
            {
                foreach (var piece in enclosures)
                    DestroyPiece(piece);
            }
        }

        if (Config.Fireplaces.IgnoreRain.Value is ModConfig.FireplacesConfig.IgnoreRainOptions.InsideShield)
        {
            UnregisterZdoProcessor = false;
            return false;
        }

        return true;
    }

    IEnumerable<ExtendedZDO> PlaceEnclosure(Vector3 pos, float offset, float coverCheckOffset)
    {
        /// <see cref="Cover.GetCoverForPoint(Vector3, out float, out bool, float)"/>
        var y = pos.y + offset + coverCheckOffset + 0.5f + 0.5f;
        yield return PlacePiece(pos with { y = y }, Prefabs.GraustenFloor4x4, 0);
        y -= 2.25f;
        yield return PlacePiece(pos with { z = pos.z - 2, y = y }, Prefabs.GraustenWall4x2, 0);
        yield return PlacePiece(pos with { x = pos.x - 2, y = y }, Prefabs.GraustenWall4x2, 90);
        yield return PlacePiece(pos with { z = pos.z + 2, y = y }, Prefabs.GraustenWall4x2, 0);
        yield return PlacePiece(pos with { x = pos.x + 2, y = y }, Prefabs.GraustenWall4x2, 90);
        while (y > pos.y)
        {
            y -= 2;
            yield return PlacePiece(pos with { z = pos.z - 2, y = y }, Prefabs.GraustenWall4x2, 0);
            yield return PlacePiece(pos with { x = pos.x - 2, y = y }, Prefabs.GraustenWall4x2, 90);
            yield return PlacePiece(pos with { z = pos.z + 2, y = y }, Prefabs.GraustenWall4x2, 0);
            yield return PlacePiece(pos with { x = pos.x + 2, y = y }, Prefabs.GraustenWall4x2, 90);
        }
        y -= 0.25f;
        yield return PlacePiece(pos with { y = y }, Prefabs.GraustenFloor4x4, 0);
    }
}
