using System.Windows;
using PointCloud.VTKWPF.Models;

namespace PointCloud.ToolViewer.Dialogs;

public partial class SetScalarFieldDialog : Window
{
    public SetScalarFieldDialog(ScalarColorAxis initialAxis)
    {
        InitializeComponent();
        SelectedAxis = initialAxis;
        ApplyInitialSelection(initialAxis);
    }

    public ScalarColorAxis SelectedAxis { get; private set; }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedAxis = AxisXRadio.IsChecked == true
            ? ScalarColorAxis.X
            : AxisYRadio.IsChecked == true
                ? ScalarColorAxis.Y
                : AxisZRadio.IsChecked == true
                    ? ScalarColorAxis.Z
                    : ScalarColorAxis.None;

        DialogResult = true;
    }

    private void ApplyInitialSelection(ScalarColorAxis initialAxis)
    {
        switch (initialAxis)
        {
            case ScalarColorAxis.X:
                AxisXRadio.IsChecked = true;
                break;
            case ScalarColorAxis.Y:
                AxisYRadio.IsChecked = true;
                break;
            case ScalarColorAxis.Z:
                AxisZRadio.IsChecked = true;
                break;
            default:
                AxisNoneRadio.IsChecked = true;
                break;
        }
    }
}
