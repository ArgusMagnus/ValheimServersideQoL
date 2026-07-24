using UnityEngine;
using static ServersideQoL.ContainerSigns.Config;

namespace ServersideQoL.ContainerSigns;

[Processor("bbbb47b4-3b9f-4d63-8bb2-a6f388ae1180")]
public sealed class ContainerProcessor : Processor<ContainerRegistryProcessor.PrefabInfo>
{
  readonly Dictionary<ServersideQoLZDO, List<ServersideQoLZDO>> _signsByChests = [];
  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _chestsBySigns = [];
  public IReadOnlyDictionary<ServersideQoLZDO, List<ServersideQoLZDO>> SignsByChests => _signsByChests;
  public IReadOnlyDictionary<ServersideQoLZDO, ServersideQoLZDO> ChestsBySigns => _chestsBySigns;

  protected override void Initialize()
  {
    foreach (var zdo in _chestsBySigns.Keys)
      zdo.Destroy();
    _signsByChests.Clear();
    _chestsBySigns.Clear();
  }

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, ContainerRegistryProcessor.PrefabInfo prefabInfo)
  {
    var cfg = Config.Instance;
    var signOptions = cfg.GetSignOptions(zdo.ZDO.GetPrefab());
    if (signOptions is SignOptions.None || !cfg.ChestSignOffsets.Value.ChestSignOffsets.TryGetValue(zdo.ZDO.GetPrefab(), out var signOffset) || _signsByChests.ContainsKey(zdo))
      return ProcessResult.UnregisterProcessor;

    var text = zdo.Vars.GetText(null!);
    if (text is null)
    {
      if (!zdo.IsOwnerOrUnassigned())
      {
        Instance<ContainerRegistryProcessor>().RequestOwnership(zdo, 0);
        return ProcessResult.SkipOtherProcessors | ProcessResult.WaitForZDORevisionChange;
      }
      zdo.Vars.SetText(text = cfg.ChestSignsDefaultText.Value);
    }
    var p = zdo.ZDO.GetPosition();
    var r = zdo.ZDO.GetRotation();
    var rot = r.eulerAngles.y + 90;
    var signs = new List<ServersideQoLZDO>();
    p.y += signOffset.Top / 2;
    if (signOptions.HasFlag(SignOptions.Left))
      signs.Add(PlacePiece(p + r * Vector3.right * signOffset.Left, Prefabs.Sign, rot));
    if (signOptions.HasFlag(SignOptions.Right))
      signs.Add(PlacePiece(p + r * Vector3.left * signOffset.Right, Prefabs.Sign, rot + 180));
    if (signOptions.HasFlag(SignOptions.Front))
      signs.Add(PlacePiece(p + r * Vector3.forward * signOffset.Front, Prefabs.Sign, rot + 270));
    if (signOptions.HasFlag(SignOptions.Back))
      signs.Add(PlacePiece(p + r * Vector3.back * signOffset.Back, Prefabs.Sign, rot + 90));
    p = zdo.ZDO.GetPosition();
    p.y += signOffset.Top;
    if (signOptions.HasFlag(SignOptions.TopLongitudinal))
      signs.Add(PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot - 90, 0)));
    if (signOptions.HasFlag(SignOptions.TopLateral))
      signs.Add(PlacePiece(p, Prefabs.Sign, Quaternion.Euler(-90, rot, 0)));
    _signsByChests.Add(zdo, signs);
    foreach (var sign in signs)
    {
      _chestsBySigns.Add(sign, zdo);
      sign.Vars.SetText(text);
      sign.Fields<WearNTear>().Set(static () => x => x.m_supports, false);
      //sign.Fields<Piece>().Set(static () => x => x.m_canBeRemoved, true);
      //sign.Destroyed += _ => RPC.Remove(zdo);
    }
    zdo.Destroyed += OnChestDestroyed;

    return ProcessResult.UnregisterProcessor;
  }

  void OnChestDestroyed(ServersideQoLZDO zdo)
  {
    if (_signsByChests.Remove(zdo, out var signs))
    {
      foreach (var sign in signs)
      {
        _chestsBySigns.Remove(sign);
        sign.Destroy();
      }
    }
  }
}
