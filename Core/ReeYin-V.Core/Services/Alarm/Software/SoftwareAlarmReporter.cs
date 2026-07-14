#nullable enable
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Software
{
    [ExposedService(Lifetime.Singleton, 6, typeof(ISoftwareAlarmReporter))]
    public sealed class SoftwareAlarmReporter : ISoftwareAlarmReporter
    {
        private const string SoftwareSourceType = "Software";
        private const string ModuleExecuteFailedCode = "SW.MODULE.EXECUTE_FAILED";
        private const string RecipeInvalidParamCode = "SW.RECIPE.INVALID_PARAM";
        private const string AlgorithmFailedCode = "SW.ALGORITHM.FAILED";

        private readonly IAlarmService _alarmService;
        private readonly IAlarmDefinitionService _definitionService;
        private readonly Dictionary<string, DateTime> _lastReportByKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();

        public SoftwareAlarmReporter(IAlarmService alarmService, IAlarmDefinitionService definitionService)
        {
            _alarmService = alarmService;
            _definitionService = definitionService;
        }

        public AlarmInfo Report(
            string code,
            string source,
            string location,
            string message,
            AlarmSeverity? severity = null,
            IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(code, source, location, message, severity, null, extraData));
        }

        public AlarmInfo ReportModuleFailed(
            int serial,
            string moduleName,
            string message,
            Exception? exception = null)
        {
            string source = string.IsNullOrWhiteSpace(moduleName) ? "Module" : moduleName.Trim();
            string location = serial > 0 ? $"{serial:D3}" : "Unknown";
            Dictionary<string, object?> extraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Serial"] = serial,
                ["ModuleName"] = source
            };

            AlarmReportRequest request = CreateRequest(ModuleExecuteFailedCode, source, location, message, AlarmSeverity.Error, exception, extraData);
            request.Operation = "ExecuteModule";
            return Report(request);
        }

        public AlarmInfo ReportRecipeInvalid(
            string recipeName,
            string parameterName,
            string message)
        {
            string source = string.IsNullOrWhiteSpace(recipeName) ? "Recipe" : recipeName.Trim();
            string location = string.IsNullOrWhiteSpace(parameterName) ? "Unknown" : parameterName.Trim();
            Dictionary<string, object?> extraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["RecipeName"] = source,
                ["ParameterName"] = location
            };

            return Report(CreateRequest(RecipeInvalidParamCode, source, location, message, AlarmSeverity.Warning, null, extraData));
        }

        public AlarmInfo ReportAlgorithmFailed(
            string algorithmName,
            string location,
            string message,
            Exception? exception = null)
        {
            string source = string.IsNullOrWhiteSpace(algorithmName) ? "Algorithm" : algorithmName.Trim();
            Dictionary<string, object?> extraData = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["AlgorithmName"] = source
            };

            AlarmReportRequest request = CreateRequest(AlgorithmFailedCode, source, location, message, AlarmSeverity.Error, exception, extraData);
            request.Operation = "Algorithm";
            return Report(request);
        }

        public bool Clear(
            string code,
            string source,
            string location,
            string? user = "System",
            string? note = null)
        {
            try
            {
                string normalizedCode = NormalizeCode(code);
                string normalizedSource = NormalizeSource(source);
                string normalizedLocation = NormalizeLocation(location);
                bool cleared = _alarmService.ClearByKey(normalizedCode, normalizedSource, normalizedLocation, user, note, AlarmClearOrigin.Recovery).Success;
                if (cleared)
                {
                    ForgetThrottle(normalizedCode, normalizedSource, normalizedLocation);
                }

                return cleared;
            }
            catch
            {
                return false;
            }
        }

        private AlarmInfo Report(AlarmReportRequest request)
        {
            AlarmRaiseRequest? raiseRequest = null;
            try
            {
                raiseRequest = _definitionService.BuildRaiseRequest(NormalizeRequest(request));
                if (ShouldThrottle(raiseRequest))
                {
                    return new AlarmInfo
                    {
                        Code = raiseRequest.Code,
                        Name = raiseRequest.Name,
                        Category = raiseRequest.Category,
                        Message = raiseRequest.Message,
                        Level = raiseRequest.Level,
                        Source = raiseRequest.Source,
                        Location = raiseRequest.Location,
                        NeedAcknowledge = raiseRequest.NeedAcknowledge,
                        PopupMode = raiseRequest.PopupMode,
                        PopupThrottleSeconds = raiseRequest.PopupThrottleSeconds,
                        AllowManualClear = raiseRequest.AllowManualClear,
                        ExtraData = raiseRequest.ExtraData
                    };
                }

                return _alarmService.AddAlarm(raiseRequest);
            }
            catch
            {
                if (raiseRequest != null)
                {
                    ForgetThrottle(raiseRequest);
                }

                return new AlarmInfo
                {
                    Code = string.Empty,
                    Message = request?.Message ?? "Software alarm report failed.",
                    Level = request?.Severity ?? AlarmSeverity.Warning,
                    Source = request?.Source ?? SoftwareSourceType,
                    Location = request?.Location ?? "Unknown",
                    IsActive = false,
                    OccurrenceCount = 0,
                    ExtraData =
                    {
                        ["ReportFailed"] = true,
                        ["RequestedCode"] = request?.Code ?? string.Empty
                    }
                };
            }
        }

        private static AlarmReportRequest CreateRequest(
            string code,
            string source,
            string location,
            string message,
            AlarmSeverity? severity,
            Exception? exception,
            IDictionary<string, object?>? extraData)
        {
            return new AlarmReportRequest
            {
                Code = code,
                Source = source,
                SourceType = SoftwareSourceType,
                Location = location,
                Message = message,
                Severity = severity,
                Exception = exception,
                ExtraData = SafeCopyExtraData(extraData)
            };
        }

        private static AlarmReportRequest NormalizeRequest(AlarmReportRequest request)
        {
            request ??= new AlarmReportRequest();
            request.Code = NormalizeCode(request.Code);
            request.Source = NormalizeSource(request.Source);
            request.Location = NormalizeLocation(request.Location);
            request.Message = string.IsNullOrWhiteSpace(request.Message) ? request.Code : request.Message.Trim();
            request.SourceType = SoftwareSourceType;
            request.Operation = request.Operation?.Trim() ?? string.Empty;
            request.ErrorCode = request.ErrorCode?.Trim() ?? string.Empty;
            request.ExtraData = SafeCopyExtraData(request.ExtraData);
            return request;
        }

        private static Dictionary<string, object?> SafeCopyExtraData(IDictionary<string, object?>? extraData)
        {
            Dictionary<string, object?> copy = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (extraData == null)
            {
                return copy;
            }

            foreach (KeyValuePair<string, object?> item in extraData)
            {
                if (string.IsNullOrWhiteSpace(item.Key))
                {
                    continue;
                }

                copy[item.Key] = item.Value;
            }

            return copy;
        }

        private bool ShouldThrottle(AlarmRaiseRequest request)
        {
            if (request.Level >= AlarmSeverity.Fatal)
            {
                return false;
            }

            string key = CreateThrottleKey(request.Code, request.Source, request.Location);
            DateTime now = DateTime.Now;
            lock (_gate)
            {
                if (_lastReportByKey.TryGetValue(key, out DateTime last) && (now - last).TotalSeconds < 1)
                {
                    return true;
                }

                _lastReportByKey[key] = now;
                return false;
            }
        }

        private void ForgetThrottle(AlarmRaiseRequest request)
        {
            ForgetThrottle(request.Code, request.Source, request.Location);
        }

        private void ForgetThrottle(string code, string source, string location)
        {
            string key = CreateThrottleKey(code, source, location);
            lock (_gate)
            {
                _lastReportByKey.Remove(key);
            }
        }

        private static string CreateThrottleKey(string code, string source, string location)
        {
            return $"{NormalizeSource(source)}|{NormalizeCode(code)}|{NormalizeLocation(location)}";
        }

        private static string NormalizeCode(string? code)
        {
            return string.IsNullOrWhiteSpace(code) ? ModuleExecuteFailedCode : code.Trim();
        }

        private static string NormalizeSource(string? source)
        {
            return string.IsNullOrWhiteSpace(source) ? SoftwareSourceType : source.Trim();
        }

        private static string NormalizeLocation(string? location)
        {
            return string.IsNullOrWhiteSpace(location) ? "Unknown" : location.Trim();
        }
    }
}
