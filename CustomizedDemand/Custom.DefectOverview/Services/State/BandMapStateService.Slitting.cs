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
	private static double? ResolveLaneWidthMillimeters(BandMapPathInput path)
	{
		if (path != null && path.PixelEquivalentX > 0.0 && path.LaneWidth > 0.0)
		{
			return path.LaneWidth / path.PixelEquivalentX;
		}
		return null;
	}

	private static BandMapSlittingSettings BuildSlittingSettings(IReadOnlyCollection<HistoryItem> history, bool isEnabled, double knifeSpacingMillimeters, double firstCutOffsetMillimeters, double configuredStripWidthMillimeters, int configuredSlitCount, double? path1LaneWidthMillimeters = null, double? path2LaneWidthMillimeters = null)
	{
		double num = ((double.IsFinite(knifeSpacingMillimeters) && knifeSpacingMillimeters > 0.0) ? knifeSpacingMillimeters : 200.0);
		double num2 = ((double.IsFinite(firstCutOffsetMillimeters) && firstCutOffsetMillimeters >= 0.0) ? firstCutOffsetMillimeters : 0.0);
		double? num3 = ResolveStripWidthMillimeters(history, path1LaneWidthMillimeters, path2LaneWidthMillimeters);
		double? stripWidthMillimeters = ((double.IsFinite(configuredStripWidthMillimeters) && configuredStripWidthMillimeters > 1.0) ? new double?(configuredStripWidthMillimeters) : num3);
		int num4 = Math.Clamp(configuredSlitCount, 1, 200);
		string summaryText = ((!isEnabled) ? (stripWidthMillimeters.HasValue ? $"切刀未启用 | 幅宽 {stripWidthMillimeters.Value:F1} mm | 切数 {num4}" : "切刀分切未启用") : (stripWidthMillimeters.HasValue ? $"切刀启用 | 幅宽 {stripWidthMillimeters.Value:F1} mm | 切数 {num4} | 刀距 {num:F1} mm | 起切 {num2:F1} mm" : $"切刀启用 | 刀距 {num:F1} mm | 起切 {num2:F1} mm | 幅宽待定 | 切数 {num4}"));
		return new BandMapSlittingSettings
		{
			IsEnabled = isEnabled,
			KnifeSpacingMillimeters = num,
			FirstCutOffsetMillimeters = num2,
			StripWidthMillimeters = stripWidthMillimeters,
			SlitCount = num4,
			SummaryText = summaryText
		};
	}

	private static double? ResolveStripWidthMillimeters(IReadOnlyCollection<HistoryItem> history, double? path1LaneWidthMillimeters, double? path2LaneWidthMillimeters)
	{
		List<HistoryItem> source = history?.Where((HistoryItem item) => item != null).ToList() ?? new List<HistoryItem>();
		double? maxFiniteOrNull = GetMaxFiniteOrNull(from item in source
			where item.OccupiesFullWidth
			select item.LaneWidthMillimeters);
		if (maxFiniteOrNull.HasValue)
		{
			return maxFiniteOrNull;
		}
		double? num = GetMaxFiniteOrNull(from item in source
			where IsLeftPathName(item.PathName)
			select item.LaneWidthMillimeters);
		double? num2 = GetMaxFiniteOrNull(from item in source
			where IsRightPathName(item.PathName)
			select item.LaneWidthMillimeters);
		double? num3 = num;
		if (!num3.HasValue)
		{
			num = (IsFiniteNullable(path1LaneWidthMillimeters) ? path1LaneWidthMillimeters : ((double?)null));
		}
		num3 = num2;
		if (!num3.HasValue)
		{
			num2 = (IsFiniteNullable(path2LaneWidthMillimeters) ? path2LaneWidthMillimeters : ((double?)null));
		}
		if (num.HasValue && num2.HasValue)
		{
			return num.Value + num2.Value;
		}
		double? maxFiniteOrNull2 = GetMaxFiniteOrNull(source.Select((HistoryItem item) => item.LaneWidthMillimeters));
		if (num.HasValue)
		{
			return num.Value;
		}
		if (num2.HasValue)
		{
			return num2.Value;
		}
		if (maxFiniteOrNull2.HasValue)
		{
			return maxFiniteOrNull2.Value;
		}
		IEnumerable<double?> values = source.Select(delegate(HistoryItem item)
		{
			if (!IsFiniteNullable(item.PhysicalXMillimeters))
			{
				return (double?)null;
			}
			double num4 = ((IsFiniteNullable(item.PhysicalWidthMillimeters) && item.PhysicalWidthMillimeters.Value > 0.0) ? (item.PhysicalWidthMillimeters.Value / 2.0) : 0.0);
			return item.PhysicalXMillimeters.Value + num4;
		});
		return GetMaxFiniteOrNull(values);
	}

	private static List<BandMapGuideLineItem> BuildGuideLines(RenderMetrics renderMetrics, BandMapSlittingSettings slittingSettings)
	{
		if (renderMetrics == null || slittingSettings == null || !slittingSettings.IsEnabled || !slittingSettings.StripWidthMillimeters.HasValue || slittingSettings.StripWidthMillimeters.Value <= 1.0)
		{
			return new List<BandMapGuideLineItem>();
		}
		double value = slittingSettings.StripWidthMillimeters.Value;
		double num = Math.Max(1.0, slittingSettings.KnifeSpacingMillimeters);
		double num2 = Math.Max(0.0, slittingSettings.FirstCutOffsetMillimeters);
		int num3 = Math.Max(1, slittingSettings.SlitCount);
		double y = SnapLineCoordinate(renderMetrics.PlotTop);
		double y2 = SnapLineCoordinate(renderMetrics.PlotTop + renderMetrics.PlotHeight);
		Brush brush = CreateFrozenBrush("#6B7280");
		Brush brush2 = CreateFrozenBrush("#00C2FF");
		List<BandMapGuideLineItem> list = new List<BandMapGuideLineItem>();
		List<double> list2 = new List<double>();
		AddGuideLinePosition(list2, 0.0, value);
		for (int i = 0; i <= num3; i++)
		{
			double num4 = num2 + (double)i * num;
			if (!double.IsFinite(num4) || num4 > value + 0.0001)
			{
				break;
			}
			AddGuideLinePosition(list2, num4, value);
		}
		AddGuideLinePosition(list2, value, value);
		for (int j = 0; j < list2.Count; j++)
		{
			double num5 = list2[j];
			double num6 = ((value <= 0.0) ? 0.0 : Math.Clamp(num5 / value, 0.0, 1.0));
			double num7 = SnapLineCoordinate(renderMetrics.PlotLeft + num6 * renderMetrics.PlotWidth);
			bool flag = j == 0 || j == list2.Count - 1;
			list.Add(new BandMapGuideLineItem
			{
				X1 = num7,
				Y1 = y,
				X2 = num7,
				Y2 = y2,
				Stroke = (flag ? brush : brush2),
				StrokeThickness = (flag ? 1.5 : 2.2)
			});
		}
		return list;
	}

	private static void AddGuideLinePosition(ICollection<double> positions, double positionMillimeters, double stripWidthMillimeters)
	{
		if (double.IsFinite(positionMillimeters) && !(positionMillimeters < -0.0001) && !(positionMillimeters > stripWidthMillimeters + 0.0001))
		{
			double clampedPosition = Math.Clamp(positionMillimeters, 0.0, stripWidthMillimeters);
			if (!positions.Any((double existing) => Math.Abs(existing - clampedPosition) < 0.5))
			{
				positions.Add(clampedPosition);
			}
		}
	}

	private static SlitEvaluation EvaluateSlit(HistoryItem item, BandMapSlittingSettings slittingSettings)
	{
		if (item == null)
		{
			return new SlitEvaluation
			{
				PercentText = string.Empty,
				PhysicalPositionText = string.Empty,
				SlitText = string.Empty,
				CombinedPositionText = "-"
			};
		}
		double? absoluteCenterMillimeters = ResolveAbsoluteCenterMillimeters(item, slittingSettings);
		double? absoluteWidthMillimeters = ResolveAbsoluteWidthMillimeters(item);
		double value = Math.Clamp(item.XRatio, 0.0, 1.0) * 100.0;
		if (absoluteCenterMillimeters.HasValue && slittingSettings != null && slittingSettings.StripWidthMillimeters.HasValue && slittingSettings.StripWidthMillimeters.Value > 1.0)
		{
			value = Math.Clamp(absoluteCenterMillimeters.Value / slittingSettings.StripWidthMillimeters.Value * 100.0, 0.0, 100.0);
		}
		string percentText = $"{value:F1}%";
		string physicalPositionText = (absoluteCenterMillimeters.HasValue ? $"{absoluteCenterMillimeters.Value:F1} mm" : string.Empty);
		bool isCrossSlit = false;
		string slitText = string.Empty;
		if (slittingSettings != null && slittingSettings.IsEnabled && slittingSettings.StripWidthMillimeters.HasValue && slittingSettings.StripWidthMillimeters.Value > 1.0 && slittingSettings.SlitCount > 0)
		{
			slitText = ResolveSlitText(absoluteCenterMillimeters, absoluteWidthMillimeters, slittingSettings, out isCrossSlit);
		}
		return new SlitEvaluation
		{
			PercentText = percentText,
			PhysicalPositionText = physicalPositionText,
			SlitText = slitText,
			CombinedPositionText = BuildCombinedPositionText(slitText, physicalPositionText, percentText),
			IsCrossSlit = isCrossSlit
		};
	}

	private static string ResolveSlitText(double? absoluteCenterMillimeters, double? absoluteWidthMillimeters, BandMapSlittingSettings slittingSettings, out bool isCrossSlit)
	{
		isCrossSlit = false;
		if (!absoluteCenterMillimeters.HasValue || slittingSettings == null || !slittingSettings.StripWidthMillimeters.HasValue || slittingSettings.StripWidthMillimeters.Value <= 1.0 || slittingSettings.SlitCount <= 0)
		{
			return string.Empty;
		}
		double value = slittingSettings.StripWidthMillimeters.Value;
		double num = Math.Max(0.0, slittingSettings.FirstCutOffsetMillimeters);
		double num2 = Math.Max(1.0, slittingSettings.KnifeSpacingMillimeters);
		double num3 = num + (double)slittingSettings.SlitCount * num2;
		double num4 = Math.Clamp(absoluteCenterMillimeters.Value, 0.0, value);
		double num5 = ((absoluteWidthMillimeters.HasValue && absoluteWidthMillimeters.Value > 0.0) ? (absoluteWidthMillimeters.Value / 2.0) : 0.0);
		double num6 = Math.Clamp(num4 - num5, 0.0, value);
		double num7 = Math.Clamp(num4 + num5, 0.0, value);
		for (int i = 1; i < slittingSettings.SlitCount; i++)
		{
			double num8 = num + (double)i * num2;
			if (num6 < num8 && num7 > num8)
			{
				isCrossSlit = true;
				return $"第{i}/{i + 1}切间";
			}
		}
		if (num4 < num || num4 > num3)
		{
			return "边缘料";
		}
		int value2 = (int)Math.Floor((num4 - num) / num2) + 1;
		value2 = Math.Clamp(value2, 1, slittingSettings.SlitCount);
		return $"第{value2}切";
	}

	private static double? ResolveAbsoluteCenterMillimeters(HistoryItem item, BandMapSlittingSettings slittingSettings)
	{
		if (item == null)
		{
			return null;
		}
		if (slittingSettings != null && slittingSettings.StripWidthMillimeters.HasValue && slittingSettings.StripWidthMillimeters.Value > 1.0)
		{
			return Math.Clamp(item.XRatio, 0.0, 1.0) * slittingSettings.StripWidthMillimeters.Value;
		}
		if (IsFiniteNullable(item.PhysicalXMillimeters))
		{
			double value = item.PhysicalXMillimeters.Value;
			if (item.OccupiesFullWidth || slittingSettings == null || !slittingSettings.StripWidthMillimeters.HasValue || !IsFiniteNullable(item.LaneWidthMillimeters))
			{
				return value;
			}
			double value2 = slittingSettings.StripWidthMillimeters.Value;
			double value3 = item.LaneWidthMillimeters.Value;
			if (value2 <= value3 + 0.001)
			{
				return value;
			}
			if (IsRightPathName(item.PathName))
			{
				return value + Math.Max(0.0, value2 - value3);
			}
			return value;
		}
		return null;
	}

	private static double? ResolveAbsoluteWidthMillimeters(HistoryItem item)
	{
		if (item == null || !IsFiniteNullable(item.PhysicalWidthMillimeters) || item.PhysicalWidthMillimeters.Value <= 0.0)
		{
			return null;
		}
		return item.PhysicalWidthMillimeters.Value;
	}

	private static string BuildCombinedPositionText(string slitText, string physicalPositionText, string percentText)
	{
		List<string> list = new List<string>();
		if (!string.IsNullOrWhiteSpace(slitText))
		{
			list.Add(slitText);
		}
		if (!string.IsNullOrWhiteSpace(physicalPositionText))
		{
			list.Add(physicalPositionText);
		}
		if (!string.IsNullOrWhiteSpace(percentText))
		{
			list.Add(percentText);
		}
		return (list.Count > 0) ? string.Join(" | ", list) : "-";
	}

	private static string BuildPointToolTip(HistoryItem item, double meter, SlitEvaluation slitEvaluation)
	{
		if (item == null)
		{
			return string.Empty;
		}
		string pathName = item.PathName;
		bool occupiesFullWidth = item.OccupiesFullWidth;
		string text = ResolvePathDisplayName(pathName, null, allowSideFallback: false, occupiesFullWidth);
		List<string> list = new List<string>
		{
			string.IsNullOrWhiteSpace(item.ClassName) ? "缺陷" : item.ClassName,
			$"累计 {meter:F2} m",
			"位置 " + (slitEvaluation?.CombinedPositionText ?? "-")
		};
		if (!string.IsNullOrWhiteSpace(item.FrameIdText))
		{
			list.Add("帧号 " + item.FrameIdText);
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add("通道 " + text);
		}
		if (!string.IsNullOrWhiteSpace(item.CoordinateSource))
		{
			list.Add("坐标来源 " + item.CoordinateSource);
		}
		list.Add($"中心 ({item.CenterX:F1}, {item.CenterY:F1})");
		list.Add($"尺寸 {item.Width:F1} x {item.Height:F1}");
		if (item.SourceWidth > 0 && item.SourceHeight > 0)
		{
			list.Add($"源图 {item.SourceWidth} x {item.SourceHeight}");
		}
		if (!string.IsNullOrWhiteSpace(item.ModelTypeText))
		{
			list.Add("模型 " + item.ModelTypeText);
		}
		return string.Join(Environment.NewLine, list);
	}

    }
}
