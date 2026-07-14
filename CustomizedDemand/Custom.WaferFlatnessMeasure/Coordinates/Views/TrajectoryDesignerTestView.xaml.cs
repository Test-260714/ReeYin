using System.Windows;
using System.Windows.Controls;

namespace Custom.WaferFlatnessMeasure.Views
{
    public partial class TrajectoryDesignerTestView : UserControl
    {
        public TrajectoryDesignerTestView()
        {
            InitializeComponent();
        }

        private void CoordinateSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TrajectoryCoordinateSettingsWindow
            {
                Owner = Window.GetWindow(this),
                DataContext = DataContext
            };

            window.ShowDialog();
        }
    }
}
