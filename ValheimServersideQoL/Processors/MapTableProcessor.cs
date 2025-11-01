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
    DateTimeOffset _nextUpdate;

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

        var now = DateTimeOffset.UtcNow;
        if (now < _nextUpdate)
            return false;
        _nextUpdate = now.AddSeconds(2);

        if (_pins is { Count: 0 })
        {
            if (Config.MapTables.AutoUpdatePortals.Value)
            {
                foreach (ExtendedZDO portal in ZDOMan.instance.GetPortals())
                {
                    if (portal.IsModCreator())
                        continue;
                    var tag = portal.Vars.GetTag();
                    if (_includePortalRegex?.IsMatch(tag) is false || _excludePortalRegex?.IsMatch(tag) is true)
                        continue;
                    var pin = new Pin(Main.PluginGuidHash, tag, portal.GetPosition(), Minimap.PinType.Icon4, false, Main.PluginGuid);
                    _pins.Add(pin);
                    _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
                }
            }
            if (Config.MapTables.AutoUpdateShips.Value)
            {
                foreach (var ship in Instance<ShipProcessor>().Ships)
                {
                    var pos = ship.GetPosition();
                    // round pos to multiples of 5 to reduce pin churn due to minor position changes
                    static float RoundToMultipleOf5(float value) => Mathf.Round(value / 5f) * 5f;
                    pos = new(RoundToMultipleOf5(pos.x), RoundToMultipleOf5(pos.y), RoundToMultipleOf5(pos.z));
                    var pin = new Pin(Main.PluginGuidHash, ship.PrefabInfo.Ship!.Value.Piece.m_name ?? "", pos, Minimap.PinType.Player, false, Main.PluginGuid);
                    _pins.Add(pin);
                    _oldPinsHash = (_oldPinsHash, pin).GetHashCode();
                }
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

        ShowMessage(peers, zdo, Config.Localization.MapTable.Updated, Config.MapTables.UpdatedMessageType.Value);

        return false;
    }
}