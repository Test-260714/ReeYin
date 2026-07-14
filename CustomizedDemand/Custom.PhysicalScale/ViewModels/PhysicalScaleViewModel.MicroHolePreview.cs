using Custom.PhysicalScale.Helpers;
using Custom.PhysicalScale.Models;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace Custom.PhysicalScale.ViewModels
{
    public partial class PhysicalScaleViewModel
    {
        private void UpdateHolePreviewDisplay()
        {
            if (_holeAnalysisResult == null)
                return;

            BitmapSource previewBitmap = Model.HolePreviewMode switch
            {
                PhysicalScaleHolePreviewMode.Binary => CreateHoleBinaryPreview(_holeAnalysisResult),
                PhysicalScaleHolePreviewMode.Edge => CreateHoleEdgePreview(_holeAnalysisResult, Model.DisplayExposureCompensation),
                PhysicalScaleHolePreviewMode.SubPixelBoundary => CreateHoleSubPixelPreview(_holeAnalysisResult, Model.DisplayExposureCompensation),
                _ => CreateHoleOriginalPreview(_holeAnalysisResult, Model.DisplayExposureCompensation)
            };

            ApplyDisplayImage(previewBitmap);
            Model.HolePreviewSummary = Model.HolePreviewMode switch
            {
                PhysicalScaleHolePreviewMode.Binary => $"当前显示：二值化，阈值 {Model.HoleThreshold}",
                PhysicalScaleHolePreviewMode.Edge => "当前显示：边缘增强叠加",
                PhysicalScaleHolePreviewMode.SubPixelBoundary => "当前显示：亚像素边界叠加",
                _ => "当前显示：局部原图 + 判定叠加"
            };

            RaisePropertyChanged(nameof(CenterSubTitle));
            RaisePropertyChanged(nameof(HoleJudgeResultMeta));
        }

        private static BitmapSource CreateHoleOriginalPreview(PhysicalScaleMicroHoleAnalysisResult result, double exposureCompensation)
        {
            BitmapSource source = ApplyExposureCompensation(result.CropBitmap, exposureCompensation);
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            DrawCircleOutline(pixels, width, height, result.NominalCenterX, result.NominalCenterY, result.NominalRadius, 255, 196, 64);
            DrawCircleOutline(pixels, width, height, result.FittedCenterX, result.FittedCenterY, result.FittedRadius, 0, 220, 255);
            TintPixels(pixels, result.DefectRegions, 255, 72, 72);

            return CreateBgraBitmap(pixels, width, height);
        }

        private static BitmapSource CreateHoleBinaryPreview(PhysicalScaleMicroHoleAnalysisResult result)
        {
            byte[] binary = new byte[result.Width * result.Height];
            for (int i = 0; i < binary.Length; i++)
            {
                if (!result.RoiMask[i])
                    binary[i] = 0;
                else
                    binary[i] = result.OpenMask[i] ? (byte)255 : (byte)0;
            }

            byte[] bgra = CreateGrayBackground(binary, result.Width, result.Height);
            DrawCircleOutline(bgra, result.Width, result.Height, result.FittedCenterX, result.FittedCenterY, result.FittedRadius, 0, 220, 255);
            TintPixels(bgra, result.DefectRegions, 255, 110, 64);
            return CreateBgraBitmap(bgra, result.Width, result.Height);
        }

        private static BitmapSource CreateHoleEdgePreview(PhysicalScaleMicroHoleAnalysisResult result, double exposureCompensation)
        {
            byte[] background = ApplyExposureCompensation(result.GrayPixels, exposureCompensation);
            byte[] bgra = CreateGrayBackground(background, result.Width, result.Height);
            for (int y = 1; y < result.Height - 1; y++)
            {
                for (int x = 1; x < result.Width - 1; x++)
                {
                    int idx = y * result.Width + x;
                    int gx =
                        -result.GrayPixels[(y - 1) * result.Width + (x - 1)] - 2 * result.GrayPixels[y * result.Width + (x - 1)] - result.GrayPixels[(y + 1) * result.Width + (x - 1)] +
                        result.GrayPixels[(y - 1) * result.Width + (x + 1)] + 2 * result.GrayPixels[y * result.Width + (x + 1)] + result.GrayPixels[(y + 1) * result.Width + (x + 1)];
                    int gy =
                        -result.GrayPixels[(y - 1) * result.Width + (x - 1)] - 2 * result.GrayPixels[(y - 1) * result.Width + x] - result.GrayPixels[(y - 1) * result.Width + (x + 1)] +
                        result.GrayPixels[(y + 1) * result.Width + (x - 1)] + 2 * result.GrayPixels[(y + 1) * result.Width + x] + result.GrayPixels[(y + 1) * result.Width + (x + 1)];

                    int magnitude = (int)Math.Min(255, Math.Sqrt(gx * gx + gy * gy));
                    if (magnitude >= 32)
                    {
                        int pixelIndex = idx * 4;
                        bgra[pixelIndex] = 36;
                        bgra[pixelIndex + 1] = 190;
                        bgra[pixelIndex + 2] = 255;
                    }
                }
            }

            DrawCircleOutline(bgra, result.Width, result.Height, result.FittedCenterX, result.FittedCenterY, result.FittedRadius, 0, 220, 255);
            TintPixels(bgra, result.DefectRegions, 255, 72, 72);
            return CreateBgraBitmap(bgra, result.Width, result.Height);
        }

        private static BitmapSource CreateHoleSubPixelPreview(PhysicalScaleMicroHoleAnalysisResult result, double exposureCompensation)
        {
            byte[] background = ApplyExposureCompensation(result.GrayPixels, exposureCompensation);
            byte[] bgra = CreateGrayBackground(background, result.Width, result.Height);
            GCHandle handle = GCHandle.Alloc(result.GrayPixels, GCHandleType.Pinned);
            try
            {
                HOperatorSet.GenImage1(out HObject image, "byte", result.Width, result.Height, handle.AddrOfPinnedObject());
                try
                {
                    HOperatorSet.EdgesSubPix(image, out HObject edges, "canny", 1.2, 8, 24);
                    try
                    {
                        HOperatorSet.CountObj(edges, out HTuple count);
                        for (int i = 1; i <= count.I; i++)
                        {
                            HOperatorSet.SelectObj(edges, out HObject contour, i);
                            try
                            {
                                HOperatorSet.GetContourXld(contour, out HTuple rows, out HTuple cols);
                                DrawContourPoints(bgra, result.Width, result.Height, rows, cols);
                            }
                            finally
                            {
                                contour.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        edges.Dispose();
                    }
                }
                finally
                {
                    image.Dispose();
                }
            }
            finally
            {
                handle.Free();
            }

            DrawCircleOutline(bgra, result.Width, result.Height, result.FittedCenterX, result.FittedCenterY, result.FittedRadius, 0, 220, 255);
            TintPixels(bgra, result.DefectRegions, 255, 92, 64);
            return CreateBgraBitmap(bgra, result.Width, result.Height);
        }

        private static BitmapSource EnsureBgra32(BitmapSource source)
        {
            if (source.Format == System.Windows.Media.PixelFormats.Bgra32)
            {
                source.Freeze();
                return source;
            }

            var converted = new FormatConvertedBitmap();
            converted.BeginInit();
            converted.Source = source;
            converted.DestinationFormat = System.Windows.Media.PixelFormats.Bgra32;
            converted.EndInit();
            converted.Freeze();
            return converted;
        }

        private static byte[] CreateGrayBackground(byte[] grayPixels, int width, int height)
        {
            byte[] bgra = new byte[width * height * 4];
            for (int i = 0; i < grayPixels.Length; i++)
            {
                int pixelIndex = i * 4;
                byte value = grayPixels[i];
                bgra[pixelIndex] = value;
                bgra[pixelIndex + 1] = value;
                bgra[pixelIndex + 2] = value;
                bgra[pixelIndex + 3] = 255;
            }

            return bgra;
        }

        private static BitmapSource CreateBgraBitmap(byte[] pixels, int width, int height)
        {
            int stride = width * 4;
            var bitmap = BitmapSource.Create(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, pixels, stride);
            bitmap.Freeze();
            return bitmap;
        }

        private static void DrawContourPoints(byte[] bgra, int width, int height, HTuple rows, HTuple cols)
        {
            int length = Math.Min(rows.Length, cols.Length);
            for (int i = 0; i < length; i++)
            {
                int y = (int)Math.Round(rows[i].D);
                int x = (int)Math.Round(cols[i].D);
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    for (int offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        PaintPixel(bgra, width, height, x + offsetX, y + offsetY, 255, 96, 64);
                    }
                }
            }
        }

        private static void DrawCircleOutline(byte[] bgra, int width, int height, double centerX, double centerY, double radius, byte r, byte g, byte b)
        {
            int steps = Math.Max(64, (int)(radius * 10));
            for (int i = 0; i < steps; i++)
            {
                double angle = 2.0 * Math.PI * i / steps;
                int x = (int)Math.Round(centerX + Math.Cos(angle) * radius);
                int y = (int)Math.Round(centerY + Math.Sin(angle) * radius);
                PaintPixel(bgra, width, height, x, y, r, g, b);
            }
        }

        private static void TintPixels(byte[] bgra, IEnumerable<PhysicalScaleMicroHoleRegion> regions, byte r, byte g, byte b)
        {
            foreach (int pixelIndex in regions.SelectMany(item => item.Pixels))
            {
                int baseIndex = pixelIndex * 4;
                if (baseIndex < 0 || baseIndex + 3 >= bgra.Length)
                    continue;

                bgra[baseIndex] = (byte)((bgra[baseIndex] * 0.35) + (b * 0.65));
                bgra[baseIndex + 1] = (byte)((bgra[baseIndex + 1] * 0.35) + (g * 0.65));
                bgra[baseIndex + 2] = (byte)((bgra[baseIndex + 2] * 0.35) + (r * 0.65));
                bgra[baseIndex + 3] = 255;
            }
        }

        private static void PaintPixel(byte[] bgra, int width, int height, int x, int y, byte r, byte g, byte b)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            int baseIndex = (y * width + x) * 4;
            bgra[baseIndex] = b;
            bgra[baseIndex + 1] = g;
            bgra[baseIndex + 2] = r;
            bgra[baseIndex + 3] = 255;
        }
    }
}
