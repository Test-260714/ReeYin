#nullable enable

using System.Windows;

namespace ReeYin_V.UI.UserControls.DefectMap
{
    public partial class DefectMapSettingsWindow : Window
    {
        public DefectMapSettingsWindow(DefectMapViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
