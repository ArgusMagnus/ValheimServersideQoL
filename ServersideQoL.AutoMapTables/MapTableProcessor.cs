using BepInEx.Configuration;
using System.Text.RegularExpressions;
using UnityEngine;
using static UnityEngine.Random;

namespace ServersideQoL.AutoMapTables;

[DependsOn<PlayerRegistryProcessor>]
public sealed class Processor : Processor<Processor.PrefabInfo>
{
  public sealed record PrefabInfo(MapTable? MapTable, PrivateArea? PrivateArea, TeleportWorld? TeleportWorld, Ship? Ship, MineRock5? MineRock5) : ProcessorPrefabInfo
  {
    public ConfigEntry<Minimap.PinType>? MineRockPinConfig => field ??= MineRock5 is not null && Config.Instance.AutoUpdateOreDeposits.TryGetValue(PrefabInfo.PrefabHash, out var value) ? value : null;
    public override bool IsValid => this switch
    {
      { PrivateArea: not null } or { TeleportWorld: not null } or { Ship: not null } => PrefabInfo.HasComponent<Piece>() && PrefabInfo.HasComponent<PieceTable>(),
      { MineRock5: not null } => MineRockPinConfig is not null,
      _ => true
    };
  }

  readonly Dictionary<ServersideQoLZDO, MapTableState> _mapTables = [];
  readonly Dictionary<long, PlayerState> _playerStates = [];
  readonly HashSet<ServersideQoLZDO> _wards = [];
  float _mapTableRangeSqr;
  float _oreDepositRangeSqr;

  static readonly ServerVar<HashSet<long>> __playerIDsVar = AutoMapTablesPlugin.RegisterServerVar<HashSet<long>>("PlayerIDs");

  protected override void Initialize()
  {
    _mapTableRangeSqr = Config.Instance.MapTableRange.Value * Config.Instance.MapTableRange.Value;
    _oreDepositRangeSqr = Config.Instance.OreDepositsDiscoverRange.Value * Config.Instance.OreDepositsDiscoverRange.Value;

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
          GetPlayerState(zdo.Vars.GetCreator()).Portals.Add(zdo);
          zdo.Destroyed += OnPortalDestroyed;
          break;

        case { Ship: not null }:
          GetPlayerState(zdo.Vars.GetCreator()).Ships.Add(zdo);
          zdo.Destroyed += OnShipDestroyed;
          break;

        case { MineRockPinConfig: not null }:
          zdo.Destroyed += OnOreDepositDestroyed;
          if (__playerIDsVar.Get(zdo) is { } playerIDs)
          {
            foreach (var id in playerIDs)
              GetPlayerState(id).OreVeins.Add(zdo);
          }
          break;
      }
    }

    _playerStates.Remove(0);

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
        if (peer.PlayerState?.PlayerID is not { } playerID || !_playerStates.TryGetValue(playerID, out var playerState) || playerState.UpToDateMapTables.Contains(zdo))
          continue;
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), peer.RefPos) > _mapTableRangeSqr)
          continue;
        UpdateMapTable(state, playerState);
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
      if (playerID is 0)
        return ProcessResult.UnregisterProcessor;
      if (peers.Any(x => x.PlayerState?.PlayerID == playerID))
      {
        var state = GetPlayerState(playerID);
        state.Portals.Add(zdo);
        state.UpToDateMapTables.Clear();
      }
      return default;
    }

    if (prefabInfo.Ship is not null)
    {
      var playerID = zdo.Vars.GetCreator();
      if (playerID is 0)
        return ProcessResult.UnregisterProcessor;
      if (peers.Any(x => x.PlayerState?.PlayerID == playerID))
      {
        var state = GetPlayerState(playerID);
        state.Ships.Add(zdo);
        state.UpToDateMapTables.Clear();
      }
      return default;
    }

    if (prefabInfo.MineRockPinConfig is not null)
    {
      HashSet<long>? ids = null;
      foreach (var peer in peers.Enumerate())
      {
        if (peer.PlayerState?.PlayerID is not { } playerID || !_playerStates.TryGetValue(playerID, out var playerState))
          continue;
        if (Utils.DistanceSqr(zdo.ZDO.GetPosition(), peer.RefPos) > _oreDepositRangeSqr)
          continue;
        if (!playerState.OreVeins.Add(zdo))
          continue;
        zdo.Destroyed += OnOreDepositDestroyed;
        ids ??= __playerIDsVar.Get(zdo) ?? [];
        ids.Add(playerID);
        playerState.UpToDateMapTables.Clear();
      }
      if (ids is not null)
        __playerIDsVar.Set(zdo, ids);
      return default;
    }

    Logger.DevLog($"Unexpected prefab: {prefabInfo.PrefabInfo.PrefabName}");
    return ProcessResult.UnregisterProcessor;

  }

  PlayerState GetPlayerState(long playerID)
  {
    if (!_playerStates.TryGetValue(playerID, out var state))
      _playerStates.Add(playerID, state = new());
    return state;
  }

  static void AddPermittedPlayerIDs(ServersideQoLZDO ward, HashSet<long> permittedPlayerIDs)
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
        permittedPlayerIDs.Add(playerID);
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

  void UpdateMapTable(MapTableState state, PlayerState playerState)
  {
    playerState.UpToDateMapTables.Add(state.ZDO);
    //asdf;
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

  sealed class PlayerState
  {
    public HashSet<ServersideQoLZDO> UpToDateMapTables => field ??= [];
    public HashSet<ServersideQoLZDO> Portals => field ??= [];
    public HashSet<ServersideQoLZDO> Ships => field ??= [];
    public HashSet<ServersideQoLZDO> OreVeins => field ??= [];
    public HashSet<ServersideQoLZDO> Dungeons => field ??= [];
  }

  sealed class MapTableState(ServersideQoLZDO zdo)
  {
    public ServersideQoLZDO ZDO { get; } = zdo;
    /// <see cref="PrivateArea.CheckAccess"/>
    public HashSet<ServersideQoLZDO>? Wards { get; set; }
    public HashSet<long>? PermittedPlayerIDs { get; set; }
  }
}

[Processor("05450dd6-13bd-42cc-9bd3-b1eed5e501af")]
public sealed class MapTableProcessor : Processor<ProcessorPrefabInfo<MapTable>>
{
  record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
  readonly List<Pin> _pins = [];
  readonly List<Pin> _existingPins = [];
  byte[]? _emptyExplored;
  int _pinsHash;
  int _oldPinsHash;
  Regex? _includePortalRegex;
  Regex? _excludePortalRegex;
  DateTimeOffset _pinsValidUntil;

  protected override void Initialize()
  {
    if (Config.Instance.Enabled.Value)
    {
      var filter = Config.Instance.PortalsInclude.Value.Trim();
      _includePortalRegex = string.IsNullOrEmpty(filter.Trim(['*'])) ? null : new(ConvertToRegexPattern(filter));
      filter = Config.Instance.PortalsExclude.Value.Trim();
      _excludePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));
    }
    else
    {
      _includePortalRegex = null;
      _excludePortalRegex = null;
    }

    _pins.Clear();
    _existingPins.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<MapTable> prefabInfo)
  {
    var now = DateTimeOffset.UtcNow;
    if (_pinsValidUntil < now)
    {
      _pins.Clear();
      _pinsHash = 0;
      _pinsValidUntil = now.AddSeconds(Config.Instance.Advanced.Value.MapTableUpdateInterval);
    }

    zdo.DelaySchedulingFor(Config.Instance.Advanced.Value.MapTableUpdateInterval);

    if (_pins is { Count: 0 })
    {
      if (Config.Instance.PortalsPinType.Value is not Minimap.PinType.None)
      {
        foreach (var portal in ZDOMan.instance.GetPortals().Values.SelectMany(static x => x.Select(static x => x.ServersideQoLZDO)))
        {
          if (portal.IsModCreator())
            continue;
          var tag = portal.Vars.GetTag();
          if (_includePortalRegex?.IsMatch(tag) is false || _excludePortalRegex?.IsMatch(tag) is true)
            continue;
          var pin = new Pin(AutoMapTablesPlugin.PluginGuidHash, tag, portal.ZDO.GetPosition(), Config.Instance.PortalsPinType.Value, false, AutoMapTablesPlugin.PluginGuid);
          _pins.Add(pin);
          _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
        }
      }
      if (Config.Instance.ShipsPinType.Value is not Minimap.PinType.None)
      {
        foreach (var ship in Instance<ShipProcessor>().Ships)
        {
          var pos = ship.ZDO.GetPosition();
          // round pos to multiples of 5 to reduce pin churn due to minor position changes
          static float RoundToMultipleOf5(float value) => Mathf.Round(value / 5f) * 5f;
          pos = new(RoundToMultipleOf5(pos.x), RoundToMultipleOf5(pos.y), RoundToMultipleOf5(pos.z));

          var shipPrefabInfo = ship.GetProcessorPrefabInfo<ShipProcessor.PrefabInfo>()!;
          var pin = new Pin(AutoMapTablesPlugin.PluginGuidHash, shipPrefabInfo.Piece.m_name ?? "", pos, Config.Instance.ShipsPinType.Value, false, AutoMapTablesPlugin.PluginGuid);
          _pins.Add(pin);
          _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
        }
      }

      (_pinsHash, _oldPinsHash) = (_oldPinsHash, _pinsHash);
    }

    if (_pinsHash == _oldPinsHash)
      return ProcessResult.ScheduleReprocessing;

    const Version.SharedMap MapDataVersion = Version.SharedMap.PinsAuthor;

    _existingPins.Clear();
    ZPackage pkg;
    var data = zdo.Vars.GetData();
    if (data is not null)
    {
      data = Utils.Decompress(data);
      pkg = new ZPackage(data);
      var version = (Version.SharedMap)pkg.ReadInt();
      if (version is not MapDataVersion)
      {
        Logger.LogWarning(Invariant($"MapTable data version {version:D} [{version}] is not supported"));
        return default;
      }
      data = pkg.ReadByteArray();
      if (data.Length != Minimap.instance.m_textureSize * Minimap.instance.m_textureSize)
      {
        Logger.LogWarning("Invalid explored map data length");
        data = null;
      }

      var pinCount = pkg.ReadInt();
      if (_existingPins.Capacity < pinCount)
        _existingPins.Capacity = pinCount;

      for (int i = 0; i < pinCount; i++)
      {
        var pin = new Pin(pkg.ReadLong(), pkg.ReadString(), pkg.ReadVector3(), (Minimap.PinType)pkg.ReadInt(), pkg.ReadBool(), pkg.ReadString());
        if (pin.OwnerId != AutoMapTablesPlugin.PluginGuidHash)
          _existingPins.Add(pin);
      }
    }

    /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
    pkg = new ZPackage();
    pkg.Write((int)MapDataVersion);

    pkg.Write(data ?? (_emptyExplored ??= new byte[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize]));

    pkg.Write(_pins.Count + _existingPins.Count);
    foreach (var pin in _pins.Concat(_existingPins))
    {
      pkg.Write(pin.OwnerId);
      pkg.Write(pin.Tag);
      pkg.Write(pin.Pos);
      pkg.Write((int)pin.Type);
      pkg.Write(pin.IsChecked);
      pkg.Write(pin.Author);
    }

    zdo.Vars.SetData(Utils.Compress(pkg.GetArray()));

    ShowMessage(peers, zdo, Config.Instance.Localization.Value.Updated, Config.Instance.UpdatedMessageType.Value);

    return ProcessResult.ScheduleReprocessing;
  }
}
