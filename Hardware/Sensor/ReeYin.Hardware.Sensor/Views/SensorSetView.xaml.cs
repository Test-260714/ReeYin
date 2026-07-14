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

namespace ReeYin.Hardware.Sensor.Views
{
    /// <summary>
    /// SensorSetView.xaml 的交互逻辑
    /// </summary>
    public partial class SensorSetView : UserControl
    {
        public SensorSetView(IRegionManager regionManager)
        {
            InitializeComponent();
        }

        private void PropertyGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
