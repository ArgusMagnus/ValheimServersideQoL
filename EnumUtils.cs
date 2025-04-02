using System.Linq.Expressions;

namespace Valheim.ServersideQoL;

static class EnumUtils
{
    static class Generic<T> where T : unmanaged, Enum
    {
        public static Func<T, ulong> EnumToUInt64 { get; } = Expression.Lambda<Func<T, ulong>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(T)) is var par ? par : throw new Exception(), typeof(ulong)), par).Compile();
        public static Func<ulong, T> UInt64ToEnum { get; } = Expression.Lambda<Func<ulong, T>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(ulong)) is var par ? par : throw new Exception(), typeof(T)), par).Compile();
        public static Func<T, long> EnumToInt64 { get; } = Expression.Lambda<Func<T, long>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(T)) is var par ? par : throw new Exception(), typeof(long)), par).Compile();
        public static Func<long, T> Int64ToEnum { get; } = Expression.Lambda<Func<long, T>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(long)) is var par ? par : throw new Exception(), typeof(T)), par).Compile();
    }

    public static ulong ToUInt64<T>(this T value) where T : unmanaged, Enum => Generic<T>.EnumToUInt64(value);
    public static T ToEnum<T>(ulong value) where T : unmanaged, Enum => Generic<T>.UInt64ToEnum(value);
    public static long ToInt64<T>(this T value) where T : unmanaged, Enum => Generic<T>.EnumToInt64(value);
    public static T ToEnum<T>(long value) where T : unmanaged, Enum => Generic<T>.Int64ToEnum(value);
}