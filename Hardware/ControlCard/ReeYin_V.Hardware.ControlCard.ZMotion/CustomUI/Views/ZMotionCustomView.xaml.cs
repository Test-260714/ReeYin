using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Views
{
    /// <summary>
    /// ZMotionCustomView.xaml 的交互逻辑
    /// </summary>
    public partial class ZMotionCustomView : UserControl
    {
        public ZMotionCustomView()
        {
            InitializeComponent();
        }

        private void JogLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ZMotionCustomViewModel vm)
            {
                vm.JogCommand.Execute("Start_Left");
            }
        }

        private void JogLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ZMotionCustomViewModel vm)
            {
                vm.JogCommand.Execute("Stop_Left");
            }
        }

        private void JogRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ZMotionCustomViewModel vm)
            {
                vm.JogCommand.Execute("Start_Right");
            }
        }

        private void JogRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ZMotionCustomViewModel vm)
            {
                vm.JogCommand.Execute("Stop_Right");
            }
        }
    }
}
