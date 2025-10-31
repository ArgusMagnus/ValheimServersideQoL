using BepInEx.Configuration;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class SignsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<string> DefaultColor { get; } = cfg.BindEx(section, "",
            "Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)");
        public ConfigEntry<bool> TimeSigns { get; } = cfg.BindEx(section, false,
            Invariant($"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time"));
    }
}