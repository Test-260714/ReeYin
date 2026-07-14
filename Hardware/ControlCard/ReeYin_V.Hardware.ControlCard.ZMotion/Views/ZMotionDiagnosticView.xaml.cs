using ReeYin_V.Hardware.ControlCard.ZMotion.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.Views
{
    public partial class ZMotionDiagnosticView : UserControl
    {
        private Window? hostWindow;

        public ZMotionDiagnosticView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            hostWindow = Window.GetWindow(this);
            if (hostWindow != null)
            {
                hostWindow.Deactivated += OnHostWindowDeactivated;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            StopPageJog();
            if (hostWindow != null)
            {
                hostWindow.Deactivated -= OnHostWindowDeactivated;
                hostWindow = null;
            }
        }

        private void OnHostWindowDeactivated(object? sender, EventArgs e)
        {
            StopPageJog();
        }

        private void StopPageJog()
        {
            if (DataContext is ZMotionDiagnosticViewModel viewModel
                && viewModel.StopJogCommand.CanExecute())
            {
                viewModel.StopJogCommand.Execute();
            }
        }
    }
}
