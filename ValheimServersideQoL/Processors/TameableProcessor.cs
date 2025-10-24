using System.Collections.Concurrent;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("fbc11c11-6193-4e9e-956d-615107d80682");

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

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        if (!firstTime)
            return;

        _states.Clear();
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (zdo.PrefabInfo.Tameable?.Tameable is not { } tameable)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var fields = zdo.Fields<Tameable>();

        if (Config.Tames.FedDurationMultiplier.Value is 1f)
            fields.Reset(static () => x => x.m_fedDuration);
        else if (fields.SetIfChanged(static () => x => x.m_fedDuration, tameable.m_fedDuration * Config.Tames.FedDurationMultiplier.Value))
            RecreateZdo = true;

        if (Config.Tames.TamingTimeMultiplier.Value is 1f)
            fields.Reset(static () => x => x.m_tamingTime);
        else if (fields.SetIfChanged(static () => x => x.m_tamingTime, tameable.m_tamingTime * Config.Tames.TamingTimeMultiplier.Value))
            RecreateZdo = true;

        if (Config.Tames.PotionTamingBoostMultiplier.Value is 1f)
            fields.Reset(static () => x => x.m_tamingBoostMultiplier);
        else if (fields.SetIfChanged(static () => x => x.m_tamingBoostMultiplier, tameable.m_tamingBoostMultiplier * Config.Tames.PotionTamingBoostMultiplier.Value))
            RecreateZdo = true;

        if (zdo.Vars.GetTamed())
        {
            UnregisterZdoProcessor = true;

            if (!Config.Tames.MakeCommandable.Value)
                fields.Reset(static () => x => x.m_commandable);
            else if (fields.SetIfChanged(static () => x => x.m_commandable, true))
                RecreateZdo = true;

            if (Config.Summons.UnsummonDistanceMultiplier.Value is 1f)
                fields.Reset(static () => x => x.m_unsummonDistance);
            else if (fields.SetIfChanged(static () => x => x.m_unsummonDistance, tameable.m_unsummonDistance * Config.Summons.UnsummonDistanceMultiplier.Value))
                RecreateZdo = true;

            if (Config.Summons.UnsummonLogoutTimeMultiplier.Value is 1f)
                fields.Reset(static () => x => x.m_unsummonOnOwnerLogoutSeconds);
            else if (fields.SetIfChanged(static () => x => x.m_unsummonOnOwnerLogoutSeconds, tameable.m_unsummonOnOwnerLogoutSeconds * Config.Summons.UnsummonLogoutTimeMultiplier.Value))
                RecreateZdo = true;

            //if (zdo.PrefabInfo.Humanoid is { Humanoid.m_damageModifiers.m_spirit: not HitData.DamageModifier.Immune })
            //{
            //    Logger.DevLog($"Undead: {zdo.PrefabInfo.PrefabName}");
            //    if (zdo.Fields<Humanoid>().SetIfChanged(static () => x => x.m_tolerateFire, true))
            //        RecreateZdo = true;
            //}

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
            /// <see cref="Tameable.GetRemainingTime()"/>
            var tameTime = fields.GetFloat(static () => x => x.m_tamingTime);
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
                    if ((ZNet.instance.GetTime() - zdo.Vars.GetTameLastFeeding()).TotalSeconds > fields.GetFloat(static () => x => x.m_fedDuration))
                        message += ", $hud_tamehungry";

                    ShowMessage(peers, zdo, message, Config.Tames.TamingProgressMessageType.Value);
                }
            }
        }

        return true;
    }
}
