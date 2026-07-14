using ALGO.RegionProcessing.ViewModels;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace ALGO.RegionProcessing.Views
{
    /// <summary>
    /// RegionProcessingView.xaml 的交互逻辑
    /// </summary>
    public partial class RegionProcessingView : UserControl
    {
        public RegionProcessingView()
        {
            InitializeComponent();
        }

        private void AddButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;
            OpenAddMethodMenu(sender as Button);
        }

        private static void OpenAddMethodMenu(Button? button)
        {
            if (button?.ContextMenu == null)
                return;

            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            button.ContextMenu.IsOpen = true;
        }
    }

    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            if (value is Enum enumValue)
            {
                string desc = GetEnumDescription(enumValue);

                // 描述格式类似 "差集|2|1"
                var parts = desc.Split('|');
                if (parts.Length >= 2)
                {
                    // 第二段是输入区域数量
                    if (int.TryParse(parts[1], out int inputCount))
                    {
                        // 如果输入区域数量==2 就显示，否则隐藏
                        return inputCount == 2 ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }

            return Visibility.Collapsed;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private string GetEnumDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                  .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? enumValue.ToString();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Binding.DoNothing;
        }
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

    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum e)
            {
                FieldInfo fi = e.GetType().GetField(e.ToString());
                var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attributes.Length > 0)
                    return attributes[0].Description.Split('|')[0];
                return e.ToString();
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
