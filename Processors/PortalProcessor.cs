using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    bool _enabled;
    readonly HashSet<ZDO> _initialPortals = new();

    public override void Initialize()
    {
        if (Config.GlobalsKeys.NoPortalsPreventsContruction.Value && !_enabled && (_enabled = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)))
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (var zdo in ZDOMan.instance.GetPortals())
                _initialPortals.Add(zdo);
        }
        base.Initialize();
    }

    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (!_enabled || prefabInfo.TeleportWorld is null || _initialPortals.Contains(zdo))
            return;

        /// <see cref="WearNTear.RPC_Remove"/>
        var owner = zdo.GetOwner();
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, zdo.m_uid, "RPC_Remove", [false]);

        /// <see cref="Player.TryPlacePiece(Piece)"/>
        var peer = peers.FirstOrDefault(x => x.m_uid == owner);
        if (peer is not null)
            Main.ShowMessage([peer], MessageHud.MessageType.Center, "$msg_nobuildzone");
    }
}