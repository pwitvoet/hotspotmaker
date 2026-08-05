using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace HotspotMaker.Util
{
    public class NullableDoubleConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue && targetType == typeof(string))
                return doubleValue.ToString(culture);

            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue) && targetType == typeof(double?))
                return double.Parse(stringValue, culture);

            return null;
        }
    }
}
