using ServersideQoL.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ServersideQoL;

partial class ServersideQoLZDO
{
  sealed class UnityObjectEqualityComparer<T> : EqualityComparer<T>
      where T : UnityEngine.Object
  {
    public static UnityObjectEqualityComparer<T> Instance { get; } = new();
    public override bool Equals(T x, T y) => x?.name == y?.name;
    public override int GetHashCode(T obj) => obj.name.GetHashCode();
  }

  delegate T GetHandler<T>(ZDO zdo, int hash, T defaultValue) where T : notnull;
  delegate void SetHandler<T>(ZDO zdo, int hash, T value) where T : notnull;
  delegate bool RemoveHandler<T>(ZDO zdo, int hash) where T : notnull;

  interface IComponentFieldAccessor
  {
    void Return();
  }

  static class ComponentFieldAccessor
  {
    public static ComponentFieldAccessor<TComponent> Get<TComponent>(ServersideQoLZDO zdo, TComponent component)
      where TComponent : MonoBehaviour
      => ComponentFieldAccessor<TComponent>.Get(zdo, component);
  }

  public sealed class ComponentFieldAccessor<TComponent> : IComponentFieldAccessor
    where TComponent : MonoBehaviour
  {
    static readonly Stack<ComponentFieldAccessor<TComponent>> __pool = [];
    static readonly Dictionary<Type, int> __hasComponentFieldsHashes = [];

    ServersideQoLZDO _zdo = default!;
    TComponent _component = default!;
    bool? _hasComponentFields;
    int _hasComponentFieldsHash;

    private ComponentFieldAccessor() { }

    public static ComponentFieldAccessor<TComponent> Get(ServersideQoLZDO zdo, TComponent component)
    {
      if (!__pool.TryPop(out var instance))
        instance = new();
      instance.Init(zdo, component);
      return instance;
    }

    [MemberNotNull(nameof(_zdo), nameof(_component))]
    void Init(ServersideQoLZDO zdo, TComponent component)
    {
      _zdo = zdo;
      _component = component;
      _hasComponentFields = null;
      var type = component.GetType();
      if (!__hasComponentFieldsHashes.TryGetValue(type, out _hasComponentFieldsHash))
        __hasComponentFieldsHashes.Add(type, _hasComponentFieldsHash = Invariant($"{ZNetView.CustomFieldsStr}{type.Name}").GetStableHashCode());
    }

    void IComponentFieldAccessor.Return()
    {
      System.Diagnostics.Debug.Assert(_zdo is not null);
      _zdo = null!;
      _component = null!;
      __pool.Push(this);
    }

    public bool HasFields => _zdo.HasFields && (_hasComponentFields ??= _zdo.ZDO.GetBool(_hasComponentFieldsHash));
    void SetHasFields(bool value)
    {
      if (value)
        _zdo.SetComponentHasFields();

      if (_hasComponentFields != value)
        _zdo.ZDO.Set(_hasComponentFieldsHash, (_hasComponentFields = value).Value);
    }

    static class ExpressionCache<T> where T : notnull
    {
      static readonly Dictionary<(string, int), Expression<Func<TComponent, T>>> __cache = [];

      public static Expression<Func<TComponent, T>> Get(Func<Expression<Func<TComponent, T>>> factory, string callerFilePath, int callerLineNo)
      {
        if (!__cache.TryGetValue((callerFilePath, callerLineNo), out var result))
          __cache.Add((callerFilePath, callerLineNo), result = factory());
        return result;
      }
    }

    sealed class FieldReference<T> where T : notnull
    {
      //readonly Expression<Func<TComponent, T>> _fieldExpression;
      readonly int _hash;
      readonly Func<TComponent, T> _getFieldValue;
      static readonly Dictionary<(Type, string), FieldReference<T>> __cacheByFieldName = [];
      static readonly Dictionary<(Type, string, int), FieldReference<T>> __cacheByLocation = [];

      static readonly (GetHandler<T> Getter, SetHandler<T> Setter, RemoveHandler<T> Remover, IEqualityComparer<T> EqualityComparer) Accessors =
          new Func<(GetHandler<T>, SetHandler<T>, RemoveHandler<T>, IEqualityComparer<T>)>(static () =>
          {
            if (typeof(T) == typeof(bool)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<bool>(static (ZDO zdo, int hash, bool defaultValue) => zdo.GetBool(hash, defaultValue)),
                      (SetHandler<T>)(Delegate)new SetHandler<bool>(static (ZDO zdo, int hash, bool value) => zdo.Set(hash, value)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<bool>(static (ZDO zdo, int hash) => zdo.RemoveInt(hash)),
                      (IEqualityComparer<T>)EqualityComparer<bool>.Default);

            if (typeof(T) == typeof(int)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<int>(static (ZDO zdo, int hash, int defaultValue) => zdo.GetInt(hash, defaultValue)),
                      (SetHandler<T>)(Delegate)new SetHandler<int>(static (ZDO zdo, int hash, int value) => zdo.Set(hash, value)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<int>(static (ZDO zdo, int hash) => zdo.RemoveInt(hash)),
                      (IEqualityComparer<T>)EqualityComparer<int>.Default);

            if (typeof(T) == typeof(float)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<float>(static (ZDO zdo, int hash, float defaultValue) => zdo.GetFloat(hash, defaultValue)),
                      (SetHandler<T>)(Delegate)new SetHandler<float>(static (ZDO zdo, int hash, float value) => zdo.Set(hash, value)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<float>(static (ZDO zdo, int hash) => zdo.RemoveFloat(hash)),
                      (IEqualityComparer<T>)EqualityComparer<float>.Default);

            if (typeof(T) == typeof(string)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<string>(static (ZDO zdo, int hash, string defaultValue) => zdo.GetString(hash, defaultValue)),
                      (SetHandler<T>)(Delegate)new SetHandler<string>(static (ZDO zdo, int hash, string value) => zdo.Set(hash, value)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<float>(static (ZDO zdo, int hash) => zdo.RemoveString(hash)),
                      (IEqualityComparer<T>)EqualityComparer<string>.Default);

            if (typeof(T) == typeof(Vector3)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<Vector3>(static (ZDO zdo, int hash, Vector3 defaultValue) => zdo.GetVec3(hash, defaultValue)),
                      (SetHandler<T>)(Delegate)new SetHandler<Vector3>(static (ZDO zdo, int hash, Vector3 value) => zdo.Set(hash, value)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<Vector3>(static (ZDO zdo, int hash) => zdo.RemoveVec3(hash)),
                      (IEqualityComparer<T>)EqualityComparer<Vector3>.Default);

            if (typeof(T) == typeof(GameObject)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<GameObject>(GetGameObject),
                      (SetHandler<T>)(Delegate)new SetHandler<GameObject>(static (ZDO zdo, int hash, GameObject value) => zdo.Set(hash, value.name)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<float>(static (ZDO zdo, int hash) => zdo.RemoveString(hash)),
                      (IEqualityComparer<T>)(object)UnityObjectEqualityComparer<GameObject>.Instance);

            if (typeof(T) == typeof(ItemDrop)) return (
                      (GetHandler<T>)(Delegate)new GetHandler<ItemDrop>(GetItemDrop),
                      (SetHandler<T>)(Delegate)new SetHandler<ItemDrop>(static (ZDO zdo, int hash, ItemDrop value) => zdo.Set(hash, value.name)),
                      (RemoveHandler<T>)(Delegate)new RemoveHandler<float>(static (ZDO zdo, int hash) => zdo.RemoveString(hash)),
                      (IEqualityComparer<T>)(object)UnityObjectEqualityComparer<ItemDrop>.Instance);

            throw new NotSupportedException();

            static GameObject GetGameObject(ZDO zdo, int hash, GameObject defaultValue)
            {
              var name = zdo.GetString(hash);
              if (string.IsNullOrEmpty(name))
                return defaultValue;
              return ZNetScene.instance.GetPrefab(name) ?? defaultValue;
            }

            static ItemDrop GetItemDrop(ZDO zdo, int hash, ItemDrop defaultValue)
            {
              var name = zdo.GetString(hash);
              if (string.IsNullOrEmpty(name))
                return defaultValue;
              return ZNetScene.instance.GetPrefab(name)?.GetComponent<ItemDrop>() ?? defaultValue;
            }
          }).Invoke();

      FieldReference(Type componentType, FieldInfo field)
      {
#if DEBUG
        if (field.FieldType != typeof(T))
          throw new Exception($"Field type {typeof(T).Name} expected, actual field type is {field.FieldType.Name}");
#endif
        _hash = Invariant($"{componentType.Name}.{field.Name}").GetStableHashCode();

        var par = Expression.Parameter(typeof(TComponent));
        _getFieldValue = Expression.Lambda<Func<TComponent, T>>(Expression.Field(par, field), par).Compile();
      }

      public static FieldReference<T> Get(Type type, Func<Expression<Func<TComponent, T>>> factory, string callerFilePath, int callerLineNo)
      {
        if (!__cacheByLocation.TryGetValue((type, callerFilePath, callerLineNo), out var result))
        {
          var expression = ExpressionCache<T>.Get(factory, callerFilePath, callerLineNo);
          var body = (MemberExpression)expression.Body;
          var field = (FieldInfo)body.Member;
          if (!__cacheByFieldName.TryGetValue((type, field.Name), out result))
            __cacheByFieldName.Add((type, field.Name), result = new(type, field));
          __cacheByLocation.Add((type, callerFilePath, callerLineNo), result);
        }
        return result;
      }

      public T GetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
      {
        var defaultValue = _getFieldValue(componentFieldAccessor._component);
        if (!componentFieldAccessor.HasFields)
          return defaultValue;
        return Accessors.Getter(componentFieldAccessor._zdo.ZDO, _hash, defaultValue);
      }

      public ComponentFieldAccessor<TComponent> SetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor, T value)
      {
        if (Accessors.EqualityComparer.Equals(value, _getFieldValue(componentFieldAccessor._component)))
          Accessors.Remover(componentFieldAccessor._zdo.ZDO, _hash);
        else
        {
          if (!componentFieldAccessor.HasFields)
            componentFieldAccessor.SetHasFields(true);
          Accessors.Setter(componentFieldAccessor._zdo.ZDO, _hash, value);
        }
        return componentFieldAccessor;
      }

      public bool UpdateValue(ComponentFieldAccessor<TComponent> componentFieldAccessor, T value)
      {
        var defaultValue = _getFieldValue(componentFieldAccessor._component);
        if (Accessors.EqualityComparer.Equals(value, Accessors.Getter(componentFieldAccessor._zdo.ZDO, _hash, defaultValue)))
          return false;

        var isDefaultValue = Accessors.EqualityComparer.Equals(value, defaultValue);

        if (isDefaultValue)
          Accessors.Remover(componentFieldAccessor._zdo.ZDO, _hash);
        else
        {
          if (!componentFieldAccessor.HasFields && !isDefaultValue)
            componentFieldAccessor.SetHasFields(true);
          Accessors.Setter(componentFieldAccessor._zdo.ZDO, _hash, value);
        }
        return true;
      }

      public ComponentFieldAccessor<TComponent> ResetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
      {
        if (!componentFieldAccessor.HasFields)
          return componentFieldAccessor;

        Accessors.Remover(componentFieldAccessor._zdo.ZDO, _hash);
        return componentFieldAccessor;
      }

      public bool UpdateResetValue(ComponentFieldAccessor<TComponent> componentFieldAccessor)
      {
        if (!componentFieldAccessor.HasFields)
          return false;
        
        return Accessors.Remover(componentFieldAccessor._zdo.ZDO, _hash);
      }
    }

    [MustBeOnUniqueLine]
    public bool GetBool(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

    [MustBeOnUniqueLine]
    public float GetFloat(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

    [MustBeOnUniqueLine]
    public int GetInt(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

    [MustBeOnUniqueLine]
    public string GetString(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).GetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, float value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, int value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, Vector3>>> fieldExpressionFactory, Vector3 value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<Vector3>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, string value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, GameObject value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Set(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, ItemDrop value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).SetValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, float value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, int value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, string value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, GameObject value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public bool UpdateValue(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, ItemDrop value, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, value);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);

    [MustBeOnUniqueLine]
    public ComponentFieldAccessor<TComponent> Reset(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).ResetValue(this);


    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool UpdateResetValue(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, bool>>> fieldExpressionFactory, bool set, bool setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<bool>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, float>>> fieldExpressionFactory, bool set, float setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<float>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, int>>> fieldExpressionFactory, bool set, int setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<int>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, string>>> fieldExpressionFactory, bool set, string setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<string>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, GameObject>>> fieldExpressionFactory, bool set, GameObject setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<GameObject>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);

    [MustBeOnUniqueLine]
    public bool SetOrReset(Func<Expression<Func<TComponent, ItemDrop>>> fieldExpressionFactory, bool set, ItemDrop setValue, [CallerFilePath] string callerFilePath = default!, [CallerLineNumber] int callerLineNo = -1)
        => set ? FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateValue(this, setValue) : FieldReference<ItemDrop>.Get(_component.GetType(), fieldExpressionFactory, callerFilePath, callerLineNo).UpdateResetValue(this);
  }
}
