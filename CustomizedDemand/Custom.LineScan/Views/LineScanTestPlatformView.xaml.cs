using Custom.LineScan.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace Custom.LineScan.Views
{
    /// <summary>
    /// LineScanTestPlatformView.xaml 的交互逻辑
    /// </summary>
    public partial class LineScanTestPlatformView : UserControl
    {
        public LineScanTestPlatformView()
        {
            InitializeComponent();
        }

        private void JogLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LineScanTestPlatformViewModel vm)
            {
                vm.JogMove(false); // 负方向
            }
        }

        private void JogLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LineScanTestPlatformViewModel vm)
            {
                vm.JogStop();
            }
        }

        private void JogRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LineScanTestPlatformViewModel vm)
            {
                vm.JogMove(true); // 正方向
            }
        }

        private void JogRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is LineScanTestPlatformViewModel vm)
            {
                vm.JogStop();
            }
        }
    }
}
