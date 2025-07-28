using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Valheim.ServersideQoL;

static class EnumUtils
{
    static readonly ConcurrentDictionary<Type, ObjectEnumUtils> __isBitSet = new();
    static class Generic<T> where T : unmanaged, Enum
    {
        public static bool IsBitSet { get; } = EnumUtils.OfType(typeof(T)).IsBitSet;

        public static Func<T, ulong> EnumToUInt64 { get; } = Expression.Lambda<Func<T, ulong>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(T)) is var par ? par : throw new Exception(), typeof(ulong)), par).Compile();
        public static Func<ulong, T> UInt64ToEnum { get; } = Expression.Lambda<Func<ulong, T>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(ulong)) is var par ? par : throw new Exception(), typeof(T)), par).Compile();
        public static Func<T, long> EnumToInt64 { get; } = Expression.Lambda<Func<T, long>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(T)) is var par ? par : throw new Exception(), typeof(long)), par).Compile();
        public static Func<long, T> Int64ToEnum { get; } = Expression.Lambda<Func<long, T>>(
            Expression.ConvertChecked(Expression.Parameter(typeof(long)) is var par ? par : throw new Exception(), typeof(T)), par).Compile();
    }

    public static ObjectEnumUtils OfType(Type type) => __isBitSet.GetOrAdd(type, static t => new(t));
    public static bool IsBitSet<T>() where T : unmanaged, Enum => Generic<T>.IsBitSet;
    public static ulong ToUInt64<T>(this T value) where T : unmanaged, Enum => Generic<T>.EnumToUInt64(value);
    public static T ToEnum<T>(ulong value) where T : unmanaged, Enum => Generic<T>.UInt64ToEnum(value);
    public static long ToInt64<T>(this T value) where T : unmanaged, Enum => Generic<T>.EnumToInt64(value);
    public static T ToEnum<T>(long value) where T : unmanaged, Enum => Generic<T>.Int64ToEnum(value);

    public static bool ExactlyOneBitSet<T>(this T value) where T : unmanaged, Enum
    {
        var set = false;
        var n = value.ToUInt64();
        while (n is not 0)
        {
            if (set)
                return false;
            set = true;
            n &= (n - 1); // Clears the lowest set bit
        }
        return set;
    }

    public sealed class ObjectEnumUtils(Type enumType)
    {
        public bool IsBitSet { get; } = enumType.GetCustomAttribute<FlagsAttribute>() is not null;

        public Func<object, ulong> EnumToUInt64 { get; } = Expression.Lambda<Func<object, ulong>>(
            Expression.ConvertChecked(Expression.Convert(Expression.Parameter(typeof(object)) is var par ? par : throw new Exception(), enumType), typeof(ulong)), par).Compile();
        public Func<ulong, object> UInt64ToEnum { get; } = Expression.Lambda<Func<ulong, object>>(
            Expression.Convert(Expression.ConvertChecked(Expression.Parameter(typeof(ulong)) is var par ? par : throw new Exception(), enumType), typeof(object)), par).Compile();
        public Func<object, long> EnumToInt64 { get; } = Expression.Lambda<Func<object, long>>(
            Expression.ConvertChecked(Expression.Convert(Expression.Parameter(typeof(object)) is var par ? par : throw new Exception(), enumType), typeof(long)), par).Compile();
        public Func<long, object> Int64ToEnum { get; } = Expression.Lambda<Func<long, object>>(
            Expression.Convert(Expression.ConvertChecked(Expression.Parameter(typeof(long)) is var par ? par : throw new Exception(), enumType), typeof(object)), par).Compile();
    }
}