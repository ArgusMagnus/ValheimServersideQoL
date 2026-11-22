using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ServersideQoL.Utilities;

public sealed class MethodCopyBuilder
{
  readonly DynamicMethodDefinition _method;
  public ModuleDefinition Module => _method.Module;

  public MethodCopyBuilder(Delegate original, string newName)
    : this(original.Method, newName) { }

  public MethodCopyBuilder(MethodInfo original, string newName)
  {
    _method = new DynamicMethodDefinition(original);

    // https://github.com/ArgusMagnus/ValheimServersideQoL/issues/326
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SetName(DynamicMethodDefinition method, string name) => method.Name = name;
    try { SetName(_method, newName); }
    catch (MissingFieldException) { }

    var originalMethodProperty = typeof(DynamicMethodDefinition).GetProperty(nameof(DynamicMethodDefinition.OriginalMethod));
    if (originalMethodProperty.CanWrite)
      originalMethodProperty.SetValue(_method, null);
    else if (typeof(DynamicMethodDefinition).GetField($"<{originalMethodProperty.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic) is { } originalMethodField)
      originalMethodField.SetValue(_method, null); // https://github.com/ArgusMagnus/ValheimServersideQoL/issues/326
    else
      throw new Exception($"Property {nameof(DynamicMethodDefinition.OriginalMethod)} could not be set");
  }

  public ParameterDefinition AddParameter<T>(string name)
  {
    var parameter = new ParameterDefinition(name, Mono.Cecil.ParameterAttributes.None, _method.Module.ImportReference(typeof(T)));
    _method.Definition.Parameters.Add(parameter);
    return parameter;
  }

  public MethodCopyBuilder Transpile(Action<MethodCopyBuilder, Mono.Collections.Generic.Collection<Instruction>> transpiler)
  {
    transpiler(this, _method.Definition.Body.Instructions);
    return this;
  }

  public T Build<T>()
    where T : Delegate
  {
    //var invoke = typeof(T).GetMethod("Invoke");
    //var parameters = invoke.GetParameters();
    //if (!parameters.Select(x => _method.Module.ImportReference(x.ParameterType)).SequenceEqual(_method.Definition.Parameters.Select(static x => x.ParameterType)))
    //  throw new NotSupportedException("Changing parameters is not supported yet");
    //for (int i = _method.Definition.Parameters.Count; i < parameters.Length; i++)
    //  _method.Definition.Parameters.Add(new(parameters[i].Name, Mono.Cecil.ParameterAttributes.None, _method.Module.ImportReference(parameters[i].ParameterType));
    var mi = _method.Generate();
    return mi.CreateDelegate<T>();
  }
}
