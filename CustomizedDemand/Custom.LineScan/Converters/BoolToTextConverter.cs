using System;
using System.Globalization;
using System.Windows.Data;

namespace Custom.LineScan.Converters
{
    /// <summary>
    /// Bool转文本转换器
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "运行中" : "已停止";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
