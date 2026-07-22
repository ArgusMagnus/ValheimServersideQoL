using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ServersideQoL.Patcher;

/// <seealso href="https://docs.bepinex.dev/articles/dev_guide/preloader_patchers.html"/>
static class ZDOPatcher
{
  const string AssemblyName = "assembly_valheim.dll";
  const string PropertyName = "PrefabInfo";

  public static IEnumerable<string> TargetDLLs => [AssemblyName];

  public static void Patch(AssemblyDefinition assembly)
  {
    // IMPORTANT:
    // Be careful to not do anything that will cause ServersideQoL.dll to actually be loaded.
    // Only use constants/nameof and indirect references.

    var module = assembly.MainModule;
    var zdoType = module.GetType("ZDO") ?? throw new Exception("ZDO type not found");

    var serversideQoLRef = new AssemblyNameReference(ServersideQoLPlugin.PluginName, new(ServersideQoLPlugin.PluginVersion));
    module.AssemblyReferences.Add(serversideQoLRef);

    var propertyTypeReference = new TypeReference(nameof(ServersideQoL), nameof(PrefabInfo), module, serversideQoLRef);
    var propertyType = module.ImportReference(propertyTypeReference);

    var backingField = new FieldDefinition($"_{PropertyName}_backing", FieldAttributes.Private, propertyType);
    zdoType.Fields.Add(backingField);

    var getMethod = new MethodDefinition($"get_{PropertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, propertyType);
    var il = getMethod.Body.GetILProcessor();
    il.Append(il.Create(OpCodes.Ldarg_0));
    il.Append(il.Create(OpCodes.Ldfld, backingField));
    il.Append(il.Create(OpCodes.Ret));
    zdoType.Methods.Add(getMethod);

    var setMethod = new MethodDefinition($"set_{PropertyName}", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName, module.TypeSystem.Void);
    setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propertyType));
    var il2 = setMethod.Body.GetILProcessor();
    il2.Append(il2.Create(OpCodes.Ldarg_0));
    il2.Append(il2.Create(OpCodes.Ldarg_1));
    il2.Append(il2.Create(OpCodes.Stfld, backingField));
    il2.Append(il2.Create(OpCodes.Ret));
    zdoType.Methods.Add(setMethod);

    var property = new PropertyDefinition(PropertyName, PropertyAttributes.None, propertyType)
    {
      GetMethod = getMethod,
      SetMethod = setMethod
    };

    zdoType.Properties.Add(property);

#if DEBUG
    Directory.CreateDirectory(BuildInfo.DependencyDirectory);
    assembly.Write(Path.Combine(BuildInfo.DependencyDirectory, AssemblyName));
#endif
  }

}
