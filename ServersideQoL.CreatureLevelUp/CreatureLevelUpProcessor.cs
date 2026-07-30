namespace ServersideQoL.CreatureLevelUp;

[Processor("5ab42c92-d2fd-4efe-8904-720a46ac7f5b")]
public sealed class CreatureLevelUpProcessor : Processor<CreatureLevelUpProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Character? Character, CreatureSpawner? CreatureSpawner, SpawnArea? SpawnArea) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    return ProcessResult.UnregisterProcessor;
  }
}
