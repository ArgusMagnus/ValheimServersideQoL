using BepInEx.Configuration;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.AutoMapTables;

//[DependsOn<PlayerRegistryProcessor>]
//public sealed class Processor : Processor<Processor.PrefabInfo>
//{
//  public sealed record PrefabInfo(MapTable? MapTable, TeleportWorld? TeleportWorld, Ship? Ship, MineRock5? MineRock5) : ProcessorPrefabInfo
//  {
//    public ConfigEntry<Minimap.PinType>? MineRockPinConfig => field ??= MineRock5 is not null && Config.Instance.AutoUpdateOreDeposits.TryGetValue(PrefabInfo.PrefabHash, out var value) ? value : null;
//    public override bool IsValid => MineRock5 is null || MineRockPinConfig is not null;
//  }

//  readonly Dictionary<ServersideQoLZDO, State> _mapTables = [];
//  readonly HashSet<ServersideQoLZDO> _portals = [];
//  readonly HashSet<ServersideQoLZDO> _ships = [];

//  protected override void Initialize()
//  {
//    //_mapTables.Clear();
//    //_portals.Clear();
//    //_ships.Clear();
//    //_mineRocks.Clear();

//    //foreach (var zdo in ZDOMan.instance.GetObjects().Select(static x => x.ServersideQoLZDO))
//    //{
//    //  var set = GetProcessorPrefabInfo(zdo) switch
//    //  {
//    //    { MapTable: not null } => _mapTables,
//    //    { TeleportWorld: not null } => _portals,
//    //    { Ship: not null } => _ships,
//    //    { MineRock5: not null } => _mineRocks,
//    //    _ => throw new Exception()
//    //  };

//    //  if (set.Add(zdo))
//    //    zdo.Destroyed += x => set.Remove(x);
//    //}
//  }

//  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
//  {
//    if (prefabInfo.MapTable is not null)
//    {
//      if (!_mapTables.TryGetValue(zdo, out var state))
//      {
//        _mapTables.Add(zdo, state = new(zdo));
//        zdo.Destroyed += x => _mapTables.Remove(x);
//      }

//      var updatePortalsAndShips = false;
//      foreach (var peer in peers.Enumerate())
//      {
//        if (peer.PlayerState is not { } playerState)
//          continue;
//        if (state.Creator == playerState.PlayerID || state.PlayerIDs.Contains(playerState.PlayerID))
//          continue;
//        if (Vector3.Distance(zdo.ZDO.GetPosition(), peer.RefPos) > Config.Instance.RegisterPlayerDistance.Value)
//          continue;

//        state.PlayerIDs.Add(playerState.PlayerID);
//        SetPlayerIDs(zdo, state.PlayerIDs);
//        updatePortalsAndShips = true;
//      }

//      if (updatePortalsAndShips)
//        asdf;

//      zdo.DelaySchedulingFor(0.5f);
//      return ProcessResult.ScheduleReprocessing;
//    }

//    if (prefabInfo.TeleportWorld is not null)
//    {
//      asdf;
//      return ProcessResult.UnregisterProcessor;
//    }

//    if (prefabInfo.Ship is not null)
//    {
//      asdf;
//      return default;
//    }

//    if (prefabInfo.MineRockPinConfig is not null)
//    {
//      asdf;
//      return default;
//    }

//    Logger.DevLog($"Unexpected prefab: {prefabInfo.PrefabInfo.PrefabName}");
//    return ProcessResult.UnregisterProcessor;
//  }

//  static readonly int __playerIDsHash = AutoMapTablesPlugin.RegisterServerVar("PlayerIDs");

//  static void SetPlayerIDs(ServersideQoLZDO zdo, HashSet<long> playerIDs)
//  {
//    var value = new byte[playerIDs.Count * sizeof(long)];
//    var span = MemoryMarshal.Cast<byte, long>(value.AsSpan());
//    int i = -1;
//    foreach (var id in playerIDs)
//      span[++i] = id;
//    zdo.ZDO.Set(__playerIDsHash, value);
//  }

//  static HashSet<long> GetPlayerIDs(ServersideQoLZDO zdo)
//  {
//    HashSet<long> result = [];
//    var value = zdo.ZDO.GetByteArray(__playerIDsHash);
//    if (value is { Length: > 0 })
//    {
//      var span = MemoryMarshal.Cast<byte, long>(value.AsSpan());
//      foreach (var id in span)
//        result.Add(id);
//    }
//    return result;
//  }

//  sealed class State(ServersideQoLZDO zdo)
//  {
//    public ServersideQoLZDO ZDO { get; } = zdo;
//    public long Creator { get; } = zdo.Vars.GetCreator();
//    public HashSet<long> PlayerIDs { get; } = GetPlayerIDs(zdo);
//  }
//}

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
