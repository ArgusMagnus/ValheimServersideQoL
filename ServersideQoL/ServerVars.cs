using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ServersideQoL;

public sealed class ServerVarBool(int hash) : Processor.ServerVar<bool>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, bool value) => zdo.ZDO.Set(_hash, value);
  public bool Get(ServersideQoLZDO zdo, bool defaultValue = default) => zdo.ZDO.GetBool(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveBool(_hash);
}

public sealed class ServerVarInt(int hash) : Processor.ServerVar<int>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, int value) => zdo.ZDO.Set(_hash, value);
  public int Get(ServersideQoLZDO zdo, int defaultValue = default) => zdo.ZDO.GetInt(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveInt(_hash);
}

public sealed class ServerVarLong(int hash) : Processor.ServerVar<long>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, long value) => zdo.ZDO.Set(_hash, value);
  public long Get(ServersideQoLZDO zdo, long defaultValue = default) => zdo.ZDO.GetLong(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveLong(_hash);
}

public sealed class ServerVarFloat(int hash) : Processor.ServerVar<float>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, float value) => zdo.ZDO.Set(_hash, value);
  public float Get(ServersideQoLZDO zdo, float defaultValue = default) => zdo.ZDO.GetFloat(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveFloat(_hash);
}

public sealed class ServerVarVec3(int hash) : Processor.ServerVar<Vector3>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, Vector3 value) => zdo.ZDO.Set(_hash, value);
  public Vector3 Get(ServersideQoLZDO zdo, Vector3 defaultValue = default) => zdo.ZDO.GetVec3(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveVec3(_hash);
}

public sealed class ServerVarQuaternion(int hash) : Processor.ServerVar<Quaternion>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, Quaternion value) => zdo.ZDO.Set(_hash, value);
  public Quaternion Get(ServersideQoLZDO zdo, Quaternion defaultValue = default) => zdo.ZDO.GetQuaternion(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveQuaternion(_hash);
}

public sealed class ServerVarString(int hash) : Processor.ServerVar<string>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, string value) => zdo.ZDO.Set(_hash, value);
  public string? Get(ServersideQoLZDO zdo, string? defaultValue = default) => zdo.ZDO.GetString(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveString(_hash);
}

public sealed class ServerVarByteArray(int hash) : Processor.ServerVar<byte[]>(hash)
{
  protected override void SetCore(ServersideQoLZDO zdo, byte[] value) => zdo.ZDO.Set(_hash, value);
  public byte[]? Get(ServersideQoLZDO zdo, byte[]? defaultValue = null) => zdo.ZDO.GetByteArray(_hash, defaultValue);
  public bool Remove(ServersideQoLZDO zdo) => zdo.ZDO.RemoveByteArray(_hash);
}

public sealed class ServerVarCollection<T>(int hash)
  where T : unmanaged
{
  readonly ServerVarByteArray _var = new(hash);

  public void Set(ServersideQoLZDO zdo, ReadOnlySpan<T> values) => _var.Set(zdo, MemoryMarshal.Cast<T, byte>(values).ToArray());
  
  public void Set<TCollection>(ServersideQoLZDO zdo, TCollection values)
    where TCollection : IReadOnlyCollection<T>
  {
    var bytes = new byte[values.Count * Unsafe.SizeOf<T>()];
    var span = MemoryMarshal.Cast<byte, T>(bytes.AsSpan());
    foreach (var item in values)
    {
      span[0] = item;
      span = span[1..];
    }
    _var.Set(zdo, bytes);
  }

  public void Get<TCollection>(ServersideQoLZDO zdo, TCollection dest)
    where TCollection : ICollection<T>
  {
    if (_var.Get(zdo) is not { Length: > 0 } bytes)
      return;

    foreach (var item in MemoryMarshal.Cast<byte, T>(bytes.AsSpan()))
      dest.Add(item);
  }

  public TCollection Get<TCollection>(ServersideQoLZDO zdo)
    where TCollection : ICollection<T>, new()
  {
    var dest = new TCollection();
    Get(zdo, dest);
    return dest;
  }

  public bool Remove(ServersideQoLZDO zdo) => _var.Remove(zdo);
}
