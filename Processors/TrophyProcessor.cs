using BepInEx.Logging;
using UnityEngine;

namespace Valheim.ServersideQoL.Processors;

sealed class TrophyProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    TimeSpan _activationDelay;
    readonly int _spawnerPrefab = "Spawner_Bat".GetStableHashCode();
    readonly Dictionary<ZDOID, TrophyState> _spawnerByTrophy = [];
    readonly HashSet<ExtendedZDO> _spawners = [];
    readonly TimeSpan _textDuration = TimeSpan.FromSeconds(DamageText.instance.m_textDuration * 2);

    sealed class TrophyState
    {
        public DateTimeOffset LastMessage { get; set; }
        public ExtendedZDO? Spawner { get; set; }
    }

    public override void Initialize(bool firstTime)
    {
        base.Initialize(firstTime);
        RegisterZdoDestroyed();
        _activationDelay = TimeSpan.FromSeconds(Config.TrophySpawner.ActivationDelay.Value);
    }

    protected override void OnZdoDestroyed(ExtendedZDO zdo)
    {
        if (_spawnerByTrophy.Remove(zdo.m_uid, out var spawner))
            spawner.Spawner?.Destroy();
        else
            _spawners.Remove(zdo);
    }

    public override bool ClaimExclusive(ExtendedZDO zdo) => _spawners.Contains(zdo) || base.ClaimExclusive(zdo);

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        /// <see cref="MonsterAI"/> <see cref="BaseAI.GetPatrolPoint"/>

        var itemDrop = zdo.PrefabInfo.ItemDrop?.ItemDrop;
        if (!Config.TrophySpawner.Enable.Value || itemDrop is null || !SharedProcessorState.CharacterByTrophy.TryGetValue(itemDrop.m_itemData.m_shared, out var trophyCharacterPrefab))
        {
            UnregisterZdoProcessor = true;
            return false;
        }

        if (!_spawnerByTrophy.TryGetValue(zdo.m_uid, out var state))
            _spawnerByTrophy[zdo.m_uid] = state = new();

        var progress = zdo.GetTimeSinceSpawned().TotalSeconds / _activationDelay.TotalSeconds;
        if (progress < 1)
        {
            if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
            {
                state.LastMessage = DateTimeOffset.UtcNow;
                RPC.ShowInWorldText(peers, DamageText.TextType.Normal, zdo.GetPosition(), $"Attracting creatures... {progress:P0}");
            }
            return false;
        }

        if (state.Spawner is null)
        {
            var fields = zdo.Fields<ItemDrop>();
            if (fields.SetIfChanged(x => x.m_autoPickup, false))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_autoDestroy, false))
                RecreateZdo = true;

            if (RecreateZdo)
            {
                UnregisterZdoProcessor = true;
                return false;
            }

            _spawners.Add(state.Spawner = (ExtendedZDO)ZDOMan.instance.CreateNewZDO(zdo.GetPosition(), _spawnerPrefab));
            state.Spawner.SetPrefab(_spawnerPrefab);
            state.Spawner.Persistent = true;
            state.Spawner.Distant = false;
            state.Spawner.Type = ZDO.ObjectType.Default;
            state.Spawner.Vars.SetCreator(Main.PluginGuidHash);

            var item = new ItemDrop.ItemData { m_shared = itemDrop.m_itemData.m_shared };
            PrivateAccessor.LoadFromZDO(item, zdo);
            var f = (float)item.m_stack / item.m_shared.m_maxStackSize;
            state.Spawner.Fields<CreatureSpawner>()
                .Set(x => x.m_creaturePrefab, trophyCharacterPrefab)
                .Set(x => x.m_maxLevel, Mathf.RoundToInt(Utils.Lerp(1, Config.TrophySpawner.MaxLevel.Value, f)))
                .Set(x => x.m_levelupChance, Config.TrophySpawner.LevelUpChance.Value)
                .Set(x => x.m_respawnTimeMinuts, (float)TimeSpan.FromSeconds(
                    Utils.Lerp(Config.TrophySpawner.MaxRespawnDelay.Value, Config.TrophySpawner.MinRespawnDelay.Value, f)).TotalMinutes);
        }
        else if (DateTimeOffset.UtcNow - state.LastMessage > _textDuration)
        {
            state.LastMessage = DateTimeOffset.UtcNow;
            RPC.ShowInWorldText(peers, DamageText.TextType.Weak, zdo.GetPosition(), $"Attracting creatures");
        }

        return false;
    }
}
