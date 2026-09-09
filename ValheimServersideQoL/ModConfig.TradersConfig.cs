using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    public sealed class TradersConfig(ConfigFile cfg, string section)
    {
        public IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> AlwaysUnlock { get; } = GetAlwaysUnlock(cfg, section);

        static IReadOnlyDictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>> GetAlwaysUnlock(ConfigFile cfg, string section)
        {
            if (!ZNet.instance.IsServer() || !ZNet.instance.IsDedicated())
                return new Dictionary<Trader, IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>>();

            return ZNetScene.instance.m_prefabs.Select(static x => x.GetComponent<Trader>()).Where(static x => x is not null)
                .Select(trader => (Trader: trader, Entries: (IReadOnlyList<(string GlobalKey, ConfigEntry<bool> ConfigEntry)>)[.. trader.m_items
                // 1.0 added trade items with no physical m_prefab (e.g. service/currency-style entries);
                // this feature is specifically about item-purchase unlocks, so skip those.
                .Where(static x => !string.IsNullOrEmpty(x.m_requiredGlobalKey) && x.m_prefab is not null)
                .Select(item => (item.m_requiredGlobalKey, cfg.Bind(section, Invariant($"{nameof(AlwaysUnlock)}{trader.name}{item.m_prefab.name}"), false,
                    Invariant($"Remove the progression requirements for buying {(global::Localization.instance.Localize(item.m_prefab.m_itemData.m_shared.m_name))} from {(global::Localization.instance.Localize(trader.m_name))}"))))]))
                .Where(static x => x.Entries.Any())
                .ToDictionary(static x => x.Trader, static x => x.Entries);
        }
    }
}