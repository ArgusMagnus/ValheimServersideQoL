using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.Utilities;

public static class SQoLUtils
{
  public static string ToPascalCase(string str)
    => Regex.Replace(str, @"(?:^|_)([a-z])", static m => m.Groups[1].Value.ToUpperInvariant());

  public static bool ApproximatelyEqual(float a, float b, float tolerance)
    => Mathf.Abs(a - b) <= tolerance;
}
