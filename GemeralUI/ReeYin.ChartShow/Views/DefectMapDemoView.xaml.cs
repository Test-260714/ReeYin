using System;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin.ChartShow.Views
{
    /// <summary>
    /// 卷材缺陷映射模拟视图�?    /// </summary>
    public partial class DefectMapDemoView : UserControl
    {
        public DefectMapDemoView()
        {
            InitializeComponent();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
