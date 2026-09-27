using System.Text.RegularExpressions;

namespace ServersideQoL.Utilities;

public static class SQoLUtils
{
  public static string ToPascalCase(string str)
    => Regex.Replace(str, @"(?:^|_)([a-z])", static m => m.Groups[1].Value.ToUpperInvariant());
}
