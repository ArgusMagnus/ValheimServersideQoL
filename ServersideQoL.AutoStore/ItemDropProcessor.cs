using BepInEx.Configuration;

namespace ServersideQoL.AutoStore;

[Processor(Id)]
public sealed class ItemDropProcessor : Processor<ItemDropProcessor.PrefabInfo>
{
    public const string Id = "5f86a765-e449-4047-afc8-a63e4d681a48";
    public sealed record PrefabInfo(ItemDrop ItemDrop) : ProcessorPrefabInfo;

    readonly ConfigEntry<bool> _cfgAutoPickup = Config.Instance.AutoPickup;
    SectorDictionary<SharedItemDataKey, HashSet<ZDO>>? _containersByItemName;

    protected override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(1);
    }

    protected override ProcessResult Process(ZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
    {
        if (_containersByItemName is null || !_cfgAutoPickup.Value)
            return ProcessResult.UnregisterProcessor;
    }
}