using BepInEx.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class InGameConfigProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly HashSet<ZDOID> _configPieces = new();

    readonly Vector3 _initialOffset = GetInitialOffset();
    readonly int _prefabFloor = "Piece_grausten_floor_4x4".GetStableHashCode();
    readonly int _prefabWall = "Piece_grausten_wall_4x2".GetStableHashCode();
    readonly int _prefabPortal = "portal_wood".GetStableHashCode();
    readonly int _prefabSconce = "piece_walltorch".GetStableHashCode();
    readonly int _prefabMetalWall = "piece_dvergr_metal_wall_2x2".GetStableHashCode();
    readonly int _prefabIronGate = "iron_grate".GetStableHashCode();
    readonly int _prefabGuardStone = "dverger_guardstone".GetStableHashCode();

    ExtendedZDO _guardStone = default!;
    readonly ConcurrentDictionary<ExtendedZDO, bool> _isAdmin = new();

    static Vector3 GetInitialOffset()
    {
        var pos = new Vector3();
        while (!Character.InInterior(pos))
            pos.y += 1000;
        return pos;
    }

    public override void Initialize()
    {
        base.Initialize();

        foreach (var zdo in PrivateAccessor.GetZDOManObjectsByID(ZDOMan.instance).Values.Cast<ExtendedZDO>().Where(x => x.Vars.GetCreator() == Main.PluginGuidHash))
            zdo.Destroy();

        var configSections = Config.ConfigFile.Keys
            .Select(x => Regex.Match(x.Section, "^[B-Z] - (?<N>.+)$").Groups["N"].Value)
            .Where(x => !string.IsNullOrEmpty(x))
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var sectionEnumerator = configSections.GetEnumerator();
        var width = (int)Math.Ceiling(Math.Sqrt(configSections.Count));
        for (int i = 0; i < width; i++)
        {
            var iIsEdge = i is 0 || i == width - 1;
            var x = (i - width / 2f) * 4;
            for (int k = 0; k < width; k++)
            {
                var z = (k - width / 2f) * 4;

                var pos = _initialOffset;
                pos.x += x;
                pos.z += z;

                PlacePiece(pos, _prefabFloor, 0f);
                pos.y += 4.5f;
                PlacePiece(pos, _prefabFloor, 0f);
                pos.y -= 4.5f;

                var kIsEdge = k is 0 || k == width - 1;
                if (!iIsEdge && !kIsEdge)
                    continue;

                if (!sectionEnumerator.MoveNext())
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
                    //    rot -= 45;
                    //rot += k is 0 ? 0 : 90;
                    //rot += i is 0 ? 90 : 180;
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

                var zdo = PlacePiece(pos, _prefabPortal, rot);
                zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
                zdo.Vars.SetTag($"Config: {sectionEnumerator.Current}");

                if (iIsEdge)
                {
                    pos = _initialOffset;
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
                    zdo = PlacePiece(pos, _prefabSconce, rot);
                    zdo.Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
                if (kIsEdge)
                {
                    pos = _initialOffset;
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
                    zdo = PlacePiece(pos, _prefabSconce, rot);
                    zdo.Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
            }
        }

        {
            var pos = _initialOffset;
            pos.x -= 2;
            pos.z -= 2;

            pos.y -= 2;
            _guardStone = PlacePiece(pos, _prefabGuardStone, 0f);

            pos.y += 2;
            pos.z -= 1.5f;
            var zdo = PlacePiece(pos, _prefabPortal, 0f);
            zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
            zdo.Vars.SetTag("Qol config");


            pos = _initialOffset;
            pos.y += 0.75f;
            pos.x -= 3;
            PlacePiece(pos, _prefabIronGate, 0);
            pos.y += 3;
            PlacePiece(pos, _prefabMetalWall, 0);
            pos.y -= 0.5f;
            pos.x += 2;
            PlacePiece(pos, _prefabMetalWall, 0);
            pos.y -= 2;
            PlacePiece(pos, _prefabMetalWall, 0);
            pos.x += 1;
            pos.z -= 1;
            PlacePiece(pos, _prefabMetalWall, 90);
            pos.y += 2;
            PlacePiece(pos, _prefabMetalWall, 90);
            pos.z -= 2;
            PlacePiece(pos, _prefabMetalWall, 90);
            pos.y -= 2;
            PlacePiece(pos, _prefabMetalWall, 90);
            pos.z -= 1;
            pos.x -= 1;
            PlacePiece(pos, _prefabMetalWall, 180);
            pos.y += 2;
            PlacePiece(pos, _prefabMetalWall, 180);
            pos.x -= 2;
            PlacePiece(pos, _prefabMetalWall, 180);
            pos.y -= 2;
            PlacePiece(pos, _prefabMetalWall, 180);
            pos.x -= 1;
            pos.z += 1;
            PlacePiece(pos, _prefabMetalWall, 270);
            pos.y += 2;
            PlacePiece(pos, _prefabMetalWall, 270);
            pos.z += 2;
            PlacePiece(pos, _prefabMetalWall, 270);
            pos.y -= 2;
            PlacePiece(pos, _prefabMetalWall, 270);
        }

        PrivateAccessor.ConvertPortals(ZDOMan.instance);


        //List<(string Name, int Prefab, Vector3 Pos, Quaternion Rot, byte[] data)> prefabs = new();
        //using (var reader = new StreamReader(typeof(Main).Assembly.GetManifestResourceStream(typeof(Main), "InWorldConfigPieces.csv")))
        //{
        //    while (reader.ReadLine() is { Length: > 0 } line)
        //    {
        //        var parts = line.Split(';');
        //        var prefab = int.Parse(parts[1]);
        //        var pos = Utils.ParseVector3(parts[2]);
        //        var rot = Quaternion.Euler(Utils.ParseVector3(parts[3]));
        //        var data = Convert.FromBase64String(parts[4]);
        //        prefabs.Add((parts[0], prefab, pos, rot, data));

        //        //pos.x += 40;
        //        //pos.y += 85;
        //        //pos.z -= 220;

        //        //var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        //        //zdo.Deserialize(new(data));
        //        //zdo.SetRotation(rot);
        //        //zdo.Vars.SetCreator(PluginGuidHash);
        //        //zdo.Fields<Piece>().Set(x => x.m_canBeRemoved, false);
        //        //zdo.Fields<WearNTear>().Set(x => x.m_noRoofWear, false).Set(x => x.m_noSupportWear, false);
        //        //if (zdo.PrefabInfo.Fireplace is not null)
        //        //    zdo.Fields<Fireplace>().Set(x => x.m_infiniteFuel, true);
        //        //_ignore.Add(zdo.m_uid);

        //        //pos.z += 4;
        //        //zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        //        //zdo.Deserialize(new(data));
        //        //zdo.SetRotation(rot);
        //        //zdo.Vars.SetCreator(PluginGuidHash);
        //        //zdo.Fields<Piece>().Set(x => x.m_canBeRemoved, false);
        //        //zdo.Fields<WearNTear>().Set(x => x.m_noRoofWear, false).Set(x => x.m_noSupportWear, false);
        //        //if (zdo.PrefabInfo.Fireplace is not null)
        //        //    zdo.Fields<Fireplace>().Set(x => x.m_infiniteFuel, true);
        //        //_ignore.Add(zdo.m_uid);
        //    }
        //}

        //List<ExtendedZDO> signs = new();
        //var z = -220;
        //foreach (var entry in base.Config.Select(x => x.Value).OrderBy(x => x.Definition.Section).ThenBy(x => x.Definition.Key))
        //{
        //    signs.Clear();
        //    foreach (var (prefabName, prefab, position, rot, data) in prefabs)
        //    {
        //        if (entry.SettingType != typeof(bool) && prefabName is "itemstandh" or "Candle_resin")
        //            continue;

        //        var pos = position;
        //        pos.x += 40;
        //        pos.y += 85;
        //        pos.z += z;

        //        var zdo = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(pos, prefab);
        //        zdo.Deserialize(new(data));
        //        zdo.SetRotation(rot);
        //        zdo.Vars.SetCreator(PluginGuidHash);
        //        zdo.Fields<Piece>().Set(x => x.m_canBeRemoved, false);
        //        zdo.Fields<WearNTear>().Set(x => x.m_noRoofWear, false).Set(x => x.m_noSupportWear, false);
        //        if (zdo.PrefabInfo.Fireplace is not null)
        //            zdo.Fields<Fireplace>().Set(x => x.m_infiniteFuel, true);
        //        if (zdo.PrefabInfo.Sign is not null)
        //            signs.Add(zdo);
        //        _ignore.Add(zdo.m_uid);
        //    }

        //    signs.Sort((a, b) => a.GetPosition().y < b.GetPosition().y ? 1 : -1);
        //    signs[0].Vars.SetText($"<color=white>{entry.Definition.Section}");
        //    signs[1].Vars.SetText($"<color=white>{entry.Definition.Key}");
        //    signs[2].Vars.SetText($"<color=white>{entry.Description.Description}");
        //    signs[3].Vars.SetText($"<color=white>{entry.BoxedValue}");

        //    z -= 4;
        //}
    }

    public override bool ClaimExclusive(ExtendedZDO zdo) => _configPieces.Contains(zdo.m_uid);

    public override void PreProcess()
    {
        base.PreProcess();
        foreach (var zdo in _isAdmin.Keys)
        {
            if (!zdo.IsValid() || zdo.PrefabInfo.Player is null)
                _isAdmin.TryRemove(zdo, out _);
        }
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Player is null)
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        ZNetPeer? peer = null;
        if (!_isAdmin.TryGetValue(zdo, out var isAdmin))
        {
            peer ??= peers.First(x => x.m_characterID == zdo.m_uid);
            isAdmin = Player.m_localPlayer?.GetZDOID() == zdo.m_uid || ZNet.instance.IsAdmin(peer.m_socket.GetHostName());
            _isAdmin.TryAdd(zdo, isAdmin);
            if (isAdmin)
                AddAdmin(_guardStone, zdo.Vars.GetPlayerID(), zdo.Vars.GetPlayerName());
        }

        if (isAdmin && zdo.GetPosition().y >= _initialOffset.y &&
            Utils.DistanceXZ(zdo.GetPosition(), _guardStone.GetPosition()) > 4 &&
            ZoneSystem.GetZone(zdo.GetPosition()) == ZoneSystem.GetZone(_guardStone.GetPosition()))
        {
            peer ??= peers.First(x => x.m_characterID == zdo.m_uid);
            var pos = _guardStone.GetPosition();
            pos.y = zdo.GetPosition().y;
            RPC.TeleportPlayer(peer, pos, zdo.GetRotation(), false);
            RPC.ShowMessage(peer, MessageHud.MessageType.Center, "$piece_noaccess");
        }

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
