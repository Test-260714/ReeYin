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
	private static void ResolvePreviewGeometry(Result defect, int displayWidth, int displayHeight, int sourceWidth, int sourceHeight, out double scaledCx, out double scaledCy, out double scaledWidth, out double scaledHeight, out double scaledAngle, out bool usedFallbackGeometry, out string geometrySource)
	{
		double num = ResolveScale(displayWidth, sourceWidth);
		double num2 = ResolveScale(displayHeight, sourceHeight);
		int num3 = ((sourceWidth > 0) ? sourceWidth : displayWidth);
		int num4 = ((sourceHeight > 0) ? sourceHeight : displayHeight);
		double cx2;
		double cy2;
		double width2;
		double height2;
		double angle2;
		if (TryResolveDisplayGeometryMetadata(defect, out var cx, out var cy, out var width, out var height, out var angle))
		{
			geometrySource = "display-metadata";
		}
		else if (DefectOverviewRuntimeOptions.UseSegmentationGeometry && TryResolveGeometryFromSeg(defect, num3, num4, out cx2, out cy2, out width2, out height2, out angle2))
		{
			cx = cx2;
			cy = cy2;
			width = width2;
			height = height2;
			angle = angle2;
			geometrySource = "seg";
		}
		else
		{
			bool flag = ShouldUseRotatedBoundingBox(defect);
			cx = ResolveMetadataDouble(defect, defect.Cx, "DefectOverview_DisplayCenterX", "XYHD_DisplayCenterX");
			cy = ResolveMetadataDouble(defect, defect.Cy, "DefectOverview_DisplayCenterY", "XYHD_DisplayCenterY");
			width = ResolveMetadataDouble(defect, defect.Width, "DefectOverview_DisplayPixelWidth", "XYHD_DisplayPixelWidth");
			height = ResolveMetadataDouble(defect, defect.Height, "DefectOverview_DisplayPixelHeight", "XYHD_DisplayPixelHeight");
			angle = ResolveMetadataDouble(defect, flag ? ResolveDisplayAngle(defect) : 0.0, "DefectOverview_DisplayAngle");
			geometrySource = "result";
		}
		usedFallbackGeometry = !HasUsableGeometry(cx, cy, width, height);
		if (usedFallbackGeometry)
		{
			cx = ((num3 > 0) ? ((double)num3 / 2.0) : 0.0);
			cy = ((num4 > 0) ? ((double)num4 / 2.0) : 0.0);
			width = ((num3 > 0) ? Math.Min(num3, 96.0) : 96.0);
			height = ((num4 > 0) ? Math.Min(num4, 96.0) : 96.0);
			angle = 0.0;
			geometrySource = "fallback";
		}
		else
		{
			if (num3 > 0)
			{
				cx = Math.Clamp(cx, 0.0, num3);
				width = Math.Min(width, num3);
			}
			if (num4 > 0)
			{
				cy = Math.Clamp(cy, 0.0, num4);
				height = Math.Min(height, num4);
			}
		}
		scaledCx = cx * num;
		scaledCy = cy * num2;
		scaledWidth = Math.Max(1.0, width * num);
		scaledHeight = Math.Max(1.0, height * num2);
		scaledAngle = angle;
	}

	private static bool TryResolveDisplayGeometryMetadata(Result defect, out double cx, out double cy, out double width, out double height, out double angle)
	{
		cx = 0.0;
		cy = 0.0;
		width = 0.0;
		height = 0.0;
		angle = 0.0;
		if (!TryResolveMetadataDouble(defect, out cx, "DefectOverview_DisplayCenterX", "XYHD_DisplayCenterX") || !TryResolveMetadataDouble(defect, out cy, "DefectOverview_DisplayCenterY", "XYHD_DisplayCenterY"))
		{
			return false;
		}
		if (!TryResolveMetadataDouble(defect, out width, "DefectOverview_DisplayPixelWidth", "XYHD_DisplayPixelWidth"))
		{
			width = ((double?)defect?.Width) ?? 0.0;
		}
		if (!TryResolveMetadataDouble(defect, out height, "DefectOverview_DisplayPixelHeight", "XYHD_DisplayPixelHeight"))
		{
			height = ((double?)defect?.Height) ?? 0.0;
		}
		if (!TryResolveMetadataDouble(defect, out angle, "DefectOverview_DisplayAngle"))
		{
			angle = (ShouldUseRotatedBoundingBox(defect) ? ResolveDisplayAngle(defect) : 0.0);
		}
		return HasUsableGeometry(cx, cy, width, height);
	}

	private static bool ShouldUseRotatedBoundingBox(Result defect)
	{
		return defect != null && defect.ModelType == eDeepLearningModelType.旋转框检测;
	}

	private static bool HasUsableGeometry(double cx, double cy, double width, double height)
	{
		return IsFinite(cx) && IsFinite(cy) && IsFinite(width) && IsFinite(height) && width > 0.0 && height > 0.0;
	}

	private static bool IsFinite(double value)
	{
		return !double.IsNaN(value) && !double.IsInfinity(value);
	}

	private static double ResolveDisplayAngle(Result defect)
	{
		if (defect == null || !IsFinite(defect.Angle))
		{
			return 0.0;
		}
		return 0f - defect.Angle;
	}

	private static void ResolveRotatedBounds(double width, double height, double angle, out double boundsWidth, out double boundsHeight)
	{
		double num = Math.Max(1.0, width);
		double num2 = Math.Max(1.0, height);
		if (!IsFinite(angle))
		{
			boundsWidth = num;
			boundsHeight = num2;
			return;
		}
		double num3 = Math.Abs(Math.Sin(angle));
		double num4 = Math.Abs(Math.Cos(angle));
		boundsWidth = Math.Max(1.0, num * num4 + num2 * num3);
		boundsHeight = Math.Max(1.0, num * num3 + num2 * num4);
	}

	private static bool IsGeometryWithinSourceBounds(double cx, double cy, double width, double height, int sourceWidth, int sourceHeight, int displayWidth, int displayHeight)
	{
		if (double.IsNaN(cx) || double.IsNaN(cy) || double.IsNaN(width) || double.IsNaN(height))
		{
			return false;
		}
		if (double.IsInfinity(cx) || double.IsInfinity(cy) || double.IsInfinity(width) || double.IsInfinity(height))
		{
			return false;
		}
		if (width <= 0.0 || height <= 0.0)
		{
			return false;
		}
		int num = ((sourceWidth > 0) ? sourceWidth : displayWidth);
		int num2 = ((sourceHeight > 0) ? sourceHeight : displayHeight);
		if (num <= 0 || num2 <= 0)
		{
			return true;
		}
		double num3 = cx - width / 2.0;
		double num4 = cy - height / 2.0;
		double num5 = cx + width / 2.0;
		double num6 = cy + height / 2.0;
		double num7 = Math.Max(32.0, (double)num * 0.05);
		double num8 = Math.Max(32.0, (double)num2 * 0.05);
		return num5 >= 0.0 - num7 && num6 >= 0.0 - num8 && num3 <= (double)num + num7 && num4 <= (double)num2 + num8;
	}

	private static bool TryResolveGeometryFromSeg(Result defect, int sourceCoordinateWidth, int sourceCoordinateHeight, out double cx, out double cy, out double width, out double height, out double angle)
	{
		cx = 0.0;
		cy = 0.0;
		width = 0.0;
		height = 0.0;
		angle = 0.0;
		try
		{
			if (defect?.Seg == null || !defect.Seg.IsInitialized())
			{
				return false;
			}
			using HObject hObject = UnionSegRegion(defect.Seg);
			HObject hObject2 = ((hObject != null && hObject.IsInitialized()) ? hObject : defect.Seg);
			ResolveSegCoordinateOffset(hObject2, sourceCoordinateWidth, sourceCoordinateHeight, out var coordinateOffsetX, out var coordinateOffsetY);
			HOperatorSet.SmallestRectangle2(hObject2, out var row, out var column, out var phi, out var length, out var length2);
			if (row.TupleLength() == 0 || column.TupleLength() == 0 || phi.TupleLength() == 0 || length.TupleLength() == 0 || length2.TupleLength() == 0)
			{
				return false;
			}
			double num = column[0].D - coordinateOffsetX;
			double num2 = row[0].D - coordinateOffsetY;
			double num3 = length[0].D * 2.0;
			double num4 = length2[0].D * 2.0;
			double d = phi[0].D;
			if (!IsFinite(num) || !IsFinite(num2) || !IsFinite(num3) || !IsFinite(num4) || !IsFinite(d))
			{
				return false;
			}
			width = Math.Max(1.0, num3);
			height = Math.Max(1.0, num4);
			cx = num;
			cy = num2;
			angle = d;
			return true;
		}
		catch
		{
			return false;
		}
	}

    }
}
