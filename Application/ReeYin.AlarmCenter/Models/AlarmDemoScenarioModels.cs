#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin.AlarmCenter.Models
{
    public sealed class AlarmDemoScenarioItem
    {
        public string DisplayName { get; init; } = string.Empty;

        public string Code { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Category { get; init; } = "Demo";

        public string Message { get; init; } = string.Empty;

        public AlarmSeverity Severity { get; init; } = AlarmSeverity.Warning;

        public AlarmPopupMode PopupMode { get; init; } = AlarmPopupMode.Growl;

        public bool NeedAcknowledge { get; init; }

        public bool AllowManualClear { get; init; } = true;

        public string Description { get; init; } = string.Empty;

        public string LevelText => AlarmWorkbenchPalette.GetSeverityText(Severity);

        public string LevelBadgeBrush => AlarmWorkbenchPalette.GetBadgeBrush(Severity);

        public string PopupModeText => PopupMode switch
        {
            AlarmPopupMode.Modal => "Modal 弹窗",
            AlarmPopupMode.Growl => "Growl 提示",
            _ => "不弹窗"
        };

        public string AcknowledgeText => NeedAcknowledge ? "需要确认" : "无需确认";

        public string ManualClearText => AllowManualClear ? "普通手动清除" : "可清除，异常仍在会重触发";

        public AlarmRaiseRequest CreateRaiseRequest(int sequence)
        {
            return new AlarmRaiseRequest
            {
                Code = Code,
                Name = Name,
                Category = Category,
                Message = $"{Message} #{sequence}",
                Level = Severity,
                Source = AlarmDemoScenarioFactory.DemoSource,
                Location = AlarmDemoScenarioFactory.DemoLocation,
                NeedAcknowledge = NeedAcknowledge,
                PopupMode = PopupMode,
                PopupThrottleSeconds = 0,
                AllowManualClear = AllowManualClear,
                ExtraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Demo"] = true,
                    ["Sequence"] = sequence,
                    ["Scenario"] = DisplayName,
                    ["CreatedBy"] = "AlarmCenter Debug Demo"
                }
            };
        }
    }

    public static class AlarmDemoScenarioFactory
    {
        public const string DemoSource = "AlarmDemo";
        public const string DemoLocation = "DebugPanel";
        public const string RepeatCode = "DEMO.ALARM.REPEAT";

        public static IReadOnlyList<AlarmDemoScenarioItem> CreateDefaults()
        {
            return new[]
            {
                new AlarmDemoScenarioItem
                {
                    DisplayName = "信息报警",
                    Code = "DEMO.ALARM.INFO",
                    Name = "Demo 信息报警",
                    Category = "Demo/Info",
                    Message = "用于验证 Info 等级和无弹窗报警链路",
                    Severity = AlarmSeverity.Info,
                    PopupMode = AlarmPopupMode.None,
                    NeedAcknowledge = false,
                    AllowManualClear = true,
                    Description = "验证低优先级记录、实时流和统计刷新，不弹出提示。"
                },
                new AlarmDemoScenarioItem
                {
                    DisplayName = "预警报警",
                    Code = "DEMO.ALARM.WARNING",
                    Name = "Demo 预警报警",
                    Category = "Demo/Warning",
                    Message = "用于验证 Warning 等级和 Growl 提示",
                    Severity = AlarmSeverity.Warning,
                    PopupMode = AlarmPopupMode.Growl,
                    NeedAcknowledge = false,
                    AllowManualClear = true,
                    Description = "验证预警颜色、Growl 提示和可手动清除链路。"
                },
                new AlarmDemoScenarioItem
                {
                    DisplayName = "错误报警",
                    Code = "DEMO.ALARM.ERROR",
                    Name = "Demo 错误报警",
                    Category = "Demo/Error",
                    Message = "用于验证 Error 等级和错误提示",
                    Severity = AlarmSeverity.Error,
                    PopupMode = AlarmPopupMode.Growl,
                    NeedAcknowledge = false,
                    AllowManualClear = true,
                    Description = "验证错误等级、活动报警列表和历史落库链路。"
                },
                new AlarmDemoScenarioItem
                {
                    DisplayName = "致命报警",
                    Code = "DEMO.ALARM.FATAL",
                    Name = "Demo 致命报警",
                    Category = "Demo/Fatal",
                    Message = "用于验证 Fatal 等级、Modal 弹窗和确认流程",
                    Severity = AlarmSeverity.Fatal,
                    PopupMode = AlarmPopupMode.Modal,
                    NeedAcknowledge = true,
                    AllowManualClear = false,
                    Description = "验证最高等级报警、阻塞弹窗、确认状态和清除后重触发预期。"
                },
                new AlarmDemoScenarioItem
                {
                    DisplayName = "持续异常",
                    Code = "DEMO.ALARM.LOCKED",
                    Name = "Demo 持续异常报警",
                    Category = "Demo/Policy",
                    Message = "用于验证清除后源头仍异常时应再次触发的报警",
                    Severity = AlarmSeverity.Error,
                    PopupMode = AlarmPopupMode.Growl,
                    NeedAcknowledge = true,
                    AllowManualClear = false,
                    Description = "验证这类报警也允许手动清除，后续若源头继续上报会重新进入活动报警。"
                }
            };
        }

        public static AlarmDemoScenarioItem CreateRepeatScenario()
        {
            return new AlarmDemoScenarioItem
            {
                DisplayName = "重复触发",
                Code = RepeatCode,
                Name = "Demo 重复触发报警",
                Category = "Demo/Repeat",
                Message = "用于验证同一 Code/Source/Location 下触发次数累加",
                Severity = AlarmSeverity.Warning,
                PopupMode = AlarmPopupMode.Growl,
                NeedAcknowledge = false,
                AllowManualClear = true,
                Description = "连续上报两次同一报警，活动列表中次数应累加。"
            };
        }

        public static IReadOnlyList<string> GetDemoCodes()
        {
            return new[]
            {
                "DEMO.ALARM.INFO",
                "DEMO.ALARM.WARNING",
                "DEMO.ALARM.ERROR",
                "DEMO.ALARM.FATAL",
                "DEMO.ALARM.LOCKED",
                RepeatCode
            };
        }
    }
}
