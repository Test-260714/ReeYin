namespace PointCloud.ToolViewer.Models;

public sealed class ScalarFieldHistogramData
{
    public ScalarFieldHistogramData(int[] bins, double minimum, double maximum)
    {
        int[] histogramBins = bins ?? Array.Empty<int>();
        Bins = histogramBins;
        Minimum = minimum;
        Maximum = maximum;
        MaxBinValue = histogramBins.Length == 0 ? 0 : histogramBins.Max();
    }

    public IReadOnlyList<int> Bins { get; }

    public double Minimum { get; }

    public double Maximum { get; }

    public int MaxBinValue { get; }

    public bool IsFlat => Math.Abs(Maximum - Minimum) <= 1e-12;
}
