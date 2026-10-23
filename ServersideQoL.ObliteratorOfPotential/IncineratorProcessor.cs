using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.ObliteratorOfPotential;

[Processor(Id)]
[DependsOn<ContainerRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorProcessor.PrefabInfo>
{
  public const string Id = "2006ea3b-d05a-49bb-9d59-28b19a4bd7f7";
  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    [MemberNotNullWhen(true, nameof(Container))]
    public bool IsIncinerator { get; private set; }
    public Container? Container { get; private set; }

    public bool IsUpgradeStation { get; private set; }

    public override bool IsValid
    {
      get
      {
        if (PrefabInfo.GetComponent<CraftingStation>() is { m_upgrader: true })
          return IsUpgradeStation = true;

        if ((Container = PrefabInfo.GetComponent<Container>()) is null)
          return false;
        if (!PrefabInfo.HasComponent<Incinerator>() || !PrefabInfo.HasComponent<Piece>() || !PrefabInfo.HasComponent<PieceTable>())
          return false;
        return IsIncinerator = true;
      }
    }
  }

  readonly Dictionary<GameObject, HashSet<GameObject>> _itemsByUpgrader = [];
  readonly HashSet<ItemDrop.ItemData> _used = [];
  readonly Dictionary<ServersideQoLZDO, State> _states = [];
  ServersideQoLZDO? _obliteratorOfPotential;

  protected override void Initialize()
  {
    _obliteratorOfPotential ??= PlacedObjects.FirstOrDefault(x => GetProcessorPrefabInfo(x) is { IsUpgradeStation: true });
    if (_obliteratorOfPotential is not null && !Config.Instance.PlaceNearForgeOfPotential.Value)
    {
      DestroyObject(_obliteratorOfPotential);
      _obliteratorOfPotential = null;
    }

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
    if (prefabInfo.IsUpgradeStation)
    {
      if (!Config.Instance.PlaceNearForgeOfPotential.Value || _obliteratorOfPotential is not null)
        return ProcessResult.UnregisterProcessor;

      var pos = zdo.ZDO.GetPosition();
      pos.y += 4;
      _obliteratorOfPotential = PlacePiece(pos, Prefabs.Incinerator, zdo.ZDO.GetRotation(), CreatorMarkers.ProcessorOwned);
      return ProcessResult.UnregisterProcessor;
    }
    else if (prefabInfo.IsIncinerator)
    {
      if (zdo != _obliteratorOfPotential && !Config.Instance.EveryObliteratorCanUpgrade.Value)
        return ProcessResult.UnregisterProcessor;

      if (_states.TryGetValue(zdo, out var state) && !state.LeverPulled && !state.LeverReturned)
        state.NewItems.Clear();

      var containerState = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
      var inventory = containerState.GetInventory();
      foreach (var upgrader in inventory.Items)
      {
        if (!_itemsByUpgrader.TryGetValue(upgrader.m_dropPrefab, out var set))
          continue;

        for (int i = 0; i < upgrader.m_stack; i++)
        {
          Logger.DevLog($"Upgrader found: {upgrader.m_dropPrefab.name}");
          foreach (var sacrifice in inventory.Items)
          {
            if (upgrader == sacrifice || !set.Contains(sacrifice.m_dropPrefab) || _used.Contains(sacrifice))
              continue;

            var sacrificeArmor = sacrifice.GetArmor();
            var sacrificeBaseBlockPower = sacrifice.GetBaseBlockPower();
            var sacrificeTotalDamage = sacrifice.GetDamage().GetTotalDamage();
            if (sacrificeArmor <= 0 && sacrificeBaseBlockPower <= 0 && sacrificeTotalDamage <= 0)
              continue;

            Logger.DevLog($"Sacrifice found: {sacrifice.m_dropPrefab.name}");
            foreach (var item in inventory.Items)
            {
              if (item == upgrader || item == sacrifice || item.m_shared.m_itemType != sacrifice.m_shared.m_itemType ||
                  (item.m_shared is { m_armorPerLevel: <= 0, m_blockPowerPerLevel: <= 0 } && item.m_shared.m_damagesPerLevel.GetTotalDamage() <= 0) ||
                  (item.GetArmor() >= sacrificeArmor && item.GetBaseBlockPower() >= sacrificeBaseBlockPower && item.GetDamage().GetTotalDamage() >= sacrificeTotalDamage) ||
                  _used.Contains(item))
                continue;

              ItemDrop.ItemData? newItem = null;
              while (true)
              {
                if (item.m_shared.m_armorPerLevel > 0 && (newItem ??= item.Clone()).GetArmor() >= sacrificeArmor)
                  break;
                if (item.m_shared.m_blockPowerPerLevel > 0 && (newItem ??= item.Clone()).GetBaseBlockPower() >= sacrificeBaseBlockPower)
                  break;
                if (item.m_shared.m_damagesPerLevel.GetTotalDamage() > 0 && (newItem ??= item.Clone()).GetDamage().GetTotalDamage() >= sacrificeTotalDamage)
                  break;
                if (newItem is null)
                  break;
                newItem.m_quality++;
              }

              if (newItem is null || newItem.m_quality == item.m_quality)
                continue;

              _used.Add(sacrifice);
              _used.Add(item);

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
      }

      var result = ProcessResult.Default;
      var done = false;
      if (_used.Count is not 0)
      {
        state.Sign ??= PlacePiece(zdo.ZDO.GetPosition(), Prefabs.Sign, zdo.ZDO.GetRotation());
        var text = string.Join("<br>", state.NewItems.Select(static x => $"{Localization.instance.Localize(x.m_shared.m_name)} {x.m_quality}"));
        state.Sign.Vars.SetText($"<mark=#000000><color=white>{text}<size=45><br><br>");
      }
      else if (state is { LeverReturned: true })
      {
        if (!zdo.IsOwnerOrUnassigned())
          result = ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerState, default));
        else
        {
          foreach (var item in state.NewItems)
            inventory.Inventory.AddItem(item);
          inventory.Save();
          done = true;
        }
      }
      else if (state?.NewItems.Count is 0)
      {
        done = true;
      }

      if (done)
      {
        zdo.Destroyed -= OnIncineratorDestroyed;
        _states.Remove(zdo);
        if (state!.Sign is not null)
          DestroyObject(state.Sign);
      }

      _used.Clear();

      return result;
    }
    else
    {
      Logger.DevLog($"Unexpected ZDO: {prefabInfo.PrefabInfo.PrefabName}");
      return ProcessResult.UnregisterProcessor;
    }
  }

  void OnIncineratorDestroyed(ServersideQoLZDO zdo)
  {
    if (_states.Remove(zdo, out var state) && state.Sign is not null)
      DestroyObject(state.Sign);
  }

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
    public ServersideQoLZDO? Sign { get; set; }
  }
}
