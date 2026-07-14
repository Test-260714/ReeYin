using Custom.WaferFlatnessMeasure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.Models
{
    [Serializable]
    public sealed class SensorTrajectoryDataPackage
    {
        public bool IsPoint { get; set; }

        public List<PreprocessDatasetModel> PreDatas { get; set; } = new List<PreprocessDatasetModel>();

        public int PreDataCount { get; set; }

        public List<WaferCollectPoint> CollectPoints { get; set; } = new List<WaferCollectPoint>();

        public List<PointCollectionStepInfo> PointCollectionSteps { get; set; } = new List<PointCollectionStepInfo>();

        public List<LineSegmentStartPositionInfo> LineSegmentStartPositions { get; set; } = new List<LineSegmentStartPositionInfo>();

        public string LineSegmentCsvSessionDirectory { get; set; } = string.Empty;

        public int CurrentLineSegmentIndex { get; set; }

        public int ExpectedLineSegmentCount { get; set; }

        public string SourceCsvPath { get; set; } = string.Empty;

        public string LastPreDatasCsvPath { get; set; } = string.Empty;

        public string LastCalibrationWaferDataCsvPath { get; set; } = string.Empty;

        public string SensorModelName { get; set; } = string.Empty;

        public string UpSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;

        public string DownSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;

        public MeasurementDataFilterOptions FilterOptions { get; set; } = new MeasurementDataFilterOptions();

        public static SensorTrajectoryDataPackage Create(
            bool isPoint,
            IEnumerable<PreprocessDatasetModel>? preDatas,
            IEnumerable<PointCollectionStepInfo>? pointCollectionSteps,
            IEnumerable<LineSegmentStartPositionInfo>? lineSegmentStartPositions,
            string? lineSegmentCsvSessionDirectory,
            int currentLineSegmentIndex,
            int expectedLineSegmentCount,
            string? sourceCsvPath,
            string? lastPreDatasCsvPath,
            string? lastCalibrationWaferDataCsvPath,
            string? sensorModelName,
            string? upSurfaceOriginalDataValueName,
            string? downSurfaceOriginalDataValueName,
            MeasurementDataFilterOptions? filterOptions)
        {
            List<PreprocessDatasetModel> preDataSnapshot = PreprocessDatasetModel.Clone(preDatas);
            return new SensorTrajectoryDataPackage
            {
                IsPoint = isPoint,
                PreDatas = preDataSnapshot,
                PreDataCount = preDataSnapshot.Count,
                CollectPoints = CreateCollectPoints(preDataSnapshot),
                PointCollectionSteps = pointCollectionSteps?
                    .Where(step => step != null)
                    .Select(step => step.Clone())
                    .ToList() ?? new List<PointCollectionStepInfo>(),
                LineSegmentStartPositions = lineSegmentStartPositions?
                    .Where(position => position != null)
                    .Select(position => position.Clone())
                    .ToList() ?? new List<LineSegmentStartPositionInfo>(),
                LineSegmentCsvSessionDirectory = lineSegmentCsvSessionDirectory ?? string.Empty,
                CurrentLineSegmentIndex = currentLineSegmentIndex,
                ExpectedLineSegmentCount = expectedLineSegmentCount,
                SourceCsvPath = sourceCsvPath ?? string.Empty,
                LastPreDatasCsvPath = lastPreDatasCsvPath ?? string.Empty,
                LastCalibrationWaferDataCsvPath = lastCalibrationWaferDataCsvPath ?? string.Empty,
                SensorModelName = sensorModelName ?? string.Empty,
                UpSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                    upSurfaceOriginalDataValueName,
                    PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName),
                DownSurfaceOriginalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(
                    downSurfaceOriginalDataValueName,
                    PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName),
                FilterOptions = filterOptions?.Clone() ?? new MeasurementDataFilterOptions()
            };
        }

        private static List<WaferCollectPoint> CreateCollectPoints(IReadOnlyList<PreprocessDatasetModel> preDatas)
        {
            return preDatas
                .Select((data, index) => new WaferCollectPoint
                {
                    Index = index + 1,
                    X = data.PosX,
                    Y = data.PosY
                })
                .ToList();
        }
    }

    [Serializable]
    public sealed class WaferCollectPoint
    {
        public int Index { get; set; }

        public double X { get; set; }

        public double Y { get; set; }
    }
}
