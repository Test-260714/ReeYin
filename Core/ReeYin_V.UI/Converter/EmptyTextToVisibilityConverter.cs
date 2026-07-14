using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ReeYin_V.UI.Converter
{
    public class EmptyTextToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 文本为空时返回 Visible，否则返回 Hidden
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string text = value?.ToString() ?? string.Empty;
            return string.IsNullOrEmpty(text.Trim()) ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// 反向转换（非必需，根据需求实现）
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
