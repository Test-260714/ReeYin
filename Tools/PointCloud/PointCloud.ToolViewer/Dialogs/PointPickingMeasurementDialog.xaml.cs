using System.Windows;
using System.Windows.Controls;
using PointCloud.VTKWPF.Models;

namespace PointCloud.ToolViewer.Dialogs;

public partial class PointPickingMeasurementDialog : Window
{
    public PointPickingMeasurementDialog(PointPickingMeasurementMode initialMode)
    {
        InitializeComponent();
        SelectedMode = initialMode;
        ApplyInitialSelection(initialMode);
    }

    public PointPickingMeasurementMode SelectedMode { get; private set; }

    public bool ClearRequested { get; private set; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedMode = GetSelectedMode();
        ClearRequested = false;
        DialogResult = true;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearRequested = true;
        DialogResult = true;
    }

    private PointPickingMeasurementMode GetSelectedMode()
    {
        RadioButton[] radios =
        {
            ModePointInfoRadio,
            ModeDistanceRadio,
            ModeAngleRadio,
            ModeNoneRadio,
        };

        foreach (RadioButton radio in radios)
        {
            if (radio.IsChecked == true && radio.Tag is PointPickingMeasurementMode mode)
            {
                return mode;
            }
        }

        return PointPickingMeasurementMode.None;
    }

    private void ApplyInitialSelection(PointPickingMeasurementMode initialMode)
    {
        switch (initialMode)
        {
            case PointPickingMeasurementMode.PointInfo:
                ModePointInfoRadio.IsChecked = true;
                break;
            case PointPickingMeasurementMode.Distance:
                ModeDistanceRadio.IsChecked = true;
                break;
            case PointPickingMeasurementMode.Angle:
                ModeAngleRadio.IsChecked = true;
                break;
            default:
                ModeNoneRadio.IsChecked = true;
                break;
        }
    }
}
