using ImageTool.VTKPCDisplay;
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

namespace ReeYin.ChartShow.Views
{
    /// <summary>
    /// VTKPointCloudView.xaml 的交互逻辑
    /// </summary>
    public partial class VTKPointCloudView : System.Windows.Controls.UserControl
    {
        #region Fields
        public VTKPCDisplay mVTKPCDisplay { set; get; } = new VTKPCDisplay();
        #endregion


        public VTKPointCloudView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.winFormHost.Child = mVTKPCDisplay;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            mVTKPCDisplay.LoadPointCloudFile("Ply");
        }
    }
}
