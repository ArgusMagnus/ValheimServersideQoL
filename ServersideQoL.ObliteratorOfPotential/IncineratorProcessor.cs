using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using static ItemSets;

namespace ServersideQoL.ObliteratorOfPotential;

[Processor(Id)]
[RunBefore<IncineratorRegistryProcessor>]
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
  ServersideQoLZDO? _obliteratorOfPotential;

  protected override void Initialize()
  {
    _obliteratorOfPotential ??= PlacedObjects.FirstOrDefault(x => GetProcessorPrefabInfo(x) is { IsIncinerator: true });
    if (_obliteratorOfPotential is not null)
    {
      if (Config.Instance.PlaceNearForgeOfPotential.Value)
        _obliteratorOfPotential.Fields<Container>().Set(static () => x => x.m_name, $"{Localization.instance.Localize(GetProcessorPrefabInfo(_obliteratorOfPotential)!.Container!.m_name)} of Potential");
      else
      {
        DestroyObject(_obliteratorOfPotential);
        _obliteratorOfPotential = null;
      }
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
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.IsUpgradeStation)
    {
      if (!Config.Instance.PlaceNearForgeOfPotential.Value || _obliteratorOfPotential is not null)
        return ProcessResult.UnregisterProcessor;

      var pos = zdo.ZDO.GetPosition();
      pos += zdo.ZDO.GetRotation() * Vector3.forward * 1f;
      pos.y -= 0.25f;
      _obliteratorOfPotential = PlacePiece(pos, Prefabs.Incinerator, zdo.ZDO.GetRotation(), CreatorMarkers.ProcessorOwned);
      _obliteratorOfPotential.Fields<Container>().Set(static () => x => x.m_name,
        $"{Localization.instance.Localize(GetProcessorPrefabInfo(_obliteratorOfPotential)!.Container!.m_name)} of Potential");
      return ProcessResult.UnregisterProcessor;
    }
    else if (prefabInfo.IsIncinerator)
    {
      if (zdo != _obliteratorOfPotential && !Config.Instance.EveryObliteratorCanUpgrade.Value)
        return ProcessResult.UnregisterProcessor;

      foreach (var upgrader in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
      {
        if (!_itemsByUpgrader.TryGetValue(upgrader.m_dropPrefab, out var set))
          continue;

        int remainingUpgraderStack = upgrader.m_stack;
        for (int i = 0; i < upgrader.m_stack; i++)
        {
          foreach (var sacrifice in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
          {
            if (upgrader == sacrifice || !set.Contains(sacrifice.m_dropPrefab))
              continue;

            var isArmor = sacrifice.m_shared.m_itemType is ItemDrop.ItemData.ItemType.Chest or ItemDrop.ItemData.ItemType.Legs or ItemDrop.ItemData.ItemType.Helmet or ItemDrop.ItemData.ItemType.Shoulder;
            var sacrificeArmor = isArmor ? sacrifice.GetArmor() : 0;
            var sacrificeBaseBlockPower = isArmor ? 0 : sacrifice.GetBaseBlockPower();
            var sacrificeTotalDamage = isArmor ? 0 : sacrifice.GetDamage().GetTotalDamage();
            if (sacrificeArmor <= 0 && sacrificeBaseBlockPower <= 0 && sacrificeTotalDamage <= 0)
              continue;

            foreach (var item in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
            {
              if (item == upgrader || item == sacrifice || item.m_shared.m_itemType != sacrifice.m_shared.m_itemType || item.m_shared.m_skillType != sacrifice.m_shared.m_skillType)
                continue;

              if (isArmor)
              {
                if (item.m_shared.m_armorPerLevel <= 0 || item.GetArmor() >= sacrificeArmor)
                  continue;
              }
              else
              {
                if (item.m_shared.m_blockPowerPerLevel <= 0 && item.m_shared.m_damagesPerLevel.GetTotalDamage() <= 0)
                  continue;
                if (item.m_shared.m_blockPowerPerLevel > 0 && item.GetBaseBlockPower() >= sacrificeBaseBlockPower)
                  continue;
                if (item.m_shared.m_damagesPerLevel.GetTotalDamage() > 0 && item.GetDamage().GetTotalDamage() >= sacrificeTotalDamage)
                  continue;
              }

              ItemDrop.ItemData? newItem = null;
              while (true)
              {
                if (isArmor)
                {
                  if (item.m_shared.m_armorPerLevel > 0 && (newItem ??= item.Clone()).GetArmor() >= sacrificeArmor)
                    break;
                }
                else
                {
                  if (item.m_shared.m_blockPowerPerLevel > 0 && (newItem ??= item.Clone()).GetBaseBlockPower() >= sacrificeBaseBlockPower)
                    break;
                  if (item.m_shared.m_damagesPerLevel.GetTotalDamage() > 0 && (newItem ??= item.Clone()).GetDamage().GetTotalDamage() >= sacrificeTotalDamage)
                    break;
                }
                if (newItem is null)
                  break;
                newItem.m_quality++;
              }

              if (newItem is null || newItem.m_quality == item.m_quality)
                continue;

              remainingUpgraderStack--;
              Instance<IncineratorRegistryProcessor>().ReserveItem(upgrader);
              Instance<IncineratorRegistryProcessor>().ReserveItem(sacrifice);
              Instance<IncineratorRegistryProcessor>().ReserveItem(item);
              Instance<IncineratorRegistryProcessor>().AddConversionResult(newItem);
            }
          }
        }
        if (remainingUpgraderStack > 0 && remainingUpgraderStack != upgrader.m_stack)
        {
          var item = upgrader.Clone();
          item.m_stack = remainingUpgraderStack;
          Instance<IncineratorRegistryProcessor>().AddConversionResult(item);
        }
      }

      return default;
    }
    else
    {
      Logger.DevLog($"Unexpected ZDO: {prefabInfo.PrefabInfo.PrefabName}");
      return ProcessResult.UnregisterProcessor;
    }
  }
}
