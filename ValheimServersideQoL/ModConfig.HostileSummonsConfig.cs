using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class HostileSummonsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> AllowReplacementSummon { get; } = cfg.BindEx(section, false,
            "True to allow the summoning of new hostile summons (such as summoned trolls) to replace older ones when the limit exceeded");
        public ConfigEntry<bool> MakeFriendly { get; } = cfg.BindEx(section, false,
            "True to make all hostile summons (such as summoned trolls) friendly");
        public ConfigEntry<bool> FollowSummoner { get; } = cfg.BindEx(section, false,
            "True to make summoned creatures follow the summoner");
    }
}