using UnityEngine;

namespace ServersideQoL.LetItFloat;

[Processor("c421a7d8-e547-46b3-9e3c-d71aefa7ada4")]
[RunAfter("5f86a765-e449-4047-afc8-a63e4d681a48")] // AutoStore.ItemDropProcessor
public sealed class ItemDropProcessor : Processor<ItemDropProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(ItemDrop ItemDrop) : ProcessorPrefabInfo
  {
    public override bool IsValid => !PrefabInfo.HasComponent<Floating>() && !PrefabInfo.HasComponent<Fish>();
  }

  readonly SectorDictionary<ServersideQoLZDO> _crates = new(4);

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (zdo.ZDO.GetPosition() is { y: > ZoneSystem.c_WaterLevel } ||
        GetHeight(zdo.ZDO.GetPosition()) is > ZoneSystem.c_WaterLevel - 2)
      return default;

    var delay = (float)(zdo.Vars.GetSpawnTime() - ZNet.instance.GetTime()).TotalSeconds + 2;
    if (delay > 0)
    {
      zdo.DelaySchedulingFor(delay);
      return ProcessResult.ScheduleReprocessing;
    }

    var pos = zdo.ZDO.GetPosition();
    pos.y += 1;
    var crate = GetCrate(pos, zdo.ZDO.GetRotation());

    var item = new ItemDrop.ItemData { m_shared = prefabInfo.ItemDrop.m_itemData.m_shared };
    ItemDrop.LoadFromZDO(item, zdo.ZDO);

    var inventory = crate.GetInventory();

    foreach (var slot in inventory.Items)
    {
      if (ItemDataKeyComparer.Instance.Equals(slot, item))
      {
        var transfer = Math.Min(item.m_stack, slot.m_shared.m_maxStackSize - slot.m_stack);
        slot.m_stack += transfer;
        item.m_stack -= transfer;
        if (item.m_stack is 0)
          break;
      }
    }

    if (item.m_stack > 0)
    {
      item.m_dropPrefab = prefabInfo.ItemDrop.gameObject;
      inventory.Items.Add(item);
      var (width, height) = GetBackpackSize(inventory.Items.Count);
      crate.ZDO.Fields<Container>()
          .Set(static () => x => x.m_width, width)
          .Set(static () => x => x.m_height, height);

      using var enumerator = inventory.Items.GetEnumerator();
      for (var y = 0; y < height; y++)
      {
        for (var x = 0; x < width; x++)
        {
          if (!enumerator.MoveNext())
            break;
          enumerator.Current.m_gridPos = new(x, y);
        }
      }
    }

    crate.ZDO.ClaimOwnershipInternal();
    inventory.Save();
    crate.ZDO.ZDO.SetOwnerInternal(zdo.ZDO.GetOwner());
    return ProcessResult.DestroyZDO;
  }

  ContainerState GetCrate(Vector3 pos, Quaternion rot)
  {
    if (!_crates.TryGetValue(pos, out var crate))
    {
      _crates.Add(pos, crate = PlaceObject(pos, Prefabs.CargoCrate, rot));
      crate.Vars.SetCreator(0);
    }
    return Instance<ContainerRegistryProcessor>().GetState(crate)!;
  }
}
