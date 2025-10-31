using BepInEx.Configuration;
using System.Text.RegularExpressions;
using Valheim.ServersideQoL.Processors;

namespace Valheim.ServersideQoL;

partial record ModConfig
{
    public sealed class NonTeleportableItemsConfig(ConfigFile cfg, string section)
    {
        public ConfigEntry<bool> Enable { get; } = cfg.BindEx(section, false, """
             True to enable the non-teleportable items feature.
             Items which are not teleportable by default (e.g. ores, metals, etc.) will be temporarily taken from a player's inventory when they enter a certain range around a portal so that they can travel through, according to the settings below.
             When the player leaves the range (e.g. by travelling through the portal), the items will be returned to their inventory.
             """);

        public ConfigEntry<float> PortalRange { get; } = cfg.BindEx(section, 4f, """
             The range around a portal in which items will be taken from a player's inventory.
             Decreasing this value will lead to a longer delay before players with non-teleportable items in their inventory can use the portal.
             Increasing this value will leave players unable to have certain items in their inventory in a larger range around portals.
             """);

        public ConfigEntry<MessageTypes> MessageType { get; } = cfg.BindEx(section, MessageTypes.None,
            "Type of message to show when a non-teleportable item is taken from/returned to a player's inventory", AcceptableEnum<MessageTypes>.Default);

        public sealed record Entry(ItemDrop ItemDrop, ConfigEntry<string> Config);

        public IReadOnlyList<Entry> Entries { get; } = new Func<IReadOnlyList<Entry>>(() =>
        {
            var acceptableValues = new AcceptableValueList<string>([.. SharedProcessorState.BossesByBiome.Values
                .OrderBy(static x => x.m_health)
                .Select(static x => x.m_defeatSetGlobalKey)]);

            List<Entry> result = new();
            foreach (var item in ObjectDB.instance.m_items)
            {
                if (item.GetComponent<ItemDrop>() is not { m_itemData.m_shared.m_teleportable: false } itemDrop)
                    continue;

                var defaultValue = "";
                if (Regex.IsMatch(item.name, @"copper|tin|bronze", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.BlackForest].m_defeatSetGlobalKey;
                else if (item.name.Contains("iron", StringComparison.OrdinalIgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Swamp].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"silver|DragonEgg", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Mountain].m_defeatSetGlobalKey;
                else if (item.name.Contains("blackmetal", StringComparison.OrdinalIgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Plains].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"DvergrNeedle|MechanicalSpring", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.Mistlands].m_defeatSetGlobalKey;
                else if (Regex.IsMatch(item.name, @"flametal|CharredCogwheel", RegexOptions.IgnoreCase))
                    defaultValue = SharedProcessorState.BossesByBiome[Heightmap.Biome.AshLands].m_defeatSetGlobalKey;

                result.Add(new(itemDrop, cfg.Bind(section, item.name, defaultValue, new ConfigDescription(
                    $"Key of the boss that will allow '{(global::Localization.instance.Localize(itemDrop.m_itemData.m_shared.m_name))}' to be teleported when defeated",
                    acceptableValues))));
            }
            return result;
        }).Invoke();
    }
}