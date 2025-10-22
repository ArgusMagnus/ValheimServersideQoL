using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class FireplaceProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("a805afd0-ecd0-4c85-8d8a-6f2f14957b0a");

    readonly Dictionary<ExtendedZDO, IEnumerable<ExtendedZDO>> _enclosure = [];
    readonly List<ExtendedZDO> _fireplaces = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _enclosure.Clear();
        _fireplaces.Clear();

        Instance<ShieldGeneratorProcessor>().ShieldGeneratorChanged -= OnShieldGeneratorChanged;
        Instance<ShieldGeneratorProcessor>().ShieldGeneratorChanged += OnShieldGeneratorChanged;
    }

    void OnShieldGeneratorChanged(ExtendedZDO shieldGenerator, bool hasFuel)
    {
        foreach (var zdo in _fireplaces)
        {
            if (Vector3.Distance(shieldGenerator.GetPosition(), zdo.GetPosition()) < shieldGenerator.PrefabInfo.ShieldGenerator!.m_maxShieldRadius)
                zdo.ResetProcessorDataRevision(this);
        }
    }

    void OnFireplaceDestroyed(ExtendedZDO zdo)
    { 
        if (_enclosure.Remove(zdo, out var enclosures))
        {
            foreach (var piece in enclosures)
                DestroyObject(piece);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Fireplace is null)
            return false;

        var fields = zdo.Fields<Fireplace>();
        if (!Config.Fireplaces.MakeToggleable.Value)
            fields.Reset(static x => x.m_canTurnOff);
        else if (fields.SetIfChanged(static x => x.m_canTurnOff, true))
            RecreateZdo = true;

        if (!Config.Fireplaces.InfiniteFuel.Value)
            fields.Reset(static x => x.m_secPerFuel).Reset(static x => x.m_canRefill);
        else
        {
            if (fields.SetIfChanged(static x => x.m_secPerFuel, 0))
                RecreateZdo = true;
            if (fields.SetIfChanged(static x => x.m_canRefill, false))
                RecreateZdo = true;
            zdo.Vars.SetFuel(fields.GetFloat(static x => x.m_maxFuel));
        }

        /// Weather has no effect, <see cref="Fireplace.CheckEnv()"/>
        if (zdo.PrefabInfo.Fireplace is { m_enabledObjectLow: null } or { m_enabledObjectHigh: null })
            return false;

        var ignoreRain = Config.Fireplaces.IgnoreRain.Value switch
        {
            ModConfig.FireplacesConfig.IgnoreRainOptions.Never => false,
            ModConfig.FireplacesConfig.IgnoreRainOptions.Always => true,
            ModConfig.FireplacesConfig.IgnoreRainOptions.InsideShield => Instance<ShieldGeneratorProcessor>().ShieldGenerators
                .Any(x => x.HasFuel && Vector3.Distance(x.ShieldGenerator.GetPosition(), zdo.GetPosition()) < x.ShieldGenerator.PrefabInfo.ShieldGenerator!.m_maxShieldRadius),
            _ => false
        };

        const float offset = -100;
        if (ignoreRain)
        {
            /// <see cref="Fireplace.CheckWet"/>
            if (fields.SetIfChanged(static x => x.m_coverCheckOffset, offset))
                RecreateZdo = true;
            if (fields.SetIfChanged(static x => x.m_disableCoverCheck, true))
                RecreateZdo = true;
            if (!RecreateZdo && !_enclosure.ContainsKey(zdo))
            {
                _enclosure.Add(zdo, [.. PlaceEnclosure(zdo.GetPosition(), offset, zdo.PrefabInfo.Fireplace.m_coverCheckOffset)]);
                zdo.Destroyed += OnFireplaceDestroyed;
            }
        }
        else
        {
            if (fields.ResetIfChanged(static x => x.m_coverCheckOffset))
                RecreateZdo = true;
            if (fields.ResetIfChanged(static x => x.m_disableCoverCheck))
                RecreateZdo = true;
            if (_enclosure.Remove(zdo, out var enclosures))
            {
                foreach (var piece in enclosures)
                    DestroyObject(piece);
                zdo.Destroyed -= OnFireplaceDestroyed;
            }
        }

        if (Config.Fireplaces.IgnoreRain.Value is ModConfig.FireplacesConfig.IgnoreRainOptions.InsideShield)
        {
            UnregisterZdoProcessor = false;
            if (!_fireplaces.Contains(zdo))
                _fireplaces.Add(zdo);
            return true;
        }

        return false;
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
