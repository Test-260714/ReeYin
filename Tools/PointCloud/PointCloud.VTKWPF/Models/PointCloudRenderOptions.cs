using System.Windows.Media;

namespace PointCloud.VTKWPF.Models;

public sealed class PointCloudRenderOptions
{
    public Color BackgroundTop { get; set; } = Color.FromScRgb(1.0f, 0.8f, 0.8f, 0.8f);

    public Color BackgroundBottom { get; set; } = Color.FromScRgb(1.0f, 0.2f, 0.3f, 0.3f);

    public bool UseGradientBackground { get; set; } = true;

    public double PointSize { get; set; } = 1.0;

    public double Opacity { get; set; } = 1.0;

    public Color SolidPointColor { get; set; } = Color.FromRgb(59, 130, 246);

    public ScalarColorAxis ColorAxis { get; set; } = ScalarColorAxis.Z;

    public bool ShowScalarBar { get; set; } = true;

    public string ScalarTitle { get; set; } = "Z";

    public ScalarFieldRenderParameters? ScalarParameters { get; set; }

    public bool ShowOrientationAxes { get; set; } = true;

    public bool EnableEdl { get; set; }
}
