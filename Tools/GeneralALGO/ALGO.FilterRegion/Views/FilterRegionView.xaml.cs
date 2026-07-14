using System.Windows;
using System.Windows.Controls;

namespace ALGO.FilterRegion.Views
{
    /// <summary>
    /// FilterRegionView.xaml 的交互逻辑
    /// </summary>
    public partial class FilterRegionView : UserControl
    {
        public FilterRegionView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 预览已迁移至 FilterRegionPreviewControl，此处无需操作
        }
    }
}
