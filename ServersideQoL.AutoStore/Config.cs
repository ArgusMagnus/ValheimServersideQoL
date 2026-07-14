using BepInEx.Configuration;

namespace ServersideQoL.AutoStore;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
    const string Section = "AutoStore";

    public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
        "Enables/disables the entire mod");

    public ConfigEntry<bool> AutoSort { get; } = BindEx(cfg, Section, false, "True to auto sort container inventories");
    public ConfigEntry<MessageTypes> SortedMessageType { get; } = BindEx(cfg, Section, MessageTypes.None,
        "Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default);
}
