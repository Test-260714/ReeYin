using ReeYin.Hardware.Sensor.ChroCodile.Models;
using ReeYin.Hardware.Sensor.ChroCodile.ViewModels;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace ReeYin.Hardware.Sensor.ChroCodile.Views
{
    /// <summary>
    /// ChroCodileSensorView.xaml 的交互逻辑
    /// </summary>
    public partial class ChroCodileSensorView : UserControl
    {
        public ChroCodileSensorView()
        {
            InitializeComponent();
        }

        private void CbSensorModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(CbSensorModel.SelectedItem.ToString() == "CHRMultiChannel")
            {
                tbOutputSignal.Text = "83,16640,16641";
            }
            else
            {
                tbOutputSignal.Text = "83,65,256,257";
            }
        }

        private void CbTriggerMode_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTriggerMode(true);
        }

        private void CbTriggerMode_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyTriggerMode(false);
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is ChroCodileSensorViewModel viewModel &&
                viewModel.ValueChangedCommand.CanExecute("手动设置宽度"))
            {
                viewModel.ValueChangedCommand.Execute("手动设置宽度");
            }
        }

        private void ApplyTriggerMode(bool enabled)
        {
            if (DataContext is not ChroCodileSensorViewModel viewModel)
            {
                return;
            }

            viewModel.ModelParam.ChroCodileSensor.CurrentConfig.TriggerModeEnabled = enabled;
        }
    }
}
