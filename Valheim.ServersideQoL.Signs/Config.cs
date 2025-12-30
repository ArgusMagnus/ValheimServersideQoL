using BepInEx.Configuration;

namespace Valheim.ServersideQoL.Signs;

sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
}
