using HardWareTool.PLC.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace HardWareTool.PLC.Views
{
    public partial class PLCView : UserControl
    {
        public PLCView()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                if (DataContext is PLCViewModel vm)
                    vm.RegionManager = RegionManager.GetRegionManager(this);
            };
        }

        private void PropertyGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
        }
    }
}
