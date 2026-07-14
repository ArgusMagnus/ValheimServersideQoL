namespace ServersideQoL.AutoStore;

[Processor(Id, RunBefore = [ContainerRegistryProcessor.Id])]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
    public const string Id = "e1c6ea7a-996b-4aad-8595-af86f02fe25b";

    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
    {
        var cfg = Config.Instance;
        if (!cfg.AutoSort.Value)
            return ProcessResult.UnregisterProcessor;

        var inventory = Instance<ContainerRegistryProcessor>().GetInventory(zdo, prefabInfo);

        return default;
    }
}
