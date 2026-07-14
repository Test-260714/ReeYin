using ALGO.NinePointCalibration.Services;
using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.ControlCard;
using ReeYin_V.Hardware.ControlCard.Models;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ALGO.NinePointCalibration.Models
{
    [Serializable]
    public class NinePointCalibrationModel : ModelParamBase
    {
        private readonly NinePointCalibrationService _calibrationService = new NinePointCalibrationService();

        public string SltModelName = string.Empty;

        [JsonIgnore]
        private ObservableCollection<ControlCardBase> _models = new ObservableCollection<ControlCardBase>();

        [JsonIgnore]
        public ObservableCollection<ControlCardBase> Models
        {
            get { return _models; }
            set { SetProperty(ref _models, value); }
        }

        [JsonIgnore]
        private ControlCardBase? _controlCard;

        [JsonIgnore]
        public ControlCardBase? ControlCard
        {
            get { return _controlCard; }
            set
            {
                if (SetProperty(ref _controlCard, value) && value != null)
                {
                    SltModelName = value.NickName;
                }
            }
        }

        private ObservableCollection<NinePointCalibrationPoint> _points = new ObservableCollection<NinePointCalibrationPoint>();

        public ObservableCollection<NinePointCalibrationPoint> Points
        {
            get { return _points; }
            set { SetProperty(ref _points, value); }
        }

        [JsonIgnore]
        private NinePointCalibrationPoint? _selectedPoint;

        [JsonIgnore]
        public NinePointCalibrationPoint? SelectedPoint
        {
            get { return _selectedPoint; }
            set { SetProperty(ref _selectedPoint, value); }
        }

        private double _centerMachineX;

        public double CenterMachineX
        {
            get { return _centerMachineX; }
            set { SetProperty(ref _centerMachineX, value); }
        }

        private double _centerMachineY;

        public double CenterMachineY
        {
            get { return _centerMachineY; }
            set { SetProperty(ref _centerMachineY, value); }
        }

        private double _pointSpacingX = 10;

        public double PointSpacingX
        {
            get { return _pointSpacingX; }
            set { SetProperty(ref _pointSpacingX, value); }
        }

        private double _pointSpacingY = 10;

        public double PointSpacingY
        {
            get { return _pointSpacingY; }
            set { SetProperty(ref _pointSpacingY, value); }
        }

        private double _previewPixelX;

        public double PreviewPixelX
        {
            get { return _previewPixelX; }
            set { SetProperty(ref _previewPixelX, value); }
        }

        private double _previewPixelY;

        public double PreviewPixelY
        {
            get { return _previewPixelY; }
            set { SetProperty(ref _previewPixelY, value); }
        }

        private double _previewMachineX;

        [OutputParam("MachineX", "像素转换后的机械X")]
        public double PreviewMachineX
        {
            get { return _previewMachineX; }
            set { SetProperty(ref _previewMachineX, value); }
        }

        private double _previewMachineY;

        [OutputParam("MachineY", "像素转换后的机械Y")]
        public double PreviewMachineY
        {
            get { return _previewMachineY; }
            set { SetProperty(ref _previewMachineY, value); }
        }

        private double _averageError;

        [OutputParam("AverageError", "平均标定误差")]
        public double AverageError
        {
            get { return _averageError; }
            set { SetProperty(ref _averageError, value); }
        }

        private double _maxError;

        [OutputParam("MaxError", "最大标定误差")]
        public double MaxError
        {
            get { return _maxError; }
            set { SetProperty(ref _maxError, value); }
        }

        private double[] _homMat2D = Array.Empty<double>();

        [OutputParam("HomMat2D", "Halcon像素到机械坐标仿射矩阵")]
        public double[] HomMat2D
        {
            get { return _homMat2D; }
            set
            {
                SetProperty(ref _homMat2D, value ?? Array.Empty<double>());
                RaisePropertyChanged(nameof(HomMat2DText));
                RaisePropertyChanged(nameof(IsCalibrated));
            }
        }

        private double[] _inverseHomMat2D = Array.Empty<double>();

        [OutputParam("InverseHomMat2D", "Halcon机械到像素坐标仿射矩阵")]
        public double[] InverseHomMat2D
        {
            get { return _inverseHomMat2D; }
            set
            {
                SetProperty(ref _inverseHomMat2D, value ?? Array.Empty<double>());
                RaisePropertyChanged(nameof(InverseHomMat2DText));
            }
        }

        private string _statusMessage = "请先输入九个像素坐标，再计算标定。";

        [JsonIgnore]
        public string StatusMessage
        {
            get { return _statusMessage; }
            set { SetProperty(ref _statusMessage, value); }
        }

        [JsonIgnore]
        public bool IsCalibrated => HomMat2D.Length == 6;

        [JsonIgnore]
        public string HomMat2DText => FormatMatrix(HomMat2D);

        [JsonIgnore]
        public string InverseHomMat2DText => FormatMatrix(InverseHomMat2D);

        [InputParam("PixelX", "预留像素X输入", false)]
        public TransmitParam InputPixelX { get; set; } = new TransmitParam();

        [InputParam("PixelY", "预留像素Y输入", false)]
        public TransmitParam InputPixelY { get; set; } = new TransmitParam();

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        public NinePointCalibrationModel()
        {
            GenerateNinePointTemplate(false);
            RefreshControlCardContext();

            TriggerModuleRun += () =>
            {
                return ExecuteModule().Result;
            };
        }

        public override bool OnceInit()
        {
            if (IsOnceInit)
            {
                return true;
            }

            base.OnceInit();
            EnsurePointTemplate();
            RefreshControlCardContext();
            IsOnceInit = true;
            return true;
        }

        public override bool LoadKeyParam()
        {
            bool loaded = base.LoadKeyParam();
            RefreshControlCardContext();
            ApplyLinkedPreviewPixels();
            return loaded;
        }

        public new Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    if (!LoadKeyParam())
                    {
                        return NodeStatus.Error;
                    }

                    if (!IsCalibrated && !TryCalculateCalibration(out string calibrationMessage))
                    {
                        StatusMessage = calibrationMessage;
                        return NodeStatus.Error;
                    }

                    if (!TryTransformPreviewPixel(out string transformMessage))
                    {
                        StatusMessage = transformMessage;
                        return NodeStatus.Error;
                    }

                    UpdateOutputParams();
                    UpdateParam();
                    StatusMessage = "九点标定执行完成。";
                    return NodeStatus.Success;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"九点标定执行失败：{ex.Message}";
                    return NodeStatus.Error;
                }
            });

            Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };

            return Task.FromResult(Output);
        }

        public void RefreshControlCardContext()
        {
            var hardwareModuleManager = PrismProvider.HardwareModuleManager;
            if (hardwareModuleManager?.Modules == null)
            {
                return;
            }

            if (!hardwareModuleManager.Modules.TryGetValue(ConfigKey.ControlCard, out var module))
            {
                return;
            }

            if (module is not ControlCardConfigModel controlCardConfig || controlCardConfig.CardModels == null)
            {
                return;
            }

            if (!ReferenceEquals(Models, controlCardConfig.CardModels))
            {
                Models = controlCardConfig.CardModels;
            }

            if (ControlCard == null && !string.IsNullOrWhiteSpace(SltModelName))
            {
                ControlCard = Models.FirstOrDefault(item => item.NickName == SltModelName);
            }

            ControlCard ??= controlCardConfig.CurSltCard ?? Models.FirstOrDefault();
        }

        public void GenerateNinePointTemplate(bool keepPixel)
        {
            List<(int Row, int Col)> grid = new List<(int Row, int Col)>
            {
                (0, 0), (0, 1), (0, 2),
                (1, 0), (1, 1), (1, 2),
                (2, 0), (2, 1), (2, 2),
            };

            var oldPoints = Points.ToDictionary(item => item.Index, item => item);
            Points.Clear();
            for (int index = 0; index < grid.Count; index++)
            {
                int pointIndex = index + 1;
                oldPoints.TryGetValue(pointIndex, out NinePointCalibrationPoint? oldPoint);

                NinePointCalibrationPoint point = new NinePointCalibrationPoint
                {
                    Index = pointIndex,
                    IsUsed = true,
                    PixelX = keepPixel && oldPoint != null ? oldPoint.PixelX : 0,
                    PixelY = keepPixel && oldPoint != null ? oldPoint.PixelY : 0,
                    MachineX = CenterMachineX + ((grid[index].Col - 1) * PointSpacingX),
                    MachineY = CenterMachineY + ((grid[index].Row - 1) * PointSpacingY),
                };

                Points.Add(point);
            }

            SelectedPoint = Points.FirstOrDefault();
            HomMat2D = Array.Empty<double>();
            InverseHomMat2D = Array.Empty<double>();
            AverageError = 0;
            MaxError = 0;
            StatusMessage = "九点机械坐标已生成，像素坐标入口已预留。";
        }

        public bool TryCalculateCalibration(out string message)
        {
            EnsurePointTemplate();

            try
            {
                NinePointCalibrationResult result = _calibrationService.Calculate(Points.ToList());
                HomMat2D = result.HomMat2D;
                InverseHomMat2D = result.InverseHomMat2D;
                AverageError = result.AverageError;
                MaxError = result.MaxError;
                message = $"标定完成，平均误差 {AverageError:F6}，最大误差 {MaxError:F6}。";
                StatusMessage = message;
                return true;
            }
            catch (Exception ex)
            {
                HomMat2D = Array.Empty<double>();
                InverseHomMat2D = Array.Empty<double>();
                AverageError = 0;
                MaxError = 0;
                message = $"标定失败：{ex.Message}";
                StatusMessage = message;
                return false;
            }
        }

        public bool TryTransformPreviewPixel(out string message)
        {
            if (!_calibrationService.TryPixelToMachine(HomMat2D, PreviewPixelX, PreviewPixelY, out double machineX, out double machineY, out message))
            {
                StatusMessage = message;
                return false;
            }

            PreviewMachineX = machineX;
            PreviewMachineY = machineY;
            message = $"像素 ({PreviewPixelX:F3}, {PreviewPixelY:F3}) -> 机械 ({PreviewMachineX:F6}, {PreviewMachineY:F6})。";
            StatusMessage = message;
            return true;
        }

        public bool TryMoveToSelectedPoint(out string message)
        {
            if (SelectedPoint == null)
            {
                message = "请先选择一个标定点。";
                StatusMessage = message;
                return false;
            }

            return TryMoveToMachine(SelectedPoint.MachineX, SelectedPoint.MachineY, out message);
        }

        public bool TryMoveToPreviewMachine(out string message)
        {
            if (!IsCalibrated && !TryCalculateCalibration(out message))
            {
                return false;
            }

            if (!TryTransformPreviewPixel(out message))
            {
                return false;
            }

            return TryMoveToMachine(PreviewMachineX, PreviewMachineY, out message);
        }

        public bool TryMoveToMachine(double machineX, double machineY, out string message)
        {
            RefreshControlCardContext();

            if (ControlCard == null)
            {
                message = "请先选择控制卡。";
                StatusMessage = message;
                return false;
            }

            if (!ControlCard.IsReady)
            {
                message = "控制卡未准备好，请确认连接、使能与回零状态。";
                StatusMessage = message;
                return false;
            }

            var targetPosition = new Dictionary<En_AxisNum, double>
            {
                { En_AxisNum.X, machineX },
                { En_AxisNum.Y, machineY },
            };

            if (!ControlCard.ValidateLimitPosition(targetPosition, out message))
            {
                StatusMessage = message;
                return false;
            }

            bool result = ControlCard.LineInterpoMoving(new LineInterPoParam
            {
                InterPoAxiss = new List<En_AxisNum> { En_AxisNum.X, En_AxisNum.Y },
                TargetPos = GetCurrentTargetPositionFallback(machineX, machineY),
                TargetPosDic = targetPosition,
                decZSpeed = new[] { 5d, 10d, 50d },
                upZSpeed = new[] { 5d, 10d, 50d },
                waitforend = true,
            });

            message = result
                ? $"已发送移动指令：X={machineX:F6}, Y={machineY:F6}。"
                : "移动指令执行失败。";
            StatusMessage = message;
            return result;
        }

        public void ClearPixelInputs()
        {
            foreach (NinePointCalibrationPoint point in Points)
            {
                point.PixelX = 0;
                point.PixelY = 0;
                point.FitMachineX = 0;
                point.FitMachineY = 0;
                point.Error = 0;
            }

            HomMat2D = Array.Empty<double>();
            InverseHomMat2D = Array.Empty<double>();
            AverageError = 0;
            MaxError = 0;
            StatusMessage = "像素输入已清空。";
        }

        private void EnsurePointTemplate()
        {
            if (Points.Count == 9)
            {
                return;
            }

            GenerateNinePointTemplate(true);
        }

        private void ApplyLinkedPreviewPixels()
        {
            object pixelXValue = GetTransmitParam(InputParams, InputPixelX, false);
            object pixelYValue = GetTransmitParam(InputParams, InputPixelY, false);

            if (TryConvertDouble(pixelXValue, out double pixelX))
            {
                PreviewPixelX = pixelX;
            }

            if (TryConvertDouble(pixelYValue, out double pixelY))
            {
                PreviewPixelY = pixelY;
            }
        }

        private void UpdateOutputParams()
        {
            Dictionary<string, object> values = OutputParamCollector.GetDataPointValues(this);
            foreach (TransmitParam item in OutputParams)
            {
                if (values.TryGetValue(item.ParamName, out object? value))
                {
                    item.Value = value;
                }
            }
        }

        private double[] GetCurrentTargetPositionFallback(double machineX, double machineY)
        {
            double[] target = ControlCard?.CurPos?.ToArray() ?? Array.Empty<double>();
            if (target.Length < 2)
            {
                return new[] { machineX, machineY };
            }

            target[0] = machineX;
            target[1] = machineY;
            return target;
        }

        private static bool TryConvertDouble(object value, out double result)
        {
            switch (value)
            {
                case double doubleValue:
                    result = doubleValue;
                    return true;
                case float floatValue:
                    result = floatValue;
                    return true;
                case int intValue:
                    result = intValue;
                    return true;
                case long longValue:
                    result = longValue;
                    return true;
                case string stringValue when double.TryParse(stringValue, out double parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private static string FormatMatrix(double[] matrix)
        {
            return matrix == null || matrix.Length == 0
                ? "-"
                : string.Join(", ", matrix.Select(item => item.ToString("F6")));
        }
    }
}
