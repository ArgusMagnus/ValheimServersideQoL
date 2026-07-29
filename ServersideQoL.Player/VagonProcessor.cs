namespace ServersideQoL.Player;

[Processor("8a10c73e-67db-4dc2-8b0c-3242aac82e3b")]
public sealed class VagonProcessor : Processor<VagonProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Vagon Vagon, Piece Piece, Container Container) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (Config.Instance.OpenCartEmote.Value is ConfigBase.DisabledEmote)
      return ProcessResult.UnregisterProcessor;

    Instance<PlayerProcessor>().UpdateAttachedCart(zdo);
    return default;
  }
}
