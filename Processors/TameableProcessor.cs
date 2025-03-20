using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers, ref bool destroy, ref bool recreate)
    {
        if (zdo.PrefabInfo.Tameable is null)
            return false;

        var fields = zdo.Fields<Tameable>();
        if (zdo.GetBool(ZDOVars.s_tamed))
        {
            fields.Set(x => x.m_commandable, Config.Tames.MakeCommandable.Value);
            if (Config.Tames.AlwaysFed.Value)
                fields.Set(x => x.m_fedDuration, float.MaxValue);
            else
                fields.Reset(x => x.m_fedDuration);

            if (zdo.GetString(ZDOVars.s_follow) is { Length: > 0 } playerName)
            {
                SharedProcessorState.FollowingTamesByPlayerName.GetOrAdd(playerName, static _ => new()).Add(zdo.m_uid);
            }
        }
        else if (Config.Tames.SendTamingPogressMessages.Value)
        {
            /// <see cref="Tameable.GetRemainingTime()"/>
            var tameTime = fields.GetFloat(x => x.m_tamingTime);
            var tameTimeLeft = zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameTime);
            if (tameTimeLeft < tameTime)
            {
                var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
                var range = fields.GetFloat(x => x.m_tamingSpeedMultiplierRange);
                var zdo2 = zdo;
                var playersInRange = peers.Where(x => Vector3.Distance(x.m_refPos, zdo2.GetPosition()) < range);
                Main.ShowMessage(playersInRange, MessageHud.MessageType.TopLeft, $"{zdo.PrefabInfo.Character?.m_name}: $hud_tameness {tameness:P0}");
            }
        }

        return true;
    }
}
