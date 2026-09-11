using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Text.RegularExpressions;
using UnityEngine;
using static ServersideQoL.ContainerSigns.Config;

namespace ServersideQoL.ContainerSigns;

[Processor(Id)]
public sealed class ContainerAndSignProcessor : Processor<ContainerAndSignProcessor.PrefabInfo>
{
  public const string Id = "bbbb47b4-3b9f-4d63-8bb2-a6f388ae1180";
  public sealed record PrefabInfo(Container? Container, Sign? Sign) : ProcessorPrefabInfo
  {
    public override bool IsValid => Sign is not null || (Container is not null && PrefabInfo.HasComponent<Piece>() && PrefabInfo.HasComponent<PieceTable>());
  }

  readonly Dictionary<ServersideQoLZDO, List<ServersideQoLZDO>> _signsByChests = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _chestsBySigns = [];

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
    foreach (var zdo in _chestsBySigns.Keys)
      zdo.Destroy();
    _signsByChests.Clear();
    _chestsBySigns.Clear();

    _contentListRegex2 = new(Regex.Escape(Config.Instance.ChestSignsContentListPlaceholder.Value));
    _chestDataRevisions.Clear();
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;
  }

  protected override bool ClaimExclusive(ServersideQoLZDO zdo) => false; // let other processor process the signs

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.Container is not null)
    {
      var cfg = Config.Instance;
      var signOptions = cfg.GetSignOptions(zdo.ZDO.GetPrefab());
      if (signOptions is SignOptions.None || !cfg.Advanced.Value.ChestSignOffsets.TryGetValue(zdo.ZDO.GetPrefab(), out var signOffset) || _signsByChests.ContainsKey(zdo) || zdo.Vars.GetCreator() == default)
        return ProcessResult.UnregisterProcessor;

      var text = zdo.Vars.GetText(null!);
      if (text is null)
      {
        if (!zdo.IsOwnerOrUnassigned())
          return ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(zdo, default));
        zdo.Vars.SetText(text = cfg.ChestSignsDefaultText.Value);
      }
      var p = zdo.ZDO.GetPosition();
      var r = zdo.ZDO.GetRotation();
      var rot = r.eulerAngles.y + 90;
      var signs = new List<ServersideQoLZDO>();
      p.y += signOffset.Top / 2;
      if (signOptions.HasFlag(SignOptions.Left))
        signs.Add(PlacePiece(p + r * Vector3.right * signOffset.Left, Prefabs.Sign, rot));
      if (signOptions.HasFlag(SignOptions.Right))
        signs.Add(PlacePiece(p + r * Vector3.left * signOffset.Right, Prefabs.Sign, rot + 180));
      if (signOptions.HasFlag(SignOptions.Front))
        signs.Add(PlacePiece(p + r * Vector3.forward * signOffset.Front, Prefabs.Sign, rot + 270));
      if (signOptions.HasFlag(SignOptions.Back))
        signs.Add(PlacePiece(p + r * Vector3.back * signOffset.Back, Prefabs.Sign, rot + 90));
      p = zdo.ZDO.GetPosition();
      p.y += signOffset.Top;
      if (signOptions.HasFlag(SignOptions.TopLongitudinal))
        signs.Add(PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot - 90, 0)));
      if (signOptions.HasFlag(SignOptions.TopLateral))
        signs.Add(PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot, 0)));
      _signsByChests.Add(zdo, signs);
      foreach (var sign in signs)
      {
        _chestsBySigns.Add(sign, zdo);
        sign.Vars.SetText(text);
        sign.Fields<WearNTear>().Set(static () => x => x.m_supports, false);
        //sign.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, true);
        //sign.Destroyed += _ => RPC.Remove(zdo);
      }
      zdo.Destroyed += OnChestDestroyed;

      return ProcessResult.UnregisterProcessor | ProcessResult.ReregisterOnRecreated;
    }
    else if (prefabInfo.Sign is not null)
    {
      if (!_chestsBySigns.TryGetValue(zdo, out var chest))
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
    else
    {
      Logger.DevLog($"Unexpected ZDO ({prefabInfo.PrefabInfo.PrefabName})");
      return ProcessResult.UnregisterProcessor;
    }
  }

  void OnChestDestroyed(ServersideQoLZDO zdo)
  {
    if (_signsByChests.Remove(zdo, out var signs))
    {
      foreach (var sign in signs)
      {
        _chestsBySigns.Remove(sign);
        sign.Destroy();
      }
    }
  }

  void OnContainerChanged(ServersideQoLZDO zdo, ContainerState state)
  {
    if (!_signsByChests.TryGetValue(zdo, out var signs))
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
