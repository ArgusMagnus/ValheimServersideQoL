using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class NetworkingConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> MeasurePing { get; } = cfg.BindEx(section, false, "True to measure player ping");
        public ConfigEntry<int> PingStatisticsWindow { get; } = cfg.BindEx(section, 60, "Number of measurements to include for statistic calculations like mean and standard deviation",
            new AcceptableValueRange<int>(1, 100000));
        public ConfigEntry<int> LogPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the server is logged if it exceeds this threshold");
        public ConfigEntry<int> ShowPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the server is shown to the player if it exceeds this threshold");
        public ConfigEntry<int> LogZoneOwnerPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the zone owner is logged if it exceeds this threshold");
        public ConfigEntry<int> ShowZoneOwnerPingThreshold { get; } = cfg.BindEx(section, 0, "A player's ping value to the zone owner is shown to the player if it exceeds this threshold");
        public ConfigEntry<string> LogPingFormat { get; } = cfg.BindEx(section,
            "Ping ({0}): {1:F0} ms (ema: {6:F0} ms, av: {2:F0} ± {3:F0} ms, jitter: {4:F0} ms)", """
                Format string for logging player ping.
                Arguments:
                  0: Player name
                  1: Ping value in milliseconds
                  2: Mean of ping value in milliseconds
                  3: Standard deviation of ping value in milliseconds
                  4: Jitter in milliseconds
                  5: Connection quality
                  6: Exponential moving average of ping value in milliseconds
                """, new AcceptableFormatString(["", 0d, 0d, 0d, 0d, 0f, 0d]));
        public ConfigEntry<string> ShowPingFormat { get; } = cfg.BindEx(section,
            "Ping: <color=yellow>{0:F0} ms</color> (ema: {5:F0} ms, av: {1:F0} ± {2:F0} ms, jitter: {3:F0} ms)", """
                Format string for player ping messages.
                Arguments:
                  0: Ping value in milliseconds
                  1: Mean ping of value in milliseconds
                  2: Standard deviation of ping value in milliseconds
                  3: Jitter in milliseconds
                  4: Connection quality
                  5: Exponential moving average of ping value in milliseconds
                """, new AcceptableFormatString([0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<string> LogZoneOwnerPingFormat { get; } = cfg.BindEx(section,
            "Ping ({0}): {1:F0} ms (ema: {12:F0} ms, av: {2:F0} ± {3:F0} ms, jitter: {4:F0} ms) + ZoneOwner ({6}): {7:F0} ms (ema: {13:F0} ms, av: {8:F0} ± {9:F0} ms, jitter: {10:F0} ms)", """
                Format string for logging player ping.
                Arguments:
                  0: Player name
                  1: Ping value in milliseconds
                  2: Mean ping of value in milliseconds
                  3: Standard deviation of ping value in milliseconds
                  4: Jitter in milliseconds
                  5: Connection quality
                  6: Zone owner player name
                  7: Zone owner ping value in milliseconds
                  8: Mean ping of zone owner ping in milliseconds
                  9: Standard deviation of zone owner ping value in milliseconds
                 10: Zone owner jitter in milliseconds
                 11: Zone owner connection quality
                 12: Exponential moving average of ping value in milliseconds
                 13: Zone owner exponential moving average of ping value in milliseconds
                """, new AcceptableFormatString(["", 0d, 0d, 0d, 0d, 0f, "", 0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<string> ShowZoneOwnerPingFormat { get; } = cfg.BindEx(section,
            "Ping: <color=yellow>{0:F0} ms</color> (ema: {11:F0} ms, av: {1:F0} ± {2:F0} ms, jitter: {3:F0} ms) + <color=yellow>{5}: {6:F0} ms</color> (ema: {12:F0} ms, av: {7:F0} ± {8:F0} ms, jitter: {9:F0} ms)", """
                Format string for player ping messages.
                Arguments:
                  0: Ping value in milliseconds
                  1: Mean ping of value in milliseconds
                  2: Standard deviation of ping value in milliseconds
                  3: Jitter in milliseconds
                  4: Connection quality
                  5: Zone owner player name
                  6: Zone owner ping value in milliseconds
                  7: Mean ping of zone owner ping in milliseconds
                  8: Standard deviation of zone owner ping value in milliseconds
                  9: Zone owner jitter in milliseconds
                 10: Zone owner connection quality
                 11: Exponential moving average of ping value in milliseconds
                 12: Zone owner exponential moving average of ping value in milliseconds
                """, new AcceptableFormatString([0d, 0d, 0d, 0d, 0f, "", 0d, 0d, 0d, 0d, 0f]));
        public ConfigEntry<bool> ReassignOwnershipBasedOnConnectionQuality { get; } = cfg.BindEx(section, false, $"""
                True to (re)assign zone ownership to the player with the best connection.
                Requires '{nameof(MeasurePing)}' to be enabled.
                The connection with the lowest connection quality value (lower = better) is chosen as the best connection,
                where connection quality = ping mean * {nameof(ConnectionQualityPingMeanWeight)} + ping stddev * {nameof(ConnectionQualityPingStdDevWeight)} + ping jitter * {nameof(ConnectionQualityPingJitterWeight)} + ping EMA * {nameof(ConnectionQualityPingEMAWeight)}
                WARNING: This feature is highly experimental and is likely to cause issues/interfere with other features of this mod and other mods
                """);
        public ConfigEntry<float> ReassignOwnershipConnectionQualityHysteresis { get; } = cfg.BindEx(section, 0.15f, $"""
                Minimum difference in connection quality required to reassign ownership.
                If this value is smaller than 1, it's interpreted as relative difference (e.g. 0.15 means 15% difference),
                otherwise as absolute value difference in connection quality.
                """);
        public ConfigEntry<float> ConnectionQualityPingMeanWeight { get; } = cfg.BindEx(section, 0f,
            "Weight of ping mean when calculating connection quality");
        public ConfigEntry<float> ConnectionQualityPingStdDevWeight { get; } = cfg.BindEx(section, 0f,
            "Weight of ping standard deviation when calculating connection quality");
        public ConfigEntry<float> ConnectionQualityPingJitterWeight { get; } = cfg.BindEx(section, 0f,
            "Weight of ping jitter when calculating connection quality");
        public ConfigEntry<float> ConnectionQualityPingEMAWeight { get; } = cfg.BindEx(section, 1f,
            "Weight of ping exponential moving average when calculating connection quality");
        public ConfigEntry<float> PingEMAHalfLife { get; } = cfg.BindEx(section, 2.5f,
            "Half-life time in seconds for the exponential moving average of the ping value");
        public ConfigEntry<bool> AssignInteractablesToClosestPlayer { get; } = cfg.BindEx(section, false, """
                True to assign ownership of some interactable objects (such as smelters or cooking stations) to the closest player.
                This should help avoiding the loss of ore, etc. due to networking issues.
                """);
        public ConfigEntry<bool> AssignMobsToClosestPlayer { get; } = cfg.BindEx(section, false, """
                True to assign ownership of hostile mobs to the closest player.
                This should help reduce issues with dodging/parrying due to networking issues.
                """);
    }
}
