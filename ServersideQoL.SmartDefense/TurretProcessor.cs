using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.SmartDefense;

[Processor(Id)]
[DependsOn<ContainerRegistryProcessor>]
public sealed class TurretProcessor : Processor<TurretProcessor.PrefabInfo>
{
  public const string Id = "bd1d9b45-ebcc-4217-a4a8-129c4a8be605";

  public sealed record PrefabInfo(Turret Turret, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  SectorDictionary<HashSet<ServersideQoLZDO>>? _turrets;
  SectorDictionary<SharedItemDataKey, HashSet<ServersideQoLZDO>>? _containersByItemName;

  protected override void Initialize()
  {

    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    if (Config.Instance.Turrets.LoadFromContainers.Value)
    {
      _turrets = new(Mathf.Max(Config.Instance.Turrets.LoadFromContainersRange.Value, Config.Instance.Turrets.FeedFromContainersMaxRange ?? 0));
      _containersByItemName = Instance<ContainerRegistryProcessor>().GetContainersByItemName(_turrets.SectorWidth);
      Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
    }
    else
    {
      _turrets = null;
      _containersByItemName = null;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.Default;

    var fields = zdo.Fields<Turret>();
    if (!Config.Instance.Turrets.DontTargetPlayers.Value)
      fields.Reset(static () => x => x.m_targetPlayers);
    else if (fields.UpdateValue(static () => x => x.m_targetPlayers, false))
      result |= ProcessResult.RecreateZDO;

    if (!Config.Instance.Turrets.DontTargetTames.Value)
      fields.Reset(static () => x => x.m_targetTamed);
    else if (fields.UpdateValue(static () => x => x.m_targetTamed, false))
      result |= ProcessResult.RecreateZDO;

    if (!Config.Instance.Turrets.DontTargetTames.Value)
      fields.Reset(static () => x => x.m_targetTamedConfig);
    else if (fields.UpdateValue(static () => x => x.m_targetTamedConfig, false))
      result |= ProcessResult.RecreateZDO;


    if (!Config.Instance.Turrets.LoadFromContainers.Value)
      return result | ProcessResult.UnregisterProcessor;

    if (_turrets is null || _containersByItemName is null)
      return result | ProcessResult.UnregisterProcessor;

    if (!CheckMinDistance(peers, zdo, Config.Instance.Turrets.LoadFromContainersMinPlayerDistance.Value))
      return result | ScheduleReprocessing();

    /// <see cref="Turret.RPC_AddAmmo"/>
    
    var maxLoaded = fields.GetInt(static () => x => x.m_maxAmmo);
    var currentAmmo = zdo.Vars.GetAmmo();
    var maxAdd = maxLoaded - currentAmmo;
    if (maxAdd < maxLoaded / 2)
      return result;

    var allowedAmmoDropPrefabName = currentAmmo > 0 ? zdo.Vars.GetAmmoType() : null;
    ItemDrop.ItemData? allowedAmmo = null;

    var addedAmmo = 0;
    List<ServersideQoLZDO>? toRemove = null;
    List<ItemDrop.ItemData>? removeSlots = null;

    foreach (var ammoItem in prefabInfo.Turret.m_allowedAmmo.Select(static x => x.m_ammo))
    {
      if (!string.IsNullOrEmpty(allowedAmmoDropPrefabName) && ammoItem.name != allowedAmmoDropPrefabName)
        continue;

      foreach (var containers in _containersByItemName.EnumerateAdjacent((zdo.ZDO.GetPosition(), ammoItem.m_itemData.m_shared)))
      {
        toRemove?.Clear();
        foreach (var containerZdo in containers)
        {
          if (Instance<ContainerRegistryProcessor>().GetState(containerZdo) is not { } containerState)
          {
            (toRemove ??= []).Add(containerZdo);
            continue;
          }

          var feedRangeSqr = containerState.FeedRange ?? Config.Instance.Turrets.LoadFromContainersRange.Value;
          feedRangeSqr *= feedRangeSqr;
          if (feedRangeSqr is 0f || Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) > feedRangeSqr)
            continue;

          if (containerZdo.Vars.GetInUse()) // || !CheckMinDistance(peers, containerZdo))
            continue; // in use or player to close

          var inventory = containerState.GetInventory();
          removeSlots?.Clear();
          var addAmmo = 0;
          var found = false;
          var requestOwn = false;
          foreach (var slot in inventory.Items.Where(x => new ItemDataKey(x) == ammoItem.m_itemData).OrderBy(static x => x.m_stack))
          {
            found = found || slot is { m_stack: > 0 };
            var take = Math.Min(maxAdd, slot.m_stack);
            if (take is 0)
              continue;
            else if (!containerZdo.IsOwnerOrUnassigned())
            {
              requestOwn = true;
              break;
            }

            allowedAmmoDropPrefabName = ammoItem.name;
            allowedAmmo = ammoItem.m_itemData;

            addAmmo += take;
            slot.m_stack -= take;
            if (slot.m_stack is 0)
              (removeSlots ??= new()).Add(slot);

            maxAdd -= take;
            if (maxAdd is 0)
              break;
          }

          if (requestOwn)
          {
            result |= ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(containerZdo, default));
            continue;
          }

          if (addAmmo is 0)
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

          currentAmmo += addAmmo;
          zdo.Vars.SetAmmo(currentAmmo);
          zdo.Vars.SetAmmoType(allowedAmmoDropPrefabName!);

          inventory.Save();

          addedAmmo += addAmmo;

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

      if (maxAdd is 0)
        break;
    }

    if (addedAmmo is not 0)
      ShowMessage(peers, zdo, Config.Instance.Localization.Value.Turrets.FormatAmmoAdded(prefabInfo.Piece.m_name, allowedAmmo!.m_shared.m_name, addedAmmo), Config.Instance.Turrets.AmmoAddedMessageType.Value);
    else if (currentAmmo is 0)
      ShowMessage(peers, zdo, Config.Instance.Localization.Value.Turrets.NoAmmoFound, Config.Instance.Turrets.NoAmmoMessageType.Value, DamageText.TextType.Bonus);

    _turrets.TryAdd(zdo);

    return result;
  }

  void OnContainerChanged(ServersideQoLZDO containerZdo, ContainerState state)
  {
    if (_turrets is null)
      throw new Exception("bug");

    var feedRangeSqr = state.FeedRange ?? Config.Instance.Turrets.LoadFromContainersRange.Value;
    feedRangeSqr *= feedRangeSqr;
    if (feedRangeSqr is 0f)
      return;

    if (state.GetInventory() is not { Items.Count: > 0 } inventory)
      return;

    foreach (var smelters in _turrets.EnumerateAdjacent(containerZdo.ZDO.GetPosition()))
    {
      foreach (var zdo in smelters)
      {
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition()) <= feedRangeSqr)
          ScheduleReprocessing(zdo);
      }
    }
  }
}
