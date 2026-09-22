using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.TameAssist;

[Processor(Id)]
[RunAfter<TameableRegistryProcessor>]
[DependsOn<ContainerRegistryProcessor>]
public sealed class TameableProcessor : Processor<TameableRegistryProcessor.PrefabInfo>
{
  public const string Id = "4386fb2c-2092-4f88-a173-9f01fadcbc6c";

  readonly Dictionary<ServersideQoLZDO, TamingState> _tamingStates = [];
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {
    _tamingStates.Clear();

    _containersByItemName = Config.Instance.FeedFromContainers.Value ?
      Instance<ContainerRegistryProcessor>().GetContainersByItemName(Mathf.Max(Config.Instance.FeedFromContainersRange.Value, Config.Instance.FeedFromContainersMaxRange.Value)) :
      null;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, TameableRegistryProcessor.PrefabInfo prefabInfo)
  {
    ServersideQoLZDO.ComponentFieldAccessor<Tameable>? fields = null;

    if (Instance<TameableRegistryProcessor>().GetState(zdo) is not { } state)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.UnregisterProcessor;

    if (state.State is TameableState.States.Tamed or TameableState.States.Taming && 
        prefabInfo.Humanoid is not { m_faction: Character.Faction.Players or Character.Faction.PlayerSpawned })
    {
      fields ??= zdo.Fields<Tameable>();
      if (Config.Instance.FedDurationMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_fedDuration);
      else if (fields.UpdateValue(static () => x => x.m_fedDuration, prefabInfo.Tameable.m_fedDuration * Config.Instance.FedDurationMultiplier.Value))
        result |= ProcessResult.RecreateZDO;
    }

    if (state.State is TameableState.States.Tamed)
    {
      fields ??= zdo.Fields<Tameable>();

      if (!Config.Instance.MakeCommandable.Value)
        fields.Reset(static () => x => x.m_commandable);
      else if (fields.UpdateValue(static () => x => x.m_commandable, true))
        result |= ProcessResult.RecreateZDO;

      _tamingStates.Remove(zdo);

      if (_containersByItemName is not null && prefabInfo.MonsterAI.m_consumeItems is { Count: > 0})
      {
        result &= ~ProcessResult.UnregisterProcessor;
        result |= FeedFromContainer(zdo, peers, prefabInfo) | ProcessResult.ReregisterOnRecreated;
      }
    }
    else if (state.State is TameableState.States.Taming)
    {
      fields ??= zdo.Fields<Tameable>();

      if (Config.Instance.TamingTimeMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_tamingTime);
      else if (fields.UpdateValue(static () => x => x.m_tamingTime, prefabInfo.Tameable.m_tamingTime * Config.Instance.TamingTimeMultiplier.Value))
        result |= ProcessResult.RecreateZDO;

      if (Config.Instance.PotionTamingBoostMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_tamingBoostMultiplier);
      else if (fields.UpdateValue(static () => x => x.m_tamingBoostMultiplier, prefabInfo.Tameable.m_tamingBoostMultiplier * Config.Instance.PotionTamingBoostMultiplier.Value))
        result |= ProcessResult.RecreateZDO;

      if (Config.Instance.TamingProgressMessageType.Value is not MessageTypes.None)
      {
        result &= ~ProcessResult.UnregisterProcessor;

        if (!_tamingStates.TryGetValue(zdo, out var tamingState))
        {
          _tamingStates.Add(zdo, tamingState = new());
          zdo.Destroyed += x => _tamingStates.Remove(x);
        }

        var now = Timestamp.Now;
        if (tamingState.NextMessage < now)
        {
          tamingState.NextMessage = now.AddSeconds(DamageText.instance.m_textDuration);

          var isHungry = false;
          /// <see cref="Tameable.IsHungry()"/>
          if ((ZNet.instance.GetTime() - zdo.Vars.GetTameLastFeeding()).TotalSeconds > fields.GetFloat(static () => x => x.m_fedDuration))
            isHungry = true;

          ShowMessage(peers, zdo, Config.Instance.Localization.Value.FormatTaming(state.Tameness, isHungry), Config.Instance.TamingProgressMessageType.Value);
        }
      }
    }

    if (state.State is not TameableState.States.Tamed && (result & ProcessResult.UnregisterProcessor) is not 0)
    {
      result |= ProcessResult.ReregisterOnRecreated;
      state.StateChanged += static state => state.ZDO.ReregisterAll();
    }

    return result;
  }

  ProcessResult FeedFromContainer(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, TameableRegistryProcessor.PrefabInfo prefabInfo)
  {
    var fields = zdo.Fields<Tameable>();
    /// <see cref="Tameable.IsHungry()"/>
    if ((ZNet.instance.GetTime() - zdo.Vars.GetTameLastFeeding()).TotalSeconds <= fields.GetFloat(static () => x => x.m_fedDuration))
      return default; // not hungry

    var result = ProcessResult.Default;
    List<ServersideQoLZDO>? toRemove = null;
    List<ItemDrop.ItemData>? removeSlots = null;
    var maxAdd = 1;
    foreach (var itemDrop in prefabInfo.MonsterAI.m_consumeItems)
    {
      var foodItem = itemDrop.m_itemData;
      var added = 0;
      foreach (var containers in _containersByItemName!.EnumerateAdjacent((zdo.ZDO.GetPosition(), foodItem.m_shared)))
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

          var feedRangeSqr = containerState.TameAssistFeedRange ?? Config.Instance.FeedFromContainersRange.Value;
          feedRangeSqr *= feedRangeSqr;
          if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > feedRangeSqr)
            continue;

          var inventory = containerState.GetInventory();
          removeSlots?.Clear();
          var add = 0;
          var leave = Config.Instance.FeedFromContainersLeaveAtLeast.Value;
          var found = false;
          var requestOwn = false;
          foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == foodItem).OrderBy(static x => x.m_stack))
          {
            found = found || slot is { m_stack: > 0 };
            var leaveDiff = Math.Min(slot.m_stack, leave);
            var take = Math.Min(maxAdd, slot.m_stack - leaveDiff);
            leave -= leaveDiff;
            if (take is 0)
              continue;
            else if (!containerZdo.IsOwnerOrUnassigned())
            {
              requestOwn = true;
              break;
            }

            add += take;
            slot.m_stack -= take;
            if (slot.m_stack is 0)
              (removeSlots ??= []).Add(slot);

            maxAdd -= take;
            if (maxAdd is 0)
              break;
          }

          if (requestOwn)
          {
            result |= ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
            continue;
          }

          if (add is 0)
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

          inventory.Save();
          added += add;

          if (maxAdd is 0)
            break;
        }

        if (toRemove is not null)
        {
          foreach (var containerZdo in toRemove)
            containers.Remove(containerZdo);
        }

        if (maxAdd is 0)
          break;
      }

      if (added is not 0)
      {
        zdo.Vars.SetTameLastFeeding(ZNet.instance.GetTime());
        zdo.ZDO.DataRevision += 100;

        var (pos, rot) = (prefabInfo.Humanoid.transform.position, prefabInfo.Humanoid.transform.rotation);
        try
        {
          (prefabInfo.Humanoid.transform.position, prefabInfo.Humanoid.transform.rotation) = (zdo.ZDO.GetPosition(), zdo.ZDO.GetRotation());
          prefabInfo.Humanoid.m_consumeItemEffects.Create(prefabInfo.Humanoid.transform.position, Quaternion.identity);
        }
        finally
        {
          (prefabInfo.Humanoid.transform.position, prefabInfo.Humanoid.transform.rotation) = (pos, rot);
        }

        if (Config.Instance.TameFedMessageType.Value is not MessageTypes.None)
        {
          ShowMessage(peers, zdo,
              Config.Instance.Localization.Value.FormatTameFed(prefabInfo.Humanoid.m_name, foodItem.m_shared.m_name),
              Config.Instance.TameFedMessageType.Value);
        }
      }

      if (maxAdd is 0)
        break;
    }

    return result;
  }

  public sealed class TamingState
  {
    public Timestamp NextMessage { get; set; }
  }
}
