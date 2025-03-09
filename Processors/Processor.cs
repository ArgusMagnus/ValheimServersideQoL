using BepInEx.Configuration;
using BepInEx.Logging;
using System.Reflection;

namespace Valheim.ServersideQoL.Processors;

record struct ItemKey(string Name, int Quality, int Variant)
{
    public static implicit operator ItemKey(ItemDrop.ItemData data) => new(data);
    public ItemKey(ItemDrop.ItemData data) : this(data.m_shared.m_name, data.m_quality, data.m_variant) { }
}

record struct SharedItemDataKey(string Name)
{
    public static implicit operator SharedItemDataKey(ItemDrop.ItemData.SharedData data) => new(data.m_name);
}

abstract class Processor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState)
{
    protected ManualLogSource Logger { get; } = logger;
    protected ModConfig Config { get; } = cfg;
    protected SharedProcessorState SharedState { get; } = sharedState;


    public virtual void Initialize() { }
    public virtual void PreProcess() { }
    public abstract void Process(ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers);

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo)
        => CheckMinDistance(peers, zdo, Config.General.MinPlayerDistance.Value);

    protected bool CheckMinDistance(IEnumerable<ZNetPeer> peers, ZDO zdo, float minDistance)
        => peers.Min(x => Utils.DistanceSqr(x.m_refPos, zdo.GetPosition())) >= minDistance * minDistance;
}
