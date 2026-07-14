using ALGO.CreatRegion.ViewModels;
using ImageTool.Halcon;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;

namespace ALGO.CreatRegion.Views
{
    /// <summary>
    /// CreatRegionView.xaml 的交互逻辑
    /// </summary>
    public partial class CreatRegionView : UserControl
    {
        public CreatRegionView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is CreatRegionViewModel viewModel && viewModel.ModelParam?.mWindowH != null)
            {
                VMHWindowControl halconWindow = viewModel.ModelParam.mWindowH;
                if (ReferenceEquals(this.winFormHost.Child, halconWindow))
                {
                    return;
                }

                if (halconWindow.Parent != null)
                {
                    halconWindow.Parent.Controls.Remove(halconWindow);
                }

                halconWindow.Dock = Forms.DockStyle.Fill;
                halconWindow.getHWindowControl().Dock = Forms.DockStyle.Fill;
                this.winFormHost.Child = halconWindow;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.winFormHost.Child is Forms.Control)
            {
                this.winFormHost.Child = null;
            }
        }

        private void WxNumericUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }
    }

    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Visibility.Collapsed;

            string enumDescription = GetEnumDescription((Enum)value);

            return enumDescription == parameter.ToString()
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string GetEnumDescription(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                  .FirstOrDefault() as DescriptionAttribute;
            return attribute?.Description ?? enumValue.ToString();
        }
    }
}
