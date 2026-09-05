using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.Backpack;

[Processor(Id)]
[DependsOn<PlayerRegistryProcessor>]
sealed class BackpackProcessor : Processor<BackpackProcessor.PrefabInfo>
{
  public const string Id = "fdd88a40-add7-413c-ac4b-894ac26c500d";

  static int BackpackPrefabHash => Prefabs.PrivateChest;

  public sealed record PrefabInfo(Container Container) : ProcessorPrefabInfo
  {
    public override bool IsValid => PrefabInfo.PrefabHash == BackpackPrefabHash;
  }

  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _backpacks = [];

  protected override void Initialize()
  {
    _backpacks.Clear();
    Instance<PlayerRegistryProcessor>().EmoteDetected += OnEmoteDetected;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!PlacedObjects.Contains(zdo))
      return ProcessResult.UnregisterProcessor;

    //if (!_backpacks.TryGetValue(zdo, out var backpack))
    //{
      //_backpacks.Add(zdo, backpack = PlacePiece(zdo.ZDO.GetPosition(), BackpackPrefabHash, zdo.ZDO.GetRotation(), CreatorMarkers.ProcessorOwned));
    //}

    return ProcessResult.UnregisterProcessor;
  }

  void OnEmoteDetected(PlayerState state, Emotes emote)
  {
  }
}
