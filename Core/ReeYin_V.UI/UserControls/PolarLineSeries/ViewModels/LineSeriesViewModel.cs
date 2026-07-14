using Arction.Wpf.ChartingMVVM;
using Prism.Events;
using Prism.Mvvm;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.IOC;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace ReeYin_V.UI.UserControls.PolarLineSeries
{
    #region 事件定义
    /// <summary>
    /// 极坐标图表配置更新事件
    /// </summary>
    public class PolarChartConfigUpdateEvent : PubSubEvent<PolarChartConfigPayload> { }

    /// <summary>
    /// 极坐标图表清空事件
    /// </summary>
    public class PolarChartClearEvent : PubSubEvent<string> { }

    /// <summary>
    /// 数据更新载荷
    /// </summary>
    public class PolarChartDataPayload
    {
        /// <summary>
        /// 图表标识（用于区分多个图表实例）
        /// </summary>
        public string ChartId { get; set; }

        /// <summary>
        /// 角度数组
        /// </summary>
        public double[] Angles { get; set; }

        /// <summary>
        /// 振幅数组
        /// </summary>
        public double[] Amplitudes { get; set; }

        /// <summary>
        /// 是否追加数据（false则替换）
        /// </summary>
        public bool IsAppend { get; set; } = false;
    }

    /// <summary>
    /// 配置更新载荷
    /// </summary>
    public class PolarChartConfigPayload
    {
        public string ChartId { get; set; }
        public double? MinAmplitude { get; set; }
        public double? MaxAmplitude { get; set; }
        public int? MajorDivCount { get; set; }
        public int? InnerCircleRadiusPercentage { get; set; }
        public double? PointSize { get; set; }
        public bool? PointsVisible { get; set; }
        public double? SectorBeginAngle { get; set; }
        public double? SectorEndAngle { get; set; }
        public double? SectorMinAmplitude { get; set; }
        public double? SectorMaxAmplitude { get; set; }
        public string ChartName { get; set; }

    }

    #endregion

    /// <summary>
    /// 极坐标折线图 ViewModel
    /// </summary>
    public class LineSeriesViewModel : DependencyObject, INotifyPropertyChanged, IDisposable
    {
        #region 字段

        private readonly LineSeriesModel _model;
        private readonly IEventAggregator _eventAggregator;
        private SubscriptionToken _dataUpdateToken;
        private SubscriptionToken _configUpdateToken;
        private SubscriptionToken _clearToken;
        private bool _disposed = false;

        #endregion

        #region 依赖属性

        public static readonly DependencyProperty SeriesPointProperty =
            DependencyProperty.Register(
                nameof(SeriesPoint),
                typeof(PolarSeriesPoint[]),
                typeof(LineSeriesViewModel),
                new PropertyMetadata(null));

        public PolarSeriesPoint[] SeriesPoint
        {
            get => GetValue(SeriesPointProperty) as PolarSeriesPoint[];
            set => SetValue(SeriesPointProperty, value);
        }

        #endregion

        #region 可绑定属性

        private string _chartId = Guid.NewGuid().ToString();
        public string ChartId
        {
            get => _chartId;
            set { _chartId = value; OnPropertyChanged(); }
        }

        private double _minAmplitude = 0;
        public double MinAmplitude
        {
            get => _minAmplitude;
            set { _minAmplitude = value; OnPropertyChanged(); }
        }

        private double _maxAmplitude = 20;
        public double MaxAmplitude
        {
            get => _maxAmplitude;
            set { _maxAmplitude = value; OnPropertyChanged(); }
        }

        private int _majorDivCount = 4;
        public int MajorDivCount
        {
            get => _majorDivCount;
            set { _majorDivCount = value; OnPropertyChanged(); }
        }

        private int _innerCircleRadiusPercentage = 10;
        public int InnerCircleRadiusPercentage
        {
            get => _innerCircleRadiusPercentage;
            set { _innerCircleRadiusPercentage = value; OnPropertyChanged(); }
        }

        private double _pointSize = 4;
        public double PointSize
        {
            get => _pointSize;
            set { _pointSize = value; OnPropertyChanged(); }
        }

        private bool _pointsVisible = true;
        public bool PointsVisible
        {
            get => _pointsVisible;
            set { _pointsVisible = value; OnPropertyChanged(); }
        }

        private double _sectorBeginAngle = 0;
        public double SectorBeginAngle
        {
            get => _sectorBeginAngle;
            set { _sectorBeginAngle = value; OnPropertyChanged(); }
        }

        private double _sectorEndAngle = 45;
        public double SectorEndAngle
        {
            get => _sectorEndAngle;
            set { _sectorEndAngle = value; OnPropertyChanged(); }
        }

        private double _sectorMinAmplitude = 10;
        public double SectorMinAmplitude
        {
            get => _sectorMinAmplitude;
            set { _sectorMinAmplitude = value; OnPropertyChanged(); }
        }

        private double _sectorMaxAmplitude = 20;
        public double SectorMaxAmplitude
        {
            get => _sectorMaxAmplitude;
            set { _sectorMaxAmplitude = value; OnPropertyChanged(); }
        }

        private string _chartName = "Polar Line Series Chart";
        public string ChartName
        {
            get => _chartName;
            set { _chartName = value; OnPropertyChanged(); }
        }

        #endregion

        #region 构造函数

        public LineSeriesViewModel()
        {
            _model = new LineSeriesModel();
            _eventAggregator = PrismProvider.EventAggregator;

            // 初始化默认数据
            SeriesPoint = _model.GenerateData();

            // 订阅事件
            SubscribeEvents();
            //StartRealTimeDataCollection();
        }

        // 模拟实时数据采集
        private async Task StartRealTimeDataCollection()
        {
            var random = new Random();
            while (true)
            {
                // 生成新数据
                var angles = Enumerable.Range(0, 360).Select(i => (double)i).ToArray();
                var amplitudes = angles.Select(a => 10 + 5 * Math.Sin(a * Math.PI / 180) + random.NextDouble() * 2).ToArray();

                // 通过事件聚合器发送数据
                PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("LineSeriesViewShow", new PolarChartDataPayload
                {
                    Angles = angles,
                    Amplitudes = amplitudes
                }));

                await Task.Delay(100); // 100ms 刷新一次
            }
        }


        /// <summary>
        /// 带图表ID的构造函数
        /// </summary>
        public LineSeriesViewModel(string chartId) : this()
        {
            ChartId = chartId;
        }

        #endregion

        #region 事件订阅

        private void SubscribeEvents()
        {
            if (_eventAggregator == null) return;

            // 订阅数据更新事件
            _dataUpdateToken = _eventAggregator.GetEvent<OutputResultEvent>().Subscribe(OnDataUpdate, ThreadOption.BackgroundThread);

            // 订阅配置更新事件
            _configUpdateToken = _eventAggregator
                .GetEvent<PolarChartConfigUpdateEvent>()
                .Subscribe(OnConfigUpdate, ThreadOption.UIThread);

            // 订阅清空事件
            _clearToken = _eventAggregator
                .GetEvent<PolarChartClearEvent>()
                .Subscribe(OnClear, ThreadOption.UIThread);
        }

        private void UnsubscribeEvents()
        {
            _dataUpdateToken?.Dispose();
            _configUpdateToken?.Dispose();
            _clearToken?.Dispose();
        }

        #endregion

        #region 事件处理

        private void OnDataUpdate((string, object) tuple)
        {
            if (tuple.Item1 != "LineSeriesViewShow") { return; }

            PolarChartDataPayload payload = (PolarChartDataPayload)tuple.Item2;
            if (payload == null) return;
            if (!string.IsNullOrEmpty(payload.ChartId) && payload.ChartId != ChartId) return;

            if (payload.IsAppend && SeriesPoint != null)
            {
                // 追加数据
                var newPoints = _model.GenerateData(payload.Angles, payload.Amplitudes);
                var combined = new PolarSeriesPoint[SeriesPoint.Length + newPoints.Length];
                Array.Copy(SeriesPoint, combined, SeriesPoint.Length);
                Array.Copy(newPoints, 0, combined, SeriesPoint.Length, newPoints.Length);
                SeriesPoint = combined;
            }
            else
            {
                // 替换数据
                SeriesPoint = _model.GenerateData(payload.Angles, payload.Amplitudes);
            }
        }

        private void OnConfigUpdate(PolarChartConfigPayload payload)
        {
            if (payload == null) return;
            if (!string.IsNullOrEmpty(payload.ChartId) && payload.ChartId != ChartId) return;

            if (payload.MinAmplitude.HasValue) MinAmplitude = payload.MinAmplitude.Value;
            if (payload.MaxAmplitude.HasValue) MaxAmplitude = payload.MaxAmplitude.Value;
            if (payload.MajorDivCount.HasValue) MajorDivCount = payload.MajorDivCount.Value;
            if (payload.InnerCircleRadiusPercentage.HasValue) InnerCircleRadiusPercentage = payload.InnerCircleRadiusPercentage.Value;
            if (payload.PointSize.HasValue) PointSize = payload.PointSize.Value;
            if (payload.PointsVisible.HasValue) PointsVisible = payload.PointsVisible.Value;
            if (payload.SectorBeginAngle.HasValue) SectorBeginAngle = payload.SectorBeginAngle.Value;
            if (payload.SectorEndAngle.HasValue) SectorEndAngle = payload.SectorEndAngle.Value;
            if (payload.SectorMinAmplitude.HasValue) SectorMinAmplitude = payload.SectorMinAmplitude.Value;
            if (payload.SectorMaxAmplitude.HasValue) SectorMaxAmplitude = payload.SectorMaxAmplitude.Value;
            if (!string.IsNullOrEmpty(payload.ChartName)) ChartName = payload.ChartName;
        }

        private void OnClear(string chartId)
        {
            if (!string.IsNullOrEmpty(chartId) && chartId != ChartId) return;
            SeriesPoint = _model.ClearData();
        }

        #endregion

        #region 公共方法（直接调用）

        /// <summary>
        /// 更新图表数据
        /// </summary>
        /// <param name="angles">角度数组</param>
        /// <param name="amplitudes">振幅数组</param>
        public void UpdateData(double[] angles, double[] amplitudes)
        {
            SeriesPoint = _model.GenerateData(angles, amplitudes);
        }

        /// <summary>
        /// 更新图表数据（仅振幅，角度自动均匀分布）
        /// </summary>
        /// <param name="amplitudes">振幅数组</param>
        public void UpdateData(double[] amplitudes)
        {
            SeriesPoint = _model.GenerateData(amplitudes);
        }

        /// <summary>
        /// 添加单个数据点
        /// </summary>
        /// <param name="angle">角度</param>
        /// <param name="amplitude">振幅</param>
        public void AddPoint(double angle, double amplitude)
        {
            SeriesPoint = _model.AddPoint(SeriesPoint, angle, amplitude);
        }

        /// <summary>
        /// 生成随机测试数据
        /// </summary>
        public void GenerateRandomData()
        {
            _model.PointCount = 360;
            _model.MinAmplitude = MinAmplitude;
            _model.MaxAmplitude = MaxAmplitude;
            SeriesPoint = _model.GenerateData();
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void ClearData()
        {
            SeriesPoint = _model.ClearData();
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(double? minAmp = null, double? maxAmp = null, int? majorDiv = null,
            int? innerRadius = null, double? pointSize = null, bool? pointsVisible = null)
        {
            if (minAmp.HasValue) MinAmplitude = minAmp.Value;
            if (maxAmp.HasValue) MaxAmplitude = maxAmp.Value;
            if (majorDiv.HasValue) MajorDivCount = majorDiv.Value;
            if (innerRadius.HasValue) InnerCircleRadiusPercentage = innerRadius.Value;
            if (pointSize.HasValue) PointSize = pointSize.Value;
            if (pointsVisible.HasValue) PointsVisible = pointsVisible.Value;
        }

        /// <summary>
        /// 更新扇区配置
        /// </summary>
        public void UpdateSector(double? beginAngle = null, double? endAngle = null,
            double? minAmp = null, double? maxAmp = null)
        {
            if (beginAngle.HasValue) SectorBeginAngle = beginAngle.Value;
            if (endAngle.HasValue) SectorEndAngle = endAngle.Value;
            if (minAmp.HasValue) SectorMinAmplitude = minAmp.Value;
            if (maxAmp.HasValue) SectorMaxAmplitude = maxAmp.Value;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            UnsubscribeEvents();
            _disposed = true;
        }

        #endregion
    }
}
