namespace ServersideQoL;

public abstract class ContainerState
{
    private protected ContainerState() { }

    public abstract List<ItemDrop.ItemData> InventoryItems { get; }
    public abstract void SaveIntenvory();
}