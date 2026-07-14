using ALGO.RegionTrans.ViewModels;
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

namespace ALGO.RegionTrans.Views
{
    /// <summary>
    /// RegionTransView.xaml 的交互逻辑
    /// </summary>
    public partial class RegionTransView : UserControl
    {
        public RegionTransView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegionTransViewModel viewModel && viewModel.ModelParam != null)
            {
                viewModel.ModelParam.LoadKeyParam();
                viewModel.ModelParam.RequestInputImagePreviewRefresh();
                VMHWindowControl halconWindow = viewModel.ModelParam.mWindowH;
                if (halconWindow == null)
                {
                    return;
                }

                if (ReferenceEquals(winFormHost.Child, halconWindow))
                {
                    viewModel.ModelParam.RequestInputImagePreviewRefresh();
                    return;
                }

                if (halconWindow.Parent != null)
                {
                    halconWindow.Parent.Controls.Remove(halconWindow);
                }

                halconWindow.Dock = Forms.DockStyle.Fill;
                halconWindow.getHWindowControl().Dock = Forms.DockStyle.Fill;
                winFormHost.Child = halconWindow;
                viewModel.ModelParam.RequestInputImagePreviewRefresh();
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (winFormHost.Child is Forms.Control)
            {
                winFormHost.Child = null;
            }
        }
    }

    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
                return enumValue.GetDescription();
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
