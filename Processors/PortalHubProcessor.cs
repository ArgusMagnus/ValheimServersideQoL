using BepInEx.Configuration;
using BepInEx.Logging;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalHubProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    readonly Dictionary<ExtendedZDO, string> _knownPortals = new();
    bool _enabled;
    float _hubRadius = 0;
    bool _update;
    Regex? _includeRegex;
    Regex? _excludeRegex;

    readonly Vector3 _offset = new Func<Vector3>(static () =>
    {
        var pos = new Vector3(WorldGenerator.worldSize * 1.1f, 0, 0);
        while (!Character.InInterior(pos))
            pos.y += 1000;
        return pos;
    }).Invoke();

    public override void Initialize()
    {
        base.Initialize();

        var changed = _enabled != Config.PortalHub.Enable.Value;
        _enabled = Config.PortalHub.Enable.Value;

        _knownPortals.Clear();
        if (_enabled)
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
        if (changed)
            UpdatePortalHub();
        if (_enabled)
            RegisterZdoDestroyed();
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        if (_enabled && _knownPortals.Remove(zdo))
            _update = true;
    }

    bool CheckFilter(ExtendedZDO zdo, string tag)
    {
        if ((_includeRegex ?? _excludeRegex) is null)
            return true;

        return _includeRegex?.IsMatch(tag) is not false && _excludeRegex?.IsMatch(tag) is not true;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (!_enabled)
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        else if (zdo.PrefabInfo.Player is not null) // Keep running at least one instance
        {
            if (_update && !peers.Any(x => Utils.DistanceXZ(x.m_refPos, _offset) < _hubRadius))
            {
                _update = false;
                UpdatePortalHub();
            }
            return false;
        }
        else if (zdo.PrefabInfo.TeleportWorld is null || PlacedPieces.Contains(zdo))
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        else
        {
            string? tag = null;
            if (zdo.Vars.GetCreator() != Main.PluginGuidHash && CheckFilter(zdo, tag = zdo.Vars.GetTag()) && (!_knownPortals.TryGetValue(zdo, out var oldTag) || oldTag != tag))
            {
                _knownPortals[zdo] = tag;
                _update = true;
            }
            return true;
        }
    }

    void UpdatePortalHub()
    {
        foreach (var zdo in PlacedPieces)
            zdo.Destroy();
        PlacedPieces.Clear();
        
        if (!_enabled)
            return;

        IReadOnlyList<string> tags = [.. _knownPortals.Values
            .GroupBy(x => x)
            .Where(x => x.Count() % 2 is not 0)
            .Select(x => x.Key)
            .Concat(Config.General.InWorldConfigRoom.Value ? [InGameConfigProcessor.PortalHubTag] : [])
            .OrderBy(x => x)];

        // 4*(width-1) = count -> width = count/4 + 1
        var width = (int)Math.Ceiling(tags.Count / 4f + 1);
        _hubRadius = (width + 1) * 4f * Mathf.Sqrt(2);

        PlacePiece(_offset, Prefabs.DvergerGuardstone, 0)
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
        for(int i = 0; i < width; i++)
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

        if (placePortal(i,k) && tagsEnumerator.MoveNext())
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
