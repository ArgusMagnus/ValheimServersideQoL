using static PlayerProfile;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class LocalizationConfig
    {
        public ContainersConfig Containers { get; init; } = new();
        public MapTableConfig MapTable { get; init; } = new();
        public NonTeleportableItemsConfig NonTeleportableItems { get; init; } = new();
        public PlayersConfig Players { get; init; } = new();
        public SleepingConfig Sleeping { get; init; } = new();
        public SmeltersConfig Smelters { get; init; } = new();
        public TamesConfig Tames { get; init; } = new();
        public TrophySpawnerConfig TrophySpawner { get; init; } = new();
        public TurretsConfig Turrets { get; init; } = new();

        public sealed class NonTeleportableItemsConfig
        {
            public string ItemsReturned { get; init; } = "Portal returned your items";
            string ItemsTaken { get; init; } = "Portal took {0} items";
            public string FormatItemsTaken(int count) => string.Format(ItemsTaken, count);
        }

        public sealed class PlayersConfig
        {
            public string SacrificedMegingjord { get; init; } = "You were permanently granted increased carrying weight";
            public string SacrificedCryptKey { get; init; } = "You were permanently granted the ability to open sunken crypt doors";
            public string SacrificedWishbone { get; init; } = "You were permanently granted the ability to sense hidden objects";
            public string SacrificedTornSpirit { get; init; } = "You were permanently granted a wisp companion";
            public BackpackConfig Backpack { get; init; } = new();

            public sealed class BackpackConfig
            {
                public string Name { get; init; } = "Backpack";
                public string ForbiddenItems { get; init; } = "Backpack cannot contain non-teleportable items";
                string WeightLimitExceeded { get; init; } = "Backpack weight limit ({0}) exceeded";
                public string FormatWeightLimitExceeded(int maxWeight) => string.Format(WeightLimitExceeded, maxWeight);
            }
        }

        public sealed class SleepingConfig
        {
            string Prompt { get; init; } = "{0} of {1} players want to sleep.<br>Sit down if you want to sleep as well";
            public string FormatPrompt(int sleepingPlayers, int totalPlayers) => string.Format(Prompt, sleepingPlayers, totalPlayers);
        }

        public sealed class ContainersConfig
        {
            string ContainerSorted { get; init; } = "{0} sorted";
            public string FormatContainerSorted(string containerName) => string.Format(ContainerSorted, containerName);
            string AutoPickup { get; init; } = "{0}: $msg_added {1} {2}x";
            public string FormatAutoPickup(string containerName, string itemName, int stack) => string.Format(AutoPickup, containerName, itemName, stack);
            public ObliteratorItemTeleporterConfig ObliteratorItemTeleporter { get; init; } = new();

            public sealed class ObliteratorItemTeleporterConfig
            {
                public string TargetNotFound { get; init; } = "No target with corresponding tag found";
                public string ForbiddenItem { get; init; } = "An item prevents the teleportation";
                public string ItemsTeleported { get; init; } = "Items teleported";
            }
        }

        public sealed class TamesConfig
        {
            string Growing { get; init; } = "$caption_growing {0}%";
            public string FormatGrowing(int percent) => string.Format(Growing, percent);
            string Taming { get; init; } = "$hud_tameness {0:P0}";
            string TamingHungry { get; init; } = "$hud_tameness {0:P0}, $hud_tamehungry";
            public string FormatTaming(float tameness, bool isHungry) => isHungry ? string.Format(TamingHungry, tameness) : string.Format(Taming, tameness);
        }

        public sealed class MapTableConfig
        {
            public string Updated { get; init; } = "$msg_mapsaved";
        }

        public sealed class SmeltersConfig
        {
            string FuelAdded { get; init; } = "{0}: $msg_added {1} {2}x";
            public string FormatFuelAdded(string smelterName, string itemName, int stack) => string.Format(FuelAdded, smelterName, itemName, stack);
            string OreAdded { get; init; } = "{0}: $msg_added {1} {2}x";
            public string FormatOreAdded(string smelterName, string itemName, int stack) => string.Format(OreAdded, smelterName, itemName, stack);
        }

        public sealed class TrophySpawnerConfig
        {
            string AttractingProgress { get; init; } = "Attracting {0}... {1:P0}";
            public string FormatAttractingProgress(string creatureName, double progress) => string.Format(AttractingProgress, creatureName, progress);
            string Attracting { get; init; } = "Attracting {0}";
            public string FormatAttracting(string creatureName) => string.Format(Attracting, creatureName);
        }

        public sealed class TurretsConfig
        {
            string AmmoAdded { get; init; } = "{0}: $msg_added {1} {2}x";
            public string FormatAmmoAdded(string turretName, string itemName, int count) => string.Format(AmmoAdded, turretName, itemName, count);
            public string NoAmmoFound { get; init; } = "<color=red>$msg_noturretammo";
        }
    }
}
