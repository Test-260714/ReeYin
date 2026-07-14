using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ReeYin_V.Hardware.PLC.Services
{
    /// <summary>
    /// PLC运动协议执行服务
    /// </summary>
    public class PLCMotionService
    {
        private readonly PLCBase _device;
        private readonly Dictionary<string, string> _lastPointWriteCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lastPointWriteCacheLock = new object();

        public PLCMotionService(PLCBase device)
        {
            _device = device;
        }

        public void ClearWriteCache()
        {
            lock (_lastPointWriteCacheLock)
            {
                _lastPointWriteCache.Clear();
            }
        }

        public bool Reset()
        {
            if (!IsReady())
            {
                return false;
            }

            WriteHostState(3);
            return _device.ExecuteCommandPoint(_device.DeviceMotionConfig.ResetWrite);
        }

        public bool ClearAlarm()
        {
            if (!IsReady())
            {
                return false;
            }

            return _device.ExecuteCommandPoint(_device.DeviceMotionConfig.ServoAlarmClearWrite);
        }

        public bool MoveAxis(PLCAxisItem axis, double targetPos, double runSpeed, double acc, double dec)
        {
            return RunAxis(axis, targetPos, runSpeed, acc, dec);
        }

        public bool RunAxis(PLCAxisItem axis, double targetPos, double runSpeed, double acc, double dec)
        {
            return RunAxis(axis, targetPos, (double?)runSpeed, acc, dec);
        }

        public bool RunAxis(PLCAxisItem axis, double targetPos, double? runSpeed, double? acc, double? dec)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion RunAxis进入：Axis={axis.AxisName}, Target={targetPos}, Speed={runSpeed}, Acc={acc}, Dec={dec}");
            bool resetResult = _device.ExecuteCommandPoint(axis.MotionConfig.MoveDoneResetWrite);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 复位到位信号：Axis={axis.AxisName}, Addr={axis.MotionConfig.MoveDoneResetWrite?.Address}, Result={resetResult}");
            if (!resetResult)
            {
                bool clearResult = _device.WriteCommandClearValue(axis.MotionConfig.MoveDoneResetWrite);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 复位到位信号清除值：Axis={axis.AxisName}, Addr={axis.MotionConfig.MoveDoneResetWrite?.Address}, Result={clearResult}");
            }

            bool positionResult = _device.WritePoint(axis.MotionConfig.RunPositionWrite, targetPos);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 写目标位置：Axis={axis.AxisName}, Addr={axis.MotionConfig.RunPositionWrite?.Address}, Value={targetPos}, Result={positionResult}");
            if (!positionResult)
            {
                return false;
            }

            if (runSpeed.HasValue)
            {
                bool speedResult = WriteMotionParameter(axis.MotionConfig.RunSpeedWrite, runSpeed.Value);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 写运行速度：Axis={axis.AxisName}, Addr={axis.MotionConfig.RunSpeedWrite?.Address}, Value={runSpeed.Value}, Result={speedResult}");
            }

            if (acc.HasValue)
            {
                bool accResult = WriteMotionParameter(axis.MotionConfig.AccWrite, acc.Value);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 写加速度：Axis={axis.AxisName}, Addr={axis.MotionConfig.AccWrite?.Address}, Value={acc.Value}, Result={accResult}");
            }

            if (dec.HasValue)
            {
                bool decResult = WriteMotionParameter(axis.MotionConfig.DecWrite, dec.Value);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 写减速度：Axis={axis.AxisName}, Addr={axis.MotionConfig.DecWrite?.Address}, Value={dec.Value}, Result={decResult}");
            }

            bool maxLimitResult = WriteMotionParameter(axis.MotionConfig.MaxLimitWrite, axis.MaxLimit);
            bool minLimitResult = WriteMotionParameter(axis.MotionConfig.MinLimitWrite, axis.MinLimit);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 写限位：Axis={axis.AxisName}, MaxAddr={axis.MotionConfig.MaxLimitWrite?.Address}, Max={axis.MaxLimit}, MaxResult={maxLimitResult}, MinAddr={axis.MotionConfig.MinLimitWrite?.Address}, Min={axis.MinLimit}, MinResult={minLimitResult}");

            axis.RuntimeState.TargetPos = targetPos;
            axis.RuntimeState.InputTargetPos = targetPos;
            axis.RuntimeState.IsMoving = true;
            axis.RuntimeState.IsInPosition = false;
            axis.RuntimeState.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            WriteHostState(2);

            bool triggerResult = _device.ExecuteCommandPoint(axis.MotionConfig.RunTriggerWrite);
            Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 触发跑位：Axis={axis.AxisName}, Addr={axis.MotionConfig.RunTriggerWrite?.Address}, ActiveValue={axis.MotionConfig.RunTriggerWrite?.ActiveValue}, AutoClear={axis.MotionConfig.RunTriggerWrite?.AutoClear}, PulseMs={axis.MotionConfig.RunTriggerWrite?.PulseMs}, Result={triggerResult}");
            if (triggerResult && HasAddress(axis.MotionConfig.RunTriggerWrite) && !axis.MotionConfig.RunTriggerWrite.AutoClear)
            {
                ForceClearRunTrigger(axis);
            }
            return triggerResult;
        }

        private void ForceClearRunTrigger(PLCAxisItem axis)
        {
            var point = axis.MotionConfig.RunTriggerWrite;
            int pulseMs = Math.Clamp(point.PulseMs, 10, 60000);
            Task.Run(async () =>
            {
                await Task.Delay(pulseMs);
                bool clearResult = _device.WriteCommandClearValue(point);
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} PLCMotion 强制清除跑位触发：Axis={axis.AxisName}, Addr={point.Address}, ClearValue={point.ClearValue}, DelayMs={pulseMs}, Result={clearResult}");
            });
        }

        public bool StartJog(PLCAxisItem axis, bool positive, double jogStep, double jogSpeed)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            WriteMotionParameter(axis.MotionConfig.JogStepWrite, jogStep);
            WriteMotionParameter(HasAddress(axis.MotionConfig.ManualSpeedWrite) ? axis.MotionConfig.ManualSpeedWrite : axis.MotionConfig.RunSpeedWrite, jogSpeed);
            WriteAccDecParameter(axis, axis.RuntimeState.InputAcc, axis.RuntimeState.InputDec);
            WriteHostState(2);

            var point = positive ? axis.MotionConfig.JogPositiveWrite : axis.MotionConfig.JogNegativeWrite;
            axis.RuntimeState.IsMoving = true;
            return WriteCommandActiveValue(point);
        }

        public bool StopJog(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            // 点动按下写有效值，松开只清点动正反位，不用停止触发替代松开。
            bool positiveResult = _device.WriteCommandClearValue(axis.MotionConfig.JogPositiveWrite);
            bool negativeResult = _device.WriteCommandClearValue(axis.MotionConfig.JogNegativeWrite);
            bool result = positiveResult || negativeResult;

            axis.RuntimeState.IsMoving = false;
            WriteHostState(1);
            return result;
        }

        public bool StopAxis(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            bool jogResult = StopJog(axis);
            bool stopResult = HasAddress(axis.MotionConfig.StopTriggerWrite) && _device.ExecuteCommandPoint(axis.MotionConfig.StopTriggerWrite);
            return jogResult || stopResult;
        }

        public bool StopAll(PLCAxisGroup axisGroup)
        {
            if (!IsReady() || axisGroup == null)
            {
                return false;
            }

            bool result = false;
            foreach (var axis in axisGroup.AxisItems.Where(a => a.IsUsing))
            {
                result |= StopAxis(axis);
            }
            return result;
        }

        public bool EnableShield(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            return _device.ExecuteCommandPoint(axis.MotionConfig.EnableShieldWrite);
        }

        public bool DisableShield(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            return _device.WriteCommandClearValue(axis.MotionConfig.EnableShieldWrite);
        }

        public bool HomeAxis(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            WriteHostState(3);
            return _device.ExecuteCommandPoint(axis.MotionConfig.HomeTriggerWrite);
        }

        public bool SetAxisPosition(PLCAxisItem axis, double position)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            if (!_device.WritePoint(axis.MotionConfig.SetPositionWrite, position))
            {
                return false;
            }

            return _device.ExecuteCommandPoint(axis.MotionConfig.SetPositionTriggerWrite);
        }

        public bool ReadAxisParameters(PLCAxisItem axis)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            bool result = false;
            result |= ReadMotionParameter(axis.MotionConfig.JogStepWrite, value => axis.RuntimeState.InputJogStep = value);
            result |= ReadMotionParameter(axis.MotionConfig.ManualSpeedWrite, value => axis.RuntimeState.InputJogSpeed = value);
            result |= ReadMotionParameter(axis.MotionConfig.RunSpeedWrite, value => axis.RuntimeState.InputRunSpeed = value);
            result |= ReadMotionParameter(axis.MotionConfig.AccWrite, value => axis.RuntimeState.InputAcc = value);
            result |= ReadMotionParameter(axis.MotionConfig.DecWrite, value => axis.RuntimeState.InputDec = value);
            return result;
        }

        public bool WriteAxisParameter(PLCAxisItem axis, string propertyName)
        {
            if (!IsReady() || axis == null)
            {
                return false;
            }

            return propertyName switch
            {
                nameof(PLCAxisRuntimeState.InputTargetPos) => WriteMotionParameter(axis.MotionConfig.RunPositionWrite, axis.RuntimeState.InputTargetPos),
                nameof(PLCAxisRuntimeState.InputJogStep) => WriteMotionParameter(axis.MotionConfig.JogStepWrite, axis.RuntimeState.InputJogStep),
                nameof(PLCAxisRuntimeState.InputJogSpeed) => WriteMotionParameter(axis.MotionConfig.ManualSpeedWrite, axis.RuntimeState.InputJogSpeed),
                nameof(PLCAxisRuntimeState.InputRunSpeed) => WriteMotionParameter(axis.MotionConfig.RunSpeedWrite, axis.RuntimeState.InputRunSpeed),
                nameof(PLCAxisRuntimeState.InputAcc) => WriteMotionParameter(axis.MotionConfig.AccWrite, axis.RuntimeState.InputAcc),
                nameof(PLCAxisRuntimeState.InputDec) => WriteMotionParameter(axis.MotionConfig.DecWrite, axis.RuntimeState.InputDec),
                _ => true,
            };
        }

        private bool WriteMotionParameter(PLCPointConfig point, double value)
        {
            if (!string.IsNullOrWhiteSpace(point?.Address))
            {
                return WritePointIfChanged(point, value);
            }

            return true;
        }

        private bool ReadMotionParameter(PLCPointConfig point, Action<double> setValue)
        {
            if (string.IsNullOrWhiteSpace(point?.Address))
            {
                return false;
            }

            if (!_device.TryReadPointValue(point, out var value))
            {
                return false;
            }

            setValue(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            return true;
        }

        private void WriteAccDecParameter(PLCAxisItem axis, double acc, double dec)
        {
            WriteMotionParameter(axis.MotionConfig.AccWrite, acc);
            WriteMotionParameter(axis.MotionConfig.DecWrite, dec);
        }

        public bool WriteSpeedProfile(PLCAxisItem axis, PLCSpeedProfile profile)
        {
            if (!IsReady() || axis == null || profile == null)
            {
                return false;
            }

            WriteMotionParameter(axis.MotionConfig.RunSpeedWrite, profile.RunSpeed);
            WriteMotionParameter(axis.MotionConfig.AccWrite, profile.Acc);
            WriteMotionParameter(axis.MotionConfig.DecWrite, profile.Dec);
            return true;
        }

        private bool IsReady()
        {
            return _device != null && _device.Config.IsConnected;
        }

        private static bool HasAddress(PLCPointConfig point)
        {
            return !string.IsNullOrWhiteSpace(point?.Address);
        }

        private void WriteHostState(int value)
        {
            if (string.IsNullOrWhiteSpace(_device.DeviceMotionConfig.HostStateWrite.Address))
            {
                return;
            }

            WritePointIfChanged(_device.DeviceMotionConfig.HostStateWrite, value);
            _device.DeviceRuntimeState.HostStateValue = value;
            _device.DeviceRuntimeState.HostStateText = ResolveStateText(_device.DeviceMotionConfig.HostStateMaps, value);
        }

        private bool WriteCommandActiveValue(PLCCommandPointConfig point)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            return _device.WritePoint(point, ConvertCommandValue(point.ActiveValue, point.DataType));
        }

        private bool WritePointIfChanged(PLCPointConfig point, object value)
        {
            if (point == null || string.IsNullOrWhiteSpace(point.Address))
            {
                return false;
            }

            string cacheValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            lock (_lastPointWriteCacheLock)
            {
                if (_lastPointWriteCache.TryGetValue(point.Address, out var lastValue) &&
                    string.Equals(lastValue, cacheValue, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            bool result = _device.WritePoint(point, value);
            if (result)
            {
                lock (_lastPointWriteCacheLock)
                {
                    _lastPointWriteCache[point.Address] = cacheValue;
                }
            }

            return result;
        }

        private static object ConvertCommandValue(string value, EnumParaInfoModelParaType dataType)
        {
            string text = value ?? string.Empty;
            switch (dataType)
            {
                case EnumParaInfoModelParaType.Bool:
                    if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    return bool.TryParse(text, out bool boolValue) && boolValue;
                case EnumParaInfoModelParaType.Short:
                    return short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out short shortValue) ? shortValue : (short)0;
                case EnumParaInfoModelParaType.Ushort:
                    return ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ushort ushortValue) ? ushortValue : (ushort)0;
                case EnumParaInfoModelParaType.Int:
                    return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out int intValue) ? intValue : 0;
                case EnumParaInfoModelParaType.Uint:
                    return uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out uint uintValue) ? uintValue : 0u;
                case EnumParaInfoModelParaType.Long:
                    return long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out long longValue) ? longValue : 0L;
                case EnumParaInfoModelParaType.Ulong:
                    return ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ulong ulongValue) ? ulongValue : 0UL;
                case EnumParaInfoModelParaType.Double:
                    return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue) ? doubleValue : 0d;
                case EnumParaInfoModelParaType.Float:
                default:
                    return float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatValue) ? floatValue : 0f;
            }
        }

        private static string ResolveStateText(System.Collections.Generic.IEnumerable<PLCStateMapItem> maps, int value)
        {
            var map = maps?.FirstOrDefault(item => item.Value == value);
            return map?.Text ?? value.ToString(CultureInfo.InvariantCulture);
        }

    }
}
