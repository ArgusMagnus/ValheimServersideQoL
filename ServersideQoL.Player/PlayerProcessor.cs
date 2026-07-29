namespace ServersideQoL.Player;

[Processor("7b156eea-3364-40ca-83ad-417a55fa6e4b")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class PlayerProcessor : Processor<PlayerRegistryProcessor.PrefabInfo>
{
  protected override void Initialize()
  {
    var subscribeSetTrigger = false;
    if (Game.m_staminaRate > 0)
      subscribeSetTrigger = Config.Instance.InfiniteBuildingStamina.Value || Config.Instance.InfiniteFarmingStamina.Value || Config.Instance.InfiniteMiningStamina.Value || Config.Instance.InfiniteWoodCuttingStamina.Value;
    RPC.Intercept.UpdateInterception("SetTrigger", OnZSyncAnimationSetTrigger, subscribeSetTrigger);

    RPC.Intercept.UpdateInterception("RPC_AnimateLever", RPC_AnimateLever,
        Config.Instance.CanSacrificeMegingjord.Value ||
        Config.Instance.CanSacrificeCryptKey.Value ||
        Config.Instance.CanSacrificeWishbone.Value ||
        Config.Instance.CanSacrificeTornSpirit.Value);

    Instance<PlayerRegistryProcessor>().StaminaUpdated -= OnPlayerStaminaUpdated;
    if (Config.Instance.InfiniteEncumberedStamina.Value || Config.Instance.InfiniteSneakingStamina.Value || Config.Instance.InfiniteSwimmingStamina.Value)
      Instance<PlayerRegistryProcessor>().StaminaUpdated += OnPlayerStaminaUpdated;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PlayerRegistryProcessor.PrefabInfo prefabInfo)
  {
    if (Instance<PlayerRegistryProcessor>().GetState(zdo) is not { } state)
      return ProcessResult.UnregisterProcessor;

    if (Config.Instance.CanSacrificeMegingjord.Value && GetSacrifiedMegingjord(state.PlayerID))
      RPC.AddStatusEffect(zdo, StatusEffects.Megingjord);
    if (Config.Instance.CanSacrificeWishbone.Value && GetSacrifiedWishbone(state.PlayerID))
      RPC.AddStatusEffect(zdo, StatusEffects.Wishbone);
    if (Config.Instance.CanSacrificeTornSpirit.Value && GetSacrifiedTornSpirit(state.PlayerID))
      RPC.AddStatusEffect(zdo, StatusEffects.Demister);

#if DEBUG
    RPC.AddStatusEffect(zdo, "Rested".GetStableHashCode());
#endif

    return ProcessResult.UnregisterProcessor;
  }

  void OnPlayerStaminaUpdated(ServersideQoLZDO zdo, PlayerState state, bool staminaValueChanged)
  {
    if (staminaValueChanged)
      return;

    if (state.Stamina < state.PrefabInfo.Player.m_encumberedStaminaDrain && Config.Instance.InfiniteEncumberedStamina.Value && zdo.Vars.GetAnimationIsEncumbered())
      RPC.UseStamina(zdo, -state.PrefabInfo.Player.m_encumberedStaminaDrain);
    else if (state.Stamina < state.PrefabInfo.Player.m_sneakStaminaDrain && Config.Instance.InfiniteSneakingStamina.Value && zdo.Vars.GetAnimationIsCrouching())
      RPC.UseStamina(zdo, -state.PrefabInfo.Player.m_sneakStaminaDrain);
    else if (state.Stamina < state.PrefabInfo.Player.m_swimStaminaDrainMinSkill && Config.Instance.InfiniteSwimmingStamina.Value && zdo.Vars.GetAnimationInWater())
      RPC.UseStamina(zdo, -state.PrefabInfo.Player.m_swimStaminaDrainMinSkill);
  }

  /// <see cref="ZSyncAnimation.SetTrigger(string)"/>
  void OnZSyncAnimationSetTrigger(ZRoutedRpc.RoutedRPCData data, string name)
  {
    if (Instance<PlayerRegistryProcessor>().GetStateForCharacterID(data.m_targetZDO) is not { } state)
      return;

    ItemDrop? rightItem = null;
    var prefab = state.ZDO.Vars.GetRightItem();
    if (prefab is not 0)
    {
      rightItem = ObjectDB.instance.GetItemPrefab(prefab)?.GetComponent<ItemDrop>();
      if (rightItem is null)
        Logger.LogWarning($"Player {state.PlayerName}: SetTrigger({name}): Right item prefab '{prefab}' not found");
    }

    static bool CheckStamina(string triggerName)
    {
      switch (triggerName)
      {
        case "swing_pickaxe":
          return Config.Instance.InfiniteMiningStamina.Value;
        case "swing_hammer":
          return Config.Instance.InfiniteBuildingStamina.Value;
        case "swing_hoe":
        case "scything":
          return Config.Instance.InfiniteFarmingStamina.Value;
        case "swing_axe0":
        case "battleaxe_attack0":
        case "dualaxes0":
          return Config.Instance.InfiniteWoodCuttingStamina.Value;
        default:
          return false;
      }
    }

    if (rightItem is not null && CheckStamina(name))
    {
      var requiredStamina = rightItem.m_itemData.m_shared.m_attack.m_attackStamina;
      if (state.ZDO.Vars.GetStamina() < 2 * requiredStamina)
        RPC.UseStamina(state.ZDO, -requiredStamina);
    }
  }

  void RPC_AnimateLever(ServersideQoLZDO zdo, ZRoutedRpc.RoutedRPCData data)
  {
    if (Instance<ContainerRegistryProcessor>().GetState(zdo) is not { PrefabInfo.Incinerator: not null } state)
      return;

    PlayerState? playerState = null;
    ContainerState.IInventory? inventory = null;
    if (Config.Instance.CanSacrificeMegingjord.Value && (inventory ??= state.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Megingjord))
    {
      playerState ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID);
      if (playerState is null)
        Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
      else
      {
        SetSacrifiedMegingjord(playerState.PlayerID, true);
        RPC.AddStatusEffect(playerState.ZDO, StatusEffects.Megingjord);
        RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Instance.Localization.Value.SacrificedMegingjord);
      }
    }
    if (Config.Instance.CanSacrificeCryptKey.Value && (inventory ??= state.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.CryptKey))
    {
      playerState ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID);
      if (playerState is null)
        Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
      else
      {
        SetSacrifiedCryptKey(playerState.PlayerID, true);
        RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Instance.Localization.Value.SacrificedCryptKey);
      }
    }
    if (Config.Instance.CanSacrificeWishbone.Value && (inventory ??= state.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.Wishbone))
    {
      playerState ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID);
      if (playerState is null)
        Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
      else
      {
        SetSacrifiedWishbone(playerState.PlayerID, true);
        RPC.AddStatusEffect(playerState.ZDO, StatusEffects.Wishbone);
        RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Instance.Localization.Value.SacrificedWishbone);
      }
    }
    if (Config.Instance.CanSacrificeTornSpirit.Value && (inventory ??= state.GetInventory()).Items.Any(static x => x.m_dropPrefab?.name is PrefabNames.TornSpirit))
    {
      playerState ??= Instance<PlayerRegistryProcessor>().GetStateForPeerID(data.m_senderPeerID);
      if (playerState is null)
        Logger.LogError($"Player ZDO with peer ID {data.m_senderPeerID} not found");
      else
      {
        SetSacrifiedTornSpirit(playerState.PlayerID, true);
        RPC.AddStatusEffect(playerState.ZDO, StatusEffects.Demister);
        RPC.ShowMessage(data.m_senderPeerID, MessageHud.MessageType.Center, Config.Instance.Localization.Value.SacrificedTornSpirit);
      }
    }
  }

  static bool GetSacrifiedMegingjord(long playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID}_SacrifiedMegingjord", defaultValue);
  static void SetSacrifiedMegingjord(long playerID, bool value) => DataZDO.ZDO.Set($"player{playerID}_SacrifiedMegingjord", value);
  static bool GetSacrifiedCryptKey(long playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID}_SacrifiedCryptKey", defaultValue);
  static void SetSacrifiedCryptKey(long playerID, bool value) => DataZDO.ZDO.Set($"player{playerID}_SacrifiedCryptKey", value);
  static bool GetSacrifiedWishbone(long playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID}_SacrifiedWishbone", defaultValue);
  static void SetSacrifiedWishbone(long playerID, bool value) => DataZDO.ZDO.Set($"player{playerID}_SacrifiedWishbone", value);
  static bool GetSacrifiedTornSpirit(long playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID}_SacrifiedTornSpirit", defaultValue);
  static void SetSacrifiedTornSpirit(long playerID, bool value) => DataZDO.ZDO.Set($"player{playerID}_SacrifiedTornSpirit", value);
  static float GetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float defaultValue = default) => DataZDO.ZDO.GetFloat($"player{playerID}_EstimatedSkillLevel_{skill}", defaultValue);
  static void SetEstimatedSkillLevel(long playerID, Skills.SkillType skill, float value) => DataZDO.ZDO.Set($"player{playerID}_EstimatedSkillLevel_{skill}", value);
}
