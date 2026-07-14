using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Custom.DefectOverview.Models;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;

namespace Custom.DefectOverview.Services
{
    public sealed partial class BandMapStateService : IBandMapStateService
    {
	private BandMapWallItem CreateWallItem(HistoryItem item, double meter, bool isSelected, BandMapSlittingSettings slittingSettings)
	{
		if (item == null)
		{
			return null;
		}
		LegendStyleAssignment legendStyleAssignment = ResolveLegendStyleLocked(item);
		SlitEvaluation slitEvaluation = EvaluateSlit(item, slittingSettings);
		string pathName = item.PathName;
		bool occupiesFullWidth = item.OccupiesFullWidth;
		string text = ResolvePathDisplayName(pathName, null, allowSideFallback: false, occupiesFullWidth);
		string text2 = ResolvePathShortText(item.PathName, item.OccupiesFullWidth);
		string text3 = $"{meter:F2} m";
		string summaryText = JoinNonEmpty(" | ", item.ClassName, text2, text3);
		string positionSummaryText = JoinNonEmpty(" | ", slitEvaluation.CombinedPositionText, text);
		string geometryText = BuildGeometryText(item);
		string sourceImageText = BuildSourceImageText(item);
		string coordinateSourceText = string.IsNullOrWhiteSpace(item.CoordinateSource) ? "-" : item.CoordinateSource;
		return new BandMapWallItem
		{
			DefectKey = item.DefectKey,
			LegendKey = item.LegendKey,
			WallImage = item.PreviewImage ?? item.ThumbnailImage,
			ThumbnailImage = item.ThumbnailImage,
			PreviewImage = (item.PreviewImage ?? item.ThumbnailImage),
			AccentBrush = legendStyleAssignment.Fill,
			PathBrush = ResolvePathBrush(item.PathName),
			FrameIdText = item.FrameIdText,
			MeterText = text3,
			PositionText = slitEvaluation.CombinedPositionText,
			PercentText = slitEvaluation.PercentText,
			PhysicalPositionText = slitEvaluation.PhysicalPositionText,
			SlitText = slitEvaluation.SlitText,
			PathText = text,
			PositionSummaryText = positionSummaryText,
			PathShortText = text2,
			ClassName = item.ClassName,
			ConfidenceText = $"{item.Confidence:P1}",
			SizeText = $"{item.Width:F1} x {item.Height:F1}",
			GeometryText = geometryText,
			SourceImageText = sourceImageText,
			ModelTypeText = string.IsNullOrWhiteSpace(item.ModelTypeText) ? "-" : item.ModelTypeText,
			CoordinateSourceText = coordinateSourceText,
			SummaryText = summaryText,
			DetailText = BuildWallDetailText(item, slitEvaluation, geometryText, sourceImageText, coordinateSourceText),
			IsCrossSlit = slitEvaluation.IsCrossSlit,
			IsSelected = isSelected
		};
	}

	private static List<HistoryItem> GetWallHistoryItems(IReadOnlyList<HistoryItem> history)
	{
		if (history == null || history.Count == 0)
		{
			return new List<HistoryItem>();
		}

		int skip = Math.Max(0, history.Count - MaxWallItems);
		return history.Skip(skip).ToList();
	}

	private static string BuildWallDetailText(HistoryItem item, SlitEvaluation slitEvaluation, string geometryText, string sourceImageText, string coordinateSourceText)
	{
		if (item == null)
		{
			return "-";
		}

		return JoinNonEmpty(
			Environment.NewLine,
			$"位置 {slitEvaluation?.CombinedPositionText ?? "-"}",
			$"尺寸 {item.Width:F1} x {item.Height:F1}",
			$"置信度 {item.Confidence:P1}",
			string.IsNullOrWhiteSpace(geometryText) || geometryText == "-" ? null : "几何 " + geometryText,
			string.IsNullOrWhiteSpace(sourceImageText) || sourceImageText == "-" ? null : "源图 " + sourceImageText,
			string.IsNullOrWhiteSpace(item.ModelTypeText) ? null : "模型 " + item.ModelTypeText,
			string.IsNullOrWhiteSpace(coordinateSourceText) || coordinateSourceText == "-" ? null : "坐标来源 " + coordinateSourceText,
			item.HasSegmentation ? "分割区域 已记录" : null,
			$"结果索引 {item.ResultIndex}");
	}

	private static string BuildGeometryText(HistoryItem item)
	{
		if (item == null)
		{
			return "-";
		}

		return $"中心 ({item.CenterX:F1}, {item.CenterY:F1}) | 角度 {item.Angle:F2}";
	}

	private static string BuildSourceImageText(HistoryItem item)
	{
		if (item == null || item.SourceWidth <= 0 || item.SourceHeight <= 0)
		{
			return "-";
		}

		return $"{item.SourceWidth} x {item.SourceHeight}";
	}

	private static string BuildDefectKey(string frameIdText, string pathName, Result result, int index)
	{
		if (result != null)
		{
			return $"{frameIdText}|{pathName}|{index}|{result.ClassId}|{result.Cx:F1}|{result.Cy:F1}|{result.Width:F1}|{result.Height:F1}";
		}
		return $"{frameIdText}|{pathName}|{index}";
	}

	private static string ResolvePathShortText(string pathName, bool occupiesFullWidth = false)
	{
		bool occupiesFullWidth2 = occupiesFullWidth;
		return ResolvePathDisplayName(pathName, null, allowSideFallback: false, occupiesFullWidth2);
	}

	private static string ResolvePathDisplayName(string pathName, bool? isLeft = null, bool allowSideFallback = false, bool occupiesFullWidth = false)
	{
		if (occupiesFullWidth && (IsLeftPathName(pathName) || IsRightPathName(pathName)))
		{
			return string.Empty;
		}
		if (TryResolveGroupedDualCameraGroupKey(pathName, out string groupKey))
		{
			return groupKey;
		}
		if (IsSchemeCLeftPathName(pathName) || IsSchemeCRightPathName(pathName))
		{
			return string.Empty;
		}
		if (IsLeftPathName(pathName))
		{
			return "左路";
		}
		if (IsRightPathName(pathName))
		{
			return "右路";
		}
		if (!string.IsNullOrWhiteSpace(pathName))
		{
			return pathName;
		}
		if (!allowSideFallback)
		{
			return string.Empty;
		}
		if (1 == 0)
		{
		}
		string result = ((!isLeft.HasValue) ? string.Empty : ((isLeft != true) ? "右路" : "左路"));
		if (1 == 0)
		{
		}
		return result;
	}

	private static string JoinNonEmpty(string separator, params string[] values)
	{
		return string.Join(separator, values.Where((string value) => !string.IsNullOrWhiteSpace(value)));
	}

	private RenderMetrics BuildRenderMetricsLocked()
	{
		double num = ((double.IsFinite(_viewportCanvasWidth) && _viewportCanvasWidth > 1.0) ? _viewportCanvasWidth : 1440.0);
		double num2 = ((double.IsFinite(_viewportCanvasHeight) && _viewportCanvasHeight > 1.0) ? _viewportCanvasHeight : 1120.0);
		double num3 = 44.0;
		double num4 = 8.0;
		double plotWidth = Math.Max(1.0, num - num3 - 30.0);
		double plotHeight = Math.Max(1.0, num2 - num4 - 56.0);
		return new RenderMetrics
		{
			CanvasWidth = num,
			CanvasHeight = num2,
			PlotLeft = num3,
			PlotTop = num4,
			PlotWidth = plotWidth,
			PlotHeight = plotHeight
		};
	}

	private static double SnapVisualCoordinate(double value)
	{
		return Math.Round(value);
	}

	private static double SnapLineCoordinate(double value)
	{
		return Math.Round(value) + 0.5;
	}

	private static List<BandMapAxisTickItem> BuildXAxisTicks(RenderMetrics renderMetrics)
	{
		Brush brush = CreateFrozenBrush("#5F6670");
		Brush brush2 = CreateFrozenBrush("#777E88");
		List<BandMapAxisTickItem> list = new List<BandMapAxisTickItem>();
		for (int i = 0; i <= 20; i++)
		{
			double num = (double)i / 20.0;
			double num2 = SnapLineCoordinate(renderMetrics.PlotLeft + num * renderMetrics.PlotWidth);
			bool flag = i % 2 == 0;
			list.Add(new BandMapAxisTickItem
			{
				X1 = num2,
				Y1 = SnapLineCoordinate(renderMetrics.PlotTop),
				X2 = num2,
				Y2 = SnapLineCoordinate(renderMetrics.PlotTop + renderMetrics.PlotHeight + 8.0),
				LabelX = SnapVisualCoordinate(num2 - 12.0),
				LabelY = SnapVisualCoordinate(renderMetrics.PlotTop + renderMetrics.PlotHeight + 12.0),
				Label = $"{num * 100.0:F0}",
				Stroke = (flag ? brush : brush2),
				StrokeThickness = (flag ? 1.95 : 1.45)
			});
		}
		return list;
	}

	private static List<BandMapAxisTickItem> BuildYAxisTicks(RenderMetrics renderMetrics, double minMeters, double maxMeters)
	{
		Brush stroke = CreateFrozenBrush("#5F6670");
		double num = Math.Max(0.2, maxMeters - minMeters);
		double num2 = ResolveNiceStep(num, 8);
		double num3 = Math.Floor(minMeters / num2) * num2;
		double num4 = Math.Ceiling(maxMeters / num2) * num2;
		List<BandMapAxisTickItem> list = new List<BandMapAxisTickItem>();
		for (double num5 = num3; num5 <= num4 + 0.0001; num5 += num2)
		{
			if (!(num5 < minMeters - 0.0001) && !(num5 > maxMeters + 0.0001))
			{
				double num6 = ((num <= 0.0) ? 0.0 : ((num5 - minMeters) / num));
				double num7 = SnapLineCoordinate(renderMetrics.PlotTop + renderMetrics.PlotHeight - num6 * renderMetrics.PlotHeight);
				list.Add(new BandMapAxisTickItem
				{
					X1 = SnapVisualCoordinate(renderMetrics.PlotLeft - 6.0),
					Y1 = num7,
					X2 = SnapVisualCoordinate(renderMetrics.PlotLeft + renderMetrics.PlotWidth),
					Y2 = num7,
					LabelX = 8.0,
					LabelY = SnapVisualCoordinate(num7 - 9.0),
					Label = $"{num5:F1}",
					Stroke = stroke,
					StrokeThickness = 1.65
				});
			}
		}
		return list;
	}

	private static double ResolveNiceStep(double range, int targetTickCount)
	{
		double num = Math.Max(0.1, range / (double)Math.Max(2, targetTickCount));
		double num2 = Math.Pow(10.0, Math.Floor(Math.Log10(num)));
		double num3 = num / num2;
		if (1 == 0)
		{
		}
		int num4 = ((num3 <= 1.0) ? 1 : ((num3 <= 2.0) ? 2 : ((!(num3 <= 5.0)) ? 10 : 5)));
		if (1 == 0)
		{
		}
		double num5 = num4;
		return num5 * num2;
	}

	private static double ResolveXRatio(Result result, bool isLeft, double laneWidth, bool occupiesFullWidth)
	{
		ResultMapGeometry resultMapGeometry = ResolveResultMapGeometry(result, laneWidth);
		double num = ((laneWidth > 1.0) ? laneWidth : Math.Max(1.0, resultMapGeometry.CenterX + Math.Max(1.0, resultMapGeometry.Width / 2.0)));
		double num2 = Math.Clamp(resultMapGeometry.CenterX / num, 0.0, 1.0);
		if (occupiesFullWidth)
		{
			return num2;
		}
		return isLeft ? (num2 * 0.5) : (0.5 + num2 * 0.5);
	}

	private static ResultMapGeometry ResolveResultMapGeometry(Result result, double laneWidth)
	{
		if (result == null)
		{
			return new ResultMapGeometry
			{
				CenterX = 0.0,
				Width = 1.0
			};
		}
		double value;
		bool flag = TryGetResultMetadataDouble(result, "DefectOverview_MapCenterX", out value);
		bool flag2 = flag || TryGetResultMetadataDouble(result, "DefectOverview_DisplayCenterX", out value);
		if (!flag2)
		{
			value = result.Cx;
		}
		if (!TryGetResultMetadataDouble(result, "DefectOverview_MapPixelWidth", out var value2) && !TryGetResultMetadataDouble(result, "DefectOverview_DisplayPixelWidth", out value2) && !TryGetResultMetadataDouble(result, "XYHD_DisplayPixelWidth", out value2))
		{
			value2 = result.Width;
		}
		if (DefectOverviewRuntimeOptions.UseSegmentationGeometry
			&& !flag2
			&& TryResolveGeometryFromSeg(result, out var centerX, out var width))
		{
			value = centerX;
			value2 = width;
		}
		bool value3 = default(bool);
		if (!flag && laneWidth > 1.0 && TryGetResultMetadataBool(result, "DefectOverview_MapXMirror", out value3) && value3)
		{
			value = laneWidth - value;
		}
		return new ResultMapGeometry
		{
			CenterX = (double.IsFinite(value) ? value : 0.0),
			Width = ((double.IsFinite(value2) && value2 > 0.0) ? value2 : 1.0)
		};
	}

	private static bool TryGetResultMetadataDouble(Result result, string key, out double value)
	{
		value = 0.0;
		if (result?.Others == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		object value2;
		return result.Others.TryGetValue(key, out value2) && value2 != null && TryConvertToDouble(value2, out value);
	}

	private static bool TryGetResultMetadataBool(Result result, string key, out bool value)
	{
		value = false;
		if (result?.Others == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}
		if (!result.Others.TryGetValue(key, out var value2) || value2 == null)
		{
			return false;
		}
		if (value2 is bool flag)
		{
			value = flag;
			return true;
		}
		if (value2 is string value3 && bool.TryParse(value3, out var result2))
		{
			value = result2;
			return true;
		}
		try
		{
			value = Convert.ToDouble(value2, CultureInfo.InvariantCulture) != 0.0;
			return true;
		}
		catch
		{
			value = false;
			return false;
		}
	}

	private static bool TryResolveGeometryFromSeg(Result result, out double centerX, out double width)
	{
		centerX = 0.0;
		width = 0.0;
		try
		{
			if (result?.Seg == null || !result.Seg.IsInitialized())
			{
				return false;
			}
			using HObject hObject = UnionSegRegion(result.Seg);
			HObject regions = ((hObject != null && hObject.IsInitialized()) ? hObject : result.Seg);
			HOperatorSet.SmallestRectangle1(regions, out var _, out var column, out var _, out var column2);
			if (column.TupleLength() == 0 || column2.TupleLength() == 0)
			{
				return false;
			}
			double d = column[0].D;
			double d2 = column2[0].D;
			if (!double.IsFinite(d) || !double.IsFinite(d2))
			{
				return false;
			}
			width = Math.Max(1.0, d2 - d + 1.0);
			centerX = d + width / 2.0;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static HObject UnionSegRegion(HObject seg)
	{
		try
		{
			if (seg == null || !seg.IsInitialized())
			{
				return null;
			}
			HOperatorSet.CountObj(seg, out var number);
			if (number.TupleLength() == 0 || number[0].I <= 1)
			{
				return null;
			}
			HOperatorSet.Union1(seg, out var regionUnion);
			return regionUnion;
		}
		catch
		{
			return null;
		}
	}

    }
}
