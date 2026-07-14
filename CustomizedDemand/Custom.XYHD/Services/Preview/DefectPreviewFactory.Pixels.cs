using ReeYin_V.Core.DeepLearning;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Custom.XYHD.Services
{
    internal static partial class DefectPreviewFactory
    {
        private static BitmapSource CreatePreviewBitmapFromPixels(
            BitmapSource baseBitmap,
            int cropWidth,
            int cropHeight,
            double cropX,
            double cropY,
            double scaledCx,
            double scaledCy,
            double scaledWidth,
            double scaledHeight,
            Result defect,
            double scaleX,
            double scaleY,
            int sourceCoordinateWidth,
            int sourceCoordinateHeight,
            bool suppressSegmentationBorder)
        {
            var sourcePixels = ExtractBgra32Pixels(baseBitmap);
            if (sourcePixels == null)
                return null;

            int sourceWidth = baseBitmap.PixelWidth;
            int sourceHeight = baseBitmap.PixelHeight;
            int sourceStride = sourceWidth * 4;
            int destinationStride = cropWidth * 4;
            var destinationPixels = new byte[cropHeight * destinationStride];

            int cropLeft = (int)Math.Floor(cropX);
            int cropTop = (int)Math.Floor(cropY);
            CopySourcePixels(
                sourcePixels,
                sourceWidth,
                sourceHeight,
                sourceStride,
                cropLeft,
                cropTop,
                destinationPixels,
                cropWidth,
                cropHeight,
                destinationStride);

            if (suppressSegmentationBorder
                || !TryDrawSegmentationBorder(destinationPixels, cropWidth, cropHeight, defect, scaleX, scaleY, sourceCoordinateWidth, sourceCoordinateHeight, cropLeft, cropTop))
            {
                var localLeft = scaledCx - scaledWidth / 2.0 - cropLeft;
                var localTop = scaledCy - scaledHeight / 2.0 - cropTop;
                DrawBorder(destinationPixels, cropWidth, cropHeight, localLeft, localTop, scaledWidth, scaledHeight);
            }

            var bitmap = BitmapSource.Create(cropWidth, cropHeight, 96, 96, PixelFormats.Bgra32, null, destinationPixels, destinationStride);
            bitmap.Freeze();
            return bitmap;
        }

        private static BitmapSource LimitBitmapSize(BitmapSource bitmap, int maxDimension)
        {
            if (bitmap == null || maxDimension <= 0)
                return bitmap;

            double currentMax = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);
            if (currentMax <= maxDimension)
                return bitmap;

            double scale = maxDimension / currentMax;
            TransformedBitmap scaled = new(bitmap, new ScaleTransform(scale, scale));
            scaled.Freeze();
            return scaled;
        }

        private static byte[] ExtractBgra32Pixels(BitmapSource source)
        {
            if (source == null || source.PixelWidth <= 0 || source.PixelHeight <= 0)
                return null;

            int width = source.PixelWidth;
            int height = source.PixelHeight;
            var format = source.Format;

            if (format == PixelFormats.Bgra32 || format == PixelFormats.Pbgra32)
            {
                int stride = width * 4;
                var pixels = new byte[height * stride];
                source.CopyPixels(pixels, stride, 0);
                return pixels;
            }

            if (format == PixelFormats.Bgr32)
            {
                int stride = width * 4;
                var raw = new byte[height * stride];
                source.CopyPixels(raw, stride, 0);
                for (int offset = 0; offset < raw.Length; offset += 4)
                    raw[offset + 3] = 255;
                return raw;
            }

            if (format == PixelFormats.Bgr24)
            {
                int rawStride = width * 3;
                var raw = new byte[height * rawStride];
                source.CopyPixels(raw, rawStride, 0);
                var pixels = new byte[height * width * 4];
                for (int y = 0; y < height; y++)
                {
                    int rawRow = y * rawStride;
                    int dstRow = y * width * 4;
                    for (int x = 0; x < width; x++)
                    {
                        int rawIndex = rawRow + x * 3;
                        int dstIndex = dstRow + x * 4;
                        pixels[dstIndex] = raw[rawIndex];
                        pixels[dstIndex + 1] = raw[rawIndex + 1];
                        pixels[dstIndex + 2] = raw[rawIndex + 2];
                        pixels[dstIndex + 3] = 255;
                    }
                }

                return pixels;
            }

            if (format == PixelFormats.Gray8)
            {
                int rawStride = width;
                var raw = new byte[height * rawStride];
                source.CopyPixels(raw, rawStride, 0);
                var pixels = new byte[height * width * 4];
                for (int y = 0; y < height; y++)
                {
                    int rawRow = y * rawStride;
                    int dstRow = y * width * 4;
                    for (int x = 0; x < width; x++)
                    {
                        byte value = raw[rawRow + x];
                        int dstIndex = dstRow + x * 4;
                        pixels[dstIndex] = value;
                        pixels[dstIndex + 1] = value;
                        pixels[dstIndex + 2] = value;
                        pixels[dstIndex + 3] = 255;
                    }
                }

                return pixels;
            }

            var converted = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);
            converted.Freeze();
            int convertedStride = width * 4;
            var convertedPixels = new byte[height * convertedStride];
            converted.CopyPixels(convertedPixels, convertedStride, 0);
            return convertedPixels;
        }

        private static void CopySourcePixels(
            byte[] sourcePixels,
            int sourceWidth,
            int sourceHeight,
            int sourceStride,
            int cropLeft,
            int cropTop,
            byte[] destinationPixels,
            int destinationWidth,
            int destinationHeight,
            int destinationStride)
        {
            int sourceStartX = Math.Max(0, cropLeft);
            int sourceStartY = Math.Max(0, cropTop);
            int destinationStartX = Math.Max(0, -cropLeft);
            int destinationStartY = Math.Max(0, -cropTop);

            int copyWidth = Math.Min(sourceWidth - sourceStartX, destinationWidth - destinationStartX);
            int copyHeight = Math.Min(sourceHeight - sourceStartY, destinationHeight - destinationStartY);
            if (copyWidth <= 0 || copyHeight <= 0)
                return;

            int rowBytes = copyWidth * 4;
            for (int row = 0; row < copyHeight; row++)
            {
                int sourceOffset = (sourceStartY + row) * sourceStride + sourceStartX * 4;
                int destinationOffset = (destinationStartY + row) * destinationStride + destinationStartX * 4;
                Buffer.BlockCopy(sourcePixels, sourceOffset, destinationPixels, destinationOffset, rowBytes);
            }
        }
    }
}
