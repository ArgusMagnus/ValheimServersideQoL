namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor : Processor
{
    bool _destroyNewPortals;
    readonly HashSet<ExtendedZDO> _initialPortals = [];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        foreach (var zdo in _initialPortals)
            zdo.Destroyed -= OnInitialPortalDestroyed;
        _initialPortals.Clear();

        _destroyNewPortals = Config.GlobalsKeys.NoPortalsPreventsContruction.Value && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals);
        if (_destroyNewPortals)
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
            {
                _initialPortals.Add(zdo);
                zdo.Destroyed += OnInitialPortalDestroyed;
            }
        }
    }

    void OnInitialPortalDestroyed(ExtendedZDO zdo)
    {
        _initialPortals.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.TeleportWorld is null)
            return false;

        if (_destroyNewPortals && !_initialPortals.Contains(zdo) && zdo.Vars.GetCreator() != Main.PluginGuidHash)
        {
            RPC.Remove(zdo);

            /// <see cref="Player.TryPlacePiece(Piece)"/>
            var owner = zdo.GetOwner();
            if (owner is not 0)
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, "$msg_nobuildzone");

            UnregisterZdoProcessor = false;
            return false;
        }

        return false;
    }
}
