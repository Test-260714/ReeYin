using Prism.Mvvm;
using System;

namespace Custom.WaferFlatnessMeasure.Models
{
    public enum DataAnalysisAlgorithmKind
    {
        Flatness,
        SurfaceStatistics,
        Parallelism,
        TTV,
        THK,
        TIR,
        Warp1,
        Warp2
    }

    [Serializable]
    public class DataAnalysisDataSourceOption : BindableBase
    {
        public Guid DataSourceId { get; set; } = Guid.NewGuid();

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        private string _name = string.Empty;
        public string Name
        {
            get { return _name; }
            set
            {
                string normalizedName = string.IsNullOrWhiteSpace(value)
                    ? "DataSource"
                    : value.Trim();
                SetProperty(ref _name, normalizedName);
            }
        }

        private string _originalDataValueName = string.Empty;
        public string OriginalDataValueName
        {
            get { return _originalDataValueName; }
            set
            {
                string normalizedValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(value);
                SetProperty(ref _originalDataValueName, normalizedValueName);
            }
        }

        private bool _isFilterEnabled;
        public bool IsFilterEnabled
        {
            get { return _isFilterEnabled; }
            set { SetProperty(ref _isFilterEnabled, value); }
        }

        private bool _isRawDataFilter;
        public bool IsRawDataFilter
        {
            get { return _isRawDataFilter; }
            set { SetProperty(ref _isRawDataFilter, value); }
        }

        private double _filterMin = -99999999d;
        public double FilterMin
        {
            get { return _filterMin; }
            set { SetProperty(ref _filterMin, value); }
        }

        private double _filterMax = 99999999d;
        public double FilterMax
        {
            get { return _filterMax; }
            set { SetProperty(ref _filterMax, value); }
        }

        public DataAnalysisDataSourceOption()
        {
        }

        public DataAnalysisDataSourceOption(
            string name,
            string originalDataValueName,
            bool isEnabled = true,
            bool isFilterEnabled = false,
            bool isRawDataFilter = false,
            double filterMin = -99999999d,
            double filterMax = 99999999d)
        {
            _name = string.IsNullOrWhiteSpace(name) ? "DataSource" : name.Trim();
            _originalDataValueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(originalDataValueName);
            _isEnabled = isEnabled;
            _isFilterEnabled = isFilterEnabled;
            _isRawDataFilter = isRawDataFilter;
            _filterMin = filterMin;
            _filterMax = filterMax;
        }
    }

    [Serializable]
    public class DataAnalysisAlgorithmOption : BindableBase
    {
        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

        private DataAnalysisAlgorithmKind _algorithm;
        public DataAnalysisAlgorithmKind Algorithm
        {
            get { return _algorithm; }
            set
            {
                if (SetProperty(ref _algorithm, value))
                {
                    RaisePropertyChanged(nameof(DisplayName));
                    RaisePropertyChanged(nameof(RequiredDataSourceText));
                    RaisePropertyChanged(nameof(Description));
                    RaisePropertyChanged(nameof(RequiresPairSource));
                }
            }
        }

        public string DisplayName => GetDisplayName(Algorithm);

        public bool RequiresPairSource => RequiresPairSources(Algorithm);

        public string RequiredDataSourceText => RequiresPairSources(Algorithm) ? "双数据源" : "单数据源";

        private Guid? _sourceADataSourceId;
        public Guid? SourceADataSourceId
        {
            get { return _sourceADataSourceId; }
            set { SetProperty(ref _sourceADataSourceId, value); }
        }

        private Guid? _sourceBDataSourceId;
        public Guid? SourceBDataSourceId
        {
            get { return _sourceBDataSourceId; }
            set { SetProperty(ref _sourceBDataSourceId, value); }
        }

        public string Description => Algorithm switch
        {
            DataAnalysisAlgorithmKind.Flatness => "对每个启用数据源计算平面度",
            DataAnalysisAlgorithmKind.SurfaceStatistics => "对每个启用数据源计算TTV/最小值/最大值",
            DataAnalysisAlgorithmKind.Parallelism => "按选择的数据源A/B计算平行度",
            DataAnalysisAlgorithmKind.TTV => "按选择的数据源A/B计算TTV",
            DataAnalysisAlgorithmKind.THK => "按选择的数据源A/B计算厚度范围",
            DataAnalysisAlgorithmKind.TIR => "按选择的数据源A/B计算TIR",
            DataAnalysisAlgorithmKind.Warp1 => "按选择的数据源A/B和GBT6620-2009计算Warp",
            DataAnalysisAlgorithmKind.Warp2 => "按选择的数据源A/B和GBT32280-2022计算Warp",
            _ => string.Empty
        };

        public DataAnalysisAlgorithmOption()
        {
        }

        public DataAnalysisAlgorithmOption(DataAnalysisAlgorithmKind algorithm, bool isEnabled)
        {
            _algorithm = algorithm;
            _isEnabled = isEnabled;
        }

        public static bool RequiresPairSources(DataAnalysisAlgorithmKind algorithm)
        {
            return algorithm is DataAnalysisAlgorithmKind.Parallelism
                or DataAnalysisAlgorithmKind.TTV
                or DataAnalysisAlgorithmKind.THK
                or DataAnalysisAlgorithmKind.TIR
                or DataAnalysisAlgorithmKind.Warp1
                or DataAnalysisAlgorithmKind.Warp2;
        }

        public static string GetDisplayName(DataAnalysisAlgorithmKind algorithm)
        {
            return algorithm switch
            {
                DataAnalysisAlgorithmKind.Flatness => "平面度",
                DataAnalysisAlgorithmKind.SurfaceStatistics => "表面统计",
                DataAnalysisAlgorithmKind.Parallelism => "平行度",
                DataAnalysisAlgorithmKind.TTV => "TTV",
                DataAnalysisAlgorithmKind.THK => "厚度(THK)",
                DataAnalysisAlgorithmKind.TIR => "TIR",
                DataAnalysisAlgorithmKind.Warp1 => "Warp1",
                DataAnalysisAlgorithmKind.Warp2 => "Warp2",
                _ => algorithm.ToString()
            };
        }
    }

    [Serializable]
    public class DataAnalysisResult : BindableBase
    {
        private string _algorithmName = string.Empty;
        public string AlgorithmName
        {
            get { return _algorithmName; }
            set { SetProperty(ref _algorithmName, value ?? string.Empty); }
        }

        private string _dataSourceName = string.Empty;
        public string DataSourceName
        {
            get { return _dataSourceName; }
            set { SetProperty(ref _dataSourceName, value ?? string.Empty); }
        }

        private double _value = double.NaN;
        public double Value
        {
            get { return _value; }
            set { SetProperty(ref _value, value); }
        }

        private double _minValue = double.NaN;
        public double MinValue
        {
            get { return _minValue; }
            set { SetProperty(ref _minValue, value); }
        }

        private double _maxValue = double.NaN;
        public double MaxValue
        {
            get { return _maxValue; }
            set { SetProperty(ref _maxValue, value); }
        }

        private int _pointCount;
        public int PointCount
        {
            get { return _pointCount; }
            set { SetProperty(ref _pointCount, value); }
        }

        private string _status = string.Empty;
        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value ?? string.Empty); }
        }
    }
}
