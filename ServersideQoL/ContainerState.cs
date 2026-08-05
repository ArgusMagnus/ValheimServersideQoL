namespace ServersideQoL;

public abstract class ContainerState
{
  private protected ContainerState() { }

  public abstract ContainerRegistryProcessor.PrefabInfo PrefabInfo { get; }
  public abstract ServersideQoLZDO ZDO { get; }
  public abstract IInventory GetInventory();

  public float? PickupRange { get; set; }
  public float? FeedRange { get; set; }

  public interface IInventory
  {
    Inventory Inventory { get; }
    List<ItemDrop.ItemData> Items { get; }
    void Save();
  }
}
