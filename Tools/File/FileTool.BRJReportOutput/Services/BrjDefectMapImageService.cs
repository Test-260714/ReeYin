using FileTool.BRJReportOutput.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileTool.BRJReportOutput.Services
{
    public static class BrjDefectMapImageService
    {
        private const int ImageWidth = 1400;
        private const int ImageHeight = 780;
        private static readonly Rect PlotRect = new(82, 36, 1060, 690);

        public static byte[] RenderMap(BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects)
        {
            byte[]? result = null;
            Exception? error = null;

            Thread thread = new(() =>
            {
                try
                {
                    result = RenderMapCore(record, defects);
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (error != null)
            {
                throw error;
            }

            return result ?? Array.Empty<byte>();
        }

        private static byte[] RenderMapCore(BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects)
        {
            DrawingVisual visual = new();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Dictionary<string, BrjDefectVisualStyle> styles = BrjDefectStyleResolver.ResolveStyles(defects);
                DrawBackground(dc);
                DrawPlot(dc, record, defects, styles);
                DrawLegend(dc, styles.Values);
            }

            RenderTargetBitmap bitmap = new(ImageWidth, ImageHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using MemoryStream stream = new();
            encoder.Save(stream);
            return stream.ToArray();
        }

        private static void DrawBackground(DrawingContext dc)
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ImageWidth, ImageHeight));
        }

        private static void DrawPlot(DrawingContext dc, BrjReportRecord record, IReadOnlyList<BrjDefectRecord> defects, IReadOnlyDictionary<string, BrjDefectVisualStyle> styles)
        {
            double widthMm = ResolveMapWidth(record, defects);
            double lengthM = ResolveMapLength(record, defects);
            Brush plotBrush = new SolidColorBrush(Color.FromRgb(168, 175, 183));
            Pen borderPen = new(new SolidColorBrush(Color.FromRgb(91, 99, 112)), 1.2);
            Pen gridPen = new(new SolidColorBrush(Color.FromRgb(118, 125, 134)), 0.8);
            Pen slitPen = new(new SolidColorBrush(Color.FromRgb(76, 83, 94)), 1.4);

            dc.DrawRectangle(plotBrush, borderPen, PlotRect);

            for (int i = 1; i < 10; i++)
            {
                double x = PlotRect.Left + PlotRect.Width * i / 10.0;
                dc.DrawLine(gridPen, new Point(x, PlotRect.Top), new Point(x, PlotRect.Bottom));
            }

            for (int i = 1; i < 10; i++)
            {
                double y = PlotRect.Bottom - PlotRect.Height * i / 10.0;
                dc.DrawLine(gridPen, new Point(PlotRect.Left, y), new Point(PlotRect.Right, y));
                DrawText(dc, FormatNumber(lengthM * i / 10.0), 12, Brushes.Black, new Point(PlotRect.Left - 12, y - 8), TextAlignment.Right);
            }

            foreach (double percent in ResolveGuideLinePercents(record, widthMm))
            {
                double x = PlotRect.Left + PlotRect.Width * percent / 100.0;
                dc.DrawLine(slitPen, new Point(x, PlotRect.Top), new Point(x, PlotRect.Bottom));
            }

            foreach (BrjDefectRecord defect in defects)
            {
                DrawDefect(dc, defect, widthMm, lengthM, BrjDefectStyleResolver.ResolveStyle(styles, defect.DefectType));
            }

            DrawText(dc, $"缺陷数：{defects.Count}", 15, Brushes.Black, new Point(PlotRect.Right - 8, PlotRect.Top - 26), TextAlignment.Right);
            DrawText(dc, "幅宽方向", 15, Brushes.Black, new Point(PlotRect.Left + PlotRect.Width / 2, PlotRect.Bottom + 24), TextAlignment.Center);
            DrawText(dc, "米数/m", 15, Brushes.Black, new Point(PlotRect.Left - 54, PlotRect.Top - 2), TextAlignment.Left);
        }

        private static void DrawDefect(DrawingContext dc, BrjDefectRecord defect, double widthMm, double lengthM, BrjDefectVisualStyle style)
        {
            double x = PlotRect.Left + Math.Clamp(defect.PositionXMm / widthMm, 0.0, 1.0) * PlotRect.Width;
            double y = PlotRect.Bottom - Math.Clamp(defect.PositionYM / lengthM, 0.0, 1.0) * PlotRect.Height;
            Brush brush = style.Fill;
            Pen pen = new(brush, 2.0);

            if (string.Equals(style.MarkerKind, "Cross", StringComparison.OrdinalIgnoreCase))
            {
                dc.DrawLine(pen, new Point(x - 4, y - 4), new Point(x + 4, y + 4));
                dc.DrawLine(pen, new Point(x + 4, y - 4), new Point(x - 4, y + 4));
                return;
            }

            if (string.Equals(style.MarkerKind, "Triangle", StringComparison.OrdinalIgnoreCase))
            {
                StreamGeometry geometry = new();
                using StreamGeometryContext context = geometry.Open();
                context.BeginFigure(new Point(x, y - 5), true, true);
                context.LineTo(new Point(x + 5, y + 5), true, false);
                context.LineTo(new Point(x - 5, y + 5), true, false);
                geometry.Freeze();
                dc.DrawGeometry(brush, null, geometry);
                return;
            }

            dc.DrawEllipse(brush, null, new Point(x, y), 4.2, 4.2);
        }

        private static void DrawLegend(DrawingContext dc, IEnumerable<BrjDefectVisualStyle> styles)
        {
            double left = 1160;
            double top = 86;
            DrawText(dc, "缺陷图例", 18, Brushes.Black, new Point(left, top - 34), TextAlignment.Left);
            int index = 0;
            foreach (BrjDefectVisualStyle style in styles.Take(12))
            {
                DrawLegendItem(dc, left, top + index * 34, style.DefectType, style.Fill, style.MarkerKind);
                index++;
            }
        }

        private static void DrawLegendItem(DrawingContext dc, double left, double top, string text, Brush brush, string shape)
        {
            Point center = new(left + 12, top + 8);
            Pen pen = new(brush, 2.2);
            if (shape == "Cross")
            {
                dc.DrawLine(pen, new Point(center.X - 5, center.Y - 5), new Point(center.X + 5, center.Y + 5));
                dc.DrawLine(pen, new Point(center.X + 5, center.Y - 5), new Point(center.X - 5, center.Y + 5));
            }
            else if (shape == "Triangle")
            {
                StreamGeometry geometry = new();
                using StreamGeometryContext context = geometry.Open();
                context.BeginFigure(new Point(center.X, center.Y - 6), true, true);
                context.LineTo(new Point(center.X + 6, center.Y + 6), true, false);
                context.LineTo(new Point(center.X - 6, center.Y + 6), true, false);
                geometry.Freeze();
                dc.DrawGeometry(brush, null, geometry);
            }
            else
            {
                dc.DrawEllipse(brush, null, center, 5.5, 5.5);
            }

            DrawText(dc, text, 15, Brushes.Black, new Point(left + 32, top - 2), TextAlignment.Left);
        }

        private static double ResolveMapWidth(BrjReportRecord record, IReadOnlyCollection<BrjDefectRecord> defects)
        {
            double maxPosition = defects.Count == 0 ? 0d : defects.Max(item => Math.Max(0d, item.PositionXMm));
            return Math.Max(1d, Math.Max(record.ProductWidthMm, maxPosition));
        }

        private static double ResolveMapLength(BrjReportRecord record, IReadOnlyCollection<BrjDefectRecord> defects)
        {
            double maxPosition = defects.Count == 0 ? 0d : defects.Max(item => Math.Max(0d, item.PositionYM));
            return Math.Max(1d, Math.Max(Math.Max(record.RollLengthM, record.DetectMeters), maxPosition));
        }

        private static IEnumerable<double> ResolveGuideLinePercents(BrjReportRecord record, double widthMm)
        {
            return ResolveCoordinateValues(record.SlitLeftCoordinates)
                .Concat(ResolveCoordinateValues(record.SlitRightCoordinates))
                .Distinct()
                .Select(item => Math.Clamp(item / widthMm * 100d, 0d, 100d));
        }

        private static IEnumerable<double> ResolveCoordinateValues(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                yield break;
            }

            foreach (Match match in Regex.Matches(text, @"-?\d+(\.\d+)?"))
            {
                if (double.TryParse(match.Value, out double value))
                {
                    yield return value;
                }
            }
        }

        private static void DrawText(DrawingContext dc, string text, double size, Brush brush, Point origin, TextAlignment alignment)
        {
            FormattedText formattedText = new(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Microsoft YaHei"),
                size,
                brush,
                1.0)
            {
                TextAlignment = alignment,
            };
            if (alignment == TextAlignment.Center)
            {
                origin.X -= formattedText.WidthIncludingTrailingWhitespace / 2.0;
            }
            else if (alignment == TextAlignment.Right)
            {
                origin.X -= formattedText.WidthIncludingTrailingWhitespace;
            }

            dc.DrawText(formattedText, origin);
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
