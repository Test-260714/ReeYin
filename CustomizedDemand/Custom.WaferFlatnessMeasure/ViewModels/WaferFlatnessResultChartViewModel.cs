using Custom.WaferFlatnessMeasure.Models;
using Prism.Commands;
using Prism.Navigation.Regions;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Custom.WaferFlatnessMeasure.ViewModels
{
    public sealed class WaferFlatnessResultChartViewModel : DialogViewModelBase, INavigationAware
    {
        private const string UpSurfaceKey = "Core.UpSurface";
        private const string DownSurfaceKey = "Core.DownSurface";
        private const string ThicknessKey = "Core.Thickness";
        private const string OriginalDataKeyPrefix = "Original.";

        private readonly List<PreprocessDatasetModel> _chartPreDatas = new List<PreprocessDatasetModel>();
        private SensorDataCollectionModel? _modelParam;
        private SensorDataCollectionModel? _subscribedModel;
        private INotifyCollectionChanged? _subscribedDataSourceCollection;
        private IReadOnlyList<WaferFlatnessResultChartPoint> _chartPoints = Array.Empty<WaferFlatnessResultChartPoint>();
        private WaferFlatnessResultChartDataSource? _selectedChartDataSource;
        private string _chartTitle = "结果曲线";
        private string _chartStatus = "等待加载结果数据。";
        private string _loadedDataPath = string.Empty;
        private double _minimumValue = double.NaN;
        private double _maximumValue = double.NaN;
        private double _averageValue = double.NaN;

        public WaferFlatnessResultChartViewModel()
        {
            Title = "结果曲线";
            Icon = "\ue7c1";

            ChartDataSources = new ObservableCollection<WaferFlatnessResultChartDataSource>();
            LoadCommand = new DelegateCommand(RefreshChartData);
            RefreshCommand = new DelegateCommand(RefreshChartData);
        }

        public ObservableCollection<WaferFlatnessResultChartDataSource> ChartDataSources { get; }

        public DelegateCommand LoadCommand { get; }

        public DelegateCommand RefreshCommand { get; }

        public new SensorDataCollectionModel? ModelParam
        {
            get => _modelParam;
            private set
            {
                if (SetProperty(ref _modelParam, value))
                {
                    AttachModel(value);
                }
            }
        }

        public WaferFlatnessResultChartDataSource? SelectedChartDataSource
        {
            get => _selectedChartDataSource;
            set
            {
                if (SetProperty(ref _selectedChartDataSource, value))
                {
                    UpdateChartPoints();
                }
            }
        }

        public IReadOnlyList<WaferFlatnessResultChartPoint> ChartPoints
        {
            get => _chartPoints;
            private set => SetProperty(ref _chartPoints, value ?? Array.Empty<WaferFlatnessResultChartPoint>());
        }

        public string ChartTitle
        {
            get => _chartTitle;
            private set => SetProperty(ref _chartTitle, value ?? string.Empty);
        }

        public string ChartStatus
        {
            get => _chartStatus;
            private set => SetProperty(ref _chartStatus, value ?? string.Empty);
        }

        public string LoadedDataPath
        {
            get => _loadedDataPath;
            private set => SetProperty(ref _loadedDataPath, value ?? string.Empty);
        }

        public double MinimumValue
        {
            get => _minimumValue;
            private set => SetProperty(ref _minimumValue, value);
        }

        public double MaximumValue
        {
            get => _maximumValue;
            private set => SetProperty(ref _maximumValue, value);
        }

        public double AverageValue
        {
            get => _averageValue;
            private set => SetProperty(ref _averageValue, value);
        }

        public int SourceCount => ChartDataSources.Count;

        public int PointCount => ChartPoints.Count;

        public bool HasChartData => ChartPoints.Count > 0;

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            int serial = -999;
            if (navigationContext.Parameters.TryGetValue<int>("Serial", out int id))
            {
                serial = id;
            }

            ModelParam = ResolveModelParam(serial);
            RefreshChartData();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            AttachModel(null);
        }

        private static SensorDataCollectionModel? ResolveModelParam(int serial)
        {
            if (serial < 0)
            {
                return null;
            }

            string cacheKey = serial.ToString("D3");
            return PrismProvider.ProjectManager?
                .SltCurSolutionItem?
                .NodeParamCaches?
                .TryGetValue(cacheKey, out object? modelObject) == true
                ? modelObject as SensorDataCollectionModel
                : null;
        }

        private void RefreshChartData()
        {
            RunOnUiThread(RefreshChartDataCore);
        }

        private void RefreshChartDataCore()
        {
            _chartPreDatas.Clear();
            LoadedDataPath = string.Empty;

            if (ModelParam == null)
            {
                ClearChart("未找到当前节点的晶圆平面度测量模型。");
                return;
            }

            IReadOnlyList<PreprocessDatasetModel> resolvedPreDatas = ResolvePreDatas(ModelParam);
            _chartPreDatas.AddRange(resolvedPreDatas.Where(data => data != null));

            if (_chartPreDatas.Count == 0)
            {
                ClearChart("当前没有可显示的结果数据。");
                return;
            }

            RebuildChartDataSources();
            RaisePropertyChanged(nameof(SourceCount));

            if (SelectedChartDataSource == null && ChartDataSources.Count == 0)
            {
                ClearChart("当前结果数据中未找到 UpSurface、DownSurface、Thickness 或已配置数据源。");
                return;
            }

            if (SelectedChartDataSource == null && ChartDataSources.Count > 0)
            {
                SelectedChartDataSource = ChartDataSources[0];
            }
            else
            {
                UpdateChartPoints();
            }
        }

        private IReadOnlyList<PreprocessDatasetModel> ResolvePreDatas(SensorDataCollectionModel model)
        {
            if (model.PreDatas != null && model.PreDatas.Count > 0)
            {
                return model.PreDatas;
            }

            string csvPath = ResolveCsvPath(model);
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                return Array.Empty<PreprocessDatasetModel>();
            }

            try
            {
                LoadedDataPath = csvPath;
                return PreprocessDatasetModel.LoadFromCsv(csvPath);
            }
            catch (Exception ex)
            {
                ChartStatus = $"CSV 数据加载失败：{ex.Message}";
                return Array.Empty<PreprocessDatasetModel>();
            }
        }

        private static string ResolveCsvPath(SensorDataCollectionModel model)
        {
            string[] candidates =
            {
                model.LastPreDatasCsvPath,
                model.FilePath,
                model.SltFile
            };

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
                .FirstOrDefault() ?? string.Empty;
        }

        private void RebuildChartDataSources()
        {
            string? selectedKey = SelectedChartDataSource?.Key;
            ChartDataSources.Clear();

            AddSourceIfAvailable(new WaferFlatnessResultChartDataSource(UpSurfaceKey, "UpSurface", "UpSurface"));
            AddSourceIfAvailable(new WaferFlatnessResultChartDataSource(DownSurfaceKey, "DownSurface", "DownSurface"));
            AddSourceIfAvailable(new WaferFlatnessResultChartDataSource(ThicknessKey, "Thickness", "Thickness"));

            foreach (DataAnalysisDataSourceOption source in ModelParam?.DataAnalysisDataSources ?? Enumerable.Empty<DataAnalysisDataSourceOption>())
            {
                if (source == null || !source.IsEnabled || string.IsNullOrWhiteSpace(source.OriginalDataValueName))
                {
                    continue;
                }

                string valueName = PreprocessDatasetModel.NormalizeOriginalDataValueName(source.OriginalDataValueName);
                string displayName = string.IsNullOrWhiteSpace(source.Name) ? valueName : source.Name.Trim();
                AddSourceIfAvailable(new WaferFlatnessResultChartDataSource(
                    OriginalDataKeyPrefix + valueName,
                    displayName,
                    valueName));
            }

            IEnumerable<string> originalDataValueNames = _chartPreDatas
                .SelectMany(data => data.OriginalDataValues?.Keys ?? Enumerable.Empty<string>())
                .Select(PreprocessDatasetModel.NormalizeOriginalDataValueName)
                .Where(valueName => !string.IsNullOrWhiteSpace(valueName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(valueName => valueName, StringComparer.OrdinalIgnoreCase);

            foreach (string valueName in originalDataValueNames)
            {
                AddSourceIfAvailable(new WaferFlatnessResultChartDataSource(
                    OriginalDataKeyPrefix + valueName,
                    valueName,
                    valueName));
            }

            SelectedChartDataSource = ChartDataSources
                .FirstOrDefault(source => string.Equals(source.Key, selectedKey, StringComparison.OrdinalIgnoreCase))
                ?? ChartDataSources.FirstOrDefault();
        }

        private void AddSourceIfAvailable(WaferFlatnessResultChartDataSource source)
        {
            if (ChartDataSources.Any(item => string.Equals(item.Key, source.Key, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            if (!_chartPreDatas.Any(data => TryGetChartValue(data, source, out _)))
            {
                return;
            }

            ChartDataSources.Add(source);
        }

        private void UpdateChartPoints()
        {
            if (SelectedChartDataSource == null)
            {
                ClearChart("请选择要显示的数据源。");
                return;
            }

            List<WaferFlatnessResultChartPoint> points = new List<WaferFlatnessResultChartPoint>();
            for (int index = 0; index < _chartPreDatas.Count; index++)
            {
                PreprocessDatasetModel data = _chartPreDatas[index];
                if (TryGetChartValue(data, SelectedChartDataSource, out double value))
                {
                    points.Add(new WaferFlatnessResultChartPoint(index + 1, value));
                }
            }

            ChartPoints = points;
            RaisePointSummaryChanged();

            if (points.Count == 0)
            {
                ChartTitle = $"{SelectedChartDataSource.DisplayName} 曲线";
                ChartStatus = $"数据源 {SelectedChartDataSource.DisplayName} 没有有效数值。";
                return;
            }

            MinimumValue = points.Min(point => point.Y);
            MaximumValue = points.Max(point => point.Y);
            AverageValue = points.Average(point => point.Y);
            ChartTitle = $"{SelectedChartDataSource.DisplayName} 曲线";
            ChartStatus = $"已加载 {points.Count} 个有效点，X 轴为数据序号，Y 轴为 {SelectedChartDataSource.DisplayName}。";
        }

        private bool TryGetChartValue(
            PreprocessDatasetModel data,
            WaferFlatnessResultChartDataSource source,
            out double value)
        {
            value = double.NaN;
            if (data == null || source == null)
            {
                return false;
            }

            switch (source.Key)
            {
                case UpSurfaceKey:
                    value = data.UpSurface;
                    break;
                case DownSurfaceKey:
                    value = data.DownSurface;
                    break;
                case ThicknessKey:
                    value = data.Thickness;
                    if (!IsFinite(value))
                    {
                        value = PreprocessDatasetModel.CalculateThickness(
                            data.UpSurface,
                            data.DownSurface,
                            ModelParam?.ChannelCalibrationC ?? double.NaN);
                    }
                    break;
                default:
                    if (source.Key.StartsWith(OriginalDataKeyPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return data.TryGetOriginalDataValue(source.ValueName, out value);
                    }

                    break;
            }

            return IsFinite(value);
        }

        private void ClearChart(string status)
        {
            ChartPoints = Array.Empty<WaferFlatnessResultChartPoint>();
            MinimumValue = double.NaN;
            MaximumValue = double.NaN;
            AverageValue = double.NaN;
            ChartTitle = "结果曲线";
            ChartStatus = status;
            RaisePointSummaryChanged();
        }

        private void RaisePointSummaryChanged()
        {
            RaisePropertyChanged(nameof(PointCount));
            RaisePropertyChanged(nameof(HasChartData));
        }

        private void AttachModel(SensorDataCollectionModel? model)
        {
            if (_subscribedModel != null)
            {
                _subscribedModel.PropertyChanged -= OnModelPropertyChanged;
            }

            if (_subscribedDataSourceCollection != null)
            {
                _subscribedDataSourceCollection.CollectionChanged -= OnDataSourceCollectionChanged;
            }

            _subscribedModel = model;
            _subscribedDataSourceCollection = model?.DataAnalysisDataSources;

            if (_subscribedModel != null)
            {
                _subscribedModel.PropertyChanged += OnModelPropertyChanged;
            }

            if (_subscribedDataSourceCollection != null)
            {
                _subscribedDataSourceCollection.CollectionChanged += OnDataSourceCollectionChanged;
            }
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(SensorDataCollectionModel.PreDatas) ||
                e.PropertyName == nameof(SensorDataCollectionModel.PreDataCount) ||
                e.PropertyName == nameof(SensorDataCollectionModel.LastPreDatasCsvPath) ||
                e.PropertyName == nameof(SensorDataCollectionModel.FilePath) ||
                e.PropertyName == nameof(SensorDataCollectionModel.SltFile) ||
                e.PropertyName == nameof(SensorDataCollectionModel.DataAnalysisDataSources) ||
                e.PropertyName == nameof(SensorDataCollectionModel.ChannelCalibrationC))
            {
                RefreshChartData();
            }
        }

        private void OnDataSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshChartData();
        }

        private static void RunOnUiThread(Action action)
        {
            if (action == null)
            {
                return;
            }

            var dispatcher = PrismProvider.Dispatcher;
            if (dispatcher == null ||
                dispatcher.CheckAccess() ||
                dispatcher.HasShutdownStarted ||
                dispatcher.HasShutdownFinished)
            {
                action();
                return;
            }

            dispatcher.Invoke(action);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }

    public sealed class WaferFlatnessResultChartDataSource
    {
        public WaferFlatnessResultChartDataSource(string key, string displayName, string valueName)
        {
            Key = key ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? valueName : displayName.Trim();
            ValueName = valueName ?? string.Empty;
        }

        public string Key { get; }

        public string DisplayName { get; }

        public string ValueName { get; }
    }

    public sealed class WaferFlatnessResultChartPoint
    {
        public WaferFlatnessResultChartPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }

        public double Y { get; }
    }
}
