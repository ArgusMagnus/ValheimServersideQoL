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
        OnConfigChanged(Config.Instance, cfg);
    }
    Config.Instance.ConfigChanged += OnConfigChanged;
  }

  static void OnConfigChanged(object sender, SettingChangedEventArgs args)
    => OnConfigChanged((Config)sender, args.ChangedSetting);

  static void OnConfigChanged(Config instance, ConfigEntryBase cfg)
  {
    switch (cfg)
    {
      case ConfigEntry<WorldPresets> presetCfg:
        if (Enum.IsDefined(typeof(WorldPresets), presetCfg.Value))
          ServerOptionsGUI.m_instance.SetPreset(ZNet.World, presetCfg.Value);
        break;

      case ConfigEntry<WorldModifierOption> modifierCfg:
        if (Enum.IsDefined(typeof(WorldModifierOption), modifierCfg.Value))
          ServerOptionsGUI.m_instance.SetPreset(ZNet.World, instance.WorldModifiers[modifierCfg], modifierCfg.Value);
        break;

      default:
        if (cfg.DefaultValue.Equals(cfg.BoxedValue))
          ZoneSystem.instance.GlobalKeyRemove(cfg.Definition.Key.ToLowerInvariant(), false);
        else if (cfg.SettingType == typeof(bool))
          ZoneSystem.instance.GlobalKeyAdd(cfg.Definition.Key.ToLowerInvariant(), false);
        else
          ZoneSystem.instance.GlobalKeyAdd(Invariant($"{cfg.Definition.Key.ToLowerInvariant()} {cfg.BoxedValue}"), false);
        ZoneSystem.instance.SendGlobalKeys(ZRoutedRpc.Everybody);
        break;
    }
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
