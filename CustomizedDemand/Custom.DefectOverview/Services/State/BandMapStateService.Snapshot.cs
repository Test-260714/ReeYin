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
	private BandMapStateSnapshot BuildSnapshotLocked()
	{
		double safeFrameSpan = Math.Max(1.0, _frameSpanMillimeters);
		double safeWindow = Math.Max(1.0, _windowMeters);
		double num = (double)_frameSequence * safeFrameSpan / 1000.0;
		double num2 = Math.Max(0.0, num - safeWindow);
		if (_isViewportPinnedToLatest)
		{
			_viewportStartMeters = num2;
		}
		else
		{
			_viewportStartMeters = Math.Clamp(_viewportStartMeters, 0.0, num2);
		}
		double minMeters = _viewportStartMeters;
		double maxMeters = minMeters + safeWindow;
		IReadOnlyList<HistoryItem> history = _history;
		List<HistoryItem> wallList = GetWallHistoryItems(history);
		BandMapSlittingSettings slittingSettings = BuildSlittingSettings(_history, _isSlittingEnabled, _knifeSpacingMillimeters, _firstCutOffsetMillimeters, _stripWidthMillimeters, _slitCount, _path1LaneWidthMillimeters, _path2LaneWidthMillimeters);
		int num3 = Math.Max(1, (int)Math.Ceiling((double)wallList.Count / WallPageSize));
		int num4 = (_isWallPinnedToLatestPage ? num3 : Math.Clamp(_wallCurrentPage, 1, num3));
		if (history.Count == 0)
		{
			_selectedDefectKey = null;
		}
		int selectedWallIndex = wallList.FindIndex((HistoryItem item) => item.DefectKey == _selectedDefectKey);
		if (selectedWallIndex >= 0)
		{
			num4 = Math.Clamp(selectedWallIndex / WallPageSize + 1, 1, num3);
		}
		_wallCurrentPage = num4;
		HistoryItem historyItem = FindHistoryItemByDefectKey(history, _selectedDefectKey);
		RenderMetrics renderMetrics = BuildRenderMetricsLocked();
		List<BandMapGuideLineItem> guideLines = BuildGuideLines(renderMetrics, slittingSettings);
		List<BandMapPointItem> list2 = BuildVisibleMapPoints(history, minMeters, maxMeters, safeFrameSpan, safeWindow, renderMetrics, slittingSettings);
		List<BandMapRecentDefectItem> recentDefects = BuildRecentDefects(history, safeFrameSpan, slittingSettings);
		List<BandMapWallItem> list3 = wallList.Skip((num4 - 1) * WallPageSize).Take(WallPageSize).Select(delegate(HistoryItem item)
		{
			double meter = (double)item.FrameSequence * safeFrameSpan / 1000.0;
			return CreateWallItem(item, meter, item.DefectKey == _selectedDefectKey, slittingSettings);
		})
			.ToList();
		List<BandMapAxisTickItem> xAxisTicks = BuildXAxisTicks(renderMetrics);
		List<BandMapAxisTickItem> yAxisTicks = BuildYAxisTicks(renderMetrics, minMeters, maxMeters);
		string batchNumberText = BuildBatchNumberText(_batchStartedLocalTime, _batchNumber);
		BandMapWallItem selectedWallItem = list3.FirstOrDefault((BandMapWallItem item) => item.DefectKey == _selectedDefectKey) ?? list3.FirstOrDefault() ?? ((historyItem == null) ? null : CreateWallItem(historyItem, (double)historyItem.FrameSequence * safeFrameSpan / 1000.0, isSelected: true, slittingSettings));
		int recentNgFrameCount = _recentFrameResults.Count((bool item) => item);
		string text = ResolveCurrentStatusCode(_lastFrameUtc, _path1Result, _path2Result);
		string currentStatusText = ResolveCurrentStatusText(text);
		return new BandMapStateSnapshot
		{
			CanvasWidth = renderMetrics.CanvasWidth,
			CanvasHeight = renderMetrics.CanvasHeight,
			PlotLeft = renderMetrics.PlotLeft,
			PlotTop = renderMetrics.PlotTop,
			PlotWidth = renderMetrics.PlotWidth,
			PlotHeight = renderMetrics.PlotHeight,
			CumulativeMeters = num,
			FrameSpanMillimeters = safeFrameSpan,
			WindowMeters = safeWindow,
			ViewportStartMeters = minMeters,
			ViewportMaxStartMeters = num2,
			BatchNumber = _batchNumber,
			BatchNumberText = batchNumberText,
			BatchStartedLocalTime = _batchStartedLocalTime,
			TotalFrames = _totalFrames,
			OkFrames = _okFrames,
			NgFrames = _ngFrames,
			RecentNgFrameCount = recentNgFrameCount,
			RecentNgWindowSize = 20,
			CurrentSpeedMetersPerMinute = _currentSpeedMetersPerMinute,
			CurrentStatusCode = text,
			CurrentStatusText = currentStatusText,
			ReportSuggestedFileName = $"DefectOverview_{batchNumberText}_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
			LastFrameUtc = _lastFrameUtc,
			LastFrameIdText = _lastFrameIdText,
			RangeSummary = $"当前累计 {num:F2} m | 显示窗口 {safeWindow:F1} m | 可视点位 {list2.Count}/{history.Count} | {slittingSettings.SummaryText}",
			IsSlittingEnabled = slittingSettings.IsEnabled,
			KnifeSpacingMillimeters = slittingSettings.KnifeSpacingMillimeters,
			FirstCutOffsetMillimeters = slittingSettings.FirstCutOffsetMillimeters,
			StripWidthMillimeters = slittingSettings.StripWidthMillimeters,
			SlitCount = slittingSettings.SlitCount,
			SlittingSummaryText = slittingSettings.SummaryText,
			ShowPathStatusBadges = _showPathStatusBadges,
			Path1Header = _path1Header,
			Path2Header = _path2Header,
			Path1Result = _path1Result,
			Path2Result = _path2Result,
			Path1DefectCount = _path1DefectCount,
			Path2DefectCount = _path2DefectCount,
			TotalDefectCount = history.Count,
			SelectedDefectKey = _selectedDefectKey,
			SelectionVersion = _selectionVersion,
			GuideLines = guideLines,
			DefectPoints = list2,
			XAxisTicks = xAxisTicks,
			YAxisTicks = yAxisTicks,
			RecentDefects = recentDefects,
			WallItems = list3,
			ReportRows = Array.Empty<BandMapReportRow>(),
			CameraSnapshots = _cameraSnapshots,
			SelectedWallItem = selectedWallItem,
			WallSummaryText = $"最近看板 {wallList.Count} / 记录 {history.Count} 条",
			WallCurrentPage = num4,
			WallTotalPages = num3,
			IsWallPinnedToLatestPage = _isWallPinnedToLatestPage
		};
	}

	private static HistoryItem FindHistoryItemByDefectKey(IReadOnlyList<HistoryItem> history, string defectKey)
	{
		if (history == null || string.IsNullOrWhiteSpace(defectKey))
		{
			return null;
		}

		for (int i = history.Count - 1; i >= 0; i--)
		{
			HistoryItem item = history[i];
			if (string.Equals(item?.DefectKey, defectKey, StringComparison.Ordinal))
			{
				return item;
			}
		}

		return null;
	}

	private List<BandMapPointItem> BuildVisibleMapPoints(
		IReadOnlyList<HistoryItem> history,
		double minMeters,
		double maxMeters,
		double safeFrameSpan,
		double safeWindow,
		RenderMetrics renderMetrics,
		BandMapSlittingSettings slittingSettings)
	{
		var points = new List<BandMapPointItem>(Math.Min(MaxVisibleMapPoints, Math.Max(0, history?.Count ?? 0)));
		if (history == null || history.Count == 0)
		{
			return points;
		}

		for (int i = history.Count - 1; i >= 0 && points.Count < MaxVisibleMapPoints; i--)
		{
			HistoryItem item = history[i];
			if (item == null)
			{
				continue;
			}

			double meter = (double)item.FrameSequence * safeFrameSpan / 1000.0;
			if (meter > maxMeters)
			{
				continue;
			}

			if (meter < minMeters)
			{
				break;
			}

			double yRatio = safeWindow <= 0.0 ? 0.0 : (meter - minMeters) / safeWindow;
			double x = renderMetrics.PlotLeft + item.XRatio * renderMetrics.PlotWidth;
			double y = renderMetrics.PlotTop + (1.0 - yRatio) * renderMetrics.PlotHeight;
			LegendStyleAssignment legendStyleAssignment = ResolveLegendStyleLocked(item);
			SlitEvaluation slitEvaluation = EvaluateSlit(item, slittingSettings);
			bool isSelected = item.DefectKey == _selectedDefectKey;
			double size = isSelected ? 28 : 20;
			points.Add(new BandMapPointItem
			{
				DefectKey = item.DefectKey,
				LegendKey = item.LegendKey,
				ClassName = item.ClassName,
				MeterText = $"{meter:F2} m",
				PositionText = slitEvaluation.CombinedPositionText,
				PercentText = slitEvaluation.PercentText,
				PhysicalPositionText = slitEvaluation.PhysicalPositionText,
				SlitText = slitEvaluation.SlitText,
				PathText = item.PathName,
				FrameIdText = item.FrameIdText,
				ResultIndex = item.ResultIndex,
				XPercent = Math.Clamp(item.XRatio, 0.0, 1.0) * 100.0,
				MeterValue = meter,
				X = SnapVisualCoordinate(x - size / 2.0),
				Y = SnapVisualCoordinate(y - size / 2.0),
				Size = size,
				MarkerKind = legendStyleAssignment.MarkerKind,
				Fill = legendStyleAssignment.Fill,
				Stroke = legendStyleAssignment.Stroke,
				ToolTip = BuildPointToolTip(item, meter, slitEvaluation),
				IsSelected = isSelected
			});
		}

		return points;
	}

	private List<BandMapRecentDefectItem> BuildRecentDefects(
		IReadOnlyList<HistoryItem> history,
		double safeFrameSpan,
		BandMapSlittingSettings slittingSettings)
	{
		var recentDefects = new List<BandMapRecentDefectItem>(MaxRecentItems);
		if (history == null || history.Count == 0)
		{
			return recentDefects;
		}

		for (int i = history.Count - 1; i >= 0 && recentDefects.Count < MaxRecentItems; i--)
		{
			HistoryItem item = history[i];
			if (item == null)
			{
				continue;
			}

			double recentMeter = (double)item.FrameSequence * safeFrameSpan / 1000.0;
			LegendStyleAssignment legendStyleAssignment = ResolveLegendStyleLocked(item);
			SlitEvaluation slitEvaluation = EvaluateSlit(item, slittingSettings);
			recentDefects.Add(new BandMapRecentDefectItem
			{
				DefectKey = item.DefectKey,
				LegendKey = item.LegendKey,
				AccentBrush = legendStyleAssignment.Fill,
				FrameIdText = item.FrameIdText,
				MeterText = $"{recentMeter:F2} m",
				PositionText = slitEvaluation.CombinedPositionText,
				PercentText = slitEvaluation.PercentText,
				PhysicalPositionText = slitEvaluation.PhysicalPositionText,
				SlitText = slitEvaluation.SlitText,
				PathText = item.PathName,
				ClassName = item.ClassName,
				ConfidenceText = $"{item.Confidence:P1}",
				IsCrossSlit = slitEvaluation.IsCrossSlit,
				IsSelected = item.DefectKey == _selectedDefectKey
			});
		}

		return recentDefects;
	}

    }
}
