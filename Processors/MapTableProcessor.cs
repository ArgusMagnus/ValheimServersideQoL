using BepInEx.Logging;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Valheim.ServersideQoL.Processors;

sealed class MapTableProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
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
        base.Initialize();

        var filter = Config.MapTables.AutoUpdatePortalsInclude.Value.Trim();
        _includePortalRegex = string.IsNullOrEmpty(filter.Trim(['*'])) ? null : new(ConvertToRegexPattern(filter));
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
        base.PreProcess();
        _pins.Clear();
        _oldPinsHash = 0;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.MapTable is null || !(Config.MapTables.AutoUpdatePortals.Value || Config.MapTables.AutoUpdateShips.Value))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (_pins is { Count: 0 })
        {
            var pins = Enumerable.Empty<Pin>();
            if (Config.MapTables.AutoUpdatePortals.Value)
            {
                pins = pins.Concat(ZDOMan.instance.GetPortals().Cast<ExtendedZDO>().Select(x => new Pin(Main.PluginGuidHash, x.Vars.GetTag(), x.GetPosition(), Minimap.PinType.Icon4, false, Main.PluginGuid)));
                if ((_includePortalRegex ?? _excludePortalRegex) is not null)
                    pins = pins.Where(x => _includePortalRegex?.IsMatch(x.Tag) is not false && _excludePortalRegex?.IsMatch(x.Tag) is not true);
            }
            if (Config.MapTables.AutoUpdateShips.Value)
            {
                pins = pins.Concat(SharedProcessorState.Ships
                    .Select(x =>
                    {
                        if (!x.IsValid() || x.PrefabInfo.Ship is null)
                        {
                            SharedProcessorState.Ships.Remove(x);
                            return null;
                        }
                        return x;
                    })
                    .Where(x => x is not null)
                    .Select(x => new Pin(Main.PluginGuidHash, x!.PrefabInfo.Piece?.m_name ?? "", x.GetPosition(), Minimap.PinType.Player, false, Main.PluginGuid)));
            }

            foreach (var pin in pins)
            {
                _pins.Add(pin);
                _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
            }

            (_pinsHash, _oldPinsHash) = (_oldPinsHash, _pinsHash);
        }

        if (_pinsHash == _oldPinsHash)
            return false;

        _existingPins.Clear();
        ZPackage pkg;
        var data = zdo.Vars.GetData();
        if (data is not null)
        {
            data = Utils.Decompress(data);
            pkg = new ZPackage(data);
            var version = pkg.ReadInt();
            if (version is not 3)
            {
                Logger.LogWarning($"MapTable data version {version} is not supported");
                return false;
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

        zdo.Vars.SetData(Utils.Compress(pkg.GetArray()));

        RPC.ShowMessage(peers, MessageHud.MessageType.TopLeft, "$msg_mapsaved");

        return false;
    }
}