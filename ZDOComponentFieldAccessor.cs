using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Valheim.ServersideQoL;

sealed class ZDOComponentFieldAccessor<TComponent>(ZDO zdo, TComponent? component) where TComponent : MonoBehaviour
{
    static readonly int __hasFieldsHash = ZNetView.CustomFieldsStr.GetStableHashCode();
    static readonly int __hasComponentFieldsHash = $"{ZNetView.CustomFieldsStr}{typeof(TComponent).Name}".GetStableHashCode();

    readonly ZDO _zdo = zdo;
    readonly TComponent? _component = component;

    bool? _hasFields;
    bool? _hasComponentFields;

    bool HasFields => (_hasFields ??= _zdo.GetBool(__hasFieldsHash)) && (_hasComponentFields ??= _zdo.GetBool(__hasComponentFieldsHash));
    void SetHasFields(bool value)
    {
        if (value && _hasFields is not true)
            _zdo.Set(__hasFieldsHash, (_hasFields = true).Value);

        if (_hasComponentFields != value)
        _zdo.Set(__hasComponentFieldsHash, (_hasComponentFields = value).Value);        
    }

    static int GetHash<T>(Expression<Func<TComponent, T>> fieldExpression, out FieldInfo field)
    {
        var body = (MemberExpression)fieldExpression.Body;
        field = (FieldInfo)body.Member;
        return $"{typeof(TComponent).Name}.{field.Name}".GetStableHashCode();
    }

    T Get<T>(Expression<Func<TComponent, T>> fieldExpression, Func<ZDO, int, T?, T> getter)
    {
        var body = (MemberExpression)fieldExpression.Body;
        var field = (FieldInfo)body.Member;
        if (!HasFields)
            return (T)field.GetValue(_component);

        var hash = $"{typeof(TComponent).Name}.{field.Name}".GetStableHashCode();
        return getter(_zdo, hash, _component is null ? default : (T)field.GetValue(_component));
    }

    public bool GetBool(Expression<Func<TComponent, bool>> fieldExpression)
        => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetBool(hash, defaultValue));

    public float GetFloat(Expression<Func<TComponent, float>> fieldExpression)
        => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetFloat(hash, defaultValue));

    public int GetInt(Expression<Func<TComponent, int>> fieldExpression)
        => Get(fieldExpression, static (zdo, hash, defaultValue) => zdo.GetInt(hash, defaultValue));

    ZDOComponentFieldAccessor<TComponent> SetCore<T>(Expression<Func<TComponent, T>> fieldExpression, T value, Action<ZDO, int> remover, Action<ZDO, int, T> setter)
        where T : notnull
    {
        var hash = GetHash(fieldExpression, out var field);
        if (_component is not null && value.Equals(field.GetValue(_component)))
            remover(_zdo, hash);
        else
        {
            if (!HasFields)
                SetHasFields(true);
            setter(_zdo, hash, value);
        }
        return this;
    }

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, bool>> fieldExpression, bool value)
        => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, float>> fieldExpression, float value)
        => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveFloat(hash), static (zdo, hash, value) => zdo.Set(hash, value));

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, int>> fieldExpression, int value)
        => SetCore(fieldExpression, value, static (zdo, hash) => zdo.RemoveInt(hash), static (zdo, hash, value) => zdo.Set(hash, value));
}