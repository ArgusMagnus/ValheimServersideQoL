﻿using BepInEx.Logging;
using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class GrowProcessor : Processor
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
        if (!firstTime)
            return;

        _lastMessage.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo is not { EggGrow: not null } and not { Growup: not null } || Config.Tames.GrowingProgressMessageType.Value is MessageTypes.None)
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
        {
            _lastMessage.Add(zdo, lastMessage = new());
            zdo.Destroyed += x => _lastMessage.Remove(x);
        }
        lastMessage.Timestamp = DateTimeOffset.UtcNow;
        lastMessage.Progress = progress;

        ShowMessage(peers, zdo, $"$caption_growing {progress}%", Config.Tames.GrowingProgressMessageType.Value);
        return false;
    }
}
