using ServersideQoL.Processors;
using ServersideQoL.Utilities;

namespace ServersideQoL.AdminOptions;

[Processor("256a8351-08e2-4b0f-9c71-f011c7cf846f")]
[DependsOn<PlayerRegistryProcessor>]
public sealed class WearNTearProcessor : Processor<WearNTearProcessor.PrefabInfo>
{
  public sealed record PrefabInfo(WearNTear WearNTear, Piece Piece, PieceTable PieceTable) : ProcessorPrefabInfo;

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (Config.Instance.ToggleDisableRainDamageEmote.Value is ConfigBase.DisabledEmote &&
        Config.Instance.ToggleDisableSupportRequirements.Value is ConfigBase.DisabledEmote &&
        Config.Instance.ToggleMakeIndestructible.Value is ConfigBase.DisabledEmote)
      return ProcessResult.UnregisterProcessor;

    const PlayerProcessor.BuildModifiers Unset = (PlayerProcessor.BuildModifiers)uint.MaxValue;
    var modifiers = __adminBuildModifiersVar.Get(zdo, Unset);
    var creator = zdo.Vars.GetCreator();
    if (modifiers is not Unset)
      return ProcessResult.UnregisterProcessor;

    modifiers = Instance<PlayerProcessor>().GetBuildModifiers(creator);
    if (modifiers is not PlayerProcessor.BuildModifiers.None)
      __adminBuildModifiersVar.Set(zdo, modifiers);

    var fields = zdo.Fields<WearNTear>();
    var result = ProcessResult.UnregisterProcessor;

    if ((modifiers & PlayerProcessor.BuildModifiers.DisableRainDamage) is not 0 && fields.UpdateValue(static () => x => x.m_noRoofWear, false))
      result |= ProcessResult.RecreateZDO;

    if ((modifiers & PlayerProcessor.BuildModifiers.DisableSupportRequirements) is not 0 && fields.UpdateValue(static () => x => x.m_noSupportWear, false))
      result |= ProcessResult.RecreateZDO;

    if ((modifiers & PlayerProcessor.BuildModifiers.MakeIndestructible) is not 0 && fields.UpdateValue(static () => x => x.m_health, -1))
    {
      zdo.Vars.SetHealth(-1);
      result |= ProcessResult.RecreateZDO;
    }

    return result;
  }

  static readonly ServerVar<PlayerProcessor.BuildModifiers> __adminBuildModifiersVar = AdminOptionsPlugin.RegisterServerVar<PlayerProcessor.BuildModifiers>("AdminBuildModifiers");
}
