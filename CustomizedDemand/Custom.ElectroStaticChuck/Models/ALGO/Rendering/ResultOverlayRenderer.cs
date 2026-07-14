using Custom.ElectroStaticChuckMeasure.ALGO.Measurement;
using HalconDotNet;
using OpenCvSharp;

namespace Custom.ElectroStaticChuckMeasure.ALGO.Rendering;

public sealed class ResultOverlayRenderer
{
    private const int MinimumOverallTextFontSize = 24;
    private const int MinimumFeatureLabelFontSize = 24;
    private const int MaximumFeatureLabelFontSize = 72;

    public Mat Render(MeasurementResult measurement)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        using Mat gray = HObjectGrayToMat(measurement.DisplayGrayImage);
        if (gray.Empty())
            throw new InvalidOperationException("Rendering workflow could not convert measurement display gray image.");

        var overlay = new Mat();
        try
        {
            Cv2.CvtColor(gray, overlay, ColorConversionCodes.GRAY2BGR);
            int contourCount = DrawFittedContours(overlay, measurement.FitConvexRegion);
            if (contourCount == 0 && measurement.ConvexResults.Count > 0)
                DrawFallbackCircles(overlay, measurement);

            DrawOverallFlatnessText(ref overlay, measurement);
            foreach (ConvexFeature convex in measurement.ConvexResults)
                DrawCenterMarker(overlay, convex);

            DrawFeatureLabels(ref overlay, measurement.ConvexResults);

            return overlay;
        }
        catch
        {
            overlay.Dispose();
            throw;
        }
    }

    private static Mat HObjectGrayToMat(HObject grayImage)
    {
        HOperatorSet.CountChannels(grayImage, out HTuple channels);
        try
        {
            if (channels.Length == 0 || channels[0].I != 1)
                throw new InvalidOperationException("Rendering workflow requires a single-channel measurement display gray image.");
        }
        finally
        {
            channels.Dispose();
        }

        HOperatorSet.GetImagePointer1(grayImage, out HTuple pointer, out HTuple type, out HTuple width, out HTuple height);
        try
        {
            if (!string.Equals(type.S, "byte", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Rendering workflow requires byte gray image, got {type.S}.");

            using Mat header = Mat.FromPixelData(height.I, width.I, MatType.CV_8UC1, pointer.IP);
            return header.Clone();
        }
        finally
        {
            pointer.Dispose();
            type.Dispose();
            width.Dispose();
            height.Dispose();
        }
    }

    private static int DrawFittedContours(Mat overlay, HObject fitConvexRegion)
    {
        HObject? selectedObject = null;
        HObject? contourSource = null;
        HObject? contour = null;
        int drawnCount = 0;

        try
        {
            HOperatorSet.CountObj(fitConvexRegion, out HTuple objectCount);
            try
            {
                for (int i = 1; i <= objectCount.I; i++)
                {
                    selectedObject?.Dispose();
                    contourSource?.Dispose();
                    contour?.Dispose();
                    selectedObject = null;
                    contourSource = null;
                    contour = null;

                    HOperatorSet.SelectObj(fitConvexRegion, out selectedObject, i);
                    HOperatorSet.GetObjClass(selectedObject, out HTuple objectClass);
                    try
                    {
                        string className = objectClass.S.ToLowerInvariant();
                        if (className == "region")
                            HOperatorSet.GenContourRegionXld(selectedObject, out contourSource, "border");
                        else
                            contourSource = selectedObject.Clone();
                    }
                    finally
                    {
                        objectClass.Dispose();
                    }

                    HOperatorSet.CountObj(contourSource, out HTuple contourCount);
                    try
                    {
                        for (int j = 1; j <= contourCount.I; j++)
                        {
                            contour?.Dispose();
                            contour = null;

                            HOperatorSet.SelectObj(contourSource, out contour, j);
                            if (TryGetContourPoints(contour, overlay.Width, overlay.Height, out OpenCvSharp.Point[] contourPoints))
                            {
                                Cv2.Polylines(overlay, new[] { contourPoints }, true, new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);
                                drawnCount++;
                            }
                        }
                    }
                    finally
                    {
                        contourCount.Dispose();
                    }
                }
            }
            finally
            {
                objectCount.Dispose();
            }
        }
        catch
        {
            return 0;
        }
        finally
        {
            selectedObject?.Dispose();
            contourSource?.Dispose();
            contour?.Dispose();
        }

        return drawnCount;
    }

    private static bool TryGetContourPoints(HObject contour, int width, int height, out OpenCvSharp.Point[] points)
    {
        points = Array.Empty<OpenCvSharp.Point>();
        HTuple? rows = null;
        HTuple? cols = null;
        try
        {
            HOperatorSet.GetContourXld(contour, out rows, out cols);
            int pointCount = Math.Min(rows.Length, cols.Length);
            if (pointCount < 2)
                return false;

            points = new OpenCvSharp.Point[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                int x = Math.Clamp((int)Math.Round(cols[i].D), 0, width - 1);
                int y = Math.Clamp((int)Math.Round(rows[i].D), 0, height - 1);
                points[i] = new OpenCvSharp.Point(x, y);
            }

            return true;
        }
        catch
        {
            points = Array.Empty<OpenCvSharp.Point>();
            return false;
        }
        finally
        {
            rows?.Dispose();
            cols?.Dispose();
        }
    }

    private static void DrawFallbackCircles(Mat overlay, MeasurementResult measurement)
    {
        foreach (ConvexFeature convex in measurement.ConvexResults)
        {
            if (measurement.IntervalX <= 0 || !double.IsFinite(measurement.IntervalX))
                continue;

            OpenCvSharp.Point center = GetDrawCenter(overlay, convex);
            int radius = Math.Max(1, (int)Math.Round(convex.Diameter / (2.0 * measurement.IntervalX)));
            Cv2.Circle(overlay, center, radius, new Scalar(0, 255, 0), 2, LineTypes.AntiAlias);
        }
    }

    private static void DrawCenterMarker(Mat overlay, ConvexFeature convex)
    {
        if (!double.IsFinite(convex.PixelX) || !double.IsFinite(convex.PixelY))
            return;

        OpenCvSharp.Point center = GetDrawCenter(overlay, convex);
        Cv2.Circle(overlay, center, 3, new Scalar(0, 0, 255), -1, LineTypes.AntiAlias);
    }

    private static OpenCvSharp.Point GetDrawCenter(Mat overlay, ConvexFeature convex)
    {
        int x = Math.Clamp((int)Math.Round(convex.PixelX), 0, overlay.Width - 1);
        int y = Math.Clamp((int)Math.Round(convex.PixelY), 0, overlay.Height - 1);
        return new OpenCvSharp.Point(x, y);
    }

    private static void DrawOverallFlatnessText(ref Mat overlay, MeasurementResult measurement)
    {
        string text = $"\u51f8\u70b9\u6574\u4f53\u5e73\u9762\u5ea6\uff1a{measurement.ConvexsFlatness:F6} \u5fae\u7c73";
        int fontSize = Math.Clamp(Math.Min(overlay.Width, overlay.Height) / 16, MinimumOverallTextFontSize, 250);
        var point = overlay.Width > 300 && overlay.Height > 260
            ? new System.Drawing.PointF(100, 200)
            : new System.Drawing.PointF(20, 30);

        if (!TryDrawTextWithGdi(ref overlay, graphics =>
            {
                using var font = new System.Drawing.Font("Microsoft YaHei", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Cyan);
                graphics.DrawString(text, font, brush, point);
            }))
        {
            string fallbackText = $"ConvexsFlatness: {measurement.ConvexsFlatness:F6} um";
            Cv2.PutText(overlay, fallbackText, new OpenCvSharp.Point(20, 40), HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 0, 0), 3, LineTypes.AntiAlias);
            Cv2.PutText(overlay, fallbackText, new OpenCvSharp.Point(20, 40), HersheyFonts.HersheySimplex, 0.8, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
        }
    }

    private static void DrawFeatureLabels(ref Mat overlay, IReadOnlyList<ConvexFeature> convexResults)
    {
        int overlayWidth = overlay.Width;
        int overlayHeight = overlay.Height;

        if (TryDrawTextWithGdi(ref overlay, graphics =>
            {
                int fontSize = ResolveFeatureLabelFontSize(overlayWidth, overlayHeight);
                using var font = new System.Drawing.Font("Microsoft YaHei", fontSize, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Pixel);
                using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Cyan);
                int lineHeight = Math.Max(fontSize, font.Height + 2);
                foreach (ConvexFeature convex in convexResults)
                    DrawFeatureLabels(graphics, font, brush, convex, overlayWidth, overlayHeight, lineHeight);
            }))
        {
            return;
        }

        double fallbackScale = ResolveFeatureLabelFallbackScale(overlayWidth, overlayHeight);
        int lineHeightFallback = Math.Max(MinimumFeatureLabelFontSize, (int)Math.Round(24 * fallbackScale));
        foreach (ConvexFeature convex in convexResults)
        {
            OpenCvSharp.Point center = GetDrawCenter(overlay, convex);
            Cv2.PutText(overlay, $"Height: {convex.Height:F2} um", new OpenCvSharp.Point(center.X + 10, center.Y), HersheyFonts.HersheySimplex, fallbackScale, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
            Cv2.PutText(overlay, $"Diameter: {convex.Diameter:F2} um", new OpenCvSharp.Point(center.X + 10, center.Y + lineHeightFallback), HersheyFonts.HersheySimplex, fallbackScale, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
            Cv2.PutText(overlay, $"Roundness: {convex.Roundness:F4}", new OpenCvSharp.Point(center.X + 10, center.Y + lineHeightFallback * 2), HersheyFonts.HersheySimplex, fallbackScale, new Scalar(0, 255, 255), 2, LineTypes.AntiAlias);
        }
    }

    private static int ResolveFeatureLabelFontSize(int overlayWidth, int overlayHeight)
    {
        int scaled = Math.Min(overlayWidth, overlayHeight) / 12;
        return Math.Clamp(scaled, MinimumFeatureLabelFontSize, MaximumFeatureLabelFontSize);
    }

    private static double ResolveFeatureLabelFallbackScale(int overlayWidth, int overlayHeight)
    {
        return Math.Clamp(Math.Min(overlayWidth, overlayHeight) / 480.0, 0.8, 2.0);
    }

    private static void DrawFeatureLabels(
        System.Drawing.Graphics graphics,
        System.Drawing.Font font,
        System.Drawing.Brush brush,
        ConvexFeature convex,
        int overlayWidth,
        int overlayHeight,
        int lineHeight)
    {
        OpenCvSharp.Point center = new(
            Math.Clamp((int)Math.Round(convex.PixelX), 0, overlayWidth - 1),
            Math.Clamp((int)Math.Round(convex.PixelY), 0, overlayHeight - 1));
        string[] labels =
        {
            $"\u9ad8\u5ea6\uff1a{convex.Height:F2} \u5fae\u7c73",
            $"\u76f4\u5f84\uff1a{convex.Diameter:F2} \u5fae\u7c73",
            $"\u5706\u5ea6\uff1a{convex.Roundness:F4}"
        };

        float maxTextWidth = 0;
        foreach (string label in labels)
        {
            System.Drawing.SizeF textSize = graphics.MeasureString(label, font);
            maxTextWidth = Math.Max(maxTextWidth, textSize.Width);
        }

        float textX = center.X + 10;
        if (textX + maxTextWidth + 8 >= overlayWidth)
            textX = center.X - maxTextWidth - 10;

        textX = Math.Clamp(textX, 0, Math.Max(0, overlayWidth - maxTextWidth - 1));

        float textY = center.Y - lineHeight;
        if (textY < lineHeight)
            textY = center.Y + lineHeight;

        int labelsHeight = lineHeight * labels.Length;
        if (textY + labelsHeight >= overlayHeight)
            textY = Math.Max(lineHeight, overlayHeight - labelsHeight - 2);

        for (int i = 0; i < labels.Length; i++)
            graphics.DrawString(labels[i], font, brush, new System.Drawing.PointF(textX, textY + i * lineHeight));
    }

    private static bool TryDrawTextWithGdi(ref Mat image, Action<System.Drawing.Graphics> draw)
    {
        try
        {
            using var bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image);
            using var graphics = System.Drawing.Graphics.FromImage(bitmap);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            draw(graphics);

            using Mat textImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            Mat replacement = textImage.Clone();
            image.Dispose();
            image = replacement;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
