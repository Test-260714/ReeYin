using System;

namespace Custom.WaferFlatnessMeasure.Models
{
    [Serializable]
    public class MeasurementDataFilterOptions
    {
        public bool MedianFilterEnabled { get; set; }

        public int MedianFilterWindowSize { get; set; } = 3;

        public bool SmoothFilterEnabled { get; set; }

        public int SmoothFilterWindowSize { get; set; } = 3;

        public bool FilterUpSurface { get; set; } = true;

        public bool FilterDownSurface { get; set; } = true;

        public bool FilterThickness { get; set; } = true;

        public bool FilterOriginalDataValues { get; set; }

        public string UpSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultUpSurfaceOriginalDataName;

        public string DownSurfaceOriginalDataValueName { get; set; } = PreprocessDatasetModel.DefaultDownSurfaceOriginalDataName;

        public MeasurementDataFilterOptions Clone()
        {
            return new MeasurementDataFilterOptions
            {
                MedianFilterEnabled = MedianFilterEnabled,
                MedianFilterWindowSize = MedianFilterWindowSize,
                SmoothFilterEnabled = SmoothFilterEnabled,
                SmoothFilterWindowSize = SmoothFilterWindowSize,
                FilterUpSurface = FilterUpSurface,
                FilterDownSurface = FilterDownSurface,
                FilterThickness = FilterThickness,
                FilterOriginalDataValues = FilterOriginalDataValues,
                UpSurfaceOriginalDataValueName = UpSurfaceOriginalDataValueName,
                DownSurfaceOriginalDataValueName = DownSurfaceOriginalDataValueName
            };
        }
    }
}
