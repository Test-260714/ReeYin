using Custom.EVEMFDJC.Models;
using ImageTool.Halcon;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Custom.EVEMFDJC.Views
{
    /// <summary>
    /// EveSensorDataCollectionView.xaml 的交互逻辑
    /// </summary>
    public partial class EveSensorDataCollectionView : UserControl
    {
        private VMHWindowControl? _imageWindow;

        public EveSensorDataCollectionView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _imageWindow = PrismProvider.ProjectManager.SltCurSolutionItem.ImgControlPair[EveSensorDataCollectionModel.ModuleName] as VMHWindowControl;
            if (_imageWindow != null)
                winFormHost.Child = _imageWindow;
        }

        private void ImagePreviewExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (winFormHost == null) return;
            winFormHost.Visibility = Visibility.Visible;
            if (winFormHost.Child == null && _imageWindow != null)
                winFormHost.Child = _imageWindow;
        }

        private void ImagePreviewExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            if (winFormHost == null) return;
            winFormHost.Visibility = Visibility.Collapsed;
        }
    }
}
