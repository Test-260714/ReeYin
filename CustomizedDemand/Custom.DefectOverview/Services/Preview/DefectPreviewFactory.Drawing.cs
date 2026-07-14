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
	private static bool TryDrawSegmentationBorder(byte[] pixels, int width, int height, Result defect, double scaleX, double scaleY, double expectedScaledCx, double expectedScaledCy, double expectedScaledWidth, double expectedScaledHeight, int sourceCoordinateWidth, int sourceCoordinateHeight, int cropLeft, int cropTop)
	{
		if (pixels == null || width <= 0 || height <= 0 || defect?.Seg == null || !defect.Seg.IsInitialized() || !IsFinite(scaleX) || !IsFinite(scaleY) || scaleX <= 0.0 || scaleY <= 0.0)
		{
			return false;
		}
		HObject hObject = null;
		HObject contours = null;
		try
		{
			hObject = UnionSegRegion(defect.Seg);
			HObject hObject2 = ((hObject != null && hObject.IsInitialized()) ? hObject : defect.Seg);
			ResolveSegCoordinateOffset(hObject2, sourceCoordinateWidth, sourceCoordinateHeight, out var coordinateOffsetX, out var coordinateOffsetY);
			if (!IsSegmentationAlignedWithExpectedGeometry(hObject2, scaleX, scaleY, expectedScaledCx, expectedScaledCy, expectedScaledWidth, expectedScaledHeight, coordinateOffsetX, coordinateOffsetY))
			{
				return false;
			}
			HOperatorSet.GenContourRegionXld(hObject2, out contours, "border");
			if (contours == null || !contours.IsInitialized())
			{
				return false;
			}
			HOperatorSet.CountObj(contours, out var number);
			int num = ((number.TupleLength() != 0) ? number[0].I : 0);
			if (num <= 0)
			{
				return false;
			}
			int thickness = 1;
			bool result = false;
			for (int i = 1; i <= num; i++)
			{
				using HObject hObject3 = contours.SelectObj(i);
				if (hObject3 == null || !hObject3.IsInitialized())
				{
					continue;
				}
				HOperatorSet.GetContourXld(hObject3, out var row, out var col);
				int num2 = Math.Min(row.TupleLength(), col.TupleLength());
				if (num2 < 2)
				{
					continue;
				}
				double num3 = (col[0].D - coordinateOffsetX) * scaleX - (double)cropLeft;
				double num4 = (row[0].D - coordinateOffsetY) * scaleY - (double)cropTop;
				double x = num3;
				double y = num4;
				for (int j = 1; j < num2; j++)
				{
					double num5 = (col[j].D - coordinateOffsetX) * scaleX - (double)cropLeft;
					double num6 = (row[j].D - coordinateOffsetY) * scaleY - (double)cropTop;
					if (SegmentMayTouchViewport(x, y, num5, num6, width, height))
					{
						DrawLine(pixels, width, height, x, y, num5, num6, thickness);
						result = true;
					}
					x = num5;
					y = num6;
				}
				if (SegmentMayTouchViewport(x, y, num3, num4, width, height))
				{
					DrawLine(pixels, width, height, x, y, num3, num4, thickness);
					result = true;
				}
			}
			return result;
		}
		catch
		{
			return false;
		}
		finally
		{
			contours?.Dispose();
			hObject?.Dispose();
		}
	}

	private static bool IsSegmentationAlignedWithExpectedGeometry(HObject geometryRegion, double scaleX, double scaleY, double expectedScaledCx, double expectedScaledCy, double expectedScaledWidth, double expectedScaledHeight, double coordinateOffsetX, double coordinateOffsetY)
	{
		if (geometryRegion == null || !geometryRegion.IsInitialized() || !IsFinite(scaleX) || !IsFinite(scaleY) || !IsFinite(expectedScaledCx) || !IsFinite(expectedScaledCy))
		{
			return true;
		}
		try
		{
			HOperatorSet.SmallestRectangle1(geometryRegion, out var row, out var column, out var row2, out var column2);
			if (row.TupleLength() == 0 || column.TupleLength() == 0 || row2.TupleLength() == 0 || column2.TupleLength() == 0)
			{
				return true;
			}
			double num = (column[0].D + column2[0].D + 1.0 - coordinateOffsetX * 2.0) * 0.5 * scaleX;
			double num2 = (row[0].D + row2[0].D + 1.0 - coordinateOffsetY * 2.0) * 0.5 * scaleY;
			if (!IsFinite(num) || !IsFinite(num2))
			{
				return true;
			}
			double num3 = Math.Max(24.0, Math.Max(1.0, expectedScaledWidth) * 2.5);
			double num4 = Math.Max(24.0, Math.Max(1.0, expectedScaledHeight) * 2.5);
			return Math.Abs(num - expectedScaledCx) <= num3 && Math.Abs(num2 - expectedScaledCy) <= num4;
		}
		catch
		{
			return true;
		}
	}

	private static bool SegmentMayTouchViewport(double x1, double y1, double x2, double y2, int width, int height)
	{
		if (width <= 0 || height <= 0)
		{
			return false;
		}
		double num = Math.Min(x1, x2);
		double num2 = Math.Max(x1, x2);
		double num3 = Math.Min(y1, y2);
		double num4 = Math.Max(y1, y2);
		return num2 >= 0.0 && num4 >= 0.0 && num < (double)width && num3 < (double)height;
	}

	private static void ResolveSegCoordinateOffset(HObject geometryRegion, int sourceCoordinateWidth, int sourceCoordinateHeight, out double coordinateOffsetX, out double coordinateOffsetY)
	{
		coordinateOffsetX = 0.0;
		coordinateOffsetY = 0.0;
		if (geometryRegion == null || !geometryRegion.IsInitialized())
		{
			return;
		}
		try
		{
			HOperatorSet.SmallestRectangle1(geometryRegion, out var row, out var column, out var row2, out var column2);
			if (row.TupleLength() == 0 || column.TupleLength() == 0 || row2.TupleLength() == 0 || column2.TupleLength() == 0)
			{
				return;
			}
			double d = row[0].D;
			double d2 = column[0].D;
			double d3 = row2[0].D;
			double d4 = column2[0].D;
			if (sourceCoordinateWidth > 1)
			{
				double num = (d2 + d4 + 1.0) * 0.5;
				if (double.IsFinite(num) && num > (double)sourceCoordinateWidth && num <= (double)sourceCoordinateWidth * 2.0 + 1.0)
				{
					coordinateOffsetX = sourceCoordinateWidth;
				}
			}
			if (sourceCoordinateHeight > 1)
			{
				double num2 = (d + d3 + 1.0) * 0.5;
				if (double.IsFinite(num2) && num2 > (double)sourceCoordinateHeight && num2 <= (double)sourceCoordinateHeight * 2.0 + 1.0)
				{
					coordinateOffsetY = sourceCoordinateHeight;
				}
			}
		}
		catch
		{
			coordinateOffsetX = 0.0;
			coordinateOffsetY = 0.0;
		}
	}

	private static void ClampCropOrigin(ref double cropX, ref double cropY, int cropWidth, int cropHeight, int sourceWidth, int sourceHeight)
	{
		if (sourceWidth > 0 && cropWidth > 0 && cropWidth <= sourceWidth)
		{
			cropX = Math.Clamp(cropX, 0.0, sourceWidth - cropWidth);
		}
		if (sourceHeight > 0 && cropHeight > 0 && cropHeight <= sourceHeight)
		{
			cropY = Math.Clamp(cropY, 0.0, sourceHeight - cropHeight);
		}
	}


	private static void DrawBorder(byte[] pixels, int width, int height, double centerX, double centerY, double rectWidth, double rectHeight, double angle)
	{
		if (pixels == null || width <= 0 || height <= 0 || rectWidth <= 0.0 || rectHeight <= 0.0)
		{
			return;
		}
		if (Math.Abs(angle) > 0.001)
		{
			DrawRotatedBorder(pixels, width, height, centerX, centerY, rectWidth, rectHeight, angle);
			return;
		}
		double num = centerX - rectWidth / 2.0;
		double num2 = centerY - rectHeight / 2.0;
		int num3 = 1;
		int num4 = (int)Math.Floor(num);
		int num5 = (int)Math.Floor(num2);
		int num6 = (int)Math.Ceiling(num + rectWidth) - 1;
		int num7 = (int)Math.Ceiling(num2 + rectHeight) - 1;
		if (num6 >= 0 && num7 >= 0 && num4 < width && num5 < height)
		{
			num4 = Math.Max(0, num4);
			num5 = Math.Max(0, num5);
			num6 = Math.Min(width - 1, num6);
			num7 = Math.Min(height - 1, num7);
			for (int i = 0; i < num3; i++)
			{
				DrawHorizontalLine(pixels, width, height, num4, num6, num5 + i);
				DrawHorizontalLine(pixels, width, height, num4, num6, num7 - i);
				DrawVerticalLine(pixels, width, height, num4 + i, num5, num7);
				DrawVerticalLine(pixels, width, height, num6 - i, num5, num7);
			}
		}
	}

	private static void DrawAxisAlignedBorder(byte[] pixels, int width, int height, double left, double top, double rectWidth, double rectHeight)
	{
		if (pixels == null || width <= 0 || height <= 0 || rectWidth <= 0.0 || rectHeight <= 0.0)
		{
			return;
		}
		int num = 1;
		int num2 = (int)Math.Floor(left);
		int num3 = (int)Math.Floor(top);
		int num4 = (int)Math.Ceiling(left + rectWidth) - 1;
		int num5 = (int)Math.Ceiling(top + rectHeight) - 1;
		if (num4 >= 0 && num5 >= 0 && num2 < width && num3 < height)
		{
			num2 = Math.Max(0, num2);
			num3 = Math.Max(0, num3);
			num4 = Math.Min(width - 1, num4);
			num5 = Math.Min(height - 1, num5);
			for (int i = 0; i < num; i++)
			{
				DrawHorizontalLine(pixels, width, height, num2, num4, num3 + i);
				DrawHorizontalLine(pixels, width, height, num2, num4, num5 - i);
				DrawVerticalLine(pixels, width, height, num2 + i, num3, num5);
				DrawVerticalLine(pixels, width, height, num4 - i, num3, num5);
			}
		}
	}

	private static void DrawRotatedBorder(byte[] pixels, int width, int height, double centerX, double centerY, double rectWidth, double rectHeight, double angle)
	{
		if (pixels == null || width <= 0 || height <= 0 || rectWidth <= 0.0 || rectHeight <= 0.0)
		{
			return;
		}

		double halfWidth = Math.Max(0.5, rectWidth / 2.0);
		double halfHeight = Math.Max(0.5, rectHeight / 2.0);
		double cos = Math.Cos(angle);
		double sin = Math.Sin(angle);

		(double X, double Y) Transform(double x, double y)
		{
			return (
				centerX + x * cos - y * sin,
				centerY + x * sin + y * cos);
		}

		var p1 = Transform(-halfWidth, -halfHeight);
		var p2 = Transform(halfWidth, -halfHeight);
		var p3 = Transform(halfWidth, halfHeight);
		var p4 = Transform(-halfWidth, halfHeight);
		const int thickness = 1;
		DrawLine(pixels, width, height, p1.X, p1.Y, p2.X, p2.Y, thickness);
		DrawLine(pixels, width, height, p2.X, p2.Y, p3.X, p3.Y, thickness);
		DrawLine(pixels, width, height, p3.X, p3.Y, p4.X, p4.Y, thickness);
		DrawLine(pixels, width, height, p4.X, p4.Y, p1.X, p1.Y, thickness);
	}

	private static void DrawHorizontalLine(byte[] pixels, int width, int height, int x1, int x2, int y)
	{
		if (y >= 0 && y < height)
		{
			int num = Math.Max(0, Math.Min(x1, x2));
			int num2 = Math.Min(width - 1, Math.Max(x1, x2));
			for (int i = num; i <= num2; i++)
			{
				SetRedPixel(pixels, width, i, y);
			}
		}
	}

	private static void DrawVerticalLine(byte[] pixels, int width, int height, int x, int y1, int y2)
	{
		if (x >= 0 && x < width)
		{
			int num = Math.Max(0, Math.Min(y1, y2));
			int num2 = Math.Min(height - 1, Math.Max(y1, y2));
			for (int i = num; i <= num2; i++)
			{
				SetRedPixel(pixels, width, x, i);
			}
		}
	}

	private static void DrawLine(byte[] pixels, int width, int height, double x1, double y1, double x2, double y2, int thickness)
	{
		int num = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1))));
		for (int i = 0; i <= num; i++)
		{
			double num2 = (double)i / (double)num;
			int x3 = (int)Math.Round(x1 + (x2 - x1) * num2);
			int y3 = (int)Math.Round(y1 + (y2 - y1) * num2);
			PaintRedPoint(pixels, width, height, x3, y3, thickness);
		}
	}

	private static void PaintRedPoint(byte[] pixels, int width, int height, int x, int y, int thickness)
	{
		int num = Math.Max(0, thickness / 2);
		for (int i = -num; i <= num; i++)
		{
			int num2 = y + i;
			if (num2 < 0 || num2 >= height)
			{
				continue;
			}
			for (int j = -num; j <= num; j++)
			{
				int num3 = x + j;
				if (num3 >= 0 && num3 < width)
				{
					SetRedPixel(pixels, width, num3, num2);
				}
			}
		}
	}

	private static void SetRedPixel(byte[] pixels, int width, int x, int y)
	{
		int num = (y * width + x) * 4;
		if (num >= 0 && num + 3 < pixels.Length)
		{
			pixels[num] = 0;
			pixels[num + 1] = 0;
			pixels[num + 2] = byte.MaxValue;
			pixels[num + 3] = byte.MaxValue;
		}
	}

    }
}
