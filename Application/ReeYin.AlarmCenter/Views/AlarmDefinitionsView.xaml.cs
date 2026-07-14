using System.Windows.Controls;
using ReeYin.AlarmCenter.ViewModels;

namespace ReeYin.AlarmCenter.Views
{
    public partial class AlarmDefinitionsView : UserControl
    {
        public AlarmDefinitionsView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AlarmDefinitionsViewModel viewModel)
            {
                await viewModel.EnsureLoadedAsync().ConfigureAwait(true);
            }
        }
    }
}
