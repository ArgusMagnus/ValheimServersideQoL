using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.Player;

[Processor("7b156eea-3364-40ca-83ad-417a55fa6e4b")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class PlayerProcessor : Processor<ProcessorPrefabInfo<global::Player>>
{
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _attachedCartsByPlayer = [];

  protected override void Initialize()
  {
    RPC.Intercept.UpdateInterception("RPC_AnimateLever", RPC_AnimateLever,
        Config.Instance.CanSacrificeMegingjord.Value ||
        Config.Instance.CanSacrificeCryptKey.Value ||
        Config.Instance.CanSacrificeWishbone.Value ||
        Config.Instance.CanSacrificeTornSpirit.Value);

    Instance<PlayerRegistryProcessor>().StaminaUpdated -= OnPlayerStaminaUpdated;
    if (Config.Instance.InfiniteEncumberedStamina.Value || Config.Instance.InfiniteSneakingStamina.Value || Config.Instance.InfiniteSwimmingStamina.Value)
      Instance<PlayerRegistryProcessor>().StaminaUpdated += OnPlayerStaminaUpdated;

    var subscribeSetTrigger = false;
    if (Game.m_staminaRate > 0)
      subscribeSetTrigger = Config.Instance.InfiniteBuildingStamina.Value || Config.Instance.InfiniteFarmingStamina.Value || Config.Instance.InfiniteMiningStamina.Value || Config.Instance.InfiniteWoodCuttingStamina.Value;
    Instance<PlayerRegistryProcessor>().ItemUsed -= OnPlayerItemUsed;
    if (subscribeSetTrigger)
      Instance<PlayerRegistryProcessor>().ItemUsed += OnPlayerItemUsed;

    Instance<PlayerRegistryProcessor>().EmoteDetected -= OnEmoteDetected;
    if (Config.Instance.OpenCartEmote.Value is not ConfigBase.DisabledEmote)
      Instance<PlayerRegistryProcessor>().EmoteDetected += OnEmoteDetected;

    _attachedCartsByPlayer.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ProcessorPrefabInfo<global::Player> prefabInfo)
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

  void OnPlayerStaminaUpdated(PlayerState state)
  {
    if (state.Stamina < state.PrefabInfo.Component.m_encumberedStaminaDrain && Config.Instance.InfiniteEncumberedStamina.Value && state.ZDO.Vars.GetAnimationIsEncumbered())
      RPC.UseStamina(state.ZDO, -state.PrefabInfo.Component.m_encumberedStaminaDrain);
    else if (state.Stamina < state.PrefabInfo.Component.m_sneakStaminaDrain && Config.Instance.InfiniteSneakingStamina.Value && state.ZDO.Vars.GetAnimationIsCrouching())
      RPC.UseStamina(state.ZDO, -state.PrefabInfo.Component.m_sneakStaminaDrain);
    else if (state.Stamina < state.PrefabInfo.Component.m_swimStaminaDrainMinSkill && Config.Instance.InfiniteSwimmingStamina.Value && state.ZDO.Vars.GetAnimationInWater())
      RPC.UseStamina(state.ZDO, -state.PrefabInfo.Component.m_swimStaminaDrainMinSkill);
  }

  void OnPlayerItemUsed(PlayerState state, string animationTriggerName)
  {
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

    if (state.LastUsedItem is not null && CheckStamina(animationTriggerName))
    {
      var requiredStamina = state.LastUsedItem.m_itemData.m_shared.m_attack.m_attackStamina;
      if (state.ZDO.Vars.GetStamina() < 2 * requiredStamina)
        RPC.UseStamina(state.ZDO, -requiredStamina);
    }
  }

  void RPC_AnimateLever(ServersideQoLZDO zdo, ZRoutedRpc.RoutedRPCData data)
  {
    if (Instance<ContainerRegistryProcessor>().GetState(zdo) is not { } state || !GetPrefabInfo(zdo).HasComponent<Incinerator>())
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

  void OnEmoteDetected(PlayerState state, Emotes emote)
  {
    if (Config.Instance.OpenCartEmote.Value is not ConfigBase.AnyEmote && Config.Instance.OpenCartEmote.Value != emote)
      return;

    if (_attachedCartsByPlayer.TryGetValue(state.ZDO, out var cart) && cart.ZDO.GetOwner() == state.Owner && cart.Vars.GetAttachJoint())
      RPC.OpenResponse(cart, true);
  }

  internal void UpdateAttachedCart(ServersideQoLZDO cart)
  {
    cart.AssertIsAll<Vagon, Container>();

    if (Instance<PlayerRegistryProcessor>().GetStateForPeerID(cart.ZDO.GetOwner()) is not { } playerState)
      return;

    if (!cart.Vars.GetAttachJoint())
      _attachedCartsByPlayer.Remove(playerState.ZDO);
    else if (!_attachedCartsByPlayer.TryAdd(playerState.ZDO, cart))
      _attachedCartsByPlayer[playerState.ZDO] = cart;
    else
    {
      playerState.ZDO.Destroyed += OnPlayerDestroyed;
      cart.Destroyed += OnCartDestroyed;
    }
  }

  void OnPlayerDestroyed(ServersideQoLZDO zdo)
  {
    if (_attachedCartsByPlayer.Remove(zdo, out var cart))
      cart.Destroyed -= OnCartDestroyed;
  }

  void OnCartDestroyed(ServersideQoLZDO zdo)
  {
    List<ServersideQoLZDO>? toRemove = null;
    foreach (var (player, cart) in _attachedCartsByPlayer)
    {
      if (cart == zdo)
        (toRemove ??= []).Add(player);
    }

    if (toRemove is null)
      return;

    foreach (var player in toRemove)
    {
      if (_attachedCartsByPlayer.Remove(player, out var cart))
        player.Destroyed -= OnPlayerDestroyed;
    }
  }

  static bool GetSacrifiedMegingjord(PlayerID playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID.Value}_SacrifiedMegingjord", defaultValue);
  static void SetSacrifiedMegingjord(PlayerID playerID, bool value) => DataZDO.ZDO.Set($"player{playerID.Value}_SacrifiedMegingjord", value);
  internal static bool GetSacrifiedCryptKey(PlayerID playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID.Value}_SacrifiedCryptKey", defaultValue);
  static void SetSacrifiedCryptKey(PlayerID playerID, bool value) => DataZDO.ZDO.Set($"player{playerID.Value}_SacrifiedCryptKey", value);
  static bool GetSacrifiedWishbone(PlayerID playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID.Value}_SacrifiedWishbone", defaultValue);
  static void SetSacrifiedWishbone(PlayerID playerID, bool value) => DataZDO.ZDO.Set($"player{playerID.Value}_SacrifiedWishbone", value);
  static bool GetSacrifiedTornSpirit(PlayerID playerID, bool defaultValue = default) => DataZDO.ZDO.GetBool($"player{playerID.Value}_SacrifiedTornSpirit", defaultValue);
  static void SetSacrifiedTornSpirit(PlayerID playerID, bool value) => DataZDO.ZDO.Set($"player{playerID.Value}_SacrifiedTornSpirit", value);
}
