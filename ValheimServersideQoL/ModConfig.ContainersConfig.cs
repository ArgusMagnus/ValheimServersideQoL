using BepInEx.Configuration;
using Valheim.ServersideQoL.Processors;
using YamlDotNet.Serialization;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class ContainersConfig(ConfigFile cfg, string section)
    {
        const string ChestSignItemNamesFileName = "ChestSignItemNames.yml";

        public ConfigEntry<bool> AutoSort { get; } = cfg.BindEx(section, false, "True to auto sort container inventories");
        public ConfigEntry<MessageTypes> SortedMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a container was sorted", AcceptableEnum<MessageTypes>.Default);

        public ConfigEntry<bool> AutoPickup { get; } = cfg.BindEx(section, false,
            "True to automatically put dropped items into containers if they already contain said item");
        public ConfigEntry<float> AutoPickupRange { get; } = cfg.BindEx(section, ZoneSystem.c_ZoneSize,
            $"Required proximity of a container to a dropped item to be considered as auto pickup target. Can be overridden per chest by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign");
        public ConfigEntry<int> AutoPickupMaxRange { get; } = cfg.BindEx(section, (int)ZoneSystem.c_ZoneSize,
            $"Max auto pickup range players can set per chest (by putting '{SignProcessor.MagnetEmoji}<Range>' on a chest sign)");
        public ConfigEntry<float> AutoPickupMinPlayerDistance { get; } = cfg.BindEx(section, 4f,
            "Min distance all player must have to a dropped item for it to be picked up");
        public ConfigEntry<bool> AutoPickupExcludeFodder { get; } = cfg.BindEx(section, true,
            "True to exclude food items for tames when tames are within search range");
        public ConfigEntry<bool> AutoPickupRequestOwnership { get; } = cfg.BindEx(section, true,
            "True to make the server request (and receive) ownership of dropped items from the clients before they are picked up. This will reduce the risk of data conflicts (e.g. item duplication) but will drastically decrease performance");
        public ConfigEntry<MessageTypes> PickedUpMessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a dropped item is added to a container", AcceptableEnum<MessageTypes>.Default);

        const string DefaultPlaceholderString = "•";
        public ConfigEntry<string> ChestSignsDefaultText { get; } = cfg.BindEx(section, DefaultPlaceholderString, "Default text for chest signs");
        public ConfigEntry<string> ChestSignsContentListPlaceholder { get; } = cfg.BindEx(section, DefaultPlaceholderString,
            "If this value is found in the text of a chest sign, it will be replaced by a list of contained items in that chest");
        public ConfigEntry<int> ChestSignsContentListMaxCount { get; } = cfg.BindEx(section, 3,
            "Max number of entries to show in the content list on chest signs.");
        public ConfigEntry<string> ChestSignsContentListSeparator { get; } = cfg.BindEx(section, "<br>",
            "Separator to use for content lists on chest signs");
        public ConfigEntry<string> ChestSignsContentListNameRest { get; } = cfg.BindEx(section, "Other",
            "Text to show for the entry summarizing the rest of the items");
        public ConfigEntry<string> ChestSignsContentListEntryFormat { get; } = cfg.BindEx(section, "{0} {1}",
            $"Format string for entries in the content list, the first argument is the name of the item, the second is the total number of per item. The item names can be configured further by editing {ChestSignItemNamesFileName}",
        new AcceptableFormatString(["Test", 0]));

        public ConfigEntry<SignOptions> WoodChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on wood chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> ReinforcedChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on reinforced chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> BlackmetalChestSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on blackmetal chests", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> BarrelSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on barrels", AcceptableEnum<SignOptions>.Default);
        public ConfigEntry<SignOptions> ObliteratorSigns { get; } = cfg.BindEx(section, SignOptions.None,
            "Options to automatically put signs on obliterators", new AcceptableEnum<SignOptions>([SignOptions.Front]));
        public ConfigEntry<ObliteratorItemTeleporterOptions> ObliteratorItemTeleporter { get; } = cfg.BindEx(section, ObliteratorItemTeleporterOptions.Disabled,
            $"Options to enable obliterators to teleport items instead of obliterating them when the lever is pulled. Requires '{nameof(ObliteratorSigns)}' and two obliterators with matching tags. The tag is set by putting '{SignProcessor.LinkEmoji}<Tag>' on the sign",
        AcceptableEnum<ObliteratorItemTeleporterOptions>.Default);
        public ConfigEntry<MessageTypes> ObliteratorItemTeleporterMessageType { get; } = cfg.BindEx(section, MessageTypes.InWorld,
            "Type of message to show for obliterator item teleporters", AcceptableEnum<MessageTypes>.Default);

        public IReadOnlyDictionary<int, ConfigEntry<string>> ContainerSizes { get; } = ZNetScene.instance.m_prefabs
        .Where(static x => SharedProcessorState.PieceTablesByPiece.ContainsKey(x.name))
        .Select(static x => (Name: x.name, Container: x.GetComponentInChildren<Container>(), Piece: x.GetComponent<Piece>()))
        .Where(static x => x is { Container: not null, Piece: not null })
        .ToDictionary(static x => x.Name.GetStableHashCode(), x => cfg
        .Bind(section, Invariant($"InventorySize_{x.Name}"), Invariant($"{x.Container.m_width}x{x.Container.m_height}"), Invariant($"""
             Inventory size for '{Localization.instance.Localize(x.Piece.m_name)}'.
             If you append '+' to the end (e.g. '{x.Container.m_width}x{x.Container.m_height}+'),
             the inventory size will keep expanding as long as only one type of item is stored inside.
             """)));

        public enum ObliteratorItemTeleporterOptions
        {
            Disabled,
            Enabled,
            EnabledAllItems,

            [Obsolete]
            False = Disabled,
            [Obsolete]
            True = Enabled
        }

        [Flags]
        public enum SignOptions
        {
            None,
            Left = (1 << 0),
            Right = (1 << 1),
            Front = (1 << 2),
            Back = (1 << 3),
            TopLongitudinal = (1 << 4),
            TopLateral = (1 << 5)
        }

        public IReadOnlyDictionary<string, string> ItemNames { get; } = new Func<IReadOnlyDictionary<string, string>>(() =>
        {
            var configDir = Path.Combine(Path.GetDirectoryName(cfg.ConfigFilePath), Path.GetFileNameWithoutExtension(cfg.ConfigFilePath));
            var itemNamesCfg = Path.Combine(configDir, ChestSignItemNamesFileName);
            Dictionary<string, string> items;
            if (!File.Exists(itemNamesCfg))
                items = new(ObjectDB.instance.m_items.Count);
            else
            {
                try
                {
                    using var stream = new StreamReader(File.OpenRead(itemNamesCfg));
                    items = new DeserializerBuilder().Build().Deserialize<Dictionary<string, string>>(stream);
                }
                catch (Exception ex)
                {
                    Main.Instance.Logger.LogWarning($"{ChestSignItemNamesFileName}: {ex}");
                    items = new(ObjectDB.instance.m_items.Count);
                }
            }

            foreach (var entry in ObjectDB.instance.m_items)
            {
                if (entry.GetComponent<ItemDrop>() is not { m_itemData.m_shared.m_icons.Length: > 0 } itemDrop)
                    continue;
                if (!items.ContainsKey(itemDrop.name))
                    items.Add(itemDrop.name, Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name));
            }

            if (!File.Exists(itemNamesCfg))
            {
                Directory.CreateDirectory(configDir);
                using var stream = new StreamWriter(File.OpenWrite(itemNamesCfg));
                WriteYamlHeader(stream);
                new SerializerBuilder().Build().Serialize(stream, items);
            }

            return items;
        }).Invoke();
    }
}