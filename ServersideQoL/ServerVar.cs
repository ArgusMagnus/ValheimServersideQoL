using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ServersideQoL;

/// <summary>
/// The point of this class is to (eventually) have an easy hook for optimizing server-only vars (which clients never need),
/// such as not sending them to clients to safe band-width.
/// </summary>
public abstract class ServerVar<T>
{
  private protected ServerVar() { }

  public abstract void Set(ServersideQoLZDO zdo, T value);
  public abstract T? Get(ServersideQoLZDO zdo, T? defaultValue = default);
  public abstract bool Remove(ServersideQoLZDO zdo);
}

static class ServerVar
{
  public static ServerVar<T> Create<T>(string name)
  {
    var type = typeof(T);
    object result;
    if (type == typeof(bool))
      result = new ServerVarBool(name);
    else if (type == typeof(int))
      result = new ServerVarInt(name);
    else if (type == typeof(long))
      result = new ServerVarLong(name);
    else if (type == typeof(float))
      result = new ServerVarLong(name);
    else if (type == typeof(Vector3))
      result = new ServerVarVec3(name);
    else if (type == typeof(Quaternion))
      result = new ServerVarQuaternion(name);
    else if (type == typeof(string))
      result = new ServerVarString(name);
    else if (type == typeof(byte[]))
      result = new ServerVarByteArray(name);
    else if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
      result = typeof(ServerVarCollection<int, List<int>>).GetGenericTypeDefinition().MakeGenericType(type.GetGenericArguments()[0], type).GetConstructor([typeof(string)]).Invoke(parameters: [name]);
    else
      result = ((Delegate)CreateStruct<int>).Method.GetGenericMethodDefinition().MakeGenericMethod(typeof(T)).Invoke(null, parameters: [name]);
    return (ServerVar<T>)result;
  }

  static ServerVar<T> CreateStruct<T>(string name)
    where T : unmanaged
  {
    switch (Unsafe.SizeOf<T>())
    {
      case sizeof(int): return new ServerVarStruct32<T>(name);
      case sizeof(long): return new ServerVarStruct64<T>(name);
      default: return new ServerVarStruct<T>(name);
    }
  }

#pragma warning disable CS0618 // Type or member is obsolete
  sealed class ServerVarBool(string name) : Processor.ServerVarCore<bool>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, bool value) => zdo.ZDO.Set(_hash, value);
    public override bool Get(ServersideQoLZDO zdo, bool defaultValue = default) => zdo.ZDO.GetBool(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveBool(_hash);
  }

  sealed class ServerVarInt(string name) : Processor.ServerVarCore<int>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, int value) => zdo.ZDO.Set(_hash, value);
    public override int Get(ServersideQoLZDO zdo, int defaultValue = default) => zdo.ZDO.GetInt(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveInt(_hash);
  }

  sealed class ServerVarLong(string name) : Processor.ServerVarCore<long>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, long value) => zdo.ZDO.Set(_hash, value);
    public override long Get(ServersideQoLZDO zdo, long defaultValue = default) => zdo.ZDO.GetLong(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveLong(_hash);
  }

  sealed class ServerVarFloat(string name) : Processor.ServerVarCore<float>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, float value) => zdo.ZDO.Set(_hash, value);
    public override float Get(ServersideQoLZDO zdo, float defaultValue = default) => zdo.ZDO.GetFloat(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveFloat(_hash);
  }

  sealed class ServerVarVec3(string name) : Processor.ServerVarCore<Vector3>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, Vector3 value) => zdo.ZDO.Set(_hash, value);
    public override Vector3 Get(ServersideQoLZDO zdo, Vector3 defaultValue = default) => zdo.ZDO.GetVec3(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveVec3(_hash);
  }

  sealed class ServerVarQuaternion(string name) : Processor.ServerVarCore<Quaternion>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, Quaternion value) => zdo.ZDO.Set(_hash, value);
    public override Quaternion Get(ServersideQoLZDO zdo, Quaternion defaultValue = default) => zdo.ZDO.GetQuaternion(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveQuaternion(_hash);
  }

  sealed class ServerVarString(string name) : Processor.ServerVarCore<string>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, string value) => zdo.ZDO.Set(_hash, value);
    public override string? Get(ServersideQoLZDO zdo, string? defaultValue = default) => zdo.ZDO.GetString(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveString(_hash);
  }

  sealed class ServerVarByteArray(string name) : Processor.ServerVarCore<byte[]>(name)
  {
    protected override void SetCore(ServersideQoLZDO zdo, byte[] value) => zdo.ZDO.Set(_hash, value);
    public override byte[]? Get(ServersideQoLZDO zdo, byte[]? defaultValue = null) => zdo.ZDO.GetByteArray(_hash, defaultValue);
    public override bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveByteArray(_hash);
  }
#pragma warning restore CS0618 // Type or member is obsolete

  sealed class ServerVarStruct32<T>(string name) : ServerVar<T>
    where T : unmanaged
  {
    readonly ServerVarInt _var = new(name);

    public override T Get(ServersideQoLZDO zdo, T defaultValue = default)
    {
      ref var value = ref Unsafe.As<T, int>(ref defaultValue);
      value = _var.Get(zdo, value);
      return defaultValue;
    }

    public override bool Remove(ServersideQoLZDO zdo) => _var.Remove(zdo);
    public override void Set(ServersideQoLZDO zdo, T value) => _var.Set(zdo, Unsafe.As<T, int>(ref value));
  }

  sealed class ServerVarStruct64<T>(string name) : ServerVar<T>
    where T : unmanaged
  {
    readonly ServerVarLong _var = new(name);

    public override T Get(ServersideQoLZDO zdo, T defaultValue = default)
    {
      ref var value = ref Unsafe.As<T, long>(ref defaultValue);
      value = _var.Get(zdo, value);
      return defaultValue;
    }

    public override bool Remove(ServersideQoLZDO zdo) => _var.Remove(zdo);
    public override void Set(ServersideQoLZDO zdo, T value) => _var.Set(zdo, Unsafe.As<T, long>(ref value));
  }

  sealed class ServerVarStruct<T>(string name) : ServerVar<T>
    where T : unmanaged
  {
    readonly ServerVarByteArray _var = new(name);

    public override T Get(ServersideQoLZDO zdo, T defaultValue = default)
    {
      if (_var.Get(zdo) is not { Length: > 0 } bytes)
        return defaultValue;
      return Unsafe.As<byte, T>(ref bytes[0]);
    }

    public override bool Remove(ServersideQoLZDO zdo) => _var.Remove(zdo);

    public override void Set(ServersideQoLZDO zdo, T value)
    {
      var bytes = new byte[Unsafe.SizeOf<T>()];
      Unsafe.As<byte, T>(ref bytes[0]) = value;
      _var.Set(zdo, bytes);
    }
  }

  sealed class ServerVarCollection<T, TCollection>(string name) : ServerVar<TCollection>
    where T : unmanaged
    where TCollection : ICollection<T>, new()
  {
    readonly ServerVarByteArray _var = new(name);

    public override TCollection? Get(ServersideQoLZDO zdo, TCollection? defaultValue = default)
    {
      if (_var.Get(zdo) is not { Length: > 0 } bytes)
        return defaultValue;
      return [.. MemoryMarshal.Cast<byte, T>(bytes.AsSpan())];
    }

    public override bool Remove(ServersideQoLZDO zdo) => _var.Remove(zdo);

    public override void Set(ServersideQoLZDO zdo, TCollection value)
    {
        var bytes = new byte[value.Count * Unsafe.SizeOf<T>()];
      var span = MemoryMarshal.Cast<byte, T>(bytes.AsSpan());
      foreach (var item in value)
      {
        span[0] = item;
        span = span[1..];
      }
      _var.Set(zdo, bytes);
    }
  }
}
