using ReeYin_V.Hardware.ControlCard.ZMotion.App;
using ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.ViewModels;
using System;
using System.Windows;
using System.Windows.Input;

namespace ReeYin_V.Hardware.ControlCard.ZMotion.CustomUI.Views
{
    /// <summary>
    /// MotionCardSettingsView.xaml 的交互逻辑
    /// </summary>
    public partial class MotionCardSettingsView : Window
    {
        private MotionCardSettingsViewModel _viewModel;

        public MotionCardSettingsView(IntPtr handle)
        {
            InitializeComponent();
            _viewModel = new MotionCardSettingsViewModel();
            DataContext = _viewModel;
            _viewModel.SetHandle(handle);
        }

        public MotionCardSettingsView(ZMotionControlCard controlCard)
            : this(controlCard?.Handle ?? IntPtr.Zero)
        {
            if (controlCard != null)
            {
                _viewModel.SetControlCard(controlCard);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void JogLeft_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.JogCommand.Execute("Start_Left");
        }

        private void JogLeft_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel.JogCommand.Execute("Stop_Left");
        }

        private void JogRight_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _viewModel.JogCommand.Execute("Start_Right");
        }

        private void JogRight_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _viewModel.JogCommand.Execute("Stop_Right");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ReleaseOwnedConnection();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel?.ReleaseOwnedConnection();
            base.OnClosed(e);
        }
    }
}
