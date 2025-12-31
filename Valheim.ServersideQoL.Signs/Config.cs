using BepInEx.Configuration;

namespace Valheim.ServersideQoL.Signs;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
    const string Section = "Signs";

    public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
        "Enables/disables the entire mod");
    public ConfigEntry<string> DefaultColor { get; } = BindEx(cfg, Section, "",
        "Default color for signs. Can be a color name or hex code (e.g. #FF0000 for red)");
    public ConfigEntry<bool> TimeSigns { get; } = BindEx(cfg, Section, false,
        Invariant($"True to update sign texts which contain time emojis (any of {string.Concat(SignProcessor.ClockEmojis)}) with the in-game time"));
}
