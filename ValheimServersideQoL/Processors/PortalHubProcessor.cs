using System.Text.RegularExpressions;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class PortalHubProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("1523cbb8-ed88-4608-84c5-8526937020f7");

    sealed class PortalState
    {
        public required string Tag { get; set; }
        public required int HubId { get; set; }
        public required bool AllowAllItems { get; init; }
    }

    readonly Dictionary<ExtendedZDO, PortalState> _knownPortals = [];
    readonly IReadOnlyList<int> _torchPrefabs = [Prefabs.StandingIronTorch, Prefabs.StandingIronTorchBlue, Prefabs.StandingIronTorchGreen];
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
                if (!zdo.IsModCreator() && CheckFilter(zdo, tag = zdo.Vars.GetTag()))
                {
                    _knownPortals.Add(zdo, new() { Tag = tag, HubId = zdo.Vars.GetPortalHubId(), AllowAllItems = zdo.Fields<TeleportWorld>().GetBool(static () => x => x.m_allowAllItems) });
                    zdo.Destroyed += OnKnownPortalDestroyed;
                }
            }
            var hubIds = _knownPortals.Values.Where(static x => !x.AllowAllItems).Select(static x => x.HubId).ToHashSet();
            var hubIdsAllItems = _knownPortals.Values.Where(static x => x.AllowAllItems).Select(static x => x.HubId).ToHashSet();
            int id = 0;
            foreach (var (zdo, state) in _knownPortals.Where(static x => x.Value.HubId is 0).OrderBy(static x => x.Value.Tag))
            {
                var ids = state.AllowAllItems ? hubIdsAllItems : hubIds;
                while (!ids.Add(++id)) ;
                zdo.Vars.SetPortalHubId(state.HubId = id);
            }
        }

        if (changed || !_hubEnabled)
            UpdatePortalHub();
    }

    void OnKnownPortalDestroyed(ExtendedZDO zdo)
    {
        if (_knownPortals.Remove(zdo) && _hubEnabled)
            _updateHub = true;
    }

    bool CheckFilter(ExtendedZDO zdo, string tag)
    {
        if ((_includeRegex ?? _excludeRegex) is null)
            return true;

        return _includeRegex?.IsMatch(tag) is not false && _excludeRegex?.IsMatch(tag) is not true;
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
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
        else if (zdo.PrefabInfo.TeleportWorld is null || zdo.IsModCreator())
        {
            UnregisterZdoProcessor = true;
            return false;
        }
        else
        {
            var tag = zdo.Vars.GetTag();
            _knownPortals.TryGetValue(zdo, out var state);

            if (_autoTag && state is null && string.IsNullOrEmpty(tag))
            {
                var biome = GetBiome(zdo.GetPosition());
                var biomeText = Localization.instance.Localize($"$biome_{biome.ToString().ToLowerInvariant()}");
                var knownTags = ZDOMan.instance.GetPortals().Cast<ExtendedZDO>().Select(static x => x.Vars.GetTag()).ToHashSet();
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

            if (!CheckFilter(zdo, tag))
            {
                if (_knownPortals.Remove(zdo))
                    _updateHub = true;
                zdo.Destroyed -= OnKnownPortalDestroyed;
            }
            else if (state?.Tag != tag)
            {
                if (state is not null)
                    state.Tag = tag;
                else
                {
                    _knownPortals.Add(zdo, state = new() { Tag = tag, HubId = zdo.Vars.GetPortalHubId(), AllowAllItems = zdo.Fields<TeleportWorld>().GetBool(static () => x => x.m_allowAllItems) });
                    zdo.Destroyed += OnKnownPortalDestroyed;

                    if (state.HubId is 0)
                    {
                        var hubIds = _knownPortals.Values.Where(x => x.AllowAllItems == state.AllowAllItems).Select(static x => x.HubId).ToHashSet();
                        int id = 0;
                        while (!hubIds.Add(++id)) ;
                        zdo.Vars.SetPortalHubId(state.HubId = id);
                    }
                }
                _updateHub = true;
            }
            return true;
        }
    }

    void UpdatePortalHub()
    {
        foreach (var zdo in PlacedObjects)
            zdo.Destroy();
        PlacedObjects.Clear();

        if (!_hubEnabled)
            return;

        IReadOnlyList<PortalState> states = [.. _knownPortals.Values
            .GroupBy(static x => x.Tag)
            .Where(static x => x.Count() % 2 is not 0)
            .Select(static x => x.First())
            .Concat(Config.General.InWorldConfigRoom.Value ? [new PortalState() { Tag = InGameConfigProcessor.PortalHubTag, HubId = 0, AllowAllItems = true }] : [])
            .OrderBy(static x => x.Tag)];

        if (states.Count is 0)
            return;

        // 4*(width-1) = count -> width = count/4 + 1
        var width = Math.Max(3, (int)Math.Ceiling(states.Count / 4f + 1));
        _hubRadius = (width + 1) * 4f * Mathf.Sqrt(2);

        PlacePiece(_offset with { y = _offset.y - 2 }, Prefabs.DvergerGuardstone, 0)
            .Fields<PrivateArea>(true).Set(static x => x.m_radius, _hubRadius);

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

        var statesEnumerator = states.GetEnumerator();
        for (int k = 0; k < width; k++)
            PlacePortalAndWalls(0, k, width, statesEnumerator, (_, k) => k < width - 1);
        for (int i = 0; i < width; i++)
            PlacePortalAndWalls(i, width - 1, width, statesEnumerator, (i, _) => i < width - 1);
        for (int k = width - 1; k >= 0; k--)
            PlacePortalAndWalls(width - 1, k, width, statesEnumerator, (_, k) => k > 0);
        for (int i = width - 1; i >= 0; i--)
            PlacePortalAndWalls(i, 0, width, statesEnumerator, (i, _) => i > 0);

        if (statesEnumerator.MoveNext())
            throw new Exception("Algorithm failed to place all portals");
    }

    IReadOnlyList<int> GetTorches(int hubId)
    {
        if (hubId is 0)
            return [];
        List<int> torches = [0];
        for (int i = 0; i < hubId; i++)
        {
            torches[0]++;
            for (int k = 0; k < torches.Count; k++)
            {
                if (torches[k] < 3)
                    continue;
                torches[k] = 0;
                if (k == torches.Count - 1)
                    torches.Add(1);
                else
                    torches[k + 1]++;
            }
        }

        for (int k = 0; k < torches.Count; k++)
            torches[k] = _torchPrefabs[torches[k]];
        return torches;
    }

    void PlacePortalAndWalls(int i, int k, int width, IEnumerator<PortalState> statesEnumerator, Func<int, int, bool> placePortal)
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

        if (placePortal(i, k) && statesEnumerator.MoveNext())
        {
            var state = statesEnumerator.Current;
            var ofsY = state.AllowAllItems ? -0.25f : 0f;
            pos.y += ofsY;
            var zdo = PlacePiece(pos, state.AllowAllItems ? Prefabs.Portal : Prefabs.PortalWood, rot);
            pos.y -= ofsY;
            zdo.Fields<TeleportWorld>().Set(static x => x.m_allowAllItems, true);
            zdo.Vars.SetTag(state.Tag);
            zdo.UnregisterAllProcessors();

            if (iIsEdge && kIsEdge)
            {
                pos.z += (k is 0 ? -0.25f : 0.25f) * Mathf.Sqrt(2);
                pos.x += (i is 0 ? -0.25f : 0.25f) * Mathf.Sqrt(2);
            }
            else if (!iIsEdge)
                pos.z += k is 0 ? -0.25f : 0.25f;
            else if (!kIsEdge)
                pos.x += i is 0 ? -0.25f : 0.25f;

            pos.y += 2f;
            var sign = PlacePiece(pos, Prefabs.Sign, rot);
            sign.Vars.SetText($"<color=white>{state.Tag}");

            if (GetTorches(state.HubId) is { Count: > 0 } torches)
            {
                pos.y -= 1.5f;
                var p = pos;
                var d = iIsEdge && kIsEdge ? 0.25f / Mathf.Sqrt(2) : 0.25f;
                for (var j = 0; j < torches.Count; j++)
                {
                    pos = p;
                    var dx = (j - (torches.Count - 1) / 2f) * d;
                    if (iIsEdge && kIsEdge)
                    {
                        pos.x += k is 0 ? dx : -dx;
                        pos.z += i is 0 ? -dx : dx;
                    }
                    else if (!iIsEdge)
                        pos.x += k is 0 ? dx : -dx;
                    else if (!kIsEdge)
                        pos.z += i is 0 ? -dx : dx;
                    PlacePiece(pos, torches[j], rot).Fields<Fireplace>()
                        .Set(static x => x.m_infiniteFuel, true)
                        .Set(static x => x.m_disableCoverCheck, true);
                }
            }
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

            //rot -= 90;
            //pos.x += i is 0 ? 0.25f : -0.25f;
            //pos.y += 0.5f;
            //PlacePiece(pos, Prefabs.Sconce, rot)
            //    .Fields<Fireplace>().Set(static x => x.m_infiniteFuel, true).Set(static x => x.m_disableCoverCheck, true);
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

            //rot -= 90;
            //pos.z += k is 0 ? 0.25f : -0.25f;
            //pos.y += 0.5f;
            //PlacePiece(pos, Prefabs.Sconce, rot)
            //    .Fields<Fireplace>().Set(static x => x.m_infiniteFuel, true).Set(static x => x.m_disableCoverCheck, true);
        }
    }
}