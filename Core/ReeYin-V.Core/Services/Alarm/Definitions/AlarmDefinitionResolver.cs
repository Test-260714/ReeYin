#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public static class AlarmDefinitionResolver
    {
        public static bool TryBuildRaiseRequest(AlarmReportRequest request, AlarmDefinitionInfo? definition, out AlarmRaiseRequest raiseRequest)
        {
            raiseRequest = new AlarmRaiseRequest();
            if (definition != null && !definition.Enabled)
            {
                return false;
            }

            raiseRequest = BuildRaiseRequest(request, definition);
            return true;
        }

        public static AlarmRaiseRequest BuildRaiseRequest(AlarmReportRequest request, AlarmDefinitionInfo? definition)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string code = FirstNonEmpty(request.Code, definition?.Code);
            string source = FirstNonEmpty(request.Source, definition?.DefaultSource, request.SourceType, "Alarm");
            string location = FirstNonEmpty(request.Location, definition?.DefaultLocation, "Unknown");
            string message = FirstNonEmpty(request.Message, definition?.Name, code);

            Dictionary<string, object?> extraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (definition?.ExtraTemplate != null)
            {
                foreach (KeyValuePair<string, object?> item in definition.ExtraTemplate)
                {
                    extraData[item.Key] = item.Value;
                }
            }

            foreach (KeyValuePair<string, object?> item in request.ExtraData ?? new Dictionary<string, object?>())
            {
                extraData[item.Key] = item.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Operation))
            {
                extraData["Operation"] = request.Operation.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.ErrorCode))
            {
                extraData["ErrorCode"] = request.ErrorCode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.SourceType))
            {
                extraData["SourceType"] = request.SourceType.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(definition?.SourceType))
            {
                extraData["SourceType"] = definition.SourceType.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.SuggestedAction))
            {
                extraData["SuggestedAction"] = request.SuggestedAction.Trim();
            }
            else if (!string.IsNullOrWhiteSpace(definition?.SuggestedAction))
            {
                extraData["SuggestedAction"] = definition.SuggestedAction.Trim();
            }

            if (request.Exception != null)
            {
                extraData["ExceptionType"] = request.Exception.GetType().FullName;
                extraData["ExceptionMessage"] = request.Exception.Message;
            }

            return new AlarmRaiseRequest
            {
                Code = code,
                Name = FirstNonEmpty(request.Name, definition?.Name, code),
                Category = FirstNonEmpty(request.Category, definition?.Category),
                Message = message,
                Level = request.Severity ?? definition?.Severity ?? AlarmSeverity.Warning,
                Source = source,
                Location = location,
                NeedAcknowledge = request.NeedAcknowledge ?? definition?.NeedAcknowledge ?? true,
                PopupMode = request.PopupMode ?? definition?.PopupMode ?? GetDefaultPopupMode(request.Severity ?? AlarmSeverity.Warning, request.NeedAcknowledge ?? true),
                PopupThrottleSeconds = Math.Max(0, request.PopupThrottleSeconds ?? definition?.PopupThrottleSeconds ?? 3),
                AllowManualClear = request.AllowManualClear ?? definition?.AllowManualClear ?? true,
                AcknowledgeResetMode = definition?.AcknowledgeResetMode ?? AlarmAcknowledgeResetMode.OnSeverityIncrease,
                ExtraData = extraData
            };
        }

        private static AlarmPopupMode GetDefaultPopupMode(AlarmSeverity severity, bool needAcknowledge)
        {
            if (severity >= AlarmSeverity.Fatal || needAcknowledge)
            {
                return AlarmPopupMode.Modal;
            }

            return severity >= AlarmSeverity.Warning
                ? AlarmPopupMode.Growl
                : AlarmPopupMode.None;
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (string? value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return string.Empty;
        }
    }
}

