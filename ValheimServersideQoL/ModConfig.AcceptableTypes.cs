using BepInEx.Configuration;

namespace Valheim.ServersideQoL;

partial record ModConfigBase
{
    internal sealed class AcceptableEnum<T> : AcceptableValueBase
        where T : unmanaged, Enum
    {
        public static AcceptableEnum<T> Default { get; } = new(GetDefaultValues());

        public IReadOnlyList<T> AcceptableValues { get; }
        readonly T _default;

        static IEnumerable<T> GetDefaultValues()
        {
            var added = new HashSet<T>();
            foreach (var value in (T[])Enum.GetValues(typeof(T)))
            {
                // Filter out duplicate (obsolete) values
                if (added.Add(value))
                    yield return value;
            }
        }

        public AcceptableEnum(IEnumerable<T> values)
        : base(typeof(T))
        {
            if (EnumUtils.IsBitSet<T>())
            {
                AcceptableValues = [.. values.Where(static x => x.ExactlyOneBitSet())];
                _default = default;
            }
            else
            {
                AcceptableValues = values as IReadOnlyList<T> ?? [.. values];
                _default = AcceptableValues.FirstOrDefault();
            }
        }

        public override object Clamp(object value)
        {
            if (value is not T e)
                return _default;

            if (EnumUtils.IsBitSet<T>())
            {
                var val = e.ToUInt64();
                ulong result = 0;
                foreach (var flag in AcceptableValues.Select(static x => x.ToUInt64()).Where(x => (val & x) == x))
                    result |= flag;
                return EnumUtils.ToEnum<T>(result);
            }
            else if (!AcceptableValues.Any(x => x.Equals(e)))
            {
                return _default;
            }
            return e;
        }

        public override bool IsValid(object value)
        {
            return Equals(value, Clamp(value));
        }

        public override string ToDescriptionString()
        {
            if (EnumUtils.IsBitSet<T>())
                return Invariant($"# Acceptable values: {_default} or combination of {string.Join(", ", AcceptableValues.Where(x => !x.Equals(_default)))}");
            else
                return Invariant($"# Acceptable values: {string.Join(", ", AcceptableValues)}");
        }
    }

    sealed class AcceptableFormatString(object[] testArgs) : AcceptableValueBase(typeof(string))
    {
        public override bool IsValid(object value)
        {
            if (value is not string format)
                return false;

            try { string.Format(format, testArgs); }
            catch (FormatException) { return false; }
            return true;
        }

        public override object Clamp(object value) => value;

        public override string ToDescriptionString()
        => Invariant($"# Acceptable values: .NET Format strings for {testArgs.Length} arguments ({string.Join(", ", testArgs.Select(static x => x.GetType().Name))}): https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-string-format#get-started-with-the-stringformat-method");
    }
}