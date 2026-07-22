using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Xml.Serialization;

namespace ServersideQoL.Patcher;

/// <seealso href="https://docs.bepinex.dev/articles/dev_guide/preloader_patchers.html"/>
static class ZDOPatcher
{
  // IMPORTANT:
  // Be careful to not do anything that will cause ServersideQoL.dll to actually be loaded.
  // Only use constants/nameof and indirect references.

  const string AssemblyName = "assembly_valheim.dll";
  const string PropertyName = nameof(ServersideQoLZDO);
  const string PropertyTypeNamespace = nameof(ServersideQoL);
  const string PropertyTypeName = nameof(ServersideQoLZDO);

  public static IEnumerable<string> TargetDLLs => [AssemblyName];

  public static void Patch(AssemblyDefinition assembly)
  {
    // todo:
    // Add:
    // - CompilerGenerated to backing field/getter/setter
    // - DebuggerBrowsable(DebuggerBrowsableState.Never) to backing field

    var module = assembly.MainModule;
    var zdoType = module.GetType("ZDO") ?? throw new Exception("ZDO type not found");

    var serversideQoLRef = new AssemblyNameReference(ServersideQoLPlugin.PluginName, new(ServersideQoLPlugin.PluginVersion));
    module.AssemblyReferences.Add(serversideQoLRef);

    var propertyType = module.ImportReference(new TypeReference(PropertyTypeNamespace, PropertyTypeName, module, serversideQoLRef));
    var nullableAttrType = module.ImportReference(new TypeReference("System.Runtime.CompilerServices", "NullableAttribute", module, module.TypeSystem.CoreLibrary));

    var backingField = new FieldDefinition($"<{PropertyName}>k__BackingField", FieldAttributes.Private | FieldAttributes.InitOnly, propertyType);
    zdoType.Fields.Add(backingField);

    var getMethod = new MethodDefinition($"get_{PropertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, propertyType);
    var il = getMethod.Body.GetILProcessor();
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, backingField));
    il.Append(il.Create(OpCodes.Ret));
    zdoType.Methods.Add(getMethod);

    // Remove FieldAttributes.InitOnly from backingField when uncommenting this code
    //var setMethod = new MethodDefinition($"set_{PropertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, module.TypeSystem.Void);
    //setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propertyType));
    //var il2 = setMethod.Body.GetILProcessor();
    //il2.Append(il2.Create(OpCodes.Ldarg_0));
    //il2.Append(il2.Create(OpCodes.Ldarg_1));
    //il2.Append(il2.Create(OpCodes.Stfld, backingField));
    //il2.Append(il2.Create(OpCodes.Ret));
    //zdoType.Methods.Add(setMethod);

    var property = new PropertyDefinition(PropertyName, PropertyAttributes.None, propertyType)
    {
      GetMethod = getMethod,
      //SetMethod = setMethod
    };

    //var nullableAttrCtor = module.ImportReference(new MethodReference(".ctor", module.TypeSystem.Void, nullableAttrType)
    //{
    //  HasThis = true,
    //  Parameters = { new ParameterDefinition(module.TypeSystem.Byte) }
    //});

    //var nullableAttr = new CustomAttribute(nullableAttrCtor);
    //nullableAttr.ConstructorArguments.Add(
    //    new CustomAttributeArgument(module.TypeSystem.Byte, (byte)2) // 2 = nullable
    //);

    //property.CustomAttributes.Add(nullableAttr);

    zdoType.Properties.Add(property);

    // init backing field to new(this)
    var ctor = zdoType.Methods.Single(static m => m.IsConstructor && !m.IsStatic);
    il = ctor.Body.GetILProcessor();
    var first = ctor.Body.Instructions[0];
    var propertyTypeCtor = module.ImportReference(new MethodReference(".ctor", module.TypeSystem.Void, propertyType)
    {
      HasThis = true,
      Parameters = { new ParameterDefinition(zdoType) }
    });

    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // for stfld
    il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // for newobj argument
    il.InsertBefore(first, il.Create(OpCodes.Newobj, propertyTypeCtor));
    il.InsertBefore(first, il.Create(OpCodes.Stfld, backingField));

#if DEBUG
    Directory.CreateDirectory(BuildInfo.DependencyDirectory);
    assembly.Write(Path.Combine(BuildInfo.DependencyDirectory, AssemblyName));
#endif
  }

}
