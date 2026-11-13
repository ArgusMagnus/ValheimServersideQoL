using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.Player;

[Processor(Id)]
[RunBefore<IncineratorRegistryProcessor>]
[DependsOn<PlayerRegistryProcessor>]
public sealed class IncineratorProcessor : Processor<IncineratorProcessor.PrefabInfo>
{
  public const string Id = "30cb1ca8-ae2d-4982-af60-f47e0207d603";
  public sealed record PrefabInfo(Incinerator Incinerator, Container Container, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  readonly Dictionary<string, SacrificeInfo> _sacrifices = [];
  bool _enabled;

  protected override void Initialize()
  {
    _enabled =
      Config.Instance.CanSacrificeMegingjord.Value ||
      Config.Instance.CanSacrificeCryptKey.Value ||
      Config.Instance.CanSacrificeWishbone.Value ||
      Config.Instance.CanSacrificeTornSpirit.Value;

    _sacrifices.Clear();
    if (Config.Instance.CanSacrificeMegingjord.Value)
      _sacrifices.Add(PrefabNames.Megingjord, new(static x => PlayerProcessor.SetSacrifiedMegingjord(x, true), StatusEffects.Megingjord, Config.Instance.Localization.Value.SacrificedMegingjord));
    if (Config.Instance.CanSacrificeCryptKey.Value)
      _sacrifices.Add(PrefabNames.CryptKey, new(static x => PlayerProcessor.SetSacrifiedCryptKey(x, true), null, Config.Instance.Localization.Value.SacrificedCryptKey));
    if (Config.Instance.CanSacrificeWishbone.Value)
      _sacrifices.Add(PrefabNames.Wishbone, new(static x => PlayerProcessor.SetSacrifiedWishbone(x, true), StatusEffects.Wishbone, Config.Instance.Localization.Value.SacrificedWishbone));
    if (Config.Instance.CanSacrificeTornSpirit.Value)
      _sacrifices.Add(PrefabNames.TornSpirit, new(static x => PlayerProcessor.SetSacrifiedTornSpirit(x, true), StatusEffects.Demister, Config.Instance.Localization.Value.SacrificedTornSpirit));
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!_enabled)
      return ProcessResult.UnregisterProcessor;

    foreach (var item in Instance<IncineratorRegistryProcessor>().GetAvailableItems(zdo))
    {
      if (item.m_dropPrefab?.name is not { } name || !_sacrifices.TryGetValue(name, out var sacrificeInfo))
        continue;

      if (Instance<IncineratorRegistryProcessor>().GetRequestingPlayer(zdo) is not { } requestingPlayer)
        continue;

      Instance<IncineratorRegistryProcessor>().ReserveItem(item);
      Instance<IncineratorRegistryProcessor>().AddConversionResult(new IncineratorRegistryProcessor.DynamicResult(
        () => $"{requestingPlayer.PlayerName}: {Localization.instance.Localize(item.m_shared.m_name)}",
        () =>
        {
          sacrificeInfo.SetSacrificed(requestingPlayer.PlayerID);
          if (sacrificeInfo.StatusEffect is { } statusEffect)
            requestingPlayer.ZDO.RPC.Character.AddStatusEffect(statusEffect);
          RPC.ShowMessage(requestingPlayer.Owner, MessageHud.MessageType.Center, sacrificeInfo.Message);
          return null;
        }));
    }

    return default;
  }

  sealed record SacrificeInfo(Action<PlayerID> SetSacrificed, int? StatusEffect, string Message)
  {
    public string? ItemName { get; set; }
  }
}
