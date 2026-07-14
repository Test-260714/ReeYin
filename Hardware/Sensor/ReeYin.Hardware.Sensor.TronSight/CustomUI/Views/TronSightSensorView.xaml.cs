using ReeYin.Hardware.Sensor.TronSight.CustomUI.ViewModels;
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

namespace ReeYin.Hardware.Sensor.TronSight.CustomUI.Views
{
    /// <summary>
    /// TronSightSensorView.xaml 的交互逻辑
    /// </summary>
    public partial class TronSightSensorView : UserControl
    {
        public TronSightSensorView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 折射率表选择模式被选中时
        /// </summary>
        private void RefractiveSelectMode_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is TronSightSensorViewModel vm)
            {
                vm.ModelParam.RefractiveConfig.IsEditMode = false;
            }
        }

        
    }
}
