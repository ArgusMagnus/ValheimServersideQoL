using static Valheim.ServersideQoL.ModConfigBase.WearNTearConfig;

namespace Valheim.ServersideQoL.Processors;

sealed class WearNTearProcessor : Processor
{
    protected override Guid Id { get; } = Guid.Parse("46374d89-a351-48f5-96b2-b1ad46e71ee6");

    protected override bool ProcessCore(ExtendedZDO zdo, IReadOnlyList<Peer> peers)
    {
        UnregisterZdoProcessor = true;
        if (zdo.PrefabInfo.WearNTear is null)
            return false;

        const PlayerProcessor.BuildModifiers Unset = (PlayerProcessor.BuildModifiers)(-1);
        var modifiers = zdo.Vars.GetAdminBuildModifiers(Unset);
        var creator = zdo.Vars.GetCreator();
        if (modifiers is Unset)
        {
            if (Instance<PlayerProcessor>().GetPeerInfoFromPlayerID(creator) is not { } peerInfo)
                modifiers = PlayerProcessor.BuildModifiers.None;
            else
            {
                modifiers = peerInfo.BuildModifiers;
                zdo.Vars.SetAdminBuildModifiers(modifiers);
            }
        }

        var fields = zdo.Fields<WearNTear>();
        var isPlayerBuilt = zdo.PrefabInfo.WearNTear is { Piece.Value: not null, PieceTable.Value: not null } && creator is not 0;
        if (isPlayerBuilt)
        {
            if (Config.WearNTear.DisableRainDamage.Value)
                modifiers |= PlayerProcessor.BuildModifiers.DisableRainDamage;

            if (Config.WearNTear.MakeIndestructible.Value)
                modifiers |= PlayerProcessor.BuildModifiers.MakeIndestructible;
        }

        if ((modifiers & PlayerProcessor.BuildModifiers.DisableSupportRequirements) is 0 && (
            Config.WearNTear.DisableSupportRequirements.Value is DisableSupportRequirementsOptions.None ? false : (
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.PlayerBuilt) && isPlayerBuilt) ||
                (Config.WearNTear.DisableSupportRequirements.Value.HasFlag(DisableSupportRequirementsOptions.World) && !isPlayerBuilt))))
        {
            modifiers |= PlayerProcessor.BuildModifiers.DisableSupportRequirements;
        }

        if ((modifiers & PlayerProcessor.BuildModifiers.DisableRainDamage) is 0)
            fields.Reset(static () => x => x.m_noRoofWear);
        else if (fields.UpdateValue(static () => x => x.m_noRoofWear, false))
            RecreateZdo = true;

        if ((modifiers & PlayerProcessor.BuildModifiers.DisableSupportRequirements) is 0)
            fields.Reset(static () => x => x.m_noSupportWear);
        else if (fields.UpdateValue(static () => x => x.m_noSupportWear, false))
            RecreateZdo = true;

        if ((modifiers & PlayerProcessor.BuildModifiers.MakeIndestructible) is 0)
        {
            if (fields.UpdateResetValue(static () => x => x.m_health))
                zdo.Vars.RemoveHealth();
        }
        else if (fields.UpdateValue(static () => x => x.m_health, -1))
        {
            zdo.Vars.SetHealth(-1);
            RecreateZdo = true;
        }

        return false;
    }
}