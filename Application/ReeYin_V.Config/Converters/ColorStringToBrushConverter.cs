using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ReeYin_V.Config.Converters
{
    public sealed class ColorStringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text && TryParseColor(text, out var color))
            {
                return new SolidColorBrush(color);
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush solidColorBrush)
            {
                return ToColorString(solidColorBrush.Color);
            }

            if (value is Color color)
            {
                return ToColorString(color);
            }

            return Binding.DoNothing;
        }

        private static string ToColorString(Color color)
        {
            return color.A == 255
                ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
                : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private static bool TryParseColor(string value, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith("{", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                var converted = ColorConverter.ConvertFromString(value.Trim());
                if (converted is Color parsed)
                {
                    color = parsed;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
