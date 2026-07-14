using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Nodify.FlowApp
{
    public class ConnectorOffsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double offset = System.Convert.ToDouble(parameter);
            if (value is Size s)
            {
                return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
            }

            return new Size(offset / 2, offset / 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double offset = System.Convert.ToDouble(parameter);
            if (value is Size s)
            {
                return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
            }

            return new Size(offset / 2, offset / 2);
        }
    }
}
