using ServersideQoL.Utilities;
using UnityEngine;

namespace ServersideQoL.Signs;

[Processor(Id)]
public sealed class FermenterSignProcessor : Processor<FermenterSignProcessor.PrefabInfo>
{
  public const string Id = "0daf063a-639c-4797-9a0d-c8fd7e7e420c";

  public sealed record PrefabInfo(Fermenter Fermenter) : ProcessorPrefabInfo
  {
    public (Vector3 Direction, float Distance, float Height) Placement { get; } = GetPlacement(Fermenter);

    static (Vector3, float, float) GetPlacement(Fermenter fermenter)
    {
      var root = fermenter.transform;
      var tap = fermenter.m_tapSwitch ? root.InverseTransformPoint(fermenter.m_tapSwitch.transform.position) : Vector3.forward;
      var direction = new Vector3(tap.x, 0, tap.z);
      direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;

      // Radius is measured perpendicular to the tap, so the protruding tap does not push the sign away from the barrel
      var side = new Vector3(direction.z, 0, -direction.x);
      float minY = float.MaxValue, maxY = float.MinValue, radius = 0;
      foreach (var filter in fermenter.GetComponentsInChildren<MeshFilter>(true))
      {
        if (!filter.sharedMesh)
          continue;
        var bounds = filter.sharedMesh.bounds;
        for (var i = 0; i < 8; i++)
        {
          var corner = bounds.center + Vector3.Scale(bounds.extents, new((i & 1) is 0 ? -1 : 1, (i & 2) is 0 ? -1 : 1, (i & 4) is 0 ? -1 : 1));
          corner = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
          minY = Mathf.Min(minY, corner.y);
          maxY = Mathf.Max(maxY, corner.y);
          radius = Mathf.Max(radius, Mathf.Abs(Vector3.Dot(corner, side)));
        }
      }

      if (minY > maxY)
        return (direction, 0.6f, 0.8f);
      return (direction, radius + 0.05f, (minY + maxY) / 2);
    }
  }

  const float UpdateInterval = 5f;

  readonly Dictionary<ServersideQoLZDO, ServersideQoLZDO> _signsByFermenters = [];

  protected override void Initialize()
  {
    foreach (var sign in _signsByFermenters.Values)
      sign.Destroy();
    _signsByFermenters.Clear();
  }

  protected override bool ClaimExclusive(ServersideQoLZDO zdo) => false; // let SignProcessor process the signs (default sign color)

  protected override ProcessResult Process(ServersideQoLZDO zdo, IReadOnlyList<Peer> peers, PrefabInfo prefabInfo)
  {
    if (!Config.Instance.FermenterSigns.Value || zdo.Vars.GetCreator().Value is 0)
      return ProcessResult.UnregisterProcessor;

    if (!_signsByFermenters.TryGetValue(zdo, out var sign))
    {
      var (direction, distance, height) = prefabInfo.Placement;
      direction = zdo.ZDO.GetRotation() * direction;
      var pos = zdo.ZDO.GetPosition() + direction * distance;
      pos.y += height;
      sign = PlacePiece(pos, Prefabs.Sign, Quaternion.LookRotation(direction).eulerAngles.y);
      sign.Fields<WearNTear>().Set(static () => x => x.m_supports, false);
      _signsByFermenters.Add(zdo, sign);
      zdo.Destroyed += OnFermenterDestroyed;
    }

    var result = ProcessResult.Default;
    string text;

    /// <see cref="Fermenter.GetStatus"/> <see cref="Fermenter.GetHoverText"/>
    var content = zdo.Vars.GetContent();
    if (content is 0)
      text = Localization.instance.Localize("$piece_container_empty");
    else if (prefabInfo.Fermenter.m_conversion.FirstOrDefault(x => x.m_from.gameObject.name.GetStableHashCode() == content) is not { } conversion)
      text = "Invalid";
    else
    {
      var name = Localization.instance.Localize(conversion.m_from.m_itemData.m_shared.m_name);
      var remaining = zdo.Fields<Fermenter>().GetFloat(static () => x => x.m_fermentationDuration);
      var startTime = zdo.Vars.GetStartTime();
      if (startTime.Ticks is not 0)
        remaining -= (float)(ZNet.instance.GetTime() - startTime).TotalSeconds;

      if (remaining < 0f)
        text = $"{name}<br>{Localization.instance.Localize("$piece_fermenter_ready")}";
      else
      {
        var time = TimeSpan.FromSeconds(Mathf.Ceil(remaining));
        text = Invariant($"{name}<br>{(int)time.TotalMinutes}:{time.Seconds:D2}");
        result = ScheduleReprocessing(Mathf.Min(UpdateInterval, remaining + 0.1f));
      }
    }

    var signText = sign.Vars.GetText();
    if (Instance<SignProcessor>().DefaultColorRegex.Match(signText) is { Success: true } color)
      text = $"{color.Value}{text}"; // keep the default color applied by the Signs mod instead of overwriting it every update

    if (signText != text)
      sign.Vars.SetText(text);

    return result;
  }

  void OnFermenterDestroyed(ServersideQoLZDO zdo)
  {
    if (_signsByFermenters.Remove(zdo, out var sign))
      sign.Destroy();
  }
}
