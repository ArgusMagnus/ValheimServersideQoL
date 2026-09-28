using BepInEx.Configuration;

namespace ServersideQoL.ControlCenter;

[Processor(Id, ReinitializeOnConfigChanged = false)]
sealed class Processor : Processor<Processor.PrefabInfo>
{
  public const string Id = "bd8fd3f9-3acd-40de-8f8f-3feab6054678";

  protected override void Initialize()
  {
    Config.Instance.ConfigChanged -= OnConfigChanged;
    var sendGlobalKeys = OnConfigChanged(Config.Instance, Config.Instance.Preset, false);
    foreach (var cfg in Config.Instance.WorldModifiers.Keys)
      sendGlobalKeys |= OnConfigChanged(Config.Instance, cfg, false);
    foreach (var cfg in Config.Instance.GlobalKeys)
    {
      if (cfg.BoxedValue is not null)
        sendGlobalKeys |= OnConfigChanged(Config.Instance, cfg, false);
    }
    if (sendGlobalKeys)
      ZoneSystem.instance.SendGlobalKeys(ZRoutedRpc.Everybody);
    Config.Instance.ConfigChanged += OnConfigChanged;
  }

  static void OnConfigChanged(object sender, SettingChangedEventArgs args)
    => OnConfigChanged((Config)sender, args.ChangedSetting, true);

  static bool OnConfigChanged(Config instance, ConfigEntryBase cfg, bool sendGlobalKeys)
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

      case ConfigEntry<bool?> { Value: { } boolCfg }:
        if (boolCfg)
          ZoneSystem.instance.GlobalKeyAdd(cfg.Definition.Key.ToLowerInvariant(), false);
        else
          ZoneSystem.instance.GlobalKeyRemove(cfg.Definition.Key.ToLowerInvariant(), false);
        if (sendGlobalKeys)
          ZoneSystem.instance.SendGlobalKeys(ZRoutedRpc.Everybody);
        return !sendGlobalKeys;

      default:
        if (cfg.BoxedValue is null)
          ZoneSystem.instance.GlobalKeyRemove(cfg.Definition.Key.ToLowerInvariant(), false);
        else
          ZoneSystem.instance.GlobalKeyAdd(Invariant($"{cfg.Definition.Key.ToLowerInvariant()} {cfg.BoxedValue}"), false);
        if (sendGlobalKeys)
          ZoneSystem.instance.SendGlobalKeys(ZRoutedRpc.Everybody);
        return !sendGlobalKeys;
    }

    return false;
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
