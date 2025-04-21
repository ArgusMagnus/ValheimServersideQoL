using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class HumanoidProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ExtendedZDO zdo, IEnumerable<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Humanoid is null)
            return false;

        var level = zdo.Vars.GetLevel();
        if (level <= 3)
            return false;

        var fields = zdo.Fields<Humanoid>();
        if (!Config.Creatures.ShowHigherLevelStars.Value)
            fields.Reset(x => x.m_name);
        else if (fields.SetIfChanged(x => x.m_name, $"<line-height=150%><voffset=-2em>{zdo.PrefabInfo.Humanoid.m_name}<size=70%><br><color=yellow>{string.Concat(Enumerable.Repeat("⭐", level - 1))}</color></size></voffset></line-height>"))
            RecreateZdo = true;        

        return false;
    }
}