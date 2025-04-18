using BepInEx.Logging;
using System.Collections.Concurrent;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentDictionary<ExtendedZDO, DateTimeOffset> _openSince = new();

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _openSince.TryRemove(zdo, out _);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Door is null || float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        /// <see cref="Door.CanInteract"/>
        if (zdo.PrefabInfo.Door.m_keyItem is not null || zdo.PrefabInfo.Door.m_canNotBeClosed)
        {
            UnregisterZdoProcessor = true;
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