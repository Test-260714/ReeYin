using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReeYin.Hardware.Sensor.JingCe.API;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using System.IO;
using static ReeYin.Hardware.Sensor.JingCe.CustomUI.Defines.General;

namespace ReeYin.Hardware.Sensor.JingCe
{
    /// <summary>
    /// 武汉精测传感器实体类
    /// </summary>
    public class JingCeSensor : SensorBase
    {
        #region Fields
        private string deviceId = "";
        private readonly List<SurfaceData> _capturedSurfaces = new List<SurfaceData>();
        private readonly object _cacheLock = new object(); // 缓存锁
        private DealWithSurfaces? _surfacesCallback;
        [JsonIgnore]
        private JinceCMOSConfig _cmosConfig = new JinceCMOSConfig();
        private volatile bool _surfaceCollectionFinished;
        private int _receiveLayerCount;
        #endregion

        #region Properties
        public int ReceiveLayerCount
        {
            get { return _receiveLayerCount; }
            set { _receiveLayerCount = Math.Max(0, value); RaisePropertyChanged(); }
        }

        public JinceCMOSConfig CmosConfig
        {
            get { return _cmosConfig; }
            set { _cmosConfig = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private JinceTriggerConfig _cmosTriggerConfig = new JinceTriggerConfig();
        public JinceTriggerConfig CmosTriggerConfig
        {
            get { return _cmosTriggerConfig; }
            set { _cmosTriggerConfig = value; RaisePropertyChanged(); }
        }
        #endregion

        #region Override
        public override bool Init()
        {
            int connectResult = FocalSDKWrapper.ConnectByIP(IP, out string connectedDeviceId);
            if (connectResult != 0)
            {
                Console.WriteLine($"Failed to connect to device. Error code: {connectResult}");
                return false;
            }
            IsConnected = true;
            deviceId = connectedDeviceId;
            Console.WriteLine("Device connected successfully.");
            _surfacesCallback = OnSurfacesCaptured;
            return true;
        }

        /// <summary>
        /// 断开传感器连接，释放资源
        /// </summary>
        public override void Close()
        {
            State = HardwareState.Disconnecting;
            if (FocalSDKWrapper.IsDeviceOnline(deviceId))
            {
                FocalSDKWrapper.Disconnect(deviceId);
                FocalSDKWrapper.DeInitSDK();
            }
        }
        #endregion

        #region Constructor
        public JingCeSensor()
        {
            try
            {
                IP = "192.168.1.10";
                // 初始化 SDK
                int initResult = FocalSDKWrapper.InitSDK();
                if (initResult != 0)
                {
                    Console.WriteLine($"Failed to initialize JingCeSensor SDK. Error code: {initResult}");
                }
                Console.WriteLine("JingCeSensor SDK initialized successfully.");
            }
            catch( Exception ex) 
            {
                Logs.LogError($"武汉精测传感器初始化失败. 原因:{ex.Message}");
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 设置传感器参数
        /// </summary>
        /// <param name="Type"></param>
        /// <param name="ParamName"></param>
        /// <param name="value"></param>
        public bool SetSensorParameters(string Type, string MethodName, object value)
        {
            //检查是否连接设备
            if (!FocalSDKWrapper.IsDeviceOnline(deviceId))
            {
                Console.WriteLine("请先连接武汉精测传感器\r\n");
                return false;
            }

            switch (Type)
            {
                case "int":
                    {
                        int[] temp = value as int[];
                        if (temp == null || temp.Length < 1)
                        {
                            Console.WriteLine("武汉精测传感器设置值时，值为空\r\n");
                            return false;
                        }
                        int state = -1;
                        switch (MethodName)
                        {
                            case "SetFrequency":
                                {
                                    state = FocalSDKWrapper.SetFrequency(deviceId, temp[0]);
                                }
                                break;
                            case "SetLEDPulseWidth":
                                {
                                    state = FocalSDKWrapper.SetLEDPulseWidth(deviceId, temp[0]);
                                }
                                break;
                            case "SetPointDataFormat":
                                {
                                    state = FocalSDKWrapper.SetPointDataFormat(deviceId, temp[0]);
                                }
                                break;
                            case "SetSensorBinning":
                                {
                                    state = FocalSDKWrapper.SetSensorBinning(deviceId, temp[0]);
                                }
                                break;
                            case "SetWindowRange":
                                {
                                    state = FocalSDKWrapper.SetWindowRange(deviceId, temp[0], temp[1]);
                                }
                                break;
                            case "SetExposure":
                                {
                                    state = FocalSDKWrapper.SetExposure(deviceId, temp[0]);
                                }
                                break;
                            case "SetBias":
                                {
                                    state = FocalSDKWrapper.SetBias(deviceId, temp[0]);
                                }
                                break;
                            case "SetLeastGray":
                                {
                                    state = FocalSDKWrapper.SetLeastGray(deviceId, temp[0]);
                                }
                                break;
                            case "SetLeastNumber":
                                {
                                    state = FocalSDKWrapper.SetLeastNumber(deviceId, temp[0]);
                                }
                                break;
                            case "SetOuterTriggerType":
                                {
                                    state = FocalSDKWrapper.SetOuterTriggerType(deviceId, temp[0]);
                                }
                                break;
                            case "SetOuterTriggerFilter":
                                {
                                    state = FocalSDKWrapper.SetOuterTriggerFilter(deviceId, temp[0]);
                                }
                                break;
                            case "SetOuterTriggerHighlevelSwitch":
                                {
                                    state = FocalSDKWrapper.SetOuterTriggerHighlevelSwitch(deviceId, temp[0]);
                                }
                                break;
                            default:
                                break;

                        }
                        if (state != 0)
                        {
                            Console.WriteLine($"设置int类型数据({MethodName})失败! 错误码:" + state.ToString());
                            return false;
                        }
                        else
                        {
                            Console.WriteLine($"设置int类型数据({MethodName})成功");
                            return true;
                        }
                    }
                case "double":
                    {
                        double temp = Convert.ToDouble(value);
                        int state = -1;
                        switch (MethodName)
                        {
                            case "SetZRange":
                                {
                                    state = FocalSDKWrapper.SetZRange(deviceId, temp);
                                }
                                break;
                            case "SetProfileInvalidValue":
                                {
                                    state = FocalSDKWrapper.SetProfileInvalidValue(deviceId, temp);
                                }
                                break;
                            default:
                                break;

                        }
                        if (state != 0)
                        {
                            Console.WriteLine($"设置double类型数据({MethodName})失败! 错误码:" + state.ToString());
                            return false;
                        }
                        else
                        {
                            Console.WriteLine($"设置double类型数据({MethodName})成功");
                            return true;
                        }
                    }
                case "":
                    {
                        int state = -1;
                        switch (MethodName)
                        {
                            case "OpenTrigger":
                                {
                                    state = FocalSDKWrapper.OpenTrigger(deviceId);
                                }
                                break;
                            case "CloseTrigger":
                                {
                                    state = FocalSDKWrapper.CloseTrigger(deviceId);
                                }
                                break;
                            default:
                                break;

                        }
                        if (state != 0)
                        {
                            Console.WriteLine($"设置无参数数据({MethodName})失败! 错误码:" + state.ToString());
                            return false;
                        }
                        else
                        {
                            Console.WriteLine($"设置无参数数据({MethodName})成功");
                            return true;
                        }
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        /// <summary>
        /// 获取传感器参数
        /// </summary>
        public void GetSensorParameters()
        {
            //检查是否连接设备
            if (!FocalSDKWrapper.IsDeviceOnline(deviceId))
            {
                Console.WriteLine("请先连接武汉精测传感器\r\n");
                return;
            }

            FocalSDKWrapper.GetLEDPulseWidth(deviceId, out int ileduration);

            CmosConfig.ILedDuration = ileduration;

            FocalSDKWrapper.GetFrequency(deviceId, out int frequency);

            CmosConfig.Frequency = frequency;

            CmosConfig.DutyRatio = CmosConfig.ILedDuration * 100 * CmosConfig.Frequency / 1000000;

            FocalSDKWrapper.GetPointDataFormat(deviceId, out int pointdataformat);

            CmosConfig.PointDataForm = (PointDataFormat)pointdataformat;

            FocalSDKWrapper.GetZRange(deviceId, out double zrange);

            CmosConfig.ZWidthRange = zrange;

            FocalSDKWrapper.GetProfileInvalidValue(deviceId, out double profileInvalidValue);
            CmosConfig.ProfileInvalidValue = profileInvalidValue;

            FocalSDKWrapper.GetWindowRange(deviceId, out int zpixelrangemin, out int zpixelrangemax);
            CmosConfig.ZPixelRangeMin = zpixelrangemin;
            CmosConfig.ZPixelRangeMax = zpixelrangemax;

            FocalSDKWrapper.GetPeakSensorBinning(deviceId, out int binning);
            CmosConfig.Binning = (FrameRateMode)binning;

            FocalSDKWrapper.GetExposure(deviceId, out int exposure);
            CmosConfig.ExposureTime = exposure;

            FocalSDKWrapper.GetBias(deviceId, out int bias);
            CmosConfig.Bias = bias;

            FocalSDKWrapper.GetLeastGray(deviceId, out int leastgray);
            CmosConfig.LeastGray = leastgray;

            FocalSDKWrapper.GetLeastNumber(deviceId, out int leastnumber);
            CmosConfig.LeastNumber = leastnumber;

            FocalSDKWrapper.GetOuterTriggerType(deviceId, out int outertriggertype);
            CmosTriggerConfig.OutTriggerType = (OuterTriggerType)outertriggertype;

            FocalSDKWrapper.GetOuterTriggerFilter(deviceId, out int outertriggerfilter);
            CmosTriggerConfig.OuterTriggerFilter = outertriggerfilter;

            FocalSDKWrapper.GetOuterTriggerHighlevelSwitch(deviceId, out int outertriggerhighlevelswtich);
            CmosTriggerConfig.OuterTriggerHighlevelSwitch = (HighLevelSwitch)outertriggerhighlevelswtich;

        }

        /// <summary>
        /// 开始采集
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public override void StartCollect()
        {
            _surfaceCollectionFinished = false;
            State = HardwareState.Running;
            FocalSDKWrapper.OpenTrigger(deviceId);
            int grabResult = FocalSDKWrapper.GrabSurfaces(deviceId, CmosTriggerConfig.YAxisResolution, CmosTriggerConfig.CapturedFrames, _surfacesCallback);
            if (grabResult != 0)
            {
                throw new InvalidOperationException($"GrabSurfaces failed with code {grabResult}");
            }

        }

        /// <summary>
        /// 停止采集
        /// </summary>
        public override void StopCollect()
        {
            TryStopCollect();
        }

        #endregion
        /// <summary>
        /// 返回数据
        /// </summary>
        /// <returns></returns>
        public override List<MeasureData> ReceiveSensorData()
        {
            Tuple<List<List<float[]>>, List<List<float[]>>>? result = ConsumeCallbackData();

            List<MeasureData> ListMeasureData = new List<MeasureData>();

            if (result == null)
                return ListMeasureData;

            var heightLayers = result.Item1;
            var grayLayers = result.Item2;

            int layerCount = Math.Min(heightLayers.Count, grayLayers.Count);
            if (ReceiveLayerCount > 0)
                layerCount = Math.Min(layerCount, ReceiveLayerCount);

            if (layerCount == 0)
                return ListMeasureData;


            int rowCount = int.MaxValue;
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                rowCount = Math.Min(rowCount, Math.Min(heightLayers[layerIndex].Count, grayLayers[layerIndex].Count));
            }

            if (rowCount <= 0 || rowCount == int.MaxValue)
                return ListMeasureData;

            //try
            //{
            //    string exportDirectory = @"D:\JingCeData";
            //    Directory.CreateDirectory(exportDirectory);
            //    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

            //    for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            //    {
            //        WriteRowsToCsv(Path.Combine(exportDirectory, $"{timestamp}_Layer{layerIndex}_Height.csv"), heightLayers[layerIndex], rowCount);
            //        WriteRowsToCsv(Path.Combine(exportDirectory, $"{timestamp}_Layer{layerIndex}_Gray.csv"), grayLayers[layerIndex], rowCount);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Logs.LogError($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}：武汉精测CSV导出异常，{ex.Message}");
            //}

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                MeasureData md = new MeasureData();
                md.AreaData = new List<float[]>(layerCount * 2);

                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    md.AreaData.Add(heightLayers[layerIndex][rowIndex]);
                }

                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                {
                    md.AreaData.Add(grayLayers[layerIndex][rowIndex]);
                }

                ListMeasureData.Add(md);
            }

            return ListMeasureData;
        }


        private void WriteRowsToCsv(string path, List<float[]> rows, int rowCount)
        {
            using StreamWriter writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            int count = Math.Min(rowCount, rows.Count);

            for (int rowIndex = 0; rowIndex < count; rowIndex++)
            {
                float[] row = rows[rowIndex];
                for (int columnIndex = 0; columnIndex < row.Length; columnIndex++)
                {
                    if (columnIndex > 0)
                    {
                        writer.Write(',');
                    }

                    writer.Write(row[columnIndex].ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                writer.WriteLine();
            }
        }

        /// <summary>
        /// 数据回调方法
        /// </summary>
        /// <param name="deviceID">设备ID</param>
        /// <param name="surfacesData">Surface数据指针</param>
        /// <param name="surfacesCount">Surface数量</param>
        private void OnSurfacesCaptured(string deviceID, IntPtr surfacesData, ref int surfacesCount)
        {
            try
            {
                int count = surfacesCount;
                Console.WriteLine($"Received {count} surface(s) for device {deviceID}.");

                if (surfacesData == IntPtr.Zero || count <= 0)
                {
                    Console.WriteLine($"武汉精测传感器没有采集到数据");
                    return;
                }

                // 使用新的高效方法转换数据
                SurfaceData[] surfaces = FocalSDKWrapper.ConvertToSurfaceData(surfacesData, count);

                lock (_cacheLock)
                {
                    _capturedSurfaces.Clear();

                    for (int i = 0; i < surfaces.Length; i++)
                    {
                        var surface = surfaces[i];
                        Console.WriteLine($"Surface {i}: layer {surface.LayerID}, {surface.Width}x{surface.Height}, " +
                            $"xRes={surface.XResolution}, yRes={surface.YResolution}, invalidZ={surface.InvalidZValue}");

                        double[] zValues = surface.ZValues;
                        byte[] intensities = surface.Intensities;

                        if (intensities != null)
                        {
                            Console.WriteLine($"Intensities length:{intensities.Length}");
                        }

                        if (zValues != null)
                        {
                            Console.WriteLine($"Z sample:{zValues.Length}");
                        }

                        if (zValues != null && intensities != null)
                        {
                            _capturedSurfaces.Add(surface);
                        }
                    }

                    _surfaceCollectionFinished = _capturedSurfaces.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Logs.LogError($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：武汉精测数据回调方法异常，{ex.Message}");
            }
        }


        /// <summary>
        /// 异步处理和导出回调函数收集到的数据。
        /// </summary>
        public Tuple<List<List<float[]>>, List<List<float[]>>>? ConsumeCallbackData()
        {
            List<SurfaceData> surfacesToProcess = new();

            lock (_cacheLock)
            {
                if (_capturedSurfaces.Count == 0)
                {
                    Console.WriteLine("武汉精测生成数据失败，请检查采集是否开始\r\n");
                    return null;
                }
                surfacesToProcess.AddRange(_capturedSurfaces);
                _capturedSurfaces.Clear();
            }

            List<List<float[]>> heightLayers = new();
            List<List<float[]>> grayLayers = new();

            foreach (SurfaceData surface in surfacesToProcess)
            {
                int width = (int)surface.Width;
                int height = (int)surface.Height;
                int pointCount = width * height;

                double[] zValues = surface.ZValues;
                byte[] intensities = surface.Intensities;

                if (zValues == null || intensities == null ||
                    zValues.Length < pointCount || intensities.Length < pointCount)
                {
                    continue;
                }

                List<float[]> heightRows = new();
                List<float[]> grayRows = new();

                for (int y = 0; y < height; y++)
                {
                    float[] heightRow = new float[width];
                    float[] grayRow = new float[width];

                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        double rawZ = zValues[index];
                        float z = (float)rawZ;
                        heightRow[x] = z;
                        grayRow[x] = intensities[index];
                    }

                    heightRows.Add(heightRow);
                    grayRows.Add(grayRow);
                }

                heightLayers.Add(heightRows);
                grayLayers.Add(grayRows);

            }

            if (heightLayers.Count == 0 || grayLayers.Count == 0)
                return null;
            return Tuple.Create(heightLayers, grayLayers);
        }

        /// <summary>
        /// 判断传感器采集数据是否完成，完成则关闭触发器和停止采集
        /// </summary>
        /// <returns></returns>
        public bool TryStopCollect()
        {
            if (!_surfaceCollectionFinished)
            {
                Console.WriteLine("武汉精测传感器数据采集还没完成");
                return false;
            }

            FocalSDKWrapper.CloseTrigger(deviceId);
            FocalSDKWrapper.Stop(deviceId);
            State = HardwareState.Complete;
            return true;
        }
    }

    /// <summary>
    /// CMOS配置参数
    /// </summary>
    public class JinceCMOSConfig : BindableBase
    {
        [JsonIgnore]
        private int _frequency;
        /// <summary>
        /// 光源频率(HZ)
        /// </summary>
        public int Frequency
        {
            get { return _frequency; }
            set { _frequency = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 占空比(%)
        /// </summary>
        [JsonIgnore]
        private int _dutyRatio;
        public int DutyRatio
        {
            get { return _dutyRatio; }
            set { _dutyRatio = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 脉冲宽度(us)
        /// </summary>
        [JsonIgnore]
        private int _iLedDuration;

        public int ILedDuration
        {
            get { return _iLedDuration; }
            set { _iLedDuration = value; RaisePropertyChanged(); }
        }


        /// <summary>
        /// 数据点格式
        /// </summary>
        [JsonIgnore]
        private PointDataFormat _pointDataForm;

        public PointDataFormat PointDataForm
        {
            get { return _pointDataForm; }
            set { _pointDataForm = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 按宽度设置开窗范围(um)
        /// </summary>
        [JsonIgnore]
        private double _zWidthRange;

        public double ZWidthRange
        {
            get { return _zWidthRange; }
            set { _zWidthRange = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Profile默认无效值
        /// </summary>
        [JsonIgnore]
        private double _profileInvalidValue;

        public double ProfileInvalidValue
        {
            get { return _profileInvalidValue; }
            set { _profileInvalidValue = value; RaisePropertyChanged(); }
        }


        /// <summary>
        /// //开窗开始范围值
        /// </summary>
        [JsonIgnore]
        private double _zPixelRangeMin;

        public double ZPixelRangeMin
        {
            get { return _zPixelRangeMin; }
            set { _zPixelRangeMin = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 开窗结束范围值
        /// </summary>
        [JsonIgnore]
        private double _zPixelRangeMax;

        public double ZPixelRangeMax
        {
            get { return _zPixelRangeMax; }
            set { _zPixelRangeMax = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 设置Binning也叫桢速度模式
        /// </summary>
        [JsonIgnore]
        private FrameRateMode _binning;

        public FrameRateMode Binning
        {
            get { return _binning; }
            set { _binning = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 曝光时间(us)
        /// </summary>
        [JsonIgnore]
        private int _exposureTime;

        public int ExposureTime
        {
            get { return _exposureTime; }
            set { _exposureTime = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 本底阈值
        /// </summary>
        [JsonIgnore]
        private int _bias;

        public int Bias
        {
            get { return _bias; }
            set { _bias = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 最低灰度阈值
        /// </summary>
        [JsonIgnore]
        private int _leastGray;

        public int LeastGray
        {
            get { return _leastGray; }
            set { _leastGray = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 最低数量阈值
        /// </summary>
        [JsonIgnore]
        private int _leastNumber;

        public int LeastNumber
        {
            get { return _leastNumber; }
            set { _leastNumber = value; RaisePropertyChanged(); }
        }


    }

    /// <summary>
    /// 触发设定配置参数
    /// </summary>
    public class JinceTriggerConfig : BindableBase
    {
        /// <summary>
        /// 外触发类型
        /// </summary>
        [JsonIgnore]
        private OuterTriggerType _outTriggerType;
        public OuterTriggerType OutTriggerType
        {
            get { return _outTriggerType; }
            set { _outTriggerType = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 外触发过滤值
        /// </summary>
        [JsonIgnore]
        private int _outerTriggerFilter;
        public int OuterTriggerFilter
        {
            get { return _outerTriggerFilter; }
            set { _outerTriggerFilter = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 高电平外触发响应状态
        /// </summary>
        [JsonIgnore]
        private HighLevelSwitch _outerTriggerHighlevelSwitch;
        public HighLevelSwitch OuterTriggerHighlevelSwitch
        {
            get { return _outerTriggerHighlevelSwitch; }
            set { _outerTriggerHighlevelSwitch = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 外触发开关
        /// </summary>
        [JsonIgnore]
        private ExternalTriggerSwitch _externalTriggerSwitch;
        public ExternalTriggerSwitch ExternalTriggerSwitch
        {
            get { return _externalTriggerSwitch; }
            set { _externalTriggerSwitch = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 需要采集的帧数(Y方向)
        /// </summary>
        [JsonIgnore]
        private uint _capturedFrames;
        public uint CapturedFrames
        {
            get { return _capturedFrames; }
            set { _capturedFrames = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// Y轴分辨率
        /// </summary>
        [JsonIgnore]
        private uint _yaxisresolution;
        public uint YAxisResolution
        {
            get { return _yaxisresolution; }
            set { _yaxisresolution = value; RaisePropertyChanged(); }
        }
    }
}
