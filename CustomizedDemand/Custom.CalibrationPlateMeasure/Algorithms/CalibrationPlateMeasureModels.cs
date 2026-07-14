using HalconDotNet;

namespace Custom.CalibrationPlateMeasure.Algorithms
{
    // 算法模式：刻槽测量拟合四条边界线，圆测量拟合目标圆轮廓。
    public enum CalibrationPlateMeasurementMode
    {
        Groove,
        Circle
    }

    // 视图模型传入算法的完整测量请求，坐标均使用显示高度图像素坐标。
    public sealed class CalibrationPlateMeasureRequest
    {
        public HObject InputImage { get; set; } = new();

        public double IntervalX { get; set; } = 0.00375;

        public double IntervalY { get; set; } = 0.00381;

        public double IntervalZ { get; set; } = 0.001;

        public double AccelerationFactor { get; set; } = 1.0;

        public double DepthExpand { get; set; } = 10.0;

        public bool HasSelectedRoi { get; set; }

        public double SelectedRoiRow1 { get; set; }

        public double SelectedRoiColumn1 { get; set; }

        public double SelectedRoiRow2 { get; set; }

        public double SelectedRoiColumn2 { get; set; }

        public double CircleMinContourLength { get; set; } = 5.0;

        public double CircleMinRadiusPx { get; set; } = 1.0;

        public double CircleDepthExpandPx { get; set; } = 0.0;
    }

    // 单个目标的测量值和显示几何，供结果表格、WPF 图像和 HALCON ROI 共用。
    public sealed class CalibrationPlateMeasureItem
    {
        public int Index { get; set; }

        public double LengthMm { get; set; }

        public double WidthMm { get; set; }

        public double DepthMm { get; set; }

        public double DepthGrayDiff { get; set; }

        public double Row1 { get; set; }

        public double Column1 { get; set; }

        public double Row2 { get; set; }

        public double Column2 { get; set; }

        public double UpperRow { get; set; }

        public double LowerRow { get; set; }

        public double LeftColumn { get; set; }

        public double RightColumn { get; set; }

        public double UpperLeftRow { get; set; }

        public double UpperLeftColumn { get; set; }

        public double LowerLeftRow { get; set; }

        public double LowerLeftColumn { get; set; }

        public double UpperRightRow { get; set; }

        public double UpperRightColumn { get; set; }

        public double LowerRightRow { get; set; }

        public double LowerRightColumn { get; set; }

        public double DepthUpperLeftRow { get; set; }

        public double DepthUpperLeftColumn { get; set; }

        public double DepthUpperRightRow { get; set; }

        public double DepthUpperRightColumn { get; set; }

        public double DepthLowerRightRow { get; set; }

        public double DepthLowerRightColumn { get; set; }

        public double DepthLowerLeftRow { get; set; }

        public double DepthLowerLeftColumn { get; set; }

        public double CircleCenterRow { get; set; }

        public double CircleCenterColumn { get; set; }

        public double CircleRadiusPx { get; set; }

        public double CircleRadiusMm { get; set; }

        public double CircleDiameterMm { get; set; }

        public bool IsCircle { get; set; }
    }

    // 框选或算法提取出的目标区域，用于失败时仍能显示候选区域。
    public sealed class CalibrationPlateTargetRegion
    {
        public int Index { get; set; }

        public double Row1 { get; set; }

        public double Column1 { get; set; }

        public double Row2 { get; set; }

        public double Column2 { get; set; }
    }

    // 算法输出对象持有 HALCON 资源，调用方必须 Dispose。
    public sealed class CalibrationPlateMeasureResult : IDisposable
    {
        public CalibrationPlateMeasureResult()
        {
            HOperatorSet.GenEmptyObj(out HObject displayContours);
            DisplayContours = displayContours;
        }

        public List<CalibrationPlateMeasureItem> Items { get; } = [];

        public List<CalibrationPlateTargetRegion> TargetRegions { get; } = [];

        public HObject DisplayImage { get; set; } = new();

        public HObject DisplayContours { get; set; }

        public HObject HeightImage { get; set; } = new();

        public double DisplayPixelSizeX { get; set; }

        public double DisplayPixelSizeY { get; set; }

        public double DisplayPixelSizeZ { get; set; }

        public void Dispose()
        {
            DisplayImage.Dispose();
            DisplayContours.Dispose();
            HeightImage.Dispose();
        }
    }
}
