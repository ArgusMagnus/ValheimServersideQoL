using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class GrowProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly Dictionary<ExtendedZDO, LastMessage> _lastMessage = new();

    sealed class LastMessage
    {
        public DateTimeOffset Timestamp { get; set; }
        public int Progress { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _lastMessage.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (zdo.PrefabInfo is not { EggGrow: not null } and not { Growup: not null } || !Config.Tames.ShowGrowingProgress.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (_lastMessage.TryGetValue(zdo, out var lastMessage) && (DateTimeOffset.UtcNow - lastMessage.Timestamp) < TimeSpan.FromSeconds(DamageText.instance.m_textDuration))
            return false;

        var growStart = zdo.PrefabInfo.EggGrow is not null ? zdo.Vars.GetGrowStart() : new TimeSpan(zdo.Vars.GetSpawnTime().Ticks).TotalSeconds;
        if (growStart is 0)
            return false;

        var growUpTime = zdo.PrefabInfo.EggGrow?.m_growTime ?? zdo.PrefabInfo.Growup!.m_growTime;
        var growTime = (float)(ZNet.instance.GetTimeSeconds() - growStart);
        var progress = (int)(100 * Mathf.Clamp01(growTime / growUpTime));
        if (lastMessage?.Progress == progress)
            return false;

        if (lastMessage is null)
            _lastMessage[zdo] = lastMessage = new();
        lastMessage.Timestamp = DateTimeOffset.UtcNow;
        lastMessage.Progress = progress;

        RPC.ShowInWorldText(peers, DamageText.TextType.Normal, zdo.GetPosition(), $"$caption_growing {progress}%");
        return false;
    }
}
