using Newtonsoft.Json;
using PrecitecClass;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.UI;
using System.Text.RegularExpressions;
using System.Windows;
using static PrecitecClass.PrecitecControllerClassSync;
using static ReeYin.Hardware.Sensor.ChroCodile.Defines.General;

namespace ReeYin.Hardware.Sensor.ChroCodile
{
    /// <summary>
    /// 普雷茨特传感器
    /// </summary>
    public class ChroCodileSensor : SensorBase
    {
        #region Fields
        [JsonIgnore]
        public PrecitecControllerClassSync _controller = new PrecitecControllerClassSync();

        [JsonIgnore]
        private readonly List<DataSamples> _resultCache = new List<DataSamples>();

        [JsonIgnore]
        private readonly object _resultCacheLock = new object();

        [JsonIgnore]
        private int _expectedDataSamplesCount;

        [JsonIgnore]
        private readonly ManualResetEventSlim _collectCompletedEvent = new ManualResetEventSlim(false);

        private const int COLLECT_WAIT_TIMEOUT_MS = 2000;
        #endregion

        #region Properties
        private ChroCodileConfig _currentConfig = new ChroCodileConfig();
        /// <summary>
        /// 普雷茨特传感器参数配方
        /// </summary>
        public ChroCodileConfig CurrentConfig
        {
            get { return _currentConfig; }
            set { _currentConfig = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Constructor
        public ChroCodileSensor()
        {
            IP = "192.168.170.3";      
        }
        #endregion

        #region Override
        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        public override bool Init()
        {
            int res = _controller.Connect(IP, (int)CurrentConfig.SensorModel);  //连接传感器 0:1代传感器  1:2S   2:DPS/CLS/CLS2  3:Mini/C 
            if (res < 0)
            {
                IsConnected = false;
                return false;
            }
            string outsigs = CurrentConfig.OutputSignal.Replace(',', ' ');

            SetParameters("输出信号", outsigs, true);

            if (_controller.DataSampling == null)
            {
                _controller.DataSampling += ChroCodileEventCallbackHandle;
            }

            if(_controller.del_Status == null)
            {
                _controller.del_Status += (status)  //连接状态委托
                =>
                {
                    try
                    {
                        if (status == ConnectStatus.Disconnected)
                        {
                            _controller.CloseConnection();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.LogError($"普雷茨特传感器:{ex.Message}");
                    }
                };
            }
            IsConnected = true;

            return true;
        }

        /// <summary>
        /// 开始触发
        /// </summary>
        public override void StartCollect()
        {
            Console.WriteLine("开始触发采集");

            _collectCompletedEvent.Reset();
            _expectedDataSamplesCount = 0;

            lock (_resultCacheLock)
            {
                Console.WriteLine("清空_resultCache集合");
                _resultCache.Clear();
                if (_resultCache.Count == 0)
                {
                    Console.WriteLine("清空_resultCache集合成功，当前集合数量为0");
                }
            }

            int width = 0, height = 0;
            if (CurrentConfig.IsIOTrigger)
            {
                width = CurrentConfig.SamplingWidth;
                height = CurrentConfig.SamplingHeight;
                if (width == 0 || height == 0)
                {
                    MessageBox.Show("采样宽度或者采样高度设定存在0", "提示");
                    return;
                }

                SetExpectedDataSamplesCount(width);
                Console.WriteLine("开始测量");
                _controller.startMeasure();
                _controller.ExecStringCommand("ETR 3 0");
                _controller.ExecStringCommand("TRE");
            }
            else if (CurrentConfig.IsEncoderTrigger)
            {
                Console.WriteLine("通过编码器触发");
                int startpos = CurrentConfig.StartPosition;
                int stoppos = CurrentConfig.EndPosition;
                float interval = CurrentConfig.Interval;
                int lines = CurrentConfig.RowCount;

                if ((startpos <= stoppos && interval < 0) || (stoppos <= startpos && interval > 0))
                {
                    MessageBox.Show("触发方向与间隔不同向!请重新设置", "提示");
                    return;
                }

                if (CurrentConfig.SetStartPulse)
                {
                    if (stoppos > startpos && startpos < CurrentConfig.StartPulseValue)
                    {
                        MessageBox.Show("起始脉冲大于起始点!", "提示");
                        return;
                    }
                    if (stoppos < startpos && startpos > CurrentConfig.StartPulseValue)
                    {
                        MessageBox.Show("起始脉冲小于起始点!", "提示");
                        return;
                    }
                }

                if (CurrentConfig.ManualWidthEnabled)
                {
                    width = CurrentConfig.WidthValueForward > CurrentConfig.WidthValueBackward ? CurrentConfig.WidthValueForward : CurrentConfig.WidthValueBackward;
                    if (width <= 0)
                    {
                        MessageBox.Show("手动设置宽度的宽度值无效", "提示");
                        return;
                    }
                }
                else
                {
                    width = (int)((stoppos - startpos) / interval) + 1;
                }

                height = lines * _controller.ChannelCount;

                if (width == 0 || height == 0)
                {
                    MessageBox.Show("采样宽度或者采样高度设定存在0", "提示");
                    return;
                }

                SetExpectedDataSamplesCount(width);
                Console.WriteLine("开始测量");
                _controller.startMeasure();

                if (CurrentConfig.SetStartPulse)
                {
                    Console.WriteLine($"ChroCodile编码器触发命令: ENC {(int)CurrentConfig.ShaftNumber} {CurrentConfig.StartPulseValue}");
                    _controller.ExecStringCommand($"ENC {(int)CurrentConfig.ShaftNumber} {CurrentConfig.StartPulseValue}");
                }

                Console.WriteLine($"ChroCodile编码器触发参数: startpos={startpos}, stoppos={stoppos}, interval={interval}, lines={lines}, width={width}, height={height}, roundTrip={(CurrentConfig.IsRoundTripTrigger ? 1 : 0)}, shaft={(int)CurrentConfig.ShaftNumber}");
                Console.WriteLine($"ChroCodile编码器触发命令: ETR 1 {stoppos}");
                _controller.ExecStringCommand($"ETR 1 {stoppos}");
                Console.WriteLine($"ChroCodile编码器触发命令: ETR 2 {interval}");
                _controller.ExecStringCommand($"ETR 2 {interval}");
                Console.WriteLine("ChroCodile编码器触发命令: ETR 3 1");
                _controller.ExecStringCommand($"ETR 3 1");
                Console.WriteLine($"ChroCodile编码器触发命令: ETR 4 {(CurrentConfig.IsRoundTripTrigger ? 1 : 0)}");
                _controller.ExecStringCommand($"ETR 4 {(CurrentConfig.IsRoundTripTrigger ? 1 : 0)}");
                Console.WriteLine($"ChroCodile编码器触发命令: ETR 5 {(int)CurrentConfig.ShaftNumber}");
                _controller.ExecStringCommand($"ETR 5 {(int)CurrentConfig.ShaftNumber}");
                Console.WriteLine($"ChroCodile编码器触发命令: ETR 0 {startpos}");
                _controller.ExecStringCommand($"ETR 0 {startpos}");
                Console.WriteLine("ChroCodile编码器触发命令: TRE");
                _controller.ExecStringCommand("TRE");
            }
        }

        /// <summary>
        /// 停止触发
        /// </summary>
        public override void StopCollect()
        {
            _controller.stopMeasure();
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public override void Close()
        {
            _controller.CloseConnection();
        }

        /// <summary>
        /// 从回调填充的 _resultCache 取出并清空缓存，转换为通用测量数据。
        /// </summary>
        public override List<MeasureData> ReceiveSensorData()
        {
            List<DataSamples> snapshot;

            if (_expectedDataSamplesCount > 0)
            {
                bool completed = _collectCompletedEvent.Wait(COLLECT_WAIT_TIMEOUT_MS);
                if (!completed)
                {
                    lock (_resultCacheLock)
                    {
                        Console.WriteLine($"ChroCodile采集等待超时: expected={_expectedDataSamplesCount}, actual={_resultCache.Count}");
                    }
                }
            }

            lock (_resultCacheLock)
            {

                if (_resultCache.Count == 0)
                {
                    Console.WriteLine("普雷茨特传感器回调方法中没有拿到数据");
                    _expectedDataSamplesCount = 0;
                    _collectCompletedEvent.Reset();
                    return new List<MeasureData>();
                }
                else
                {
                    Console.WriteLine($"普雷茨特传感器回调方法中拿到数据，DataSamples数量为{_resultCache.Count}");
                }
                snapshot = _resultCache.Select(s => s.Clone()).ToList();    

                _resultCache.Clear();
                _expectedDataSamplesCount = 0;
                _collectCompletedEvent.Reset();
            }

            List<MeasureData> measureData = ConvertSnapshotToMeasureData(snapshot);
            return measureData;
        }
        #endregion

        #region CallBack
        /// <summary>
        /// 回调函数
        /// </summary>
        /// <param name="DataSamples"></param>
        public void ChroCodileEventCallbackHandle(List<DataSamples> DataSamples)
        {
            lock (_resultCacheLock)
            {
                if (DataSamples == null || DataSamples.Count == 0)
                {
                    Console.WriteLine("普雷茨特传感器中回调函数ChroCodileEventCallbackHandle拿到的集合数据为空");
                    return;
                }
                _resultCache.AddRange(DataSamples);
                if (_expectedDataSamplesCount > 0 && _resultCache.Count >= _expectedDataSamplesCount && !_collectCompletedEvent.IsSet)
                {
                    Console.WriteLine($"ChroCodile采集数据已达到目标: expected={_expectedDataSamplesCount}, actual={_resultCache.Count}");
                    _collectCompletedEvent.Set();
                }
            }
        }

        #endregion

        #region Methods
        /// <summary>
        /// 设置传感器的参数值
        /// </summary>
        /// <param name="ParamName">参数名</param>
        /// <param name="value">参数值</param>
        /// <param name="value">设置完参数值，有无返回值</param>
        public void SetParameters(string ParamName, object value,bool flag)
        {
            if (_controller.Status == ConnectStatus.Disconnected || _controller.Status == ConnectStatus.Idle)
            {
                return;
            }

            string rsp = "";
            string paramvalue = value.ToString();
            switch (ParamName)
            {
                case "输出信号":
                    rsp = _controller.ExecStringCommand($"SODX {paramvalue}"); //设置输出信号
                    break;
                case "采样频率":
                    rsp = _controller.ExecStringCommand($"SHZ {paramvalue}");
                    break;
                case "光强":
                    rsp = _controller.ExecStringCommand($"LAI {paramvalue}");
                    if (rsp == "")
                    {
                        MessageView.Ins.MessageBoxShow($"设置参数{ParamName}值异常，值为{paramvalue}", eMsgType.Info);
                        return;
                    }
                    break;
                case "阈值":
                    string mod = _controller.ExecStringCommand("MMD?");
                    Match matchd = Regex.Match(mod, "\\d+");
                    string cmd = "";
                    if (matchd.Value == "1")
                    {
                        cmd = "QTH";
                    }
                    else
                    {
                        cmd = "THR";
                    }
                    rsp = _controller.ExecStringCommand($"{cmd} {paramvalue}");
                    if (rsp == "")
                    {
                        MessageView.Ins.MessageBoxShow($"设置参数{ParamName}值异常，值为{paramvalue}", eMsgType.Info);
                        return;
                    }
                    break;
                case "外部触发":
                    _controller.ExecStringCommand("TRE");
                    break;
                case "连续采集":
                    _controller.ExecStringCommand("CTN");
                    break;
                default:
                    break;
            }

            if (rsp == "" && flag)
            {
                MessageView.Ins.MessageBoxShow($"设置参数{ParamName}值异常，值为{value}", eMsgType.Info);

            }
        }

        /// <summary>
        /// 获取传感器参数值
        /// </summary>
        /// <param name="Command"></param>
        /// <returns></returns>
        public string GetParameters(string Command)
        {
            string rsp = "";

            rsp = _controller.ExecStringCommand($"{Command}"); 
     
            return rsp;
        }

        private void SetExpectedDataSamplesCount(int width)
        {
            int outputSignalCount = GetOutputSignalCount();
            _expectedDataSamplesCount = width > 0 && outputSignalCount > 0 ? width * outputSignalCount : 0;
            Console.WriteLine($"ChroCodile采集目标DataSamples数量: width={width}, outputSignalCount={outputSignalCount}, expected={_expectedDataSamplesCount}");
        }

        private int GetOutputSignalCount()
        {
            if (string.IsNullOrWhiteSpace(CurrentConfig.OutputSignal))
                return 0;

            return CurrentConfig.OutputSignal
                .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Length;
        }


        private List<PrecitecControllerClassSync.DataSamples> GetValidSamples(List<PrecitecControllerClassSync.DataSamples> snapshot)
        {
            return snapshot
                .Where(s => s.data != null && s.data.Length > 0)
                .ToList();
        }

        private List<float[]> GetHeightRows(List<PrecitecControllerClassSync.DataSamples> validSamples)
        {
            return validSamples
                .Where(s => s.id == 256 || s.id == 16640)
                .Select(s => s.data.Select(x => x == 0 ? 888888.0f : -(float)x / 10.0f).ToArray())
                .ToList();
        }

        private List<float[]> GetGrayRows(List<PrecitecControllerClassSync.DataSamples> validSamples)
        {
            var graySamples = validSamples
                .Where(s => s.id == 257 || s.id == 16641)
                .ToList();

            if (graySamples.Count == 0)
                return new List<float[]>();

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            //ExportRawGrayRowsToCsv(graySamples, timestamp);

            double rawMin = graySamples.Min(s => s.data.Min());
            Console.WriteLine($"ChroCodile灰度数据最小值: {rawMin}");
            double min = rawMin < 0 ? 0 : rawMin;
            Console.WriteLine($"ChroCodile灰度数据映射最小值: {min}");
            double max = graySamples.Max(s => s.data.Max());
            Console.WriteLine($"ChroCodile灰度数据最大值: {max}");

            var mappedGrayRows = graySamples
                .Select(s => s.data.Select(x => MapGrayToByteRange(x, min, max)).ToArray())
                .ToList();

            //ExportMappedGrayRowsToCsv(mappedGrayRows, timestamp);

            return mappedGrayRows;
        }

        private static float MapGrayToByteRange(double value, double min, double max)
        {
            if (max <= min)
                return 0f;

            double mapped = (value - min) * 255.0 / (max - min);

            if (mapped < 0)
                return 177f;

            if (mapped > 255)
                return 255f;

            return (float)mapped;
        }

        private void ExportRawGrayRowsToCsv(List<PrecitecControllerClassSync.DataSamples> graySamples, string timestamp)
        {
            string directory = @"D:\ReeYin";
            System.IO.Directory.CreateDirectory(directory);

            string fileName = $"ChroCodile_GrayRaw_{timestamp}.csv";
            string path = System.IO.Path.Combine(directory, fileName);

            using (var writer = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(true)))
            {
                foreach (var sample in graySamples)
                {
                    writer.WriteLine(string.Join(",", sample.data.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture))));
                }
            }
        }

        private void ExportMappedGrayRowsToCsv(List<float[]> grayRows, string timestamp)
        {
            string directory = @"D:\ReeYin";
            System.IO.Directory.CreateDirectory(directory);

            string fileName = $"ChroCodile_GrayMapped_{timestamp}.csv";
            string path = System.IO.Path.Combine(directory, fileName);

            using (var writer = new System.IO.StreamWriter(path, false, new System.Text.UTF8Encoding(true)))
            {
                foreach (float[] row in grayRows)
                {
                    writer.WriteLine(string.Join(",", row.Select(x => x.ToString(System.Globalization.CultureInfo.InvariantCulture))));
                }
            }
        }

        /// <summary>
        /// 将传感器拿到的DataSamples对象转换为通用的MeasureData对象
        /// </summary>
        public List<MeasureData> ConvertSnapshotToMeasureData(List<PrecitecControllerClassSync.DataSamples> snapshot)
        {
            var result = new List<MeasureData>();
            if (snapshot == null || snapshot.Count == 0)
                return result;

            var validSamples = GetValidSamples(snapshot);
            if (validSamples.Count == 0)
                return result;


            var heightRows = GetHeightRows(validSamples);


            var grayRows = GetGrayRows(validSamples);
 

            if (heightRows.Count == 0 && grayRows.Count == 0)
                return result;

            int rowCount = Math.Max(heightRows.Count, grayRows.Count);
            for (int i = 0; i < rowCount; i++)
            {
                List<float[]> areaData = new List<float[]>(2);
                if (i < heightRows.Count)
                {
                    areaData.Add(heightRows[i]);
                }

                if (i < grayRows.Count)
                {
                    areaData.Add(grayRows[i]);
                }

                if (areaData.Count == 0)
                {
                    continue;
                }

                result.Add(new MeasureData
                {
                    AreaData = areaData
                });
            }

            return result;
        }
        #endregion

    }


    /// <summary>
    /// 普雷茨特传感器参数类
    /// </summary>
    public class ChroCodileConfig : BindableBase
    {
        /// <summary>
        /// 传感器型号
        /// </summary>
        [JsonIgnore]
        private PreciTecModel _sensorModel;

        public PreciTecModel SensorModel
        {
            get { return _sensorModel; }
            set { _sensorModel = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 输出信号(单通道一般输出值为83,65,256,257，多通道为83,16640,16641)
        /// </summary>
        private string _outputSignal;
        public string OutputSignal
        {
            get { return _outputSignal; }
            set { _outputSignal = value; RaisePropertyChanged(); }
        }

        /// <summary>采样频率</summary>
        private double _samplingFrequency;
        public double SamplingFrequency
        {
            get => _samplingFrequency;
            set { _samplingFrequency = value; RaisePropertyChanged(); }
        }

        /// <summary>光强</summary>
        private int _lightIntensity;
        public int LightIntensity
        {
            get => _lightIntensity;
            set { _lightIntensity = value; RaisePropertyChanged(); }
        }

        /// <summary>阈值</summary>
        private int _threshold;
        public int Threshold
        {
            get => _threshold;
            set { _threshold = value; RaisePropertyChanged(); }
        }

        #region 触发设定

        /// <summary>是否连续采集</summary>
        private bool _isContinuouTrigger = false;
        public bool IsContinuouTrigger
        {
            get => _isContinuouTrigger;
            set { _isContinuouTrigger = value; RaisePropertyChanged(); }
        }

        /// <summary>是否IO 触发</summary>
        private bool _isIOTrigger = false;
        public bool IsIOTrigger
        {
            get => _isIOTrigger;
            set
            {
                _isIOTrigger = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TriggerModeEnabled));
            }
        }

        /// <summary>是否编码器触发</summary>
        private bool _isEncoderTrigger = true;
        public bool IsEncoderTrigger
        {
            get => _isEncoderTrigger;
            set
            {
                _isEncoderTrigger = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(TriggerModeEnabled));
            }
        }

        /// <summary>是否启用外部触发模式。</summary>
        [JsonIgnore]
        public bool TriggerModeEnabled
        {
            get => IsIOTrigger || IsEncoderTrigger;
            set
            {
                if (value)
                {
                    IsContinuouTrigger = false;
                    if (!IsIOTrigger && !IsEncoderTrigger)
                    {
                        IsEncoderTrigger = true;
                    }
                }
                else
                {
                    IsIOTrigger = false;
                    IsEncoderTrigger = false;
                    IsContinuouTrigger = true;
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>采样宽度</summary>
        private int _samplingWidth;
        public int SamplingWidth
        {
            get => _samplingWidth;
            set { _samplingWidth = value; RaisePropertyChanged(); }
        }

        /// <summary>采样高度</summary>
        private int _samplingHeight;
        public int SamplingHeight
        {
            get => _samplingHeight;
            set { _samplingHeight = value; RaisePropertyChanged(); }
        }

        /// <summary>起始位置</summary>
        private int _startPosition;
        public int StartPosition
        {
            get => _startPosition;
            set { _startPosition = value; RaisePropertyChanged(); }
        }

        /// <summary>结束位置</summary>
        private int _endPosition;
        public int EndPosition
        {
            get => _endPosition;
            set { _endPosition = value; RaisePropertyChanged(); }
        }

        /// <summary>行数</summary>
        private int _rowCount;
        public int RowCount
        {
            get => _rowCount;
            set { _rowCount = value; RaisePropertyChanged(); }
        }

        /// <summary>间隔</summary>
        private float _interval;
        public float Interval
        {
            get => _interval;
            set { _interval = value; RaisePropertyChanged(); }
        }

        /// <summary>往返触发</summary>
        private bool _isRoundTripTrigger;
        public bool IsRoundTripTrigger
        {
            get => _isRoundTripTrigger;
            set { _isRoundTripTrigger = value; RaisePropertyChanged(); }
        }

        /// <summary>设置起始脉冲</summary>
        private bool _setStartPulse;
        public bool SetStartPulse
        {
            get => _setStartPulse;
            set { _setStartPulse = value; RaisePropertyChanged(); }
        }

        /// <summary>起始脉冲值</summary>
        private int _startPulseValue;
        public int StartPulseValue
        {
            get => _startPulseValue;
            set { _startPulseValue = value; RaisePropertyChanged(); }
        }

        /// <summary>手动设置宽度</summary>
        private bool _manualWidthEnabled;
        public bool ManualWidthEnabled
        {
            get => _manualWidthEnabled;
            set { _manualWidthEnabled = value; RaisePropertyChanged(); }
        }

        /// <summary>宽度值--></summary>
        private int _widthValueForward;
        public int WidthValueForward
        {
            get => _widthValueForward;
            set { _widthValueForward = value; RaisePropertyChanged(); }
        }

        /// <summary>宽度值<--</summary>
        private int _widthValueBackward;
        public int WidthValueBackward
        {
            get => _widthValueBackward;
            set { _widthValueBackward = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 编码器轴号
        /// </summary>
        private ShaftNumber _shaftNumber;
        public ShaftNumber ShaftNumber
        {
            get => _shaftNumber;
            set { _shaftNumber = value; RaisePropertyChanged(); }
        }
        #endregion
    }
}
