namespace Valheim.ServersideQoL.Processors;

sealed class WindmillProcesser : Processor
{
    protected override Guid Id { get; } = Guid.Parse("fd865439-b237-48a8-a27e-76f262eedf31");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.Windmill is null)
            return false;

        /// <see cref="Windmill.GetPowerOutput()"/>
        var fields = zdo.Fields<Windmill>();
        if (!Config.Windmills.IgnoreWind.Value)
            fields.Reset(static () => x => x.m_minWindSpeed);
        else if (fields.SetIfChanged(static () => x => x.m_minWindSpeed, float.MinValue))
            RecreateZdo = true;

        return true;
    }
}
