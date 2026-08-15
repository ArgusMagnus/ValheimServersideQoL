using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.Player;

[Processor("28b93edf-6ede-4dbe-92ed-99275be3915f")]
public sealed class CryptDoorProcessor : Processor<CryptDoorProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Door Door) : ProcessorPrefabInfo;

  readonly List<ServersideQoLZDO> _allowedPlayers = [];
  readonly Dictionary<int, int> _keyItemWeightByHash = [];

  /// <see cref="VisEquipment"/>
  readonly IEnumerable<int> _visEquipmentVars = [ZDOVars.s_helmetItem, ZDOVars.s_chestItem, ZDOVars.s_legItem, ZDOVars.s_shoulderItem, ZDOVars.s_utilityItem,
        ZDOVars.s_leftItem, ZDOVars.s_rightItem, ZDOVars.s_leftBackItem, ZDOVars.s_rightBackItem];

  protected override void Initialize()
  {
    _allowedPlayers.Clear();
    _keyItemWeightByHash.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    const int StateClosed = 0;

    if (prefabInfo.Door.m_keyItem is not { name: PrefabNames.CryptKey } || zdo.Vars.GetState() is not StateClosed)
      return ProcessResult.UnregisterProcessor;

    var fields = zdo.Fields<Door>();
    if (!Config.Instance.CanSacrificeCryptKey.Value)
    {
      fields.Reset(static () => x => x.m_keyItem);
      return ProcessResult.UnregisterProcessor;
    }

    _allowedPlayers.Clear();
    if (ZoneSystem.instance.GetGlobalKey(GlobalKeys.defeated_gdking))
    {
      foreach (var peer in peers)
      {
        if (Vector3.Distance(peer.RefPos, zdo.ZDO.GetPosition()) > ZoneSystem.c_ZoneSizeHalf / 2)
          continue;
        if (peer.PlayerState is { } state && PlayerProcessor.GetSacrifiedCryptKey(state.PlayerID))
          _allowedPlayers.Add(state.ZDO);
      }
    }

    if (_allowedPlayers.Count is 0)
    {
      if (fields.UpdateResetValue(static () => x => x.m_keyItem))
        return ProcessResult.RecreateZDO;
    }
    else
    {
      // Not possible to set m_keyItem to null, so an item possessed by all players is chosen
      int maxWeight = 0;
      int keyHash = 0;
      _keyItemWeightByHash.Clear();
      foreach (var zdoVar in _visEquipmentVars)
      {
        foreach (var player in _allowedPlayers)
        {
          var itemHash = player.ZDO.GetInt(zdoVar);
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
      _allowedPlayers.Clear();

      if (keyHash is 0 || ObjectDB.instance.GetItemPrefab(keyHash)?.GetComponent<ItemDrop>() is not { } keyItem)
        Logger.LogWarning($"Item {keyHash} was chosen as key, but it's not a valid ItemDrop");
      else if (fields.UpdateValue(static () => x => x.m_keyItem, keyItem))
        return ProcessResult.RecreateZDO;
    }

    return default;
  }
}
