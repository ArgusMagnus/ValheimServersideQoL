using BepInEx.Logging;

namespace Valheim.ServersideQoL.Processors;

sealed class PlantProcessor : Processor
{
    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Plant is null || (Config.Plants.GrowTimeMultiplier.Value is 1f && Config.Plants.SpaceRequirementMultiplier.Value is 1f))
            return false;

        var fields = zdo.Fields<Plant>();
        if (Config.Plants.GrowTimeMultiplier.Value is not 1f)
        {
            if (fields.SetIfChanged(x => x.m_growTime, zdo.PrefabInfo.Plant.m_growTime * Config.Plants.GrowTimeMultiplier.Value))
                RecreateZdo = true;
            if (fields.SetIfChanged(x => x.m_growTimeMax, zdo.PrefabInfo.Plant.m_growTimeMax * Config.Plants.GrowTimeMultiplier.Value))
                RecreateZdo = true;
        }
        if (Config.Plants.SpaceRequirementMultiplier.Value is not 1f)
        {
            if (fields.SetIfChanged(x => x.m_growRadius, zdo.PrefabInfo.Plant.m_growRadius * Config.Plants.SpaceRequirementMultiplier.Value))
                RecreateZdo = true;
            //if (fields.SetIfChanged(x => x.m_growRadiusVines, zdo.PrefabInfo.Plant.m_growRadiusVines * Config.Plants.SpaceRequirementMultiplier.Value))
            //    RecreateZdo = true;
        }

        if (!Config.Plants.DontDestroyIfCantGrow.Value)
            fields.Reset(x => x.m_destroyIfCantGrow);
        else if (fields.SetIfChanged(x => x.m_destroyIfCantGrow, false))
            RecreateZdo = true;

        return false;
    }
}
