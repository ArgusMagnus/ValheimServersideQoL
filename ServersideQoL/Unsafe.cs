using System.Runtime.CompilerServices;

namespace ServersideQoL;

static unsafe class Unsafe
{
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int SizeOf<T>() where T : unmanaged => sizeof(T);

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static ref TTo As<TFrom, TTo>(ref TFrom source)
    where TFrom : unmanaged
    where TTo : unmanaged
  {
    fixed (TFrom* p = &source)
      return ref *(TTo*)p;
  }
}
