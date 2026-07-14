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

namespace ReeYin_V.Hardware.ControlCard.Views
{
    /// <summary>
    /// ControlCardConfigView.xaml 的交互逻辑
    /// </summary>
    public partial class ControlCardConfigView : UserControl
    {
        public ControlCardConfigView()
        {
            InitializeComponent();
        }

        private void PropertyGrid_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
