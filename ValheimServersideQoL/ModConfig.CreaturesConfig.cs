using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class CreaturesConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> ShowHigherLevelStars { get; } = cfg.BindEx(section, false,
            "True to show stars for higher level creatures (> 2 stars)");

        public ConfigEntry<ShowHigherLevelAuraOptions> ShowHigherLevelAura { get; } = cfg.BindEx(section, ShowHigherLevelAuraOptions.Never,
            "Show an aura for higher level creatures (> 2 stars)", AcceptableEnum<ShowHigherLevelAuraOptions>.Default);

        public ConfigEntry<int> MaxLevelIncrease { get; } = cfg.BindEx(section, 0, """
             Amount the max level of creatures is incremented throughout the world.
             The level up chance increases with the max level.
             Example: if this value is set to 2, a creature will spawn with 4 stars with the same probability as it would spawn with 2 stars without this setting.
             """);

        public ConfigEntry<int> MaxLevelIncreasePerDefeatedBoss { get; } = cfg.BindEx(section, 0, """
             Amount the max level of creatures is incremented per defeated boss.
             The respective boss's biome and previous biomes are affected and the level up chance increases with the max level.
             Example: If this value is set to 1 and Eikthyr and the Elder is defeated, the max creature level in the Black Forest will be raised by 1 and in the Meadows by 2.
             """);

        public ConfigEntry<Heightmap.Biome> TreatOceanAs { get; } = cfg.BindEx(section, Heightmap.Biome.BlackForest,
            "Biome to treat the ocean as for the purpose of leveling up creatures",
            new AcceptableEnum<Heightmap.Biome>(AcceptableEnum<Heightmap.Biome>.Default.AcceptableValues.Where(static x => x is not Heightmap.Biome.Ocean)));

        public ConfigEntry<bool> LevelUpBosses { get; } = cfg.BindEx(section, false, "True to also level up bosses");

        public ConfigEntry<RespawnOneTimeSpawnsConditions> RespawnOneTimeSpawnsCondition { get; } = cfg.BindEx(section, RespawnOneTimeSpawnsConditions.Never,
            "Condition for one-time spawns to respawn");

        public ConfigEntry<float> RespawnOneTimeSpawnsAfter { get; } = cfg.BindEx(section, 240f,
            "Time after one-time spawns are respawned in minutes");

        [Flags]
        public enum ShowHigherLevelAuraOptions
        {
            Never = 0,
            Wild = (1 << 0),
            Tamed = (1 << 1)
        }

        public enum RespawnOneTimeSpawnsConditions
        {
            Never,
            Always,
            AfterBossDefeated
        }
    }
}