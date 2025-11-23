using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class DoorsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<float> AutoCloseMinPlayerDistance { get; } = cfg.BindEx(section, float.NaN,
        Invariant($"Min distance all players must have to the door before it is closed. {float.NaN} to disable this feature"));
    }
}