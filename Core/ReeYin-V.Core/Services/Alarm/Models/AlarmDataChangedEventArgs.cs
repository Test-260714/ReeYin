using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 报警状态发生变化后通过 IAlarmService.DataChanged 发送的数据快照。
    /// </summary>
    public sealed class AlarmDataChangedEventArgs : EventArgs
    {
        public AlarmDataChangedEventArgs(
            AlarmDashboardSnapshot dashboard,
            IReadOnlyList<AlarmActiveRecord> activeAlarms,
            AlarmRealtimeEntry? latestEvent)
        {
            Dashboard = dashboard ?? new AlarmDashboardSnapshot();
            ActiveAlarms = activeAlarms ?? Array.Empty<AlarmActiveRecord>();
            LatestEvent = latestEvent;
        }

        public AlarmDashboardSnapshot Dashboard { get; }

        public IReadOnlyList<AlarmActiveRecord> ActiveAlarms { get; }

        public AlarmRealtimeEntry? LatestEvent { get; }
    }
}
