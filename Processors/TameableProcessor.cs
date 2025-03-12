﻿using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TameableProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Tameable is null)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        bool? tamed = null;

        if (Config.Tames.MakeCommandable.Value && !prefabInfo.Tameable.m_commandable && (tamed = zdo.GetBool(ZDOVars.s_tamed)).Value)
        {
            zdo.Fields<Tameable>()
                .SetHasFields(true)
                .Set(x => x.m_commandable, true);
        }
        if (Config.Tames.SendTamingPogressMessages.Value && !(tamed ??= zdo.GetBool(ZDOVars.s_tamed)))
        {
            /// <see cref="Tameable.GetRemainingTime()"/>
            var fields = zdo.Fields(prefabInfo.Tameable);
            var tameTime = fields.GetFloat(x => x.m_tamingTime);
            var tameTimeLeft = zdo.GetFloat(ZDOVars.s_tameTimeLeft, tameTime);
            if (tameTimeLeft < tameTime)
            {
                var tameness = 1f - Mathf.Clamp01(tameTimeLeft / tameTime);
                var range = fields.GetFloat(x => x.m_tamingSpeedMultiplierRange);
                var zdo2 = zdo;
                var playersInRange = peers.Where(x => Vector3.Distance(x.m_refPos, zdo2.GetPosition()) < range);
                Main.ShowMessage(playersInRange, MessageHud.MessageType.TopLeft, $"{prefabInfo.Character?.m_name}: $hud_tameness {tameness:P0}");
            }
        }
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}
