using BepInEx.Configuration;

namespace ServersideQoL.WorldOptions;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "WorldOptions";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public ConfigEntry<RemoveMistlandsMistOptions> RemoveMistlandsMist { get; } = BindEx(cfg, Section, RemoveMistlandsMistOptions.AfterQueenKilled, """
    Condition to remove the mist from the mistlands.
    Beware that there are a few cases of mist (namely mist around POIs like ancient bones/skulls)
    that cannot be removed by this mod and will remain regardless of this setting.
    """, AcceptableEnum<RemoveMistlandsMistOptions>.Default);

  public enum RemoveMistlandsMistOptions
  {
    Never,
    Always,
    AfterQueenKilled
  }
}
