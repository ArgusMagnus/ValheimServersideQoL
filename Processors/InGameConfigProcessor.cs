using BepInEx.Configuration;
using BepInEx.Logging;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Color = System.Drawing.Color;
using IEnumerable = System.Collections.IEnumerable;

namespace Valheim.ServersideQoL.Processors;

sealed class InGameConfigProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    const string SignFormatWhite = "<color=white>";
    const string SignFormatGreen = "<color=#00FF00>";
    const string MainPortalTag = $"{Main.PluginName} Config-Room";
    internal const string PortalHubTag = $"{Main.PluginName} Portal Hub";
    const float FloorOffset = 5;

    readonly Dictionary<ZDOID, (ExtendedZDO Player, bool IsAdmin)> _isAdmin = new();

    sealed record ConfigState(ConfigEntryBase Entry, object? Value, ExtendedZDO Sign)
    {
        public bool CandleState { get; set; }
    }

    readonly List<(ExtendedZDO Sign, string Text, IReadOnlyList<ConfigEntryBase> Entries)> _portalSigns = new();
    readonly Dictionary<ZDOID, ConfigState> _candleToggles = new();
    readonly Dictionary<ZDOID, ConfigEntryBase> _configBySign = new();

    /// <see cref="Game.FindSpawnPoint">
    readonly (Vector3 WorldSpawn, Vector3 Room) _offset = new Func<(Vector3, Vector3)>(static () =>
    {
        var worldSpawn = ZoneSystem.instance.GetLocationIcon(Game.instance.m_StartLocation, out var pos) ? pos : default;
        var room = worldSpawn;
        while (!Character.InInterior(room))
            room.y += 1000;
        return (worldSpawn, room);
    }).Invoke();

    static string GetSignText(object? value, Type type, Color c)
        => Invariant($"<color=#{c.R:X2}{c.G:X2}{c.B:X2}>{TomlTypeConverter.ConvertToString(value, type)}");
    static string GetSignText(ConfigEntryBase entry, Color? color = null)
        => GetSignText(entry.BoxedValue, entry.SettingType, color ?? (Equals(entry.BoxedValue, entry.DefaultValue) ? Color.White : Color.Lime));

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (!firstTime)
            return;

        if (!Config.General.InWorldConfigRoom.Value)
            return;

        if (_offset.WorldSpawn == default)
        {
            Logger.LogWarning($"{Game.instance.m_StartLocation} not found, skipping generation of config room");
            return;
        }

        {
            var pos = _offset.WorldSpawn;
            pos.z -= 3;
            PlacePiece(pos, Prefabs.PortalWood, 0f)
                .Vars.SetTag(MainPortalTag);
            pos.y -= 3;
            PlacePiece(pos, Prefabs.DvergerGuardstone, 0)
                .Fields<PrivateArea>(true).Set(x => x.m_radius, 3).Set(x => x.m_enabledByDefault, true);
        }

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

                var pos = _offset.Room;
                pos.x += x;
                pos.z += z;

                PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
                pos.y += 4.5f;
                PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
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
                    IReadOnlyList<ConfigEntryBase> entries = [.. sectionEnumerator.Current];
                    var hasNonDefault = entries.Any(x => !Equals(x.BoxedValue, x.DefaultValue));
                    var zdo = PlacePiece(pos, Prefabs.PortalWood, rot);
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
                    var sign = PlacePiece(pos, Prefabs.Sign, rot);
                    sign.Vars.SetText($"{(hasNonDefault ? SignFormatGreen : SignFormatWhite)}{section}");
                    _portalSigns.Add((sign, section, entries));
                }

                if (iIsEdge)
                {
                    pos = _offset.Room;
                    pos.x += x;
                    pos.z += z;
                    pos.y += 0.25f;
                    rot = i is 0 ? 90 : 270;
                    pos.x += i is 0 ? -2f : 2f;
                    PlacePiece(pos, Prefabs.GraustenWall4x2, rot);
                    pos.y += 2;
                    PlacePiece(pos, Prefabs.GraustenWall4x2, rot);

                    rot -= 90;
                    pos.x += i is 0 ? 0.25f : -0.25f;
                    pos.y += 0.5f;
                    PlacePiece(pos, Prefabs.Sconce, rot)
                        .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
                if (kIsEdge)
                {
                    pos = _offset.Room;
                    pos.x += x;
                    pos.z += z;
                    pos.y += 0.25f;
                    rot = k is 0 ? 0 : 180;
                    pos.z += k is 0 ? -2f : 2f;
                    PlacePiece(pos, Prefabs.GraustenWall4x2, rot);
                    pos.y += 2;
                    PlacePiece(pos, Prefabs.GraustenWall4x2, rot);

                    rot -= 90;
                    pos.z += k is 0 ? 0.25f : -0.25f;
                    pos.y += 0.5f;
                    PlacePiece(pos, Prefabs.Sconce, rot)
                        .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
                }
            }
        }

        if (sectionEnumerator.MoveNext())
            throw new Exception("Algorithm failed to place all portals");

        {
            var pos = _offset.Room;
            pos.x -= 2;
            pos.z -= 2;

            if (Config.PortalHub.Enable.Value)
            {
                pos.z -= 0.5f;
                PlacePiece(pos, Prefabs.PortalWood, 180f)
                    .Vars.SetTag(PortalHubTag);
                pos.z += 1;
            }
            PlacePiece(pos, Prefabs.PortalWood, 0f)
                .Vars.SetTag(MainPortalTag);
        }

        var yOffset = _offset.Room.y;
        foreach (var group in configSections)
        {
            yOffset += FloorOffset;
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

                    var pos = _offset.Room with { y = yOffset };
                    pos.x += x;
                    pos.z += z;

                    PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
                    pos.y += 4.5f;
                    PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
                    pos.y -= 4.5f;

                    var kIsEdge = k is 0 || k == width - 1;
                    if (!iIsEdge && !kIsEdge)
                        continue;

                    if (iIsEdge)
                        PlaceConfigWall(new(_offset.Room.x + x, yOffset + 0.25f, _offset.Room.z + z), false, i is 0, 90, entryEnumerator);
                    if (kIsEdge)
                        PlaceConfigWall(new(_offset.Room.x + x, yOffset + 0.25f, _offset.Room.z + z), true, k is 0, 0, entryEnumerator);
                }
            }

            if (entryEnumerator.MoveNext())
                throw new Exception($"Algorithm failed to place all signs in config section {group.Key}");

            {
                var section = Regex.Replace(group.Key, @"^[A-Z] - ", "");
                var pos = _offset.Room with { y = yOffset };
                pos.x -= 2;
                pos.z -= 2;
                var zdo = PlacePiece(pos, Prefabs.PortalWood, 0f);
                zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
                zdo.Vars.SetTag($"Config: {section}");

            }
        }

        ZDOMan.instance.ConvertPortals();

        Config.ConfigFile.SettingChanged -= OnSettingsChanged;
        Config.ConfigFile.SettingChanged += OnSettingsChanged;
        RegisterZdoDestroyed();
    }

    void OnSettingsChanged(object sender, EventArgs args)
    {
        foreach (var (sign, text, entries) in _portalSigns)
        {
            var hasNonDefault = entries.Any(x => !Equals(x.BoxedValue, x.DefaultValue));
            sign.Vars.SetText($"{(hasNonDefault ? SignFormatGreen : SignFormatWhite)}{text}");
        }
    }

    void PlaceConfigWall(Vector3 pos, bool alongX, bool isStart, float rot, IEnumerator<ConfigEntryBase> entryEnumerator)
    {
        ref var x = ref (alongX ? ref pos.x : ref pos.z);
        ref var z = ref (alongX ? ref pos.z : ref pos.x);
        rot += isStart ? 0 : 180;
        z += isStart ? -2f : 2f;
        PlacePiece(pos, Prefabs.GraustenWall4x2, rot);
        pos.y += 2;
        PlacePiece(pos, Prefabs.GraustenWall4x2, rot);

        if (entryEnumerator.MoveNext())
        {
            var entry = entryEnumerator.Current;

            rot -= 90;
            z += isStart ? 0.25f : -0.25f;
            pos.y += 1.1f + 0.25f;
            PlacePiece(pos, Prefabs.Sign, rot + 90)
                .Vars.SetText($"{SignFormatWhite}{entry.Definition.Key}");
            pos.y -= 0.6f;
            PlacePiece(pos, Prefabs.Sign, rot + 90)
                .Vars.SetText($"{SignFormatWhite}{entry.Description.Description}");


            pos.y -= 0.25f;
            x -= 1;
            PlacePiece(pos, Prefabs.Sconce, rot)
                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
            x += 2;
            PlacePiece(pos, Prefabs.Sconce, rot)
                .Fields<Fireplace>().Set(x => x.m_infiniteFuel, true).Set(x => x.m_disableCoverCheck, true);
            x -= 1;
            pos.y += 0.25f;

            pos.y -= 1;

            IReadOnlyList<object>? values = null;
            if (entry.Description.AcceptableValues?.GetType() is { IsConstructedGenericType: true } acceptableValuesType)
            {
                var genericDef = acceptableValuesType.GetGenericTypeDefinition();
                if (genericDef == typeof(AcceptableValueList<>))
                {
                    values = [.. (IEnumerable)acceptableValuesType.GetProperty(nameof(AcceptableValueList<int>.AcceptableValues), BindingFlags.Public | BindingFlags.Instance)
                        .GetValue(entry.Description.AcceptableValues)];
                }
                else if (genericDef == typeof(ModConfig.AcceptableEnum<>))
                {
                    values = [.. (IEnumerable)acceptableValuesType.GetProperty(nameof(ModConfig.AcceptableEnum<WorldPresets>.AcceptableValues), BindingFlags.Public | BindingFlags.Instance)
                        .GetValue(entry.Description.AcceptableValues)];
                }
            }

            if (values is { Count: > 0 and <= 8 })
            {
                var cols = Math.Max(2, (values.Count + 1) / 2);
                var dx = 1f; // 4f / cols;
                var initialX = x - (dx * (cols - 1) / 2);
                pos.y += 1;
                for (int i = 0; i < values.Count; i++)
                {
                    if ((i % cols) is 0)
                    {
                        x = initialX;
                        pos.y -= 1;
                    }

                    var sign = PlacePiece(pos, Prefabs.Sign, rot + 90);
                    sign.Vars.SetText(GetSignText(values[i], entry.SettingType, Color.Silver));

                    var configState = new ConfigState(entry, values[i], sign);
                    if (entry.SettingType.IsEnum && EnumUtils.OfType(entry.SettingType) is { IsBitSet: true } enumUtils)
                    {
                        var value = enumUtils.EnumToUInt64(entry.BoxedValue);
                        var flag = enumUtils.EnumToUInt64(values[i]);
                        configState.CandleState = (value & flag) == flag;
                    }
                    else
                    {
                        configState.CandleState = Equals(entry.BoxedValue, values[i]);
                    }
                    pos.y -= 0.55f;
                    var candle = PlacePiece(pos, Prefabs.Sconce, rot);
                    candle.Fields<Fireplace>()
                        .Set(x => x.m_secPerFuel, 0)
                        .Set(x => x.m_canRefill, false)
                        .Set(x => x.m_canTurnOff, true)
                        .Set(x => x.m_disableCoverCheck, true);
                    candle.Vars.SetFuel(candle.PrefabInfo.Fireplace!.m_maxFuel);
                    candle.Vars.SetState(configState.CandleState ? 1 : 2);
                    _candleToggles.Add(candle.m_uid, configState);
                    pos.y += 0.55f;

                    x += dx;
                }
            }
            else
            {
                var sign = PlacePiece(pos, Prefabs.Sign, rot + 90);
                sign.Vars.SetText(GetSignText(entry));

                if (entry.SettingType != typeof(bool))
                    _configBySign.Add(sign.m_uid, entry);
                else
                {
                    var configState = new ConfigState(entry, null, sign) { CandleState = (bool)entry.BoxedValue };
                    pos.y -= 0.55f;
                    var candle = PlacePiece(pos, Prefabs.Sconce, rot);
                    candle.Fields<Fireplace>()
                        .Set(x => x.m_secPerFuel, 0)
                        .Set(x => x.m_canRefill, false)
                        .Set(x => x.m_canTurnOff, true)
                        .Set(x => x.m_disableCoverCheck, true);
                    candle.Vars.SetState(configState.CandleState ? 1 : 2);
                    candle.Vars.SetFuel(candle.PrefabInfo.Fireplace!.m_maxFuel);
                    _candleToggles.Add(candle.m_uid, configState);
                    pos.y += 0.55f;
                }
            }
        }
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _isAdmin.Remove(zdo.m_uid);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        if (!Config.General.InWorldConfigRoom.Value)
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        
        if (zdo.PrefabInfo.Player is not null)
        {
            Peer peer = default;
            bool isAdmin;
            if (_isAdmin.TryGetValue(zdo.m_uid, out var entry))
                isAdmin = entry.IsAdmin;
            else
            {
                if (peer.IsDefault)
                    peer = peers.First(x => x.m_characterID == zdo.m_uid);
                isAdmin = Player.m_localPlayer?.GetZDOID() == zdo.m_uid || ZNet.instance.IsAdmin(peer.GetHostName());
                _isAdmin.Add(zdo.m_uid, (zdo, isAdmin));
                if (isAdmin)
                    UnregisterZdoProcessor = true;
            }

            if (!isAdmin && Character.InInterior(zdo.GetPosition()) &&
                ZoneSystem.GetZone(zdo.GetPosition()) == ZoneSystem.GetZone(_offset.WorldSpawn))
            {
                if (peer.IsDefault)
                    peer = peers.First(x => x.m_characterID == zdo.m_uid);
                /// <see cref="Game.FindSpawnPoint">
                var pos = _offset.WorldSpawn + Vector3.up * 2f;
                RPC.TeleportPlayer(peer, pos, zdo.GetRotation(), false);
                RPC.ShowMessage(peer, MessageHud.MessageType.Center, "$piece_noaccess");
            }
            return false;
        }
        else if (!PlacedPieces.Contains(zdo))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        var maxPeerY = peers.Max(x => x.m_refPos.y);

        if (maxPeerY > _offset.Room.y)
        {
            if (zdo.PrefabInfo.Fireplace is not null && _candleToggles.TryGetValue(zdo.m_uid, out var configState))
            {
                var state = zdo.Vars.GetState(1) is 1;
                string? text = null;
                // Check if a player has entered one of the upper (config) floors, to introduce a grace period before
                // processing the candles. Otherwise candles might still be turned off by rain while teleporting in.
                if (state != configState.CandleState && maxPeerY > _offset.Room.y + FloorOffset)
                {
                    configState.CandleState = state;
                    if (configState.Entry.SettingType == typeof(bool))
                    {
                        //Logger.LogWarning($"Setting config {configState.Entry.Definition.Key} = {state}, (CachedState: {configState.CandleState}, Setting: {configState.Entry.BoxedValue}, State: {state})");
                        configState.Entry.BoxedValue = state;
                        text = GetSignText(configState.Entry);
                    }
                    else if (configState.Value is not null && configState.Entry.SettingType.IsEnum && EnumUtils.OfType(configState.Entry.SettingType) is { IsBitSet: true } enumUtils)
                    {
                        var value = enumUtils.EnumToUInt64(configState.Entry.BoxedValue);
                        var flag = enumUtils.EnumToUInt64(configState.Value);
                        Color color;
                        if (state)
                        {
                            value |= flag;
                            var defaultValue = enumUtils.EnumToUInt64(configState.Entry.DefaultValue);
                            color = ((defaultValue & flag) == flag) ? Color.White : Color.Lime;
                        }
                        else
                        {
                            value &= ~flag;
                            color = Color.Silver;
                        }
                        configState.Entry.BoxedValue = enumUtils.UInt64ToEnum(value);
                        text = GetSignText(configState.Entry, color);
                    }
                    else
                    {
                        configState.Entry.BoxedValue = configState.Value;
                        text = GetSignText(configState.Entry);
                    }
                }
                else
                {
                    if (configState.Entry.SettingType == typeof(bool))
                    {
                        var entryState = (bool)configState.Entry.BoxedValue;
                        if (configState.CandleState != entryState)
                        {
                            //Logger.LogWarning($"Setting state {configState.Entry.Definition.Key} = {configState.Entry.BoxedValue}, (CachedState: {configState.CandleState}, Setting: {configState.Entry.BoxedValue}, State: {state})");
                            configState.CandleState = entryState;
                            text = GetSignText(configState.Entry);
                        }
                    }
                    else if (configState.Value is not null && configState.Entry.SettingType.IsEnum && EnumUtils.OfType(configState.Entry.SettingType) is { IsBitSet: true } enumUtils)
                    {
                        var value = enumUtils.EnumToUInt64(configState.Entry.BoxedValue);
                        var flag = enumUtils.EnumToUInt64(configState.Value);
                        var entryState = (value & flag) == flag;
                        if (configState.CandleState != entryState)
                        {
                            configState.CandleState = entryState;
                            Color color;
                            if (!configState.CandleState)
                                color = Color.Silver;
                            else
                            {
                                var defaultValue = enumUtils.EnumToUInt64(configState.Entry.DefaultValue);
                                color = ((defaultValue & flag) == flag) ? Color.White : Color.Lime;
                            }
                            text = GetSignText(configState.Value, configState.Entry.SettingType, color);
                        }
                    }
                    else
                    {
                        var entryState = Equals(configState.Value, configState.Entry.BoxedValue);
                        if (configState.CandleState != entryState)
                        {
                            configState.CandleState = entryState;
                            Color color;
                            if (configState.CandleState)
                                color = Equals(configState.Value, configState.Entry.DefaultValue) ? Color.White : Color.Lime;
                            else
                                color = Color.Silver;
                            text = GetSignText(configState.Value, configState.Entry.SettingType, color);
                        }
                    }

                    if (configState.CandleState != state)
                        zdo.Vars.SetState(configState.CandleState ? 1 : 2);
                }

                if (text is not null)
                    configState.Sign.Vars.SetText(text);

                return false;
            }

            if (zdo.PrefabInfo.Sign is not null && _configBySign.TryGetValue(zdo.m_uid, out var entry))
            {
                var text = zdo.Vars.GetText().RemoveRichTextTags();

                try { entry.BoxedValue = TomlTypeConverter.ConvertToValue(text, entry.SettingType); }
                catch (Exception)
                {
                    RPC.ShowMessage(peers.Where(x => _isAdmin.TryGetValue(x.m_characterID, out var y) && y.IsAdmin),
                        MessageHud.MessageType.Center, "$invalid_keybind_header");
                }
                zdo.Vars.SetText(GetSignText(entry));
                return true;
            }

            UnregisterZdoProcessor = true;
        }

        //if (zdo.PrefabInfo.TeleportWorld is not null && _configPieces.Contains(zdo.m_uid))
        //{
        //    // Not sure, why this is needed. Main portal loses connection sometimes
        //    if (zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal) == ZDOID.None)
        //        ZDOMan.instance.ConvertPortals();
        //}
        return false;
    }

    static void AddAdmin(ExtendedZDO guardStone, long playerId, string playerName)
    {
        /// <see cref="PrivateArea.SetPermittedPlayers"/>
        var i = guardStone.Vars.GetPermitted();
        guardStone.Vars.SetPermitted(i + 1);
        guardStone.Set(Invariant($"pu_id{i}"), playerId);
        guardStone.Set(Invariant($"pu_name{i}"), playerName);
    }

    //sealed record ConfigPiece(string PrefabName, Vector3 Pos, Vector3 Rot)
    //{
    //    public int Prefab { get; } = PrefabName.GetStableHashCode();
    //    public Quaternion Rotation { get; } = Quaternion.Euler(Rot);
    //}

    //readonly IReadOnlyList<ConfigPiece>
}
