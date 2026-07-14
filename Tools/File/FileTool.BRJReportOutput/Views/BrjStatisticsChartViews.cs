using FileTool.BRJReportOutput.ViewModels;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FileTool.BRJReportOutput.Views
{
    public class BrjPieChartView : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(BrjPieChartView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        private readonly List<ChartHitInfo> _hitInfos = new();
        private INotifyCollectionChanged? _itemsCollection;
        private int _hoveredIndex = -1;
        private int _selectedIndex = -1;

        public IEnumerable? ItemsSource
        {
            get { return (IEnumerable?)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public BrjPieChartView()
        {
            Cursor = Cursors.Hand;
            SnapsToDevicePixels = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(620, 360);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _hitInfos.Clear();

            List<BrjChartStatItem> items = GetItems().Where(item => item.Count > 0).ToList();
            if (items.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            double topHeight = Math.Max(170, ActualHeight * 0.52);
            double barTop = Math.Min(ActualHeight - 120, topHeight + 18);
            double legendWidth = Math.Min(260, Math.Max(190, ActualWidth * 0.3));
            double chartWidth = Math.Max(160, ActualWidth - legendWidth - 24);
            double depth = 18;
            double radiusX = Math.Max(58, Math.Min(chartWidth * 0.32, (topHeight - 50) * 0.64));
            double radiusY = Math.Max(46, radiusX * 0.58);
            Point center = new Point(16 + chartWidth * 0.5, Math.Max(radiusY + 20, (topHeight - depth) * 0.5));
            double total = items.Sum(item => item.Count);
            double startAngle = -90;

            for (int i = 0; i < items.Count; i++)
            {
                BrjChartStatItem item = items[i];
                double sweepAngle = item.Count / total * 360d;
                DrawPieSlice(drawingContext, center + new Vector(0, depth), radiusX, radiusY, startAngle, sweepAngle, Darken(item.Fill), null, 0);
                startAngle += sweepAngle;
            }

            startAngle = -90;
            for (int i = 0; i < items.Count; i++)
            {
                BrjChartStatItem item = items[i];
                double sweepAngle = item.Count / total * 360d;
                double midAngle = startAngle + sweepAngle * 0.5;
                Vector offset = i == _selectedIndex ? AngleVector(midAngle, 10, 6) : new Vector();
                Pen? border = i == _hoveredIndex || i == _selectedIndex ? new Pen(Brushes.White, 3) : new Pen(new SolidColorBrush(Color.FromArgb(150, 80, 80, 80)), 1);
                Point sliceCenter = center + offset;

                DrawPieSlice(drawingContext, sliceCenter, radiusX, radiusY, startAngle, sweepAngle, item.Fill, border, 1);
                _hitInfos.Add(new ChartHitInfo(i, item, sliceCenter, radiusX, radiusY, startAngle, sweepAngle));

                startAngle += sweepAngle;
            }

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            DrawPieLegend(drawingContext, items, chartWidth + 22, 20, legendWidth - 8);
            DrawBarChart(drawingContext, items, new Rect(42, barTop, Math.Max(120, ActualWidth - 74), Math.Max(90, ActualHeight - barTop - 36)), pixelsPerDip);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hitIndex = FindHitIndex(e.GetPosition(this));
            if (_hoveredIndex != hitIndex)
            {
                _hoveredIndex = hitIndex;
                ToolTip = hitIndex >= 0 ? BuildTip(_hitInfos.First(hit => hit.Index == hitIndex).Item) : null;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex >= 0)
            {
                _hoveredIndex = -1;
                ToolTip = null;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            int hitIndex = FindHitIndex(e.GetPosition(this));
            if (hitIndex >= 0)
            {
                _selectedIndex = _selectedIndex == hitIndex ? -1 : hitIndex;
                InvalidateVisual();
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BrjPieChartView view)
            {
                view.UpdateCollectionChanged(e.NewValue as INotifyCollectionChanged);
                view._hoveredIndex = -1;
                view._selectedIndex = -1;
                view.InvalidateVisual();
            }
        }

        private void UpdateCollectionChanged(INotifyCollectionChanged? newValue)
        {
            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged -= ItemsCollection_CollectionChanged;
            }

            _itemsCollection = newValue;
            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged += ItemsCollection_CollectionChanged;
            }
        }

        private void ItemsCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _hoveredIndex = -1;
            _selectedIndex = -1;
            InvalidateVisual();
        }

        private void DrawPieLegend(DrawingContext drawingContext, IReadOnlyList<BrjChartStatItem> items, double x, double y, double width)
        {
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            for (int i = 0; i < items.Count; i++)
            {
                BrjChartStatItem item = items[i];
                double rowY = y + i * 27;
                Rect marker = new Rect(x, rowY + 4, 14, 14);
                Pen border = i == _hoveredIndex || i == _selectedIndex ? new Pen(Brushes.White, 2) : new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 1);
                drawingContext.DrawRectangle(item.Fill, border, marker);

                string text = $"{item.Name}  {item.Count}  {item.Percent:0.00}%";
                FormattedText formattedText = CreateText(text, 13, Brushes.Black, pixelsPerDip);
                formattedText.MaxTextWidth = Math.Max(60, width - 22);
                formattedText.Trimming = TextTrimming.CharacterEllipsis;
                drawingContext.DrawText(formattedText, new Point(x + 22, rowY));

                _hitInfos.Add(new ChartHitInfo(i, item, new Rect(x, rowY, Math.Max(70, width), 24)));
            }
        }

        private void DrawBarChart(DrawingContext drawingContext, IReadOnlyList<BrjChartStatItem> items, Rect chart, double pixelsPerDip)
        {
            int maxCount = Math.Max(1, items.Max(item => item.Count));
            Pen gridPen = new Pen(new SolidColorBrush(Color.FromRgb(160, 160, 160)), 1);
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 1.2);

            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(218, 218, 218)), null, new Rect(chart.Left - 28, chart.Top - 16, chart.Width + 48, chart.Height + 44));
            for (int i = 0; i <= 5; i++)
            {
                double y = chart.Bottom - chart.Height / 5 * i;
                drawingContext.DrawLine(i == 0 ? axisPen : gridPen, new Point(chart.Left, y), new Point(chart.Right, y));
                drawingContext.DrawText(CreateText(Math.Round(maxCount / 5d * i).ToString(CultureInfo.InvariantCulture), 11, Brushes.Black, pixelsPerDip), new Point(chart.Left - 36, y - 8));
            }

            drawingContext.DrawLine(axisPen, chart.BottomLeft, chart.BottomRight);

            double step = chart.Width / items.Count;
            double barWidth = Math.Max(10, Math.Min(42, step * 0.36));
            for (int i = 0; i < items.Count; i++)
            {
                BrjChartStatItem item = items[i];
                double x = chart.Left + step * i + step * 0.5;
                double barHeight = item.Count / (double)maxCount * chart.Height;
                Rect bar = new Rect(x - barWidth * 0.5, chart.Bottom - barHeight, barWidth, barHeight);
                Brush fill = i == _hoveredIndex || i == _selectedIndex ? Lighten(item.Fill) : item.Fill;
                Pen barPen = i == _hoveredIndex || i == _selectedIndex ? new Pen(Brushes.White, 2.4) : new Pen(Darken(item.Fill), 1);

                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), null, new Rect(bar.X + 3, bar.Y + 3, bar.Width, bar.Height));
                drawingContext.DrawRectangle(fill, barPen, bar);
                _hitInfos.Add(new ChartHitInfo(i, item, bar));

                FormattedText countText = CreateText(item.Count.ToString(CultureInfo.InvariantCulture), 12, item.Fill, pixelsPerDip);
                drawingContext.DrawText(countText, new Point(x - countText.Width * 0.5, Math.Max(chart.Top - 4, bar.Top - 18)));

                FormattedText nameText = CreateText(item.Name, 12, item.Fill, pixelsPerDip);
                nameText.MaxTextWidth = Math.Max(42, step * 1.55);
                nameText.Trimming = TextTrimming.CharacterEllipsis;
                drawingContext.DrawText(nameText, new Point(x - nameText.Width * 0.5, chart.Bottom + 6));
            }
        }

        private void DrawPieSlice(DrawingContext drawingContext, Point center, double radiusX, double radiusY, double startAngle, double sweepAngle, Brush fill, Pen? pen, double opacity)
        {
            Brush brush = fill;
            if (opacity < 1)
            {
                brush = fill.Clone();
                brush.Opacity = 0.68;
            }

            if (sweepAngle >= 359.99)
            {
                drawingContext.DrawEllipse(brush, pen, center, radiusX, radiusY);
                return;
            }

            drawingContext.DrawGeometry(brush, pen, CreatePieSliceGeometry(center, radiusX, radiusY, startAngle, sweepAngle));
        }

        private int FindHitIndex(Point point)
        {
            for (int i = _hitInfos.Count - 1; i >= 0; i--)
            {
                if (_hitInfos[i].Contains(point))
                {
                    return _hitInfos[i].Index;
                }
            }

            return -1;
        }

        private IEnumerable<BrjChartStatItem> GetItems()
        {
            return ItemsSource?.Cast<object>().OfType<BrjChartStatItem>() ?? Enumerable.Empty<BrjChartStatItem>();
        }

        private static StreamGeometry CreatePieSliceGeometry(Point center, double radiusX, double radiusY, double startAngle, double sweepAngle)
        {
            Point startPoint = EllipsePoint(center, radiusX, radiusY, startAngle);
            Point endPoint = EllipsePoint(center, radiusX, radiusY, startAngle + sweepAngle);
            StreamGeometry geometry = new StreamGeometry();

            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(center, true, true);
                context.LineTo(startPoint, true, false);
                context.ArcTo(endPoint, new Size(radiusX, radiusY), 0, sweepAngle > 180, SweepDirection.Clockwise, true, false);
                context.LineTo(center, true, false);
            }

            geometry.Freeze();
            return geometry;
        }

        private static Point EllipsePoint(Point center, double radiusX, double radiusY, double angle)
        {
            double radians = angle * Math.PI / 180d;
            return new Point(center.X + Math.Cos(radians) * radiusX, center.Y + Math.Sin(radians) * radiusY);
        }

        private static Vector AngleVector(double angle, double x, double y)
        {
            double radians = angle * Math.PI / 180d;
            return new Vector(Math.Cos(radians) * x, Math.Sin(radians) * y);
        }

        private static Brush Darken(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Color color = solid.Color;
                return new SolidColorBrush(Color.FromRgb((byte)(color.R * 0.62), (byte)(color.G * 0.62), (byte)(color.B * 0.62)));
            }

            return brush;
        }

        private static Brush Lighten(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Color color = solid.Color;
                return new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Min(255, color.R + 36),
                    (byte)Math.Min(255, color.G + 36),
                    (byte)Math.Min(255, color.B + 36)));
            }

            return brush;
        }

        private static FormattedText CreateText(string text, double size, Brush brush, double pixelsPerDip)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Microsoft YaHei"), size, brush, pixelsPerDip);
        }

        private static string BuildTip(BrjChartStatItem item)
        {
            return $"{item.Name}\r\n数量：{item.Count}\r\n占比：{item.Percent:0.00}%";
        }
    }

    public class BrjMultiAxisChartView : FrameworkElement
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable),
                typeof(BrjMultiAxisChartView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnItemsSourceChanged));

        private readonly List<ChartHitInfo> _hitInfos = new();
        private INotifyCollectionChanged? _itemsCollection;
        private int _hoveredIndex = -1;
        private int _selectedIndex = -1;

        public IEnumerable? ItemsSource
        {
            get { return (IEnumerable?)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public BrjMultiAxisChartView()
        {
            Cursor = Cursors.Hand;
            SnapsToDevicePixels = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(660, 360);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            _hitInfos.Clear();

            List<BrjChartStatItem> items = GetItems().ToList();
            if (items.Count == 0 || ActualWidth <= 0 || ActualHeight <= 0)
            {
                return;
            }

            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            Rect chart = new Rect(58, 38, Math.Max(80, ActualWidth - 124), Math.Max(80, ActualHeight - 112));
            int maxCount = Math.Max(1, items.Max(item => item.Count));
            Pen gridPen = new Pen(new SolidColorBrush(Color.FromRgb(238, 238, 238)), 1);
            Pen axisPen = new Pen(new SolidColorBrush(Color.FromRgb(70, 70, 70)), 1.2);

            drawingContext.DrawRectangle(Brushes.White, new Pen(new SolidColorBrush(Color.FromRgb(190, 190, 190)), 1), chart);
            for (int i = 0; i <= 5; i++)
            {
                double y = chart.Bottom - chart.Height / 5 * i;
                drawingContext.DrawLine(i == 0 ? axisPen : gridPen, new Point(chart.Left, y), new Point(chart.Right, y));
                drawingContext.DrawText(CreateText(Math.Round(maxCount / 5d * i).ToString(CultureInfo.InvariantCulture), 11, Brushes.Black, pixelsPerDip), new Point(6, y - 8));
                drawingContext.DrawText(CreateText($"{i * 20}%", 11, Brushes.Black, pixelsPerDip), new Point(chart.Right + 8, y - 8));
            }

            drawingContext.DrawLine(axisPen, chart.TopLeft, chart.BottomLeft);
            drawingContext.DrawLine(axisPen, chart.TopRight, chart.BottomRight);
            drawingContext.DrawText(CreateText("数量", 12, Brushes.Black, pixelsPerDip), new Point(chart.Left - 46, chart.Top - 26));
            drawingContext.DrawText(CreateText("占比", 12, Brushes.Black, pixelsPerDip), new Point(chart.Right + 8, chart.Top - 26));

            double step = chart.Width / items.Count;
            double barWidth = Math.Max(8, Math.Min(36, step * 0.48));
            List<Point> linePoints = new();

            for (int i = 0; i < items.Count; i++)
            {
                BrjChartStatItem item = items[i];
                double x = chart.Left + step * i + step * 0.5;
                double barHeight = item.Count / (double)maxCount * chart.Height;
                Rect bar = new Rect(x - barWidth * 0.5, chart.Bottom - barHeight, barWidth, barHeight);
                Brush fill = i == _hoveredIndex || i == _selectedIndex ? Lighten(item.Fill) : item.Fill;
                Pen barPen = i == _hoveredIndex || i == _selectedIndex ? new Pen(Brushes.White, 2.4) : new Pen(new SolidColorBrush(Color.FromRgb(100, 100, 100)), 1);

                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromArgb(70, 0, 0, 0)), null, new Rect(bar.X + 3, bar.Y + 3, bar.Width, bar.Height));
                drawingContext.DrawRectangle(fill, barPen, bar);
                _hitInfos.Add(new ChartHitInfo(i, item, bar));

                Point linePoint = new Point(x, chart.Bottom - item.Percent / 100d * chart.Height);
                linePoints.Add(linePoint);

                if (i % Math.Max(1, (int)Math.Ceiling(items.Count / 10d)) == 0)
                {
                    FormattedText label = CreateText(item.Name, 11, Brushes.Black, pixelsPerDip);
                    label.MaxTextWidth = Math.Max(44, step * 1.6);
                    label.Trimming = TextTrimming.CharacterEllipsis;
                    drawingContext.PushTransform(new RotateTransform(-30, x, chart.Bottom + 18));
                    drawingContext.DrawText(label, new Point(x - label.Width * 0.5, chart.Bottom + 14));
                    drawingContext.Pop();
                }
            }

            Pen linePen = new Pen(new SolidColorBrush(Color.FromRgb(0, 102, 204)), 2);
            for (int i = 1; i < linePoints.Count; i++)
            {
                drawingContext.DrawLine(linePen, linePoints[i - 1], linePoints[i]);
            }

            for (int i = 0; i < linePoints.Count; i++)
            {
                Brush markerFill = i == _hoveredIndex || i == _selectedIndex ? Brushes.White : new SolidColorBrush(Color.FromRgb(0, 102, 204));
                Pen markerPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 102, 204)), 2);
                drawingContext.DrawEllipse(markerFill, markerPen, linePoints[i], 4.5, 4.5);
                _hitInfos.Add(new ChartHitInfo(i, items[i], new Rect(linePoints[i].X - 8, linePoints[i].Y - 8, 16, 16)));
            }

            DrawLegend(drawingContext, chart, pixelsPerDip);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hitIndex = FindHitIndex(e.GetPosition(this));
            if (_hoveredIndex != hitIndex)
            {
                _hoveredIndex = hitIndex;
                ToolTip = hitIndex >= 0 ? BuildTip(_hitInfos.First(hit => hit.Index == hitIndex).Item) : null;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoveredIndex >= 0)
            {
                _hoveredIndex = -1;
                ToolTip = null;
                InvalidateVisual();
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            int hitIndex = FindHitIndex(e.GetPosition(this));
            if (hitIndex >= 0)
            {
                _selectedIndex = _selectedIndex == hitIndex ? -1 : hitIndex;
                InvalidateVisual();
            }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BrjMultiAxisChartView view)
            {
                view.UpdateCollectionChanged(e.NewValue as INotifyCollectionChanged);
                view._hoveredIndex = -1;
                view._selectedIndex = -1;
                view.InvalidateVisual();
            }
        }

        private void UpdateCollectionChanged(INotifyCollectionChanged? newValue)
        {
            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged -= ItemsCollection_CollectionChanged;
            }

            _itemsCollection = newValue;
            if (_itemsCollection != null)
            {
                _itemsCollection.CollectionChanged += ItemsCollection_CollectionChanged;
            }
        }

        private void ItemsCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _hoveredIndex = -1;
            _selectedIndex = -1;
            InvalidateVisual();
        }

        private void DrawLegend(DrawingContext drawingContext, Rect chart, double pixelsPerDip)
        {
            double x = chart.Left + 8;
            double y = 10;
            drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(195, 140, 20)), null, new Rect(x, y + 4, 14, 12));
            drawingContext.DrawText(CreateText("数量(左轴)", 12, Brushes.Black, pixelsPerDip), new Point(x + 20, y));
            drawingContext.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(0, 102, 204)), 2), new Point(x + 112, y + 10), new Point(x + 148, y + 10));
            drawingContext.DrawEllipse(new SolidColorBrush(Color.FromRgb(0, 102, 204)), null, new Point(x + 130, y + 10), 4, 4);
            drawingContext.DrawText(CreateText("占比(右轴)", 12, Brushes.Black, pixelsPerDip), new Point(x + 156, y));
        }

        private int FindHitIndex(Point point)
        {
            for (int i = _hitInfos.Count - 1; i >= 0; i--)
            {
                if (_hitInfos[i].Bounds.Contains(point))
                {
                    return _hitInfos[i].Index;
                }
            }

            return -1;
        }

        private IEnumerable<BrjChartStatItem> GetItems()
        {
            return ItemsSource?.Cast<object>().OfType<BrjChartStatItem>() ?? Enumerable.Empty<BrjChartStatItem>();
        }

        private static Brush Lighten(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Color color = solid.Color;
                return new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Min(255, color.R + 36),
                    (byte)Math.Min(255, color.G + 36),
                    (byte)Math.Min(255, color.B + 36)));
            }

            return brush;
        }

        private static FormattedText CreateText(string text, double size, Brush brush, double pixelsPerDip)
        {
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Microsoft YaHei"), size, brush, pixelsPerDip);
        }

        private static string BuildTip(BrjChartStatItem item)
        {
            return $"{item.Name}\r\n数量：{item.Count}\r\n占比：{item.Percent:0.00}%";
        }
    }

    internal sealed class ChartHitInfo
    {
        public ChartHitInfo(int index, BrjChartStatItem item, Rect bounds)
        {
            Index = index;
            Item = item;
            Bounds = bounds;
        }

        public ChartHitInfo(int index, BrjChartStatItem item, Point center, double radiusX, double radiusY, double startAngle, double sweepAngle)
        {
            Index = index;
            Item = item;
            Center = center;
            RadiusX = radiusX;
            RadiusY = radiusY;
            StartAngle = NormalizeAngle(startAngle);
            SweepAngle = sweepAngle;
        }

        public int Index { get; }

        public BrjChartStatItem Item { get; }

        public Rect Bounds { get; } = Rect.Empty;

        private Point Center { get; }

        private double RadiusX { get; }

        private double RadiusY { get; }

        private double StartAngle { get; }

        private double SweepAngle { get; }

        public bool Contains(Point point)
        {
            if (!Bounds.IsEmpty)
            {
                return Bounds.Contains(point);
            }

            double nx = (point.X - Center.X) / RadiusX;
            double ny = (point.Y - Center.Y) / RadiusY;
            if (nx * nx + ny * ny > 1)
            {
                return false;
            }

            if (SweepAngle >= 359.99)
            {
                return true;
            }

            double angle = NormalizeAngle(Math.Atan2(ny, nx) * 180d / Math.PI);
            double delta = NormalizeAngle(angle - StartAngle);
            return delta <= SweepAngle;
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360d;
            return angle < 0 ? angle + 360d : angle;
        }
    }
}
