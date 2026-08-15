using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.ContainerSizes;

[Processor("9212deb1-7a75-40e6-b74a-79843d5fe465")]
[RunBefore<ContainerRegistryProcessor>]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  readonly record struct ContainerSizeConfig(int Width, int Height, bool Growing);
  readonly Dictionary<int, ContainerSizeConfig> _containerSizes = [];

  protected override void Initialize()
  {
    _containerSizes.Clear();
    foreach (var (prefab, cfg) in Config.Instance.ContainerSizes)
    {
      if (Regex.Match(cfg.Value, @"^(?<w>\d+)x(?<h>\d+)(?<g>\+)?$") is not { Success: true } match)
      {
        Logger.LogWarning($"Invalid container size config value: {cfg.Value}");
        continue;
      }
      _containerSizes.Add(prefab, new(
          int.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture),
          int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture),
          match.Groups["g"].Success));
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
  {
    var state = Instance<ContainerRegistryProcessor>().GetState(zdo, prefabInfo.Container);
    var fields = zdo.Fields<Container>();
    var inventory = state.GetInventory();
    var width = inventory.Inventory.GetWidth();
    var height = inventory.Inventory.GetHeight();
    if (!_containerSizes.TryGetValue(zdo.ZDO.GetPrefab(), out var sizeCfg))
      sizeCfg = new(width, height, false);
    else if ((sizeCfg.Width, sizeCfg.Height) != (width, height))
    {
      if (inventory is { Items.Count: 0 })
      {
        fields.Set(static () => x => x.m_width, width = sizeCfg.Width);
        fields.Set(static () => x => x.m_height, height = sizeCfg.Height);
        return ProcessResult.RecreateZDO;
      }
    }

    if (!sizeCfg.Growing && (sizeCfg.Width, sizeCfg.Height) == (width, height))
      return ProcessResult.UnregisterProcessor;

    var checkShrink = true;
    if (sizeCfg.Growing)
    {
      checkShrink = false;
      var key = new ItemDataKey(inventory.Items[0]);
      for (int i = 1; !checkShrink && i < inventory.Items.Count; i++)
        checkShrink = key != new ItemDataKey(inventory.Items[i]);

      if (!checkShrink)
        sizeCfg = sizeCfg with { Height = Math.Max(sizeCfg.Height, Mathf.CeilToInt((float)inventory.Items.Count / sizeCfg.Width) + 1) };
    }

    var result = ProcessResult.Default;

    if ((width, height) != (sizeCfg.Width, sizeCfg.Height))
    {
      result = ProcessResult.RecreateZDO;
      if (checkShrink && inventory.Items.Count > sizeCfg.Width * sizeCfg.Height)
      {
        var found = false;
        for (var h = sizeCfg.Height; !found && h <= height; h++)
        {
          for (var w = sizeCfg.Width; !found && w <= width; w++)
          {
            if (inventory.Items.Count <= w * h)
            {
              found = true;
              sizeCfg = sizeCfg with { Width = w, Height = h };
            }
          }
        }

        if (!found || (width, height) == (sizeCfg.Width, sizeCfg.Height))
          result = ProcessResult.Default;
      }

      if (result is ProcessResult.RecreateZDO)
      {
        //Logger.DevLog($"Change {zdo.PrefabInfo.PrefabName} inventory size: {(width, height)} -> {(sizeCfg.Width, sizeCfg.Height)}, check shrink: {checkShrink}");
        fields.Set(static () => x => x.m_width, width = sizeCfg.Width);
        fields.Set(static () => x => x.m_height, height = sizeCfg.Height);
      }
    }

    if (result is ProcessResult.RecreateZDO && !zdo.IsOwnerOrUnassigned())
      result = ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(zdo, zdo.Vars.GetCreator(), state));

    return result;
  }
}
