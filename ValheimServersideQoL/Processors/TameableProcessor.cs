using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor : Processor
{
    public interface ITameableState
    {
        ExtendedZDO ZDO { get; }
        bool IsTamed { get; }
    }

    sealed record TameableState(ExtendedZDO ZDO) : ITameableState
    {
        public bool IsTamed { get; set; }
        public DateTimeOffset LastMessage { get; set; }
    }

    readonly ConcurrentDictionary<ExtendedZDO, TameableState> _states = [];
    public IReadOnlyCollection<ITameableState> Tames => (IReadOnlyCollection<ITameableState>)_states.Values;

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
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
            {
                if (!_states.TryGetValue(zdo, out var state))
                {
                    _states.TryAdd(zdo, state = new(zdo));
                    zdo.Destroyed += x => _states.Remove(x, out _);
                }
                state.IsTamed = true;
            }
        }
        else if (Config.Tames.TamingProgressMessageType.Value is not MessageTypes.None)
        {
            UnregisterZdoProcessor = false;

            /// <see cref="Tameable.GetRemainingTime()"/>
            var tameTime = fields.GetFloat(x => x.m_tamingTime);
            var tameTimeLeft = zdo.Vars.GetTameTimeLeft(tameTime);
            if (tameTimeLeft < tameTime)
            {
                if (!_states.TryGetValue(zdo, out var state))
                {
                    _states.TryAdd(zdo, state = new(zdo));
                    zdo.Destroyed += x => _states.Remove(x, out _);
                }

                if ((DateTimeOffset.UtcNow - state.LastMessage) > TimeSpan.FromSeconds(DamageText.instance.m_textDuration))
                {
                    state.LastMessage = DateTimeOffset.UtcNow;
                    var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);

                    var message = $"$hud_tameness {tameness:P0}";

                    /// <see cref="Tameable.IsHungry()"/>
                    if ((ZNet.instance.GetTime() - zdo.Vars.GetTameLastFeeding()).TotalSeconds > fields.GetFloat(x => x.m_fedDuration))
                        message += ", $hud_tamehungry";

                    ShowMessage(peers, zdo, message, Config.Tames.TamingProgressMessageType.Value);
                }
            }
        }

        return true;
    }
}
