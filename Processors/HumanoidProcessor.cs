using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class HumanoidProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    //protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    //{
    //    if (prefabInfo.Character is null)
    //        return;

    //    if (SharedState.DataRevisions.TryGetValue(zdo.m_uid, out var dataRevision) && dataRevision == zdo.DataRevision)
    //        return;

    //    var level = zdo.GetInt(ZDOVars.s_level, 1);
    //    if (level < 3)
    //    {
    //        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    //        return;
    //    }

    //    var name = Config.Creatures.ShowLevelInName.Value ? $"{prefabInfo.Character.m_name} Lvl {zdo.GetInt(ZDOVars.s_level, 1)}" : prefabInfo.Character.m_name;
    //    var fields = zdo.Fields(prefabInfo.Character);
    //    if (fields.GetString(x => x.m_name) == name)
    //    {
    //        SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    //        return;
    //    }

    //    if (Config.Creatures.ShowLevelInName.Value)
    //        fields.Set(x => x.m_name, name);
    //    else
    //        fields.Reset(x => x.m_name);

    //    SharedState.DataRevisions.TryRemove(zdo.m_uid, out _);
    //    zdo = zdo.Recreate();
    //    SharedState.DataRevisions[zdo.m_uid] = zdo.DataRevision;
    //}

    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Humanoid is null)
            return false;

        var level = zdo.Vars.GetLevel();
        if (level <= 1)
            return false;

        var fields = zdo.Fields<Humanoid>();

        var name = $"<line-height=150%><voffset=-2em>{zdo.PrefabInfo.Humanoid.m_name}<size=70%><br><color=yellow>{string.Concat(Enumerable.Repeat("⭐", level - 1))}</color></size></voffset></line-height>";
        if (fields.SetIfChanged(x => x.m_name, name))
            RecreateZdo = true;

        return false;
    }
}