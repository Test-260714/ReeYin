using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Custom.LineScan.Converters
{
    /// <summary>
    /// 位置值转换为GridLength星号宽度
    /// ConverterParameter格式: "总距离" 如 "860"
    /// </summary>
    public class PositionToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double position = 0;
            if (value is float f) position = f;
            else if (value is double d) position = d;
            else if (value is int i) position = i;

            double maxDistance = 860;
            if (parameter is string param && double.TryParse(param, out double parsed))
            {
                maxDistance = parsed;
            }

            // 限制在0-maxDistance范围内
            position = Math.Max(0, Math.Min(position, maxDistance));
            
            // 返回星号宽度，位置为0时返回很小的值避免0*
            double starValue = Math.Max(position, 0.001);
            return new GridLength(starValue, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 计算剩余宽度（总距离 - 当前位置）
    /// </summary>
    public class RemainingWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double position = 0;
            if (values.Length > 0)
            {
                if (values[0] is float f) position = f;
                else if (values[0] is double d) position = d;
                else if (values[0] is int i) position = i;
            }

            double maxDistance = 860;
            if (parameter is string param && double.TryParse(param, out double parsed))
            {
                maxDistance = parsed;
            }

            // 限制在0-maxDistance范围内
            position = Math.Max(0, Math.Min(position, maxDistance));
            
            // 剩余宽度
            double remaining = maxDistance - position;
            double starValue = Math.Max(remaining, 0.001);
            return new GridLength(starValue, GridUnitType.Star);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
