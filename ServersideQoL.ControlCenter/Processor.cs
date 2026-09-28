using BepInEx.Configuration;

namespace ServersideQoL.ControlCenter;

[Processor(Id, ReinitializeOnConfigChanged = false)]
sealed class Processor : Processor<Processor.PrefabInfo>
{
  public const string Id = "bd8fd3f9-3acd-40de-8f8f-3feab6054678";

  protected override void Initialize()
  {
    Config.Instance.ConfigChanged -= OnConfigChanged;
    foreach (var cfg in Config.Instance.GlobalKeys)
    {
      if (!cfg.DefaultValue.Equals(cfg.BoxedValue))
        OnConfigChanged(cfg);
    }
    Config.Instance.ConfigChanged += OnConfigChanged;
  }

  void OnConfigChanged(object sender, SettingChangedEventArgs args)
    => OnConfigChanged(args.ChangedSetting);

  void OnConfigChanged(ConfigEntryBase cfg)
  {
    if (cfg.DefaultValue.Equals(cfg.BoxedValue))
      ZoneSystem.instance.GlobalKeyRemove(cfg.Definition.Key.ToLowerInvariant(), false);
    else if (cfg.SettingType == typeof(bool))
      ZoneSystem.instance.GlobalKeyAdd(cfg.Definition.Key.ToLowerInvariant(), false);
    else
      ZoneSystem.instance.GlobalKeyAdd(Invariant($"{cfg.Definition.Key.ToLowerInvariant()} {cfg.BoxedValue}"), false);
    ZoneSystem.instance.SendGlobalKeys(ZRoutedRpc.Everybody);
  }

  public sealed record PrefabInfo : ProcessorPrefabInfo
  {
    public override bool IsValid => false;
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    throw new NotSupportedException();
  }
}
