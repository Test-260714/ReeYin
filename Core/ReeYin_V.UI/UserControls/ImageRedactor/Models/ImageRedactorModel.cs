using Arction.Wpf.ChartingMVVM;
using Arction.Wpf.ChartingMVVM.Annotations;
using Arction.Wpf.ChartingMVVM.Axes;
using Arction.Wpf.ChartingMVVM.SeriesXY;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ReeYin_V.UI.UserControls.ImageRedactor
{
    public class ImageRedactorModel
    {
        public AxisX CreateAxisX()
        {
            return new AxisX
            {
                Minimum = 0,
                Maximum = 100,
                ValueType = AxisValueType.Number,
                AutoFormatLabels = false,
                LabelsPosition = Alignment.Near,
                LabelsVisible = false,
                AllowScaling = true,
                AllowScrolling = true,
                MajorGrid = { Visible = false },
                MinorGrid = { Visible = false },
                Title = { Visible = false }
            };
        }

        public AxisY CreateAxisY()
        {
            return new AxisY
            {
                Minimum = 0,
                Maximum = 100,
                Alignment = AlignmentHorizontal.Right,
                AutoFormatLabels = false,
                LabelsVisible = false,
                AllowScaling = true,
                AllowScrolling = true,
                MajorGrid = { Visible = false },
                MinorGrid = { Visible = false },
                Title = { Visible = false }
            };
        }

        public BitmapFrame LoadBitmapFrame(string filePath, out int pixelWidth, out int pixelHeight)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Image file path cannot be empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Image file does not exist.", filePath);
            }

            BitmapImage bitmap = new();

            using (FileStream stream = File.OpenRead(filePath))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }

            pixelWidth = bitmap.PixelWidth;
            pixelHeight = bitmap.PixelHeight;

            BitmapFrame frame = BitmapFrame.Create(bitmap);
            if (frame.CanFreeze)
            {
                frame.Freeze();
            }

            return frame;
        }

        public AnnotationXY CreateBackgroundAnnotation(BitmapFrame imageFrame, double imageWidth, double imageHeight)
        {
            AnnotationXY annotation = new()
            {
                Sizing = AnnotationXYSizing.AxisValuesBoundaries,
                Style = AnnotationStyle.Rectangle,
                AllowUserInteraction = false,
                Behind = true,
                BorderVisible = false
            };

            annotation.Fill.Bitmap.Image = imageFrame;
            annotation.Fill.Style = RectFillStyle.Bitmap;
            annotation.Fill.Bitmap.Layout = BitmapFillLayout.Stretch;
            annotation.Fill.Color = Colors.Transparent;
            annotation.Fill.GradientFill = GradientFill.Solid;
            annotation.Fill.GradientColor = Colors.Transparent;
            annotation.TextStyle.Visible = false;
            annotation.Shadow.Visible = false;
            annotation.AxisValuesBoundaries.SetValues(0, imageWidth, 0, imageHeight);

            return annotation;
        }

        public AnnotationXY CreateTextAnnotation(string text, double xValue, double yValue)
        {
            AnnotationXY annotation = new()
            {
                Text = text,
                TargetCoordinateSystem = AnnotationTargetCoordinates.AxisValues,
                LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget,
                Style = AnnotationStyle.RoundedCallout
            };

            annotation.TargetAxisValues.SetValues(xValue, yValue);
            return annotation;
        }

        public AnnotationXY CreateBitmapAnnotation(BitmapFrame imageFrame, double xValue, double yValue)
        {
            AnnotationXY annotation = new()
            {
                TargetCoordinateSystem = AnnotationTargetCoordinates.AxisValues,
                LocationCoordinateSystem = CoordinateSystem.RelativeCoordinatesToTarget,
                Sizing = AnnotationXYSizing.ScreenCoordinates,
                BorderVisible = false
            };

            annotation.TargetAxisValues.SetValues(xValue, yValue);
            annotation.TextStyle.Visible = false;
            annotation.SizeScreenCoords.Width = 80;
            annotation.SizeScreenCoords.Height = 80;
            annotation.Fill.Bitmap.Image = imageFrame;
            annotation.Fill.Style = RectFillStyle.Bitmap;
            annotation.Fill.Bitmap.Layout = BitmapFillLayout.Stretch;
            annotation.Fill.Color = Colors.Transparent;
            annotation.Fill.GradientColor = Colors.Transparent;
            annotation.Shadow.Visible = false;

            return annotation;
        }

        public AnnotationXY CreateRingPointAnnotation(double xValue, double yValue, Color ringColor, double lineWidth)
        {
            double borderWidth = Math.Max(1, lineWidth);
            double diameter = Math.Max(12, 10 + borderWidth * 4);

            AnnotationXY annotation = new()
            {
                Style = AnnotationStyle.Ellipse,
                Sizing = AnnotationXYSizing.ScreenCoordinates,
                LocationCoordinateSystem = CoordinateSystem.AxisValues,
                AllowUserInteraction = false,
                BorderVisible = true
            };

            annotation.LocationAxisValues.SetValues(xValue, yValue);
            annotation.Anchor.SetValues(0.5, 0.5);
            annotation.SizeScreenCoords.Width = diameter;
            annotation.SizeScreenCoords.Height = diameter;
            annotation.Fill.Color = Colors.Transparent;
            annotation.Fill.GradientFill = GradientFill.Solid;
            annotation.Fill.GradientColor = Colors.Transparent;
            annotation.BorderLineStyle.Color = ringColor;
            annotation.BorderLineStyle.Width = borderWidth;
            annotation.TextStyle.Visible = false;
            annotation.Shadow.Visible = false;

            return annotation;
        }

        public FreeformPointLineSeries CreateSketchSeries(Color lineColor, double lineWidth)
        {
            FreeformPointLineSeries series = new()
            {
                AllowUserInteraction = false
            };

            series.LineStyle.Color = lineColor;
            series.LineStyle.Width = lineWidth;
            return series;
        }

        public PolygonSeries CreatePolygon(Color lineColor, IEnumerable<SeriesPoint> points)
        {
            PointDouble2D[] polygonPoints = points
                .Select(point => new PointDouble2D(point.X, point.Y))
                .ToArray();

            PolygonSeries polygon = new()
            {
                AllowUserInteraction = false,
                IntersectionsAllowed = true,
                Points = polygonPoints
            };

            polygon.Fill.Color = Color.FromArgb(96, lineColor.R, lineColor.G, lineColor.B);
            polygon.Fill.GradientFill = GradientFill.Solid;
            polygon.BorderVisible = true;
            polygon.Border.Color = lineColor;
            polygon.Border.Width = 1.2;
            return polygon;
        }
    }
}
