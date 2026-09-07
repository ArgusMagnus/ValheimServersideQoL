using ServersideQoL.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace ServersideQoL.DropControl;

[Processor(Id)]
public sealed class CharacterDropAndRagdollProcessor : Processor<CharacterDropAndRagdollProcessor.PrefabInfo>
{
  public const string Id = "4872b51f-6b0d-4f49-94ed-041f6c13fba6";

  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    public ItemDrop? ItemDrop { get; }

    [MemberNotNullWhen(true, nameof(CharacterDrop))]
    public bool IsCharacterDrop { get; }
    public CharacterDrop? CharacterDrop { get; private set; }

    [MemberNotNullWhen(true, nameof(Ragdoll), nameof(CharacterDrop))]
    public bool IsRagdoll { get; }
    public Ragdoll? Ragdoll { get; private set; }

    public Config.DropsConfig.DropConfig DropConfig { get; private set; } = default!;
    public bool HasQualityIncreaseChance { get; private set; }
    public IReadOnlyDictionary<int, CharacterDrop.Drop> OriginalDropsByHash { get; private set; } = default!;
    public bool HasRagdoll { get; private set; }

    static IReadOnlyDictionary<string, Config.DropsConfig.DropConfig> DropsByName
      => field ??= Config.Instance.Drops.Value.Entries.Where(static x => x.Enabled).ToDictionary(static x => x.Name);
    static IReadOnlyDictionary<Ragdoll, CharacterDrop> CharacterDropByRagdoll
      => field ??= GetCharacterDropByRagdoll();

    public PrefabInfo(ItemDrop? itemDrop, CharacterDrop? characterDrop, Ragdoll? ragdoll)
    {
      ItemDrop = itemDrop;
      CharacterDrop = characterDrop;
      IsCharacterDrop = characterDrop is not null;
      Ragdoll = ragdoll;
      IsRagdoll = ragdoll is not null;
    }

    public override bool IsValid
    {
      get
      {
        if (ItemDrop is not null)
          return true;

        if (CharacterDrop is not null)
        {
          Ragdoll = PrefabInfo.GetComponent<Character>()?.m_deathEffects.m_effectPrefabs
            .Select(static x => x.m_prefab.GetComponent<Ragdoll>())
            .FirstOrDefault(static x => x is not null);
        }
        else if (Ragdoll is not null)
        {
          CharacterDrop = CharacterDropByRagdoll.GetValueOrDefault(Ragdoll);
        }

        DropConfig = default!;
        if (CharacterDrop is null || !DropsByName.TryGetValue(CharacterDrop.gameObject.name, out var cfg))
          return false;

        DropConfig = cfg;
        OriginalDropsByHash = CharacterDrop.m_drops
          .Where(static x => x.m_levelMultiplier && !x.m_onePerPlayer)
          .ToDictionary(static x => ZNetScene.instance.GetPrefabHash(x.m_prefab));
        CharacterDrop.m_drops.Clear();
        foreach (var drop in cfg.Drops)
        {
          if (ZNetScene.instance.GetPrefab(drop.Prefab) is not { } prefab)
          {
            Instance<CharacterDropAndRagdollProcessor>().Logger.LogWarning($"Invalid prefab name: {drop.Prefab}");
            continue;
          }

          CharacterDrop.m_drops.Add(new()
          {
            m_prefab = prefab,
            m_amountMin = drop.AmountMin,
            m_amountMax = drop.AmountMax,
            m_chance = drop.Chance,
            m_onePerPlayer = drop.OnePerPlayer,
            m_levelMultiplier = drop.DoubleAmountAndChancePerLevel,
            m_dontScale = drop.IgnoreWorldResourceRate
          });

          if (!HasQualityIncreaseChance && drop is { QualityIncreaseChance: > 0 } and ({ MaxQuality: > 1 } or { MultiplyMaxQualityByLevel: true}))
            HasQualityIncreaseChance = true;
        }
        return true;
      }
    }

    static IReadOnlyDictionary<Ragdoll, CharacterDrop> GetCharacterDropByRagdoll()
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

        if (DropsByName.ContainsKey(characterDrop.gameObject.name))
          dict[ragdoll] = characterDrop;
        else
          dict.TryAdd(ragdoll, characterDrop);
      }
      return dict;
    }
  }

  float _dropArea;
  Vector3 _ragdollDropOffset;
  SectorDictionary<Ragdoll, List<(CharacterDrop, int)>> _characterDropsByRagdoll = new(1);

  protected override void Initialize()
  {
    _dropArea = (float)typeof(CharacterDrop).GetField("m_dropArea", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetRawConstantValue();
    _ragdollDropOffset = Vector3.up * (float)typeof(Ragdoll).GetField("m_dropOffset", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).GetRawConstantValue();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    const float DestroySpawnHeight = 100_000;
    const float DestroyHeight = DestroySpawnHeight - 10_000;

    var result = ProcessResult.UnregisterProcessor;

    if (prefabInfo.ItemDrop is not null)
    {
      if (zdo.ZDO.GetPosition().y > DestroyHeight)
        result = ProcessResult.DestroyZDO;
    }
    else if (prefabInfo.IsCharacterDrop)
    {
      if (prefabInfo.Ragdoll is not null)
        zdo.Destroyed += OnCharacterDropWithRagdollDestroyed;
      else
      {
        if (zdo.Fields<CharacterDrop>().UpdateValue(static () => x => x.m_spawnOffset, new Vector3(0, DestroySpawnHeight, 0)))
          result |= ProcessResult.RecreateZDO;

        zdo.Destroyed += OnCharacterDropDestroyed;
      }
    }
    else if (prefabInfo.IsRagdoll)
    {
      var characterDrop = prefabInfo.CharacterDrop;
      var level = 1;
      if (_characterDropsByRagdoll.TryPop((zdo.ZDO.GetPosition(), prefabInfo.Ragdoll), true, out var characterDropAndLevel))
        (characterDrop, level) = characterDropAndLevel;
      else
      {
        Logger.DevLog($"CharacterDrop for Ragdoll not found: {prefabInfo.PrefabInfo.PrefabName}");
        var minLevelMultiplier = 0;
        var maxLevelMultiplierSaturated = int.MaxValue;
        var maxLevelMultiplierUnsaturated = int.MaxValue;
        for (var i = 0; i < zdo.Vars.GetDrops(); i++)
        {
          var hash = zdo.Vars.GetDropHash(i);
          if (!prefabInfo.OriginalDropsByHash.TryGetValue(hash, out var drop))
            continue;
          var amount = zdo.Vars.GetDropAmount(i);
          // minAmount*levelMultiplier <= amount <= maxAmount*levelMultiplier
          if (drop.m_dontScale || Game.m_resourceRate is 1)
          {
            minLevelMultiplier = Math.Max(minLevelMultiplier, amount / drop.m_amountMax);
            maxLevelMultiplierSaturated = Math.Min(maxLevelMultiplierSaturated, amount / drop.m_amountMin);
          }
          else
          {
            var minAmount = Mathf.Ceil((amount - 0.5f) / Game.m_resourceRate);
            var maxAmount = Mathf.Floor((amount + 0.5f) / Game.m_resourceRate);
            minLevelMultiplier = Math.Max(minLevelMultiplier, Mathf.CeilToInt(minAmount / drop.m_amountMax));
            maxLevelMultiplierSaturated = Math.Min(maxLevelMultiplierSaturated, Mathf.FloorToInt(maxAmount / drop.m_amountMin));
          }

          if (amount < 100)
            maxLevelMultiplierUnsaturated = Math.Min(maxLevelMultiplierUnsaturated, maxLevelMultiplierSaturated);

          if (minLevelMultiplier >= maxLevelMultiplierUnsaturated)
            break;
        }

        if (maxLevelMultiplierUnsaturated is int.MaxValue)
        {
          maxLevelMultiplierUnsaturated = maxLevelMultiplierSaturated;
          Logger.DevLog($"Ragdoll drop amounts exceeded 100, level multiplier could not be determined exactly (min: {minLevelMultiplier}, max: {maxLevelMultiplierUnsaturated}): {prefabInfo.PrefabInfo.PrefabName}");
        }

        if (minLevelMultiplier <= 0)
          Logger.DevLog($"Ragdoll drop level multiplier could not be determined (min: {minLevelMultiplier}, max: {maxLevelMultiplierUnsaturated}): {prefabInfo.PrefabInfo.PrefabName}");
        else
          level = (int)Mathf.Log(minLevelMultiplier, 2) + 1;
      }

      if (prefabInfo.HasQualityIncreaseChance)
      {
        if (level > 1)
          zdo.Vars.SetLevel(level);
        zdo.Vars.SetDrops(0);
        zdo.Destroyed += OnCharacterDropDestroyed;
      }
      else
      {
        var drops = GenerateDropList(characterDrop, level);
        /// <see cref="Ragdoll.Setup"/>
        zdo.Vars.SetDrops(drops.Count);
        for (int i = 0; i < drops.Count; i++)
        {
          var (prefab, amount) = drops[i];
          int prefabHash = ZNetScene.instance.GetPrefabHash(prefab);
          zdo.Vars.SetDropHash(i, prefabHash);
          zdo.Vars.SetDropAmount(i, amount);
        }
      }
      zdo.ZDO.DataRevision += 100;
    }

    return result;
  }

  void OnCharacterDropWithRagdollDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo) is not { CharacterDrop: { } characterDrop, Ragdoll: { } ragdoll })
      return;
    _characterDropsByRagdoll.Add((zdo.ZDO.GetPosition(), ragdoll), (characterDrop, zdo.Vars.GetLevel()));
  }

  static List<KeyValuePair<GameObject,int>> GenerateDropList(CharacterDrop characterDrop, int level)
  {
    var character = characterDrop.GetComponent<Character>();
    var lvlBkp = character.GetLevel();
    character.SetLevel(level);
    try
    {
      return characterDrop.GenerateDropList();
    }
    finally
    {
      character.SetLevel(lvlBkp);
    }
  }

  void OnCharacterDropDestroyed(ServersideQoLZDO zdo)
  {
    if (GetProcessorPrefabInfo(zdo) is not { CharacterDrop: not null } prefabInfo)
      return;
    if (prefabInfo.Ragdoll is null && zdo.Vars.GetHealth(1) > 0)
      return;

    var offset = prefabInfo.Ragdoll is not null ? _ragdollDropOffset : prefabInfo.CharacterDrop.m_spawnOffset;

    if (prefabInfo.HasQualityIncreaseChance)
      SpawnDrops(zdo, prefabInfo.CharacterDrop, prefabInfo.DropConfig, offset, _dropArea);
    else
    {
      var drops = GenerateDropList(prefabInfo.CharacterDrop, zdo.Vars.GetLevel());
      /// <see cref="CharacterDrop.OnDeath"/>
      CharacterDrop.DropItems(drops, zdo.ZDO.GetPosition() + offset, _dropArea);
    }

    static void SpawnDrops(ServersideQoLZDO zdo, CharacterDrop characterDrop, Config.DropsConfig.DropConfig dropConfig, Vector3 offset, float dropArea)
    {
      /// <see cref="CharacterDrop.GenerateDropList"/>

      var level = zdo.Vars.GetLevel();
      int levelMultiplier = Mathf.Max(1, (int)Mathf.Pow(2f, level - 1));
      for (var i = 0; i < characterDrop.m_drops.Count; i++)
      {
        var drop = characterDrop.m_drops[i];
        if (drop.m_prefab == null)
          continue;

        float chance = drop.m_chance;
        if (drop.m_levelMultiplier)
          chance *= levelMultiplier;

        if (UnityEngine.Random.value > chance)
          continue;

        int amount = (drop.m_dontScale ? UnityEngine.Random.Range(drop.m_amountMin, drop.m_amountMax) : Game.instance.ScaleDrops(drop.m_prefab, drop.m_amountMin, drop.m_amountMax));
        if (drop.m_levelMultiplier)
          amount *= levelMultiplier;

        if (drop.m_onePerPlayer)
          amount = ZNet.instance.GetNrOfPlayers();

        if (amount > 100)
          amount = 100;

        if (amount > 0)
          SpawnDrop(drop, dropConfig.Drops[i], amount, level, levelMultiplier, zdo.ZDO.GetPosition() + offset, dropArea);
      }
    }

    static void SpawnDrop(CharacterDrop.Drop drop, Config.DropsConfig.DropConfig.Drop cfg, int amount, int level, float levelMultiplier, Vector3 centerPos, float dropArea)
    {
      /// <see cref="CharacterDrop.DropItems"/>

      for (int i = 0; i < amount; i++)
      {
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
        Vector3 vector = UnityEngine.Random.insideUnitSphere * dropArea;
        GameObject gameObject = UnityEngine.Object.Instantiate(drop.m_prefab, centerPos + vector, rotation);
        if (gameObject.GetComponent<ItemDrop>() is { } itemDrop)
        {
          itemDrop.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
          var chance = cfg.QualityIncreaseChance;
          if (cfg.DoubleQualityIncreaseChancePerLevel)
            chance *= levelMultiplier;

          var maxQuality = cfg.MaxQuality;
          if (cfg.MultiplyMaxQualityByLevel && level > 1)
          {
            maxQuality *= level;
            // keep max quality chance the same
            if (cfg.MaxQuality > 1)
              chance = Mathf.Pow(chance, cfg.MaxQuality - 1f);
            chance = Mathf.Pow(chance, 1f / (maxQuality - 1f));
          }

          var quality = 0;
          while (++quality < maxQuality && UnityEngine.Random.value <= chance) ;
          if (quality > 1)
          {
            itemDrop.SetQuality(quality);
            ItemDrop.SaveToZDO(itemDrop.m_itemData, itemDrop.GetComponent<ZNetView>().GetZDO());
          }
        }

        Rigidbody component2 = gameObject.GetComponent<Rigidbody>();
        if ((bool)component2)
        {
          Vector3 insideUnitSphere = UnityEngine.Random.insideUnitSphere;
          if (insideUnitSphere.y < 0f)
          {
            insideUnitSphere.y = 0f - insideUnitSphere.y;
          }

          component2.AddForce(insideUnitSphere * 5f, ForceMode.VelocityChange);
        }
      }
    }
  }
}
