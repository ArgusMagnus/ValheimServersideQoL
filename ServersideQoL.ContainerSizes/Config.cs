using BepInEx.Configuration;

namespace ServersideQoL.ContainerSizes;

public sealed class Config(ConfigFile cfg, Logger logger) : ConfigBase<Config>(cfg, logger)
{
  const string Section = "ContainerSizes";

  public override ConfigEntry<bool> Enabled { get; } = BindEx(cfg, Section, true,
    "Enables/disables the entire mod");

  public IReadOnlyDictionary<int, ConfigEntry<string>> ContainerSizes { get; } = ZNetScene.instance.m_prefabs
    .Where(static x => PieceTablesByPieceName.ContainsKey(x.name))
    .Select(static x => (Name: x.name, Container: x.GetComponentInChildren<Container>(), Piece: x.GetComponent<Piece>()))
    .Where(static x => x is { Container: not null, Piece: not null })
    .ToDictionary(static x => x.Name.GetStableHashCode(), x => cfg
    .Bind(Section, Invariant($"InventorySize_{x.Name}"), Invariant($"{x.Container.m_width}x{x.Container.m_height}"), Invariant($"""
      Inventory size for '{(global::Localization.instance.Localize(x.Piece.m_name))}'.
      If you append '+' to the end (e.g. '{x.Container.m_width}x{x.Container.m_height}+'),
      the inventory size will keep expanding as long as only one type of item is stored inside.
      """)));
}
