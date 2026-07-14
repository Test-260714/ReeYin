using System.Windows;

namespace Custom.WaferFlatnessMeasure.Views
{
    public partial class TrajectoryCoordinateSettingsWindow : Window
    {
        public TrajectoryCoordinateSettingsWindow()
        {
            InitializeComponent();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
