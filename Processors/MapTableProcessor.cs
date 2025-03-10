using BepInEx.Logging;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class MapTableProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
    readonly List<Pin> _pins = new();
    readonly List<Pin> _existingPins = new();
    byte[]? _emptyExplored;
    int _pinsHash;
    int _oldPinsHash;
    Regex? _includePortalRegex;
    Regex? _excludePortalRegex;

    public override void Initialize()
    {
        var filter = Config.MapTables.AutoUpdatePortalsInclude.Value.Trim();
        _includePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));
        filter = Config.MapTables.AutoUpdatePortalsExclude.Value.Trim();
        _excludePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));

        static string ConvertToRegexPattern(string searchPattern)
        {
            searchPattern = Regex.Escape(searchPattern);
            searchPattern = searchPattern.Replace("\\*", ".*").Replace("\\?", ".?");
            return $"(?i)^{searchPattern}$";
        }
    }

    public override void PreProcess()
    {
        _pins.Clear();
        _oldPinsHash = 0;
    }

    public override void Process(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.MapTable is null || !(Config.MapTables.AutoUpdatePortals.Value || Config.MapTables.AutoUpdateShips.Value))
            return;

        if (_pins is { Count: 0 })
        {
            var pins = Enumerable.Empty<Pin>();
            if (Config.MapTables.AutoUpdatePortals.Value)
            {
                pins = pins.Concat(ZDOMan.instance.GetPortals().Select(x => new Pin(Main.PluginGuidHash, x.GetString(ZDOVars.s_tag), x.GetPosition(), Minimap.PinType.Icon4, false, Main.PluginGuid)));
                if ((_includePortalRegex ?? _excludePortalRegex) is not null)
                    pins = pins.Where(x => _includePortalRegex?.IsMatch(x.Tag) is not false && _excludePortalRegex?.IsMatch(x.Tag) is not true);
            }
            if (Config.MapTables.AutoUpdateShips.Value)
            {
                pins = pins.Concat(SharedState.Ships
                    .Select(x =>
                    {
                        var y = ZDOMan.instance.GetZDO(x);
                        if (y is null)
                            SharedState.Ships.Remove(x);
                        return y;
                    })
                    .Where(x => x is not null)
                    .Select(x => new Pin(Main.PluginGuidHash, SharedState.PrefabInfo.TryGetValue(x!.GetPrefab(), out var info) ? info.Piece?.m_name ?? "" : "", x.GetPosition(), Minimap.PinType.Player, false, Main.PluginGuid)));
            }

            foreach (var pin in pins)
            {
                _pins.Add(pin);
                _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
            }

            (_pinsHash, _oldPinsHash) = (_oldPinsHash, _pinsHash);
        }

        if (_pinsHash == _oldPinsHash && SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        _existingPins.Clear();
        ZPackage pkg;
        var data = zdo.GetByteArray(ZDOVars.s_data);
        if (data is not null)
        {
            data = Utils.Decompress(data);
            pkg = new ZPackage(data);
            var version = pkg.ReadInt();
            if (version is not 3)
            {
                Logger.LogWarning($"MapTable data version {version} is not supported");
                return;
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

            foreach (var i in Enumerable.Range(0, pinCount))
            {
                var pin = new Pin(pkg.ReadLong(), pkg.ReadString(), pkg.ReadVector3(), (Minimap.PinType)pkg.ReadInt(), pkg.ReadBool(), pkg.ReadString());
                if (pin.OwnerId != Main.PluginGuidHash)
                    _existingPins.Add(pin);
            }
        }

        /// taken from <see cref="Minimap.GetSharedMapData"/> and <see cref="MapTable.GetMapData"/> 
        pkg = new ZPackage();
        pkg.Write(3);

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

        zdo.Set(ZDOVars.s_data, Utils.Compress(pkg.GetArray()));
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;

        Main.ShowMessage(peers, MessageHud.MessageType.TopLeft, "$msg_mapsaved");
    }
}