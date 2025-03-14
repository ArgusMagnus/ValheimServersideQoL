using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class CharacterProcessor(ManualLogSource logger, ModConfig cfg, SharedProcessorState sharedState) : Processor(logger, cfg, sharedState)
{
    protected override void ProcessCore(ref ZDO zdo, PrefabInfo prefabInfo, IEnumerable<ZNetPeer> peers)
    {
        if (prefabInfo.Character is null)
            return;

        if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
            return;

        var level = zdo.GetInt(ZDOVars.s_level, 1);
        if (level < 3)
        {
            SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
            return;
        }

        var name = Config.Creatures.ShowLevelInName.Value ? $"{prefabInfo.Character.m_name} Lvl {zdo.GetInt(ZDOVars.s_level, 1)}" : prefabInfo.Character.m_name;
        var fields = zdo.Fields(prefabInfo.Character);
        if (fields.GetString(x => x.m_name) == name)
        {
            SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
            return;
        }

        if (Config.Creatures.ShowLevelInName.Value)
            fields.Set(x => x.m_name, name);
        else
            fields.Reset(x => x.m_name);

        SharedState.DataRevisions.TryRemove(zdo.m_uid, out _);
        zdo = zdo.Recreate();
        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    }
}