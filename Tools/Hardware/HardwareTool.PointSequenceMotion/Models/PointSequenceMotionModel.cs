using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace HardwareTool.PointSequenceMotion.Models
{
    public enum PointSequenceSelectMode
    {
        内部循环,
        执行次数,
        点位序号
    }

    [Serializable]
    public class PointSequenceMotionModel : ModelParamBase
    {
        private const string CurrentPointCoordinateParamName = "CurrentPointCoordinate";
        private const string CurrentPointXParamName = "CurrentPointX";
        private const string CurrentPointYParamName = "CurrentPointY";
        private const string CurrentPointZParamName = "CurrentPointZ";
        private const string CurrentPointZ1ParamName = "CurrentPointZ1";
        private const string CurrentPointZ2ParamName = "CurrentPointZ2";

        private static readonly CoordinateOutputParamDefinition[] CoordinateOutputParamDefinitions =
        {
            new(CurrentPointCoordinateParamName, DataType._object, "Current point coordinate"),
            new(CurrentPointXParamName, DataType.Double, "Current point X coordinate"),
            new(CurrentPointYParamName, DataType.Double, "Current point Y coordinate"),
            new(CurrentPointZParamName, DataType.Double, "Current point Z coordinate"),
            new(CurrentPointZ1ParamName, DataType.Double, "Current point Z1 coordinate"),
            new(CurrentPointZ2ParamName, DataType.Double, "Current point Z2 coordinate"),
        };

        public string SltModelName { get; set; } = string.Empty;

        [JsonIgnore]
        private ObservableCollection<ControlCardBase> _models = new ObservableCollection<ControlCardBase>();

        [JsonIgnore]
        public ObservableCollection<ControlCardBase> Models
        {
            get => _models;
            set { _models = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private ControlCardBase? _controlCard;

        [JsonIgnore]
        public ControlCardBase? ControlCard
        {
            get => _controlCard;
            set
            {
                _controlCard = value;
                if (_controlCard != null)
                {
                    SltModelName = _controlCard.NickName;
                }

                RaisePropertyChanged();
            }
        }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; } = null!;

        private ObservableCollection<PointSequenceItem> _points = new ObservableCollection<PointSequenceItem>();
        public ObservableCollection<PointSequenceItem> Points
        {
            get => _points;
            set { _points = value ?? new ObservableCollection<PointSequenceItem>(); RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PointSequenceItem? _selectedPoint;

        [JsonIgnore]
        public PointSequenceItem? SelectedPoint
        {
            get => _selectedPoint;
            set { _selectedPoint = value; RaisePropertyChanged(); }
        }

        private PointSequenceSelectMode _selectMode = PointSequenceSelectMode.内部循环;
        public PointSequenceSelectMode SelectMode
        {
            get => _selectMode;
            set { _selectMode = value; RaisePropertyChanged(); }
        }

        private TransmitParam _executeCountInput = new TransmitParam
        {
            Name = "执行次数",
            ParamName = "ExecuteCount",
            Type = DataType.Int,
            Resourece = ResoureceType.None,
            Describe = "外部循环执行次数"
        };

        [InputParam("ExecuteCount", "外部循环执行次数", false)]
        public TransmitParam ExecuteCountInput
        {
            get => _executeCountInput;
            set { _executeCountInput = value ?? CreateIntInput("执行次数", "ExecuteCount", "外部循环执行次数"); RaisePropertyChanged(); }
        }

        private TransmitParam _triggerPointIndexInput = new TransmitParam
        {
            Name = "触发点位序号",
            ParamName = "TriggerPointIndex",
            Type = DataType.Int,
            Resourece = ResoureceType.None,
            Describe = "外部指定触发点位序号"
        };

        [InputParam("TriggerPointIndex", "外部指定触发点位序号", false)]
        public TransmitParam TriggerPointIndexInput
        {
            get => _triggerPointIndexInput;
            set { _triggerPointIndexInput = value ?? CreateIntInput("触发点位序号", "TriggerPointIndex", "外部指定触发点位序号"); RaisePropertyChanged(); }
        }

        private bool _indexStartsAtOne = true;
        public bool IndexStartsAtOne
        {
            get => _indexStartsAtOne;
            set { _indexStartsAtOne = value; RaisePropertyChanged(); }
        }

        private bool _loopWhenOverflow = true;
        public bool LoopWhenOverflow
        {
            get => _loopWhenOverflow;
            set { _loopWhenOverflow = value; RaisePropertyChanged(); }
        }

        private int _nextInternalIndex;
        public int NextInternalIndex
        {
            get => _nextInternalIndex;
            set { _nextInternalIndex = Math.Max(0, value); RaisePropertyChanged(); }
        }

        private bool _useX = true;
        public bool UseX
        {
            get => _useX;
            set { _useX = value; RaisePropertyChanged(); }
        }

        private bool _useY = true;
        public bool UseY
        {
            get => _useY;
            set { _useY = value; RaisePropertyChanged(); }
        }

        private bool _useZ;
        public bool UseZ
        {
            get => _useZ;
            set { _useZ = value; RaisePropertyChanged(); }
        }

        private bool _useZ1;
        public bool UseZ1
        {
            get => _useZ1;
            set { _useZ1 = value; RaisePropertyChanged(); }
        }

        private bool _useZ2;
        public bool UseZ2
        {
            get => _useZ2;
            set { _useZ2 = value; RaisePropertyChanged(); }
        }

        private bool _isSafetyMoving = true;
        public bool IsSafetyMoving
        {
            get => _isSafetyMoving;
            set { _isSafetyMoving = value; RaisePropertyChanged(); }
        }

        private EN_SpeedType _defaultSpeed = EN_SpeedType.Mid;
        public EN_SpeedType DefaultSpeed
        {
            get => _defaultSpeed;
            set { _defaultSpeed = value; RaisePropertyChanged(); }
        }

        [OutputParam("CurrentPointIndex", "当前执行点位序号")]
        public int CurrentPointIndex { get; set; }

        [OutputParam("CurrentPointName", "当前执行点位名称")]
        public string CurrentPointName { get; set; } = string.Empty;

        [OutputParam(CurrentPointCoordinateParamName, "Current point coordinate")]
        public CoordinatePos CurrentPointCoordinate { get; set; } = new CoordinatePos
        {
            TargetPos = new List<double> { 0.0, 0.0, 0.0, 0.0, 0.0 }
        };

        [OutputParam(CurrentPointXParamName, "Current point X coordinate")]
        public double CurrentPointX { get; set; }

        [OutputParam(CurrentPointYParamName, "Current point Y coordinate")]
        public double CurrentPointY { get; set; }

        [OutputParam(CurrentPointZParamName, "Current point Z coordinate")]
        public double CurrentPointZ { get; set; }

        [OutputParam(CurrentPointZ1ParamName, "Current point Z1 coordinate")]
        public double CurrentPointZ1 { get; set; }

        [OutputParam(CurrentPointZ2ParamName, "Current point Z2 coordinate")]
        public double CurrentPointZ2 { get; set; }

        [OutputParam("TotalExecutionCount", "本节点累计执行次数")]
        public int TotalExecutionCount { get; set; }

        [OutputParam("MoveCompleted", "本次点位是否移动完成")]
        public bool MoveCompleted { get; set; }

        [OutputParam("LastMessage", "最后一次执行消息")]
        public string LastMessage { get; set; } = string.Empty;

        public PointSequenceMotionModel()
        {
            EnsureTrigger();
            EnsureDefaultPoints();
            RefreshControlCardContext();
        }

        public override bool OnceInit()
        {
            EnsureTrigger();
            EnsureDefaultPoints();
            RefreshControlCardContext();
            EnsureCoordinateOutputParams(Guid);
            RefreshOutputParams();
            return base.OnceInit();
        }

        public override void InitOutputParamResource(System.Guid linkGuid)
        {
            base.InitOutputParamResource(linkGuid);
            EnsureCoordinateOutputParams(linkGuid);
            RefreshOutputParams();
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
        }

        public void SetPointCount(int count)
        {
            count = Math.Max(1, count);

            while (Points.Count < count)
            {
                AddPoint();
            }

            while (Points.Count > count)
            {
                Points.RemoveAt(Points.Count - 1);
            }

            NormalizePointNames();
            if (SelectedPoint == null && Points.Count > 0)
            {
                SelectedPoint = Points[0];
            }

            if (NextInternalIndex >= Points.Count)
            {
                NextInternalIndex = 0;
            }
        }

        public PointSequenceItem AddPoint()
        {
            var point = new PointSequenceItem
            {
                Name = $"P{Points.Count + 1}",
                Description = $"点位{Points.Count + 1}"
            };

            Points.Add(point);
            SelectedPoint = point;
            return point;
        }

        public void RemoveSelectedPoint()
        {
            if (SelectedPoint == null)
            {
                return;
            }

            int index = Points.IndexOf(SelectedPoint);
            if (index < 0)
            {
                return;
            }

            Points.RemoveAt(index);
            NormalizePointNames();

            if (Points.Count == 0)
            {
                SelectedPoint = null;
                NextInternalIndex = 0;
                return;
            }

            SelectedPoint = Points[Math.Min(index, Points.Count - 1)];
            if (NextInternalIndex >= Points.Count)
            {
                NextInternalIndex = 0;
            }
        }

        public void ResetCycle()
        {
            NextInternalIndex = 0;
            CurrentPointIndex = 0;
            CurrentPointName = string.Empty;
            MoveCompleted = false;
            LastMessage = "点位循环已重置";
            RefreshOutputParams();
        }

        public bool MoveSelectedPoint(out string message)
        {
            LoadKeyParam();
            RefreshControlCardContext();

            if (SelectedPoint == null)
            {
                message = "未选择点位";
                LastMessage = message;
                return false;
            }

            int originalIndex = Points.IndexOf(SelectedPoint) + 1;
            CurrentPointIndex = originalIndex;
            CurrentPointName = SelectedPoint.Name;
            MoveCompleted = TryMovePoint(SelectedPoint, out message);
            if (MoveCompleted)
            {
                SetCurrentPointOutputs(SelectedPoint);
            }

            LastMessage = message;
            RefreshOutputParams();
            if (MoveCompleted && !UpdateParam())
            {
                Console.WriteLine($"Module_{Serial} update output params failed");
            }

            return MoveCompleted;
        }

        public new async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {
                    LoadKeyParam();
                    RefreshControlCardContext();

                    MoveCompleted = false;

                    if (!TryGetEnabledPoints(out var enabledPoints, out var message))
                    {
                        return Finish(NodeStatus.Error, message);
                    }

                    if (!TryResolvePointIndex(enabledPoints, out int enabledIndex, out message))
                    {
                        return Finish(NodeStatus.NotRun, message);
                    }

                    var point = enabledPoints[enabledIndex];
                    CurrentPointIndex = Points.IndexOf(point) + 1;
                    CurrentPointName = point.Name;

                    if (!TryMovePoint(point, out message))
                    {
                        return Finish(NodeStatus.Error, message);
                    }

                    SetCurrentPointOutputs(point);
                    MoveCompleted = true;
                    TotalExecutionCount++;
                    AdvanceInternalIndex(enabledIndex, enabledPoints.Count);

                    return Finish(NodeStatus.Success, message);
                }
                catch (Exception ex)
                {
                    return Finish(NodeStatus.Error, ex.Message);
                }
            });

            await Task.CompletedTask;

            return Output = new ExecuteModuleOutput
            {
                RunStatus = result,
                RunTime = time,
            };
        }

        private void EnsureTrigger()
        {
            TriggerModuleRun = () => ExecuteModule().Result;
        }

        private void EnsureDefaultPoints()
        {
            if (Points.Count == 0)
            {
                SetPointCount(5);
            }
        }

        private static TransmitParam CreateIntInput(string name, string paramName, string description)
        {
            return new TransmitParam
            {
                Name = name,
                ParamName = paramName,
                Type = DataType.Int,
                Resourece = ResoureceType.None,
                Describe = description
            };
        }

        private bool TryGetEnabledPoints(out List<PointSequenceItem> enabledPoints, out string message)
        {
            enabledPoints = Points.Where(item => item.IsEnabled).ToList();
            if (enabledPoints.Count == 0)
            {
                message = "未配置启用点位";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private bool TryResolvePointIndex(
            IReadOnlyList<PointSequenceItem> enabledPoints,
            out int enabledIndex,
            out string message)
        {
            enabledIndex = 0;
            message = string.Empty;

            int rawIndex;
            switch (SelectMode)
            {
                case PointSequenceSelectMode.执行次数:
                    if (!TryReadInputIndex(ExecuteCountInput, out rawIndex, out message))
                    {
                        return false;
                    }
                    break;

                case PointSequenceSelectMode.点位序号:
                    if (!TryReadInputIndex(TriggerPointIndexInput, out rawIndex, out message))
                    {
                        return false;
                    }
                    break;

                default:
                    rawIndex = NextInternalIndex;
                    break;
            }

            if (IndexStartsAtOne && SelectMode != PointSequenceSelectMode.内部循环)
            {
                rawIndex--;
            }

            if (rawIndex < 0)
            {
                message = $"点位序号无效：{rawIndex + (IndexStartsAtOne ? 1 : 0)}";
                return false;
            }

            if (rawIndex >= enabledPoints.Count)
            {
                if (!LoopWhenOverflow)
                {
                    message = $"点位序号超出范围：{rawIndex + (IndexStartsAtOne ? 1 : 0)}，启用点位数量 {enabledPoints.Count}";
                    return false;
                }

                rawIndex %= enabledPoints.Count;
            }

            enabledIndex = rawIndex;
            return true;
        }

        private bool TryReadInputIndex(TransmitParam input, out int index, out string message)
        {
            index = 0;
            message = string.Empty;

            object? value = input?.Value;
            if (value == null)
            {
                message = $"未读取到{input?.Name ?? "外部索引"}";
                return false;
            }

            if (value is int intValue)
            {
                index = intValue;
                return true;
            }

            if (value is long longValue)
            {
                index = checked((int)longValue);
                return true;
            }

            if (value is double doubleValue)
            {
                index = (int)Math.Round(doubleValue);
                return true;
            }

            if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                || int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture, out parsed))
            {
                index = parsed;
                return true;
            }

            message = $"{input?.Name ?? "外部索引"}无法转换为整数：{value}";
            return false;
        }

        private bool TryMovePoint(PointSequenceItem point, out string message)
        {
            message = string.Empty;

            if (ControlCard == null)
            {
                message = "请先选择控制卡";
                return false;
            }

            var targets = point.ToTargetDictionary(UseX, UseY, UseZ, UseZ1, UseZ2);
            if (targets.Count == 0)
            {
                message = "未选择参与运动的轴";
                return false;
            }

            if (!ControlCard.ValidateLimitPosition(targets, out var limitMessage))
            {
                message = limitMessage;
                return false;
            }

            var interpoAxes = new List<En_AxisNum>();
            if (UseX && targets.ContainsKey(En_AxisNum.X)) interpoAxes.Add(En_AxisNum.X);
            if (UseY && targets.ContainsKey(En_AxisNum.Y)) interpoAxes.Add(En_AxisNum.Y);

            var targetPos = point.ToTargetArray();
            bool moved;
            if (interpoAxes.Count >= 2)
            {
                moved = ControlCard.CustomInterpolationMoving(new CustomInterPoParam
                {
                    IsSafetyMoving = IsSafetyMoving,
                    InterPoAxiss = interpoAxes,
                    DefaultSpeed = DefaultSpeed,
                    TargetPos = targetPos,
                    TargetPosDic = targets,
                    waitforend = true,
                }, () =>
                {
                    return ControlCard.LineInterpoMoving(new LineInterPoParam
                    {
                        IsSafetyMoving = IsSafetyMoving,
                        InterPoAxiss = interpoAxes,
                        DefaultSpeed = DefaultSpeed,
                        TargetPos = targetPos,
                        TargetPosDic = targets,
                        decZSpeed = new double[] { 5, 10, 50 },
                        upZSpeed = new double[] { 5, 10, 50 },
                        waitforend = true,
                    })
                        ? "OK"
                        : "NG";
                }, true);
            }
            else
            {
                moved = MoveAxesOneByOne(targets);
            }

            message = moved
                ? $"点位 {point.Name} 移动完成"
                : $"点位 {point.Name} 移动失败";

            return moved;
        }

        private bool MoveAxesOneByOne(IReadOnlyDictionary<En_AxisNum, double> targets)
        {
            foreach (var target in targets)
            {
                if (!ControlCard!.MoveAbsoluteAxis(target.Key, target.Value, true))
                {
                    return false;
                }
            }

            return true;
        }

        private void AdvanceInternalIndex(int executedEnabledIndex, int enabledCount)
        {
            if (SelectMode != PointSequenceSelectMode.内部循环)
            {
                return;
            }

            int nextIndex = executedEnabledIndex + 1;
            if (nextIndex >= enabledCount && LoopWhenOverflow)
            {
                nextIndex = 0;
            }

            NextInternalIndex = nextIndex;
        }

        private void SetCurrentPointOutputs(PointSequenceItem point)
        {
            CurrentPointX = point.X;
            CurrentPointY = point.Y;
            CurrentPointZ = point.Z;
            CurrentPointZ1 = point.Z1;
            CurrentPointZ2 = point.Z2;
            CurrentPointCoordinate = new CoordinatePos
            {
                Name = point.Name,
                Describe = point.PositionDescription,
                TargetPos = point.ToTargetArray().ToList()
            };

            RaisePropertyChanged(nameof(CurrentPointX));
            RaisePropertyChanged(nameof(CurrentPointY));
            RaisePropertyChanged(nameof(CurrentPointZ));
            RaisePropertyChanged(nameof(CurrentPointZ1));
            RaisePropertyChanged(nameof(CurrentPointZ2));
            RaisePropertyChanged(nameof(CurrentPointCoordinate));
        }

        private void EnsureCoordinateOutputParams(System.Guid linkGuid)
        {
            if (OutputParams == null)
            {
                OutputParams = new ObservableCollection<TransmitParam>();
            }

            foreach (var definition in CoordinateOutputParamDefinitions)
            {
                var outputParam = OutputParams.FirstOrDefault(item =>
                    string.Equals(item?.ParamName, definition.ParamName, StringComparison.OrdinalIgnoreCase));

                if (outputParam == null)
                {
                    outputParam = new TransmitParam
                    {
                        LinkGuid = ResolveCoordinateOutputLinkGuid(linkGuid, System.Guid.Empty),
                        ParamName = definition.ParamName,
                        Serial = Serial,
                        Name = BuildCoordinateOutputName(definition.ParamName),
                        Type = definition.Type,
                        Resourece = ResoureceType.None,
                        Describe = definition.Description,
                        ResourcePath = GetCoordinateOutputResourcePath(definition.ParamName),
                        IsGlobal = true,
                    };

                    OutputParams.Add(outputParam);
                    continue;
                }

                outputParam.LinkGuid = ResolveCoordinateOutputLinkGuid(linkGuid, outputParam.LinkGuid);
                outputParam.Serial = Serial;
                outputParam.Type = definition.Type;
                outputParam.Resourece = ResoureceType.None;
                outputParam.Describe = definition.Description;
                outputParam.ResourcePath = GetCoordinateOutputResourcePath(definition.ParamName);
                outputParam.IsGlobal = true;

                if (string.IsNullOrWhiteSpace(outputParam.Name)
                    || outputParam.Name == definition.ParamName
                    || outputParam.Name.StartsWith("-999_", StringComparison.Ordinal))
                {
                    outputParam.Name = BuildCoordinateOutputName(definition.ParamName);
                }
            }
        }

        private string BuildCoordinateOutputName(string paramName)
        {
            return Serial >= 0
                ? $"{Serial}_PointSequenceMotion_{paramName}"
                : $"PointSequenceMotion_{paramName}";
        }

        private System.Guid ResolveCoordinateOutputLinkGuid(System.Guid requestedLinkGuid, System.Guid existingLinkGuid)
        {
            if (requestedLinkGuid != System.Guid.Empty
                && (requestedLinkGuid != Guid || existingLinkGuid == System.Guid.Empty))
            {
                return requestedLinkGuid;
            }

            return existingLinkGuid != System.Guid.Empty ? existingLinkGuid : Guid;
        }

        private static string GetCoordinateOutputResourcePath(string paramName)
        {
            return $"{typeof(PointSequenceMotionModel).FullName}.{paramName}";
        }

        private NodeStatus Finish(NodeStatus status, string message)
        {
            LastMessage = message;
            RefreshOutputParams();

            if (!UpdateParam())
            {
                Console.WriteLine($"模块_{Serial}更新参数失败");
            }

            return status;
        }

        private void RefreshOutputParams()
        {
            EnsureCoordinateOutputParams(Guid);
            var values = OutputParamCollector.GetDataPointValues(this);
            foreach (var item in OutputParams)
            {
                if (!string.IsNullOrWhiteSpace(item.ParamName)
                    && values.TryGetValue(item.ParamName, out var value))
                {
                    item.Value = value;
                }
            }
        }

        private void NormalizePointNames()
        {
            for (int i = 0; i < Points.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(Points[i].Name))
                {
                    Points[i].Name = $"P{i + 1}";
                }

                if (string.IsNullOrWhiteSpace(Points[i].Description))
                {
                    Points[i].Description = $"点位{i + 1}";
                }
            }
        }

        private sealed class CoordinateOutputParamDefinition
        {
            public CoordinateOutputParamDefinition(string paramName, DataType type, string description)
            {
                ParamName = paramName;
                Type = type;
                Description = description;
            }

            public string ParamName { get; }

            public DataType Type { get; }

            public string Description { get; }
        }
    }
}
