using ServersideQoL.Processors;
using System.Text.RegularExpressions;

namespace ServersideQoL.ContainerSigns;

[Processor("9091713d-f86e-43ab-bf37-9d64a9649858")]
[RunAfter("806bdb85-c857-4154-a246-a0b1d0917987")] // Signs.SignProcessor
public sealed class SignProcessor : Processor<ProcessorPrefabInfo<Sign>>
{
  internal const string MagnetEmoji = "🧲";
  readonly Regex _chestPickupRangeRegex = new($@"{Regex.Escape(MagnetEmoji)}\s*(?<R>\d+)");

  internal const string LeftRightArrowEmoji = "↔️";
  readonly Regex _chestFeedRangeRegex = new($@"{Regex.Escape(LeftRightArrowEmoji)}\s*(?<R>\d+)");

  //internal const string LinkEmoji = "🔗";
  //readonly Regex _incineratorTagRegex = new($@"{Regex.Escape(LinkEmoji)}\s*(?<T>\w*)");

  const string ContentListStart = "<i ls></i>";
  const string ContentListEnd = "<i le></i>";
  readonly Regex _contentListRegex = new($@"{ContentListStart}.*?{ContentListEnd}");
  Regex _contentListRegex2 = default!;

  readonly Dictionary<ServersideQoLZDO, uint> _chestDataRevisions = [];

  protected override void Initialize()
  {
    _contentListRegex2 = new(Regex.Escape(Config.Instance.ChestSignsContentListPlaceholder.Value));

    _chestDataRevisions.Clear();
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<Sign> prefabInfo)
  {
    if (!Instance<ContainerProcessor>().ChestsBySigns.TryGetValue(zdo, out var chest))
      return ProcessResult.UnregisterProcessor;

    var text = zdo.Vars.GetText();
    var newText = text;
    ContainerState? containerState = null;
    if (Config.Instance.AutoPickup)
    {
      containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
      containerState.PickupRange = null;
      newText = _chestPickupRangeRegex.Replace(newText, match =>
      {
        var result = match.Value;
        var range = int.Parse(match.Groups["R"].Value);
        if (range > Config.Instance.AutoPickupMaxRange.Value)
        {
          range = Config.Instance.AutoPickupMaxRange.Value;
          result = Invariant($"{MagnetEmoji}{range}");
        }
        containerState.PickupRange = range;
        return result;
      });
    }
    if (Config.Instance.FeedFromContainers)
    {
      containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
      containerState.FeedRange = null;
      newText = _chestFeedRangeRegex.Replace(newText, match =>
      {
        var result = match.Value;
        var range = int.Parse(match.Groups["R"].Value);
        if (range > Config.Instance.FeedFromContainersMaxRange.Value)
        {
          range = Config.Instance.FeedFromContainersMaxRange.Value;
          result = Invariant($"{LeftRightArrowEmoji}{range}");
        }
        containerState.FeedRange = range;
        return result;
      });
    }

    var found = false;
    string EvaluateMatch(Match match)
    {
      found = true;
      if (Config.Instance.ChestSignsContentListMaxCount.Value <= 0)
        return Config.Instance.ChestSignsContentListPlaceholder.Value;

      containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
      if (containerState.GetInventory() is not { Items.Count: > 0 } inventory)
        return Config.Instance.ChestSignsContentListPlaceholder.Value;

      var list = inventory.Items
          .GroupBy(static x => x.m_dropPrefab.name, static (k, g) => (Name: k, Count: g.Sum(static x => x.m_stack)))
          .OrderByDescending(static x => x.Count)
          .ToList();

      var items = list.AsEnumerable();
      if (list.Count > Config.Instance.ChestSignsContentListMaxCount.Value)
      {
        items = list
            .Take(Config.Instance.ChestSignsContentListMaxCount.Value - 1)
            .Append((Config.Instance.ChestSignsContentListNameRest.Value, list.Skip(Config.Instance.ChestSignsContentListMaxCount.Value - 1).Sum(static x => x.Count)));
      }

      var listStr = string.Join(Config.Instance.ChestSignsContentListSeparator.Value, items
          .Select(x => string.Format(Config.Instance.ChestSignsContentListEntryFormat.Value, x.Name, x.Count)));

      return $"{ContentListStart}{listStr}{ContentListEnd}";
    }

    newText = _contentListRegex.Replace(newText, EvaluateMatch, 1);
    if (!found)
      newText = _contentListRegex2.Replace(newText, EvaluateMatch, 1);

    if (newText != text)
      zdo.Vars.SetText(text = newText);

    if (text != chest.Vars.GetText())
    {
      if (!chest.IsOwnerOrUnassigned())
        return ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(chest, default));

      chest.Vars.SetText(text);
    }

    return default;
  }

  void OnContainerChanged(ServersideQoLZDO zdo, ContainerState state)
  {
    if (!Instance<ContainerProcessor>().SignsByChests.TryGetValue(zdo, out var signs))
      return;

    var dataRevision = zdo.ZDO.DataRevision;
    if (!_chestDataRevisions.TryGetValue(zdo, out var lastRevision))
    {
      _chestDataRevisions.Add(zdo, dataRevision);
      zdo.Destroyed += x => _chestDataRevisions.Remove(x);
    }
    else if (lastRevision != dataRevision)
      _chestDataRevisions[zdo] = dataRevision;
    else
      return;

    var text = zdo.Vars.GetText();
    foreach (var sign in signs)
    {
      sign.Vars.SetText(text);
      ScheduleReprocessing(sign);
    }
  }
}
