using BepInEx.Logging;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly ConcurrentDictionary<ExtendedZDO, DateTimeOffset> _lastMessage = new();

    public override void PreProcess()
    {
        base.PreProcess();
        foreach (var zdo in _lastMessage.Keys)
        {
            if (!zdo.IsValid() || zdo.PrefabInfo.Tameable is null)
                _lastMessage.TryRemove(zdo, out _);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Tameable is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var fields = zdo.Fields<Tameable>();
        if (zdo.Vars.GetTamed())
        {
            if (fields.SetIfChanged(x => x.m_commandable, Config.Tames.MakeCommandable.Value))
                RecreateZdo = true;

            if (!Config.Tames.AlwaysFed.Value)
                fields.Reset(x => x.m_fedDuration);
            else if (fields.SetIfChanged(x => x.m_fedDuration, float.MaxValue))
                RecreateZdo = true;

            if (Config.Summons.UnsummonDistanceMultiplier.Value is 1f)
                fields.Reset(x => x.m_unsummonDistance);
            else if (fields.SetIfChanged(x => x.m_unsummonDistance, zdo.PrefabInfo.Tameable.m_unsummonDistance * Config.Summons.UnsummonDistanceMultiplier.Value))
                RecreateZdo = true;

            if (Config.Summons.UnsummonLogoutTimeMultiplier.Value is 1f)
                fields.Reset(x => x.m_unsummonOnOwnerLogoutSeconds);
            else if (fields.SetIfChanged(x => x.m_unsummonOnOwnerLogoutSeconds, zdo.PrefabInfo.Tameable.m_unsummonOnOwnerLogoutSeconds * Config.Summons.UnsummonLogoutTimeMultiplier.Value))
                RecreateZdo = true;

            if (!RecreateZdo && zdo.Vars.GetFollow() is { Length: > 0 } playerName)
                SharedProcessorState.FollowingTamesByPlayerName.GetOrAdd(playerName, static _ => new()).Add(zdo.m_uid);
        }
        else if (Config.Tames.ShowTamingProgress.Value)
        {
            /// <see cref="Tameable.GetRemainingTime()"/>
            var tameTime = fields.GetFloat(x => x.m_tamingTime);
            var tameTimeLeft = zdo.Vars.GetTameTimeLeft(tameTime);
            if (tameTimeLeft < tameTime)
            {
                if (!_lastMessage.TryGetValue(zdo, out var lastMessage) || (DateTimeOffset.UtcNow - lastMessage) > TimeSpan.FromSeconds(DamageText.instance.m_textDuration))
                {
                    _lastMessage[zdo] = DateTimeOffset.UtcNow;
                    var range = DamageText.instance.m_maxTextDistance;
                    var playersInRange = peers.Where(x => Vector3.Distance(x.m_refPos, zdo.GetPosition()) <= range);
                    var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
                    RPC.ShowInWorldText(playersInRange, DamageText.TextType.Normal, zdo.GetPosition(), $"$hud_tameness {tameness:P0}");
                }
            }
        }

        return true;
    }
}
