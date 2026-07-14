using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace ReeYin.ChartShow.Views
{
    /// <summary>
    /// PointCloud.xaml µÄ½»»¥Âß¼­
    /// </summary>
    public partial class PointCloud : UserControl
    {
        public PointCloud()
        {
            InitializeComponent();
        }

        private async void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Select Point Cloud File",
                Filter = "Point Cloud|*.ply;*.xyz;*.txt;*.csv;*.obj|PLY|*.ply|XYZ|*.xyz;*.txt;*.csv|OBJ|*.obj|All Files|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await pointCloudDisplay.LoadPointCloudFileAsync(dialog.FileName);
        }
    }
}
