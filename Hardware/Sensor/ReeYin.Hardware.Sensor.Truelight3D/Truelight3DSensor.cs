using Newtonsoft.Json;
using ReeYin.Hardware.Sensor.Models;
using ReeYin.Hardware.Sensor.Truelight3D.API;
using ReeYin_V.Core;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Logger;
using ReeYin_V.Logger.Extension;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace ReeYin.Hardware.Sensor.Truelight3D
{
    public class Truelight3DSensor : SensorBase
    {
        private ITruelight3DApi _api = new Truelight3DApi();

        [JsonIgnore]
        public ITruelight3DApi Api
        {
            get { return _api; }
            set { _api = value ?? new Truelight3DApiStub(); }
        }

        [JsonIgnore]
        private string _lastApiMessage = "TrueLight3D 正式 SDK 接入中。";

        [JsonIgnore]
        public string LastApiMessage
        {
            get { return _lastApiMessage; }
            set { _lastApiMessage = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Truelight3DFrame? _lastPreviewFrame;

        [JsonIgnore]
        public Truelight3DFrame? LastPreviewFrame
        {
            get { return _lastPreviewFrame; }
            private set
            {
                _lastPreviewFrame = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private Truelight3DFrame? _lastScanTextureFrame;

        [JsonIgnore]
        public Truelight3DFrame? LastScanTextureFrame
        {
            get { return _lastScanTextureFrame; }
            private set
            {
                _lastScanTextureFrame = value;
                RaisePropertyChanged();
            }
        }

        private Truelight3DScanType _scanType = Truelight3DScanType.Variation;

        public Truelight3DScanType ScanType
        {
            get { return _scanType; }
            set
            {
                if (_scanType == value)
                {
                    return;
                }

                _scanType = value;
                RaisePropertyChanged();

                if (_scanType == Truelight3DScanType.Confocal)
                {
                    UseScanRange = true;
                }
            }
        }

        public Truelight3DObjectiveMagnification ObjectiveMagnification { get; set; } = Truelight3DObjectiveMagnification.Magnification20;

        public uint ExposureTimeUs { get; set; } = 701;

        public uint WindowSize { get; set; } = 15;

        public float ZFilter { get; set; } = 0.8f;

        private bool _useScanRange;

        public bool UseScanRange
        {
            get { return _useScanRange; }
            set
            {
                if (_useScanRange == value)
                {
                    return;
                }

                _useScanRange = value;
                RaisePropertyChanged();
            }
        }

        // SDK sample and current UI both use um for range input; avoid an extra 1/1000 conversion here.
        private float _scanRangeMm = 20f;

        public float ScanRangeMm
        {
            get { return _scanRangeMm; }
            set
            {
                if (Math.Abs(_scanRangeMm - value) < 0.000001f)
                {
                    return;
                }

                _scanRangeMm = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ScanRangeUm));
            }
        }

        private float _scanStartPositionMm;

        public float ScanStartPositionMm
        {
            get { return _scanStartPositionMm; }
            set
            {
                if (Math.Abs(_scanStartPositionMm - value) < 0.000001f)
                {
                    return;
                }

                _scanStartPositionMm = value;
                RaisePropertyChanged();
            }
        }

        private float _scanEndPositionMm;

        public float ScanEndPositionMm
        {
            get { return _scanEndPositionMm; }
            set
            {
                if (Math.Abs(_scanEndPositionMm - value) < 0.000001f)
                {
                    return;
                }

                _scanEndPositionMm = value;
                RaisePropertyChanged();
            }
        }

        private float _scanStepMm = 1f;

        public float ScanStepMm
        {
            get { return _scanStepMm; }
            set
            {
                if (Math.Abs(_scanStepMm - value) < 0.000001f)
                {
                    return;
                }

                _scanStepMm = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ScanStepUm));
            }
        }

        [JsonIgnore]
        public float ScanRangeUm
        {
            get { return NormalizeLegacyScanRangeToUm(ScanRangeMm); }
            set
            {
                ScanRangeMm = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ScanRangeMm));
            }
        }

        [JsonIgnore]
        public float ScanStepUm
        {
            get { return NormalizeLegacyScanStepToUm(ScanStepMm); }
            set
            {
                ScanStepMm = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ScanStepMm));
            }
        }

        [JsonIgnore]
        private float _zAxisSpeedMmPerSec = 2f;

        public float ZAxisSpeedMmPerSec
        {
            get { return _zAxisSpeedMmPerSec; }
            set
            {
                if (Math.Abs(_zAxisSpeedMmPerSec - value) < 0.000001f)
                {
                    return;
                }

                _zAxisSpeedMmPerSec = value;
                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        private float _zAxisAppliedSpeedMmPerSec = 2f;

        [JsonIgnore]
        public float ZAxisAppliedSpeedMmPerSec
        {
            get { return _zAxisAppliedSpeedMmPerSec; }
            private set { _zAxisAppliedSpeedMmPerSec = value; RaisePropertyChanged(); }
        }

        public float ZAxisAbsoluteTargetMm { get; set; }

        public float ZAxisRelativeOffsetMm { get; set; } = 0.001f;

        public bool ZAxisHomeWait { get; set; } = true;

        [JsonIgnore]
        private float _zAxisCurrentPositionMm;

        [JsonIgnore]
        public float ZAxisCurrentPositionMm
        {
            get { return _zAxisCurrentPositionMm; }
            private set { _zAxisCurrentPositionMm = value; RaisePropertyChanged(); }
        }

        private bool _useWhiteLightMode = true;

        public bool UseWhiteLightMode
        {
            get { return _useWhiteLightMode; }
            set
            {
                if (_useWhiteLightMode == value)
                {
                    return;
                }

                _useWhiteLightMode = value;
                RaisePropertyChanged();
            }
        }

        public byte WhiteLightValue { get; set; } = 7;

        public byte LightRed { get; set; }

        public byte LightGreen { get; set; }

        public byte LightBlue { get; set; }

        public bool EnableCircleLight { get; set; }

        public uint CircleLightValue { get; set; }

        public bool ApplyPreviewSettingsOnConnect { get; set; } = true;

        public bool EnablePointCloudOutput { get; set; } = true;

        [JsonIgnore]
        private string _lastScanPointCloudStatus = "未开始扫描点云输出校验。";

        [JsonIgnore]
        public string LastScanPointCloudStatus
        {
            get { return _lastScanPointCloudStatus; }
            private set { _lastScanPointCloudStatus = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private Truelight3DPointCloud? _lastScanPointCloud;

        [JsonIgnore]
        public Truelight3DPointCloud? LastScanPointCloud
        {
            get { return _lastScanPointCloud; }
            private set { _lastScanPointCloud = value; RaisePropertyChanged(); }
        }

        public uint PreviewTimeoutMs { get; set; } = 500;

        public int PreviewIntervalMs { get; set; } = 120;

        public int ScanResultTimeoutMs { get; set; }

        public int ScanResultPollIntervalMs { get; set; } = 200;

        public Truelight3DSensor()
        {
            VenderName = "Truelight3D";
            VenderType = "Truelight3D";
            NickName = "Truelight3D";
            IP = "192.168.1.188";
            Port = 58080;
            EnableCircleLight = false;
            CircleLightValue = 0;
            LightRed = 0;
            LightGreen = 0;
            LightBlue = 0;
        }

        public override bool Init()
        {
            ReportStage("连接", "Begin", $"准备初始化设备，目标地址 {IP}:{Port}。");
            State = HardwareState.Initializing;

            ReportStage("连接", "Initialize", "开始调用 AMSDK Initialize().");
            Truelight3DApiResult initResult = Api.Initialize();
            ReportResult("连接", "Initialize", initResult);
            if (!initResult.Success)
            {
                IsConnected = false;
                State = HardwareState.NotConnected;
                LastApiMessage = $"初始化阶段失败，尚未进入设备连接：{initResult.Message}";
                ReportStage("连接", "InitializeFailed", LastApiMessage, isWarning: true, isError: true);
                return false;
            }

            ReportStage("连接", "Connect", "Initialize 成功，开始调用 Connect().");
            Truelight3DApiResult connectResult = Api.Connect();
            ReportResult("连接", "Connect", connectResult);
            IsConnected = connectResult.Success;
            State = connectResult.Success ? HardwareState.Connected : HardwareState.NotConnected;
            LastApiMessage = connectResult.Success
                ? connectResult.Message
                : $"设备连接失败：{connectResult.Message}";
            if (connectResult.Success)
            {
                ReportStage("连接", "End", "设备连接完成。");
            }
            else
            {
                ReportStage("连接", "ConnectFailed", LastApiMessage, isWarning: true, isError: true);
            }
            if (connectResult.Success && ApplyPreviewSettingsOnConnect)
            {
                ApplyPreviewSettings("Connect");
            }

            if (connectResult.Success)
            {
                RefreshZSpeedSilently();
            }

            return connectResult.Success;
        }

        public override void Close()
        {
            ReportStage("断开", "Begin", "开始断开当前设备连接。");
            Truelight3DApiResult disconnectResult = Api.Disconnect();
            ReportResult("断开", "Disconnect", disconnectResult);
            Truelight3DApiResult shutdownResult = Api.Shutdown();
            ReportResult("断开", "Shutdown", shutdownResult);

            IsConnected = false;
            State = HardwareState.Closed;
            LastPreviewFrame = null;
            LastScanTextureFrame = null;
            ZAxisAppliedSpeedMmPerSec = ZAxisSpeedMmPerSec;
            LastScanPointCloudStatus = "设备已断开，点云输出状态已重置。";
            LastScanPointCloud = null;
            LastApiMessage = shutdownResult.Success ? shutdownResult.Message : disconnectResult.Message;
            ReportStage("断开", "End", $"断开流程结束，当前状态 {State}。");
        }

        public override void StartCollect()
        {
            EnsurePointCloudOutputEnabled();

            ReportStage("采集", "Begin", $"收到开始采集请求，当前连接状态: {IsConnected}。", isWarning: !IsConnected);
            if (!IsConnected)
            {
                LastApiMessage = "未连接设备，无法开始扫描。";
                return;
            }

            if (State == HardwareState.Running)
            {
                LastApiMessage = "当前扫描仍在进行中，请勿重复开始采集。";
                ReportStage("采集", "BeginRejected", LastApiMessage, isWarning: true, isError: true);
                return;
            }

            float minScanStepUm = GetMinimumScanStepUm(ScanType);
            if (ScanStepUm < minScanStepUm)
            {
                string scanTypeName = GetScanTypeDisplayName(ScanType);
                LastApiMessage = $"扫描步距过小：当前输入 {ScanStepUm:0.###} um，{scanTypeName}最小支持 {minScanStepUm:0.##} um。";
                State = HardwareState.Error;
                ReportStage("采集", "ValidateStep", LastApiMessage, isWarning: true, isError: true);
                return;
            }

            ReportStage(
                "采集",
                "Configure",
                $"开始配置扫描参数(输入): ScanType={ScanType}, Exposure={ExposureTimeUs}, Window={WindowSize}, ZFilter={ZFilter}, UseScanRange={UseScanRange}, Range={ScanRangeUm}um, Start={ScanStartPositionMm}mm, End={ScanEndPositionMm}mm, Step={ScanStepUm}um, ZSpeed={ZAxisSpeedMmPerSec}mm/s, LightMode={(UseWhiteLightMode ? "White" : "Single")}, White={WhiteLightValue}, RGB=({LightRed},{LightGreen},{LightBlue}).");
            LastScanPointCloudStatus = EnablePointCloudOutput
                ? "本次扫描已请求点云输出，等待扫描结果返回。"
                : "本次扫描未请求点云输出。";

            Truelight3DScanConfiguration configuration = BuildScanConfiguration();

            string effectiveZSpeedText = configuration.ZSpeedMmPerSec.HasValue
                ? configuration.ZSpeedMmPerSec.Value.ToString("0.###", CultureInfo.InvariantCulture)
                : "null";
            ReportStage(
                "采集",
                "ConfigureEffective",
                $"下发扫描参数(实际): UseScanRange={configuration.UseScanRange}, Range={configuration.ScanRangeMm}um, Start={configuration.ScanStartMm}mm, End={configuration.ScanEndMm}mm, Step(Input)={ScanStepUm}um, Step(Send)={configuration.ScanStepMm}um, ZSpeed={effectiveZSpeedText}mm/s.");

            Truelight3DApiResult configureResult = Api.ConfigureScan(configuration);
            ReportResult("采集", "ConfigureScan", configureResult);
            if (!configureResult.Success)
            {
                State = HardwareState.Error;
                LastApiMessage = configureResult.Message;
                return;
            }

            ReportStage("采集", "Start", "扫描参数下发成功，开始调用 StartScan().");
            Truelight3DApiResult startResult = Api.StartScan();
            ReportResult("采集", "StartScan", startResult);
            State = startResult.Success ? HardwareState.Running : HardwareState.Error;
            LastApiMessage = startResult.Message;
        }

        public override void StopCollect()
        {
            ReportStage("采集", "Stop", "开始调用 StopScan().");
            Truelight3DApiResult stopResult = Api.StopScan();
            ReportResult("采集", "StopScan", stopResult);
            State = stopResult.Success ? HardwareState.Complete : HardwareState.Error;
            LastApiMessage = stopResult.Message;
        }

        public void ReadPreviewFrame()
        {
            ReportStage("预览", "Begin", $"收到读取单帧请求，当前连接状态: {IsConnected}。", isWarning: !IsConnected);
            LastPreviewFrame = null;
            if (!IsConnected)
            {
                LastApiMessage = "未连接设备，无法读取单帧图像。";
                return;
            }

            ReportStage("预览", "ReadImage", $"开始读取单帧图像，Timeout={PreviewTimeoutMs} ms。");
            if (!ApplyPreviewSettings("Preview"))
            {
                return;
            }

            Truelight3DApiResult<Truelight3DFrame> frameResult = Api.ReadImage(PreviewTimeoutMs);
            ReportResult("预览", "ReadImage", frameResult);
            if (!frameResult.Success || frameResult.Data == null)
            {
                LastApiMessage = frameResult.Message;
                return;
            }

            LastPreviewFrame = frameResult.Data;
            ReportStage("预览", "End", $"单帧图像读取完成: {frameResult.Data.Width} x {frameResult.Data.Height}, Channel={frameResult.Data.Channel}.");
            LastApiMessage = $"已读取单帧图像：{frameResult.Data.Width} x {frameResult.Data.Height}，通道数：{frameResult.Data.Channel}。";
        }

        public void ReadPreviewFrameSilently()
        {
            if (!IsConnected)
            {
                return;
            }

            Truelight3DApiResult<Truelight3DFrame> frameResult = Api.ReadImage(PreviewTimeoutMs);
            if (!frameResult.Success || frameResult.Data == null)
            {
                return;
            }

            LastPreviewFrame = frameResult.Data;
        }

        public bool TryReadPreviewFrameDataOnce(out Truelight3DFrame? frame)
        {
            frame = null;
            if (!IsConnected)
            {
                return false;
            }

            Truelight3DApiResult<Truelight3DFrame> frameResult = Api.ReadImage(PreviewTimeoutMs);
            if (!frameResult.Success || frameResult.Data == null)
            {
                return false;
            }

            frame = frameResult.Data;
            return true;
        }

        public void ApplyPreviewFrameData(Truelight3DFrame frame)
        {
            LastPreviewFrame = frame;
        }

        public override List<MeasureData> ReceiveSensorData()
        {
            EnsurePointCloudOutputEnabled();

            ReportStage("采集结果", "Begin", $"收到读取扫描结果请求，当前连接状态: {IsConnected}。", isWarning: !IsConnected);
            if (!IsConnected)
            {
                LastApiMessage = "未连接设备，无法读取扫描结果。";
                return [];
            }

            ReportStage("采集结果", "Poll", $"开始轮询扫描结果，Timeout=Infinite, Interval={ScanResultPollIntervalMs} ms。");
            DateTime? deadline = null;
            Truelight3DApiResult<Truelight3DScanResult> scanResult = Truelight3DApiResult<Truelight3DScanResult>.FailureResult(
                Truelight3DStatus.STATUS_ERROR,
                "扫描结果尚未就绪。");

            do
            {
                scanResult = Api.ReadScanResult(EnablePointCloudOutput);
                if (TryApplyScanResult(scanResult, out List<MeasureData> scanMeasureData))
                {
                    return scanMeasureData;
                }

                if (scanResult.Success && scanResult.Data != null)
                {
                    List<MeasureData> measureData = ConvertToMeasureData(scanResult.Data);
                    bool hasPointCloud = scanResult.Data.PointCloud?.Points != null && scanResult.Data.PointCloud.Points.Length > 0;
                    int pointCount = hasPointCloud ? scanResult.Data.PointCloud!.Points.Length : 0;
                    LastScanPointCloudStatus = EnablePointCloudOutput
                        ? (hasPointCloud
                            ? $"点云输出成功，点数={pointCount}。"
                            : "已请求点云输出，但本次扫描结果未返回点云数据。")
                        : "本次扫描未请求点云输出。";
                    LastScanPointCloud = hasPointCloud ? scanResult.Data.PointCloud : null;
                    LastScanTextureFrame = CreateTextureFrame(scanResult.Data);
                    State = HardwareState.Complete;
                    ReportStage("PointCloud", "Status", LastScanPointCloudStatus, isWarning: EnablePointCloudOutput && !hasPointCloud);
                    ReportStage("采集结果", "End", $"扫描结果读取成功: Width={scanResult.Data.Width}, Height={scanResult.Data.Height}, Rows={measureData.Count}.");
                    LastApiMessage = $"扫描结果读取完成，共 {measureData.Count} 行；{LastScanPointCloudStatus}";
                    return measureData;
                }

                if (deadline.HasValue && DateTime.UtcNow >= deadline.Value)
                {
                    ReportStage("采集结果", "Timeout", $"轮询超时，最近返回: {scanResult.Message}", isWarning: true);
                    break;
                }

                Thread.Sleep(Math.Max(1, ScanResultPollIntervalMs));
            }
            while (true);

            LastScanPointCloudStatus = EnablePointCloudOutput
                ? "扫描结果超时或失败，无法确认点云输出。"
                : "扫描结果超时或失败。";
            LastScanPointCloud = null;
            LastScanTextureFrame = null;
            LastApiMessage = scanResult.Message;
            return [];
        }

        public bool TryExportLastPointCloudToFile(string filePath, out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                message = "保存点云失败：目标文件路径为空。";
                return false;
            }

            if (LastScanPointCloud?.Points == null || LastScanPointCloud.Points.Length == 0)
            {
                message = "当前没有可导出的点云数据，请先完成一次勾选“输出点云”的扫描。";
                return false;
            }

            try
            {
                string? folder = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                using StreamWriter writer = new(filePath, false);
                foreach (Truelight3DPoint point in LastScanPointCloud.Points)
                {
                    if (float.IsNaN(point.X) || float.IsNaN(point.Y) || float.IsNaN(point.Z) ||
                        float.IsInfinity(point.X) || float.IsInfinity(point.Y) || float.IsInfinity(point.Z))
                    {
                        continue;
                    }

                    writer.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"{point.X:F6} {point.Y:F6} {point.Z:F6}"));
                }

                message = $"点云已保存到：{filePath}";
                return true;
            }
            catch (Exception ex)
            {
                message = $"保存点云失败：{ex.Message}";
                return false;
            }
        }

        public bool TryExportLastPointCloudToTempFile(out string filePath, out string message)
        {
            filePath = string.Empty;

            try
            {
                string folder = Path.Combine(Path.GetTempPath(), "ReeYin", "Truelight3D");
                Directory.CreateDirectory(folder);
                filePath = Path.Combine(folder, $"Truelight3D_{DateTime.Now:yyyyMMdd_HHmmssfff}.xyz");
                return TryExportLastPointCloudToFile(filePath, out message);
            }
            catch (Exception ex)
            {
                message = $"导出点云失败：{ex.Message}";
                return false;
            }
        }

        public bool TryReceiveScanResultOnce()
        {
            EnsurePointCloudOutputEnabled();

            if (!IsConnected)
            {
                return false;
            }

            return TryApplyScanResult(Api.ReadScanResult(EnablePointCloudOutput), out _);
        }

        public bool TryReadScanResultDataOnce(out Truelight3DScanResult? scanResult)
        {
            EnsurePointCloudOutputEnabled();
            scanResult = null;

            if (!IsConnected)
            {
                return false;
            }

            Truelight3DApiResult<Truelight3DScanResult> result = Api.ReadScanResult(EnablePointCloudOutput);
            if (!result.Success || result.Data == null)
            {
                return false;
            }

            scanResult = result.Data;
            return true;
        }

        public List<MeasureData> ApplyScanResultData(Truelight3DScanResult scanResult)
        {
            return ApplyScanResultCore(scanResult);
        }

        private void EnsurePointCloudOutputEnabled()
        {
            if (!EnablePointCloudOutput)
            {
                EnablePointCloudOutput = true;
            }
        }

        public override bool SettingParam(string key, object value)
        {
            ReportStage("参数", "Begin", $"收到参数写入请求 {key} => {value}.");
            if (TryUpdateLocalConfiguration(key, value))
            {
                LastApiMessage = $"已更新本地参数：{key}。";
                return true;
            }

            Truelight3DApiResult result = Api.SetParameter(key, value);
            ReportResult("参数", $"SetParameter:{key}", result);
            LastApiMessage = result.Message;
            return result.Success;
        }

        public bool ApplyPreviewSettings(string source = "Manual")
        {
            if (!IsConnected)
            {
                LastApiMessage = "未连接设备，无法下发预览参数。";
                return false;
            }

            ReportStage("光源", "Apply", $"开始下发预览参数，Source={source}, Objective={ObjectiveMagnification}, Exposure={ExposureTimeUs}, LightMode={(UseWhiteLightMode ? "White" : "Single")}, White={WhiteLightValue}, RGB=({LightRed},{LightGreen},{LightBlue}).");

            Truelight3DApiResult result = Api.SetObjectiveMagnification(ObjectiveMagnification);
            ReportResult("光源", "SetObjective", result);
            if (!result.Success)
            {
                LastApiMessage = result.Message;
                return false;
            }

            result = Api.SetExposureTime(ExposureTimeUs);
            ReportResult("光源", "SetExposure", result);
            if (!result.Success)
            {
                LastApiMessage = result.Message;
                return false;
            }

            result = ApplyConfiguredLightMode();
            LastApiMessage = result.Message;
            return result.Success;
        }

        public void MoveZPositive()
        {
            if (!PrepareZMotion("点动上"))
            {
                return;
            }

            ReportStage("Z轴", "MovePositive", "开始执行 Z 轴正向连续运动。");
            Truelight3DApiResult result = Api.MoveZ(Truelight3DMotionDirection.Positive);
            ReportResult("Z轴", "MoveZPositive", result);
            LastApiMessage = result.Message;
        }

        public void MoveZNegative()
        {
            if (!PrepareZMotion("点动下"))
            {
                return;
            }

            ReportStage("Z轴", "MoveNegative", "开始执行 Z 轴反向连续运动。");
            Truelight3DApiResult result = Api.MoveZ(Truelight3DMotionDirection.Negative);
            ReportResult("Z轴", "MoveZNegative", result);
            LastApiMessage = result.Message;
        }

        public void MoveZRelative()
        {
            if (!PrepareZMotion("相对移动"))
            {
                return;
            }
            if (!EnsureZAxisStoppedForPreciseMove("MoveZRelative"))
            {
                return;
            }

            ReportStage("Z轴", "MoveRelative", $"开始执行相对移动，Offset={ZAxisRelativeOffsetMm} mm。");
            Truelight3DApiResult result = Api.MoveZRelative(ZAxisRelativeOffsetMm);
            ReportResult("Z轴", "MoveZRelative", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("MoveZRelative");
            }
        }

        public void MoveZStepPositive()
        {
            if (!PrepareZMotion("步进上"))
            {
                return;
            }
            if (!EnsureZAxisStoppedForPreciseMove("MoveZStepPositive"))
            {
                return;
            }

            float offset = Math.Abs(ZAxisRelativeOffsetMm);
            ReportStage("Z轴", "StepPositive", $"开始执行步进上，Offset={offset} mm。");
            Truelight3DApiResult result = Api.MoveZRelative(offset);
            ReportResult("Z轴", "MoveZStepPositive", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("MoveZStepPositive");
            }
        }

        public void MoveZStepNegative()
        {
            if (!PrepareZMotion("步进下"))
            {
                return;
            }
            if (!EnsureZAxisStoppedForPreciseMove("MoveZStepNegative"))
            {
                return;
            }

            float offset = -Math.Abs(ZAxisRelativeOffsetMm);
            ReportStage("Z轴", "StepNegative", $"开始执行步进下，Offset={offset} mm。");
            Truelight3DApiResult result = Api.MoveZRelative(offset);
            ReportResult("Z轴", "MoveZStepNegative", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("MoveZStepNegative");
            }
        }

        public void MoveZAbsolute()
        {
            if (!PrepareZMotion("绝对移动"))
            {
                return;
            }
            if (!EnsureZAxisStoppedForPreciseMove("MoveZAbsolute"))
            {
                return;
            }

            ReportStage("Z轴", "MoveAbsolute", $"开始执行绝对移动，Target={ZAxisAbsoluteTargetMm} mm。");
            Truelight3DApiResult result = Api.MoveZAbsolute(ZAxisAbsoluteTargetMm);
            ReportResult("Z轴", "MoveZAbsolute", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("MoveZAbsolute");
            }
        }

        public void MoveZHome()
        {
            if (!PrepareZMotion("回零"))
            {
                return;
            }
            if (!EnsureZAxisStoppedForPreciseMove("MoveZHome"))
            {
                return;
            }

            ReportStage("Z轴", "MoveHome", $"开始执行回零，Wait={ZAxisHomeWait}。");
            Truelight3DApiResult result = Api.MoveZHome(ZAxisHomeWait);
            ReportResult("Z轴", "MoveZHome", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("MoveZHome");
            }
        }

        public void StopZMotion()
        {
            ReportStage("Z轴", "Stop", "开始执行停止 Z 轴。");
            Truelight3DApiResult result = Api.StopZ();
            ReportResult("Z轴", "StopZ", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                RefreshZPosition("StopZ");
            }
        }

        public void RefreshZPosition(string source = "Manual")
        {
            ReportStage("Z轴", "ReadPosition", $"开始读取当前位置，Source={source}。");
            Truelight3DApiResult<float> result = Api.GetZPosition();
            ReportResult("Z轴", "GetZPosition", result);
            LastApiMessage = result.Message;
            if (result.Success)
            {
                ZAxisCurrentPositionMm = result.Data;
                ReportStage("Z轴", "Position", $"当前位置更新完成: {ZAxisCurrentPositionMm:F3} mm。");
            }
        }

        public void RefreshZPositionSilently()
        {
            if (!IsConnected)
            {
                return;
            }

            Truelight3DApiResult<float> result = Api.GetZPosition();
            if (result.Success)
            {
                ZAxisCurrentPositionMm = result.Data;
            }
        }

        public void RefreshZSpeedSilently()
        {
            if (!IsConnected)
            {
                return;
            }

            Truelight3DApiResult<float> result = Api.GetZSpeed();
            if (result.Success)
            {
                ZAxisAppliedSpeedMmPerSec = result.Data;
            }
        }

        private Truelight3DScanConfiguration BuildScanConfiguration()
        {
            GetActiveLightValues(out byte red, out byte green, out byte blue);

            return new Truelight3DScanConfiguration
            {
                ScanType = ScanType,
                ObjectiveMagnification = ObjectiveMagnification,
                ExposureTimeUs = ExposureTimeUs,
                WindowSize = WindowSize,
                ZFilter = ZFilter,
                UseScanRange = UseScanRange,
                ScanRangeMm = ScanRangeUm,
                ScanStartMm = ScanStartPositionMm,
                ScanEndMm = ScanEndPositionMm,
                ScanStepMm = ScanStepUm,
                LightRed = red,
                LightGreen = green,
                LightBlue = blue,
                CircleLightValue = null,
                ZSpeedMmPerSec = ZAxisSpeedMmPerSec > 0f ? ZAxisSpeedMmPerSec : null,
            };
        }

        private static float GetMinimumScanStepUm(Truelight3DScanType scanType)
        {
            return scanType == Truelight3DScanType.Confocal ? 0.01f : 0.1f;
        }

        private static string GetScanTypeDisplayName(Truelight3DScanType scanType)
        {
            return scanType == Truelight3DScanType.Confocal ? "共焦" : "变焦";
        }

        private bool TryApplyScanResult(Truelight3DApiResult<Truelight3DScanResult> scanResult, out List<MeasureData> measureData)
        {
            measureData = [];
            if (!scanResult.Success || scanResult.Data == null)
            {
                return false;
            }

            measureData = ApplyScanResultCore(scanResult.Data);
            return true;
        }

        private List<MeasureData> ApplyScanResultCore(Truelight3DScanResult scanResult)
        {
            List<MeasureData> measureData = ConvertToMeasureData(scanResult);
            bool hasPointCloud = scanResult.PointCloud?.Points != null && scanResult.PointCloud.Points.Length > 0;
            int pointCount = hasPointCloud ? scanResult.PointCloud!.Points.Length : 0;
            LastScanPointCloudStatus = EnablePointCloudOutput
                ? (hasPointCloud
                    ? $"Point cloud output succeeded, point count {pointCount}."
                    : "Point cloud was requested, but this scan result did not return point cloud data.")
                : "Point cloud output was not requested for this scan.";
            LastScanPointCloud = hasPointCloud ? scanResult.PointCloud : null;
            LastScanTextureFrame = CreateTextureFrame(scanResult);
            State = HardwareState.Complete;
            ReportStage("PointCloud", "Status", LastScanPointCloudStatus, isWarning: EnablePointCloudOutput && !hasPointCloud);
            ReportStage("閲囬泦缁撴灉", "End", $"鎵弿缁撴灉璇诲彇鎴愬姛: Width={scanResult.Width}, Height={scanResult.Height}, Rows={measureData.Count}.");
            LastApiMessage = $"鎵弿缁撴灉璇诲彇瀹屾垚锛屽叡 {measureData.Count} 琛岋紱{LastScanPointCloudStatus}";
            return measureData;
        }

        private static Truelight3DFrame? CreateTextureFrame(Truelight3DScanResult scanResult)
        {
            if (scanResult.Width <= 0 ||
                scanResult.Height <= 0 ||
                scanResult.TextureData == null ||
                scanResult.TextureData.Length == 0 ||
                scanResult.TextureChannel <= 0)
            {
                return null;
            }

            int expectedLength = scanResult.Width * scanResult.Height * scanResult.TextureChannel;
            if (scanResult.TextureData.Length < expectedLength)
            {
                return null;
            }

            if (scanResult.TextureChannel != 1 && scanResult.TextureChannel != 3)
            {
                return null;
            }

            byte[] pixelData = new byte[expectedLength];
            Array.Copy(scanResult.TextureData, pixelData, expectedLength);

            return new Truelight3DFrame
            {
                Width = scanResult.Width,
                Height = scanResult.Height,
                Channel = scanResult.TextureChannel,
                Format = scanResult.TextureChannel == 1 ? Truelight3DPixelFormat.Gray : scanResult.TextureFormat,
                PixelData = pixelData,
            };
        }

        private static List<MeasureData> ConvertToMeasureData(Truelight3DScanResult scanResult)
        {
            List<MeasureData> result = new(scanResult.Height);
            for (int row = 0; row < scanResult.Height; row++)
            {
                int offset = row * scanResult.Width;
                float[] heightRow = new float[scanResult.Width];
                Array.Copy(scanResult.DepthData, offset, heightRow, 0, scanResult.Width);

                float[] grayRow = new float[scanResult.Width];
                for (int column = 0; column < scanResult.Width; column++)
                {
                    grayRow[column] = ReadGrayValue(scanResult, offset + column);
                }

                result.Add(new MeasureData
                {
                    AreaData = [heightRow, grayRow],
                    RTime = DateTime.Now,
                });
            }

            return result;
        }

        private static float ReadGrayValue(Truelight3DScanResult scanResult, int pixelIndex)
        {
            if (scanResult.TextureData.Length == 0 || scanResult.TextureChannel <= 0)
            {
                return 0f;
            }

            if (scanResult.TextureChannel == 1)
            {
                return scanResult.TextureData[pixelIndex];
            }

            int baseIndex = pixelIndex * scanResult.TextureChannel;
            if (baseIndex + scanResult.TextureChannel > scanResult.TextureData.Length)
            {
                return 0f;
            }

            float total = 0f;
            for (int i = 0; i < scanResult.TextureChannel; i++)
            {
                total += scanResult.TextureData[baseIndex + i];
            }

            return total / scanResult.TextureChannel;
        }

        private Truelight3DApiResult ApplyConfiguredLightMode()
        {
            Truelight3DApiResult circleOffResult = Api.SetCircleLight(0);
            if (circleOffResult.Status == Truelight3DStatus.STATUS_NOT_SUPPORTED)
            {
                ReportStage("光源", "ResetCircleLight", "当前设备不支持环形光，已按 RGB 光源模式继续。");
            }
            else
            {
                ReportResult("光源", "ResetCircleLight", circleOffResult);
            }
            if (!circleOffResult.Success && circleOffResult.Status != Truelight3DStatus.STATUS_NOT_SUPPORTED)
            {
                return circleOffResult;
            }

            GetActiveLightValues(out byte red, out byte green, out byte blue);

            Truelight3DApiResult rgbResult = Api.SetLightRgb(red, green, blue);
            ReportResult("光源", "SetLightRgb", rgbResult);
            return rgbResult;
        }

        private void GetActiveLightValues(out byte red, out byte green, out byte blue)
        {
            if (UseWhiteLightMode)
            {
                red = WhiteLightValue;
                green = WhiteLightValue;
                blue = WhiteLightValue;
                return;
            }

            red = LightRed;
            green = LightGreen;
            blue = LightBlue;
        }

        private bool TryUpdateLocalConfiguration(string key, object value)
        {
            switch (key)
            {
                case "ScanType":
                    if (value is Truelight3DScanType scanType)
                    {
                        ScanType = scanType;
                        return true;
                    }
                    break;
                case "ObjectiveMagnification":
                    if (value is Truelight3DObjectiveMagnification magnification)
                    {
                        ObjectiveMagnification = magnification;
                        return true;
                    }
                    break;
                case "ExposureTimeUs":
                    if (TryConvertToUInt(value, out uint exposure))
                    {
                        ExposureTimeUs = exposure;
                        return true;
                    }
                    break;
                case "WindowSize":
                    if (TryConvertToUInt(value, out uint windowSize))
                    {
                        WindowSize = windowSize;
                        return true;
                    }
                    break;
                case "ZFilter":
                    if (TryConvertToFloat(value, out float zFilter))
                    {
                        ZFilter = zFilter;
                        return true;
                    }
                    break;
                case "ScanRangeMm":
                    if (TryConvertToFloat(value, out float scanRange))
                    {
                        ScanRangeMm = scanRange;
                        return true;
                    }
                    break;
                case "ScanStepMm":
                    if (TryConvertToFloat(value, out float scanStep))
                    {
                        ScanStepMm = scanStep;
                        return true;
                    }
                    break;
                case "ZSpeed":
                    if (TryConvertToFloat(value, out float zSpeed))
                    {
                        ZAxisSpeedMmPerSec = zSpeed;
                        return true;
                    }
                    break;
                case "LightRed":
                    if (TryConvertToByte(value, out byte red))
                    {
                        LightRed = red;
                        return true;
                    }
                    break;
                case "LightGreen":
                    if (TryConvertToByte(value, out byte green))
                    {
                        LightGreen = green;
                        return true;
                    }
                    break;
                case "LightBlue":
                    if (TryConvertToByte(value, out byte blue))
                    {
                        LightBlue = blue;
                        return true;
                    }
                    break;
                case "WhiteLightValue":
                    if (TryConvertToByte(value, out byte white))
                    {
                        WhiteLightValue = white;
                        return true;
                    }
                    break;
                case "UseWhiteLightMode":
                    if (value is bool useWhiteLightMode)
                    {
                        UseWhiteLightMode = useWhiteLightMode;
                        return true;
                    }
                    break;
                case "CircleLightValue":
                    if (TryConvertToUInt(value, out uint circleLight))
                    {
                        CircleLightValue = circleLight;
                        return true;
                    }
                    break;
                case "EnableCircleLight":
                    if (value is bool enableCircleLight)
                    {
                        EnableCircleLight = enableCircleLight;
                        return true;
                    }
                    break;
                case "ApplyPreviewSettingsOnConnect":
                    if (value is bool applyPreviewSettingsOnConnect)
                    {
                        ApplyPreviewSettingsOnConnect = applyPreviewSettingsOnConnect;
                        return true;
                    }
                    break;
                case "UseScanRange":
                    if (value is bool useScanRange)
                    {
                        UseScanRange = useScanRange;
                        return true;
                    }
                    break;
                case "ScanStartPositionMm":
                    if (TryConvertToFloat(value, out float scanStart))
                    {
                        ScanStartPositionMm = scanStart;
                        return true;
                    }
                    break;
                case "ScanEndPositionMm":
                    if (TryConvertToFloat(value, out float scanEnd))
                    {
                        ScanEndPositionMm = scanEnd;
                        return true;
                    }
                    break;
                case "ScanRangeUm":
                    if (TryConvertToFloat(value, out float scanRangeUm))
                    {
                        ScanRangeUm = scanRangeUm;
                        return true;
                    }
                    break;
                case "ScanStepUm":
                    if (TryConvertToFloat(value, out float scanStepUm))
                    {
                        ScanStepUm = scanStepUm;
                        return true;
                    }
                    break;
                case "ZAxisSpeedMmPerSec":
                    if (TryConvertToFloat(value, out float zAxisSpeed))
                    {
                        ZAxisSpeedMmPerSec = zAxisSpeed;
                        return true;
                    }
                    break;
            }

            return false;
        }

        private bool PrepareZMotion(string actionName)
        {
            if (!IsConnected)
            {
                LastApiMessage = $"TrueLight3D 未连接，无法执行 Z 轴{actionName}。";
                ReportStage("Z轴", actionName, "设备未连接，无法执行当前 Z 轴动作。", isWarning: true);
                return false;
            }

            if (ZAxisSpeedMmPerSec > 0f)
            {
                ReportStage("Z轴", "SetSpeed", $"开始设置 Z 轴速度，Speed={ZAxisSpeedMmPerSec} mm/s。");
                Truelight3DApiResult speedResult = Api.SetParameter("ZSpeed", ZAxisSpeedMmPerSec);
                ReportResult("Z轴", "SetZSpeed", speedResult);
                if (!speedResult.Success)
                {
                    LastApiMessage = speedResult.Message;
                    return false;
                }

                RefreshZSpeedSilently();
            }

            return true;
        }

        private bool EnsureZAxisStoppedForPreciseMove(string sourceAction)
        {
            ReportStage("Z轴", "StopBeforeMove", $"在 {sourceAction} 前尝试停止连续运动。");
            Truelight3DApiResult stopResult = Api.StopZ();
            ReportResult("Z轴", "StopBeforeMove", stopResult);
            if (!stopResult.Success)
            {
                LastApiMessage = stopResult.Message;
                return false;
            }

            return true;
        }

        private void ReportResult(string actionName, string stageName, Truelight3DApiResult result)
        {
            ReportStage(actionName, stageName, result.Message, isWarning: !result.Success, isError: !result.Success);
        }

        private void ReportResult<T>(string actionName, string stageName, Truelight3DApiResult<T> result)
        {
            ReportStage(actionName, stageName, result.Message, isWarning: !result.Success, isError: !result.Success);
        }

        private static void ReportStage(string actionName, string stageName, string message, bool isWarning = false, bool isError = false)
        {
            string fullMessage = $"[Truelight3D][{actionName}][{stageName}] {message}";
            ConsoleEx.WriteLineEx(fullMessage);

            if (isError)
            {
                Logs.LogError(fullMessage);
                return;
            }

            if (isWarning)
            {
                Logs.LogWarning(fullMessage);
                return;
            }

            Logs.LogInfo(fullMessage);
        }

        private static bool TryConvertToUInt(object? value, out uint result)
        {
            switch (value)
            {
                case byte byteValue:
                    result = byteValue;
                    return true;
                case ushort ushortValue:
                    result = ushortValue;
                    return true;
                case uint uintValue:
                    result = uintValue;
                    return true;
                case int intValue when intValue >= 0:
                    result = (uint)intValue;
                    return true;
                case string stringValue when uint.TryParse(stringValue, out uint parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static bool TryConvertToFloat(object? value, out float result)
        {
            switch (value)
            {
                case float floatValue:
                    result = floatValue;
                    return true;
                case double doubleValue:
                    result = (float)doubleValue;
                    return true;
                case decimal decimalValue:
                    result = (float)decimalValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case string stringValue when float.TryParse(stringValue, out float parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0f;
                    return false;
            }
        }

        private static bool TryConvertToByte(object? value, out byte result)
        {
            switch (value)
            {
                case byte byteValue:
                    result = byteValue;
                    return true;
                case int intValue when intValue >= byte.MinValue && intValue <= byte.MaxValue:
                    result = (byte)intValue;
                    return true;
                case string stringValue when byte.TryParse(stringValue, out byte parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static float NormalizeLegacyScanRangeToUm(float rawValue)
        {
            return rawValue > 0f && rawValue < 1f
                ? rawValue * 1000f
                : rawValue;
        }

        private static float NormalizeLegacyScanStepToUm(float rawValue)
        {
            return rawValue > 0f && rawValue < 0.01f
                ? rawValue * 1000f
                : rawValue;
        }
    }
}
