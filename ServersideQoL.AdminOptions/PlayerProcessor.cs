using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ServersideQoL.AdminOptions;

[Processor("fff17191-3ccc-4b56-9aa0-03e1c38b4175")]
[RunAfter<PlayerRegistryProcessor>]
public sealed class PlayerProcessor : Processor<PlayerProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(Player? Player, CamShaker? CamShaker) : ProcessorPrefabInfo
  {
    static readonly int __mudRoadPrefab = "vfx_Place_mud_road".GetStableHashCode();
    public override bool IsValid => Player is not null || PrefabInfo.PrefabHash == __mudRoadPrefab;
  }

  readonly Dictionary<ServersideQoLZDO, State> _states = [];
  readonly int _numberOfLevelGroundModes = Enum.GetValues(typeof(LevelGroundModes)).Length;

  ZoneSystem.ZoneLocation DevGround1 => field ??= GetZoneLocation();
  ZoneSystem.ZoneLocation DevGround2 => field ??= GetZoneLocation();

  static ZoneSystem.ZoneLocation GetZoneLocation([CallerMemberName] string name = default!)
      => ZoneSystem.instance.GetLocationsByHash()[name.GetStableHashCode()];

  public BuildModifiers GetBuildModifiers(PlayerID playerId)
    => Instance<PlayerRegistryProcessor>().GetStateForPlayerID(playerId) is { } playerState && _states.TryGetValue(playerState.ZDO, out var state) ? state.BuildModifiers : BuildModifiers.None;

  protected override void Initialize()
  {
    Instance<PlayerRegistryProcessor>().EmoteDetected -= OnEmoteDetected;
    Instance<PlayerRegistryProcessor>().EmoteDetected += OnEmoteDetected;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (prefabInfo.Player is not null)
    {
      if (!_states.TryGetValue(zdo, out var state))
      {
        if (Instance<PlayerRegistryProcessor>().GetState(zdo) is not { IsAdmin: true })
          return ProcessResult.UnregisterProcessor;
        return default;
      }

      var now = Timestamp.Now;

      if (state.NextBuildModifierMessage == default || (
          state.BuildModifiers != default && state.NextBuildModifierMessage < now && zdo.Vars.GetRightItem() == Prefabs.Hammer))
      {
        state.NextBuildModifierMessage = now.AddSeconds(4);
        RPC.ShowMessage(state.PlayerState.Owner, MessageHud.MessageType.TopLeft, $"Build modifiers: {state.BuildModifiers}");
      }

      if (state.NextLevelGroundModeMessage == default || (
          state.LevelGroundMode != default && state.NextLevelGroundModeMessage < now && zdo.Vars.GetRightItem() == Prefabs.Hoe))
      {
        state.NextLevelGroundModeMessage = now.AddSeconds(4);
        RPC.ShowMessage(state.PlayerState.Owner, MessageHud.MessageType.TopLeft, $"Level ground mode: {state.LevelGroundMode}");
      }
    }
    else
    {
      if (Config.Instance.CycleLevelGroundMode.Value is ConfigBase.DisabledEmote)
        return ProcessResult.UnregisterProcessor;

      if (GetState(zdo.ZDO.GetOwner()) is not { } state)
        return ProcessResult.UnregisterProcessor;

      if (state.LevelGroundMode is LevelGroundModes.Reset )
      {
        var zdos = new List<ZDO>();
        ZDOMan.instance.FindSectorObjects(zdo.ZDO.GetSector(), ZoneSystem.instance.ActiveArea, 0, zdos);
        foreach (var zdo2 in zdos.Select(static x => x.ServersideQoLZDO))
        {
          var prefabInfo2 = GetPrefabInfo(zdo2);
          if (prefabInfo2.HasComponent<LocationProxy>())
          {
            var hash = zdo2.Vars.GetLocation();
            _ = Remove(zdo2, hash, zdo.ZDO.GetPosition(), DevGround1) || Remove(zdo2, hash, zdo.ZDO.GetPosition(), DevGround2);

            static bool Remove(ServersideQoLZDO zdo, int hash, Vector3 pos, ZoneSystem.ZoneLocation location)
            {
              if (hash != location.Hash || Utils.DistanceXZ(pos, zdo.ZDO.GetPosition()) > location.m_exteriorRadius)
                return false;
              zdo.Destroy();
              return true;
            }
          }
          else if (prefabInfo2.HasComponent<TerrainComp>() && TerrainCompData.Load(zdo2) is { } terrainComp)
          {
            terrainComp.ResetTerrain(zdo.ZDO.GetPosition(), Config.Instance.Advanced.Value.ResetTerrainRadius);
            if (terrainComp.HasModifications is false)
              zdo2.Destroy();
          }
        }
      }
      else if (state.LevelGroundMode switch
      {
        LevelGroundModes.FlattenMedium => DevGround1,
        LevelGroundModes.FlattenLarge => DevGround2,
        _ => default
      } is { } location)
      {
        /// <see cref="ZoneSystem.instance.TestSpawnLocation"/>
        ZoneSystem.instance.SpawnLocation(location, 0, zdo.ZDO.GetPosition(), zdo.ZDO.GetRotation(), ZoneSystem.SpawnMode.Full);
        var zdos = new List<ZDO>();
        ZDOMan.instance.FindSectorObjects(zdo.ZDO.GetSector(), ZoneSystem.instance.ActiveArea, 0, zdos);
        foreach (var zdo2 in zdos.Select(static x => x.ServersideQoLZDO))
        {
          if (GetPrefabInfo(zdo2).HasComponent<TerrainComp>() && TerrainCompData.Load(zdo2) is { } terrainComp)
          {
            terrainComp.ResetTerrain(zdo.ZDO.GetPosition(), location.m_exteriorRadius);
            if (terrainComp.HasModifications is false)
              zdo2.Destroy();
          }
        }
      }
    }

    return default;
  }

  void OnEmoteDetected(PlayerState playerState, Emotes emote)
  {
    if (!playerState.IsAdmin)
      return;

    static void UpdateGlobalKeyModification(State state, BuildModifiers modifier, GlobalKeys key)
    {
      if ((state.BuildModifiers & modifier) is 0)
        state.PlayerState.RemoveGlobalKeyModification(new(key));
      else
        state.PlayerState.AddGlobalKeyModification(new(key), true);
    }

    static bool CheckEmote(Emotes emote, Emotes cfg)
      => cfg is ConfigBase.AnyEmote || cfg == emote;

    if (!_states.TryGetValue(playerState.ZDO, out var state))
    {
      _states.Add(playerState.ZDO, state = new(playerState));
      playerState.ZDO.Destroyed += x => _states.Remove(x);
    }

    if (CheckEmote(emote, Config.Instance.ToggleDisableRainDamageEmote.Value))
    {
      state.BuildModifiers ^= BuildModifiers.DisableRainDamage;
      state.NextBuildModifierMessage = default;
    }
    if (CheckEmote(emote, Config.Instance.ToggleDisableSupportRequirements.Value))
    {
      state.BuildModifiers ^= BuildModifiers.DisableSupportRequirements;
      state.NextBuildModifierMessage = default;
    }
    if (CheckEmote(emote, Config.Instance.ToggleMakeIndestructible.Value))
    {
      state.BuildModifiers ^= BuildModifiers.MakeIndestructible;
      state.NextBuildModifierMessage = default;
    }
    if (CheckEmote(emote, Config.Instance.ToggleNoWorkbench.Value))
    {
      state.BuildModifiers ^= BuildModifiers.NoWorkbench;
      state.NextBuildModifierMessage = default;
      UpdateGlobalKeyModification(state, BuildModifiers.NoWorkbench, GlobalKeys.NoWorkbench);
    }
    if (CheckEmote(emote, Config.Instance.ToggleDungeonBuild.Value))
    {
      state.BuildModifiers ^= BuildModifiers.DungeonBuild;
      state.NextBuildModifierMessage = default;
      UpdateGlobalKeyModification(state, BuildModifiers.DungeonBuild, GlobalKeys.DungeonBuild);
    }
    if (CheckEmote(emote, Config.Instance.ToggleNoBuildCost.Value))
    {
      state.BuildModifiers ^= BuildModifiers.NoBuildCost;
      state.NextBuildModifierMessage = default;
      UpdateGlobalKeyModification(state, BuildModifiers.NoBuildCost, GlobalKeys.NoBuildCost);
    }
    if (CheckEmote(emote, Config.Instance.ToggleAllPiecesUnlocked.Value))
    {
      state.BuildModifiers ^= BuildModifiers.AllPiecesUnlocked;
      state.NextBuildModifierMessage = default;
      UpdateGlobalKeyModification(state, BuildModifiers.AllPiecesUnlocked, GlobalKeys.AllPiecesUnlocked);
    }
    if (CheckEmote(emote, Config.Instance.CycleLevelGroundMode.Value))
    {
      state.LevelGroundMode = (LevelGroundModes)(((int)state.LevelGroundMode + 1) % _numberOfLevelGroundModes);
      state.NextLevelGroundModeMessage = default;
    }

    IEnumerable<string> messageParts = [];
    if (CheckEmote(emote, Config.Instance.DemigodMode.Value))
    {
      state.DemigodMode = !state.DemigodMode;
      if (state.DemigodMode)
        state.PlayerState.AddGlobalKeyModification(new(GlobalKeys.EnemyDamage), 0);
      else
        state.PlayerState.RemoveGlobalKeyModification(new(GlobalKeys.EnemyDamage));
      messageParts = messageParts.Append($"Demigod mode: {state.DemigodMode}");
    }
    if (CheckEmote(emote, Config.Instance.InfiniteStamina.Value))
    { 
      state.InfiniteStamina = !state.InfiniteStamina;
      if (state.InfiniteStamina)
        state.PlayerState.AddGlobalKeyModification(new(GlobalKeys.StaminaRate), 0);
      else
        state.PlayerState.RemoveGlobalKeyModification(new(GlobalKeys.StaminaRate));
      messageParts = messageParts.Append($"Infnite stamina: {state.InfiniteStamina}");
    }

    if (string.Join(", ", messageParts) is { Length: > 0 } message)
      RPC.ShowMessage(state.PlayerState.Owner, MessageHud.MessageType.TopLeft, message);
  }

  State? GetState(long peerID)
    => Instance<PlayerRegistryProcessor>().GetStateForPeerID(peerID) is { } playerState && _states.TryGetValue(playerState.ZDO, out var state) ? state : null;

  [Flags]
  public enum BuildModifiers : uint
  {
    None = 0,
    DisableRainDamage = 1 << 0,
    DisableSupportRequirements = 1 << 1,
    MakeIndestructible = 1 << 2,
    NoWorkbench = 1 << 3,
    DungeonBuild = 1 << 4,
    NoBuildCost = 1 << 5,
    AllPiecesUnlocked = 1 << 6
  }

  public enum LevelGroundModes
  {
    Default,
    FlattenMedium,
    FlattenLarge,
    Reset
  }

  sealed class State(PlayerState playerState)
  {
    public PlayerState PlayerState { get; } = playerState;
    public BuildModifiers BuildModifiers { get; set; }
    public LevelGroundModes LevelGroundMode { get; set; }
    public bool DemigodMode { get; set; }
    public bool InfiniteStamina { get; set; }
    public Timestamp NextBuildModifierMessage { get; set; } = Timestamp.Now.AddSeconds(float.PositiveInfinity);
    public Timestamp NextLevelGroundModeMessage { get; set; } = Timestamp.Now.AddSeconds(float.PositiveInfinity);
  }

  sealed class TerrainCompData
  {
    const int TerrainCompVersion = 1;
    readonly ServersideQoLZDO _zdo;
    readonly Heightmap _hmap;
    bool[]? _modifiedHeight;
    float[] _levelDelta = default!;
    float[] _smoothDelta = default!;
    bool[] _modifiedPaint = default!;
    Color[] _paintMask = default!;
    int _operations;
    Vector3 _lastOpPoint;
    float _lastOpRadius;

    public bool? HasModifications { get; private set; }

    public static TerrainCompData? Load(ServersideQoLZDO zdo)
    {
      zdo.AssertIs<TerrainComp>();
      if (GetHeightmap(zdo.ZDO.GetPosition()) is not { } hmap)
      {
        AdminOptionsPlugin.Logger.LogWarning($"Heightmap not found at {zdo.ZDO.GetPosition()}");
        return null;
      }
      return new(zdo, hmap);
    }

    TerrainCompData(ServersideQoLZDO zdo, Heightmap hmap)
    {
      _zdo = zdo;
      _hmap = hmap;
    }

    [MemberNotNullWhen(true, nameof(_modifiedHeight))]
    bool Load()
    {
      if (_modifiedHeight is not null)
        return true;

      /// <see cref="TerrainComp.Load"/>
      byte[] byteArray = _zdo.ZDO.GetByteArray(ZDOVars.s_TCData);
      if (byteArray == null)
        return false;

      var expectedLength = _hmap.m_width + 1;
      expectedLength *= expectedLength;

      ZPackage zPackage = new ZPackage(Utils.Decompress(byteArray));
      if (zPackage.ReadInt() is not TerrainCompVersion)
      {
        AdminOptionsPlugin.Logger.LogWarning("Terrain data load error, version missmatch");
        return false;
      }
      _operations = zPackage.ReadInt();
      _lastOpPoint = zPackage.ReadVector3();
      _lastOpRadius = zPackage.ReadSingle();
      int num = zPackage.ReadInt();
      if (num != expectedLength)
      {
        AdminOptionsPlugin.Logger.LogWarning("Terrain data load error, height array missmatch");
        return false;
      }

      _modifiedHeight = new bool[expectedLength];
      _levelDelta = new float[expectedLength];
      _smoothDelta = new float[expectedLength];
      _modifiedPaint = new bool[expectedLength];
      _paintMask = new Color[expectedLength];
      HasModifications = false;

      for (int i = 0; i < num; i++)
      {
        _modifiedHeight[i] = zPackage.ReadBool();
        if (_modifiedHeight[i])
        {
          _levelDelta[i] = zPackage.ReadSingle();
          _smoothDelta[i] = zPackage.ReadSingle();
          HasModifications = true;
        }
        else
        {
          _levelDelta[i] = 0f;
          _smoothDelta[i] = 0f;
        }
      }

      int num2 = zPackage.ReadInt();
      for (int j = 0; j < num2; j++)
      {
        _modifiedPaint[j] = zPackage.ReadBool();
        if (_modifiedPaint[j])
        {
          var color = new Color
          {
            r = zPackage.ReadSingle(),
            g = zPackage.ReadSingle(),
            b = zPackage.ReadSingle(),
            a = zPackage.ReadSingle()
          };
          _paintMask[j] = color;
          HasModifications = true;
        }
        else
        {
          _paintMask[j] = Color.black;
        }
      }

      if (num2 == _hmap.m_width * _hmap.m_width)
      {
        Color[] array = new Color[_paintMask.Length];
        _paintMask.CopyTo(array, 0);
        bool[] array2 = new bool[_modifiedPaint.Length];
        _modifiedPaint.CopyTo(array2, 0);
        int num3 = _hmap.m_width + 1;
        for (int k = 0; k < _paintMask.Length; k++)
        {
          int num4 = k / num3;
          int num5 = (k + 1) / num3;
          int num6 = k - num4;
          if (num4 == _hmap.m_width)
          {
            num6 -= _hmap.m_width;
          }

          if (k > 0 && (k - num4) % _hmap.m_width == 0 && (k + 1 - num5) % _hmap.m_width == 0)
          {
            num6--;
          }

          _paintMask[k] = array[num6];
          _modifiedPaint[k] = array2[num6];
        }
      }

      return true;
    }

    void Save()
    {
      if (_modifiedHeight is null)
        return;

      HasModifications = false;

      ZPackage zPackage = new();
      zPackage.Write(TerrainCompVersion);
      zPackage.Write(_operations);
      zPackage.Write(_lastOpPoint);
      zPackage.Write(_lastOpRadius);
      zPackage.Write(_modifiedHeight.Length);
      for (int i = 0; i < _modifiedHeight.Length; i++)
      {
        zPackage.Write(_modifiedHeight[i]);
        if (_modifiedHeight[i])
        {
          zPackage.Write(_levelDelta[i]);
          zPackage.Write(_smoothDelta[i]);
          HasModifications = true;
        }
      }

      zPackage.Write(_modifiedPaint.Length);
      for (int j = 0; j < _modifiedPaint.Length; j++)
      {
        zPackage.Write(_modifiedPaint[j]);
        if (_modifiedPaint[j])
        {
          zPackage.Write(_paintMask[j].r);
          zPackage.Write(_paintMask[j].g);
          zPackage.Write(_paintMask[j].b);
          zPackage.Write(_paintMask[j].a);
          HasModifications = true;
        }
      }

      byte[] bytes = Utils.Compress(zPackage.GetArray());
      _zdo.ZDO.Set(ZDOVars.s_TCData, bytes);
    }

    public void ResetTerrain(Vector3 pos, float radius)
    {
      _hmap.WorldToVertex(pos, out var x, out var y);
      float b = pos.y - _zdo.ZDO.GetPosition().y;
      float num = radius / _hmap.m_scale;
      int num2 = Mathf.CeilToInt(num);
      Vector2 a = new Vector2(x, y);
      int num3 = _hmap.m_width + 1;

      var save = false;
      for (int i = y - num2; i <= y + num2; i++)
      {
        for (int j = x - num2; j <= x + num2; j++)
        {
          float num4 = Vector2.Distance(a, new Vector2(j, i));
          if (!(num4 > num) && j >= 0 && i >= 0 && j < num3 && i < num3)
          {
            if (!Load())
              return;

            int num7 = i * num3 + j;
            _modifiedHeight[num7] = false;
            _smoothDelta[num7] = 0;
            _levelDelta[num7] = 0;
            _modifiedPaint[num7] = false;
            _paintMask[num7] = Color.black;
            save = true;
          }
        }
      }

      if (save)
        Save();
    }
  }
}
