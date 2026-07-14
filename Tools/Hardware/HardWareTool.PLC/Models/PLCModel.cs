//using Custom.MFDJC;
using Microsoft.Win32;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using ReeYin_V.Core.Config;
using ReeYin_V.Core.Enums;
using ReeYin_V.Core.Events;
using ReeYin_V.Core.Extension;
using ReeYin_V.Core.Helper;
using ReeYin_V.Core.Interfaces;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.DataCollectRelated;
using ReeYin_V.Core.Services.Module;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Hardware.PLC.Models;
using ReeYin_V.Hardware.PLC.Services;
using ReeYin_V.Share;
using ReeYin_V.UI;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HardWareTool.PLC.Models
{
    [Serializable]
    public class PLCModel : ModelParamBase
    {
        #region Fields
        private const string StartCollectEventName = "TrrigerStartCollect";
        private const string StopCollectEventName = "TrrigerStopCollect";

        [JsonIgnore]
        private PLCLineScanSegmentInfo? _currentLineScanSegment;

        #endregion

        #region Properties
        [JsonIgnore]
        public PLCBase CurPLC { get; set; }

        [JsonIgnore]
        public override Func<ExecuteModuleOutput> TriggerModuleRun { get; set; }

        [JsonIgnore]
        private ObservableCollection<PLCOrder> _plcOrder = new ObservableCollection<PLCOrder>();
        /// <summary>
        /// PLC指令集合
        /// </summary>
        [RecipeParam("PLCOrder", "PLC指令合集", RequiresPageEditor = true, EditorPageName = "PLCView")]
        public ObservableCollection<PLCOrder> PLCOrder
        {
            get { return _plcOrder; }
            set { _plcOrder = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        private PLCAixsGroupModel _axisGroupSetting;
        /// <summary>
        /// 轴组设置
        /// </summary>
        public PLCAixsGroupModel AxisGroupSetting
        {
            get { return _axisGroupSetting; }
            set { _axisGroupSetting = value; }
        }

        [JsonIgnore]
        private PLCOrder _sltPLCOrder;
        /// <summary>
        /// 选中的PLC指令
        /// </summary>
        [JsonIgnore]
        public PLCOrder SltPLCOrder
        {
            get { return _sltPLCOrder; }
            set { _sltPLCOrder = value; RaisePropertyChanged(); }
        }

        [JsonIgnore]
        public ObservableCollection<EventSubscriptionInfo> AllEvents;

        #endregion

        #region Constructor
        public PLCModel()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel ?? new PLCSetModel();

            if (models.Models.Count > 0)
                CurPLC = models.Models[0];
        }
        #endregion

        #region Override
        public override bool LoadKeyParam()
        {
            return base.LoadKeyParam();
        }

        public override bool OnceInit()
        {
            try
            {
                if (IsOnceInit)
                {
                    return true;
                }

                if (!base.OnceInit())
                {
                    return false;
                }

                InitializeMotionServices();

                if (TriggerModuleRun == null)
                {
                    TriggerModuleRun ??= () =>
                    {
                        return ExecuteModule().Result;
                    };
                }

                IsOnceInit = true;
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

        }
        #endregion

        #region Commands


        #endregion

        #region Methods
        /// <summary>
        /// 模块执行
        /// </summary>
        /// <returns></returns>
        public async Task<ExecuteModuleOutput> ExecuteModule()
        {
            var (result, time) = SetTimeHelper.SetTimer(() =>
            {
                try
                {

                    LoadKeyParam();

                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLC模块开始执行：Serial={Serial}, OrderCount={PLCOrder?.Count ?? 0}, Thread={Thread.CurrentThread.ManagedThreadId}");
                    int orderIndex = 0;
                    foreach (var Order in PLCOrder)
                    {
                        orderIndex++;
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLC指令[{orderIndex}]：Type={Order.OperationType}, Describe={Order.Describe}, IsUsing={Order.IsUsing}, WaitMoveDone={Order.WaitMoveDone}");
                        if (Order.IsUsing)
                        {
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLC指令[{orderIndex}]开始执行：{Order.Describe}");
                            var tempStatus = SingleExecute(Order);
                            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLC指令[{orderIndex}]执行完成：{Order.Describe}, Status={tempStatus}");
                            if (tempStatus != NodeStatus.Success)
                                return tempStatus;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace.ToString());
                    return NodeStatus.Error;
                }

                Console.WriteLine($"开始输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                //执行后对输出参数重新赋值
                foreach (var item in OutputParams)
                {
                    item.Value = OutputParamCollector.GetDataPointValues(this)[item.ParamName];
                    Console.WriteLine(JsonHelper.Serialize(item.Value));
                }

                #region 输出
                if (!UpdateParam())
                {
                    Console.WriteLine($"模块_{Serial}更新参数失败");
                }
                Console.WriteLine($"结束输出参数：{DateTime.Now.ToString($"HH:mm:ss.fff")}");
                #endregion

                return NodeStatus.Success;
            });

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}：运动模块执行时间：{time} 毫秒");
            return Output = new ExecuteModuleOutput()
            {
                RunStatus = result,
                RunTime = time,
            };

        }

        /// <summary>
        /// 导入PLC配置
        /// </summary>
        public void ImportConfig()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "导入PLC指令",
                    Filter = "Json文件|*.json|所有文件|*.*",
                    DefaultExt = ".json",
                    CheckFileExists = true
                };

                if (openFileDialog.ShowDialog() != true)
                    return;

                var orders = JsonHelper.JsonFileToList<PLCOrder>(openFileDialog.FileName);
                if (orders == null)
                {
                    MessageView.Ins.MessageBoxShow("导入失败，文件内容为空或格式不正确。", eMsgType.Info);
                    return;
                }

                PLCOrder = new ObservableCollection<PLCOrder>(orders);
                SltPLCOrder = null;
                MessageView.Ins.MessageBoxShow("导入配置成功！", eMsgType.Info);
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"导入配置失败：{ex.Message}", eMsgType.Error);
            }
        }

        /// <summary>
        /// 导出PLC配置
        /// </summary>
        public void ExportConfig()
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Title = "导出PLC指令",
                    Filter = "Json文件|*.json|所有文件|*.*",
                    DefaultExt = ".json",
                    AddExtension = true,
                    FileName = "PLCConfig.json"
                };

                if (saveFileDialog.ShowDialog() != true)
                    return;

                JsonHelper.ListToJsonFile(PLCOrder?.ToList() ?? new List<PLCOrder>(), saveFileDialog.FileName);
                MessageView.Ins.MessageBoxShow("导出配置成功！", eMsgType.Info);
            }
            catch (Exception ex)
            {
                MessageView.Ins.MessageBoxShow($"导出配置失败：{ex.Message}", eMsgType.Error);
            }
        }

        /// <summary>
        /// 单步执行
        /// </summary>
        /// <returns></returns>
        public NodeStatus SingleExecute(PLCOrder Order)
        {
            try
            {
                switch (Order.OperationType)
                {
                    case OperationType.读单个地址:
                        {
                            var Param = new PLCParaInfoModel
                            {
                                PLCAddress = Order.Addr,
                                ParaType = Order.ParamType
                            };
                            //先读取一遍
                            CurPLC.ReadPLCPara(Param);
                            if (Param.ParaValue == null) return NodeStatus.Error;


                            //通知数据更新
                            if (Order.IsUsingPublish)
                                 PrismProvider.EventAggregator.GetEvent<OutputResultEvent>().Publish(("PLC", Param.ParaValue));

                            Console.WriteLine($"地址：{Param.PLCAddress}，值为：{Param.ParaValue}");
                            if (Order.IsUsingJedge)
                            {
                                //等于设定值直接跳出
                                if (Param.ParaValue != null && Order.JudgeValue != null && Param.ParaValue.ToString() == Order.JudgeValue) return NodeStatus.Success;

                                // 记录循环开始时间（用于计算超时）
                                DateTime startTime = DateTime.Now;
                                TimeSpan timeout = TimeSpan.FromMilliseconds(Order.Delay * 1000);

                                while (true)
                                {
                                    try
                                    {
                                        CurPLC.ReadPLCPara(Param);

                                        if (Param.ParaValue != null && Order.JudgeValue != null && Param.ParaValue.ToString() == Order.JudgeValue) return NodeStatus.Success;

                                        TimeSpan elapsed = DateTime.Now - startTime;
                                        if (elapsed >= timeout)
                                        {
                                            Console.WriteLine($"超出设定时间！");
                                            return NodeStatus.Timeout; // 超时未达到目标，返回失败
                                        }
                                        Task.Delay(50).Wait();
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"读取PLC参数异常：{ex.Message}");
                                        return NodeStatus.Error;
                                    }
                                }
                            }
                        }break;

                    case OperationType.写单个地址:
                        {
                            var Param = new PLCParaInfoModel
                            {
                                PLCAddress = Order.Addr,
                                ParaType = Order.ParamType,
                                ParaValue = Order.Value
                            };
                            if (CurPLC.WritePLCPara(Param))
                            {
                                Console.WriteLine($"地址{Order.Addr}写入{Order.Value}成功！");
                            }

                        }break;

                    case OperationType.轴操作:
                        {
                            var status = ExecuteAxisMove(Order);
                            if (status != NodeStatus.Success)
                                return status;
                        }break;

                    case OperationType.延时操作:
                        {
                            Task.Delay(Order.WaitDelay).Wait();
                        }break;
                    case OperationType.触发事件:
                        {
                            PublishLineScanSegmentIfNeeded(Order);
                            List<string> alleventname = EventSubscriptionRegistry._subscriptions.Select(x => x.MethodName).ToList();
                            if (Order.IsUsing && alleventname.Contains(Order.SltEventName))
                                   PrismProvider.EventAggregator.GetEvent<UpdateMessageEvent>().Publish(Order.SltEventName);
                        }
                        break;
                    default:
                        {

                        }break;
                }
                return NodeStatus.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.StackTrace}");
                return NodeStatus.Error;
            }
        }

        private NodeStatus ExecuteAxisMove(PLCOrder order)
        {
            try
            {
                var plc = GetOrderPlc(order);
                if (plc == null)
                    return LogAxisMoveError($"未找到PLC，TargetPlcId={order.TargetPlcId}");

                var axisGroup = plc.AxisGroups?.FirstOrDefault(group => string.Equals(group.GroupName, order.AxisGroupName, StringComparison.OrdinalIgnoreCase));
                if (axisGroup == null)
                    return LogAxisMoveError($"未找到轴组，AxisGroupName={order.AxisGroupName}");

                var moveItems = order.AxisMoveItems.Where(item => item.IsUsing).ToList();
                if (moveItems.Count == 0)
                    return LogAxisMoveError("没有启用的轴移动项");
                var service = plc.MotionService;

                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 轴操作开始：Describe={order.Describe}, PLC={plc.Config?.DisplayName}, AxisGroup={axisGroup.GroupName}, WaitMoveDone={order.WaitMoveDone}, TimeoutMs={order.MoveTimeoutMs}, EnabledAxes={string.Join(" | ", moveItems.Select(item => $"{item.AxisName}/{item.AxisType}:Target={item.TargetPosition},Speed={item.RunSpeed},Acc={item.Acc},Dec={item.Dec}"))}");
                var movedAxes = new List<(PLCAxisItem Axis, double TargetPosition)>();
                foreach (var item in moveItems)
                {
                    var axis = axisGroup.AxisItems?.FirstOrDefault(a => string.Equals(a.AxisName, item.AxisName, StringComparison.OrdinalIgnoreCase)) ??
                               axisGroup.AxisItems?.FirstOrDefault(a => a.AxisType == item.AxisType);
                    if (axis == null)
                        return LogAxisMoveError($"未找到轴，AxisName={item.AxisName}，AxisType={item.AxisType}");

                    if (item.TargetPosition < axis.MinLimit || item.TargetPosition > axis.MaxLimit)
                        return LogAxisMoveError($"目标位置超出轴限位，Axis={axis.AxisName}，Target={item.TargetPosition}，Min={axis.MinLimit}，Max={axis.MaxLimit}");

                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 下发轴移动开始：Order={order.Describe}, Axis={axis.AxisName}, AxisType={axis.AxisType}, Target={item.TargetPosition}, Speed={item.RunSpeed}, Acc={item.Acc}, Dec={item.Dec}, PosAddr={axis.MotionConfig.RunPositionWrite?.Address}, TriggerAddr={axis.MotionConfig.RunTriggerWrite?.Address}, DoneAddr={axis.MotionConfig.MoveDoneRead?.Address}, ResetDoneAddr={axis.MotionConfig.MoveDoneResetWrite?.Address}, Thread={Thread.CurrentThread.ManagedThreadId}");
                    if (!service.RunAxis(axis,
                            item.TargetPosition,
                            item.RunSpeed > 0 ? item.RunSpeed : null,
                            item.Acc > 0 ? item.Acc : null,
                            item.Dec > 0 ? item.Dec : null))
                        return LogAxisMoveError($"轴移动指令执行失败，Axis={axis.AxisName}");

                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 下发轴移动成功：Order={order.Describe}, Axis={axis.AxisName}, Target={item.TargetPosition}");
                    movedAxes.Add((axis, item.TargetPosition));
                }
                if (!order.WaitMoveDone)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 轴操作未启用等待到位，直接成功：Describe={order.Describe}");
                    return NodeStatus.Success;
                }

                foreach (var item in movedAxes)
                {
                    var status = WaitAxisMoveDone(plc, item.Axis, item.TargetPosition, order.MoveTimeoutMs);
                    if (status != NodeStatus.Success)
                        return LogAxisMoveError($"等待轴移动完成失败，Axis={item.Axis.AxisName}，Status={status}", status);
                }

                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 轴操作完成：Describe={order.Describe}");
                return NodeStatus.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExecuteAxisMove()" + ex.ToString());
                return NodeStatus.Error;
            }

        }

        private void InitializeMotionServices()
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
            if (models?.Models == null || models.Models.Count == 0)
            {
                _ = CurPLC?.MotionService;
                return;
            }

            foreach (var plc in models.Models)
            {
                _ = plc?.MotionService;
            }
        }

        private NodeStatus LogAxisMoveError(string message, NodeStatus status = NodeStatus.Error)
        {
            Console.WriteLine($"PLC轴移动失败：{message}");
            return status;
        }

        private PLCBase GetOrderPlc(PLCOrder order)
        {
            var models = PrismProvider.HardwareModuleManager.Modules[ConfigKey.PLCConfig] as PLCSetModel;
            if (models?.Models == null || models.Models.Count == 0)
                return CurPLC;

            var plc = models.Models.FirstOrDefault(model =>
                string.Equals(model.Config.GetID(), order.TargetPlcId, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(model.Config.DisplayName, order.TargetPlcId, StringComparison.OrdinalIgnoreCase));
            return plc ?? CurPLC;
        }

        private void PublishLineScanSegmentIfNeeded(PLCOrder order)
        {
            if (!IsLineScanCollectEvent(order.SltEventName, out bool isStart))
                return;

            var segmentInfo = BuildLineScanSegmentInfo(order, isStart);
            PrismProvider.EventAggregator.GetEvent<PLCLineScanSegmentEvent>().Publish(segmentInfo);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} 发布线扫段坐标：Stage={segmentInfo.Stage}, Event={segmentInfo.SourceEventName}, Row={segmentInfo.RowIndex}, Start=({segmentInfo.StartX},{segmentInfo.StartY},{segmentInfo.StartZ}), End=({segmentInfo.EndX},{segmentInfo.EndY},{segmentInfo.EndZ}), ReadSuccess={segmentInfo.PositionReadSuccess}");
        }

        private bool IsLineScanCollectEvent(string eventName, out bool isStart)
        {
            isStart = string.Equals(eventName, StartCollectEventName, StringComparison.OrdinalIgnoreCase);
            return isStart || string.Equals(eventName, StopCollectEventName, StringComparison.OrdinalIgnoreCase);
        }

        private PLCLineScanSegmentInfo BuildLineScanSegmentInfo(PLCOrder order, bool isStart)
        {
            bool hasOrderSegment = order.PublishLineScanSegment && order.LineScanSegmentInfo != null;
            var segmentInfo = hasOrderSegment ? CloneLineScanSegmentInfo(order.LineScanSegmentInfo!) : new PLCLineScanSegmentInfo();
            segmentInfo.Stage = isStart ? "Start" : "Stop";
            segmentInfo.SourceEventName = order.SltEventName ?? string.Empty;
            segmentInfo.SourceSerial = Serial;
            segmentInfo.OrderDescribe = order.Describe ?? string.Empty;
            segmentInfo.EventTime = DateTime.Now;

            bool readSuccess = TryReadCurrentCoordinate(order, out double currentX, out double currentY, out double currentZ);
            segmentInfo.PositionReadSuccess = readSuccess;
            if (hasOrderSegment)
            {
                if (readSuccess)
                {
                    segmentInfo.StartZ = currentZ;
                    segmentInfo.EndZ = currentZ;
                }
            }
            else if (isStart)
            {
                if (readSuccess)
                {
                    segmentInfo.StartX = currentX;
                    segmentInfo.StartY = currentY;
                    segmentInfo.StartZ = currentZ;
                    segmentInfo.EndX = currentX;
                    segmentInfo.EndY = currentY;
                    segmentInfo.EndZ = currentZ;
                }
            }
            else
            {
                if (_currentLineScanSegment != null)
                {
                    segmentInfo.StartX = _currentLineScanSegment.StartX;
                    segmentInfo.StartY = _currentLineScanSegment.StartY;
                    segmentInfo.StartZ = _currentLineScanSegment.StartZ;
                }
                else if (readSuccess)
                {
                    segmentInfo.StartX = currentX;
                    segmentInfo.StartY = currentY;
                    segmentInfo.StartZ = currentZ;
                }

                if (readSuccess)
                {
                    segmentInfo.EndX = currentX;
                    segmentInfo.EndY = currentY;
                    segmentInfo.EndZ = currentZ;
                }
            }

            if (string.IsNullOrWhiteSpace(segmentInfo.Direction))
                segmentInfo.Direction = segmentInfo.EndX >= segmentInfo.StartX ? "PositiveX" : "NegativeX";

            if (isStart)
                _currentLineScanSegment = CloneLineScanSegmentInfo(segmentInfo);
            else
                _currentLineScanSegment = null;

            return segmentInfo;
        }

        private PLCLineScanSegmentInfo CloneLineScanSegmentInfo(PLCLineScanSegmentInfo source)
        {
            return new PLCLineScanSegmentInfo
            {
                RowIndex = source.RowIndex,
                Stage = source.Stage,
                SourceEventName = source.SourceEventName,
                StartX = source.StartX,
                StartY = source.StartY,
                StartZ = source.StartZ,
                EndX = source.EndX,
                EndY = source.EndY,
                EndZ = source.EndZ,
                Direction = source.Direction,
                SourceSerial = source.SourceSerial,
                OrderDescribe = source.OrderDescribe,
                EventTime = source.EventTime,
                PositionReadSuccess = source.PositionReadSuccess,
            };
        }

        private bool TryReadCurrentCoordinate(PLCOrder order, out double x, out double y, out double z)
        {
            x = 0;
            y = 0;
            z = 0;
            var plc = GetOrderPlc(order);
            var axisGroup = plc?.AxisGroups?.FirstOrDefault(group => string.Equals(group.GroupName, order.AxisGroupName, StringComparison.OrdinalIgnoreCase)) ??
                            plc?.AxisGroups?.FirstOrDefault();
            if (plc == null || axisGroup == null)
                return false;

            bool xSuccess = TryReadAxisPosition(plc, axisGroup, EnumAxisType.X, out x);
            bool ySuccess = TryReadAxisPosition(plc, axisGroup, EnumAxisType.Y, out y);
            bool zSuccess = TryReadAxisPosition(plc, axisGroup, EnumAxisType.ZTop, out z) ||
                            TryReadAxisPosition(plc, axisGroup, EnumAxisType.ZBottom, out z);
            return xSuccess && ySuccess && zSuccess;
        }

        private bool TryReadAxisPosition(PLCBase plc, PLCAxisGroup axisGroup, EnumAxisType axisType, out double position)
        {
            position = 0;
            var axis = axisGroup.AxisItems?.FirstOrDefault(item => item.AxisType == axisType);
            if (axis == null || string.IsNullOrWhiteSpace(axis.MotionConfig.CurrentPosRead?.Address))
                return false;

            if (!plc.TryReadPointValue(axis.MotionConfig.CurrentPosRead, out var value))
                return false;

            try
            {
                position = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
        private NodeStatus WaitAxisMoveDone(PLCBase plc, PLCAxisItem axis, double targetPosition, int timeoutMs)
        {
            const int staleDoneSignalTimeoutMs = 1000;
            DateTime startTime = DateTime.Now;
            DateTime waitNotDoneStartTime = DateTime.Now;
            object? lastValue = null;
            bool lastReadSuccess = false;
            bool hasSeenNotDone = false;
            Console.WriteLine($"开始等待轴到位：Axis={axis.AxisName}, MoveDoneRead={axis.MotionConfig.MoveDoneRead?.Address}, TimeoutMs={timeoutMs}");
            while (true)
            {
                lastReadSuccess = plc.TryReadPointValue(axis.MotionConfig.MoveDoneRead, out var value);
                lastValue = value;
                if (lastReadSuccess)
                {
                    bool isDone = IsTrueValue(value);
                    if (!isDone)
                    {
                        hasSeenNotDone = true;
                    }
                    else if (hasSeenNotDone)
                    {
                        Console.WriteLine($"轴到位成功：Axis={axis.AxisName}, MoveDoneRead={axis.MotionConfig.MoveDoneRead?.Address}, Value={value}, ElapsedMs={(DateTime.Now - startTime).TotalMilliseconds:0}");
                        return NodeStatus.Success;
                    }
                    else if ((DateTime.Now - waitNotDoneStartTime).TotalMilliseconds >= staleDoneSignalTimeoutMs)
                    {
                        if (!string.IsNullOrWhiteSpace(axis.MotionConfig.CurrentPosRead?.Address) &&
                            plc.TryReadPointValue(axis.MotionConfig.CurrentPosRead, out var currentValue))
                        {
                            if (double.TryParse(currentValue?.ToString(), out var currentPos))
                            {
                                if (Math.Abs(currentPos - targetPosition) <= 0.01)
                                {
                                    Console.WriteLine($"轴到位信号保持True，但当前位置已到目标，按已到位处理：Axis={axis.AxisName}, CurrentPosRead={axis.MotionConfig.CurrentPosRead?.Address}, Current={currentPos}, Target={targetPosition}, ElapsedMs={(DateTime.Now - startTime).TotalMilliseconds:0}");
                                    return NodeStatus.Success;
                                }

                                Console.WriteLine($"轴到位信号疑似旧值，当前位置未到目标：Axis={axis.AxisName}, CurrentPosRead={axis.MotionConfig.CurrentPosRead?.Address}, Current={currentPos}, Target={targetPosition}");
                            }
                        }

                        Console.WriteLine($"轴到位信号疑似旧值，未经历False，取消继续执行：Axis={axis.AxisName}, MoveDoneRead={axis.MotionConfig.MoveDoneRead?.Address}, LastValue={lastValue}, WaitNotDoneMs={staleDoneSignalTimeoutMs}");
                        return NodeStatus.Timeout;
                    }
                }

                if ((DateTime.Now - startTime).TotalMilliseconds >= timeoutMs)
                {
                    Console.WriteLine($"轴到位超时：Axis={axis.AxisName}, MoveDoneRead={axis.MotionConfig.MoveDoneRead?.Address}, LastReadSuccess={lastReadSuccess}, LastValue={lastValue}, TimeoutMs={timeoutMs}");
                    return NodeStatus.Timeout;
                }

                Task.Delay(50).Wait();
            }
        }

        private bool IsTrueValue(object value)
        {
            if (value is bool boolValue)
                return boolValue;

            var text = value?.ToString();
            return string.Equals(text, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(text, "True", StringComparison.OrdinalIgnoreCase);
        }
        #endregion
    }
}
