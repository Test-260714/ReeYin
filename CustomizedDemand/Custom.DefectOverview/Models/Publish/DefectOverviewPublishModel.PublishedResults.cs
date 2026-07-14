using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Custom.DefectOverview.Services;
using HalconDotNet;
using Newtonsoft.Json;
using ReeYin_V.Core.DeepLearning;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel : ModelParamBase
    {
	private List<Result> BuildPublishedResults(HImage image, List<Result> inputResults, string frameKey, string frameIdText, double laneWidth, double pixelEquivalentX, double pixelEquivalentY, double edgeCalibrationX, string schemeFilePath)
	{
		return BuildPublishedResults(image, inputResults, frameKey, frameIdText, laneWidth, pixelEquivalentX, pixelEquivalentY, edgeCalibrationX, schemeFilePath, FrameLayout, PathRole, ResolvePathName());
	}

	private List<Result> BuildPublishedResults(HImage image, List<Result> inputResults, string frameKey, string frameIdText, double laneWidth, double pixelEquivalentX, double pixelEquivalentY, double edgeCalibrationX, string schemeFilePath, DefectOverviewFrameLayout frameLayout, DefectOverviewPathRole pathRole, string pathName)
	{
		if (inputResults == null)
		{
			inputResults = new List<Result>();
		}
		if (ResultMode == DefectOverviewResultMode.FilteredResults)
		{
			return inputResults.Where((Result item) => item != null).ToList();
		}
		return ResolvePostProcessService().FilterResults(new DefectOverviewPathPacket
		{
			SourceName = (string.IsNullOrWhiteSpace(SourceName) ? "DefectOverview" : SourceName),
			FrameKey = frameKey,
			FrameIdText = frameIdText,
			CreatedUtc = DateTime.UtcNow,
			FrameLayout = frameLayout,
			PathRole = pathRole,
			PathName = pathName,
			PathImage = image,
			OriginalImage = image,
			ApplyPostProcess = true,
			IsNg = (inputResults.Count > 0),
			Results = inputResults,
			LaneWidth = ((laneWidth > 0.0) ? new double?(laneWidth) : ((double?)null)),
			PixelEquivalentX = ((pixelEquivalentX > 0.0) ? new double?(pixelEquivalentX) : ((double?)null)),
			PixelEquivalentY = ((pixelEquivalentY > 0.0) ? new double?(pixelEquivalentY) : ((double?)null)),
			EdgeCalibrationX = edgeCalibrationX,
			SchemeFilePath = (schemeFilePath ?? string.Empty)
		})?.Where((Result item) => item != null).ToList() ?? new List<Result>();
	}

	private static void AttachPreviewMetadata(HImage image, IReadOnlyList<Result> results)
	{
		if (results == null || results.Count == 0)
		{
			return;
		}
		BitmapSource bitmapSource = null;
		bool attachBitmap = DefectOverviewRuntimeOptions.AttachMetadataBitmap;
		int width = 0;
		int height = 0;
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		try
		{
			if (image != null && image.IsInitialized())
			{
				image.GetImageSize(out width, out height);
				if (attachBitmap)
				{
					bitmapSource = DefectPreviewFactory.CreateBitmapFromHImage(image);
				}
			}
		}
		catch
		{
			bitmapSource = null;
			width = 0;
			height = 0;
		}
		if (width <= 0 || height <= 0)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] PreviewMetadata skipped: no current image, keep existing result metadata, results={results.Count}");
			return;
		}
		if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] PreviewMetadata source={width}x{height}, metadataBitmap={DescribeBitmapState(bitmapSource)}, results={results.Count}");
		}
		for (int i = 0; i < results.Count; i++)
		{
			Result result = results[i];
			if (result != null)
			{
				Result result2 = result;
				if (result2.Others == null)
				{
					result2.Others = new Dictionary<string, object>();
				}
				if (bitmapSource != null)
				{
					result.Others["DefectOverview_DisplayTargetBitmap"] = bitmapSource;
				}
				else
				{
					result.Others.Remove("DefectOverview_DisplayTargetBitmap");
					result.Others.Remove("XYHD_DisplayTargetBitmap");
				}
				if (width > 0)
				{
					result.Others["DefectOverview_DisplayTargetSourceWidth"] = width;
				}
				if (height > 0)
				{
					result.Others["DefectOverview_DisplayTargetSourceHeight"] = height;
				}
				double centerX;
				double centerY;
				double width2;
				double height2;
				double angle;
				string text;
				if (HasPreviewGeometryMetadata(result))
				{
					centerX = ResolvePreviewGeometryMetadata(result, result.Cx, "DefectOverview_DisplayCenterX");
					centerY = ResolvePreviewGeometryMetadata(result, result.Cy, "DefectOverview_DisplayCenterY");
					width2 = ResolvePreviewGeometryMetadata(result, Math.Max(1.0, result.Width), true, "DefectOverview_DisplayPixelWidth", "XYHD_DisplayPixelWidth");
					height2 = ResolvePreviewGeometryMetadata(result, Math.Max(1.0, result.Height), true, "DefectOverview_DisplayPixelHeight", "XYHD_DisplayPixelHeight");
					angle = ResolvePreviewGeometryMetadata(result, 0.0, "DefectOverview_DisplayAngle");
					text = "metadata";
				}
				else
				{
					text = ResolvePreviewGeometryForDefectWall(result, out centerX, out centerY, out width2, out height2, out angle);
				}
				switch (text)
				{
				case "metadata":
					num++;
					break;
				case "seg":
					num2++;
					break;
				case "rotated":
					num3++;
					break;
				default:
					num4++;
					break;
				}
				result.Others["DefectOverview_DisplayCenterX"] = centerX;
				result.Others["DefectOverview_DisplayCenterY"] = centerY;
				result.Others["DefectOverview_DisplayPixelWidth"] = width2;
				result.Others["DefectOverview_DisplayPixelHeight"] = height2;
				result.Others["DefectOverview_DisplayAngle"] = angle;
				if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
				{
					Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] PreviewResult[{i}] class={DescribeResultIdentity(result)}, geo={text}, center=({centerX:F1},{centerY:F1}), size=({width2:F1},{height2:F1}), angle={angle:F4}, segReady={HasInitializedSeg(result)}");
				}
			}
		}
		if (Custom.DefectOverview.DefectOverviewConsole.IsVerboseEnabled)
		{
			Custom.DefectOverview.DefectOverviewConsole.WriteLine($"[DefectOverviewPublish] PreviewGeometry metadata={num}, seg={num2}, rotated={num3}, boxFallback={num4}");
		}
	}

	private static void ClearPreviewMetadata(IReadOnlyList<Result> results)
	{
		if (results == null)
		{
			return;
		}
		foreach (Result result in results)
		{
			if (result?.Others != null)
			{
				result.Others.Remove("DefectOverview_DisplayTargetBitmap");
				result.Others.Remove("DefectOverview_DisplayTargetSourceWidth");
				result.Others.Remove("DefectOverview_DisplayTargetSourceHeight");
				result.Others.Remove("DefectOverview_DisplayCenterX");
				result.Others.Remove("DefectOverview_DisplayCenterY");
				result.Others.Remove("DefectOverview_DisplayPixelWidth");
				result.Others.Remove("DefectOverview_DisplayPixelHeight");
				result.Others.Remove("DefectOverview_DisplayAngle");
				result.Others.Remove("XYHD_DisplayTargetBitmap");
				result.Others.Remove("XYHD_DisplayTargetSourceWidth");
				result.Others.Remove("XYHD_DisplayTargetSourceHeight");
				result.Others.Remove("XYHD_DisplayPixelWidth");
				result.Others.Remove("XYHD_DisplayPixelHeight");
			}
		}
	}

	private static double ResolvePreviewGeometryMetadata(Result result, double fallback, params string[] keys)
	{
		return ResolvePreviewGeometryMetadata(result, fallback, requirePositive: false, keys);
	}

	private static double ResolvePreviewGeometryMetadata(Result result, double fallback, bool requirePositive, params string[] keys)
	{
		if (result?.Others == null || keys == null || keys.Length == 0)
		{
			return fallback;
		}
		foreach (string text in keys)
		{
			if (string.IsNullOrWhiteSpace(text) || !result.Others.TryGetValue(text, out var value) || value == null)
			{
				continue;
			}
			try
			{
				double num = Convert.ToDouble(value, CultureInfo.InvariantCulture);
				if (!double.IsNaN(num) && !double.IsInfinity(num) && (!requirePositive || num > 0.0))
				{
					return num;
				}
			}
			catch
			{
			}
		}
		return fallback;
	}

	private static bool HasPreviewGeometryMetadata(Result result)
	{
		if (result?.Others == null)
		{
			return false;
		}
		bool flag = result.Others.ContainsKey("DefectOverview_DisplayCenterX");
		bool flag2 = result.Others.ContainsKey("DefectOverview_DisplayCenterY");
		double num = ResolvePreviewGeometryMetadata(result, 0.0, true, "DefectOverview_DisplayPixelWidth", "XYHD_DisplayPixelWidth");
		double num2 = ResolvePreviewGeometryMetadata(result, 0.0, true, "DefectOverview_DisplayPixelHeight", "XYHD_DisplayPixelHeight");
		return flag && flag2 && num > 0.0 && num2 > 0.0;
	}

	private static string ResolvePreviewGeometryForDefectWall(Result result, out double centerX, out double centerY, out double width, out double height, out double angle)
	{
		centerX = 0.0;
		centerY = 0.0;
		width = 1.0;
		height = 1.0;
		angle = 0.0;
		if (result == null)
		{
			return "box";
		}
		double num = ResolvePreviewDisplayAngle(result);
		if (Math.Abs(num) > 0.001)
		{
			centerX = result.Cx;
			centerY = result.Cy;
			width = Math.Max(1.0, result.Width);
			height = Math.Max(1.0, result.Height);
			angle = num;
			return "rotated";
		}
		if (DefectOverviewRuntimeOptions.UseSegmentationGeometry
			&& TryResolvePreviewGeometryFromSeg(result, out centerX, out centerY, out width, out height, out angle))
		{
			return "seg";
		}
		centerX = result.Cx;
		centerY = result.Cy;
		width = Math.Max(1.0, result.Width);
		height = Math.Max(1.0, result.Height);
		angle = 0.0;
		return "box";
	}

	private static bool TryResolvePreviewGeometryFromSeg(Result result, out double centerX, out double centerY, out double width, out double height, out double angle)
	{
		centerX = 0.0;
		centerY = 0.0;
		width = 0.0;
		height = 0.0;
		angle = 0.0;
		try
		{
			if (!HasInitializedSeg(result))
			{
				return false;
			}
			HOperatorSet.Union1(result.Seg, out var regionUnion);
			try
			{
				HObject regions = ((regionUnion != null && regionUnion.IsInitialized()) ? regionUnion : result.Seg);
				HOperatorSet.SmallestRectangle2(regions, out var row, out var column, out var phi, out var length, out var length2);
				if (row.TupleLength() == 0 || column.TupleLength() == 0 || phi.TupleLength() == 0 || length.TupleLength() == 0 || length2.TupleLength() == 0)
				{
					return false;
				}
				double d = column[0].D;
				double d2 = row[0].D;
				double num = length[0].D * 2.0;
				double num2 = length2[0].D * 2.0;
				double d3 = phi[0].D;
				if (double.IsNaN(d) || double.IsNaN(d2) || double.IsNaN(num) || double.IsNaN(num2) || double.IsNaN(d3) || double.IsInfinity(d) || double.IsInfinity(d2) || double.IsInfinity(num) || double.IsInfinity(num2) || double.IsInfinity(d3))
				{
					return false;
				}
				centerX = d;
				centerY = d2;
				width = Math.Max(1.0, num);
				height = Math.Max(1.0, num2);
				angle = d3;
				return true;
			}
			finally
			{
				regionUnion?.Dispose();
			}
		}
		catch
		{
			return false;
		}
	}

	private static double ResolvePreviewDisplayAngle(Result result)
	{
		if (result == null || result.ModelType != eDeepLearningModelType.旋转框检测)
		{
			return 0.0;
		}
		return (double.IsNaN(result.Angle) || double.IsInfinity(result.Angle)) ? 0.0 : ((double)(0f - result.Angle));
	}

	private static Result MatchPreviewGeometryResult(Result displayResult, IReadOnlyList<Result> candidates)
	{
		if (displayResult == null || candidates == null || candidates.Count == 0)
		{
			return displayResult;
		}
		Result result = FindBestIoUMatch(displayResult, candidates, requireSameClass: true);
		if (result != null)
		{
			return result;
		}
		Result result2 = FindBestIoUMatch(displayResult, candidates, requireSameClass: false);
		if (result2 != null)
		{
			return result2;
		}
		Result result3 = FindBestDistanceMatch(displayResult, candidates, requireSameClass: true);
		if (result3 != null)
		{
			return result3;
		}
		Result result4 = FindBestDistanceMatch(displayResult, candidates, requireSameClass: false);
		return result4 ?? displayResult;
	}

	private static Result FindBestIoUMatch(Result displayResult, IReadOnlyList<Result> candidates, bool requireSameClass)
	{
		Result result = null;
		double num = double.MinValue;
		foreach (Result candidate in candidates)
		{
			if ((!requireSameClass || IsSamePreviewClass(displayResult, candidate)) && TryGetSegmentationIoU(displayResult, candidate, out var iou) && iou > num)
			{
				num = iou;
				result = candidate;
			}
		}
		return (result != null && num > 0.0) ? result : null;
	}

	private static Result FindBestDistanceMatch(Result displayResult, IReadOnlyList<Result> candidates, bool requireSameClass)
	{
		Result result = null;
		double num = double.MaxValue;
		foreach (Result candidate in candidates)
		{
			if (!requireSameClass || IsSamePreviewClass(displayResult, candidate))
			{
				double num2 = Math.Abs(candidate.Cx - displayResult.Cx) + Math.Abs(candidate.Cy - displayResult.Cy) + Math.Abs(candidate.Width - displayResult.Width) + Math.Abs(candidate.Height - displayResult.Height);
				if (num2 < num)
				{
					num = num2;
					result = candidate;
				}
			}
		}
		return result;
	}

	private static bool IsSamePreviewClass(Result left, Result right)
	{
		if (left == null || right == null)
		{
			return false;
		}
		if (left.ClassId == right.ClassId)
		{
			return true;
		}
		return !string.IsNullOrWhiteSpace(left.ClassName) && !string.IsNullOrWhiteSpace(right.ClassName) && string.Equals(left.ClassName, right.ClassName, StringComparison.OrdinalIgnoreCase);
	}

	private static bool TryGetSegmentationIoU(Result left, Result right, out double iou)
	{
		iou = 0.0;
		if (!HasInitializedSeg(left) || !HasInitializedSeg(right))
		{
			return false;
		}
		HObject regionUnion = null;
		HObject regionUnion2 = null;
		HObject regionIntersection = null;
		HObject objectsConcat = null;
		HObject regionUnion3 = null;
		try
		{
			HOperatorSet.Union1(left.Seg, out regionUnion);
			HOperatorSet.Union1(right.Seg, out regionUnion2);
			HOperatorSet.Intersection(regionUnion, regionUnion2, out regionIntersection);
			HOperatorSet.ConcatObj(regionUnion, regionUnion2, out objectsConcat);
			HOperatorSet.Union1(objectsConcat, out regionUnion3);
			double regionArea = GetRegionArea(regionIntersection);
			double regionArea2 = GetRegionArea(regionUnion3);
			if (regionArea2 <= 0.0)
			{
				return false;
			}
			iou = regionArea / regionArea2;
			return !double.IsNaN(iou) && !double.IsInfinity(iou);
		}
		catch
		{
			return false;
		}
		finally
		{
			regionUnion3?.Dispose();
			objectsConcat?.Dispose();
			regionIntersection?.Dispose();
			regionUnion2?.Dispose();
			regionUnion?.Dispose();
		}
	}

	private static double GetRegionArea(HObject region)
	{
		if (region == null)
		{
			return 0.0;
		}
		try
		{
			if (!region.IsInitialized())
			{
				return 0.0;
			}
			HOperatorSet.AreaCenter(region, out var area, out var _, out var _);
			return area.TupleSum().D;
		}
		catch
		{
			return 0.0;
		}
	}

	private static bool HasInitializedSeg(Result result)
	{
		try
		{
			return result?.Seg != null && result.Seg.IsInitialized();
		}
		catch
		{
			return false;
		}
	}

    }
}
