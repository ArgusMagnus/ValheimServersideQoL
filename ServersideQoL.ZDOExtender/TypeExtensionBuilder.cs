using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace ServersideQoL.ZDOExtender;

static class TypeExtensionBuilder
{
    static string __moduleName = $"{typeof(ZDOExtender).Assembly.GetName().Name}.Dynamic";
    static readonly ModuleBuilder __moduleBuilder = AssemblyBuilder
        .DefineDynamicAssembly(new(__moduleName), AssemblyBuilderAccess.Run)
        .DefineDynamicModule(__moduleName);

    internal static TypeBuilder DefineType(string name, Type baseType) => __moduleBuilder.DefineType($"{__moduleName}.{name}", default, baseType);
}

public sealed class TypeExtensionBuilder<TBaseInterface, TBaseType>(string? typeName = default)
    where TBaseInterface : class
    where TBaseType : class, TBaseInterface
{
    static readonly ConstructorInfo __baseCtor;
    static TypeExtensionBuilder()
    {
        if (!typeof(TBaseInterface).IsInterface)
            throw new ArgumentException($"{typeof(TBaseInterface).FullName} is not an interface type.");
        if (typeof(TBaseType) is not { IsClass: true, IsSealed: false })
            throw new ArgumentException($"{typeof(TBaseType).FullName} must be valid base class (not sealed)");
        __baseCtor = typeof(TBaseType).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, types: Type.EmptyTypes, modifiers: null)
            ?? throw new ArgumentException($"{typeof(TBaseType).FullName}: No parameterless constructor found");
    }

    readonly string _typeName = typeName ?? typeof(TBaseType).Name;
    TypeBuilder? _typeBuilder;
    Type? _type;
    ConstructorInfo? _constructorInfo;
    Func<TBaseType>? _factory;
    HashSet<Type>? _interfaces;

    public bool HasInterfaces => _typeBuilder is not null;

    public void AddInterface<TInterface>()
        where TInterface : TBaseInterface
    {
        if (_type is not null)
            throw new InvalidOperationException("The type has already been created");

        var iface = typeof(TInterface);

        if (!iface.IsInterface)
            throw new ArgumentException($"{iface.FullName} is not an interface type.");
        if (iface == typeof(TBaseInterface))
            throw new ArgumentException($"{iface.FullName} is the base interface and cannot be registered.");

        if (!(_interfaces ??= []).Add(iface))
            return;

        var members = iface.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
        IReadOnlyList<PropertyInfo> properties = [.. members.OfType<PropertyInfo>().Where(static x => x is { GetMethod: not null, SetMethod: not null })];
        foreach (var property in properties)
        {
            members.Remove(property);
            members.Remove(property.GetMethod);
            members.Remove(property.SetMethod);
        }
        if (members.Count is not 0)
            throw new ArgumentException($"{iface.FullName} contains unsupported members. Only properties with both get and set accessors are supported.");

        if (_typeBuilder is null)
        {
            _typeBuilder = TypeExtensionBuilder.DefineType(_typeName, typeof(TBaseType));
            var ctorBuilder = _typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            var cil = ctorBuilder.GetILGenerator();
            cil.Emit(OpCodes.Ldarg_0);
            cil.Emit(OpCodes.Call, __baseCtor); // must be accessible (public/protected) or CreateType will fail
            cil.Emit(OpCodes.Ret);
        }

        // MethodAttributes used for explicit interface implementations (private + virtual + final + newslot)
        const MethodAttributes implAttrs = MethodAttributes.Private | MethodAttributes.HideBySig
            | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final;

        _typeBuilder.AddInterfaceImplementation(iface);

        foreach (var prop in properties)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;

            var backingField = _typeBuilder.DefineField($"_{iface.Name}_{propName}_backing", propType, FieldAttributes.Private);

            //var propertyBuilder = typeBuilder.DefineProperty(propName, PropertyAttributes.None, propType, Type.EmptyTypes);

            var getterName = $"{iface.Name}_get_{propName}";
            var getter = _typeBuilder.DefineMethod(getterName, implAttrs, propType, Type.EmptyTypes);
            var getIl = getter.GetILGenerator();
            // load 'this', load field, return
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, backingField);
            getIl.Emit(OpCodes.Ret);
            //propertyBuilder.SetGetMethod(getter);

            var setterName = $"{iface.Name}_set_{propName}";
            var setter = _typeBuilder.DefineMethod(setterName, implAttrs, typeof(void), new[] { propType });
            var setIl = setter.GetILGenerator();
            // this._field = value; return;
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, backingField);
            setIl.Emit(OpCodes.Ret);
            //propertyBuilder.SetSetMethod(setter);

            // map explicit implementations to the interface's get/set methods
            _typeBuilder.DefineMethodOverride(getter, prop.GetMethod);
            _typeBuilder.DefineMethodOverride(setter, prop.SetMethod);
        }
    }

    public Type Build()
    {
        if (_type is not null)
            return _type;
        if (_typeBuilder is null)
            throw new InvalidOperationException("No interfaces have been added");
        _type = _typeBuilder.CreateType();
        _typeBuilder = null;
        _interfaces = null;
        return _type;
    }

    public ConstructorInfo GetConstructorInfo()
        => _constructorInfo ??= Build().GetConstructor(Type.EmptyTypes);

    public Func<TBaseType> GetFactory()
        => _factory ??= Expression.Lambda<Func<TBaseType>>(Expression.Convert(Expression.New(GetConstructorInfo()), typeof(TBaseType))).Compile();
}
