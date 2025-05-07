using BepInEx.Logging;
using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor : Processor
{
    readonly Dictionary<ExtendedZDO, DateTimeOffset> _lastMessage = new();
    readonly List<ExtendedZDO> _tames = new();
    public IReadOnlyList<ExtendedZDO> Tames => _tames;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _lastMessage.Remove(zdo);
        _tames.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Tameable is null)
            return false;

        var (tameable, _) = zdo.PrefabInfo.Tameable.Value;

        var fields = zdo.Fields<Tameable>();
        if (zdo.Vars.GetTamed())
        {
            if (!Config.Tames.MakeCommandable.Value)
                fields.Reset(x => x.m_commandable);
            else if (fields.SetIfChanged(x => x.m_commandable, true))
                RecreateZdo = true;

            if (!Config.Tames.AlwaysFed.Value)
                fields.Reset(x => x.m_fedDuration);
            else if (fields.SetIfChanged(x => x.m_fedDuration, float.MaxValue))
                RecreateZdo = true;

            if (Config.Summons.UnsummonDistanceMultiplier.Value is 1f)
                fields.Reset(x => x.m_unsummonDistance);
            else if (fields.SetIfChanged(x => x.m_unsummonDistance, tameable.m_unsummonDistance * Config.Summons.UnsummonDistanceMultiplier.Value))
                RecreateZdo = true;

            if (Config.Summons.UnsummonLogoutTimeMultiplier.Value is 1f)
                fields.Reset(x => x.m_unsummonOnOwnerLogoutSeconds);
            else if (fields.SetIfChanged(x => x.m_unsummonOnOwnerLogoutSeconds, tameable.m_unsummonOnOwnerLogoutSeconds * Config.Summons.UnsummonLogoutTimeMultiplier.Value))
                RecreateZdo = true;

            if (!RecreateZdo)
                _tames.Add(zdo);
        }
        else if (Config.Tames.ShowTamingProgress.Value)
        {
            UnregisterZdoProcessor = false;

            /// <see cref="Tameable.GetRemainingTime()"/>
            var tameTime = fields.GetFloat(x => x.m_tamingTime);
            var tameTimeLeft = zdo.Vars.GetTameTimeLeft(tameTime);
            if (tameTimeLeft < tameTime)
            {
                if (!_lastMessage.TryGetValue(zdo, out var lastMessage) || (DateTimeOffset.UtcNow - lastMessage) > TimeSpan.FromSeconds(DamageText.instance.m_textDuration))
                {
                    _lastMessage[zdo] = DateTimeOffset.UtcNow;
                    var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
                    RPC.ShowInWorldText(peers, DamageText.TextType.Normal, zdo.GetPosition(), $"$hud_tameness {tameness:P0}");
                }
            }
        }

        return true;
    }
}
