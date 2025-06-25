namespace Valheim.ServersideQoL.Processors;

sealed class HumanoidProcessor : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        if (!Config.Summons.MakeFriendly.Value || zdo.PrefabInfo.Humanoid is not { Humanoid.m_faction: Character.Faction.PlayerSpawned } || zdo.Vars.GetTamed())
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        RPC.SetTamed(zdo, true);
        return true;
    }
}
