using System.ComponentModel;
using UnityEngine;
using YamlDotNet.Core.Tokens;

namespace ServersideQoL.AutoMapTables;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
public sealed class AutoMapTablesProcessor : Processor<AutoMapTablesProcessor.PrefabInfo>
{
  public const string Id = "05450dd6-13bd-42cc-9bd3-b1eed5e501af";

  public sealed record PrefabInfo(MapTable? MapTable, PrivateArea? PrivateArea, TeleportWorld? TeleportWorld, Ship? Ship, MineRock5? MineRock5, LocationProxy? LocationProxy) : ProcessorPrefabInfo
  {
    public Config.OreDepositConfig? MineRockPinConfig => field ??= MineRock5 is not null && Config.Instance.AutoUpdateOreDeposits.TryGetValue(PrefabInfo.PrefabHash, out var value) ? value : null;
    public Piece? Piece => field ??= PrefabInfo.GetComponent<Piece>();
    public override bool IsValid => this switch
    {
      { PrivateArea: not null } or { TeleportWorld: not null } or { Ship: not null } => Piece is not null && PrefabInfo.HasComponent<PieceTable>(),
      { MineRock5: not null } => MineRockPinConfig is not null,
      _ => true
    };
  }

  readonly Dictionary<ServersideQoLZDO, MapTableState> _mapTables = [];
  readonly Dictionary<PlayerID, PlayerState> _playerStates = [];
  readonly HashSet<ServersideQoLZDO> _wards = [];
  readonly Dictionary<PlayerID, (Peer, PlayerState)> _updateList = [];
  readonly List<Pin> _pins = [];
  readonly HashSet<(Minimap.PinType, string)> _oreDepositPins = [];
  byte[]? _emptyExplored;
  float _mapTableRangeSqr;
  float _oreDepositRangeSqr;
  float _dungeonRangeSqr;

  static readonly ServerVar<HashSet<PlayerID>> __playerIDsVar = AutoMapTablesPlugin.RegisterServerVar<HashSet<PlayerID>>("PlayerIDs");

  protected override void Initialize()
  {
    _mapTableRangeSqr = Config.Instance.MapTableRange.Value * Config.Instance.MapTableRange.Value;
    _oreDepositRangeSqr = Config.Instance.OreDepositsDiscoverRange.Value * Config.Instance.OreDepositsDiscoverRange.Value;
    _dungeonRangeSqr = Config.Instance.DungeonsDiscoverRange.Value * Config.Instance.DungeonsDiscoverRange.Value;

    _oreDepositPins.Clear();
    foreach (var (pin, label) in Config.Instance.AutoUpdateOreDeposits.Values)
    {
      if (pin.Value is not Minimap.PinType.None)
        _oreDepositPins.Add((pin.Value, label.Value));
    }

    _mapTables.Clear();
    _playerStates.Clear();
    _wards.Clear();

    foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
    {
      switch (GetProcessorPrefabInfo(zdo))
      {
        case { MapTable: not null }:
          _mapTables.Add(zdo, new(zdo));
          zdo.Destroyed += OnMapTableDestroyed;
          break;

        case { PrivateArea: not null }:
          _wards.Add(zdo);
          zdo.Destroyed += OnWardDestroyed;
          break;

        case { TeleportWorld: not null }:
          GetOrAddPlayerState(zdo.Vars.GetCreator()).Portals.Add(zdo);
          zdo.Destroyed += OnPortalDestroyed;
          break;

        case { Ship: not null }:
          GetOrAddPlayerState(zdo.Vars.GetCreator()).Ships.Add(zdo);
          zdo.Destroyed += OnShipDestroyed;
          break;

        case { MineRockPinConfig: not null }:
          if (!Character.InInterior(zdo.ZDO.GetPosition()))
          {
            zdo.Destroyed += OnOreDepositDestroyed;
            if (__playerIDsVar.Get(zdo) is { } playerIDs)
            {
              foreach (var id in playerIDs)
                GetOrAddPlayerState(id).OreVeins.Add(zdo);
            }
          }
          break;

        case { LocationProxy: not null }:
          {
            if (__playerIDsVar.Get(zdo) is { } playerIDs)
            {
              foreach (var id in playerIDs)
                GetOrAddPlayerState(id).Dungeons.Add(zdo, null);

              if (Config.Instance.DungeonsPinType.Value is not Minimap.PinType.None)
              {
                var hash = zdo.Vars.GetLocation();
                if (hash is not 0 && ZoneSystem.instance.GetLocationsByHash().TryGetValue(hash, out var location) &&
                    location.m_prefab is { IsValid: true, IsLoaded: false, IsLoading: false })
                {
                  location.m_prefab.LoadAsync();
                }
              }
            }
          }
          break;
      }
    }

    _playerStates.Remove(default);

    if (_wards.Count is not 0)
    {
      foreach (var (mapTable, mapTableState) in _mapTables)
      {
        foreach (var ward in _wards)
        {
          if (Utils.DistanceXZ(ward.ZDO.GetPosition(), mapTable.ZDO.GetPosition()) > ward.Fields<PrivateArea>().GetFloat(static () => x => x.m_radius))
            continue;

          (mapTableState.Wards ??= []).Add(ward);
        }

        UpdateMapTablePermittedPlayerIDs(mapTableState);
      }
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.MapTable is not null)
    {
      if (!_mapTables.TryGetValue(zdo, out var state))
      {
        _mapTables.Add(zdo, state = new(zdo));
        zdo.Destroyed += OnMapTableDestroyed;

        foreach (var ward in _wards)
        {
          if (Utils.DistanceXZ(ward.ZDO.GetPosition(), zdo.ZDO.GetPosition()) > ward.Fields<PrivateArea>().GetFloat(static () => x => x.m_radius))
            continue;

          (state.Wards ??= []).Add(ward);
        }

        UpdateMapTablePermittedPlayerIDs(state);
      }

      foreach (var peer in peers.Enumerate())
      {
        if (peer.PlayerState?.PlayerID is not { } playerID)
          continue;
        if (state.Wards is { Count: > 0 } && !state.PermittedPlayerIDs!.Contains(playerID))
          continue;
        if (!_playerStates.TryGetValue(playerID, out var playerState) || playerState.UpToDateMapTables.Contains(zdo))
          continue;
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), peer.RefPos) > _mapTableRangeSqr)
          continue;
        _updateList.Add(playerID, (peer, playerState));
      }

      if (_updateList.Count > 0 || state.LastDataRevision != zdo.ZDO.DataRevision)
      {
        UpdateMapTable(state, _updateList);
        _updateList.Clear();
      }

      zdo.DelaySchedulingFor(0.5f);
      return ProcessResult.ScheduleReprocessing;
    }

    if (prefabInfo.PrivateArea is not null)
    {
      if (_wards.Add(zdo))
      {
        zdo.Destroyed += OnWardDestroyed;
        foreach (var (mapTable, mapTableState) in _mapTables)
        {
          if (Utils.DistanceXZ(zdo.ZDO.GetPosition(), mapTable.ZDO.GetPosition()) > zdo.Fields<PrivateArea>().GetFloat(static () => x => x.m_radius))
            continue;

          (mapTableState.Wards ??= []).Add(zdo);
        }
      }

      foreach (var state in _mapTables.Values)
        UpdateMapTablePermittedPlayerIDs(state);
    }

    if (prefabInfo.TeleportWorld is not null)
    {
      var playerID = zdo.Vars.GetCreator();
      if (playerID.Value is 0)
        return ProcessResult.UnregisterProcessor;
      if (peers.Any(x => x.PlayerState?.PlayerID == playerID))
      {
        var state = GetOrAddPlayerState(playerID);
        if (state.Portals.Add(zdo))
        {
          zdo.Destroyed += OnPortalDestroyed;
          state.UpToDateMapTables.Clear();
        }
      }
      return default;
    }

    if (prefabInfo.Ship is not null)
    {
      var playerID = zdo.Vars.GetCreator();
      if (playerID.Value is 0)
        return ProcessResult.UnregisterProcessor;
      if (peers.Any(x => x.PlayerState?.PlayerID == playerID))
      {
        var state = GetOrAddPlayerState(playerID);
        if (state.Ships.Add(zdo))
        {
          zdo.Destroyed += OnShipDestroyed;
          state.UpToDateMapTables.Clear();
        }
      }
      return default;
    }

    if (prefabInfo is { MineRock5: not null, MineRockPinConfig: not null })
    {
      if (Character.InInterior(zdo.ZDO.GetPosition()))
        return ProcessResult.UnregisterProcessor;

      HashSet<PlayerID>? ids = null;
      foreach (var peer in peers.Enumerate())
      {
        if (peer.PlayerState?.PlayerID is not { } playerID || !_playerStates.TryGetValue(playerID, out var playerState))
          continue;
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), peer.RefPos) > _oreDepositRangeSqr)
          continue;
        if (!playerState.OreVeins.Add(zdo))
          continue;
        zdo.Destroyed -= OnOreDepositDestroyed;
        zdo.Destroyed += OnOreDepositDestroyed;
        ids ??= __playerIDsVar.Get(zdo) ?? [];
        ids.Add(playerID);
        playerState.UpToDateMapTables.Clear();
        if (prefabInfo.MineRockPinConfig.PinType.Value is not Minimap.PinType.None)
          ShowMessage([peer], zdo, Config.Instance.Localization.Value.Discovered(prefabInfo.MineRock5.m_name), Config.Instance.DiscoveredMessageType.Value);
      }
      if (ids is not null)
        __playerIDsVar.Set(zdo, ids);
      return default;
    }

    if (prefabInfo.LocationProxy is not null)
    {
      var hash = zdo.Vars.GetLocation();
      if (hash is 0)
        return default;

      using var loc = ZoneSystem.instance.GetAndLoadLocationByHash(hash);
      if (!loc.IsValid)
        return ProcessResult.UnregisterProcessor;

      if (loc.Prefab is not { } prefab)
        return ProcessResult.ScheduleReprocessing;

      HashSet<PlayerID>? ids = null;
      foreach (var component in prefab.GetComponentsInChildren<Teleport>())
      {
        var pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * component.gameObject.transform.position;
        if (Character.InInterior(pos))
          continue;

        foreach (var peer in peers.Enumerate())
        {
          if (peer.PlayerState?.PlayerID is not { } playerID || !_playerStates.TryGetValue(playerID, out var playerState))
            continue;
          if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), peer.RefPos) > _dungeonRangeSqr)
            continue;
          if (!playerState.Dungeons.TryAdd(zdo, (pos, component.m_enterText)))
            continue;
          ids ??= __playerIDsVar.Get(zdo) ?? [];
          ids.Add(playerID);
          playerState.UpToDateMapTables.Clear();
          if (Config.Instance.DungeonsPinType.Value is not Minimap.PinType.None)
            ShowMessage([peer], zdo, Config.Instance.Localization.Value.Discovered(component.m_enterText), Config.Instance.DiscoveredMessageType.Value);
        }

        break;
      }

      if (ids is not null)
        __playerIDsVar.Set(zdo, ids);

      zdo.DelaySchedulingFor(0.5f);
      return ProcessResult.ScheduleReprocessing;
    }

    Logger.DevLog($"Unexpected prefab: {prefabInfo.PrefabInfo.PrefabName}");
    return ProcessResult.UnregisterProcessor;
  }

  PlayerState GetOrAddPlayerState(PlayerID playerID)
  {
    if (!_playerStates.TryGetValue(playerID, out var state))
      _playerStates.Add(playerID, state = new(playerID));
    return state;
  }

  static void AddPermittedPlayerIDs(ServersideQoLZDO ward, HashSet<PlayerID> permittedPlayerIDs)
  {
    ward.AssertIs<PrivateArea>();
    System.Diagnostics.Debug.Assert(ward.Vars.GetEnabled());

    permittedPlayerIDs.Add(ward.Vars.GetCreator());

    /// <see cref="PrivateArea.GetPermittedPlayers"/>
    var count = ward.Vars.GetPermitted();
    for (int i = 0; i < count; i++)
    {
      var playerID = ward.ZDO.GetLong(Invariant($"pu_id{i}"));
      if (playerID is not 0)
        permittedPlayerIDs.Add(new(playerID));
    }
  }

  void UpdateMapTablePermittedPlayerIDs(MapTableState state)
  {
    foreach (var playerState in _playerStates.Values)
      playerState.UpToDateMapTables.Remove(state.ZDO);

    state.PermittedPlayerIDs?.Clear();
    if (state.Wards is not { Count: > 0 })
      return;

    foreach (var ward in state.Wards)
    {
      if (ward.Vars.GetEnabled())
        AddPermittedPlayerIDs(ward, state.PermittedPlayerIDs ??= []);
    }
  }

  void UpdateMapTable(MapTableState state, Dictionary<PlayerID, (Peer, PlayerState)> peers)
  {
    /// <see cref="Minimap.GetSharedMapData"/> <see cref="Minimap.AddSharedMapData"/>
    
    const Version.SharedMap MapDataVersion = Version.SharedMap.PinsAuthor;

    var pkg = SingletonCache<ZPackage>.Instance;
    var data = state.ZDO.Vars.GetData();
    if (data is not null)
    {
      data = Utils.Decompress(data);
      pkg.Load(data);
      var version = (Version.SharedMap)pkg.ReadInt();
      if (version is not MapDataVersion)
      {
        Logger.LogWarning(Invariant($"MapTable data version {version:D} [{version}] is not supported"));
        return;
      }
      data = pkg.ReadByteArray();
      if (data.Length != Minimap.instance.m_textureSize * Minimap.instance.m_textureSize)
      {
        Logger.LogWarning("Invalid explored map data length");
        data = null;
      }

      var pinCount = pkg.ReadInt();
      if (_pins.Capacity < pinCount)
        _pins.Capacity = pinCount;

      for (int i = 0; i < pinCount; i++)
      {
        var pin = new Pin(new(pkg.ReadLong()), pkg.ReadString(), pkg.ReadVector3(), (Minimap.PinType)pkg.ReadInt(), pkg.ReadBool(), pkg.ReadString());
        if (pin.OwnerId.IsModPlayerID(out PlayerID playerID))
        {
          if (!peers.ContainsKey(playerID))
            _pins.Add(pin);
        }
        else if (!Config.Instance.DiscardPlayerPins.Value || _oreDepositPins.Contains((pin.Type, pin.Tag)))
          _pins.Add(pin);
      }
    }

    foreach (var (_, playerState) in peers.Values)
    {
      playerState.UpToDateMapTables.Add(state.ZDO);

      if (Config.Instance.PortalsPinType.Value is not Minimap.PinType.None)
      {
        foreach (var zdo in playerState.Portals)
          _pins.Add(new(playerState.PlayerID.AsModPlayerID(), zdo.Vars.GetTag(), zdo.ZDO.GetPosition(), Config.Instance.PortalsPinType.Value, false, AutoMapTablesPlugin.PluginGuid));
      }

      if (Config.Instance.ShipsPinType.Value is not Minimap.PinType.None)
      {
        foreach (var zdo in playerState.Ships)
          _pins.Add(new(playerState.PlayerID.AsModPlayerID(), GetProcessorPrefabInfo(zdo)!.Piece!.m_name, zdo.ZDO.GetPosition(), Config.Instance.ShipsPinType.Value, false, AutoMapTablesPlugin.PluginGuid));
      }

      if (_oreDepositPins.Count is not 0)
      {
        foreach (var zdo in playerState.OreVeins)
        {
          if (GetProcessorPrefabInfo(zdo) is { MineRockPinConfig: { PinType.Value: not Minimap.PinType.None } cfg, MineRock5: { } mineRock })
            _pins.Add(new(playerState.PlayerID.AsModPlayerID(), cfg.Label.Value is Config.DefaultOreDepositName ? mineRock.m_name : cfg.Label.Value, zdo.ZDO.GetPosition(), cfg.PinType.Value, false, AutoMapTablesPlugin.PluginGuid));
        }
      }

      if (Config.Instance.DungeonsPinType.Value is not Minimap.PinType.None)
      {
        List<(ServersideQoLZDO, Vector3, string)>? updatedPos = null;
        foreach (var (zdo, nullableValue) in playerState.Dungeons)
        {
          if (nullableValue is not { } value)
          {
            var hash = zdo.Vars.GetLocation();
            if (hash is 0)
              continue;

            using var loc = ZoneSystem.instance.GetAndLoadLocationByHash(hash);
            if (loc.Prefab is not { } prefab)
              continue;

            var found = false;
            value = default;
            foreach (var component in prefab.GetComponentsInChildren<Teleport>())
            {
              var pos = zdo.ZDO.GetPosition() + zdo.ZDO.GetRotation() * component.gameObject.transform.position;
              if (Character.InInterior(pos))
                continue;
              found = true;
              (updatedPos ??= []).Add((zdo, pos, component.m_enterText));
              value = (pos, component.m_enterText);
              break;
            }

            if (!found)
              continue;
          }

          _pins.Add(new(playerState.PlayerID.AsModPlayerID(), Config.Instance.DungeonsLabel.Value is Config.DefaultOreDepositName ? value.Item2! : Config.Instance.DungeonsLabel.Value, zdo.ZDO.GetPosition(), Config.Instance.DungeonsPinType.Value, false, AutoMapTablesPlugin.PluginGuid));
        }

        if (updatedPos is not null)
        {
          foreach (var (zdo, pos, text) in updatedPos)
            playerState.Dungeons[zdo] = (pos, text);
        }
      }
    }

    pkg.Clear();
    pkg.Write((int)MapDataVersion);
    pkg.Write(data ?? (_emptyExplored ??= new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]));

    pkg.Write(_pins.Count);
    foreach (var pin in _pins)
    {
      pkg.Write(pin.OwnerId.Value);
      pkg.Write(pin.Tag);
      pkg.Write(pin.Pos);
      pkg.Write((int)pin.Type);
      pkg.Write(pin.IsChecked);
      pkg.Write(pin.Author);
    }

    state.ZDO.Vars.SetData(Utils.Compress(pkg.GetArray()));

    ShowMessage(peers.Values.Select(static x => x.Item1), state.ZDO, Config.Instance.Localization.Value.Updated, Config.Instance.UpdatedMessageType.Value);

    state.LastDataRevision = state.ZDO.ZDO.DataRevision;
    _pins.Clear();
  }

  void OnMapTableDestroyed(ServersideQoLZDO zdo)
  {
    if (!_mapTables.Remove(zdo))
      return;

    foreach (var state in _playerStates.Values)
      state.UpToDateMapTables.Remove(zdo);
  }

  void OnPortalDestroyed(ServersideQoLZDO zdo)
  {
    if (!_playerStates.TryGetValue(zdo.Vars.GetCreator(), out var state))
      return;
    if (state.Portals.Remove(zdo))
      state.UpToDateMapTables.Clear();
  }

  void OnShipDestroyed(ServersideQoLZDO zdo)
  {
    if (!_playerStates.TryGetValue(zdo.Vars.GetCreator(), out var state))
      return;
    if (state.Ships.Remove(zdo))
      state.UpToDateMapTables.Clear();
  }

  void OnWardDestroyed(ServersideQoLZDO zdo)
  {
    if (!_wards.Remove(zdo))
      return;

    foreach (var state in _mapTables.Values)
    {
      if (state.Wards?.Remove(zdo) is true)
        UpdateMapTablePermittedPlayerIDs(state);
    }
  }

  void OnOreDepositDestroyed(ServersideQoLZDO zdo)
  {
    if (__playerIDsVar.Get(zdo) is not { } ids)
      return;

    foreach (var id in ids)
    {
      if (!_playerStates.TryGetValue(id, out var state))
        continue;
      if (state.OreVeins.Remove(zdo))
        state.UpToDateMapTables.Clear();
    }
  }
  readonly record struct Pin(PlayerID OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);

  sealed class PlayerState(PlayerID playerID)
  {
    public PlayerID PlayerID { get; } = playerID;
    public HashSet<ServersideQoLZDO> UpToDateMapTables => field ??= [];
    public HashSet<ServersideQoLZDO> Portals => field ??= [];
    public HashSet<ServersideQoLZDO> Ships => field ??= [];
    public HashSet<ServersideQoLZDO> OreVeins => field ??= [];
    public Dictionary<ServersideQoLZDO, (Vector3, string)?> Dungeons => field ??= [];
  }

  sealed class MapTableState(ServersideQoLZDO zdo)
  {
    public ServersideQoLZDO ZDO { get; } = zdo;
    public uint LastDataRevision { get; set; }

    /// <see cref="PrivateArea.CheckAccess"/>
    public HashSet<ServersideQoLZDO>? Wards { get; set; }
    public HashSet<PlayerID>? PermittedPlayerIDs { get; set; }
  }
}
