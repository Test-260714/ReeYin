using System.Windows.Controls;
using System.Windows.Threading;
using Prism.Navigation.Regions;
using ReeYin.AlarmCenter.ViewModels;

namespace ReeYin.AlarmCenter.Views
{
    public partial class AlarmWorkbenchShellView : UserControl
    {
        public AlarmWorkbenchShellView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            if (DataContext is AlarmWorkbenchShellViewModel viewModel)
            {
                IRegionManager? regionManager = RegionManager.GetRegionManager(this);
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    if (regionManager != null)
                    {
                        viewModel.AttachContentRegionManager(regionManager);
                        return;
                    }

                    viewModel.EnsureInitialContentNavigation();
                }), DispatcherPriority.Loaded);
            }
        }
    }
}
