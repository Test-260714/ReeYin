using MPSizectorS_DotNet;
using ReeYin.Hardware.Sensor.MEGAPHASE.Models;
using ReeYin.Hardware.Sensor.Models;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using System.ComponentModel;
using System.Windows;

namespace ReeYin.Hardware.Sensor.MEGAPHASE
{
    public class MegAphaseSensor : SensorBase
    {
        #region Fields
        private readonly MPSizectorS _sensor;
        private readonly MPSizectorS.DataCallBackType _dataCallBack;
        private readonly object _dataLock = new object();
        private readonly List<MeasureData> _pendingMeasureData = new List<MeasureData>();
        private TaskCompletionSource<bool>? _collectOnceTaskSource;
        private bool _isRefreshingSettings;
        private readonly object _softwarePreprocessLock = new object();

        #endregion

        #region Constructor

        /// <summary>
        /// 创建MEGAPHASE传感器实例，并注册SDK数据回调
        /// </summary>
        public MegAphaseSensor()
        {
            VenderName = "MEGAPHASE";
            VenderType = "MEGAPHASE";
            _sensor = new MPSizectorS(LogMediaType.Console, 0, true);
            _dataCallBack = DataHandlerz;
            _sensor.SetDataCallBack(_dataCallBack);
            Settings.PropertyChanged += Settings_PropertyChanged;
        }
        #endregion

        #region Properties
        public MegAphaseSensorSettings Settings { get; } = new MegAphaseSensorSettings();
        #endregion

        #region Methods

        /// <summary>
        /// 刷新设备列表并尝试连接第一个可用的传感器
        /// </summary>
        /// <returns>连接并完成基础校验返回true，否则返回false</returns>
        public override bool Init()
        {
            State = HardwareState.Initializing;
            IsConnected = false;

            try
            {
                _sensor.UpdateDeviceList();

                int deviceCount = _sensor.GetDeviceCount();
                if (deviceCount <= 0)
                {
                    State = HardwareState.NotConnected;
                    return false;
                }

                DeviceInfoStructType[] deviceList = new DeviceInfoStructType[deviceCount];
                for (int i = 0; i < deviceCount; i++)
                {
                    deviceList[i] = _sensor.GetDeviceInfo(i);
                }

                State = HardwareState.Connecting;
                for (int i = 0; i < deviceCount; i++)
                {
                    _sensor.SetAutoReconnect(true);
                    if (_sensor.Open(deviceList[i]))
                    {
                        int count = 0;
                        while ((_sensor.DeviceState == DeviceStateType.Disconnected) && (count++ < 20))
                        {
                            Thread.Sleep(50);
                        }

                        if (_sensor.CurrentDeviceInfo.MinimumSDKVersion > MPSizectorS.GetSDKVersion())
                        {
                            MessageBox.Show("Device required minimum SDK version is higher than current SDK version. Please use new version of SDK.");
                            _sensor.Close();
                            State = HardwareState.Error;
                            return false;
                        }
                        IsConnected = true;
                        State = HardwareState.Ready;
                        RefreshSettingsFromDevice();
                        return true;
                    }
                }

                State = HardwareState.NotConnected;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE Init failed: {ex.Message}");
                IsConnected = false;
                State = HardwareState.Error;
                return false;
            }
        }

        /// <summary>
        /// 从设备读取当前参数，并刷新设置页面绑定值
        /// </summary>
        public void RefreshSettingsFromDevice()
        {
            if (!IsConnected)
            {
                return;
            }

            try
            {
                SettingsStructType settings = _sensor.Settings;
                _isRefreshingSettings = true;

                Settings.WorkingMode = settings.BasicSetings.WorkingMode;
                Settings.HoldState = settings.BasicSetings.HoldState;
                Settings.BinningState = settings.BasicSetings.BinningState;
                Settings.ROIX0Ratio = settings.BasicSetings.ROIX0Ratio;
                Settings.ROIY0Ratio = settings.BasicSetings.ROIY0Ratio;
                Settings.ROIWidthRatio = settings.BasicSetings.ROIWidthRatio;
                Settings.ROIHeightRatio = settings.BasicSetings.ROIHeightRatio;
                Settings.TriggerSource = settings.BasicSetings.TriggerSource;

                Settings.ProjectionMode = settings.MultiHeadSettings.ProjectionMode;
                Settings.MergeParam0 = settings.MultiHeadSettings.MergeParam0;
                Settings.MergeParam1 = settings.MultiHeadSettings.MergeParam1;
                Settings.MergeParam2 = settings.MultiHeadSettings.MergeParam2;
                Settings.MergeParam3 = _sensor.MergeParam3;
                Settings.MergeThreshold = settings.MultiHeadSettings.MergeThreshold;
                Settings.MergeNumber = _sensor.MergeNumber;

                ExposureBasicSettingStructType exposureBasicSetting = settings.ExposureSettings.ExposureBasicSetting;
                Settings.ExposureMode = exposureBasicSetting.ExposureMode;
                Settings.ExposureNumber = exposureBasicSetting.ExposureNumber;
                Settings.AutoHDRPriority = exposureBasicSetting.AutoHDRPriority;
                Settings.AutoPHDRQuality = exposureBasicSetting.AutoPHDRQuality;
                Settings.UserGain = settings.ExposureSettings.UserGain;
                Settings.ExposureIntensity3D_1st = settings.ExposureSettings.ExposureIntensity3D_1st;
                Settings.ExposureIntensity3D_2nd = settings.ExposureSettings.ExposureIntensity3D_2nd;
                Settings.ExposureIntensity3D_3rd = settings.ExposureSettings.ExposureIntensity3D_3rd;
                Settings.ExposureIntensity2D = settings.ExposureSettings.ExposureIntensity2D;
                Settings.MultiHeadExpoVarMode = settings.ExposureSettings.MultiHeadExpoVarMode;
                Settings.MultiHeadExpoVarRatio = settings.ExposureSettings.MultiHeadExpoVarRatio;

                Settings.OverExposureFilterThreshold = settings.ReconstructionSettings.OverExposureFilterThreshold;
                Settings.ValidPointThreshold0 = settings.ReconstructionSettings.ValidPointThreshold0;
                Settings.ValidPointThreshold1 = settings.ReconstructionSettings.ValidPointThreshold1;
                Settings.BurrFilterThreshold0 = settings.ReconstructionSettings.BurrFilterThreshold0;
                Settings.BurrFilterThreshold1 = settings.ReconstructionSettings.BurrFilterThreshold1;
                Settings.PreProcessLoopNum = settings.ReconstructionSettings.PreProcessLoopNum;
                Settings.PreProcessThreshold = settings.ReconstructionSettings.PreProcessThreshold;

                Settings.DataOutMode = settings.PostProcessSettings.DataOutMode;
                Settings.UserRTMatrixEnableState = settings.PostProcessSettings.UserRTMatrixEnableState;
                Settings.RangeCheckEnableState = settings.PostProcessSettings.RangeCheckEnableState;

                PointScaleSettingStructType pointScaleSetting = settings.PostProcessSettings.PointScaleSetting;
                Settings.PointScaleX0Pos = pointScaleSetting.X0Pos;
                Settings.PointScaleXIncrement = pointScaleSetting.XIncrement;
                Settings.PointScaleY0Pos = pointScaleSetting.Y0Pos;
                Settings.PointScaleYIncrement = pointScaleSetting.YIncrement;
                Settings.PointScaleZ0Pos = pointScaleSetting.Z0Pos;
                Settings.PointScaleZIncrement = pointScaleSetting.ZIncrement;

                RangeCheckSettingStructType rangeCheckSetting = settings.PostProcessSettings.RangeCheckSetting;
                Settings.RangeUXmin = rangeCheckSetting.UXmin;
                Settings.RangeUXmax = rangeCheckSetting.UXmax;
                Settings.RangeUYmin = rangeCheckSetting.UYmin;
                Settings.RangeUYmax = rangeCheckSetting.UYmax;
                Settings.RangeUZmin = rangeCheckSetting.UZmin;
                Settings.RangeUZmax = rangeCheckSetting.UZmax;

                RTMatrixStructType userRTMatrix = settings.PostProcessSettings.UserRTMatrix;
                Settings.UserRTMatrixR00 = userRTMatrix.R00;
                Settings.UserRTMatrixR01 = userRTMatrix.R01;
                Settings.UserRTMatrixR02 = userRTMatrix.R02;
                Settings.UserRTMatrixR10 = userRTMatrix.R10;
                Settings.UserRTMatrixR11 = userRTMatrix.R11;
                Settings.UserRTMatrixR12 = userRTMatrix.R12;
                Settings.UserRTMatrixR20 = userRTMatrix.R20;
                Settings.UserRTMatrixR21 = userRTMatrix.R21;
                Settings.UserRTMatrixR22 = userRTMatrix.R22;
                Settings.UserRTMatrixT0 = userRTMatrix.T0;
                Settings.UserRTMatrixT1 = userRTMatrix.T1;
                Settings.UserRTMatrixT2 = userRTMatrix.T2;

                RefreshSoftwarePreprocessSettingsFromDevice();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE refresh settings failed: {ex.Message}");
            }
            finally
            {
                _isRefreshingSettings = false;
            }
        }

        /// <summary>
        /// 关闭当前连接，并清空尚未取走的缓存数据
        /// </summary>
        public override void Close()
        {
            State = HardwareState.Disconnecting;

            try
            {
                _sensor.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE Close failed: {ex.Message}");
            }

            IsConnected = false;
            State = HardwareState.Closed;
        }

        /// <summary>
        /// 开始采集
        /// </summary>
        public override void StartCollect()
        {
            if (!IsConnected)
            {
                State = HardwareState.NotConnected;
                return;
            }

            try
            {
                State = HardwareState.Running;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE StartCollect failed: {ex.Message}");
                State = HardwareState.Error;
            }
        }

        /// <summary>
        /// 采集一次
        /// </summary>
        public void CollectOnce()
        {
            if (!IsConnected)
            {
                State = HardwareState.NotConnected;
                return;
            }

            try
            {
                _sensor.FireSoftwareTrigger();
                State = HardwareState.Running;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE CollectOnce failed: {ex.Message}");
                State = HardwareState.Error;
            }
        }

        /// <summary>
        /// 采集一次，并等待SDK回调返回有效数据
        /// </summary>
        /// <param name="timeoutMilliseconds">等待回调的超时时间</param>
        public async Task CollectOnceAsync(int timeoutMilliseconds = 3000)
        {
            if (!IsConnected)
            {
                State = HardwareState.NotConnected;
                return;
            }

            TaskCompletionSource<bool> taskSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
  
            _collectOnceTaskSource = taskSource;
           

            try
            {
                _sensor.FireSoftwareTrigger();
                State = HardwareState.Running;
            }
            catch (Exception ex)
            {
                lock (_dataLock)
                {
                    if (ReferenceEquals(_collectOnceTaskSource, taskSource))
                    {
                        _collectOnceTaskSource = null;
                    }
                }

                Console.WriteLine($"MEGAPHASE CollectOnce failed: {ex.Message}");
                State = HardwareState.Error;
                return;
            }

            Task completedTask = await Task.WhenAny(taskSource.Task, Task.Delay(timeoutMilliseconds));
            if (!ReferenceEquals(completedTask, taskSource.Task))
            {
                lock (_dataLock)
                {
                    if (ReferenceEquals(_collectOnceTaskSource, taskSource))
                    {
                        _collectOnceTaskSource = null;
                    }
                }
            }
        }

        /// <summary>
        /// 将设备切回Hold状态，暂停响应后续触发
        /// </summary>
        public override void StopCollect()
        {
            if (!IsConnected)
            {
                State = HardwareState.NotConnected;
                return;
            }

            try
            {
                State = HardwareState.Complete;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE StopCollect failed: {ex.Message}");
                State = HardwareState.Error;
            }
        }

        /// <summary>
        /// 取出回调线程缓存的测量数据，并在返回后清空缓存
        /// </summary>
        /// <returns>当前缓存中的公共测量数据列表</returns>
        public override List<MeasureData> ReceiveSensorData()
        {
            lock (_dataLock)
            {
                if (_pendingMeasureData.Count == 0)
                {
                    return new List<MeasureData>();
                }

                List<MeasureData> result = new List<MeasureData>(_pendingMeasureData);
                _pendingMeasureData.Clear();
                return result;
            }
        }

        /// <summary>
        /// SDK数据回调，负责把收到的一帧数据转换并缓存为公共MeasureData
        /// </summary>
        /// <param name="dataFormat">当前回调返回的数据类型</param>
        /// <param name="dataFrame">当前回调返回的原始数据帧</param>
        private void DataHandlerz(DataFormatType dataFormat, UnmanagedDataFrameUndefinedStruct dataFrame)
        {
            try
            {
                List<MeasureData> measureData = ConvertFrameToMeasureData(dataFormat, dataFrame);
                if (measureData.Count == 0)
                {
                    return;
                }

                TaskCompletionSource<bool>? collectOnceTaskSource;
                lock (_dataLock)
                {
                    _pendingMeasureData.Clear();
                    _pendingMeasureData.AddRange(measureData);
                    collectOnceTaskSource = _collectOnceTaskSource;
                    _collectOnceTaskSource = null;
                }

                collectOnceTaskSource?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE callback failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 按回调数据格式分发到对应的2D/3D转换逻辑
        /// </summary>
        /// <param name="dataFormat">回调返回的数据格式</param>
        /// <param name="dataFrame">原始未托管数据帧</param>
        /// <returns>统一转换后的公共测量数据</returns>
        private static List<MeasureData> ConvertFrameToMeasureData(DataFormatType dataFormat, UnmanagedDataFrameUndefinedStruct dataFrame)
        {
            switch (dataFormat)
            {
                case DataFormatType.Image2D:
                    return Convert2DFrameToMeasureData(dataFrame.ToManaged2D());
                case DataFormatType.FixZMap:
                    return ConvertFixZMapFrameToMeasureData(dataFrame.ToManagedFixZMap());
                case DataFormatType.FixZMapSimple:
                    return ConvertFixZMapSimpleFrameToMeasureData(dataFrame.ToManagedFixZMapSimple());
                case DataFormatType.FixPointCloud:
                    return ConvertFix3DFrameToMeasureData(dataFrame.ToManagedFix3D());
                case DataFormatType.FloatPointCloud:
                    return ConvertFloat3DFrameToMeasureData(dataFrame.ToManagedFloat3D());
                default:
                    return new List<MeasureData>();
            }
        }

        /// <summary>
        /// 将2D图像帧转换成公共MeasureData
        /// </summary>
        /// <param name="frame">托管2D图像帧</param>
        /// <returns>按行拆分后的测量数据</returns>
        private static List<MeasureData> Convert2DFrameToMeasureData(ManagedDataFrame2DStruct frame)
        {
            return Build2DMeasureData(frame.Data, frame.FrameInfo);
        }

        /// <summary>
        /// 将简单ZMap帧转换成公共MeasureData
        /// </summary>
        /// <param name="frame">托管FixZMapSimple帧</param>
        /// <returns>包含Z和灰度通道的测量数据</returns>
        private static List<MeasureData> ConvertFixZMapSimpleFrameToMeasureData(ManagedDataFrameFixZMapSimpleStruct frame)
        {
            return BuildZGrayMeasureData(frame.Z, frame.Data, frame.FrameInfo);
        }

        /// <summary>
        /// 将FixZMap帧转换成公共MeasureData
        /// </summary>
        /// <param name="frame">托管FixZMap帧</param>
        /// <returns>包含Z和灰度通道的测量数据</returns>
        private static List<MeasureData> ConvertFixZMapFrameToMeasureData(ManagedDataFrameFixZMapStruct frame)
        {
            ResolveFrameSize(frame.FrameInfo, frame.Data?.Length ?? 0, out int width, out int height);
            if (width <= 0 || height <= 0 || frame.Data == null)
            {
                return new List<MeasureData>();
            }

            List<MeasureData> result = new List<MeasureData>(height);
            for (int y = 0; y < height; y++)
            {
                float[] zRow = new float[width];
                float[] grayRow = new float[width];

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= frame.Data.Length)
                    {
                        break;
                    }

                    DataPointFixZMapStruct point = frame.Data[index];
                    zRow[x] = point.IsValid ? point.Z : float.NaN;
                    grayRow[x] = point.Gray;
                }

                result.Add(CreateAreaMeasureData(zRow, grayRow));
            }

            return result;
        }

        /// <summary>
        /// 将定点3D点云帧转换成公共MeasureData
        /// </summary>
        /// <param name="frame">托管Fix3D帧</param>
        /// <returns>包含Z、Gray两个通道的测量数据</returns>
        private static List<MeasureData> ConvertFix3DFrameToMeasureData(ManagedDataFrameFix3DStruct frame)
        {
            ResolveFrameSize(frame.FrameInfo, frame.Data?.Length ?? 0, out int width, out int height);
            if (width <= 0 || height <= 0 || frame.Data == null)
            {
                return new List<MeasureData>();
            }

            List<MeasureData> result = new List<MeasureData>(height);
            for (int y = 0; y < height; y++)
            {
                float[] zRow = new float[width];
                float[] grayRow = new float[width];

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= frame.Data.Length)
                    {
                        break;
                    }

                    DataPointFix3DStruct point = frame.Data[index];
                    zRow[x] = point.IsValid ? point.Z : float.NaN;
                    grayRow[x] = point.Gray;
                }

                result.Add(CreateAreaMeasureData(zRow, grayRow));
            }

            return result;
        }

        /// <summary>
        /// 将浮点3D点云帧转换成公共MeasureData
        /// </summary>
        /// <param name="frame">托管Float3D帧</param>
        /// <returns>包含Z、Gray两个通道的测量数据</returns>
        private static List<MeasureData> ConvertFloat3DFrameToMeasureData(ManagedDataFrameFloat3DStruct frame)
        {
            ResolveFrameSize(frame.FrameInfo, frame.Data?.Length ?? 0, out int width, out int height);
            if (width <= 0 || height <= 0 || frame.Data == null)
            {
                return new List<MeasureData>();
            }

            List<MeasureData> result = new List<MeasureData>(height);
            for (int y = 0; y < height; y++)
            {
                float[] zRow = new float[width];
                float[] grayRow = new float[width];

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= frame.Data.Length)
                    {
                        break;
                    }

                    DataPointFloat3DStruct point = frame.Data[index];
                    zRow[x] = point.IsValid ? point.Z : float.NaN;
                    grayRow[x] = point.Gray;
                }

                result.Add(CreateAreaMeasureData(zRow, grayRow));
            }

            return result;
        }

        /// <summary>
        /// 将2D原始灰度数组按帧宽高拆成公共MeasureData
        /// </summary>
        /// <param name="data">原始灰度数组</param>
        /// <param name="frameInfo">当前帧头信息</param>
        /// <returns>按行拆分后的测量数据</returns>
        private static List<MeasureData> Build2DMeasureData(byte[] data, HeadPackStructType frameInfo)
        {
            ResolveFrameSize(frameInfo, data?.Length ?? 0, out int width, out int height);
            if (width <= 0 || height <= 0 || data == null)
            {
                return new List<MeasureData>();
            }

            List<MeasureData> result = new List<MeasureData>(height);
            for (int y = 0; y < height; y++)
            {
                float[] grayRow = new float[width];
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index >= data.Length)
                    {
                        break;
                    }

                    grayRow[x] = data[index];
                }

                result.Add(CreateAreaMeasureData(grayRow));
            }

            return result;
        }

        /// <summary>
        /// 将Z数据和灰度数据按帧宽高组合成公共MeasureData
        /// </summary>
        /// <param name="zData">高度通道数据</param>
        /// <param name="grayData">灰度通道数据</param>
        /// <param name="frameInfo">当前帧头信息</param>
        /// <returns>包含Z和Gray两个通道的测量数据</returns>
        private static List<MeasureData> BuildZGrayMeasureData(ushort[] zData, byte[] grayData, HeadPackStructType frameInfo)
        {
            int maxLength = Math.Max(zData?.Length ?? 0, grayData?.Length ?? 0);
            ResolveFrameSize(frameInfo, maxLength, out int width, out int height);
            if (width <= 0 || height <= 0)
            {
                return new List<MeasureData>();
            }

            List<MeasureData> result = new List<MeasureData>(height);
            for (int y = 0; y < height; y++)
            {
                float[] zRow = new float[width];
                float[] grayRow = new float[width];

                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    zRow[x] = (zData != null && index < zData.Length) ? zData[index] : float.NaN;
                    grayRow[x] = (grayData != null && index < grayData.Length) ? grayData[index] : 0f;
                }

                result.Add(CreateAreaMeasureData(zRow, grayRow));
            }

            return result;
        }

        /// <summary>
        /// 创建一条面阵测量数据，并把传入通道直接写入AreaData
        /// </summary>
        /// <param name="channels">当前行的一个或多个通道数据</param>
        /// <returns>封装后的公共MeasureData</returns>
        private static MeasureData CreateAreaMeasureData(params float[][] channels)
        {
            return new MeasureData
            {
                AreaData = new List<float[]>(channels),
                RTime = DateTime.Now
            };
        }

        /// <summary>
        /// 根据帧头和数据长度推导出当前帧的宽高
        /// </summary>
        /// <param name="frameInfo">当前帧头信息</param>
        /// <param name="dataLength">当前数据长度</param>
        /// <param name="width">解析出的宽度</param>
        /// <param name="height">解析出的高度</param>
        private static void ResolveFrameSize(HeadPackStructType frameInfo, int dataLength, out int width, out int height)
        {
            width = frameInfo.DataInfo.XPixResolution;
            height = frameInfo.DataInfo.YPixResolution;

            if (width <= 0)
            {
                width = (int)frameInfo.DeviceInfo.SensorWidth;
            }

            if (height <= 0)
            {
                height = (int)frameInfo.DeviceInfo.SensorHeight;
            }

            if (width > 0 && height <= 0 && dataLength > 0)
            {
                height = (int)Math.Ceiling((double)dataLength / width);
            }
            else if (height > 0 && width <= 0 && dataLength > 0)
            {
                width = (int)Math.Ceiling((double)dataLength / height);
            }
            else if (width <= 0 && height <= 0 && dataLength > 0)
            {
                width = dataLength;
                height = 1;
            }
        }

        /// <summary>
        /// 监听设置参数变化并写入SDK
        /// </summary>
        private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isRefreshingSettings || !IsConnected || string.IsNullOrEmpty(e.PropertyName))
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(MegAphaseSensorSettings.SoftwarePreprocessEnabled):
                case nameof(MegAphaseSensorSettings.RemoveBurrsMode):
                case nameof(MegAphaseSensorSettings.RemoveBurrsWinSize):
                case nameof(MegAphaseSensorSettings.RemoveBurrsWinSize2):
                case nameof(MegAphaseSensorSettings.RemoveBurrsSlopeLevel):
                case nameof(MegAphaseSensorSettings.RemoveBurrsNeighborCloseLevel):
                case nameof(MegAphaseSensorSettings.RemoveBurrsNeighborNumLevel):
                case nameof(MegAphaseSensorSettings.RemoveBurrsEdgeSuppressLevel):
                case nameof(MegAphaseSensorSettings.MendMode):
                case nameof(MegAphaseSensorSettings.MendWinSize):
                case nameof(MegAphaseSensorSettings.MendWinSize2):
                case nameof(MegAphaseSensorSettings.MendMethod):
                case nameof(MegAphaseSensorSettings.FiltrateMode):
                case nameof(MegAphaseSensorSettings.FiltrateWinSize):
                case nameof(MegAphaseSensorSettings.FiltrateNeighborCloseLevel):
                case nameof(MegAphaseSensorSettings.FiltrateNeighborNumLevel):
                case nameof(MegAphaseSensorSettings.FiltrateMendPointOnly):
                    ApplySoftwarePreprocessSettings();
                    break;
                case nameof(MegAphaseSensorSettings.WorkingMode):
                    SetSensorValue(() => _sensor.WorkingMode = Settings.WorkingMode);
                    break;
                case nameof(MegAphaseSensorSettings.HoldState):
                    SetSensorValue(() => _sensor.HoldState = Settings.HoldState);
                    break;
                case nameof(MegAphaseSensorSettings.BinningState):
                    SetSensorValue(() => _sensor.BinningState = Settings.BinningState);
                    break;
                case nameof(MegAphaseSensorSettings.ROIX0Ratio):
                    SetSensorValue(() => _sensor.ROIX0Ratio = Settings.ROIX0Ratio);
                    break;
                case nameof(MegAphaseSensorSettings.ROIY0Ratio):
                    SetSensorValue(() => _sensor.ROIY0Ratio = Settings.ROIY0Ratio);
                    break;
                case nameof(MegAphaseSensorSettings.ROIWidthRatio):
                    SetSensorValue(() => _sensor.ROIWidthRatio = Settings.ROIWidthRatio);
                    break;
                case nameof(MegAphaseSensorSettings.ROIHeightRatio):
                    SetSensorValue(() => _sensor.ROIHeightRatio = Settings.ROIHeightRatio);
                    break;
                case nameof(MegAphaseSensorSettings.TriggerSource):
                    SetSensorValue(() => _sensor.TriggerSource = Settings.TriggerSource);
                    break;
                case nameof(MegAphaseSensorSettings.ProjectionMode):
                    SetSensorValue(() => _sensor.ProjectionMode = Settings.ProjectionMode);
                    break;
                case nameof(MegAphaseSensorSettings.MergeParam0):
                    SetSensorValue(() => _sensor.MergeParam0 = Settings.MergeParam0);
                    break;
                case nameof(MegAphaseSensorSettings.MergeParam1):
                    SetSensorValue(() => _sensor.MergeParam1 = Settings.MergeParam1);
                    break;
                case nameof(MegAphaseSensorSettings.MergeParam2):
                    SetSensorValue(() => _sensor.MergeParam2 = Settings.MergeParam2);
                    break;
                case nameof(MegAphaseSensorSettings.MergeParam3):
                    SetSensorValue(() => _sensor.MergeParam3 = Settings.MergeParam3);
                    break;
                case nameof(MegAphaseSensorSettings.MergeThreshold):
                    SetSensorValue(() => _sensor.MergeThreshold = Settings.MergeThreshold);
                    break;
                case nameof(MegAphaseSensorSettings.MergeNumber):
                    SetSensorValue(() => _sensor.MergeNumber = Settings.MergeNumber);
                    break;
                case nameof(MegAphaseSensorSettings.ExposureMode):
                case nameof(MegAphaseSensorSettings.ExposureNumber):
                case nameof(MegAphaseSensorSettings.AutoHDRPriority):
                case nameof(MegAphaseSensorSettings.AutoPHDRQuality):
                    SetSensorValue(() => _sensor.ExposureBasicSetting = Settings.GetExposureBasicSetting());
                    break;
                case nameof(MegAphaseSensorSettings.UserGain):
                    SetSensorValue(() => _sensor.UserGain = Settings.UserGain);
                    break;
                case nameof(MegAphaseSensorSettings.ExposureIntensity3D_1st):
                    SetSensorValue(() => _sensor.ExposureIntensity3D_1st = Settings.ExposureIntensity3D_1st);
                    break;
                case nameof(MegAphaseSensorSettings.ExposureIntensity3D_2nd):
                    SetSensorValue(() => _sensor.ExposureIntensity3D_2nd = Settings.ExposureIntensity3D_2nd);
                    break;
                case nameof(MegAphaseSensorSettings.ExposureIntensity3D_3rd):
                    SetSensorValue(() => _sensor.ExposureIntensity3D_3rd = Settings.ExposureIntensity3D_3rd);
                    break;
                case nameof(MegAphaseSensorSettings.ExposureIntensity2D):
                    SetSensorValue(() => _sensor.ExposureIntensity2D = Settings.ExposureIntensity2D);
                    break;
                case nameof(MegAphaseSensorSettings.MultiHeadExpoVarMode):
                    SetSensorValue(() => _sensor.MultiHeadExpoVarMode = Settings.MultiHeadExpoVarMode);
                    break;
                case nameof(MegAphaseSensorSettings.MultiHeadExpoVarRatio):
                    SetSensorValue(() => _sensor.MultiHeadExpoVarRatio = Settings.MultiHeadExpoVarRatio);
                    break;
                case nameof(MegAphaseSensorSettings.OverExposureFilterThreshold):
                    SetSensorValue(() => _sensor.OverExposureFilterThreshold = Settings.OverExposureFilterThreshold);
                    break;
                case nameof(MegAphaseSensorSettings.ValidPointThreshold0):
                    SetSensorValue(() => _sensor.ValidPointThreshold0 = Settings.ValidPointThreshold0);
                    break;
                case nameof(MegAphaseSensorSettings.ValidPointThreshold1):
                    SetSensorValue(() => _sensor.ValidPointThreshold1 = Settings.ValidPointThreshold1);
                    break;
                case nameof(MegAphaseSensorSettings.BurrFilterThreshold0):
                    SetSensorValue(() => _sensor.BurrFilterThreshold0 = Settings.BurrFilterThreshold0);
                    break;
                case nameof(MegAphaseSensorSettings.BurrFilterThreshold1):
                    SetSensorValue(() => _sensor.BurrFilterThreshold1 = Settings.BurrFilterThreshold1);
                    break;
                case nameof(MegAphaseSensorSettings.PreProcessLoopNum):
                    SetSensorValue(() => _sensor.PreProcessLoopNum = Settings.PreProcessLoopNum);
                    break;
                case nameof(MegAphaseSensorSettings.PreProcessThreshold):
                    SetSensorValue(() => _sensor.PreProcessThreshold = Settings.PreProcessThreshold);
                    break;
                case nameof(MegAphaseSensorSettings.DataOutMode):
                    SetSensorValue(() => _sensor.DataOutMode = Settings.DataOutMode);
                    break;
                case nameof(MegAphaseSensorSettings.UserRTMatrixEnableState):
                    SetSensorValue(() => _sensor.UserRTMatrixEnableState = Settings.UserRTMatrixEnableState);
                    break;
                case nameof(MegAphaseSensorSettings.RangeCheckEnableState):
                    SetSensorValue(() => _sensor.RangeCheckEnableState = Settings.RangeCheckEnableState);
                    break;
                case nameof(MegAphaseSensorSettings.PointScaleX0Pos):
                case nameof(MegAphaseSensorSettings.PointScaleXIncrement):
                case nameof(MegAphaseSensorSettings.PointScaleY0Pos):
                case nameof(MegAphaseSensorSettings.PointScaleYIncrement):
                case nameof(MegAphaseSensorSettings.PointScaleZ0Pos):
                case nameof(MegAphaseSensorSettings.PointScaleZIncrement):
                    SetSensorValue(() => _sensor.PointScaleSetting = Settings.GetPointScaleSetting());
                    break;
                case nameof(MegAphaseSensorSettings.RangeUXmin):
                case nameof(MegAphaseSensorSettings.RangeUXmax):
                case nameof(MegAphaseSensorSettings.RangeUYmin):
                case nameof(MegAphaseSensorSettings.RangeUYmax):
                case nameof(MegAphaseSensorSettings.RangeUZmin):
                case nameof(MegAphaseSensorSettings.RangeUZmax):
                    SetSensorValue(() => _sensor.RangeCheckSetting = Settings.GetRangeCheckSetting());
                    break;
                case nameof(MegAphaseSensorSettings.UserRTMatrixR00):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR01):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR02):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR10):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR11):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR12):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR20):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR21):
                case nameof(MegAphaseSensorSettings.UserRTMatrixR22):
                case nameof(MegAphaseSensorSettings.UserRTMatrixT0):
                case nameof(MegAphaseSensorSettings.UserRTMatrixT1):
                case nameof(MegAphaseSensorSettings.UserRTMatrixT2):
                    SetSensorValue(() => _sensor.UserRTMatrix = Settings.GetUserRTMatrix());
                    break;
            }
        }

        /// <summary>
        /// 按当前设置重建SDK软件预处理链。
        /// </summary>
        private void ApplySoftwarePreprocessSettings()
        {
            SetSensorValue(() =>
            {
                lock (_softwarePreprocessLock)
                {
                    _sensor.SetSoftwarePreprocessEnable(false);
                    _sensor.SoftwarePreprocessClear();

                    if (!Settings.SoftwarePreprocessEnabled)
                    {
                        return;
                    }

                    bool hasStep = false;
                    if (Settings.RemoveBurrsMode != SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off)
                    {
                        if (!_sensor.AppendSoftwarePreprocess_RemoveBurrs(Settings.RemoveBurrsMode, Settings.RemoveBurrsWinSize, Settings.RemoveBurrsWinSize2, Settings.RemoveBurrsSlopeLevel, Settings.RemoveBurrsNeighborCloseLevel, Settings.RemoveBurrsNeighborNumLevel, Settings.RemoveBurrsEdgeSuppressLevel))
                        {
                            return;
                        }

                        hasStep = true;
                    }

                    if (Settings.MendMode != SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off)
                    {
                        if (!_sensor.AppendSoftwarePreprocess_Mend(Settings.MendMode, Settings.MendWinSize, Settings.MendWinSize2, Settings.MendMethod))
                        {
                            _sensor.SetSoftwarePreprocessEnable(false);
                            return;
                        }

                        hasStep = true;
                    }

                    if (Settings.FiltrateMode != SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off)
                    {
                        byte filtrateMendPointOnly = (byte)(Settings.FiltrateMendPointOnly ? 1 : 0);
                        if (!_sensor.AppendSoftwarePreprocess_Filtrate(Settings.FiltrateMode, Settings.FiltrateWinSize, Settings.FiltrateNeighborCloseLevel, Settings.FiltrateNeighborNumLevel, filtrateMendPointOnly))
                        {
                            _sensor.SetSoftwarePreprocessEnable(false);
                            return;
                        }

                        hasStep = true;
                    }

                    _sensor.SetSoftwarePreprocessEnable(hasStep);
                }
            });
        }

        /// <summary>
        /// 读取设备当前软件预处理链，只映射本页面支持的三类算法。
        /// </summary>
        private void RefreshSoftwarePreprocessSettingsFromDevice()
        {
            lock (_softwarePreprocessLock)
            {
                Settings.RemoveBurrsMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;
                Settings.MendMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;
                Settings.FiltrateMode = SoftwarePreprocessModeType.SoftwarePreprocessModeType_Off;

                if (!_sensor.GetSoftwarePreprocessEnable(out bool enable))
                {
                    Settings.SoftwarePreprocessEnabled = false;
                    return;
                }

                Settings.SoftwarePreprocessEnabled = enable;
                if (!_sensor.GetSoftwarePreprocessCount(out uint count))
                {
                    return;
                }

                for (uint i = 0; i < count; i++)
                {
                    _sensor.GetSoftwarePreprocessTypeAt(i, out SoftwarePreprocessType type);
                    if (!_sensor.GetSoftwarePreprocessModeAt(i, out SoftwarePreprocessModeType mode))
                    {
                        continue;
                    }

                    switch (type)
                    {
                        case SoftwarePreprocessType.SoftwareProcessType_RemoveBurrs:
                            Settings.RemoveBurrsMode = mode;
                            if (_sensor.GetSoftwarePreprocess_RemoveBurrsAt(i, mode, out byte removeWinSize, out byte removeWinSize2, out byte slopeLevel, out byte removeNeighborCloseLevel, out byte removeNeighborNumLevel, out byte edgeSuppressLevel))
                            {
                                Settings.RemoveBurrsWinSize = removeWinSize;
                                Settings.RemoveBurrsWinSize2 = removeWinSize2;
                                Settings.RemoveBurrsSlopeLevel = slopeLevel;
                                Settings.RemoveBurrsNeighborCloseLevel = removeNeighborCloseLevel;
                                Settings.RemoveBurrsNeighborNumLevel = removeNeighborNumLevel;
                                Settings.RemoveBurrsEdgeSuppressLevel = edgeSuppressLevel;
                            }
                            break;
                        case SoftwarePreprocessType.SoftwareProcessType_Mend:
                            Settings.MendMode = mode;
                            if (_sensor.GetSoftwarePreprocess_MendAt(i, mode, out byte mendWinSize, out byte mendWinSize2, out byte mendMethod))
                            {
                                Settings.MendWinSize = mendWinSize;
                                Settings.MendWinSize2 = mendWinSize2;
                                Settings.MendMethod = mendMethod;
                            }
                            break;
                        case SoftwarePreprocessType.SoftwareProcessType_Filtrate:
                            Settings.FiltrateMode = mode;
                            if (_sensor.GetSoftwarePreprocess_FiltrateAt(i, mode, out byte filtrateWinSize, out byte filtrateNeighborCloseLevel, out byte filtrateNeighborNumLevel, out byte filtrateMendPointOnly))
                            {
                                Settings.FiltrateWinSize = filtrateWinSize;
                                Settings.FiltrateNeighborCloseLevel = filtrateNeighborCloseLevel;
                                Settings.FiltrateNeighborNumLevel = filtrateNeighborNumLevel;
                                Settings.FiltrateMendPointOnly = filtrateMendPointOnly != 0;
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// 页面参数变更时写入设备；刷新页面数据时不反写设备
        /// </summary>
        private void SetSensorValue(Action setValue)
        {
            if (_isRefreshingSettings || !IsConnected)
            {
                return;
            }

            try
            {
                setValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MEGAPHASE set setting failed: {ex.Message}");
                State = HardwareState.Error;
            }
        }


        #endregion
    }
}
