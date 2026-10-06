namespace ServersideQoL.AutoMapTables;

static class WardAccess
{
  public static bool HasActiveWard<TWard>(IEnumerable<TWard>? wards, Func<TWard, bool> isEnabled)
  {
    if (wards is null)
      return false;

    foreach (var ward in wards)
    {
      if (isEnabled(ward))
        return true;
    }

    return false;
  }
}
