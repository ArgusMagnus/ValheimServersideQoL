using BepInEx.Configuration;
using System.Reflection;

namespace ServersideQoL.PhoenixPersons;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, true,
    "Enables/disables the entire mod");

  public ConfigEntry<float> SummoningRange { get; } = BindEx(cfg, 10f,
    "A player must be withint this range around a tombstone to be able to summon the tombstone's owner");
  public ConfigEntry<float> SummoningDurationSeconds { get; } = BindEx(cfg, 3f,
    "How long it takes (seconds) to summon the tombstone's owner");

  public ConfigEntry<Emotes> SummoningEmote { get; } = BindEx(cfg, Emotes.Sit, """
    The summoner needs to perform this emote near the tombstone,
    the player being summoned needs to perform this emote to accept.
    """, new AcceptableEnum<Emotes>(typeof(Emotes)
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .Where(static x => x.GetCustomAttribute<Emote>() is { OneShot: false })
      .Select(static x => (Emotes)x.GetRawConstantValue())));

  public YamlConfigEntry<AdvancedConfig> Advanced { get; } = BindYaml<AdvancedConfig>(cfg);
  public YamlConfigEntry<LocalizationConfig> Localization { get; } = BindYaml<LocalizationConfig>(cfg);

  public sealed class AdvancedConfig
  {
    public float TombStoneProcessingIntervalSeconds { get; init; } = 1;
  }

  public sealed class LocalizationConfig
  {
    string SummoningMessage { get; init; } = "Summoning ready in {0:F0}";
    public string FormatSummoningMessage(float delay) => string.Format(SummoningMessage, delay);

    string AwaitingAcceptanceMessage { get; init; } = "Waiting for {0} to accept the summon";
    public string FormatAwaitingAcceptanceMessage(string playerName) => string.Format(AwaitingAcceptanceMessage, playerName);

    string BeingSummonedMessage { get; init; } = "You're being summoned to your tombstone by {0} in {1:F0}";
    public string FormatBeingSummonedMessage(string summonerName, float delay) => string.Format(BeingSummonedMessage, summonerName, delay);

    string AcceptSummonMessage { get; init; } = "{0} is trying to summon you to your tombstone. {1} to accept";
    public string FormatAcceptSummonMessage(string summonerName, Emotes emote) => string.Format(AcceptSummonMessage, summonerName, emote);
  }
}
