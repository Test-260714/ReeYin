using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 活动报警内存缓存的查询条件。
    /// </summary>
    public sealed class AlarmActiveQuery
    {
        public AlarmSeverity? Severity { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Keyword { get; set; } = string.Empty;

        public int MaxCount { get; set; } = 200;

        public bool OnlyUnacknowledged { get; set; }
    }

    /// <summary>
    /// 持久化报警历史的筛选和分页条件。
    /// </summary>
    public sealed class AlarmHistoryQuery
    {
        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public AlarmSeverity? Severity { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Keyword { get; set; } = string.Empty;

        public int PageIndex { get; set; } = 1;

        public int PageSize { get; set; } = 100;

        public int MaxCount { get; set; } = 500;

        /// <summary>
        /// 默认不包含活动报警，使历史界面只显示已闭环的报警。
        /// </summary>
        public bool IncludeActive { get; set; }
    }

    /// <summary>
    /// 趋势和分布统计的查询条件。
    /// </summary>
    public sealed class AlarmStatisticsQuery
    {
        public DateTime StartTime { get; set; } = DateTime.Today.AddDays(-6);

        public DateTime EndTime { get; set; } = DateTime.Today.AddDays(1).AddTicks(-1);

        public AlarmSeverity? Severity { get; set; }

        public string Source { get; set; } = string.Empty;

        public string Keyword { get; set; } = string.Empty;

        public AlarmChartBucket Bucket { get; set; } = AlarmChartBucket.Day;

        public int TopCount { get; set; } = 8;
    }

    /// <summary>
    /// 报警历史查询使用的通用分页结果。
    /// </summary>
    public sealed class AlarmPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
