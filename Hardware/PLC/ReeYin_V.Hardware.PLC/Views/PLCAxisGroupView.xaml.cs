using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin_V.Hardware.PLC.Views
{
    public partial class PLCAxisGroupView : UserControl
    {
        public PLCAxisGroupView()
        {
            InitializeComponent();
        }

        private void AxisDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DrawerRight.IsOpen = true;
        }

        private void AxisEditButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DrawerRight.IsOpen = true;
        }

        private void DrawerCloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DrawerRight.IsOpen = false;
        }
    }
}
