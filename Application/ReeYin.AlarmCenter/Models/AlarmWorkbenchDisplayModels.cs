using Prism.Mvvm;
using ReeYin_V.Core.Services.Alarm.Models;
using System.Windows.Media;

namespace ReeYin.AlarmCenter.Models
{
    // 中文文案请直接使用 UTF-8 文本，避免写成 \uXXXX 转义。
    internal static class AlarmWorkbenchPalette
    {
        public static string GetSeverityText(AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Fatal => "致命",
                AlarmSeverity.Error => "错误",
                AlarmSeverity.Warning => "预警",
                _ => "信息"
            };
        }

        public static string GetBadgeBrush(AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Fatal => "#7C3AED",
                AlarmSeverity.Error => "#DC2626",
                AlarmSeverity.Warning => "#D97706",
                _ => "#2563EB"
            };
        }

        public static string GetSoftBrush(AlarmSeverity severity)
        {
            return severity switch
            {
                AlarmSeverity.Fatal => "#EDE9FE",
                AlarmSeverity.Error => "#FEE2E2",
                AlarmSeverity.Warning => "#FEF3C7",
                _ => "#DBEAFE"
            };
        }

        public static string GetRealtimeActionText(AlarmEventKind eventKind)
        {
            return eventKind switch
            {
                AlarmEventKind.Raised => "新增报警",
                AlarmEventKind.Repeated => "重复触发",
                AlarmEventKind.Confirmed => "报警确认",
                AlarmEventKind.Cleared => "报警解除",
                _ => "状态更新"
            };
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalSeconds < 60)
            {
                return $"{Math.Max(1, (int)duration.TotalSeconds)} 秒";
            }

            if (duration.TotalMinutes < 60)
            {
                return $"{Math.Max(1, (int)duration.TotalMinutes)} 分";
            }

            if (duration.TotalHours < 24)
            {
                return $"{(int)duration.TotalHours} 小时 {(int)duration.Minutes} 分";
            }

            return $"{(int)duration.TotalDays} 天 {(int)duration.Hours} 小时";
        }
    }

    public sealed class AlarmSummaryCard
    {
        public string Title { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;

        public string Caption { get; init; } = string.Empty;

        public string AccentBrush { get; init; } = "#2563EB";

        public string BackgroundBrush { get; init; } = "#F8FAFC";
    }

    public sealed class AlarmOptionItem
    {
        public string Label { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }

    public sealed class AlarmWorkbenchPageItem : BindableBase
    {
        private bool _isSelected;

        public string Header { get; init; } = string.Empty;

        public string NavigationCode { get; init; } = string.Empty;

        public string ViewName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        // 用于导航按钮高亮，和实际区域导航解耦。
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public sealed class AlarmActiveItem : BindableBase
    {
        public string ActiveId { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Location { get; init; } = string.Empty;

        public AlarmSeverity Severity { get; init; }

        public DateTime RaisedAt { get; init; }

        public DateTime LastRaisedAt { get; init; }

        public int OccurrenceCount { get; init; }

        public bool NeedAcknowledge { get; init; }

        public bool AllowManualClear { get; init; }

        public bool IsConfirmed { get; init; }

        public string ConfirmUser { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string Detail { get; init; } = string.Empty;

        public string LevelText => AlarmWorkbenchPalette.GetSeverityText(Severity);

        public string LevelBadgeBrush => AlarmWorkbenchPalette.GetBadgeBrush(Severity);

        public string LevelSoftBrush => AlarmWorkbenchPalette.GetSoftBrush(Severity);

        public string RaisedAtText => RaisedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public string LastRaisedAtText => LastRaisedAt.ToString("HH:mm:ss");

        public string DurationText => AlarmWorkbenchPalette.FormatDuration(DateTime.Now - RaisedAt);

        public string ConfirmText => !NeedAcknowledge
            ? "无需确认"
            : IsConfirmed
                ? $"已确认 / {ConfirmUser}"
                : "待确认";

        public string Subtitle => string.IsNullOrWhiteSpace(Location)
            ? $"{Source} / {Code}"
            : $"{Source} / {Location} / {Code}";

        public void RefreshClock()
        {
            RaisePropertyChanged(nameof(DurationText));
        }

        public static AlarmActiveItem FromCore(AlarmActiveRecord record)
        {
            return new AlarmActiveItem
            {
                ActiveId = record.ActiveId,
                Code = record.Code,
                Title = record.Name,
                Category = record.Category,
                Source = record.SourceName,
                Location = record.Location,
                Severity = record.Severity,
                RaisedAt = record.RaisedAt,
                LastRaisedAt = record.LastRaisedAt,
                OccurrenceCount = record.OccurrenceCount,
                NeedAcknowledge = record.NeedAcknowledge,
                AllowManualClear = record.AllowManualClear,
                IsConfirmed = record.IsAcknowledged,
                ConfirmUser = record.IsAcknowledged ? "已处理" : string.Empty,
                Message = record.Message,
                Detail = record.Detail
            };
        }
    }

    public sealed class AlarmFeedItem : BindableBase
    {
        private bool _isLatest;

        public string Code { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public AlarmSeverity Severity { get; init; }

        public string ActionText { get; init; } = string.Empty;

        public DateTime Timestamp { get; init; }

        public bool IsLatest
        {
            get => _isLatest;
            set
            {
                if (SetProperty(ref _isLatest, value))
                {
                    RaisePropertyChanged(nameof(RowBackground));
                    RaisePropertyChanged(nameof(RowBorderBrush));
                }
            }
        }

        public string LevelText => AlarmWorkbenchPalette.GetSeverityText(Severity);

        public string LevelBadgeBrush => AlarmWorkbenchPalette.GetBadgeBrush(Severity);

        public string RowBackground => IsLatest ? "#FFFDF4D7" : "#FFFFFFFF";

        public string RowBorderBrush => IsLatest ? "#F59E0B" : "#E5E7EB";

        public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

        public string Summary => $"{Title} ({Code}) / {Source}";

        public static AlarmFeedItem FromCore(AlarmRealtimeEntry entry, bool isLatest)
        {
            return new AlarmFeedItem
            {
                Code = entry.Code,
                Title = entry.Name,
                Message = entry.Message,
                Source = entry.Source,
                Severity = entry.Severity,
                ActionText = AlarmWorkbenchPalette.GetRealtimeActionText(entry.EventKind),
                Timestamp = entry.EventTime,
                IsLatest = isLatest
            };
        }
    }

    public sealed class AlarmHistoryItem
    {
        public string ActiveId { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string Source { get; init; } = string.Empty;

        public string Location { get; init; } = string.Empty;

        public AlarmSeverity Severity { get; init; }

        public DateTime RaisedAt { get; init; }

        public DateTime ClearedAt { get; init; }

        public int OccurrenceCount { get; init; }

        public bool IsConfirmed { get; init; }

        public string ConfirmUser { get; init; } = string.Empty;

        public string Message { get; init; } = string.Empty;

        public string LevelText => AlarmWorkbenchPalette.GetSeverityText(Severity);

        public string LevelBadgeBrush => AlarmWorkbenchPalette.GetBadgeBrush(Severity);

        public string RaisedAtText => RaisedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public string ClearedAtText => ClearedAt.ToString("yyyy-MM-dd HH:mm:ss");

        public string DurationText => AlarmWorkbenchPalette.FormatDuration(ClearedAt - RaisedAt);

        public string ConfirmText => IsConfirmed
            ? string.IsNullOrWhiteSpace(ConfirmUser) ? "已确认" : ConfirmUser
            : "未确认";

        public static AlarmHistoryItem FromCore(AlarmHistoryEntry entry)
        {
            return new AlarmHistoryItem
            {
                ActiveId = entry.ActiveId,
                Code = entry.Code,
                Title = entry.Name,
                Source = entry.SourceName,
                Location = entry.Location,
                Severity = entry.Severity,
                RaisedAt = entry.RaisedAt,
                ClearedAt = entry.ClearedAt,
                OccurrenceCount = entry.OccurrenceCount,
                IsConfirmed = entry.WasAcknowledged,
                ConfirmUser = entry.ConfirmUser,
                Message = entry.Message
            };
        }
    }

    public sealed class AlarmStatisticBarItem
    {
        public string Label { get; init; } = string.Empty;

        public int Count { get; init; }

        public int Maximum { get; init; }

        public string AccentBrush { get; init; } = "#2563EB";

        public string Description { get; init; } = string.Empty;

        public string CountText => Count.ToString();
    }

    public sealed class AlarmTrendPoint
    {
        public string Label { get; init; } = string.Empty;

        public int Count { get; init; }

        public int Maximum { get; init; }

        public string AccentBrush { get; init; } = "#2563EB";

        public string Description { get; init; } = string.Empty;

        public string CountText => Count.ToString();
    }

    public sealed class AlarmPieSliceItem
    {
        public string Label { get; init; } = string.Empty;

        public int Count { get; init; }

        public double Percentage { get; init; }

        public Brush FillBrush { get; init; } = Brushes.SteelBlue;

        public Geometry Geometry { get; init; } = Geometry.Empty;

        public string CountText => Count.ToString();

        public string PercentageText => Percentage.ToString("P1");

        public string LegendText => $"{Label} / {CountText} / {PercentageText}";
    }
}
