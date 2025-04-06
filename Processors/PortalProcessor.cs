using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    bool _enabled;
    readonly HashSet<ExtendedZDO> _initialPortals = new();

    public override void Initialize()
    {
        base.Initialize();
        _enabled = false;
        _initialPortals.Clear();
        if (Config.GlobalsKeys.NoPortalsPreventsContruction.Value && (_enabled = ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals)))
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
                _initialPortals.Add(zdo);
            RegisterZdoDestroyed();
        }
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _initialPortals.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        UnregisterZdoProcessor = true;
        if (!_enabled || zdo.PrefabInfo.TeleportWorld is null || _initialPortals.Contains(zdo))
            return false;

        RPC.Remove(zdo);

        /// <see cref="Player.TryPlacePiece(Piece)"/>
        var owner = zdo.GetOwner();
        if (owner is not 0)
            RPC.ShowMessage(owner, MessageHud.MessageType.Center, "$msg_nobuildzone");

        return false;
    }
}