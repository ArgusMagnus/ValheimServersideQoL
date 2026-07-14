namespace Valheim.ServersideQoL;

public static class PrefabNames
{
    public const string Megingjord = "BeltStrength";
    public const string CryptKey = "CryptKey";
    public const string Wishbone = "Wishbone";
    public const string TornSpirit = "YagluthDrop";
    public const string BlackmetalChest = "piece_chest_blackmetal";
    public const string ReinforcedChest = "piece_chest";
    public const string WoodChest = "piece_chest_wood";
    public const string Barrel = "piece_chest_barrel";
    public const string Incinerator = "incinerator";
    public const string GiantBrain = "giant_brain";
}

public static class Prefabs
{
    public static int GraustenFloor4x4 { get; } = "Piece_grausten_floor_4x4".GetStableHashCode();
    public static int GraustenWall4x2 { get; } = "Piece_grausten_wall_4x2".GetStableHashCode();
    public static int PortalWood { get; } = "portal_wood".GetStableHashCode();
    public static int Portal { get; } = "portal".GetStableHashCode();
    public static int Sconce { get; } = "piece_walltorch".GetStableHashCode();
    public static int DvergerGuardstone { get; } = "dverger_guardstone".GetStableHashCode();
    public static int Sign { get; } = "sign".GetStableHashCode();
    public static int Candle { get; } = "Candle_resin".GetStableHashCode();
    public static int BlackmetalChest { get; } = PrefabNames.BlackmetalChest.GetStableHashCode();
    public static int ReinforcedChest { get; } = PrefabNames.ReinforcedChest.GetStableHashCode();
    public static int Barrel { get; } = PrefabNames.Barrel.GetStableHashCode();
    public static int WoodChest { get; } = PrefabNames.WoodChest.GetStableHashCode();
    public static int Incinerator { get; } = PrefabNames.Incinerator.GetStableHashCode();
    public static int CargoCrate { get; } = "CargoCrate".GetStableHashCode();
    public static int PrivateChest { get; } = "piece_chest_private".GetStableHashCode();
    public static int StandingIronTorch { get; } = "piece_groundtorch".GetStableHashCode();
    public static int StandingIronTorchGreen { get; } = "piece_groundtorch_green".GetStableHashCode();
    public static int StandingIronTorchBlue { get; } = "piece_groundtorch_blue".GetStableHashCode();
    //public static IReadOnlyList<int> Banners { get; } = [.. Enumerable.Range(1, 10).Select(static x => $"piece_banner{x:D2}".GetStableHashCode())];
    public static int MountainRemainsBuried { get; } = "Pickable_MountainRemains01_buried".GetStableHashCode();
}
