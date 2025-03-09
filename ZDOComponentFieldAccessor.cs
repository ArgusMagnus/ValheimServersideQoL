using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace Valheim.ServersideQoL;

readonly struct ZDOComponentFieldAccessor<TComponent>(ZDO zdo) where TComponent : Component
{
    static readonly int _hasFields = ZNetView.CustomFieldsStr.GetStableHashCode();
    static readonly int _hasComponentFields = $"{ZNetView.CustomFieldsStr}{typeof(TComponent).Name}".GetStableHashCode();

    readonly ZDO _zdo = zdo;

    public bool GetHasFields() => _zdo.GetBool(_hasFields) && _zdo.GetBool(_hasComponentFields);
    public ZDOComponentFieldAccessor<TComponent> SetHasFields(bool value)
    {
        if (value)
            _zdo.Set(_hasFields, value);
        _zdo.Set(_hasComponentFields, value);
        return this;
    }

    static int GetHash<T>(Expression<Func<TComponent, T>> fieldExpression)
    {
        var body = (MemberExpression)fieldExpression.Body;
        var field = (FieldInfo)body.Member;
        return $"{typeof(TComponent).Name}.{field.Name}".GetStableHashCode();
    }

    public bool GetBool(Expression<Func<TComponent, bool>> fieldExpression, bool defaultValue = default)
        => _zdo.GetBool(GetHash(fieldExpression), defaultValue);

    public float GetFloat(Expression<Func<TComponent, float>> fieldExpression, float defaultValue = default)
        => _zdo.GetFloat(GetHash(fieldExpression), defaultValue);

    public int GetInt(Expression<Func<TComponent, int>> fieldExpression, int defaultValue = default)
        => _zdo.GetInt(GetHash(fieldExpression), defaultValue);

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, bool>> fieldExpression, bool value)
    {
        _zdo.Set(GetHash(fieldExpression), value);
        return this;
    }

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, float>> fieldExpression, float value)
    {
        _zdo.Set(GetHash(fieldExpression), value);
        return this;
    }

    public ZDOComponentFieldAccessor<TComponent> Set(Expression<Func<TComponent, int>> fieldExpression, int value)
    {
        _zdo.Set(GetHash(fieldExpression), value);
        return this;
    }
}