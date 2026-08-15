using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.AutoProcess;

[Processor("1db73d4d-e930-402f-933e-cb92e5748312")]
[RunAfter<ContainerRegistryProcessor>]
public sealed class SmelterProcessor : Processor<SmelterProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Smelter? Smelter, ShieldGenerator? ShieldGenerator) : ProcessorPrefabInfo
  {
    [MemberNotNullWhen(true, nameof(Smelter))]
    public bool HasProduct { get; } = Smelter?.m_conversion.Any(static x => x.m_to is not null) is true;
  }

  SectorDictionary<HashSet<ServersideQoLZDO>>? _smelters;
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    if (Config.Instance.FeedFromContainers.Value)
    {
      _smelters = new(Mathf.Max(Config.Instance.FeedFromContainersRange.Value, Config.Instance.FeedFromContainersMaxRange ?? 0));
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(_smelters.SectorWidth);
      Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
    }
    else
    {
      _smelters = null;
      _containersByItemName = null;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.Default;

    if (Config.Instance.CapacityMultiplier.Value is 1f)
    {
      if (prefabInfo.Smelter is not null)
      {
        zdo.Fields<Smelter>()
            .Reset(static () => x => x.m_maxFuel)
            .Reset(static () => x => x.m_maxOre);
      }
      else
        zdo.Fields<ShieldGenerator>().Reset(static () => x => x.m_maxFuel);
    }
    else
    {
      if (prefabInfo.ShieldGenerator is not null)
      {
        if (zdo.Fields<ShieldGenerator>().UpdateValue(static () => x => x.m_maxFuel, Mathf.RoundToInt(Config.Instance.CapacityMultiplier.Value * prefabInfo.ShieldGenerator.m_maxFuel)))
          result = ProcessResult.RecreateZDO;
      }
      else if (prefabInfo.Smelter is not null)
      {
        if (zdo.Fields<Smelter>().UpdateValue(static () => x => x.m_maxFuel, Mathf.RoundToInt(Config.Instance.CapacityMultiplier.Value * prefabInfo.Smelter.m_maxFuel)))
          result = ProcessResult.RecreateZDO;
        if (zdo.Fields<Smelter>().UpdateValue(static () => x => x.m_maxOre, Mathf.RoundToInt(Config.Instance.CapacityMultiplier.Value * prefabInfo.Smelter.m_maxOre)))
          result = ProcessResult.RecreateZDO;
      }
    }

    if (prefabInfo.HasProduct)
    {
      if (Config.Instance.TimePerProductMultiplier.Value is 1f)
        zdo.Fields<Smelter>().Reset(static () => x => x.m_secPerProduct);
      else if (zdo.Fields<Smelter>().UpdateValue(static () => x => x.m_secPerProduct, Mathf.Max(1f, prefabInfo.Smelter.m_secPerProduct * Config.Instance.TimePerProductMultiplier.Value)))
        result = ProcessResult.RecreateZDO;
    }

    if (_smelters is null || _containersByItemName is null)
      return result | ProcessResult.UnregisterProcessor;

    if (!CheckMinDistance(peers, zdo, Config.Instance.FeedFromContainersMinPlayerDistance.Value))
      return result | ProcessResult.ScheduleReprocessing;

    List<ServersideQoLZDO>? toRemove = null;
    List<ItemDrop.ItemData>? removeSlots = null;

    /// <see cref="Smelter.OnAddFuel"/>
    {
      var maxFuel = prefabInfo.Smelter is not null ?
          zdo.Fields<Smelter>().GetInt(static () => x => x.m_maxFuel) :
          zdo.Fields<ShieldGenerator>().GetInt(static () => x => x.m_maxFuel);
      var currentFuel = zdo.Vars.GetFuel();
      var maxFuelAdd = (int)(maxFuel - currentFuel);
      if (maxFuelAdd > maxFuel / 2)
      {
        foreach (var fuelItem in prefabInfo.ShieldGenerator?.m_fuelItems.Select(static x => x.m_itemData) ?? [prefabInfo.Smelter!.m_fuelItem.m_itemData])
        {
          var addedFuel = 0;
          foreach (var containers in _containersByItemName.EnumerateAdjacent((zdo.ZDO.GetPosition(), fuelItem.m_shared)))
          {
            toRemove?.Clear();
            foreach (var containerZdo in containers)
            {
              if (Instance<ContainerRegistryProcessor>().GetState(containerZdo) is not { } containerState)
              {
                (toRemove ??= []).Add(containerZdo);
                continue;
              }

              if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                continue; // in use or player to close

              var feedRangeSqr = containerState.FeedRange ?? Config.Instance.FeedFromContainersRange.Value;
              feedRangeSqr *= feedRangeSqr;
              if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > feedRangeSqr)
                continue;

              var inventory = containerState.GetInventory();
              removeSlots?.Clear();
              var addFuel = 0;
              var leave = Config.Instance.FeedFromContainersLeaveAtLeastFuel.Value;
              var found = false;
              var requestOwn = false;
              foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == fuelItem).OrderBy(static x => x.m_stack))
              {
                found = found || slot is { m_stack: > 0 };
                var take = Math.Min(maxFuelAdd, slot.m_stack);
                var leaveDiff = Math.Min(take, leave);
                leave -= leaveDiff;
                take -= leaveDiff;
                if (take is 0)
                  continue;
                else if (!containerZdo.IsOwnerOrUnassigned())
                {
                  requestOwn = true;
                  break;
                }

                addFuel += take;
                slot.m_stack -= take;
                if (slot.m_stack is 0)
                  (removeSlots ??= []).Add(slot);

                maxFuelAdd -= take;
                if (maxFuelAdd is 0)
                  break;
              }

              if (requestOwn)
              {
                zdo.DelaySchedulingFor(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
                result |= ProcessResult.ScheduleReprocessing;
                continue;
              }

              if (addFuel is 0)
              {
                if (!found)
                  (toRemove ??= []).Add(containerZdo);
                continue;
              }

              if (removeSlots is { Count: > 0 })
              {
                foreach (var remove in removeSlots)
                  inventory.Items.Remove(remove);

                if (inventory.Items is { Count: 0 })
                  (toRemove ??= []).Add(containerZdo);
              }

              zdo.ReleaseOwnership();
              currentFuel += addFuel;
              zdo.Vars.SetFuel(currentFuel);
              inventory.Save();

              addedFuel += addFuel;

              if (maxFuelAdd is 0)
                break;
            }

            if (toRemove is not null)
            {
              foreach (var containerZdo in toRemove)
                containers.Remove(containerZdo);
            }

            if (maxFuelAdd is 0)
              break;
          }

          if (addedFuel is not 0)
          {
            ShowMessage(peers, zdo,
                Config.Instance.Localization.Value.FormatFuelAdded(prefabInfo.Smelter?.m_name ?? prefabInfo.ShieldGenerator!.m_name, fuelItem.m_shared.m_name, addedFuel),
                Config.Instance.OreOrFuelAddedMessageType.Value);
          }

          if (maxFuelAdd is 0)
            break;
        }
      }
    }

    /// <see cref="Smelter.OnAddOre"/> <see cref="Smelter.QueueOre"/>
    if (prefabInfo.Smelter is not null)
    {
      int maxOre = zdo.Fields<Smelter>().GetInt(static () => x => x.m_maxOre);
      var currentOre = zdo.Vars.GetQueued();
      var maxOreAdd = maxOre - currentOre;
      if (maxOreAdd > maxOre / 2)
      {
        foreach (var conversion in prefabInfo.Smelter.m_conversion)
        {
          var oreItem = conversion.m_from.m_itemData;
          var addedOre = 0;
          foreach (var containers in _containersByItemName.EnumerateAdjacent((zdo.ZDO.GetPosition(), oreItem.m_shared)))
          {
            toRemove?.Clear();
            foreach (var containerZdo in containers)
            {
              if (Instance<ContainerRegistryProcessor>().GetState(containerZdo) is not { } containerState)
              {
                (toRemove ??= []).Add(containerZdo);
                continue;
              }

              if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
                continue; // in use or player to close

              var feedRangeSqr = containerState.FeedRange ?? Config.Instance.FeedFromContainersRange.Value;
              feedRangeSqr *= feedRangeSqr;
              if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > feedRangeSqr)
                continue;

              var inventory = containerState.GetInventory();
              removeSlots?.Clear();
              int addOre = 0;
              var leave = Config.Instance.FeedFromContainersLeaveAtLeastOre.Value;
              var found = false;
              var requestOwn = false;
              foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == oreItem).OrderBy(static x => x.m_stack))
              {
                found = found || slot is { m_stack: > 0 };
                var take = Math.Min(maxOreAdd, slot.m_stack);
                var leaveDiff = Math.Min(take, leave);
                leave -= leaveDiff;
                take -= leaveDiff;
                if (take is 0)
                  continue;
                else if (!containerZdo.IsOwnerOrUnassigned())
                {
                  requestOwn = true;
                  break;
                }

                addOre += take;
                slot.m_stack -= take;
                if (slot.m_stack is 0)
                  (removeSlots ??= new()).Add(slot);

                maxOreAdd -= take;
                if (maxOreAdd is 0)
                  break;
              }

              if (requestOwn)
              {
                zdo.DelaySchedulingFor(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
                result |= ProcessResult.ScheduleReprocessing;
                continue;
              }

              if (addOre is 0)
              {
                if (!found)
                  (toRemove ??= []).Add(containerZdo);
                continue;
              }

              if (removeSlots is { Count: > 0 })
              {
                foreach (var remove in removeSlots)
                  inventory.Items.Remove(remove);

                if (inventory.Items is { Count: 0 })
                  (toRemove ??= []).Add(containerZdo);
              }

              zdo.ReleaseOwnership();
              for (int i = 0; i < addOre; i++)
                zdo.Vars.SetItem(currentOre + i, conversion.m_from.gameObject.name);
              currentOre += addOre;
              zdo.Vars.SetQueued(currentOre);

              inventory.Save();

              addedOre += addOre;

              if (maxOreAdd is 0)
                break;
            }

            if (toRemove is not null)
            {
              foreach (var containerZdo in toRemove)
                containers.Remove(containerZdo);
            }

            if (maxOreAdd is 0)
              break;
          }

          if (addedOre is not 0)
          {
            ShowMessage(peers, zdo,
                Config.Instance.Localization.Value.FormatOreAdded(prefabInfo.Smelter.m_name, oreItem.m_shared.m_name, addedOre),
                Config.Instance.OreOrFuelAddedMessageType.Value);
          }

          if (maxOreAdd is 0)
            break;
        }
      }
    }

    _smelters.TryAdd(zdo);
    return result;
  }

  void OnContainerChanged(ServersideQoLZDO containerZdo, ContainerState state)
  {
    if (_smelters is null)
      return;

    var feedRangeSqr = state.FeedRange ?? Config.Instance.FeedFromContainersRange.Value;
    feedRangeSqr *= feedRangeSqr;
    if (feedRangeSqr is 0f)
      return;

    if (state.GetInventory() is not { Items.Count: > 0 } inventory)
      return;

    foreach (var smelters in _smelters.EnumerateAdjacent(containerZdo.ZDO.GetPosition()))
    {
      foreach (var zdo in smelters)
      {
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) <= feedRangeSqr)
          ScheduleReprocessing(zdo);
      }
    }
  }
}
