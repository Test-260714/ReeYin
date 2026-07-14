using System.Windows;
using HandyControl.Controls;
using HandyControl.Data;
using PointCloud.ToolViewer.Models;

namespace PointCloud.ToolViewer.Dialogs;

public partial class ScalePointCloudDialog : System.Windows.Window
{
    public ScalePointCloudDialog()
    {
        InitializeComponent();
        Parameters = new PointCloudScaleParameters();
        DataContext = this;
        ApplyParametersToEditors();
        UpdateAxisEditors();
    }

    public PointCloudScaleParameters Parameters { get; private set; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SyncLinkedAxesFromX();
        DialogResult = true;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        Parameters = PointCloudScaleParameters.CreateResetResult();
        DialogResult = true;
    }

    private void SameScaleCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        SyncLinkedAxesFromX();
    }

    private void SameScaleCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        UpdateAxisEditors();
    }

    private void ScaleXNumeric_ValueChanged(object sender, FunctionEventArgs<double> e)
    {
        if (Parameters.SameScaleForAllDimensions)
        {
            SyncLinkedAxesFromX();
        }
    }

    private void SyncLinkedAxesFromX()
    {
        Parameters.ScaleX = ScaleXNumeric.Value;
        Parameters.SyncLinkedAxes();
        ApplyParametersToEditors();
        UpdateAxisEditors();
    }

    private void ApplyParametersToEditors()
    {
        if (Parameters.SameScaleForAllDimensions)
        {
            Parameters.SyncLinkedAxes();
        }

        ScaleXNumeric.Value = Parameters.ScaleX;
        ScaleYNumeric.Value = Parameters.ScaleY;
        ScaleZNumeric.Value = Parameters.ScaleZ;
    }

    private void UpdateAxisEditors()
    {
        bool allowIndividualAxes = !Parameters.SameScaleForAllDimensions;
        ScaleYNumeric.IsEnabled = allowIndividualAxes;
        ScaleZNumeric.IsEnabled = allowIndividualAxes;
    }
}
