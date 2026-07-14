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

namespace HardwareTool.Motion.Views
{
    /// <summary>
    /// MotionView.xaml 的交互逻辑
    /// </summary>
    public partial class MotionView : UserControl
    {
        public MotionView()
        {
            InitializeComponent();
        }

        private void PropertyGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
