using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class DoorProcessor : Processor
{
    readonly Dictionary<ExtendedZDO, DateTimeOffset> _openSince = [];
    readonly List<ExtendedZDO> _allowedPlayers = [];
    readonly Dictionary<int, int> _keyItemWeightByHash = [];

    /// <see cref="VisEquipment"/>
    readonly IEnumerable<int> _visEquipmentVars = [ZDOVars.s_helmetItem, ZDOVars.s_chestItem, ZDOVars.s_legItem, ZDOVars.s_shoulderItem, ZDOVars.s_utilityItem,
        ZDOVars.s_leftItem, ZDOVars.s_rightItem, ZDOVars.s_leftBackItem, ZDOVars.s_rightBackItem];

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);

        if (!firstTime)
            return;

        _openSince.Clear();
        _allowedPlayers.Clear();
        _keyItemWeightByHash.Clear();
    }

    void OnDoorDestroyed(ExtendedZDO zdo)
    {
        _openSince.Remove(zdo);
    }

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        const int StateClosed = 0;

        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Door is null)
            return false;

        if (zdo.PrefabInfo.Door.m_keyItem is { name: PrefabNames.CryptKey } && zdo.Vars.GetState() is StateClosed)
        {
            var fields = zdo.Fields<Door>();
            if (!Config.Players.CanSacrificeCryptKey.Value)
                fields.Reset(static x => x.m_keyItem);
            else
            {
                UnregisterZdoProcessor = false;

                /// <see cref="RandEventSystem.GetPossibleRandomEvents"/> <see cref="Player.UpdateEvents"/>
                _allowedPlayers.Clear();
                if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.defeated_gdking))
                {
                    foreach (var peer in peers)
                    {
                        if (Vector3.Distance(peer.m_refPos, zdo.GetPosition()) > ZoneSystem.c_ZoneHalfSize / 2)
                            continue;
                        if (Instance<PlayerProcessor>().Players.TryGetValue(peer.m_characterID, out var player) && DataZDO.Vars.GetSacrifiedCryptKey(player.Vars.GetPlayerID()))
                            _allowedPlayers.Add(player);
                    }
                }

                if (_allowedPlayers.Count is 0)
                {
                    if (fields.ResetIfChanged(static x => x.m_keyItem))
                        RecreateZdo = true;
                }
                else
                {
                    // Not possible to set m_keyItem to null, so an item possessed by all players is chosen
                    int maxWeight = 0;
                    int keyHash = 0;
                    foreach (var zdoVar in _visEquipmentVars)
                    {
                        foreach (var player in _allowedPlayers)
                        {
                            var itemHash = player.GetInt(zdoVar);
                            if (itemHash is 0)
                                continue;
                            if (!_keyItemWeightByHash.TryGetValue(itemHash, out var weight))
                                weight = 1;
                            else
                                weight++;
                            _keyItemWeightByHash[itemHash] = weight;
                            if (weight <= maxWeight)
                                continue;
                            maxWeight = weight;
                            keyHash = itemHash;
                        }
                    }
                    _keyItemWeightByHash.Clear();

                    if (keyHash is 0 || ObjectDB.instance.GetItemPrefab(keyHash)?.GetComponent<ItemDrop>() is not { } keyItem)
                        Logger.LogWarning($"Item {keyHash} was chosen as key, but it's not a valid ItemDrop");
                    else if (fields.SetIfChanged(static x => x.m_keyItem, keyItem))
                        RecreateZdo = true;
                }
            }
        }

        if (float.IsNaN(Config.Doors.AutoCloseMinPlayerDistance.Value))
            return false;

        /// <see cref="Door.CanInteract"/>
        if (zdo.PrefabInfo.Door.m_keyItem is not null || zdo.PrefabInfo.Door.m_canNotBeClosed || zdo.Vars.GetCreator() is 0)
            return false;

        UnregisterZdoProcessor = false;

        if (!CheckMinDistance(peers, zdo, Config.Doors.AutoCloseMinPlayerDistance.Value))
            return false;

        if (zdo.Vars.GetState() is StateClosed)
        {
            if (_openSince.Remove(zdo))
                zdo.Destroyed -= OnDoorDestroyed;
            return true;
        }

        if (!_openSince.TryGetValue(zdo, out var openSince))
        {
            _openSince.Add(zdo, openSince = DateTimeOffset.UtcNow);
            zdo.Destroyed += OnDoorDestroyed;
        }

        if (DateTimeOffset.UtcNow - openSince < TimeSpan.FromSeconds(2))
            return false;

        zdo.Vars.SetState(StateClosed);
        if (_openSince.Remove(zdo))
            zdo.Destroyed -= OnDoorDestroyed;

        return true;
    }
}