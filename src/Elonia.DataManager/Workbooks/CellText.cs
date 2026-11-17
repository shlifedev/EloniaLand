using System;
using System.Globalization;

namespace Elonia.DataManager
{
    public static class CellText
    {
        private const double MAX_EXACT_INTEGER = 9007199254740992.0;

        public static string FromValue(object? valueOrNull)
        {
            switch (valueOrNull)
            {
                case null:
                    return "";
                case string text:
                    return text;
                case double number:
                    return FromNumber(number);
                case bool bValue:
                    return bValue ? "true" : "false";
                case DateTime dateTime:
                    return FromDateTime(dateTime);
                case IFormattable formattable:
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return valueOrNull.ToString() ?? "";
            }
        }

        private static string FromNumber(double number)
        {
            bool bInteger = Math.Floor(number) == number && Math.Abs(number) < MAX_EXACT_INTEGER;
            if (bInteger)
            {
                return ((long)number).ToString(CultureInfo.InvariantCulture);
            }

            return number.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string FromDateTime(DateTime dateTime)
        {
            if (dateTime.TimeOfDay == TimeSpan.Zero)
            {
                return dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }
    }
}
