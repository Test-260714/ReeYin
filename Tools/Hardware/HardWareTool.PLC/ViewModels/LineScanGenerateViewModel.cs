using HardWareTool.PLC.Models;
using Prism.Dialogs;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.IOC;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace HardWareTool.PLC.ViewModels
{
    public class LineScanGenerateViewModel : DialogViewModelBase
    {
        private const string AppendMode = "Append";
        private const string ReplaceMode = "Replace";
        private const int MaxGeneratedOrderCount = 10000;
        private bool _isUpdatingPreview;

        private PLCModel? _modelParam;
        public new PLCModel? ModelParam
        {
            get { return _modelParam; }
            set { SetProperty(ref _modelParam, value); }
        }

        private PLCLineScanGenerateModel _lineScan = new PLCLineScanGenerateModel();
        public PLCLineScanGenerateModel LineScan
        {
            get { return _lineScan; }
            set { SetProperty(ref _lineScan, value); }
        }

        private ObservableCollection<PLCBase> _plcDevices = new ObservableCollection<PLCBase>();
        public ObservableCollection<PLCBase> PlcDevices
        {
            get { return _plcDevices; }
            set { SetProperty(ref _plcDevices, value); }
        }

        private PLCBase? _selectedPlc;
        public PLCBase? SelectedPlc
        {
            get { return _selectedPlc; }
            set
            {
                if (!SetProperty(ref _selectedPlc, value))
                    return;

                LineScan.TargetPlcId = value?.Config?.GetID() ?? string.Empty;
                AxisGroups = value?.AxisGroups ?? new ObservableCollection<PLCAxisGroup>();
                SelectedAxisGroup = AxisGroups.FirstOrDefault(group =>
                    string.Equals(group.GroupName, LineScan.AxisGroupName, StringComparison.OrdinalIgnoreCase)) ??
                    AxisGroups.FirstOrDefault();
                UpdatePreview();
            }
        }

        private ObservableCollection<PLCAxisGroup> _axisGroups = new ObservableCollection<PLCAxisGroup>();
        public ObservableCollection<PLCAxisGroup> AxisGroups
        {
            get { return _axisGroups; }
            set { SetProperty(ref _axisGroups, value); }
        }

        private PLCAxisGroup? _selectedAxisGroup;
        public PLCAxisGroup? SelectedAxisGroup
        {
            get { return _selectedAxisGroup; }
            set
            {
                if (!SetProperty(ref _selectedAxisGroup, value))
                    return;

                LineScan.AxisGroupName = value?.GroupName ?? string.Empty;
                UpdatePreview();
            }
        }

        public LineScanGenerateViewModel()
        {
            LineScan.PropertyChanged += OnLineScanPropertyChanged;
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            ModelParam = parameters.GetValue<PLCModel>("Param");
            string currentPlcId = parameters.GetValue<string>("CurrentPlcId");
            string currentAxisGroupName = parameters.GetValue<string>("CurrentAxisGroupName");

            LoadPlcDevices();

            SelectedPlc = PlcDevices.FirstOrDefault(plc =>
                string.Equals(plc.Config.GetID(), currentPlcId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(plc.Config.DisplayName, currentPlcId, StringComparison.OrdinalIgnoreCase)) ??
                PlcDevices.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(currentAxisGroupName))
            {
                SelectedAxisGroup = AxisGroups.FirstOrDefault(group =>
                    string.Equals(group.GroupName, currentAxisGroupName, StringComparison.OrdinalIgnoreCase)) ??
                    SelectedAxisGroup;
            }

            UpdatePreview();
        }

        public DelegateCommand ReadLeftTopCommand => new DelegateCommand(() =>
        {
            if (!TryReadCurrentPoint(out double currentX, out double currentY))
                return;

            LineScan.LeftTopX = currentX;
            LineScan.LeftTopY = currentY;
            UpdatePreview();
        });

        public DelegateCommand ReadRightBottomCommand => new DelegateCommand(() =>
        {
            if (!TryReadCurrentPoint(out double currentX, out double currentY))
                return;

            LineScan.RightBottomX = currentX;
            LineScan.RightBottomY = currentY;
            UpdatePreview();
        });

        public DelegateCommand AppendCommand => new DelegateCommand(() => GenerateOrders(AppendMode));

        public DelegateCommand ReplaceCommand => new DelegateCommand(() => GenerateOrders(ReplaceMode));

        public DelegateCommand CancelCommand => new DelegateCommand(() =>
        {
            CloseDialog(ButtonResult.Cancel);
        });

        private void LoadPlcDevices()
        {
            var plcSet = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
            PlcDevices = plcSet?.Models ?? new ObservableCollection<PLCBase>();
        }

        private bool TryReadCurrentPoint(out double currentX, out double currentY)
        {
            currentX = 0;
            currentY = 0;

            if (SelectedPlc == null || SelectedAxisGroup == null)
            {
                MessageView.Ins.MessageBoxShow("请先选择PLC和轴组。", eMsgType.Error);
                return false;
            }

            if (!SelectedPlc.Config.IsConnected)
            {
                MessageView.Ins.MessageBoxShow("请先连接PLC。", eMsgType.Error);
                return false;
            }

            if (!TryGetAxis(SelectedAxisGroup, EnumAxisType.X, out var xAxis) ||
                !TryGetAxis(SelectedAxisGroup, EnumAxisType.Y, out var yAxis))
            {
                MessageView.Ins.MessageBoxShow("当前轴组未找到X/Y轴配置。", eMsgType.Error);
                return false;
            }

            if (!SelectedPlc.TryReadPointValue(xAxis!.MotionConfig.CurrentPosRead, out var xValue) ||
                !SelectedPlc.TryReadPointValue(yAxis!.MotionConfig.CurrentPosRead, out var yValue))
                {
                MessageView.Ins.MessageBoxShow("读取当前X/Y坐标失败，请先确认当前位置读取地址。", eMsgType.Error);
                return false;
            }

            currentX = Convert.ToDouble(xValue, CultureInfo.InvariantCulture);
            currentY = Convert.ToDouble(yValue, CultureInfo.InvariantCulture);
            return true;
        }

        private void GenerateOrders(string mode)
        {
            if (!TryBuildScanLines(out var scanLines, out string errorMessage))
            {
                MessageView.Ins.MessageBoxShow(errorMessage, eMsgType.Error);
                return;
            }

            var orders = BuildOrders(scanLines);
            if (orders.Count == 0)
            {
                MessageView.Ins.MessageBoxShow("没有生成任何轨迹指令。", eMsgType.Warn);
                return;
            }

            CloseDialog(ButtonResult.OK, new DialogParameters
            {
                { "Orders", orders },
                { "Mode", mode },
            });
        }

        private bool TryBuildScanLines(out List<(int RowIndex, double StartX, double EndX, double Y)> scanLines, out string errorMessage)
        {
            scanLines = new List<(int RowIndex, double StartX, double EndX, double Y)>();
            errorMessage = string.Empty;

            if (SelectedPlc == null)
            {
                errorMessage = "请选择PLC设备。";
                return false;
            }

            if (SelectedAxisGroup == null)
            {
                errorMessage = "请选择轴组。";
                return false;
            }

            if (!TryGetAxis(SelectedAxisGroup, EnumAxisType.X, out _) ||
                !TryGetAxis(SelectedAxisGroup, EnumAxisType.Y, out _))
            {
                errorMessage = "当前轴组缺少X/Y轴配置。";
                return false;
            }

            if (LineScan.StepY <= 0)
            {
                errorMessage = "Y步距必须大于0。";
                return false;
            }

            if (!TryGetScanBounds(out double startX, out double endX, out double startY, out double endY, out errorMessage))
                return false;

            int lineCount = CalculateScanLineCount(startY, endY);
            int orderCount = CalculateOrderCount(lineCount);
            if (orderCount > MaxGeneratedOrderCount)
            {
                errorMessage = $"预计生成{orderCount:N0}条指令，超过上限{MaxGeneratedOrderCount:N0}，请增大Y步距或缩小范围。";
                return false;
            }

            double yDirection = Math.Sign(endY - startY);
            for (int i = 0; i < lineCount; i++)
            {
                double y = startY + yDirection * LineScan.StepY * i;
                bool leftToRight = i % 2 == 0;
                scanLines.Add((i + 1, leftToRight ? startX : endX, leftToRight ? endX : startX, y));
            }

            return true;
        }

        private List<PLCOrder> BuildOrders(List<(int RowIndex, double StartX, double EndX, double Y)> scanLines)
        {
            var orders = new List<PLCOrder>();
            if (SelectedPlc == null || SelectedAxisGroup == null)
                return orders;

            for (int i = 0; i < scanLines.Count; i++)
            {
                var line = scanLines[i];
                if (i == 0)
                {
                    orders.Add(CreatePositionOrder($"第{line.RowIndex}行定位", line.StartX, line.Y, LineScan.OffsetSpeed));
                    if (LineScan.OffsetStableDelayMs > 0)
                    {
                        orders.Add(CreateDelayOrder($"第{line.RowIndex}行定位后延时", LineScan.OffsetStableDelayMs));
                    }
                }
                else
                {
                    orders.Add(CreateOffsetOrder($"第{line.RowIndex}行偏移", line.Y, LineScan.OffsetSpeed));
                    if (LineScan.OffsetStableDelayMs > 0)
                    {
                        orders.Add(CreateDelayOrder($"第{line.RowIndex}行偏移后延时", LineScan.OffsetStableDelayMs));
                    }
                }

                orders.Add(CreateEventOrder($"第{line.RowIndex}行开始采集", LineScan.StartCollectEventName, CreateLineScanSegmentInfo(line, "Start")));
                orders.Add(CreateScanOrder($"第{line.RowIndex}行扫描", line.EndX, LineScan.ScanSpeed));

                if (LineScan.StopCollectDelayMs > 0)
                {
                    orders.Add(CreateDelayOrder($"第{line.RowIndex}行停止采集前延时", LineScan.StopCollectDelayMs));
                }

                orders.Add(CreateEventOrder($"第{line.RowIndex}行停止采集", LineScan.StopCollectEventName, CreateLineScanSegmentInfo(line, "Stop")));
            }

            return orders;
        }

        private PLCOrder CreatePositionOrder(string description, double targetX, double targetY, double speed)
        {
            return CreateAxisOrder(description, speed, axis =>
            {
                if (axis.AxisType == EnumAxisType.X)
                    return (true, targetX);

                if (axis.AxisType == EnumAxisType.Y)
                    return (true, targetY);

                return (false, 0d);
            });
        }

        private PLCOrder CreateScanOrder(string description, double targetX, double speed)
        {
            return CreateAxisOrder(description, speed, axis =>
            {
                if (axis.AxisType == EnumAxisType.X)
                    return (true, targetX);

                return (false, 0d);
            });
        }

        private PLCOrder CreateOffsetOrder(string description, double targetY, double speed)
        {
            return CreateAxisOrder(description, speed, axis =>
            {
                if (axis.AxisType == EnumAxisType.Y)
                    return (true, targetY);

                return (false, 0d);
            });
        }

        private PLCOrder CreateAxisOrder(string description, double speed, Func<PLCAxisItem, (bool IsUsing, double TargetPosition)> axisSelector)
        {
            var order = new PLCOrder
            {
                IsUsing = true,
                OperationType = OperationType.轴操作,
                Describe = description,
                TargetPlcId = SelectedPlc?.Config.GetID() ?? string.Empty,
                AxisGroupName = SelectedAxisGroup?.GroupName ?? string.Empty,
                WaitMoveDone = LineScan.WaitMoveDone,
                MoveTimeoutMs = LineScan.MoveTimeoutMs,
            };

            foreach (var axis in SelectedAxisGroup?.AxisItems ?? Enumerable.Empty<PLCAxisItem>())
            {
                var axisConfig = axisSelector(axis);
                var item = new PLCOrderAxisMoveItem
                {
                    AxisName = axis.AxisName,
                    AxisType = axis.AxisType,
                    IsUsing = axisConfig.IsUsing,
                    RunSpeed = speed,
                    Acc = LineScan.Acc,
                    Dec = LineScan.Dec,
                    TargetPosition = axisConfig.TargetPosition,
                };

                order.AxisMoveItems.Add(item);
            }

            return order;
        }

        private PLCOrder CreateEventOrder(string description, string eventName, PLCLineScanSegmentInfo? segmentInfo = null)
        {
            return new PLCOrder
            {
                IsUsing = true,
                OperationType = OperationType.触发事件,
                Describe = description,
                SltEventName = eventName,
                TargetPlcId = SelectedPlc?.Config.GetID() ?? string.Empty,
                AxisGroupName = SelectedAxisGroup?.GroupName ?? string.Empty,
                PublishLineScanSegment = segmentInfo != null,
                LineScanSegmentInfo = segmentInfo,
            };
        }

        private PLCLineScanSegmentInfo CreateLineScanSegmentInfo((int RowIndex, double StartX, double EndX, double Y) line, string stage)
        {
            return new PLCLineScanSegmentInfo
            {
                RowIndex = line.RowIndex,
                Stage = stage,
                StartX = line.StartX,
                StartY = line.Y,
                EndX = line.EndX,
                EndY = line.Y,
                Direction = line.EndX >= line.StartX ? "PositiveX" : "NegativeX",
            };
        }

        private static PLCOrder CreateDelayOrder(string description, int delayMs)
        {
            return new PLCOrder
            {
                IsUsing = true,
                OperationType = OperationType.延时操作,
                Describe = description,
                WaitDelay = delayMs,
            };
        }

        private void UpdatePreview()
        {
            if (_isUpdatingPreview)
                return;

            _isUpdatingPreview = true;
            try
            {
                if (!TryGetScanBounds(out _, out _, out double startY, out double endY, out _))
                {
                    LineScan.PreviewLineCount = 0;
                    LineScan.PreviewOrderCount = 0;
                    return;
                }

                int lineCount = CalculateScanLineCount(startY, endY);
                LineScan.PreviewLineCount = lineCount;
                LineScan.PreviewOrderCount = CalculateOrderCount(lineCount);
            }
            finally
            {
                _isUpdatingPreview = false;
            }
        }

        private bool TryGetScanBounds(out double startX, out double endX, out double startY, out double endY, out string errorMessage)
        {
            startX = 0;
            endX = 0;
            startY = 0;
            endY = 0;
            errorMessage = string.Empty;

            startX = LineScan.LeftTopX;
            endX = LineScan.RightBottomX;
            startY = LineScan.LeftTopY;
            endY = LineScan.RightBottomY;

            if (Math.Abs(endX - startX) < 0.000001)
            {
                errorMessage = "X方向范围不能为0。";
                return false;
            }

            if (Math.Abs(endY - startY) < 0.000001)
            {
                errorMessage = "Y方向范围不能为0。";
                return false;
            }

            return true;
        }

        private int CalculateScanLineCount(double startY, double endY)
        {
            double range = Math.Abs(endY - startY);
            return (int)Math.Ceiling(range / LineScan.StepY) + 1;
        }

        private int CalculateOrderCount(int lineCount)
        {
            int orderPerLine = LineScan.StopCollectDelayMs > 0 ? 5 : 4;
            int stableDelayCount = LineScan.OffsetStableDelayMs > 0 ? lineCount : 0;
            return lineCount * orderPerLine + stableDelayCount;
        }

        private static bool TryGetAxis(PLCAxisGroup axisGroup, EnumAxisType axisType, out PLCAxisItem? axisItem)
        {
            axisItem = axisGroup.AxisItems?.FirstOrDefault(axis => axis.AxisType == axisType);
            if (axisItem != null)
                return true;

            axisItem = axisGroup.AxisItems?.FirstOrDefault(axis =>
                string.Equals(axis.AxisName, axisType.ToString(), StringComparison.OrdinalIgnoreCase));
            return axisItem != null;
        }

        private void OnLineScanPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PLCLineScanGenerateModel.PreviewLineCount) ||
                e.PropertyName == nameof(PLCLineScanGenerateModel.PreviewOrderCount))
                return;

            UpdatePreview();
        }
    }
}
