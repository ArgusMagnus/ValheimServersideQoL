using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace ServersideQoL.DropControl;

[Processor(Id)]
public sealed class CharacterDropAndRagdollProcessor : Processor<CharacterDropAndRagdollProcessor.PrefabInfo>
{
  public const string Id = "4872b51f-6b0d-4f49-94ed-041f6c13fba6";

  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    public CharacterDrop CharacterDrop { get; private init; }
    public Ragdoll? Ragdoll { get; private init; }
    public Config.DropsConfig.DropConfig DropConfig { get; private init; }

    static IReadOnlyDictionary<string, Config.DropsConfig.DropConfig>? __dropsByName;
    static IReadOnlyDictionary<Ragdoll, CharacterDrop>? __characterDropByRagdoll;

    public PrefabInfo(CharacterDrop? characterDrop, Ragdoll? ragdoll)
    {
      if (CharacterDrop is not null)
      {
        if (PrefabInfo.GetComponent<Character>()?.m_deathEffects.m_effectPrefabs
          .Select(static x => x.m_prefab.GetComponent<Ragdoll>())
          .FirstOrDefault(static x => x is not null) is not null)
          characterDrop = null;
      }
      else if (Ragdoll is not null)
      {
        if (__characterDropByRagdoll is null)
          InitializeCharacterDropByRagdoll();
        characterDrop = __characterDropByRagdoll.GetValueOrDefault(Ragdoll);
      }

      CharacterDrop = characterDrop!;
      Ragdoll = ragdoll;
      DropConfig = default!;
      if (characterDrop is null)
        return;

      __dropsByName ??= Config.Instance.Drops.Value.Entries.ToDictionary(static x => x.Name);
      if (!__dropsByName.TryGetValue(characterDrop.gameObject.name, out var cfg))
        return;

      DropConfig = cfg;
      characterDrop.m_drops.Clear();
      foreach (var drop in cfg.Drops)
      {
        if (ZNetScene.instance.GetPrefab(drop.Prefab) is not { } prefab)
        {
          Instance<CharacterDropAndRagdollProcessor>().Logger.LogWarning($"Invalid prefab name: {drop.Prefab}");
          continue;
        }

        characterDrop.m_drops.Add(new()
        {
          m_prefab = prefab,
          m_amountMin = drop.AmountMin,
          m_amountMax = drop.AmountMax,
          m_chance = drop.Chance,
          m_onePerPlayer = drop.OnePerPlayer,
          m_levelMultiplier = drop.LevelMultiplier,
          m_dontScale = drop.DontScale
        });
      }
    }

    public override bool IsValid => CharacterDrop is not null && DropConfig is not null;

    [MemberNotNull(nameof(__characterDropByRagdoll))]
    static void InitializeCharacterDropByRagdoll()
    {
      var dict = new Dictionary<Ragdoll, CharacterDrop>();
      foreach (var prefab in ZNetScene.instance.m_prefabs)
      {
        if (prefab.GetComponent<CharacterDrop>() is not { } characterDrop || prefab.GetComponent<Character>() is not { } character)
          continue;

        if (character.m_deathEffects.m_effectPrefabs
          .Select(static x => x.m_prefab.GetComponent<Ragdoll>())
          .FirstOrDefault(static x => x is not null)
          is not { } ragdoll)
          continue;

        dict.TryAdd(ragdoll, characterDrop);
      }
      __characterDropByRagdoll = dict;
    }
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    var result = ProcessResult.UnregisterProcessor;
    if (prefabInfo.Ragdoll is not null)
    {
      var drops = prefabInfo.CharacterDrop.GenerateDropList();
      /// todo: set zdo vars, <see cref="Ragdoll.Setup"/>
      zdo.ZDO.DataRevision += 100;
    }
    else if (prefabInfo.CharacterDrop is not null)
    {
      var offset = WorldGenerator.worldSize * 2;
      if (zdo.Fields<CharacterDrop>().UpdateValue(static () => x => x.m_spawnOffset, new Vector3(offset, 0, offset)))
        result |= ProcessResult.RecreateZDO;

      var drops = prefabInfo.CharacterDrop.GenerateDropList();
      // todo: manually spawn
    }
    return result;
  }
}
