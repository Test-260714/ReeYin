using ReeYin_V.Hardware.PLC.Models;
using System;
using System.Globalization;
using System.Linq;

namespace ReeYin_V.Hardware.PLC.Services
{
    /// <summary>
    /// PLC状态轮询服务
    /// </summary>
    public class PLCMonitorService
    {
        private readonly PLCBase _device;
        private System.Timers.Timer _timer;

        public PLCMonitorService(PLCBase device)
        {
            _device = device;
        }

        public void Start()
        {
            Stop();
            _timer = new System.Timers.Timer(500);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void Stop()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Enabled = false;
            _timer.Elapsed -= OnTimerElapsed;
            _timer.Dispose();
            _timer = null;
        }

        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_device == null || !_device.Config.IsConnected)
            {
                return;
            }

            try
            {
                PollDeviceState();
                PollAxisState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PLC状态监听失败: {ex.Message}");
            }
        }

        private void PollDeviceState()
        {
            if (_device.TryReadPointValue(_device.DeviceMotionConfig.PLCStateRead, out var plcStateValue))
            {
                int value = ConvertToInt(plcStateValue);
                _device.DeviceRuntimeState.PLCStateValue = value;
                _device.DeviceRuntimeState.PLCStateText = ResolveStateText(_device.DeviceMotionConfig.PLCStateMaps, value);
            }

            if (_device.TryReadPointValue(_device.DeviceMotionConfig.ResetDoneRead, out var resetDoneValue))
            {
                _device.DeviceRuntimeState.ResetCompleted = ConvertToBool(resetDoneValue);
            }

            if (_device.TryReadPointValue(_device.DeviceMotionConfig.AlarmRead, out var alarmValue))
            {
                bool hasAlarm = ConvertToBool(alarmValue);
                _device.DeviceRuntimeState.HasAlarm = hasAlarm;
                _device.DeviceRuntimeState.AlarmText = hasAlarm ? Convert.ToString(alarmValue, CultureInfo.InvariantCulture) : "";
            }

            _device.DeviceRuntimeState.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void PollAxisState()
        {
            foreach (var axis in _device.AxisGroups.SelectMany(group => group.AxisItems).Where(axis => axis.IsUsing))
            {
                if (_device.TryReadPointValue(axis.MotionConfig.CurrentPosRead, out var currentPos))
                {
                    axis.RuntimeState.CurrentPos = Convert.ToDouble(currentPos, CultureInfo.InvariantCulture);
                    axis.RuntimeState.LastUpdateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                if (_device.TryReadPointValue(axis.MotionConfig.MoveDoneRead, out var moveDone))
                {
                    axis.RuntimeState.IsInPosition = ConvertToBool(moveDone);
                    axis.RuntimeState.IsMoving = !axis.RuntimeState.IsInPosition;
                }

                if (_device.TryReadPointValue(axis.MotionConfig.AlarmRead, out var axisAlarm))
                {
                    axis.RuntimeState.IsAlarm = ConvertToBool(axisAlarm);
                }

                if (_device.TryReadPointValue(axis.MotionConfig.HomeDoneRead, out var homeDone))
                {
                    axis.RuntimeState.IsHomeDone = ConvertToBool(homeDone);
                }

                if (_device.TryReadPointValue(axis.MotionConfig.BusyRead, out var busy))
                {
                    axis.RuntimeState.IsBusy = ConvertToBool(busy);
                }

                if (_device.TryReadPointValue(axis.MotionConfig.EnabledRead, out var enabled))
                {
                    axis.RuntimeState.IsEnabled = ConvertToBool(enabled);
                }
            }
        }

        private static string ResolveStateText(System.Collections.Generic.IEnumerable<PLCStateMapItem> maps, int value)
        {
            var map = maps?.FirstOrDefault(item => item.Value == value);
            return map?.Text ?? value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool ConvertToBool(object value)
        {
            if (value == null)
            {
                return false;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (string.Equals(text, "1", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(text, "0", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return bool.TryParse(text, out var result) && result;
        }

        private static int ConvertToInt(object value)
        {
            if (value == null)
            {
                return 0;
            }

            if (value is int intValue)
            {
                return intValue;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            return int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }
    }
}
