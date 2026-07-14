using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm
{
    /// <summary>
    /// 报警中心服务，负责报警触发、确认、清除、查询和导出。
    /// </summary>
    public interface IAlarmService
    {
        /// <summary>
        /// 内存中的报警状态变化后触发。
        /// </summary>
        event EventHandler<AlarmDataChangedEventArgs>? DataChanged;

        /// <summary>
        /// 内存中保留的最大实时事件数量。
        /// </summary>
        int MaxCacheCount { get; }

        /// <summary>
        /// 从数据库加载活动报警和最近历史到服务缓存。
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用最小必填字段触发报警。
        /// </summary>
        AlarmInfo AddAlarm(string code, string message, AlarmSeverity level, string source);

        /// <summary>
        /// 使用统一报警信号触发报警，软件和硬件入口都可以调用。
        /// </summary>
        AlarmInfo Report(AlarmSignal signal);

        /// <summary>
        /// 触发或重复触发报警。Code、Source 和 Location 作为活动报警去重键。
        /// </summary>
        AlarmInfo AddAlarm(AlarmRaiseRequest request);

        /// <summary>
        /// 清除所有符合编码以及可选来源、位置条件的活动报警。
        /// </summary>
        bool ClearAlarm(string code, string? source = null, string? user = null, string? note = null, string? location = null);

        AlarmOperationResult ClearByKey(string code, string? source, string? location, string? user, string? note, AlarmClearOrigin origin);

        /// <summary>
        /// 按生命周期 Id 将一个活动报警标记为已确认。
        /// </summary>
        bool ConfirmAlarm(string id, string user, string? note = null);

        /// <summary>
        /// 确认一个活动报警的异步包装方法。
        /// </summary>
        Task AcknowledgeAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default);

        Task<AlarmOperationResult> AcknowledgeOperationAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按生命周期 Id 清除一个活动报警。
        /// </summary>
        Task ClearAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default);

        Task<AlarmOperationResult> ClearByIdAsync(string activeId, string user, string? note, AlarmClearOrigin origin, CancellationToken cancellationToken = default);

        /// <summary>
        /// 根据当前服务状态返回看板计数。
        /// </summary>
        Task<AlarmDashboardSnapshot> GetDashboardAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 从内存缓存返回活动报警。
        /// </summary>
        Task<IReadOnlyList<AlarmActiveRecord>> GetActiveAlarmsAsync(AlarmActiveQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 返回一页持久化报警历史。
        /// </summary>
        Task<AlarmPagedResult<AlarmHistoryEntry>> GetHistoryPageAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 返回持久化报警历史，用于导出或简表展示。
        /// </summary>
        Task<IReadOnlyList<AlarmHistoryEntry>> GetHistoryAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 返回指定查询范围内的趋势和分布统计。
        /// </summary>
        Task<AlarmStatisticsResult> GetStatisticsAsync(AlarmStatisticsQuery query, CancellationToken cancellationToken = default);

        /// <summary>
        /// 返回持久化记录和活动报警中的所有已知来源。
        /// </summary>
        Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 返回内存中保留的最新实时事件。
        /// </summary>
        Task<IReadOnlyList<AlarmRealtimeEntry>> GetRealtimeFeedAsync(int maxCount = 200, CancellationToken cancellationToken = default);

        /// <summary>
        /// 将匹配的历史记录导出到指定目录，并返回生成的文件路径。
        /// </summary>
        Task<string> ExportHistoryAsync(
            AlarmHistoryQuery query,
            string outputDirectory,
            AlarmExportFormat format = AlarmExportFormat.Csv,
            CancellationToken cancellationToken = default);
    }
}
