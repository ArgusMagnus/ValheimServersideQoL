using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.ObliteratorOfPotential;

[Processor(Id)]
[DependsOn<ContainerRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorProcessor.PrefabInfo>
{
  public const string Id = "2006ea3b-d05a-49bb-9d59-28b19a4bd7f7";
  public sealed record PrefabInfo(Incinerator Incinerator, Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly Dictionary<GameObject, HashSet<GameObject>> _itemsByUpgrader = [];
  readonly HashSet<ItemDrop.ItemData> _used = [];
  readonly Dictionary<ServersideQoLZDO, State> _states = [];

  protected override void Initialize()
  {
    if (_itemsByUpgrader.Count is 0)
    {
      foreach (var recipe in ObjectDB.instance.m_recipes)
      {
        if (recipe is not { m_item: not null, m_resources: not null })
          continue;

        foreach (var resource in recipe.m_resources)
        {
          if (resource is not { m_resItem: not null, m_upgraderResource: true })
            continue;

          if (!_itemsByUpgrader.TryGetValue(resource.m_resItem.gameObject, out var set))
            _itemsByUpgrader.Add(resource.m_resItem.gameObject, set = []);
          set.Add(recipe.m_item.gameObject);
        }
      }
    }

    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLever, RPC_AnimateLever, true);
    RPC.Intercept.UpdateInterception(RPC.RpcName.Incinerator.AnimateLeverReturn, RPC_AnimateLeverReturn, true);
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (_states.TryGetValue(zdo, out var state) && !state.LeverPulled && !state.LeverReturned)
      state.NewItems.Clear();

    var containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
    var inventory = containerState.GetInventory();
    foreach (var upgrader in inventory.Items)
    {
      if (!_itemsByUpgrader.TryGetValue(upgrader.m_dropPrefab, out var set))
        continue;

      // todo: handle upgrader stacks

      Logger.DevLog($"Upgrader found: {upgrader.m_dropPrefab.name}");
      foreach (var sacrifice in inventory.Items)
      {
        if (upgrader == sacrifice || sacrifice.GetArmor() <= 0 || !set.Contains(sacrifice.m_dropPrefab) || _used.Contains(sacrifice))
          continue;

        Logger.DevLog($"Sacrifice found: {sacrifice.m_dropPrefab.name}");
        foreach (var item in inventory.Items)
        {
          if (item == upgrader || item == sacrifice || item.m_shared.m_itemType != sacrifice.m_shared.m_itemType ||
              item.m_shared.m_armorPerLevel <= 0 || item.GetArmor() >= sacrifice.GetArmor() || _used.Contains(item))
            continue;

          _used.Add(sacrifice);
          _used.Add(item);

          var newItem = item.Clone();
          while (newItem.GetArmor() < sacrifice.GetArmor())
            newItem.m_quality++;

          Logger.DevLog($"Item found: {item.m_dropPrefab.name}, new quality: {newItem.m_quality}");
          if (state is null)
          {
            _states.Add(zdo, state = new() { Revision = zdo.ZDO.DataRevision });
            zdo.Destroyed += OnIncineratorDestroyed;
          }
          else if (state.Revision != zdo.ZDO.DataRevision)
          {
            state.Revision = zdo.ZDO.DataRevision;
            state.NewItems.Clear();
          }
          state.NewItems.Add(newItem);
        }
      }
    }

    var result = ProcessResult.Default;
    if (_used.Count is 0 && state is { LeverReturned: true })
    {
      if (!zdo.IsOwnerOrUnassigned())
        result = ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerState, default));
      else
      {
        foreach (var item in state.NewItems)
          inventory.Inventory.AddItem(item);
        inventory.Save();
        _states.Remove(zdo);
      }
    }

    _used.Clear();

    return result;
  }

  void OnIncineratorDestroyed(ServersideQoLZDO zdo) => _states.Remove(zdo);

  void RPC_AnimateLever(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverPulled = true;
  }

  void RPC_AnimateLeverReturn(ServersideQoLZDO zdo)
  {
    if (_states.TryGetValue(zdo, out var state))
      state.LeverReturned = true;
  }

  sealed class State
  {
    public required uint Revision { get; set; }
    public List<ItemDrop.ItemData> NewItems { get; } = [];
    public bool LeverPulled { get; set; }
    public bool LeverReturned { get; set; }
  }
}
