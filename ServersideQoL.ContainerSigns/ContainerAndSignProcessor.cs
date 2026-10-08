extern alias Signs;
using ServersideQoL.Processors;
using ServersideQoL.Utilities;
using System.Text.RegularExpressions;
using UnityEngine;
using static ServersideQoL.ContainerSigns.Config;

namespace ServersideQoL.ContainerSigns;

[Processor(Id)]
[RunAfter<ContainerRegistryProcessor>]
//[RunBefore(Signs::ServersideQoL.Signs.SignProcessor.Id)]
public sealed class ContainerAndSignProcessor : Processor<ContainerAndSignProcessor.PrefabInfo>
{
  public const string Id = "bbbb47b4-3b9f-4d63-8bb2-a6f388ae1180";
  public sealed record PrefabInfo(Container? Container, Sign? Sign) : ProcessorPrefabInfo
  {
    public override bool IsValid => Sign is not null || (Container is not null && PrefabInfo.HasComponent<Piece>() && PrefabInfo.HasComponent<PieceTable>() && !PrefabInfo.HasComponent<ZSyncTransform>());
  }

  readonly Dictionary<ServersideQoLZDO, List<ServersideQoLZDO>> _signsByChests = [];
  readonly Dictionary<ServersideQoLZDO, List<ServersideQoLZDO>> _userSignsByChests = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _chestsBySigns = [];
  SectorDictionary<HashSet<ServersideQoLZDO>>? _containers;
  Regex? _chestAutoStorePickupRangeRegex;
  Regex? _chestAutoProcessFeedRangeRegex;
  Regex? _chestTameAssistFeedRangeRegex;

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
    {
      if (zdo.IsModCreator())
        zdo.Destroy();
    }
    _signsByChests.Clear();
    _userSignsByChests.Clear();
    _chestsBySigns.Clear();

    if (Config.Instance.AutoStorePickupRangeSignPrefix is not null)
    {
      Config.Instance.AutoStorePickupRangeSignPrefix.SettingChanged -= OnConfigChanged;
      OnConfigChanged(null, null);
      Config.Instance.AutoStorePickupRangeSignPrefix.SettingChanged += OnConfigChanged;

      void OnConfigChanged(object? sender, EventArgs? e)
      {
        var str = Config.Instance.AutoStorePickupRangeSignPrefix?.Value ?? "";
        var str2 = str.Replace("\uFE0F", ""); // strip variation selector;
        _chestAutoStorePickupRangeRegex = str == str2 ?
          new($@"{Regex.Escape(str)}(?<R>\d+)") :
          new($@"(?:{Regex.Escape(str)}|{Regex.Escape(str2)})(?<R>\d+)");
      }
    }

    if (Config.Instance.AutoProcessFeedFromContainersRangeSignPrefix is not null)
    {
      Config.Instance.AutoProcessFeedFromContainersRangeSignPrefix.SettingChanged -= OnConfigChanged;
      OnConfigChanged(null, null);
      Config.Instance.AutoProcessFeedFromContainersRangeSignPrefix.SettingChanged += OnConfigChanged;

      void OnConfigChanged(object? sender, EventArgs? e)
      {
        var str = Config.Instance.AutoProcessFeedFromContainersRangeSignPrefix?.Value ?? "";
        var str2 = str.Replace("\uFE0F", ""); // strip variation selector;
        _chestAutoProcessFeedRangeRegex = str == str2 ?
          new($@"{Regex.Escape(str)}(?<R>\d+)") :
          new($@"(?:{Regex.Escape(str)}|{Regex.Escape(str2)})(?<R>\d+)");
      }
    }

    if (Config.Instance.TameAssistFeedFromContainersRangeSignPrefix is not null)
    {
      Config.Instance.TameAssistFeedFromContainersRangeSignPrefix.SettingChanged -= OnConfigChanged;
      OnConfigChanged(null, null);
      Config.Instance.TameAssistFeedFromContainersRangeSignPrefix.SettingChanged += OnConfigChanged;

      void OnConfigChanged(object? sender, EventArgs? e)
      {
        var str = Config.Instance.TameAssistFeedFromContainersRangeSignPrefix?.Value ?? "";
        var str2 = str.Replace("\uFE0F", ""); // strip variation selector;
        _chestTameAssistFeedRangeRegex = str == str2 ?
          new($@"{Regex.Escape(str)}(?<R>\d+)") :
          new($@"(?:{Regex.Escape(str)}|{Regex.Escape(str2)})(?<R>\d+)");
      }
    }

    _contentListRegex2 = new(Regex.Escape(Config.Instance.ChestSignsContentListPlaceholder.Value));
    _chestDataRevisions.Clear();
    Instance<ContainerRegistryProcessor>().ContainerChanged -= OnContainerChanged;
    Instance<ContainerRegistryProcessor>().ContainerChanged += OnContainerChanged;

    _containers = null;
    if (!string.IsNullOrWhiteSpace(Config.Instance.SignConnectText.Value))
      _containers = new(ZoneSystem.c_ZoneSizeHalf);
  }

  protected override bool ClaimExclusive(ServersideQoLZDO zdo) => false; // let other processor process the signs

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.Container is not null)
    {
      var result = ProcessResult.UnregisterProcessor;
      if (_containers is not null)
      {
        _containers.TryAdd(zdo);
        result |= ProcessResult.ReregisterOnRecreated;
      }

      var cfg = Config.Instance;
      var signOptions = cfg.GetSignOptions(zdo.ZDO.GetPrefab());
      if (signOptions is SignOptions.None || !cfg.Advanced.Value.ChestSignOffsets.TryGetValue(zdo.ZDO.GetPrefab(), out var signOffset) /*|| zdo.Vars.GetCreator() == default*/)
        return result;

      if (!_signsByChests.ContainsKey(zdo))
      {
        if (zdo.Vars.GetText(null!) is not { } text)
        {
          if (!zdo.IsOwnerOrUnassigned())
            return ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(zdo, default));
          zdo.Vars.SetText(text = cfg.ChestSignsDefaultText.Value);
        }
        var p = zdo.ZDO.GetPosition();
        var r = zdo.ZDO.GetRotation();
        var rot = r.eulerAngles.y + 90;
        var signs = new List<ServersideQoLZDO>();
        p.y += signOffset.VerticalOffset;
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
        zdo.Destroyed -= OnChestDestroyed;
        zdo.Destroyed += OnChestDestroyed;
      }

      return result;
    }
    else if (prefabInfo.Sign is not null)
    {
      string? text = null;
      string? newText = null;
      if (!_chestsBySigns.TryGetValue(zdo, out var chest))
      {
        const ZDOExtraData.ConnectionType ConnectionType = (ZDOExtraData.ConnectionType)0x80;
        if (_containers is null)
          return ProcessResult.UnregisterProcessor;

        text ??= zdo.Vars.GetText();
        newText ??= text;

        if (ZDOMan.instance.GetZDO(zdo.ZDO.GetConnectionZDOID(ConnectionType))?.ServersideQoLZDO is { } connectedZdo && GetPrefabInfo(connectedZdo).HasComponent<Container>())
          chest = connectedZdo;
        else
        {
          if (text.RemoveRichTextTags() != Config.Instance.SignConnectText.Value)
            return default;

          chest = null;
          var minDistSqr = float.PositiveInfinity;
          foreach (var containers in _containers.EnumerateAdjacent(zdo.ZDO.GetPosition()))
          {
            foreach (var containerZdo in containers)
            {
              var distSqr = Utils.DistanceSqr(zdo.ZDO.GetPosition(), containerZdo.ZDO.GetPosition());
              if (distSqr >= minDistSqr)
                continue;
              minDistSqr = distSqr;
              chest = containerZdo;
            }
          }

          if (chest is null)
            return default;

          zdo.ZDO.SetConnection(ConnectionType, chest.ZDO.m_uid);
        }

        if (chest.Vars.GetText(null!) is { } containerText)
          newText = containerText;
        else
        {
          if (!chest.IsOwnerOrUnassigned())
            return ScheduleReprocessing(Instance<ContainerRegistryProcessor>().RequestOwnership(chest, default));
          chest.Vars.SetText(newText = Config.Instance.ChestSignsDefaultText.Value);
        }
        _chestsBySigns.Add(zdo, chest);
        if (!_userSignsByChests.TryGetValue(chest, out var set))
        {
          _userSignsByChests.Add(chest, set = []);
          chest.Destroyed -= OnChestDestroyed;
          chest.Destroyed += OnChestDestroyed;
        }
        set.Add(zdo);
        zdo.Destroyed += zdo =>
        {
          _chestsBySigns.Remove(zdo);
          set.Remove(zdo);
        };
      }

      text ??= zdo.Vars.GetText();
      newText ??= text;
      ContainerState? containerState = null;
      if (_chestAutoStorePickupRangeRegex is not null && Config.Instance.AutoStorePickup && Config.Instance.AutoStorePickupMaxRange is { } autoPickupMaxRange)
      {
        containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
        containerState.AutoStorePickupRange = null;
        newText = _chestAutoStorePickupRangeRegex.Replace(newText, match =>
        {
          var result = match.Value;
          var range = int.Parse(match.Groups["R"].Value);
          if (range > autoPickupMaxRange)
          {
            range = autoPickupMaxRange;
            result = Invariant($"{Config.Instance.AutoStorePickupRangeSignPrefix}{range}");
          }
          containerState.AutoStorePickupRange = range;
          return result;
        });
      }
      if (_chestAutoProcessFeedRangeRegex is not null && Config.Instance.AutoProcessFeedFromContainers && Config.Instance.AutoProcessFeedFromContainersMaxRange is { } feedMaxRange)
      {
        containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
        containerState.AutoProcessFeedRange = null;
        newText = _chestAutoProcessFeedRangeRegex.Replace(newText, match =>
        {
          var result = match.Value;
          var range = int.Parse(match.Groups["R"].Value);
          if (range > feedMaxRange)
          {
            range = feedMaxRange;
            result = Invariant($"{Config.Instance.AutoProcessFeedFromContainersRangeSignPrefix}{range}");
          }
          containerState.AutoProcessFeedRange = range;
          return result;
        });
      }
      if (_chestTameAssistFeedRangeRegex is not null && Config.Instance.TameAssistFeedFromContainers && Config.Instance.TameAssistFeedFromContainersMaxRange is { } tamefeedMaxRange)
      {
        containerState ??= Instance<ContainerRegistryProcessor>().GetState(chest)!;
        containerState.TameAssistFeedRange = null;
        newText = _chestTameAssistFeedRangeRegex.Replace(newText, match =>
        {
          var result = match.Value;
          var range = int.Parse(match.Groups["R"].Value);
          if (range > tamefeedMaxRange)
          {
            range = tamefeedMaxRange;
            result = Invariant($"{Config.Instance.TameAssistFeedFromContainersRangeSignPrefix}{range}");
          }
          containerState.TameAssistFeedRange = range;
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
    _userSignsByChests.Remove(zdo);
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
    if (!_signsByChests.TryGetValue(zdo, out var signs) && !_userSignsByChests.TryGetValue(zdo, out signs))
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
