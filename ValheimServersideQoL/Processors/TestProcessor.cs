#if DEBUG
namespace Valheim.ServersideQoL.Processors;

sealed class TestProcessor : Processor
{
    protected override Guid Id { get; } = new("1351d884-9d48-4bb8-82a0-ddb6144d8c01");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (!peers.Any(static x => x.Info is not null))
            return false;

        //var prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
        //Logger.DevLog($"ZDO at {zdo.GetPosition()} with prefab {prefab?.name ?? "null"} ({zdo.GetPrefab()})");

        return false;
    }
}
#endif