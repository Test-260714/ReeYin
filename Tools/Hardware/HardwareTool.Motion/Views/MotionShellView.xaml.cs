using HardwareTool.Motion.ViewModels;
using System.Windows.Controls;

namespace HardwareTool.Motion.Views
{
    public partial class MotionShellView : UserControl
    {
        public MotionShellView()
        {
            Loaded += (_, __) =>
            {
                if (DataContext is MotionViewModel vm)
                    vm.RegionManager = RegionManager.GetRegionManager(this);
            };

            InitializeComponent();
        }
    }
}
