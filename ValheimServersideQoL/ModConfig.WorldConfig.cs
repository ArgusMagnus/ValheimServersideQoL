using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class WorldConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<RemoveMistlandsMistOptions> RemoveMistlandsMist { get; } = cfg.BindEx(section, RemoveMistlandsMistOptions.Never, """
             Condition to remove the mist from the mistlands.
             Beware that there are a few cases of mist (namely mist around POIs like ancient bones/skulls)
             that cannot be removed by this mod and will remain regardless of this setting.
             """, AcceptableEnum<RemoveMistlandsMistOptions>.Default);

        public ConfigEntry<bool> MakeAllItemsFloat { get; } = cfg.BindEx(section, false, """
             True to make all items float.
             Non-floating items will be put in a floating cargo crate if they sink 2m below water level.
             """);

        public enum RemoveMistlandsMistOptions
        {
            Never,
            Always,
            AfterQueenKilled,
            InsideShield
        }
    }
}