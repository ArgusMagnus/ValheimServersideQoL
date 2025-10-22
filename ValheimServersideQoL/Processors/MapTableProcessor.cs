using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class MapTableProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("7d16d783-e298-43ee-b621-11022e4b392a");

    record Pin(long OwnerId, string Tag, Vector3 Pos, Minimap.PinType Type, bool IsChecked, string Author);
    readonly List<Pin> _pins = new();
    readonly List<Pin> _existingPins = new();
    byte[]? _emptyExplored;
    int _pinsHash;
    int _oldPinsHash;
    Regex? _includePortalRegex;
    Regex? _excludePortalRegex;

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        var filter = Config.MapTables.AutoUpdatePortalsInclude.Value.Trim();
        _includePortalRegex = string.IsNullOrEmpty(filter.Trim(['*'])) ? null : new(ConvertToRegexPattern(filter));
        filter = Config.MapTables.AutoUpdatePortalsExclude.Value.Trim();
        _excludePortalRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));

        if (!firstTime)
            return;

        _pins.Clear();
        _existingPins.Clear();
    }

    protected override void PreProcessCore(IEnumerable<Peer> peers)
    {
        _pins.Clear();
        _oldPinsHash = 0;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
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
                pins = [.. pins, .. ZDOMan.instance.GetPortals().Cast<ExtendedZDO>()
                    .Where(static x => !x.IsModCreator()) // exclude map room portals
                    .Select(static x => new Pin(Main.PluginGuidHash, x.Vars.GetTag(), x.GetPosition(), Minimap.PinType.Icon4, false, Main.PluginGuid))];
                if ((_includePortalRegex ?? _excludePortalRegex) is not null)
                    pins = pins.Where(x => _includePortalRegex?.IsMatch(x.Tag) is not false && _excludePortalRegex?.IsMatch(x.Tag) is not true);
            }
            if (Config.MapTables.AutoUpdateShips.Value)
            {
                pins = [.. pins, .. Instance<ShipProcessor>().Ships
                    .Where(static x => x is not null)
                    .Select(static x => new Pin(Main.PluginGuidHash, x!.PrefabInfo.Ship!.Value.Piece.m_name ?? "", x.GetPosition(), Minimap.PinType.Player, false, Main.PluginGuid))];
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
                Logger.LogWarning(Invariant($"MapTable data version {version} is not supported"));
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

        ShowMessage(peers, zdo, "$msg_mapsaved", Config.MapTables.UpdatedMessageType.Value);

        return false;
    }
}