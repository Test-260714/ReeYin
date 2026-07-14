using Custom.WaferRoutePlan.ViewModels;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using static Custom.WaferRoutePlan.ViewModels.WaferRoutePlanViewModel;

namespace Custom.WaferRoutePlan.Views
{
    /// <summary>
    /// WaferRoutePlanView.xaml 的交互逻辑
    /// </summary>
    public partial class WaferRoutePlanView : UserControl
    {
        public WaferRoutePlanView()
        {
            InitializeComponent();
        }
    }

    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ScanTypes currentScanType)
                return Visibility.Collapsed;

            if (parameter is not string param || string.IsNullOrWhiteSpace(param))
                return Visibility.Collapsed;

            var allowedScanTypes = param
                .Split(',')
                .Select(p => p.Trim())
                .Select(p =>
                {
                    return Enum.TryParse<ScanTypes>(p, out var result)
                        ? (ScanTypes?)result
                        : null;
                })
                .Where(p => p.HasValue)
                .Select(p => p.Value);

            return allowedScanTypes.Contains(currentScanType)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class ParamTemplateSelector : DataTemplateSelector
    {
        public DataTemplate NumberTemplate { get; set; }
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate ComboBoxTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ParamDefinition param)
            {
                return param.UIType switch
                {
                    ParamUIType.Number => NumberTemplate,
                    ParamUIType.Text => TextTemplate,
                    ParamUIType.ComboBox => ComboBoxTemplate,
                    _ => TextTemplate
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}
