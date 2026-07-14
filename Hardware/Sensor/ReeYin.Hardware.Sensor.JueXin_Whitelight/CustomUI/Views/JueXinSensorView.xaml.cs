using Arction.Wpf.ChartingMVVM;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin.Hardware.Sensor.JueXin.CustomUI.Views;

public partial class JueXinSensorView : UserControl
{
    public JueXinSensorView()
    {
        InitializeComponent();
    }

    private void LightningChart_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is LightningChart chart)
        {
            chart.Title.Visible = false;
        }
    }
}
