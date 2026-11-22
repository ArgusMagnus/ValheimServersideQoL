using Mono.Cecil.Cil;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ServersideQoL.Utilities;

public static class SQoLUtils
{
  public static string ToPascalCase(string str)
    => Regex.Replace(str, @"(?:^|_)([a-z])", static m => m.Groups[1].Value.ToUpperInvariant());

  public static bool ApproximatelyEqual(float a, float b, float tolerance)
    => Mathf.Abs(a - b) <= tolerance;

  public static T CreateCopy<T>(this MethodInfo method, string newName, Action<MethodCopyBuilder, Mono.Collections.Generic.Collection<Instruction>> transpiler)
    where T : Delegate
    => new MethodCopyBuilder(method, newName)
      .Transpile(transpiler)
      .Build<T>();

  public static T CreateCopy<T>(this Delegate method, string newName, Action<MethodCopyBuilder, Mono.Collections.Generic.Collection<Instruction>> transpiler)
    where T : Delegate
    => CreateCopy<T>(method.Method, newName, transpiler);
}
