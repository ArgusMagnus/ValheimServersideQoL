﻿using BepInEx.Logging;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentDictionary<ExtendedZDO, DateTimeOffset> _openSince = new();

    public override void PreProcess()
    {
        base.PreProcess();
        foreach (var zdo in _openSince.Keys)
        {
            if (!zdo.IsValid() || zdo.PrefabInfo.Door is null)
                _openSince.TryRemove(zdo, out _);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Door is null || float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
        {
            zdo.Unregister(this);
            return false;
        }

        /// <see cref="Door.CanInteract"/>
        if (zdo.PrefabInfo.Door.m_keyItem is not null || zdo.PrefabInfo.Door.m_canNotBeClosed)
        {
            zdo.Unregister(this);
            return false;
        }

        if (!CheckMinDistance(peers, zdo, Config.Doors.AutoCloseMinPlayerDistance.Value))
            return false;

        const int StateClosed = 0;
        if (zdo.Vars.GetState() is StateClosed)
        {
            _openSince.TryRemove(zdo, out _);
            return true;
        }

        var openSince = _openSince.GetOrAdd(zdo, DateTimeOffset.UtcNow);
        if (DateTimeOffset.UtcNow - openSince < TimeSpan.FromSeconds(2))
            return false;

        zdo.Vars.SetState(StateClosed);
        _openSince.TryRemove(zdo, out _);

        return true;
    }
}