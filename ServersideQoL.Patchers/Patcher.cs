using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ServersideQoL.Patchers;

/// <seealso href="https://docs.bepinex.dev/articles/dev_guide/preloader_patchers.html"/>
static class Patcher
{
  // IMPORTANT:
  // Be careful to not do anything that will cause non-patcher types to be loaded.
  // Only use constants/nameof and indirect references.

  const string AssemblyName = "assembly_valheim.dll";

  public static IEnumerable<string> TargetDLLs => [AssemblyName];

  public static void Patch(AssemblyDefinition assembly)
  {
    AddProperty(assembly, "ZDO", nameof(ServersideQoL), "ServersideQoLZDO", "ServersideQoLZDO");
    AddProperty(assembly, "ZNetPeer", nameof(ServersideQoL), "Peer", "ServersideQoLPeer");

    static void AddProperty(AssemblyDefinition assembly, string typeToExtendName, string propertyTypeNamespace, string propertyTypeName, string propertyName)
    {
      // todo:
      // Add:
      // - CompilerGenerated to backing field/getter/setter
      // - DebuggerBrowsable(DebuggerBrowsableState.Never) to backing field

      var module = assembly.MainModule;
      var typeToExtend = module.GetType(typeToExtendName) ?? throw new Exception($"Type {typeToExtendName} not found");

      var serversideQoLRef = new AssemblyNameReference(PatchersPlugin.PluginName.Replace(".Patchers", ""), new(PatchersPlugin.PluginVersion));
      module.AssemblyReferences.Add(serversideQoLRef);

      var propertyType = module.ImportReference(new TypeReference(propertyTypeNamespace, propertyTypeName, module, serversideQoLRef));
      var nullableAttrType = module.ImportReference(new TypeReference("System.Runtime.CompilerServices", "NullableAttribute", module, module.TypeSystem.CoreLibrary));

      var backingField = new FieldDefinition($"<{propertyName}>k__BackingField", FieldAttributes.Private | FieldAttributes.InitOnly, propertyType);
      typeToExtend.Fields.Add(backingField);

      var getMethod = new MethodDefinition($"get_{propertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, propertyType);
      var il = getMethod.Body.GetILProcessor();
      il.Append(il.Create(OpCodes.Ldarg_0));
      il.Append(il.Create(OpCodes.Ldfld, backingField));
      il.Append(il.Create(OpCodes.Ret));
      typeToExtend.Methods.Add(getMethod);

      // Remove FieldAttributes.InitOnly from backingField when uncommenting this code
      //var setMethod = new MethodDefinition($"set_{PropertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, module.TypeSystem.Void);
      //setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propertyType));
      //var il2 = setMethod.Body.GetILProcessor();
      //il2.Append(il2.Create(OpCodes.Ldarg_0));
      //il2.Append(il2.Create(OpCodes.Ldarg_1));
      //il2.Append(il2.Create(OpCodes.Stfld, backingField));
      //il2.Append(il2.Create(OpCodes.Ret));
      //zdoType.Methods.Add(setMethod);

      var property = new PropertyDefinition(propertyName, PropertyAttributes.None, propertyType)
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

      typeToExtend.Properties.Add(property);

      // init backing field to new(this)
      var ctor = typeToExtend.Methods.Single(static m => m.IsConstructor && !m.IsStatic);
      il = ctor.Body.GetILProcessor();
      var first = ctor.Body.Instructions[0];
      var propertyTypeCtor = module.ImportReference(new MethodReference(".ctor", module.TypeSystem.Void, propertyType)
      {
        HasThis = true,
        Parameters = { new ParameterDefinition(typeToExtend) }
      });

      il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // for stfld
      il.InsertBefore(first, il.Create(OpCodes.Ldarg_0)); // for newobj argument
      il.InsertBefore(first, il.Create(OpCodes.Newobj, propertyTypeCtor));
      il.InsertBefore(first, il.Create(OpCodes.Stfld, backingField));
    }

#if DEBUG
    Directory.CreateDirectory(PatchersPlugin.DependencyDirectory);
    assembly.Write(Path.Combine(PatchersPlugin.DependencyDirectory, AssemblyName));
#endif
  }
}
