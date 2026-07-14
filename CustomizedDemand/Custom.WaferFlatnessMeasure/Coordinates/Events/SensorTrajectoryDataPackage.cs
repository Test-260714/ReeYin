using Custom.WaferFlatnessMeasure.Models;
using System;
using System.Collections.Generic;

namespace Custom.WaferFlatnessMeasure
{
    public sealed class SensorTrajectoryDataPackage
    {
        public bool IsPoint { get; private set; }

        public List<PreprocessDatasetModel> PreDatas { get; private set; } = new List<PreprocessDatasetModel>();

        public List<double[]> CollectPoints { get; private set; } = new List<double[]>();

        public List<PointCollectionStepInfo> PointCollectionSteps { get; private set; } = new List<PointCollectionStepInfo>();

        public List<LineSegmentStartPositionInfo> LineSegmentStartPositions { get; private set; } = new List<LineSegmentStartPositionInfo>();

        public string LineSegmentCsvSessionDirectory { get; private set; } = string.Empty;

        public int CurrentLineSegmentIndex { get; private set; }

        public int ExpectedLineSegmentCount { get; private set; }

        public string SourceCsvPath { get; private set; } = string.Empty;

        public string LastPreDatasCsvPath { get; private set; } = string.Empty;

        public string LastCalibrationWaferDataCsvPath { get; private set; } = string.Empty;

        public string SensorModelName { get; private set; } = string.Empty;

        public string UpSurfaceOriginalDataValueName { get; private set; } =
            PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;

        public string DownSurfaceOriginalDataValueName { get; private set; } =
            PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;

        public MeasurementDataFilterOptions FilterOptions { get; private set; } = new MeasurementDataFilterOptions();

        public int PreDataCount => PreDatas.Count;

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
            List<PreprocessDatasetModel> clonedPreDatas = PreprocessDatasetModel.Clone(preDatas);

            return new SensorTrajectoryDataPackage
            {
                IsPoint = isPoint,
                PreDatas = clonedPreDatas,
                CollectPoints = PreprocessDatasetModel.ToUpSurfacePointCloud(clonedPreDatas),
                PointCollectionSteps = ClonePointCollectionSteps(pointCollectionSteps),
                LineSegmentStartPositions = CloneLineSegmentStartPositions(lineSegmentStartPositions),
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

        private static List<PointCollectionStepInfo> ClonePointCollectionSteps(
            IEnumerable<PointCollectionStepInfo>? source)
        {
            var result = new List<PointCollectionStepInfo>();
            if (source == null)
            {
                return result;
            }

            foreach (PointCollectionStepInfo item in source)
            {
                if (item != null)
                {
                    result.Add(item.Clone());
                }
            }

            return result;
        }

        private static List<LineSegmentStartPositionInfo> CloneLineSegmentStartPositions(
            IEnumerable<LineSegmentStartPositionInfo>? source)
        {
            var result = new List<LineSegmentStartPositionInfo>();
            if (source == null)
            {
                return result;
            }

            foreach (LineSegmentStartPositionInfo item in source)
            {
                if (item != null)
                {
                    result.Add(item.Clone());
                }
            }

            return result;
        }
    }
}
