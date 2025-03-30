using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    bool _enabled;
    readonly HashSet<ExtendedZDO> _initialPortals = new();

    public override void Initialize()
    {
        if (Config.GlobalsKeys.NoPortalsPreventsContruction.Value && _initialPortals.Count is 0 && !_enabled && (_enabled = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)))
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
                _initialPortals.Add(zdo);
        }
        base.Initialize();
    }

	protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (!_enabled || zdo.PrefabInfo.TeleportWorld is null || !_initialPortals.Contains(zdo))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        /// <see cref="WearNTear.RPC_Remove"/>
        var owner = zdo.GetOwner();
        ZRoutedRpc.instance.InvokeRoutedRPC(owner, zdo.m_uid, "RPC_Remove", [false]);

        /// <see cref="Player.TryPlacePiece(Piece)"/>
        var peer = peers.FirstOrDefault(x => x.m_uid == owner);
        if (peer is not null)
            RPC.ShowMessage(peer, MessageHud.MessageType.Center, "$msg_nobuildzone");

        return false;
    }
}