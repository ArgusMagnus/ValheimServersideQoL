namespace ServersideQoL.AdminBuildOptions;

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
    var modifiers = GetAdminBuildModifiers(zdo, Unset);
    var creator = zdo.Vars.GetCreator();
    if (modifiers is not Unset)
      return ProcessResult.UnregisterProcessor;

    modifiers = Instance<PlayerProcessor>().GetBuildModifiers(creator);
    if (modifiers is not PlayerProcessor.BuildModifiers.None)
      SetAdminBuildModifiers(zdo, modifiers);

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

  static readonly int __adminBuildModifiers = AdminBuildOptionsPlugin.RegisterServerVar("AdminBuildModifiers");
  static PlayerProcessor.BuildModifiers GetAdminBuildModifiers(ServersideQoLZDO zdo, PlayerProcessor.BuildModifiers defaultValue = default) => (PlayerProcessor.BuildModifiers)zdo.ZDO.GetInt(__adminBuildModifiers, (int)defaultValue);
  static void SetAdminBuildModifiers(ServersideQoLZDO zdo, PlayerProcessor.BuildModifiers value) => zdo.ZDO.Set(__adminBuildModifiers, (int)value);
}
