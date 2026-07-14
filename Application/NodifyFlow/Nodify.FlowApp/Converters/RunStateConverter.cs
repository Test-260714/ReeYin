using Newtonsoft.Json.Linq;
using ReeYin_V.Core.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace Nodify.FlowApp
{
    public class RunStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            NodeStatus status = (NodeStatus)value;
            switch (status)
            {
                case NodeStatus.Success: return Brushes.Green;
                case NodeStatus.Running: return Brushes.Yellow;
                case NodeStatus.Waiting: return Brushes.Yellow;
                case NodeStatus.Failed: return Brushes.Red;
                case NodeStatus.Error: return Brushes.Red;
                case NodeStatus.NoParam: return Brushes.Red;
                case NodeStatus.Warning: return Brushes.Red;
                case NodeStatus.None: return Brushes.Gray;
                default: return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
