using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;

namespace Custom.DefectOverview.Services
{
    public static partial class DefectPreviewFactory
    {
	internal sealed class PreviewPixelCache
	{
		private PreviewPixelCache(BitmapSource bitmap, byte[] pixels)
		{
			Bitmap = bitmap;
			Pixels = pixels;
			PixelWidth = bitmap.PixelWidth;
			PixelHeight = bitmap.PixelHeight;
			Stride = PixelWidth * 4;
		}

		public BitmapSource Bitmap { get; }

		public byte[] Pixels { get; }

		public int PixelWidth { get; }

		public int PixelHeight { get; }

		public int Stride { get; }

		public bool Matches(BitmapSource bitmap)
		{
			return ReferenceEquals(Bitmap, bitmap)
				&& bitmap != null
				&& PixelWidth == bitmap.PixelWidth
				&& PixelHeight == bitmap.PixelHeight;
		}

		public static PreviewPixelCache Create(BitmapSource bitmap)
		{
			if (bitmap == null)
			{
				return null;
			}

			byte[] pixels = ExtractBgra32Pixels(bitmap);
			return pixels == null ? null : new PreviewPixelCache(bitmap, pixels);
		}
	}

	internal static PreviewPixelCache CreatePixelCache(BitmapSource bitmap)
	{
		return PreviewPixelCache.Create(bitmap);
	}

	private static BitmapSource CreatePreviewBitmapFromPixels(BitmapSource baseBitmap, PreviewPixelCache pixelCache, int cropWidth, int cropHeight, double cropX, double cropY, double scaledCx, double scaledCy, double scaledWidth, double scaledHeight, double scaledAngle, Result defect, double scaleX, double scaleY, int sourceCoordinateWidth, int sourceCoordinateHeight, bool suppressSegmentationBorder, out bool usedSegmentationOutline)
	{
		usedSegmentationOutline = false;
		byte[] array;
		int pixelWidth;
		int pixelHeight;
		int sourceStride;
		if (pixelCache != null && pixelCache.Matches(baseBitmap))
		{
			array = pixelCache.Pixels;
			pixelWidth = pixelCache.PixelWidth;
			pixelHeight = pixelCache.PixelHeight;
			sourceStride = pixelCache.Stride;
		}
		else
		{
			array = ExtractBgra32Pixels(baseBitmap);
			pixelWidth = baseBitmap?.PixelWidth ?? 0;
			pixelHeight = baseBitmap?.PixelHeight ?? 0;
			sourceStride = pixelWidth * 4;
		}

		if (array == null)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine("[DefectPreviewFactory] PixelExtract failed: base=" + DescribeBitmap(baseBitmap));
			return null;
		}
		int num = cropWidth * 4;
		byte[] array2 = new byte[cropHeight * num];
		int num2 = (int)Math.Floor(cropX);
		int num3 = (int)Math.Floor(cropY);
		if (!CopySourcePixels(array, pixelWidth, pixelHeight, sourceStride, num2, num3, array2, cropWidth, cropHeight, num))
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectPreviewFactory] CopySourcePixels failed: base={DescribeBitmap(baseBitmap)}, crop=({num2},{num3},{cropWidth},{cropHeight})");
			return null;
		}
		usedSegmentationOutline = DefectOverviewRuntimeOptions.UseSegmentationGeometry
			&& !suppressSegmentationBorder
			&& TryDrawSegmentationBorder(array2, cropWidth, cropHeight, defect, scaleX, scaleY, scaledCx, scaledCy, scaledWidth, scaledHeight, sourceCoordinateWidth, sourceCoordinateHeight, num2, num3);
		if (!usedSegmentationOutline && Math.Abs(scaledAngle) > 0.001)
		{
			double centerX = scaledCx - (double)num2;
			double centerY = scaledCy - (double)num3;
			DrawBorder(array2, cropWidth, cropHeight, centerX, centerY, scaledWidth, scaledHeight, scaledAngle);
		}
		else if (!usedSegmentationOutline)
		{
			double left = scaledCx - scaledWidth / 2.0 - (double)num2;
			double top = scaledCy - scaledHeight / 2.0 - (double)num3;
			DrawAxisAlignedBorder(array2, cropWidth, cropHeight, left, top, scaledWidth, scaledHeight);
		}
		BitmapSource bitmapSource = BitmapSource.Create(cropWidth, cropHeight, 96.0, 96.0, PixelFormats.Bgra32, null, array2, num);
		((Freezable)bitmapSource).Freeze();
		return bitmapSource;
	}

	private static BitmapSource LimitBitmapSize(BitmapSource bitmap, int maxDimension)
	{
		if (bitmap == null || maxDimension <= 0)
		{
			return bitmap;
		}
		double num = Math.Max(bitmap.PixelWidth, bitmap.PixelHeight);
		if (num <= (double)maxDimension)
		{
			return bitmap;
		}
		double num2 = (double)maxDimension / num;
		TransformedBitmap transformedBitmap = new TransformedBitmap(bitmap, new ScaleTransform(num2, num2));
		((Freezable)transformedBitmap).Freeze();
		return transformedBitmap;
	}


	private static double ResolveScale(int displaySize, int sourceSize)
	{
		if (displaySize <= 0 || sourceSize <= 0)
		{
			return 1.0;
		}
		return (double)displaySize / (double)sourceSize;
	}

	private static byte[] ExtractBgra32Pixels(BitmapSource source)
	{
		if (source == null || source.PixelWidth <= 0 || source.PixelHeight <= 0)
		{
			return null;
		}
		int pixelWidth = source.PixelWidth;
		int pixelHeight = source.PixelHeight;
		PixelFormat format = source.Format;
		if (format == PixelFormats.Bgra32 || format == PixelFormats.Pbgra32)
		{
			int num = pixelWidth * 4;
			byte[] array = new byte[pixelHeight * num];
			source.CopyPixels(array, num, 0);
			return array;
		}
		if (format == PixelFormats.Bgr32)
		{
			int num2 = pixelWidth * 4;
			byte[] array2 = new byte[pixelHeight * num2];
			source.CopyPixels(array2, num2, 0);
			for (int i = 0; i < array2.Length; i += 4)
			{
				array2[i + 3] = byte.MaxValue;
			}
			return array2;
		}
		if (format == PixelFormats.Bgr24)
		{
			int num3 = pixelWidth * 3;
			byte[] array3 = new byte[pixelHeight * num3];
			source.CopyPixels(array3, num3, 0);
			byte[] array4 = new byte[pixelHeight * pixelWidth * 4];
			for (int j = 0; j < pixelHeight; j++)
			{
				int num4 = j * num3;
				int num5 = j * pixelWidth * 4;
				for (int k = 0; k < pixelWidth; k++)
				{
					int num6 = num4 + k * 3;
					int num7 = num5 + k * 4;
					array4[num7] = array3[num6];
					array4[num7 + 1] = array3[num6 + 1];
					array4[num7 + 2] = array3[num6 + 2];
					array4[num7 + 3] = byte.MaxValue;
				}
			}
			return array4;
		}
		if (format == PixelFormats.Gray8)
		{
			int num8 = pixelWidth;
			byte[] array5 = new byte[pixelHeight * num8];
			source.CopyPixels(array5, num8, 0);
			byte[] array6 = new byte[pixelHeight * pixelWidth * 4];
			for (int l = 0; l < pixelHeight; l++)
			{
				int num9 = l * num8;
				int num10 = l * pixelWidth * 4;
				for (int m = 0; m < pixelWidth; m++)
				{
					byte b = array5[num9 + m];
					int num11 = num10 + m * 4;
					array6[num11] = b;
					array6[num11 + 1] = b;
					array6[num11 + 2] = b;
					array6[num11 + 3] = byte.MaxValue;
				}
			}
			return array6;
		}
		FormatConvertedBitmap formatConvertedBitmap = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0.0);
		((Freezable)formatConvertedBitmap).Freeze();
		int num12 = pixelWidth * 4;
		byte[] array7 = new byte[pixelHeight * num12];
		formatConvertedBitmap.CopyPixels(array7, num12, 0);
		return array7;
	}

	private static bool CopySourcePixels(byte[] sourcePixels, int sourceWidth, int sourceHeight, int sourceStride, int cropLeft, int cropTop, byte[] destinationPixels, int destinationWidth, int destinationHeight, int destinationStride)
	{
		int num = Math.Max(0, cropLeft);
		int num2 = Math.Max(0, cropTop);
		int num3 = Math.Max(0, -cropLeft);
		int num4 = Math.Max(0, -cropTop);
		int num5 = Math.Min(sourceWidth - num, destinationWidth - num3);
		int num6 = Math.Min(sourceHeight - num2, destinationHeight - num4);
		if (num5 <= 0 || num6 <= 0)
		{
			return false;
		}
		int count = num5 * 4;
		for (int i = 0; i < num6; i++)
		{
			int srcOffset = (num2 + i) * sourceStride + num * 4;
			int dstOffset = (num4 + i) * destinationStride + num3 * 4;
			Buffer.BlockCopy(sourcePixels, srcOffset, destinationPixels, dstOffset, count);
		}
		return true;
	}

    }
}
