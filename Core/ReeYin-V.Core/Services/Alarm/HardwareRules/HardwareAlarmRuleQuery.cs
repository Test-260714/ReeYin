#nullable enable
namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string DefinitionCode { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public bool? Enabled { get; set; }
        public bool IncludeSystem { get; set; } = true;
        public int MaxCount { get; set; } = 500;
    }
}
