#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public interface IHardwareAlarmReporter
    {
        AlarmInfo ReportConnectionFailed(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null);
        AlarmInfo ReportDisconnected(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null);
        AlarmInfo ReportInitializationFailed(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null);
        AlarmInfo ReportOperationFailed(string source, string location, string operation, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null);
        AlarmInfo ReportSafetyError(string source, string location, string message, Exception? exception = null, IDictionary<string, object?>? extraData = null);
        AlarmInfo ReportNoData(string source, string location, string message, IDictionary<string, object?>? extraData = null);
        AlarmInfo Report(AlarmReportRequest request);
        bool Clear(string code, string source, string location, string? user = null, string? note = null);
    }
}

