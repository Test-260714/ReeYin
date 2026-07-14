using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using Arction.Wpf.ChartingMVVM.Titles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ReeYin_V.UI.UserControls.AxisMotionTrajectoryMonitor
{
    /// <summary>
    /// 轨迹监控的绘制计算模型，负责世界坐标、画布坐标和 LightningChart 数据之间的转换。
    /// </summary>
    public sealed class AxisMotionTrajectoryMonitorModel
    {
        // 坐标轴刻度间隔和默认世界坐标范围，单位沿用外部传入的运动坐标单位。
        public const double AxisTickInterval = 50d;
        public const double DefaultXMinimum = 0d;
        public const double DefaultXMaximum = 800d;
        public const double DefaultYMinimum = 0d;
        public const double DefaultYMaximum = 800d;
        public const double DefaultAxisMinimum = DefaultYMinimum;
        public const double DefaultAxisMaximum = DefaultYMaximum;

        // 固定画布布局参数：Plot 区域用于绘制轨迹，周边空间用于显示坐标刻度和标题。
        public const double CanvasSize = 820d;
        public const double PlotLeft = 92d;
        public const double PlotTop = 48d;
        public const double PlotWidth = 680d;
        public const double PlotHeight = 680d;
        public const double PlotRight = PlotLeft + PlotWidth;
        public const double PlotBottom = PlotTop + PlotHeight;

        private static readonly DoubleCollection PendingTrajectoryDashArray = CreateDashArray(10d, 6d);
        private static readonly DoubleCollection SimulatedCircleDashArray = CreateDashArray(6d, 4d);

        /// <summary>
        /// 规范化外部传入的坐标范围，避免 NaN、Infinity 或最大值小于最小值导致绘制异常。
        /// </summary>
        public AxisMonitorBounds CreateBounds(double xMinimum, double xMaximum, double yMinimum, double yMaximum)
        {
            double normalizedXMinimum = double.IsFinite(xMinimum) ? xMinimum : DefaultXMinimum;
            double normalizedXMaximum = double.IsFinite(xMaximum) ? xMaximum : DefaultXMaximum;
            double normalizedYMinimum = double.IsFinite(yMinimum) ? yMinimum : DefaultYMinimum;
            double normalizedYMaximum = double.IsFinite(yMaximum) ? yMaximum : DefaultYMaximum;

            if (normalizedXMaximum <= normalizedXMinimum)
            {
                normalizedXMaximum = normalizedXMinimum + 1d;
            }

            if (normalizedYMaximum <= normalizedYMinimum)
            {
                normalizedYMaximum = normalizedYMinimum + 1d;
            }

            return new AxisMonitorBounds(normalizedXMinimum, normalizedXMaximum, normalizedYMinimum, normalizedYMaximum);
        }

        public IReadOnlyList<AxisMonitorLineVisual> CreateGridLines(
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            // X/Y 方向分别投影到画布坐标，首尾刻度按边界线加粗显示。
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            double[] xTicks = CreateTicks(bounds.XMinimum, bounds.XMaximum);
            double[] yTicks = CreateTicks(bounds.YMinimum, bounds.YMaximum);
            List<AxisMonitorLineVisual> lines = new(xTicks.Length + yTicks.Length);

            for (int index = 0; index < xTicks.Length; index++)
            {
                double x = ProjectX(xTicks[index], bounds);
                bool isBoundary = index == 0 || index == xTicks.Length - 1;
                lines.Add(new AxisMonitorLineVisual
                {
                    X1 = x,
                    Y1 = PlotTop,
                    X2 = x,
                    Y2 = PlotBottom,
                    Stroke = CreateBrush(isBoundary ? settings.AxisBoundaryColor : settings.AxisGridColor),
                    StrokeThickness = isBoundary ? 1.6d : 1d,
                    Opacity = isBoundary ? 1d : 0.9d
                });
            }

            for (int index = 0; index < yTicks.Length; index++)
            {
                double y = ProjectY(yTicks[index], bounds);
                bool isBoundary = index == 0 || index == yTicks.Length - 1;
                lines.Add(new AxisMonitorLineVisual
                {
                    X1 = PlotLeft,
                    Y1 = y,
                    X2 = PlotRight,
                    Y2 = y,
                    Stroke = CreateBrush(isBoundary ? settings.AxisBoundaryColor : settings.AxisGridColor),
                    StrokeThickness = isBoundary ? 1.6d : 1d,
                    Opacity = isBoundary ? 1d : 0.9d
                });
            }

            return lines;
        }

        public IReadOnlyList<AxisMonitorLabelVisual> CreateXLabels(AxisMonitorBounds bounds)
        {
            double[] ticks = CreateTicks(bounds.XMinimum, bounds.XMaximum);
            return ticks
                .Select(tick => new AxisMonitorLabelVisual
                {
                    Left = ProjectX(tick, bounds) - 32d,
                    Top = PlotBottom + 16d,
                    Width = 64d,
                    Text = FormatTick(tick),
                    TextAlignment = TextAlignment.Center
                })
                .ToArray();
        }

        public IReadOnlyList<AxisMonitorLabelVisual> CreateYLabels(AxisMonitorBounds bounds)
        {
            double[] ticks = CreateTicks(bounds.YMinimum, bounds.YMaximum);
            return ticks
                .Select(tick => new AxisMonitorLabelVisual
                {
                    Left = 14d,
                    Top = ProjectY(tick, bounds) - 10d,
                    Width = 56d,
                    Text = FormatTick(tick),
                    TextAlignment = TextAlignment.Right
                })
                .ToArray();
        }

        public AxisMonitorPathVisual CreateTrajectoryVisual(
            AxisTrajectoryItem trajectoryItem,
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            TrajectoryVisualStyle style = ResolveTrajectoryStyle(trajectoryItem.State, ResolveAppearance(appearance));
            return new AxisMonitorPathVisual
            {
                Points = CreateProjectedPath(trajectoryItem.Points, bounds),
                Stroke = style.Stroke,
                StrokeThickness = style.StrokeThickness,
                StrokeDashArray = style.StrokeDashArray,
                Opacity = style.Opacity
            };
        }

        public AxisMonitorPathVisual CreateSimulationCircleVisual(
            Point center,
            double radius,
            AxisMonitorBounds bounds,
            int segmentCount = 180,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            return new AxisMonitorPathVisual
            {
                Points = CreateProjectedPath(CreateCirclePoints(center, radius, segmentCount), bounds),
                Stroke = CreateBrush(settings.SimulatedCircleColor),
                StrokeThickness = 2.8d,
                StrokeDashArray = SimulatedCircleDashArray,
                Opacity = 0.96d
            };
        }

        public PointCollection CreateProjectedPath(IEnumerable<Point> points, AxisMonitorBounds bounds)
        {
            // 画布层使用像素坐标显示，忽略无效点以避免 Path 渲染异常。
            PointCollection projectedPoints = new();
            foreach (Point point in points.Where(IsFinitePoint))
            {
                projectedPoints.Add(Project(point, bounds));
            }

            return projectedPoints;
        }

        public Point Project(Point point, AxisMonitorBounds bounds)
        {
            // WPF 画布 Y 轴向下递增，因此世界坐标 Y 需要反向投影。
            return new Point(ProjectX(point.X, bounds), ProjectY(point.Y, bounds));
        }

        public AxisXCollection CreateChartXAxes(
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            AxisX axis = new()
            {
                Minimum = bounds.XMinimum,
                Maximum = bounds.XMaximum,
                ValueType = AxisValueType.Number,
                AutoFormatLabels = false,
                LabelsVisible = true,
                EndPointLabelsVisible = true,
                AllowScaling = false,
                AllowScrolling = false,
                AxisColor = settings.AxisLineColor,
                LabelsColor = settings.AxisLabelColor,
                Title = new AxisXTitle
                {
                    Text = "X",
                    Color = settings.AxisTitleColor
                }
            };

            axis.MajorGrid.Visible = true;
            axis.MinorGrid.Visible = false;
            axis.CustomTicks = CreateCustomTicks(axis, bounds.XMinimum, bounds.XMaximum, settings);
            axis.CustomTicksEnabled = true;
            axis.SetRange(bounds.XMinimum, bounds.XMaximum);

            return new AxisXCollection
            {
                axis
            };
        }

        public AxisYCollection CreateChartYAxes(
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            AxisY axis = new()
            {
                Minimum = bounds.YMinimum,
                Maximum = bounds.YMaximum,
                AutoFormatLabels = false,
                LabelsVisible = true,
                EndPointLabelsVisible = true,
                AllowScaling = false,
                AllowScrolling = false,
                AxisColor = settings.AxisLineColor,
                LabelsColor = settings.AxisLabelColor,
                Title = new AxisYTitle
                {
                    Text = "Y",
                    Color = settings.AxisTitleColor
                }
            };

            axis.MajorGrid.Visible = true;
            axis.MinorGrid.Visible = false;
            axis.CustomTicks = CreateCustomTicks(axis, bounds.YMinimum, bounds.YMaximum, settings);
            axis.CustomTicksEnabled = true;
            axis.SetRange(bounds.YMinimum, bounds.YMaximum);

            return new AxisYCollection
            {
                axis
            };
        }

        public FreeformPointLineSeries CreateTrajectoryChartSeries(
            AxisTrajectoryItem trajectoryItem,
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            // LightningChart 图表层直接使用世界坐标，单点轨迹退化为点序列显示。
            TrajectoryChartStyle style = ResolveTrajectoryChartStyle(trajectoryItem.State, ResolveAppearance(appearance));
            SeriesPoint[] points = CreateSeriesPoints(trajectoryItem.Points, bounds);
            if (points.Length <= 1)
            {
                return CreatePointSeries(points, style.Color, style.PointSize);
            }

            return CreateLineSeries(points, style.Color, style.Width);
        }

        public FreeformPointLineSeries CreateSimulationCircleChartSeries(
            Point center,
            double radius,
            AxisMonitorBounds bounds,
            int segmentCount = 180,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            return CreateLineSeries(
                CreateSeriesPoints(CreateCirclePoints(center, radius, segmentCount), bounds),
                settings.SimulatedCircleColor,
                2.4d);
        }

        public FreeformPointLineSeries CreateMotionTraceChartSeries(
            IEnumerable<Point> points,
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            return CreateLineSeries(
                CreateSeriesPoints(points, bounds),
                settings.MotionTraceColor,
                2.8d);
        }

        public FreeformPointLineSeries? CreateCurrentMarkerChartSeries(
            Point point,
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            return CreateMarkerSeries(point, bounds, settings.CurrentMarkerColor, 15d);
        }

        public FreeformPointLineSeries? CreateTargetMarkerChartSeries(
            Point point,
            AxisMonitorBounds bounds,
            AxisMotionTrajectoryMonitorAppearance? appearance = null)
        {
            AxisMotionTrajectoryMonitorAppearance settings = ResolveAppearance(appearance);
            return CreateMarkerSeries(point, bounds, settings.TargetMarkerColor, 13d);
        }

        public bool IsInRange(Point point, AxisMonitorBounds bounds)
        {
            if (!IsFinitePoint(point))
            {
                return false;
            }

            return point.X >= bounds.XMinimum &&
                   point.X <= bounds.XMaximum &&
                   point.Y >= bounds.YMinimum &&
                   point.Y <= bounds.YMaximum;
        }

        private static IReadOnlyList<Point> CreateCirclePoints(Point center, double radius, int segmentCount)
        {
            // 通过闭合折线模拟圆形，至少保留 24 段以避免预览图形过于粗糙。
            if (!IsFinitePoint(center) || !double.IsFinite(radius) || radius <= 0d)
            {
                return Array.Empty<Point>();
            }

            int normalizedSegmentCount = Math.Max(24, segmentCount);
            Point[] points = new Point[normalizedSegmentCount + 1];

            for (int index = 0; index <= normalizedSegmentCount; index++)
            {
                double angle = (Math.PI * 2d * index) / normalizedSegmentCount;
                points[index] = new Point(
                    center.X + (radius * Math.Cos(angle)),
                    center.Y + (radius * Math.Sin(angle)));
            }

            return points;
        }

        private static AxisMotionTrajectoryMonitorAppearance ResolveAppearance(
            AxisMotionTrajectoryMonitorAppearance? appearance)
        {
            return appearance ?? AxisMotionTrajectoryMonitorAppearance.Default;
        }

        private static TrajectoryVisualStyle ResolveTrajectoryStyle(
            AxisTrajectoryState state,
            AxisMotionTrajectoryMonitorAppearance appearance)
        {
            return state switch
            {
                AxisTrajectoryState.执行中 => new TrajectoryVisualStyle(CreateBrush(appearance.RunningTrajectoryColor), 3.2d, null, 0.96d),
                AxisTrajectoryState.已完成 => new TrajectoryVisualStyle(CreateBrush(appearance.CompletedTrajectoryColor), 2.6d, null, 0.84d),
                _ => new TrajectoryVisualStyle(CreateBrush(appearance.PendingTrajectoryColor), 2.1d, PendingTrajectoryDashArray, 0.72d)
            };
        }

        private static TrajectoryChartStyle ResolveTrajectoryChartStyle(
            AxisTrajectoryState state,
            AxisMotionTrajectoryMonitorAppearance appearance)
        {
            return state switch
            {
                AxisTrajectoryState.执行中 => new TrajectoryChartStyle(ApplyAlpha(appearance.RunningTrajectoryColor, 0xFF), 2.8d, 11d),
                AxisTrajectoryState.已完成 => new TrajectoryChartStyle(ApplyAlpha(appearance.CompletedTrajectoryColor, 0xE0), 2.2d, 9d),
                _ => new TrajectoryChartStyle(ApplyAlpha(appearance.PendingTrajectoryColor, 0xD6), 1.8d, 8d)
            };
        }

        private static double[] CreateTicks(double minimum, double maximum)
        {
            // 刻度从 50 的整数倍开始，确保动态范围变化时网格对齐稳定。
            if (!double.IsFinite(minimum) || !double.IsFinite(maximum))
            {
                return Array.Empty<double>();
            }

            if (maximum <= minimum)
            {
                return new[] { minimum, maximum };
            }

            List<double> ticks = new();
            int tickCount = Math.Max((int)Math.Ceiling((maximum - minimum) / AxisTickInterval), 1);
            for (int index = 0; index <= tickCount; index++)
            {
                double tickValue = minimum + (AxisTickInterval * index);
                if (tickValue > maximum + 1e-9d)
                {
                    break;
                }

                ticks.Add(Math.Round(tickValue, 6, MidpointRounding.AwayFromZero));
            }

            if (ticks.Count == 0 || Math.Abs(ticks[^1] - maximum) > 1e-6d)
            {
                ticks.Add(maximum);
            }

            return ticks.ToArray();
        }

        private static CustomAxisTickCollection CreateCustomTicks(
            AxisBase axis,
            double minimum,
            double maximum,
            AxisMotionTrajectoryMonitorAppearance appearance)
        {
            CustomAxisTickCollection ticks = new();
            foreach (double tickValue in CreateTicks(minimum, maximum))
            {
                ticks.Add(new CustomAxisTick(
                    axis,
                    tickValue,
                    FormatTick(tickValue),
                    6,
                    true,
                    appearance.AxisBoundaryColor,
                    CustomTickStyle.TickAndGrid));
            }

            return ticks;
        }

        private static string FormatTick(double value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("F2");
        }

        private static double ProjectX(double xValue, AxisMonitorBounds bounds)
        {
            double normalized = (xValue - bounds.XMinimum) / bounds.XSpan;
            double clamped = Math.Clamp(normalized, 0d, 1d);
            return PlotLeft + (PlotWidth * clamped);
        }

        private static double ProjectY(double yValue, AxisMonitorBounds bounds)
        {
            double normalized = (yValue - bounds.YMinimum) / bounds.YSpan;
            double clamped = Math.Clamp(normalized, 0d, 1d);
            return PlotBottom - (PlotHeight * clamped);
        }

        private static bool IsFinitePoint(Point point)
        {
            return double.IsFinite(point.X) && double.IsFinite(point.Y);
        }

        private static SeriesPoint[] CreateSeriesPoints(IEnumerable<Point> points, AxisMonitorBounds bounds)
        {
            // 图表层保留世界坐标值，仅过滤非有限点并裁剪到当前坐标范围。
            return points
                .Where(IsFinitePoint)
                .Select(point => ClampPointToBounds(point, bounds))
                .Select(point => new SeriesPoint(point.X, point.Y))
                .ToArray();
        }

        private static Point ClampPointToBounds(Point point, AxisMonitorBounds bounds)
        {
            return new Point(
                Math.Clamp(point.X, bounds.XMinimum, bounds.XMaximum),
                Math.Clamp(point.Y, bounds.YMinimum, bounds.YMaximum));
        }

        private static FreeformPointLineSeries CreateLineSeries(
            SeriesPoint[] points,
            Color color,
            double lineWidth)
        {
            return new FreeformPointLineSeries
            {
                AllowUserInteraction = false,
                IncludeInAutoFit = false,
                LineVisible = true,
                PointsVisible = false,
                Points = points,
                LineStyle = new LineStyle
                {
                    Color = color,
                    Width = lineWidth
                }
            };
        }

        private static FreeformPointLineSeries CreatePointSeries(
            SeriesPoint[] points,
            Color color,
            double pointSize)
        {
            FreeformPointLineSeries series = new()
            {
                AllowUserInteraction = false,
                IncludeInAutoFit = false,
                LineVisible = false,
                PointsVisible = true,
                Points = points
            };

            series.PointStyle.Shape = Shape.Circle;
            series.PointStyle.Width = pointSize;
            series.PointStyle.Height = pointSize;
            series.PointStyle.Color1 = color;
            series.PointStyle.GradientFill = GradientFillPoint.Solid;

            return series;
        }

        private static FreeformPointLineSeries? CreateMarkerSeries(
            Point point,
            AxisMonitorBounds bounds,
            Color color,
            double pointSize)
        {
            if (!IsFinitePoint(point))
            {
                return null;
            }

            Point visiblePoint = ClampPointToBounds(point, bounds);
            FreeformPointLineSeries series = new()
            {
                AllowUserInteraction = false,
                IncludeInAutoFit = false,
                LineVisible = false,
                PointsVisible = true,
                Points = new[]
                {
                    new SeriesPoint(visiblePoint.X, visiblePoint.Y)
                }
            };

            series.PointStyle.Shape = Shape.Circle;
            series.PointStyle.Width = pointSize;
            series.PointStyle.Height = pointSize;
            series.PointStyle.Color1 = color;
            series.PointStyle.GradientFill = GradientFillPoint.Solid;

            return series;
        }

        private static Color ApplyAlpha(Color color, byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        private static SolidColorBrush CreateBrush(Color color)
        {
            SolidColorBrush brush = new(color);
            brush.Freeze();
            return brush;
        }

        private static DoubleCollection CreateDashArray(params double[] values)
        {
            DoubleCollection collection = new(values);
            collection.Freeze();
            return collection;
        }

        private readonly record struct TrajectoryVisualStyle(
            Brush Stroke,
            double StrokeThickness,
            DoubleCollection? StrokeDashArray,
            double Opacity);

        private readonly record struct TrajectoryChartStyle(
            Color Color,
            double Width,
            double PointSize);
    }
}
