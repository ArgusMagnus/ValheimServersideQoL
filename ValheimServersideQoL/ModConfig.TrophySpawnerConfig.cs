using BepInEx.Configuration;
using UnityEngine;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class TrophySpawnerConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, "True to make dropped trophies attract mobs.");

        public ConfigEntry<int> ActivationDelay { get; } = cfg.BindEx(section, 3600, "Time in seconds before trophies start attracting mobs");
        public ConfigEntry<int> RespawnDelay { get; } = cfg.BindEx(section, 12, "Respawn delay in seconds");
        static float MaxDistance => Mathf.Round(Mathf.Sqrt(2) * ZoneSystem.instance.m_activeArea * ZoneSystem.c_ZoneSize);
        public ConfigEntry<float> MinSpawnDistance { get; } = cfg.BindEx(section, MaxDistance,
            "Min distance from the trophy mobs can spawn", new AcceptableValueRange<float>(0, MaxDistance));
        public ConfigEntry<float> MaxSpawnDistance { get; } = cfg.BindEx(section, MaxDistance,
            "Max distance from the trophy mobs can spawn", new AcceptableValueRange<float>(0, MaxDistance));
        public ConfigEntry<int> MaxLevel { get; } = cfg.BindEx(section, 3,
            "Maximum level of spawned mobs", new AcceptableValueRange<int>(1, 9));
        public ConfigEntry<int> LevelUpChanceOverride { get; } = cfg.BindEx(section, -1,
            "Level up chance override for spawned mobs. If < 0, world default is used", new AcceptableValueRange<int>(-1, 100));
        public ConfigEntry<int> SpawnLimit { get; } = cfg.BindEx(section, 20,
            "Maximum number of mobs of the trophy's type in the active area", new AcceptableValueRange<int>(1, 10000));
        public ConfigEntry<bool> SuppressDrops { get; } = cfg.BindEx(section, true,
            "True to suppress drops from mobs spawned by trophies. Does not work reliably (yet)");
        public ConfigEntry<MessageTypes> MessageType { get; } = cfg.BindEx(section, MessageTypes.InWorld,
            "Type of message to show when a trophy is attracting mobs", AcceptableEnum<MessageTypes>.Default);
    }
}