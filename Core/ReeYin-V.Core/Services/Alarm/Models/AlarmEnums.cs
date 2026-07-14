namespace ReeYin_V.Core.Services.Alarm.Models
{
    public enum AlarmAcknowledgeResetMode
    {
        Never = 0,
        OnSeverityIncrease = 1,
        OnEveryRepeat = 2
    }

    /// <summary>
    /// 报警引擎和持久化记录使用的严重等级。
    /// </summary>
    public enum AlarmSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Fatal = 3,
        Critical = Fatal
    }

    public enum AlarmPopupMode
    {
        None = 0,
        Growl = 1,
        Modal = 2
    }

    /// <summary>
    /// 报警趋势统计的时间桶粒度。
    /// </summary>
    public enum AlarmChartBucket
    {
        Hour = 0,
        Day = 1,
        Week = 2,
        Month = 3
    }

    /// <summary>
    /// 报警状态变化时发出的实时事件类型。
    /// </summary>
    public enum AlarmEventKind
    {
        Raised = 0,
        Repeated = 1,
        Confirmed = 2,
        Cleared = 3
    }

    /// <summary>
    /// 报警历史支持的导出格式。
    /// </summary>
    public enum AlarmExportFormat
    {
        Csv = 0,
        ExcelXml = 1
    }
}
