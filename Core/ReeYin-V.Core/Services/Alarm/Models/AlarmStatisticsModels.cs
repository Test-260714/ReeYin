using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    /// <summary>
    /// 报警趋势图中的一个时间桶统计点。
    /// </summary>
    public sealed class AlarmTrendPoint
    {
        public DateTime BucketStart { get; set; }

        public int Count { get; set; }
    }

    /// <summary>
    /// 按报警类型聚合后的统计项。
    /// </summary>
    public sealed class AlarmTypeDistributionItem
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public int Count { get; set; }

        public AlarmSeverity HighestSeverity { get; set; }

        public DateTime? LatestRaisedAt { get; set; }
    }

    /// <summary>
    /// 按报警来源聚合后的统计项。
    /// </summary>
    public sealed class AlarmSourceDistributionItem
    {
        public string Source { get; set; } = string.Empty;

        public int Count { get; set; }

        public AlarmSeverity HighestSeverity { get; set; }

        public DateTime? LatestRaisedAt { get; set; }
    }

    /// <summary>
    /// 报警看板图表使用的完整统计结果。
    /// </summary>
    public sealed class AlarmStatisticsResult
    {
        public DateTime RangeStart { get; set; }

        public DateTime RangeEnd { get; set; }

        public AlarmChartBucket Bucket { get; set; } = AlarmChartBucket.Day;

        public IReadOnlyList<AlarmTrendPoint> TrendPoints { get; set; } = Array.Empty<AlarmTrendPoint>();

        public IReadOnlyList<AlarmTypeDistributionItem> TypeDistribution { get; set; } = Array.Empty<AlarmTypeDistributionItem>();

        public IReadOnlyList<AlarmSourceDistributionItem> SourceDistribution { get; set; } = Array.Empty<AlarmSourceDistributionItem>();
    }
}
