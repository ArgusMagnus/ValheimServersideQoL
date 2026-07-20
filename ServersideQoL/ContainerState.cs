namespace ServersideQoL;

public abstract class ContainerState
{
    private protected ContainerState() { }

    public abstract ContainerRegistryProcessor.PrefabInfo PrefabInfo { get; }
    public abstract Inventory Inventory { get; }
    public abstract List<ItemDrop.ItemData> InventoryItems { get; }
    public abstract void SaveIntenvory();
}