using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class InGameConfigProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly int _prefabFloor = "Piece_grausten_floor_4x4".GetStableHashCode();
    readonly int _prefabWall = "Piece_grausten_wall_4x2".GetStableHashCode();
    readonly int _prefabPortal = "portal_wood".GetStableHashCode();
    readonly int _prefabSconce = "piece_walltorch".GetStableHashCode();
    readonly int _prefabGuardStone = "dverger_guardstone".GetStableHashCode(); //"guard_stone".GetStableHashCode();
    readonly int _prefabSign = "sign".GetStableHashCode();
    readonly int _prefabCandle = "Candle_resin".GetStableHashCode();
    const string SignFormatPrefix = "<color=white>";
    const string MainPortalTag = $"{Main.PluginName} Config-Room";

    readonly HashSet<ZDOID> _configPieces = new();
    readonly ConcurrentDictionary<ZDOID, (ExtendedZDO Player, bool IsAdmin)> _isAdmin = new();
    readonly Dictionary<ZDOID, ExtendedZDO> _signsByCandle = new();
    readonly Dictionary<ZDOID, ConfigEntryBase> _configBySign = new();
    readonly Vector3 _worldSpawn = ZoneSystem.instance.m_locationInstances.Values.FirstOrDefault(x => x.m_location.m_prefabName is "StartTemple").m_position;

    public override void Initialize()
    {
        base.Initialize();

        if (_configPieces.Count > 0)
            return;

        foreach (var zdo in ZDOMan.instance.GetObjectsByID().Values.Cast<ExtendedZDO>().Where(x => x.Vars.GetCreator() == Main.PluginGuidHash))
            zdo.Destroy();

        if (!Config.General.InWorldConfigRoom.Value)
            return;

        if (_worldSpawn == default)
        {
            Logger.LogWarning($"StartTemple not found, skipping generation of config room");
            return;
        }

        {
            var pos = _worldSpawn;
            pos.z -= 3;
            PlacePiece(pos, _prefabPortal, 0f)
                .Vars.SetTag(MainPortalTag);
            pos.y -= 3;
            PlacePiece(pos, _prefabGuardStone, 0)
                .Fields<PrivateArea>(true).Set(x => x.m_radius, 3).Set(x => x.m_enabledByDefault, true);
        }

        var offset = _worldSpawn;
        while (!Character.InInterior(offset))
            offset.y += 1000;

        var configSections = Config.ConfigFile
            .Where(x => x.Key.Section != Main.DummyConfigSection && !x.Key.Section.StartsWith("A - "))
            .GroupBy(x => x.Key.Section, x => x.Value)
            .OrderBy(x => x.Key)
            .ToList();

        var sectionEnumerator = configSections.GetEnumerator();
        // 4*(width-1) = count -> width = count/4 + 1
        var width = (int)Math.Ceiling(configSections.Count / 4f + 1);
        for (int i = 0; i < width; i++)
        {
            var iIsEdge = i is 0 || i == width - 1;
            var x = (i - width / 2f) * 4;
            for (int k = 0; k < width; k++)
            {
                var z = (k - width / 2f) * 4;

                var pos = offset;
                pos.x += x;
                pos.z += z;

                PlacePiece(pos, _prefabFloor, 0f);
                pos.y += 4.5f;
                PlacePiece(pos, _prefabFloor, 0f);
                pos.y -= 4.5f;

                var kIsEdge = k is 0 || k == width - 1;
                if (!iIsEdge && !kIsEdge)
                    continue;

                var rot = 0f;
                if (iIsEdge && kIsEdge)
                {
                    if (k is 0 && i is 0)
                        rot = 45;
                    else if (k is 0 && i is not 0)
                        rot = 270 + 45;
                    else if (k is not 0 && i is 0)
                        rot = 90 + 45;
                    else
                        rot = 180 + 45;
                }
                else if (iIsEdge)
                {
                    if (i is 0)
                        rot += 90;
                    else
                        rot += 270;
                }
                else if (kIsEdge)
                {
                    if (k is not 0)
                        rot += 180;
                }

                if (!iIsEdge)
                    pos.z += k is 0 ? -1.5f : 1.5f;
                else if (!kIsEdge)
                    pos.x += i is 0 ? -1.5f : 1.5f;
                
                if (sectionEnumerator.MoveNext())
                {
                    var section = Regex.Replace(sectionEnumerator.Current.Key, @"^[A-Z] - ", "");
                    var zdo = PlacePiece(pos, _prefabPortal, rot);
                    zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
                    zdo.Vars.SetTag($"Config: {section}");

                    if (iIsEdge && kIsEdge)
                    {
                        pos.z += (k is 0 ? -0.25f : 0.25f) * Mathf.Sqrt(2);
                        pos.x += (i is 0 ? -0.25f : 0.25f) * Mathf.Sqrt(2);
                    }
                    else if (!iIsEdge)
                        pos.z += k is 0 ? -0.25f : 0.25f;
                    else if (!kIsEdge)
                        pos.x += i is 0 ? -0.25f : 0.25f;
                    pos.y += 2;
                    PlacePiece(pos, _prefabSign, rot)
                        .Vars.SetText($"{SignFormatPrefix}{section}");
                }

                if (iIsEdge)
                {
                    pos = offset;
                    pos.x += x;
                    pos.z += z;
                    pos.y += 0.25f;
                    rot = i is 0 ? 90 : 270;
                    pos.x += i is 0 ? -2f : 2f;
                    PlacePiece(pos, _prefabWall, rot);
                    pos.y += 2;
                    PlacePiece(pos, _prefabWall, rot);

                    rot -= 90;
                    pos.x += i is 0 ? 0.25f : -0.25f;
                    pos.y += 0.5f;
                    PlacePiece(pos, _prefabSconce, rot)
                        .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
                if (kIsEdge)
                {
                    pos = offset;
                    pos.x += x;
                    pos.z += z;
                    pos.y += 0.25f;
                    rot = k is 0 ? 0 : 180;
                    pos.z += k is 0 ? -2f : 2f;
                    PlacePiece(pos, _prefabWall, rot);
                    pos.y += 2;
                    PlacePiece(pos, _prefabWall, rot);

                    rot -= 90;
                    pos.z += k is 0 ? 0.25f : -0.25f;
                    pos.y += 0.5f;
                    PlacePiece(pos, _prefabSconce, rot)
                        .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
            }
        }

        if (sectionEnumerator.MoveNext())
            throw new Exception("Algorithm failed to place all portals");

        {
            var pos = offset;
            pos.x -= 2;
            pos.z -= 2;

            pos.z -= 1.5f;
            PlacePiece(pos, _prefabPortal, 0f)
                .Vars.SetTag(MainPortalTag);
        }

        var yOffset = offset.y;
        foreach (var group in configSections)
        {
            yOffset += 5;
            var entryEnumerator = group.GetEnumerator();
            // count = 4*width
            width = Math.Max(3, (int)Math.Ceiling(group.Count() / 4f));
            for (int i = 0; i < width; i++)
            {
                var iIsEdge = i is 0 || i == width - 1;
                var x = (i - width / 2f) * 4;
                for (int k = 0; k < width; k++)
                {
                    var z = (k - width / 2f) * 4;

                    var pos = offset with { y = yOffset };
                    pos.x += x;
                    pos.z += z;

                    PlacePiece(pos, _prefabFloor, 0f);
                    pos.y += 4.5f;
                    PlacePiece(pos, _prefabFloor, 0f);
                    pos.y -= 4.5f;

                    var kIsEdge = k is 0 || k == width - 1;
                    if (!iIsEdge && !kIsEdge)
                        continue;

                    if (iIsEdge)
                    {
                        pos = offset with { y = yOffset };
                        pos.x += x;
                        pos.z += z;
                        pos.y += 0.25f;
                        float rot = i is 0 ? 90 : 270;
                        pos.x += i is 0 ? -2f : 2f;
                        PlacePiece(pos, _prefabWall, rot);
                        pos.y += 2;
                        PlacePiece(pos, _prefabWall, rot);

                        if (entryEnumerator.MoveNext())
                        {
                            var entry = entryEnumerator.Current;
                            rot -= 90;
                            pos.x += i is 0 ? 0.25f : -0.25f;
                            pos.y += 1.1f;
                            PlacePiece(pos, _prefabSign, rot + 90)
                                .Vars.SetText($"{SignFormatPrefix}{entry.Definition.Key}");
                            pos.y -= 0.6f;
                            PlacePiece(pos, _prefabSign, rot + 90)
                                .Vars.SetText($"{SignFormatPrefix}{entry.Description.Description}");
                            pos.y -= 1;
                            var sign = PlacePiece(pos, _prefabSign, rot + 90);
                            sign.Vars.SetText($"{SignFormatPrefix}{entry.BoxedValue}");
                            _configBySign.Add(sign.m_uid, entry);

                            if (entry.SettingType == typeof(bool))
                            {
                                pos.y -= 0.55f;
                                var candle = PlacePiece(pos, _prefabCandle, rot);
                                candle.Fields<Fireplace>().Set(x => x.m_secPerFuel, 0).Set(x => x.m_canTurnOff, true);
                                candle.Vars.SetState((bool)entry.BoxedValue ? 1 : 2);
                                _signsByCandle.Add(candle.m_uid, sign);
                                pos.y += 0.55f;
                            }

                            pos.z -= 1;
                            PlacePiece(pos, _prefabSconce, rot)
                                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                            pos.z += 2;
                            PlacePiece(pos, _prefabSconce, rot)
                                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                        }
                    }
                    if (kIsEdge)
                    {
                        pos = offset with { y = yOffset };
                        pos.x += x;
                        pos.z += z;
                        pos.y += 0.25f;
                        float rot = k is 0 ? 0 : 180;
                        pos.z += k is 0 ? -2f : 2f;
                        PlacePiece(pos, _prefabWall, rot);
                        pos.y += 2;
                        PlacePiece(pos, _prefabWall, rot);

                        if (entryEnumerator.MoveNext())
                        {
                            var entry = entryEnumerator.Current;

                            rot -= 90;
                            pos.z += k is 0 ? 0.25f : -0.25f;
                            pos.y += 1.1f;
                            PlacePiece(pos, _prefabSign, rot + 90)
                                .Vars.SetText($"{SignFormatPrefix}{entry.Definition.Key}");
                            pos.y -= 0.6f;
                            PlacePiece(pos, _prefabSign, rot + 90)
                                .Vars.SetText($"{SignFormatPrefix}{entry.Description.Description}");
                            pos.y -= 1;
                            var sign = PlacePiece(pos, _prefabSign, rot + 90);
                            sign.Vars.SetText($"{SignFormatPrefix}{entry.BoxedValue}");
                            _configBySign.Add(sign.m_uid, entry);

                            if (entry.SettingType == typeof(bool))
                            {
                                pos.y -= 0.55f;
                                var candle = PlacePiece(pos, _prefabCandle, rot);
                                candle.Fields<Fireplace>().Set(x => x.m_secPerFuel, 0).Set(x => x.m_canTurnOff, true);
                                candle.Vars.SetState((bool)entry.BoxedValue ? 1 : 2);
                                _signsByCandle.Add(candle.m_uid, sign);
                                pos.y += 0.55f;
                            }

                            pos.x -= 1;
                            PlacePiece(pos, _prefabSconce, rot)
                                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                            pos.x += 2;
                            PlacePiece(pos, _prefabSconce, rot)
                                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                        }
                    }
                }
            }

            if (entryEnumerator.MoveNext())
                throw new Exception($"Algorithm failed to place all signs in config section {group.Key}");

            {
                var section = Regex.Replace(group.Key, @"^[A-Z] - ", "");
                var pos = offset with { y = yOffset };
                pos.x -= 2;
                pos.z -= 2;
                var zdo = PlacePiece(pos, _prefabPortal, 0f);
                zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
                zdo.Vars.SetTag($"Config: {section}");

            }
        }

        ZDOMan.instance.ConvertPortals();
    }

    public override bool ClaimExclusive(ExtendedZDO zdo) => _configPieces.Contains(zdo.m_uid);

    public override void PreProcess()
    {
        base.PreProcess();
        foreach (var (id, zdo) in _isAdmin.Select(x => (x.Key, x.Value.Player)))
        {
            if (!zdo.IsValid() || zdo.PrefabInfo.Player is null)
                _isAdmin.TryRemove(id, out _);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (!Config.General.InWorldConfigRoom.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        
        if (zdo.PrefabInfo.Player is not null)
        {
            ZNetPeer? peer = null;
            bool isAdmin;
            if (_isAdmin.TryGetValue(zdo.m_uid, out var entry))
                isAdmin = entry.IsAdmin;
            else
            {
                peer ??= peers.First(x => x.m_characterID == zdo.m_uid);
                isAdmin = Player.m_localPlayer?.GetZDOID() == zdo.m_uid || ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
                _isAdmin.TryAdd(zdo.m_uid, (zdo, isAdmin));
                if (isAdmin)
                    UnregisterZdoProcessor = true;
            }

            if (!isAdmin && Character.InInterior(zdo.GetPosition()) &&
                ZoneSystem.GetZone(zdo.GetPosition()) == ZoneSystem.GetZone(_worldSpawn))
            {
                peer ??= peers.First(x => x.m_characterID == zdo.m_uid);
                var pos = _worldSpawn;
                pos.y += 1;
                RPC.TeleportPlayer(peer, pos, zdo.GetRotation(), false);
                RPC.ShowMessage(peer, MessageHud.MessageType.Center, "$piece_noaccess");
            }
            return false;
        }

        if (zdo.PrefabInfo.Fireplace is not null && _signsByCandle.TryGetValue(zdo.m_uid, out var signZdo))
        {
            var state = zdo.Vars.GetState();
            signZdo.Vars.SetText($"{SignFormatPrefix}{(state is 1 ? bool.TrueString : bool.FalseString)}");
            return true;
        }
        
        if (zdo.PrefabInfo.Sign is not null && _configBySign.TryGetValue(zdo.m_uid, out var config))
        {
            var text = zdo.Vars.GetText();
            if (text.StartsWith(SignFormatPrefix))
                text = text.Substring(SignFormatPrefix.Length);

            try { config.BoxedValue = TomlTypeConverter.ConvertToValue(text, config.SettingType); }
            catch (Exception)
            {
                zdo.Vars.SetText($"{SignFormatPrefix}{config.BoxedValue}");
                RPC.ShowMessage(peers.Where(x => _isAdmin.TryGetValue(x.m_characterID, out var y) && y.IsAdmin),
                    MessageHud.MessageType.Center, "$invalid_keybind_header");
            }
            return true;
        }

        if (zdo.PrefabInfo.Door is not null && _configPieces.Contains(zdo.m_uid))
        {
            if (!CheckMinDistance(peers, zdo, 8))
                return false;

            const int StateClosed = 0;
            if (zdo.Vars.GetState() is not StateClosed)
                zdo.Vars.SetState(StateClosed);
            return true;
        }

        //if (zdo.PrefabInfo.TeleportWorld is not null && _configPieces.Contains(zdo.m_uid))
        //{
        //    // Not sure, why this is needed. Main portal loses connection sometimes
        //    if (zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) == ZDOID.None)
        //        ZDOMan.instance.ConvertPortals();
        //}

        UnregisterZdoProcessor = true;
        return false;
    }

    static void AddAdmin(ExtendedZDO guardStone, long playerId, string playerName)
    {
        /// <see cref="PrivateArea.SetPermittedPlayers"/>
        var i = guardStone.Vars.GetPermitted();
        guardStone.Vars.SetPermitted(i + 1);
        guardStone.Set($"pu_id{i}", playerId);
        guardStone.Set($"pu_name{i}", playerName);
    }

    //sealed record ConfigPiece(string PrefabName, Vector3 Pos, Vector3 Rot)
    //{
    //    public int Prefab { get; } = PrefabName.GetStableHashCode();
    //    public Quaternion Rotation { get; } = Quaternion.Euler(Rot);
    //}

    ExtendedZDO PlacePiece(Vector3 pos, int prefab, float rot)
    {
        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        zdo.SetPrefab(prefab);
        zdo.Persistent = true;
        zdo.Distant = false;
        zdo.Type = ZDO.ObjectType.Default;
        zdo.SetRotation(Quaternion.Euler(0, rot, 0));
        zdo.Vars.SetCreator(Main.PluginGuidHash);
        zdo.Vars.SetHealth(-1);
        _configPieces.Add(zdo.m_uid);
        zdo.Fields<Piece>().Set(x => x.m_canBeRemoved, false);
        zdo.Fields<WearNTear>().Set(x => x.m_noRoofWear, false).Set(x => x.m_noSupportWear, false).Set(x => x.m_health, -1);
        return zdo;
    }

    //readonly IReadOnlyList<ConfigPiece>
}
