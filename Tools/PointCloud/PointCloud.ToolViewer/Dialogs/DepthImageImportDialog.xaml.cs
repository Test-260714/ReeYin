using System.Windows;
using PointCloud.ToolViewer.Models;

namespace PointCloud.ToolViewer.Dialogs;

public partial class DepthImageImportDialog : Window
{
    public DepthImageImportDialog(DepthImageImportParameters? parameters = null)
    {
        InitializeComponent();
        Parameters = parameters?.Clone() ?? new DepthImageImportParameters();
        DataContext = this;
        UpdateInvalidValueEditor();
    }

    public DepthImageImportParameters Parameters { get; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Parameters.ToLoadOptions().Validate();
            DialogResult = true;
        }
        catch (ArgumentOutOfRangeException ex)
        {
            MessageBox.Show(
                this,
                ex.Message,
                "Invalid depth image parameters",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void UseInvalidValueCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        UpdateInvalidValueEditor();
    }

    private void UpdateInvalidValueEditor()
    {
        InvalidValueNumeric.IsEnabled = Parameters.UseInvalidValue;
    }
}
