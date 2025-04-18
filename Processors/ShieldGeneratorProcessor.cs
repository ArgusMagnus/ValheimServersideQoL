﻿using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class ShieldGeneratorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly Dictionary<ExtendedZDO, bool> _shieldGenerators = [];

    public readonly record struct ShieldGeneratorInfo(ExtendedZDO ShieldGenerator, bool HasFuel);
    IReadOnlyList<ShieldGeneratorInfo>? _info;
    public IReadOnlyList<ShieldGeneratorInfo> ShieldGenerators => _info ??= [.. _shieldGenerators.Select(x => new ShieldGeneratorInfo(x.Key, x.Value))];

    public delegate void ShieldGeneratorChangedHandler(ExtendedZDO shieldGenerator, bool hasFuel);
    public event ShieldGeneratorChangedHandler? ShieldGeneratorChanged;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        if (_shieldGenerators.Remove(zdo))
            _info = null;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.ShieldGenerator is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var hasFuel = zdo.Vars.GetFuel() > 0;
        if (!_shieldGenerators.TryGetValue(zdo, out var oldFuel) || hasFuel != oldFuel)
        {
            _shieldGenerators[zdo] = hasFuel;
            _info = null;
            ShieldGeneratorChanged?.Invoke(zdo, hasFuel);
        }

        return true;
    }
}
