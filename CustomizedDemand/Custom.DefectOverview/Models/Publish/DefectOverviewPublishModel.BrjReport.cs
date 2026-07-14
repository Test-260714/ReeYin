using Newtonsoft.Json;
using ReeYin_V.Core.Services.Project;
using ReeYin_V.Share;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Custom.DefectOverview.Models
{
    public sealed partial class DefectOverviewPublishModel
    {
        private static readonly string[] SnInputAliases =
        {
            "SN", "BatchSN", "BatchNo", "BatchNumber", "LotNo", "RollSN", "RollNo", "批次号", "卷号"
        };

        private static readonly string[] DetectMetersInputAliases =
        {
            "DetectMeters", "Meter", "Meters", "MeterValue", "RollMeters", "LengthMeters", "检测米数", "米数", "当前米数"
        };

        private static readonly string[] IsRollCompletedInputAliases =
        {
            "IsRollCompleted", "RollCompleted", "IsRollFinished", "RollFinished", "IsBatchCompleted", "换卷完成", "卷完成", "批次完成"
        };

        [JsonIgnore]
        private string _publishedSN = string.Empty;
        [OutputParam("SN", "BRJ report SN")]
        public string PublishedSN
        {
            get => _publishedSN;
            set => SetProperty(ref _publishedSN, value ?? string.Empty);
        }

        [JsonIgnore]
        private double _publishedDetectMeters;
        [OutputParam("DetectMeters", "BRJ report detect meters")]
        public double PublishedDetectMeters
        {
            get => _publishedDetectMeters;
            set => SetProperty(ref _publishedDetectMeters, value);
        }

        [JsonIgnore]
        private bool _publishedIsRollCompleted;
        [OutputParam("IsRollCompleted", "BRJ report roll completed flag")]
        public bool PublishedIsRollCompleted
        {
            get => _publishedIsRollCompleted;
            set => SetProperty(ref _publishedIsRollCompleted, value);
        }

        private void RefreshBrjReportOutputValues()
        {
            PublishedSN = ResolveReportString(SnInputAliases, PublishedSN);
            PublishedDetectMeters = ResolveReportDouble(DetectMetersInputAliases, PublishedDetectMeters);
            PublishedIsRollCompleted = ResolveReportBool(IsRollCompletedInputAliases, PublishedIsRollCompleted);
        }

        private string ResolveReportString(IEnumerable<string> aliases, string fallback)
        {
            object value = ResolveReportInputValue(aliases);
            return value == null ? fallback ?? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private double ResolveReportDouble(IEnumerable<string> aliases, double fallback)
        {
            object value = ResolveReportInputValue(aliases);
            if (value == null)
            {
                return fallback;
            }

            if (value is IConvertible)
            {
                try
                {
                    return Convert.ToDouble(value, CultureInfo.InvariantCulture);
                }
                catch
                {
                }
            }

            return double.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : fallback;
        }

        private bool ResolveReportBool(IEnumerable<string> aliases, bool fallback)
        {
            object value = ResolveReportInputValue(aliases);
            if (value == null)
            {
                return fallback;
            }

            if (value is bool boolValue)
            {
                return boolValue;
            }

            string text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return fallback;
            }

            if (bool.TryParse(text, out bool parsedBool))
            {
                return parsedBool;
            }

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
            {
                return Math.Abs(number) > double.Epsilon;
            }

            return string.Equals(text, "是", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "完成", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "结束", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "OK", StringComparison.OrdinalIgnoreCase);
        }

        private object ResolveReportInputValue(IEnumerable<string> aliases)
        {
            HashSet<string> aliasSet = aliases
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (TransmitParam param in EnumerateReportInputTransmitParams())
            {
                if (param == null)
                {
                    continue;
                }

                if (MatchesAnyAlias(param.ParamName, aliasSet)
                    || MatchesAnyAlias(param.Name, aliasSet)
                    || MatchesAnyAlias(param.Describe, aliasSet))
                {
                    return param.Value;
                }
            }

            if (moduleInputParam?.TransmitParams != null)
            {
                foreach (KeyValuePair<string, object> pair in moduleInputParam.TransmitParams)
                {
                    if (MatchesAnyAlias(pair.Key, aliasSet))
                    {
                        return pair.Value is TransmitParam transmitParam ? transmitParam.Value : pair.Value;
                    }
                }
            }

            return null;
        }

        private IEnumerable<TransmitParam> EnumerateReportInputTransmitParams()
        {
            foreach (TransmitParam param in InputParams ?? Enumerable.Empty<TransmitParam>())
            {
                yield return param;
            }

            if (moduleInputParam?.TransmitParams == null)
            {
                yield break;
            }

            foreach (object value in moduleInputParam.TransmitParams.Values)
            {
                if (value is TransmitParam param)
                {
                    yield return param;
                }
            }
        }

        private static bool MatchesAnyAlias(string value, HashSet<string> aliases)
        {
            return !string.IsNullOrWhiteSpace(value) && aliases.Contains(value.Trim());
        }
    }
}
