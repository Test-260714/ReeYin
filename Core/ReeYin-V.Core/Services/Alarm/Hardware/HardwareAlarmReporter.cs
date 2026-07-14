#nullable enable
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    [ExposedService(Lifetime.Singleton, 6, typeof(IHardwareAlarmReporter))]
    public sealed class HardwareAlarmReporter : IHardwareAlarmReporter
    {
        private readonly IAlarmService _alarmService;
        private readonly IAlarmDefinitionService _definitionService;
        private readonly Dictionary<string, DateTime> _lastReportByKey = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();

        public HardwareAlarmReporter(IAlarmService alarmService, IAlarmDefinitionService definitionService)
        {
            _alarmService = alarmService;
            _definitionService = definitionService;
        }

        public AlarmInfo ReportConnectionFailed(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.ConnectionFailed, source, location, message, HardwareAlarmSources.Hardware, string.Empty, exception, extraData));
        }

        public AlarmInfo ReportDisconnected(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.Disconnected, source, location, message, HardwareAlarmSources.Hardware, string.Empty, exception, extraData));
        }

        public AlarmInfo ReportInitializationFailed(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.InitializationFailed, source, location, message, HardwareAlarmSources.Hardware, "Init", exception, extraData));
        }

        public AlarmInfo ReportOperationFailed(string source, string location, string operation, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.OperationFailed, source, location, message, HardwareAlarmSources.Hardware, operation, exception, extraData));
        }

        public AlarmInfo ReportSafetyError(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null)
        {
            AlarmReportRequest request = CreateRequest(HardwareAlarmCodes.SafetyError, source, location, message, HardwareAlarmSources.Hardware, "Safety", exception, extraData);
            request.Severity = AlarmSeverity.Fatal;
            request.AllowManualClear = false;
            return Report(request);
        }

        public AlarmInfo ReportNoData(string source, string location, string message, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.SensorNoData, source, location, message, HardwareAlarmSources.Sensor, "ReadData", null, extraData));
        }

        public AlarmInfo Report(AlarmReportRequest request)
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
                    Message = request?.Message ?? "硬件报警上报失败",
                    Level = request?.Severity ?? AlarmSeverity.Warning,
                    Source = request?.Source ?? HardwareAlarmSources.Hardware,
                    Location = request?.Location ?? "Unknown",
                    ExtraData =
                    {
                        ["ReportFailed"] = true,
                        ["RequestedCode"] = request?.Code ?? string.Empty
                    }
                };
            }
        }

        public bool Clear(string code, string source, string location, string? user = null, string? note = null)
        {
            try
            {
                AlarmReportRequest request = NormalizeRequest(new AlarmReportRequest
                {
                    Code = code,
                    Source = source,
                    Location = location
                });

                bool cleared = _alarmService.ClearByKey(request.Code, request.Source, request.Location, user, note, AlarmClearOrigin.Recovery).Success;
                if (cleared)
                {
                    ForgetThrottle(request.Code, request.Source, request.Location);
                }

                return cleared;
            }
            catch
            {
                return false;
            }
        }

        private static AlarmReportRequest CreateRequest(string code, string source, string location, string message, string sourceType, string operation, Exception? exception, IDictionary<string, object?>? extraData)
        {
            return new AlarmReportRequest
            {
                Code = code,
                Source = source,
                SourceType = sourceType,
                Location = location,
                Message = message,
                Operation = operation,
                Exception = exception,
                ExtraData = extraData == null
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, object?>(extraData, StringComparer.OrdinalIgnoreCase)
            };
        }

        private static AlarmReportRequest NormalizeRequest(AlarmReportRequest request)
        {
            request ??= new AlarmReportRequest();
            request.Code = string.IsNullOrWhiteSpace(request.Code) ? HardwareAlarmCodes.OperationFailed : request.Code.Trim();
            request.Source = string.IsNullOrWhiteSpace(request.Source) ? HardwareAlarmSources.Hardware : request.Source.Trim();
            request.Location = string.IsNullOrWhiteSpace(request.Location) ? "Unknown" : request.Location.Trim();
            request.Message = string.IsNullOrWhiteSpace(request.Message) ? request.Code : request.Message.Trim();
            request.SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? request.Source : request.SourceType.Trim();
            request.Operation = request.Operation?.Trim() ?? string.Empty;
            request.ErrorCode = request.ErrorCode?.Trim() ?? string.Empty;
            request.ExtraData ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            return request;
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
            return $"{source}|{code}|{location}";
        }
    }
}
