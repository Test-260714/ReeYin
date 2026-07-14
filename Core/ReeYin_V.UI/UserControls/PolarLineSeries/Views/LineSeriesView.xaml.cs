using System;
using System.Windows.Controls;

namespace ReeYin_V.UI.UserControls.PolarLineSeries
{
    /// <summary>
    /// LineSeriesView.xaml 的交互逻辑
    /// </summary>
    public partial class LineSeriesView : UserControl
    {
        public LineSeriesView()
        {
            InitializeComponent();
            DataContext = new LineSeriesViewModel();
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 带图表ID的构造函数
        /// </summary>
        public LineSeriesView(string chartId)
        {
            InitializeComponent();
            DataContext = new LineSeriesViewModel(chartId);
            Unloaded += OnUnloaded;
        }

        /// <summary>
        /// 获取 ViewModel
        /// </summary>
        public LineSeriesViewModel ViewModel => DataContext as LineSeriesViewModel;

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.Dispose();
        }
    }
}
