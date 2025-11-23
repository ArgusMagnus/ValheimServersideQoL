using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class WorldModifiersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> SetPresetFromConfig { get; } = cfg.BindEx(section, false,
            Invariant($"True to set the world preset according to the '{nameof(Preset)}' config entry"));
        public ConfigEntry<WorldPresets> Preset { get; } = GetPreset(cfg, section);

        public ConfigEntry<bool> SetModifiersFromConfig { get; } = cfg.BindEx(section, false,
            "True to set world modifiers according to the following configuration entries");
        public IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> Modifiers { get; } = GetModifiers(cfg, section);

        static ConfigEntry<WorldPresets> GetPreset(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldPresets)"/>
            var presets = PrivateAccessor.GetServerOptionsGUIPresets();
            return cfg.Bind(section, nameof(Preset), WorldPresets.Default, new ConfigDescription(
                Invariant($"World preset. Enable '{nameof(SetPresetFromConfig)}' for this to have an effect"),
                new AcceptableEnum<WorldPresets>(presets.Select(static x => x.m_preset))));
        }

        static IReadOnlyDictionary<WorldModifiers, ConfigEntry<WorldModifierOption>> GetModifiers(ConfigFile cfg, string section)
        {
            /// <see cref="ServerOptionsGUI.SetPreset(World, WorldModifiers, WorldModifierOption)"/>
            var modifiers = PrivateAccessor.GetServerOptionsGUIModifiers()
                .OfType<KeySlider>()
                .Select(keySlider => (Key: keySlider.m_modifier, Cfg: cfg.Bind(section, Invariant($"{keySlider.m_modifier}"), WorldModifierOption.Default,
                    new ConfigDescription(Invariant($"World modifier '{keySlider.m_modifier}'. Enable '{nameof(SetModifiersFromConfig)}' for this to have an effect"),
                    new AcceptableEnum<WorldModifierOption>(keySlider.m_settings.Select(static x => x.m_modifierValue))))))
                .ToDictionary(static x => x.Key, static x => x.Cfg);
            return modifiers;
        }
    }
}