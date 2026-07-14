using Custom.WaferFlatnessMeasure.ViewModels;
using System.Windows;

namespace Custom.WaferFlatnessMeasure.Views
{
    public partial class TrajectoryDesignerEditWindow : Window
    {
        private TrajectoryDesignerEditViewModel? _viewModel;

        public TrajectoryDesignerEditWindow()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Closed += OnClosed;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= OnRequestClose;
            }

            _viewModel = e.NewValue as TrajectoryDesignerEditViewModel;
            if (_viewModel != null)
            {
                _viewModel.RequestClose += OnRequestClose;
            }
        }

        private void OnClosed(object? sender, System.EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.RequestClose -= OnRequestClose;
            }
        }

        private void OnRequestClose()
        {
            DialogResult = true;
            Close();
        }

        private void CoordinateSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TrajectoryCoordinateSettingsWindow
            {
                Owner = this,
                DataContext = DataContext
            };

            window.ShowDialog();
        }
    }
}
