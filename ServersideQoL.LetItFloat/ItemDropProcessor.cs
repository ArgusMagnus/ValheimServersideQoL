extern alias AutoStore;

using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.LetItFloat;

[Processor(Id)]
[RunAfter(AutoStore::ServersideQoL.AutoStore.ItemDropProcessor.Id)]
public sealed class ItemDropProcessor : Processor<ItemDropProcessor.PrefabInfo>
{
  public const string Id = "c421a7d8-e547-46b3-9e3c-d71aefa7ada4";

  public sealed record PrefabInfo(ItemDrop? ItemDrop, Container? Container) : ProcessorPrefabInfo
  {
    [MemberNotNullWhen(true, nameof(ItemDrop))]
    public bool IsItemDrop { get; private set; }

    public override bool IsValid =>
      (IsItemDrop = (ItemDrop is { IsItem: true } && !PrefabInfo.HasComponent<Floating>() && !PrefabInfo.HasComponent<Fish>())) ||
      (PrefabInfo.PrefabHash == CratePrefab);
  }

  static int CratePrefab => Prefabs.CargoCrate;

  readonly SectorDictionary<ServersideQoLZDO> _crates = new(4);

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.IsItemDrop)
    {
      if (Config.Instance.Advanced.Value.ExcludedPrefabHashes.Contains(zdo.ZDO.GetPrefab()))
        return ProcessResult.UnregisterProcessor;

      if (zdo.ZDO.GetPosition() is { y: > ZoneSystem.c_WaterLevel } ||
          GetHeight(zdo.ZDO.GetPosition()) is > ZoneSystem.c_WaterLevel - 2)
        return default;

      var delay = (float)(zdo.Vars.GetSpawnTime() - ZNet.instance.GetTime()).TotalSeconds + 2;
      if (delay > 0)
        return ScheduleReprocessing(delay);

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
        var (width, height) = GetInventorySize(inventory.Items.Count, false);
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
      __spawnTime.Set(crate.ZDO, ZNet.instance.GetTime());
      crate.ZDO.ZDO.SetOwnerInternal(zdo.ZDO.GetOwner());
      return ProcessResult.DestroyZDO;
    }
    else if (__spawnTime.Get(zdo) is { Ticks: not 0 } spawnTime)
    {
      if ((ZNet.instance.GetTime() - spawnTime).TotalMinutes > Config.Instance.DespawnAfterMinutes.Value)
        return ProcessResult.DestroyZDO;
      return default;
    }
    else
    {
      return ProcessResult.UnregisterProcessor;
    }
  }

  ContainerState GetCrate(Vector3 pos, Quaternion rot)
  {
    if (!_crates.TryGetValue(pos, out var crate) || !crate.ZDO.IsValid() || crate.ZDO.GetPrefab() != CratePrefab)
    {
      _crates[pos] = crate = PlaceObject(pos, CratePrefab, rot);
      crate.Vars.SetCreator(default);
    }
    return Instance<ContainerRegistryProcessor>().GetState(crate)!;
  }

  static readonly ServerVar<DateTime> __spawnTime = LetItFloatPlugin.RegisterServerVar<DateTime>("SpawnTime");
}
