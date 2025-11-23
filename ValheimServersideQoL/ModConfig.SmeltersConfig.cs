using BepInEx.Configuration;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class SmeltersConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> FeedFromContainers { get; } = cfg.BindEx(section, false,
            "True to automatically feed smelters from nearby containers");
        public ConfigEntry<float> FeedFromContainersRange { get; } = cfg.BindEx(section, 4f, $"""
             Required proximity of a container to a smelter to be used as feeding source.
             Can be overridden per chest by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign.
             """);
        public ConfigEntry<int> FeedFromContainersMaxRange { get; } = cfg.BindEx(section, (int)ZoneSystem.c_ZoneSize,
            $"Max feeding range players can set per chest (by putting '{SignProcessor.LeftRightArrowEmoji}<Range>' on a chest sign)");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastFuel { get; } = cfg.BindEx(section, 1,
            "Minimum amount of fuel to leave in a container");
        public ConfigEntry<int> FeedFromContainersLeaveAtLeastOre { get; } = cfg.BindEx(section, 1,
            "Minimum amount of ore to leave in a container");
        public ConfigEntry<MessageTypes> OreOrFuelAddedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when ore or fuel is added to a smelter", AcceptableEnum<MessageTypes>.Default);
        public ConfigEntry<float> CapacityMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply a smelter's ore/fuel capacity by this factor");
        public ConfigEntry<float> TimePerProductMultiplier { get; } = cfg.BindEx(section, 1f,
            "Multiply the time it takes to produce one product by this factor (will not go below 1 second per product).");
    }
}