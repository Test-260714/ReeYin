using Prism.Navigation.Regions;
using System.Windows.Controls;
using System.Windows.Input;

namespace ReeYin.AlarmCenter.Views
{
    public partial class AlarmStatisticsView : UserControl, INavigationAware, IRegionMemberLifetime
    {
        public AlarmStatisticsView()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
        }

        public bool KeepAlive => true;

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
        }

        private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Mouse.Capture(null);
        }
    }
}
