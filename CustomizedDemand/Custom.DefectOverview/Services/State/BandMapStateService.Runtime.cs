using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Custom.DefectOverview.Models;
using Custom.DefectOverview.Services.GroupedDualCamera;
using HalconDotNet;
using ReeYin_V.Core.DeepLearning;

namespace Custom.DefectOverview.Services
{
    public sealed partial class BandMapStateService : IBandMapStateService
    {
	private void AppendPathHistoryLocked(BandMapPathInput path, bool isLeft, bool allowSideFallback, long frameSequence, string frameIdText)
	{
		if (path?.Results == null)
		{
			return;
		}
		for (int i = 0; i < path.Results.Count; i++)
		{
			Result result = path.Results[i];
			if (result != null)
			{
				BandMapDefectSeed bandMapDefectSeed = ((path.Defects != null && i < path.Defects.Count) ? path.Defects[i] : null);
				string pathName = ResolveResultPathDisplayName(result, path.PathName, isLeft, allowSideFallback, path.OccupiesFullWidth);
				string defectKey = bandMapDefectSeed?.DefectKey ?? BuildDefectKey(frameIdText, pathName, result, i);
				LegendStyleAssignment legendStyleAssignment = ResolveOrCreateLegendStyleLocked(result.ClassName, result.ClassId);
				PhysicalMetrics physicalMetrics = ResolvePhysicalMetrics(result, path);
				ImageSource localImageSource = bandMapDefectSeed?.PreviewImage ?? bandMapDefectSeed?.ThumbnailImage;
				string localImagePath = !path.SaveLocalDefectImages || localImageSource == null
					? string.Empty
					: BuildLocalDefectImagePath(BuildBatchNumberText(_batchStartedLocalTime, _batchNumber), frameIdText, _history.Count + 1);
				if (path.SaveLocalDefectImages)
				{
					QueueLocalDefectImageSave(localImageSource, localImagePath);
				}
				HistoryItem historyItem = new HistoryItem
				{
					DefectKey = defectKey,
					LegendKey = legendStyleAssignment.LegendKey,
					FrameSequence = frameSequence,
					XRatio = ResolveXRatio(result, isLeft, path.LaneWidth, path.OccupiesFullWidth),
					OccupiesFullWidth = path.OccupiesFullWidth,
					PathName = pathName,
					ResultIndex = bandMapDefectSeed?.ResultIndex ?? i,
					ClassId = result.ClassId,
					ClassName = legendStyleAssignment.ClassName,
					Confidence = result.Confidence,
					FrameIdText = frameIdText,
					CenterX = result.Cx,
					CenterY = result.Cy,
					Width = bandMapDefectSeed?.Width ?? result.Width,
					Height = bandMapDefectSeed?.Height ?? result.Height,
					Angle = bandMapDefectSeed?.Angle ?? result.Angle,
					SourceWidth = bandMapDefectSeed?.SourceWidth ?? 0,
					SourceHeight = bandMapDefectSeed?.SourceHeight ?? 0,
					PixelEquivalentX = path.PixelEquivalentX,
					PixelEquivalentY = path.PixelEquivalentY,
					EdgeCalibrationX = path.EdgeCalibrationX,
					HasSegmentation = bandMapDefectSeed?.HasSegmentation ?? HasInitializedSegmentation(result),
					ModelTypeText = bandMapDefectSeed?.ModelTypeText ?? result.ModelType.ToString(),
					PhysicalXMillimeters = physicalMetrics.PhysicalXMillimeters,
					PhysicalWidthMillimeters = physicalMetrics.PhysicalWidthMillimeters,
					LaneWidthMillimeters = physicalMetrics.LaneWidthMillimeters,
					CoordinateSource = physicalMetrics.CoordinateSource,
					ThumbnailImage = bandMapDefectSeed?.ThumbnailImage,
					PreviewImage = bandMapDefectSeed?.PreviewImage,
					LocalImagePath = localImagePath
				};
				_history.Add(historyItem);
				IncrementLegendCount(historyItem.LegendKey);
			}
		}
	}

	private static string ResolveResultPathDisplayName(Result result, string pathName, bool isLeft, bool allowSideFallback, bool occupiesFullWidth)
	{
		string groupedPathName = ResolveGroupedDualCameraResultPathName(result);
		if (!string.IsNullOrWhiteSpace(groupedPathName))
		{
			return groupedPathName;
		}

		string displayName = ResolveResultMetadataText(result, GroupedDualCameraOverviewBuilder.DisplayNameMetadataKey);
		if (!string.IsNullOrWhiteSpace(displayName))
		{
			return displayName;
		}

		return ResolvePathDisplayName(pathName, isLeft, allowSideFallback, occupiesFullWidth);
	}

	private static string ResolveGroupedDualCameraResultPathName(Result result)
	{
		string groupKey = ResolveResultMetadataText(result, GroupedDualCameraOverviewBuilder.GroupKeyMetadataKey);
		if (string.IsNullOrWhiteSpace(groupKey))
		{
			return string.Empty;
		}

		groupKey = NormalizeGroupedDualCameraGroupKey(groupKey);
		string sideText = ResolveGroupedDualCameraSideSuffix(
			ResolveResultMetadataText(result, GroupedDualCameraOverviewBuilder.SideMetadataKey));
		return string.IsNullOrWhiteSpace(sideText) ? groupKey : $"{groupKey}-{sideText}";
	}

	private static string ResolveGroupedDualCameraSideSuffix(string sideText)
	{
		if (string.IsNullOrWhiteSpace(sideText))
		{
			return string.Empty;
		}

		string normalized = sideText.Trim();
		if (string.Equals(normalized, "Left", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "L", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "左", StringComparison.OrdinalIgnoreCase))
		{
			return "L";
		}

		if (string.Equals(normalized, "Right", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "R", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(normalized, "右", StringComparison.OrdinalIgnoreCase))
		{
			return "R";
		}

		return string.Empty;
	}

	private static string ResolvePathHeaderDisplayName(BandMapPathInput path, bool isLeft, bool allowSideFallback)
	{
		if (path == null)
		{
			return ResolvePathDisplayName(null, isLeft, allowSideFallback);
		}

		string groupedText = ResolveGroupedDualCameraGroupsText(path.Results);
		if (!string.IsNullOrWhiteSpace(groupedText))
		{
			return groupedText;
		}

		return ResolvePathDisplayName(path.PathName, isLeft, allowSideFallback, path.OccupiesFullWidth);
	}

	private static string ResolveGroupedDualCameraGroupsText(IReadOnlyList<Result> results)
	{
		if (results == null || results.Count == 0)
		{
			return string.Empty;
		}

		List<string> groupKeys = new();
		HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
		foreach (Result result in results)
		{
			string groupKey = NormalizeGroupedDualCameraGroupKey(
				ResolveResultMetadataText(result, GroupedDualCameraOverviewBuilder.GroupKeyMetadataKey));
			if (!string.IsNullOrWhiteSpace(groupKey) && seen.Add(groupKey))
			{
				groupKeys.Add(groupKey);
			}
		}

		return groupKeys.Count == 0 ? string.Empty : string.Join("/", groupKeys);
	}

	private static string ResolveResultMetadataText(Result result, string key)
	{
		if (result?.Others == null || string.IsNullOrWhiteSpace(key))
		{
			return string.Empty;
		}

		return result.Others.TryGetValue(key, out object value) && value != null
			? Convert.ToString(value, CultureInfo.InvariantCulture)
			: string.Empty;
	}
	private static bool HasInitializedSegmentation(Result result)
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

	private void ResetRuntimeLocked()
	{
		_history.Clear();
		_legendCounts.Clear();
		_recentFrameResults.Clear();
		_processedFrameKeyOrder.Clear();
		_processedFrameKeys.Clear();
		_batchStartedLocalTime = DateTime.Now;
		_frameSequence = 0L;
		_totalFrames = 0;
		_okFrames = 0;
		_ngFrames = 0;
		_currentSpeedMetersPerMinute = 0.0;
		_lastFrameUtc = DateTime.MinValue;
		_lastFrameIdText = "-";
		_path1Header = "左路";
		_path2Header = "右路";
		_path1Result = "-";
		_path2Result = "-";
		_path1DefectCount = 0;
		_path2DefectCount = 0;
		_path1LaneWidthMillimeters = null;
		_path2LaneWidthMillimeters = null;
		_showPathStatusBadges = false;
		_selectedDefectKey = null;
		_selectionVersion++;
		_wallCurrentPage = 1;
		_viewportStartMeters = 0.0;
		_isViewportPinnedToLatest = true;
		_isWallPinnedToLatestPage = true;
	}

	private void UpdateFrameMetricsLocked(DateTime nowUtc)
	{
		if (_lastFrameUtc == DateTime.MinValue)
		{
			_currentSpeedMetersPerMinute = 0.0;
			return;
		}
		double totalMilliseconds = (nowUtc - _lastFrameUtc).TotalMilliseconds;
		if (double.IsFinite(totalMilliseconds) && !(totalMilliseconds <= 1.0) && !(totalMilliseconds > 15000.0))
		{
			double num = Math.Max(0.0, _frameSpanMillimeters) * 60.0 / totalMilliseconds;
			_currentSpeedMetersPerMinute = ((_currentSpeedMetersPerMinute <= 0.01) ? num : (_currentSpeedMetersPerMinute * 0.65 + num * 0.35));
		}
	}

	private void RecordFrameResultLocked(bool isNgFrame)
	{
		_totalFrames++;
		if (isNgFrame)
		{
			_ngFrames++;
		}
		else
		{
			_okFrames++;
		}
		_recentFrameResults.Enqueue(isNgFrame);
		while (_recentFrameResults.Count > 20)
		{
			_recentFrameResults.Dequeue();
		}
	}

	private void ArchiveCurrentRollLocked(DateTime endedLocalTime)
	{
		if (MaxArchivedRolls <= 0)
		{
			return;
		}

		if (_totalFrames > 0 || _history.Count != 0)
		{
			RollArchive item = CreateCurrentRollArchiveLocked(isCurrent: false, endedLocalTime);
			_archivedRolls.Add(item);
			while (_archivedRolls.Count > MaxArchivedRolls)
			{
				_archivedRolls.RemoveAt(0);
			}
		}
	}

	private RollArchive CreateCurrentRollArchiveLocked(bool isCurrent, DateTime? endedLocalTime = null)
	{
		BandMapSlittingSettings slittingSettings = BuildSlittingSettings(_history, _isSlittingEnabled, _knifeSpacingMillimeters, _firstCutOffsetMillimeters, _stripWidthMillimeters, _slitCount, _path1LaneWidthMillimeters, _path2LaneWidthMillimeters);
		string batchNumberText = BuildBatchNumberText(_batchStartedLocalTime, _batchNumber);
		return new RollArchive
		{
			BatchNumber = _batchNumber,
			BatchNumberText = batchNumberText,
			BatchStartedLocalTime = _batchStartedLocalTime,
			BatchEndedLocalTime = (isCurrent ? ((DateTime?)null) : endedLocalTime),
			FrameSequence = _frameSequence,
			TotalFrames = _totalFrames,
			OkFrames = _okFrames,
			NgFrames = _ngFrames,
			FrameSpanMillimeters = _frameSpanMillimeters,
			CumulativeMeters = (double)_frameSequence * Math.Max(1.0, _frameSpanMillimeters) / 1000.0,
			CurrentSpeedMetersPerMinute = _currentSpeedMetersPerMinute,
			LastFrameUtc = _lastFrameUtc,
			LastFrameIdText = _lastFrameIdText,
			Path1Header = _path1Header,
			Path2Header = _path2Header,
			Path1Result = _path1Result,
			Path2Result = _path2Result,
			Path1DefectCount = _path1DefectCount,
			Path2DefectCount = _path2DefectCount,
			ShowPathStatusBadges = _showPathStatusBadges,
			IsCurrent = isCurrent,
			SlittingSettings = slittingSettings,
			History = []
		};
	}

	private PhysicalMetrics ResolvePhysicalMetrics(Result result, BandMapPathInput path)
	{
		if (result == null)
		{
			return new PhysicalMetrics();
		}
		double? num = ((path != null && path.PixelEquivalentX > 0.0 && path.LaneWidth > 0.0) ? new double?(path.LaneWidth / path.PixelEquivalentX) : ((double?)null));
		double? num2 = TryGetResultPhysicalCoordinate(result, "DefectPostProcess.WorldX");
		string coordinateSource = ResolveCoordinateSourceText(result);
		ResultMapGeometry resultMapGeometry = ResolveResultMapGeometry(result, path?.LaneWidth ?? 0.0);
		if (!num2.HasValue && path != null && path.PixelEquivalentX > 0.0)
		{
			num2 = resultMapGeometry.CenterX / path.PixelEquivalentX - path.EdgeCalibrationX;
			coordinateSource = "PixelEquivalent";
		}
		double? num3 = TryGetResultPhysicalCoordinate(result, "DefectPostProcess.ActualWidth");
		if (!num3.HasValue && path != null && path.PixelEquivalentX > 0.0)
		{
			num3 = resultMapGeometry.Width / path.PixelEquivalentX;
		}
		return new PhysicalMetrics
		{
			PhysicalXMillimeters = (IsFiniteNullable(num2) ? num2 : ((double?)null)),
			PhysicalWidthMillimeters = (IsFiniteNullable(num3) ? num3 : ((double?)null)),
			LaneWidthMillimeters = (IsFiniteNullable(num) ? num : ((double?)null)),
			CoordinateSource = coordinateSource
		};
	}

    }
}
