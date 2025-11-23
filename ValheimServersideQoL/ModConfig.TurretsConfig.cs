using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class TurretsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> DontTargetPlayers { get; } = cfg.BindEx(section, false,
            "True to stop ballistas from targeting players");
        public ConfigEntry<bool> DontTargetTames { get; } = cfg.BindEx(section, false,
            "True to stop ballistas from targeting tames");
        public ConfigEntry<bool> LoadFromContainers { get; } = cfg.BindEx(section, false,
            "True to automatically load ballistas from containers");
        public ConfigEntry<float> LoadFromContainersRange { get; } = cfg.BindEx(section, 4f,
            "Required proximity of a container to a ballista to be used as ammo source");
        public ConfigEntry<MessageTypes> AmmoAddedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when ammo is added to a turret", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<MessageTypes> NoAmmoMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when there is no ammo to add to a turret", AcceptableEnum<MessageTypes>.Default);
    }
}