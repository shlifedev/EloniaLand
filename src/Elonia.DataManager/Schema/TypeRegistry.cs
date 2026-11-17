using System;
using System.Collections.Generic;
using System.Globalization;

namespace Elonia.DataManager
{
    public sealed class TypeRegistry
    {
        private readonly Dictionary<string, ScalarColumnType> _types = new Dictionary<string, ScalarColumnType>(StringComparer.Ordinal);

        public static TypeRegistry CreateDefault()
        {
            TypeRegistry registry = new TypeRegistry();
            registry.Add(CreateInteger<int>("int", int.TryParse, int.MinValue, int.MaxValue));
            registry.Add(CreateInteger<uint>("uint", uint.TryParse, uint.MinValue, uint.MaxValue));
            registry.Add(CreateInteger<long>("long", long.TryParse, long.MinValue, long.MaxValue));
            registry.Add(CreateInteger<ulong>("ulong", ulong.TryParse, ulong.MinValue, ulong.MaxValue));
            registry.Add(CreateInteger<short>("short", short.TryParse, short.MinValue, short.MaxValue));
            registry.Add(CreateInteger<ushort>("ushort", ushort.TryParse, ushort.MinValue, ushort.MaxValue));
            registry.Add(CreateInteger<byte>("byte", byte.TryParse, byte.MinValue, byte.MaxValue));
            registry.Add(new ScalarType<float>("float", 0.0f, TryParseFloat, "숫자가 아닙니다.", "1.5", false));
            registry.Add(new ScalarType<double>("double", 0.0, TryParseDouble, "숫자가 아닙니다.", "1.5", false));
            registry.Add(new ScalarType<decimal>("decimal", 0m, TryParseDecimal, "숫자가 아닙니다.", "1.5", false));
            registry.Add(new ScalarType<bool>("bool", false, TryParseBool, "참·거짓 값이 아닙니다. true, false, 1, 0 중 하나를 씁니다.", "true", false));
            registry.Add(new ScalarType<string>("string", "", TryParseString, "", "글자", true));
            registry.Add(new ScalarType<DateTime>("DateTime", default(DateTime), TryParseDateTime, "날짜가 아닙니다. 2026-01-31 또는 2026-01-31 13:30:00 형식으로 씁니다.", "2026-01-31", false));
            registry.Add(new ScalarType<Guid>("Guid", Guid.Empty, Guid.TryParse, "GUID가 아닙니다.", "3f2504e0-4f89-11d3-9a0c-0305e82c3301", true));
            return registry;
        }

        public void Add(ScalarColumnType type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (_types.ContainsKey(type.CanonicalName))
            {
                throw new ArgumentException($"The type '{type.CanonicalName}' is already registered.", nameof(type));
            }

            _types.Add(type.CanonicalName, type);
        }

        public ScalarColumnType? FindOrNull(string name)
        {
            return _types.TryGetValue(name, out ScalarColumnType? type) ? type : null;
        }

        public string? FindSimilarNameOrNull(string name)
        {
            foreach (string registered in _types.Keys)
            {
                if (string.Equals(registered, name, StringComparison.OrdinalIgnoreCase))
                {
                    return registered;
                }
            }

            return null;
        }

        public string GetNameList()
        {
            List<string> names = new List<string>(_types.Keys);
            names.Sort(StringComparer.Ordinal);
            return string.Join(", ", names);
        }

        private delegate bool TryParseInteger<T>(string text, NumberStyles styles, IFormatProvider provider, out T value);

        private static ScalarType<T> CreateInteger<T>(string name, TryParseInteger<T> tryParse, T min, T max)
            where T : struct, IFormattable
        {
            string range = min.ToString(null, CultureInfo.InvariantCulture) + " ~ " + max.ToString(null, CultureInfo.InvariantCulture);
            TryParseValue<T> parse = (string text, out T value) => tryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
            return new ScalarType<T>(name, default(T), parse, name + " 범위(" + range + ")의 정수가 아닙니다.", "10", true);
        }

        private static bool TryParseFloat(string text, out float value)
        {
            return float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool TryParseDouble(string text, out double value)
        {
            return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool TryParseDecimal(string text, out decimal value)
        {
            return decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseBool(string text, out bool bValue)
        {
            string trimmed = text.Trim();
            if (trimmed == "1" || string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase))
            {
                bValue = true;
                return true;
            }

            bValue = false;
            return trimmed == "0" || string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseString(string text, out string value)
        {
            value = text;
            return true;
        }

        private static bool TryParseDateTime(string text, out DateTime value)
        {
            return DateTime.TryParse(text.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value);
        }
    }
}
