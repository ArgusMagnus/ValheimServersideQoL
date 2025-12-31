namespace Valheim.ServersideQoL.Signs;

sealed record PrefabInfo(Sign Sign) : PrefabInfoBase;

sealed class SignProcessor : Processor<PrefabInfo>
{
    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    {
        throw new NotImplementedException();
    }
}
