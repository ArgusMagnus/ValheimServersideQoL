namespace Valheim.ServersideQoL.Processors;

sealed class PlantProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("6b337a7f-e751-454e-ac84-b6c3b4679535");

    //readonly HashSet<int> _cropPickables = [];

    //public override void Initialize(bool firstTime)
    //{
    //    base.Initialize(firstTime);

    //    if (Config.Plants.MakeHarvestableWithScythe.Value && _cropPickables.Count is 0)
    //    {
    //        foreach (var prefab in ZNetScene.instance.m_prefabs
    //            .Select(static x => x.GetComponent<Plant>()).Where(static x => x is not null)
    //            .SelectMany(static x => x.m_grownPrefabs).Where(static x => x.GetComponent<Pickable>() is not null))
    //        {
    //            Logger.DevLog(prefab.name);
    //            _cropPickables.Add(prefab.name.GetStableHashCode());
    //        }
    //    }
    //}

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;

        if (zdo.PrefabInfo.Plant is not null && !(Config.Plants.GrowTimeMultiplier.Value is 1f && Config.Plants.SpaceRequirementMultiplier.Value is 1f))
        {
            var fields = zdo.Fields<Plant>();
            if (Config.Plants.GrowTimeMultiplier.Value is not 1f)
            {
                if (fields.SetIfChanged(static x => x.m_growTime, zdo.PrefabInfo.Plant.m_growTime * Config.Plants.GrowTimeMultiplier.Value))
                    RecreateZdo = true;
                if (fields.SetIfChanged(static x => x.m_growTimeMax, zdo.PrefabInfo.Plant.m_growTimeMax * Config.Plants.GrowTimeMultiplier.Value))
                    RecreateZdo = true;
            }
            if (Config.Plants.SpaceRequirementMultiplier.Value is not 1f)
            {
                if (fields.SetIfChanged(static x => x.m_growRadius, zdo.PrefabInfo.Plant.m_growRadius * Config.Plants.SpaceRequirementMultiplier.Value))
                    RecreateZdo = true;
                //if (fields.SetIfChanged(static x => x.m_growRadiusVines, zdo.PrefabInfo.Plant.m_growRadiusVines * Config.Plants.SpaceRequirementMultiplier.Value))
                //    RecreateZdo = true;
            }

            if (!Config.Plants.DontDestroyIfCantGrow.Value)
                fields.Reset(static x => x.m_destroyIfCantGrow);
            else if (fields.SetIfChanged(static x => x.m_destroyIfCantGrow, false))
                RecreateZdo = true;
        }

        //if (zdo.PrefabInfo.Pickable is not null && Config.Plants.MakeHarvestableWithScythe.Value && _cropPickables.Contains(zdo.GetPrefab()))
        //{
        //    Logger.DevLog($"Pickable: {zdo.PrefabInfo.PrefabName}");
        //    var fields = zdo.Fields<Pickable>();
        //    if (fields.SetIfChanged(static x => x.m_harvestable, true))
        //        RecreateZdo = true;
        //}

        return false;
    }
}
