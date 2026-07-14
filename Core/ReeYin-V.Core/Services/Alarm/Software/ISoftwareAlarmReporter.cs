#nullable enable
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Software
{
    public interface ISoftwareAlarmReporter
    {
        AlarmInfo Report(
            string code,
            string source,
            string location,
            string message,
            AlarmSeverity? severity = null,
            IDictionary<string, object?>? extraData = null);

        AlarmInfo ReportModuleFailed(
            int serial,
            string moduleName,
            string message,
            Exception? exception = null);

        AlarmInfo ReportRecipeInvalid(
            string recipeName,
            string parameterName,
            string message);

        AlarmInfo ReportAlgorithmFailed(
            string algorithmName,
            string location,
            string message,
            Exception? exception = null);

        bool Clear(
            string code,
            string source,
            string location,
            string? user = "System",
            string? note = null);
    }
}
