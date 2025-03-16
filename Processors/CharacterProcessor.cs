using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class CharacterProcessor(ManualLogSource logger, ModConfig cfg) : Processor(logger, cfg)
{
    protected override bool ProcessCore(ref ExtendedZDO zdo, IEnumerable<ZNetPeer> peers)
    {
        if (zdo.PrefabInfo.Character is null)
            return false;

        var level = zdo.GetInt(ZDOVars.s_level, 1);
        //if (level < 3)
        //    return true;

        var name = Config.Creatures.ShowLevelInName.Value ? $"{zdo.PrefabInfo.Character.m_name} Lvl {zdo.GetInt(ZDOVars.s_level, 1)}" : zdo.PrefabInfo.Character.m_name;
        var fields = zdo.Fields<Character>();
        if (fields.GetString(x => x.m_name) == name)
            return true;

        //if (Config.Creatures.ShowLevelInName.Value)
            fields.Set(x => x.m_name, name);
        //else
        //    fields.Reset(x => x.m_name);

        // leads to black screen when connecting to server
        //zdo = zdo.Recreate();
        return true;
    }
}