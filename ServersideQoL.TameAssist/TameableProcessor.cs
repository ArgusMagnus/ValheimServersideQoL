using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.TameAssist;

[Processor(Id)]
[RunAfter<TameableRegistryProcessor>]
public sealed class TameableProcessor : Processor<TameableRegistryProcessor.PrefabInfo>
{
  public const string Id = "4386fb2c-2092-4f88-a173-9f01fadcbc6c";

  readonly Dictionary<ServersideQoLZDO, State> _states = [];

  protected override void Initialize()
  {
    _states.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, TameableRegistryProcessor.PrefabInfo prefabInfo)
  {
    ServersideQoLZDO.ComponentFieldAccessor<Tameable>? fields = null;

    if (Instance<TameableRegistryProcessor>().GetState(zdo) is not { } state)
      return ProcessResult.UnregisterProcessor;

    var result = ProcessResult.UnregisterProcessor;

    if (state.State is TameableState.States.Tamed or TameableState.States.Taming && 
        prefabInfo.Humanoid is not { m_faction: Character.Faction.Players or Character.Faction.PlayerSpawned })
    {
      fields ??= zdo.Fields<Tameable>();
      if (Config.Instance.FedDurationMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_fedDuration);
      else if (fields.UpdateValue(static () => x => x.m_fedDuration, prefabInfo.Tameable.m_fedDuration * Config.Instance.FedDurationMultiplier.Value))
        result |= ProcessResult.RecreateZDO;
    }

    if (state.State is TameableState.States.Tamed)
    {
      fields ??= zdo.Fields<Tameable>();

      if (!Config.Instance.MakeCommandable.Value)
        fields.Reset(static () => x => x.m_commandable);
      else if (fields.UpdateValue(static () => x => x.m_commandable, true))
        result |= ProcessResult.RecreateZDO;

      _states.Remove(zdo);
    }
    else if (state.State is TameableState.States.Taming)
    {
      fields ??= zdo.Fields<Tameable>();

      if (Config.Instance.TamingTimeMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_tamingTime);
      else if (fields.UpdateValue(static () => x => x.m_tamingTime, prefabInfo.Tameable.m_tamingTime * Config.Instance.TamingTimeMultiplier.Value))
        result |= ProcessResult.RecreateZDO;

      if (Config.Instance.PotionTamingBoostMultiplier.Value is 1f)
        fields.Reset(static () => x => x.m_tamingBoostMultiplier);
      else if (fields.UpdateValue(static () => x => x.m_tamingBoostMultiplier, prefabInfo.Tameable.m_tamingBoostMultiplier * Config.Instance.PotionTamingBoostMultiplier.Value))
        result |= ProcessResult.RecreateZDO;

      if (Config.Instance.TamingProgressMessageType.Value is not MessageTypes.None)
      {
        result &= ~ProcessResult.UnregisterProcessor;

        if (!_states.TryGetValue(zdo, out var tamingState))
        {
          _states.Add(zdo, tamingState = new());
          zdo.Destroyed += x => _states.Remove(x, out _);
        }

        var now = Timestamp.Now;
        if (tamingState.NextMessage < now)
        {
          tamingState.NextMessage = now.AddSeconds(DamageText.instance.m_textDuration);

          var isHungry = false;
          /// <see cref="Tameable.IsHungry()"/>
          if ((ZNet.instance.GetTime() - zdo.Vars.GetTameLastFeeding()).TotalSeconds > fields.GetFloat(static () => x => x.m_fedDuration))
            isHungry = true;

          ShowMessage(peers, zdo, Config.Instance.Localization.Value.FormatTaming(state.Tameness, isHungry), Config.Instance.TamingProgressMessageType.Value);
        }
      }
    }

    return result;
  }

  public sealed class State
  {
    public Timestamp NextMessage { get; set; }
  }
}
