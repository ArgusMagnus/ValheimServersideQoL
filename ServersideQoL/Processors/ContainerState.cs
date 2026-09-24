namespace ServersideQoL.Processors;

public abstract class ContainerState
{
  private protected ContainerState() { }

  public abstract Container Container { get; }
  public abstract ServersideQoLZDO ZDO { get; }
  public abstract IInventory GetInventory();

  public float? AutoStorePickupRange { get; set; }
  public float? TameAssistFeedRange { get; set; }
  public float? AutoProcessFeedRange { get; set; }

  [Obsolete($"Use {nameof(AutoStorePickupRange)} instead", true)]
  public float? PickupRange { get => AutoStorePickupRange; set => AutoStorePickupRange = value; }
  [Obsolete($"Use {nameof(AutoStorePickupRange)} instead", true)]
  public float? FeedRange { get => AutoProcessFeedRange; set => AutoProcessFeedRange = value; }

  public interface IInventory
  {
    Inventory Inventory { get; }
    List<ItemDrop.ItemData> Items { get; }
    void Save();
  }
}
