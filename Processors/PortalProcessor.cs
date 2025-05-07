using BepInEx.Logging;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    bool _destroyNewPortals;
    readonly HashSet<ExtendedZDO> _initialPortals = [];

    readonly Dictionary<ExtendedZDO, string> _knownPortals = [];
    bool _hubEnabled;
    float _hubRadius = 0;
    bool _updateHub;
    Regex? _includeRegex;
    Regex? _excludeRegex;

    bool _autoTag;

    readonly Vector3 _offset = new Func<Vector3>(static () =>
    {
        var pos = new Vector3(WorldGenerator.waterEdge + 5 * ZoneSystem.c_ZoneSize, 0, 0);
        while (!Character.InInterior(pos))
            pos.y += 1000;
        return pos;
    }).Invoke();

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        var changed = _hubEnabled != Config.PortalHub.Enable.Value;
        _hubEnabled = Config.PortalHub.Enable.Value;
        _autoTag = _hubEnabled && Config.PortalHub.AutoNameNewPortals.Value;

        _knownPortals.Clear();
        if (_hubEnabled)
        {
            var filter = Config.PortalHub.Include.Value.Trim();
            _includeRegex = string.IsNullOrEmpty(filter.Trim(['*'])) ? null : new(ConvertToRegexPattern(filter));
            filter = Config.PortalHub.Exclude.Value.Trim();
            _excludeRegex = string.IsNullOrEmpty(filter) ? null : new(ConvertToRegexPattern(filter));

            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
            {
                string? tag = null;
                if (zdo.Vars.GetCreator() != Main.PluginGuidHash && CheckFilter(zdo, tag = zdo.Vars.GetTag()))
                    _knownPortals.Add(zdo, tag);
            }
        }

        if (changed || !_hubEnabled)
            UpdatePortalHub();

        _initialPortals.Clear();
        _destroyNewPortals = Config.GlobalsKeys.NoPortalsPreventsContruction.Value && ZoneSystem.instance.GetGlobalKey(GlobalKeys.NoPortals);
        if (_destroyNewPortals)
        {
            ZoneSystem.instance.RemoveGlobalKey(GlobalKeys.NoPortals);
            foreach (ExtendedZDO zdo in ZDOMan.instance.GetPortals())
                _initialPortals.Add(zdo);
        }

        if (_destroyNewPortals || _hubEnabled)
            RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        _initialPortals.Remove(zdo);
        if (_hubEnabled && _knownPortals.Remove(zdo))
            _updateHub = true;
    }

    bool CheckFilter(ExtendedZDO zdo, string tag)
    {
        if ((_includeRegex ?? _excludeRegex) is null)
            return true;

        return _includeRegex?.IsMatch(tag) is not false && _excludeRegex?.IsMatch(tag) is not true;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        long? creator = null;
        if (_destroyNewPortals && zdo.PrefabInfo.TeleportWorld is not null && !_initialPortals.Contains(zdo) && (creator = zdo.Vars.GetCreator()) != Main.PluginGuidHash)
        {
            RPC.Remove(zdo);

            /// <see cref="Player.TryPlacePiece(Piece)"/>
            var owner = zdo.GetOwner();
            if (owner is not 0)
                RPC.ShowMessage(owner, MessageHud.MessageType.Center, "$msg_nobuildzone");

            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_hubEnabled)
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        else if (zdo.PrefabInfo.Player is not null) // Keep running at least one instance
        {
            if (_updateHub && !peers.Any(x => Utils.DistanceXZ(x.m_refPos, _offset) < _hubRadius))
            {
                _updateHub = false;
                UpdatePortalHub();
            }
            return false;
        }
        else if (zdo.PrefabInfo.TeleportWorld is null || (creator ??= zdo.Vars.GetCreator()) == Main.PluginGuidHash)
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        else
        {
            var tag = zdo.Vars.GetTag();
            _knownPortals.TryGetValue(zdo, out var oldTag);
            if (_autoTag && oldTag is null && string.IsNullOrEmpty(tag))
            {
                var biome = WorldGenerator.instance.GetBiome(zdo.GetPosition());
                var biomeText = Localization.instance.Localize($"$biome_{biome.ToString().ToLowerInvariant()}");
                var knownTags = ZDOMan.instance.GetPortals().Cast<ExtendedZDO>().Select(x => x.Vars.GetTag()).ToHashSet();
                foreach (var i in Enumerable.Range(1, 1000))
                {
                    var newTag = string.Format(Config.PortalHub.AutoNameNewPortalsFormat.Value, biomeText, i);
                    if (!knownTags.Contains(newTag))
                    {
                        zdo.Vars.SetTag(tag = newTag);
                        break;
                    }
                }
            }

            if (CheckFilter(zdo, tag) && oldTag != tag)
            {
                _knownPortals[zdo] = tag;
                _updateHub = true;
            }
            return true;
        }
    }

    void UpdatePortalHub()
    {
        foreach (var zdo in PlacedPieces)
            zdo.Destroy();
        PlacedPieces.Clear();

        if (!_hubEnabled)
            return;

        IReadOnlyList<string> tags = [.. _knownPortals.Values
            .GroupBy(x => x)
            .Where(x => x.Count() % 2 is not 0)
            .Select(x => x.Key)
            .Concat(Config.General.InWorldConfigRoom.Value ? [InGameConfigProcessor.PortalHubTag] : [])
            .OrderBy(x => x)];

        if (tags.Count is 0)
            return;

        // 4*(width-1) = count -> width = count/4 + 1
        var width = Math.Max(3, (int)Math.Ceiling(tags.Count / 4f + 1));
        _hubRadius = (width + 1) * 4f * Mathf.Sqrt(2);

        PlacePiece(_offset with { y = _offset.y - 2 }, Prefabs.DvergerGuardstone, 0)
            .Fields<PrivateArea>(true).Set(x => x.m_radius, _hubRadius);

        for (int i = 0; i < width; i++)
        {
            var x = (i - width / 2f) * 4;
            for (int k = 0; k < width; k++)
            {
                var z = (k - width / 2f) * 4;

                var pos = _offset;
                pos.x += x;
                pos.z += z;

                PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
                pos.y += 4.5f;
                PlacePiece(pos, Prefabs.GraustenFloor4x4, 0f);
                pos.y -= 4.5f;
            }
        }

        var tagsEnumerator = tags.GetEnumerator();
        for (int k = 0; k < width; k++)
            PlacePortalAndWalls(0, k, width, tagsEnumerator, (_, k) => k < width - 1);
        for (int i = 0; i < width; i++)
            PlacePortalAndWalls(i, width - 1, width, tagsEnumerator, (i, _) => i < width - 1);
        for (int k = width - 1; k >= 0; k--)
            PlacePortalAndWalls(width - 1, k, width, tagsEnumerator, (_, k) => k > 0);
        for (int i = width - 1; i >= 0; i--)
            PlacePortalAndWalls(i, 0, width, tagsEnumerator, (i, _) => i > 0);

        if (tagsEnumerator.MoveNext())
            throw new Exception("Algorithm failed to place all portals");

        ZDOMan.instance.ConvertPortals();
    }

    void PlacePortalAndWalls(int i, int k, int width, IEnumerator<string> tagsEnumerator, Func<int, int, bool> placePortal)
    {
        var x = (i - width / 2f) * 4;
        var z = (k - width / 2f) * 4;

        var pos = _offset;
        pos.x += x;
        pos.z += z;

        var iIsEdge = i is 0 || i == width - 1;
        var kIsEdge = k is 0 || k == width - 1;
        if (!iIsEdge && !kIsEdge)
            throw new Exception("Unexpected values");

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

        if (placePortal(i, k) && tagsEnumerator.MoveNext())
        {
            var tag = tagsEnumerator.Current;
            var zdo = PlacePiece(pos, Prefabs.PortalWood, rot);
            zdo.Fields<TeleportWorld>().Set(x => x.m_allowAllItems, true);
            zdo.Vars.SetTag(tag);

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
            sign.Vars.SetText($"<color=white>{tag}");
        }

        if (iIsEdge)
        {
            pos = _offset;
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
            pos = _offset;
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