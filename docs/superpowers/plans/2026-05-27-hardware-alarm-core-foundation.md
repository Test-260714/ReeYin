# Hardware Alarm Core Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the Core-side foundation for hardware alarm definitions, unified hardware alarm reporting, and automatic hardware state monitoring.

**Architecture:** Keep `IAlarmService` as the lifecycle engine. Add a definition resolver that maps hardware-domain requests to `AlarmRaiseRequest`, a reporter that hardware modules call, and a monitor that converts `HardwareStatusChangedEvent` into report/clear actions.

**Tech Stack:** C# 12, .NET 8 WPF class libraries, Prism, SqlSugar, Newtonsoft.Json, existing ReeYin-V Core services.

---

## Scope

This plan implements the first usable increment from `docs/superpowers/specs/2026-05-27-hardware-alarm-extension-design.md`:

- Alarm definition models, table entity, defaults, resolver, and service.
- Hardware alarm reporter and request model.
- Hardware state policy and monitor service.
- Core module registration and `PrismProvider` exposure.
- A local scratch check project that exercises the pure mapping and reporter behavior without external test packages.

AlarmCenter rule editing UI, hardware module pilot edits, event audit tables, shelving, suppression, and notification routing are separate increments after this foundation is green.

## File Structure

- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionEntity.cs` - SqlSugar entity for `alarm_definition`.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionInfo.cs` - UI/service DTO for alarm rules.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionQuery.cs` - query filters for rule listing.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\IAlarmDefinitionService.cs` - service contract.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionResolver.cs` - pure conversion from hardware request + definition to `AlarmRaiseRequest`.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionService.cs` - persisted definition service and built-in seed logic.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRequest.cs` - hardware-domain alarm input model.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\IHardwareAlarmReporter.cs` - hardware alarm reporting contract.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmReporter.cs` - reporter implementation using `IAlarmDefinitionService` and `IAlarmService`.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCodes.cs` - built-in code constants.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmSources.cs` - source type constants.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCategories.cs` - category constants.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRuleDefaults.cs` - built-in definitions.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStatePolicy.cs` - pure state-to-alarm mapping.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStateSnapshot.cs` - monitor memory state.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmMonitorService.cs` - subscribes to hardware status events.
- Modify: `Core\ReeYin-V.Core\CoreModule.cs` - register `AlarmDefinitionEntity` table.
- Modify: `Core\ReeYin-V.Core\IOC\PrismProvider.cs` - expose definition service and hardware reporter.
- Create: `Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj` - local executable check project.
- Create: `Scratch\AlarmCoreChecks\Program.cs` - assertion checks for resolver, reporter, and state policy.

## Task 1: Add The Failing Core Check Project

**Files:**
- Create: `Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj`
- Create: `Scratch\AlarmCoreChecks\Program.cs`

- [ ] **Step 1: Create the scratch check project**

Create `Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\Core\ReeYin-V.Core\ReeYin_V.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write the failing checks**

Create `Scratch\AlarmCoreChecks\Program.cs`:

```csharp
using ReeYin_V.Core;
using ReeYin_V.Core.Services.Alarm;
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using ReeYin_V.Core.Services.Alarm.Monitoring;

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertEqual<T>(T expected, T actual, string message)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"{message}. Expected={expected}, Actual={actual}");
    }
}

var definition = new AlarmDefinitionInfo
{
    Code = HardwareAlarmCodes.ConnectionFailed,
    Name = "硬件连接失败",
    Category = HardwareAlarmCategories.Communication,
    SourceType = HardwareAlarmSources.Plc,
    Severity = AlarmSeverity.Error,
    NeedAcknowledge = true,
    AllowManualClear = false,
    Enabled = true,
    SuggestedAction = "检查网线、IP、端口和设备电源。"
};

var request = new HardwareAlarmRequest
{
    Code = HardwareAlarmCodes.ConnectionFailed,
    Source = "PLC",
    SourceType = HardwareAlarmSources.Plc,
    Location = "PLC:Main",
    Message = "PLC 连接失败",
    ExtraData =
    {
        ["Ip"] = "192.168.1.10"
    }
};

AlarmRaiseRequest raiseRequest = AlarmDefinitionResolver.BuildRaiseRequest(request, definition);
AssertEqual(HardwareAlarmCodes.ConnectionFailed, raiseRequest.Code, "Resolver keeps code");
AssertEqual("硬件连接失败", raiseRequest.Name, "Resolver applies definition name");
AssertEqual(AlarmSeverity.Error, raiseRequest.Level, "Resolver applies definition severity");
AssertTrue(!raiseRequest.AllowManualClear, "Connection failure is not manually clearable");
AssertEqual("检查网线、IP、端口和设备电源。", raiseRequest.ExtraData["SuggestedAction"], "Suggested action is copied to extra data");
AssertEqual("192.168.1.10", raiseRequest.ExtraData["Ip"], "Request extra data is preserved");

var disabledDefinition = definition.CreateCopy();
disabledDefinition.Enabled = false;
AssertTrue(!AlarmDefinitionResolver.TryBuildRaiseRequest(request, disabledDefinition, out _), "Disabled definitions do not raise alarms");

var alarmService = new RecordingAlarmService();
var definitionService = new InMemoryAlarmDefinitionService(definition);
var reporter = new HardwareAlarmReporter(alarmService, definitionService);

AlarmInfo alarm = reporter.ReportConnectionFailed("PLC", "PLC:Main", "PLC 连接失败", null, new Dictionary<string, object?> { ["Ip"] = "192.168.1.10" });
AssertEqual(HardwareAlarmCodes.ConnectionFailed, alarm.Code, "Reporter returns alarm snapshot");
AssertEqual(1, alarmService.AddRequests.Count, "Reporter calls alarm service once");
AssertEqual("PLC:Main", alarmService.AddRequests[0].Location, "Reporter forwards location");

bool cleared = reporter.Clear(HardwareAlarmCodes.ConnectionFailed, "PLC", "PLC:Main", "System", "连接恢复");
AssertTrue(cleared, "Reporter clear returns service result");
AssertEqual(1, alarmService.ClearRequests.Count, "Reporter calls clear once");

HardwareAlarmStateAction disconnectedAction = HardwareAlarmStatePolicy.Resolve(HardwareState.NotConnected, false);
AssertTrue(disconnectedAction.ShouldRaise, "NotConnected raises disconnected alarm");
AssertEqual(HardwareAlarmCodes.Disconnected, disconnectedAction.Code, "NotConnected maps to disconnected code");

HardwareAlarmStateAction readyAction = HardwareAlarmStatePolicy.Resolve(HardwareState.Ready, true);
AssertTrue(readyAction.ShouldClear, "Ready clears communication alarms");

Console.WriteLine("Alarm core checks passed.");

internal sealed class RecordingAlarmService : IAlarmService
{
    public event EventHandler<AlarmDataChangedEventArgs>? DataChanged;

    public int MaxCacheCount => 1000;

    public List<AlarmRaiseRequest> AddRequests { get; } = new();

    public List<(string Code, string? Source, string? User, string? Note, string? Location)> ClearRequests { get; } = new();

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public AlarmInfo AddAlarm(string code, string message, AlarmSeverity level, string source)
    {
        return AddAlarm(new AlarmRaiseRequest { Code = code, Message = message, Level = level, Source = source });
    }

    public AlarmInfo AddAlarm(AlarmRaiseRequest request)
    {
        AddRequests.Add(request);
        return new AlarmInfo
        {
            Id = "alarm-1",
            Code = request.Code,
            Name = request.Name,
            Category = request.Category,
            Message = request.Message,
            Level = request.Level,
            Source = request.Source,
            Location = request.Location,
            NeedAcknowledge = request.NeedAcknowledge,
            AllowManualClear = request.AllowManualClear,
            ExtraData = new Dictionary<string, object?>(request.ExtraData)
        };
    }

    public bool ClearAlarm(string code, string? source = null, string? user = null, string? note = null, string? location = null)
    {
        ClearRequests.Add((code, source, user, note, location));
        return true;
    }

    public bool ConfirmAlarm(string id, string user, string? note = null) => true;

    public Task AcknowledgeAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ClearAsync(string activeId, string user, string? note = null, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<AlarmDashboardSnapshot> GetDashboardAsync(CancellationToken cancellationToken = default) => Task.FromResult(new AlarmDashboardSnapshot());

    public Task<IReadOnlyList<AlarmActiveRecord>> GetActiveAlarmsAsync(AlarmActiveQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AlarmActiveRecord>>(Array.Empty<AlarmActiveRecord>());

    public Task<AlarmPagedResult<AlarmHistoryEntry>> GetHistoryPageAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new AlarmPagedResult<AlarmHistoryEntry>());

    public Task<IReadOnlyList<AlarmHistoryEntry>> GetHistoryAsync(AlarmHistoryQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AlarmHistoryEntry>>(Array.Empty<AlarmHistoryEntry>());

    public Task<AlarmStatisticsResult> GetStatisticsAsync(AlarmStatisticsQuery query, CancellationToken cancellationToken = default) => Task.FromResult(new AlarmStatisticsResult());

    public Task<IReadOnlyList<string>> GetSourcesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<IReadOnlyList<AlarmRealtimeEntry>> GetRealtimeFeedAsync(int maxCount = 200, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AlarmRealtimeEntry>>(Array.Empty<AlarmRealtimeEntry>());

    public Task<string> ExportHistoryAsync(AlarmHistoryQuery query, string outputDirectory, AlarmExportFormat format = AlarmExportFormat.Csv, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
}

internal sealed class InMemoryAlarmDefinitionService : IAlarmDefinitionService
{
    private readonly Dictionary<string, AlarmDefinitionInfo> _definitions;

    public InMemoryAlarmDefinitionService(params AlarmDefinitionInfo[] definitions)
    {
        _definitions = definitions.ToDictionary(item => item.Code, item => item, StringComparer.OrdinalIgnoreCase);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<AlarmDefinitionInfo>> GetDefinitionsAsync(AlarmDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<AlarmDefinitionInfo>>(_definitions.Values.Select(item => item.CreateCopy()).ToArray());
    }

    public Task<AlarmDefinitionInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        _definitions.TryGetValue(code, out AlarmDefinitionInfo? definition);
        return Task.FromResult(definition?.CreateCopy());
    }

    public Task SaveAsync(AlarmDefinitionInfo definition, string operatorName, CancellationToken cancellationToken = default)
    {
        _definitions[definition.Code] = definition.CreateCopy();
        return Task.CompletedTask;
    }

    public Task SetEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default)
    {
        if (_definitions.TryGetValue(code, out AlarmDefinitionInfo? definition))
        {
            definition.Enabled = enabled;
        }

        return Task.CompletedTask;
    }

    public AlarmRaiseRequest BuildRaiseRequest(HardwareAlarmRequest request)
    {
        _definitions.TryGetValue(request.Code, out AlarmDefinitionInfo? definition);
        return AlarmDefinitionResolver.BuildRaiseRequest(request, definition);
    }
}
```

- [ ] **Step 3: Run the checks and verify RED**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: FAIL at compile time because namespaces such as `ReeYin_V.Core.Services.Alarm.Definitions` and types such as `HardwareAlarmReporter` do not exist.

## Task 2: Add Alarm Definition Models And Resolver

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionEntity.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionInfo.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionQuery.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\IAlarmDefinitionService.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionResolver.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRequest.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCodes.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmSources.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCategories.cs`

- [ ] **Step 1: Add the entity**

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionEntity.cs`:

```csharp
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    [SugarTable("alarm_definition", TableDescription = "报警定义和硬件报警规则")]
    public sealed class AlarmDefinitionEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = false)]
        public string Code { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string Category { get; set; } = string.Empty;

        [SugarColumn(Length = 64, IsNullable = true)]
        public string SourceType { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string DefaultSource { get; set; } = string.Empty;

        [SugarColumn(Length = 128, IsNullable = true)]
        public string DefaultLocation { get; set; } = string.Empty;

        public int SeverityValue { get; set; }

        public bool NeedAcknowledge { get; set; } = true;

        public bool AllowManualClear { get; set; } = true;

        public bool AutoClearOnRecovery { get; set; } = true;

        public int DebounceMilliseconds { get; set; }

        public int ThrottleSeconds { get; set; } = 1;

        public bool Enabled { get; set; } = true;

        public bool IsSystem { get; set; }

        [SugarColumn(Length = 512, IsNullable = true)]
        public string SuggestedAction { get; set; } = string.Empty;

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string ExtraTemplateJson { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 2: Add the DTO**

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionInfo.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public sealed class AlarmDefinitionInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string DefaultSource { get; set; } = string.Empty;
        public string DefaultLocation { get; set; } = string.Empty;
        public AlarmSeverity Severity { get; set; } = AlarmSeverity.Warning;
        public bool NeedAcknowledge { get; set; } = true;
        public bool AllowManualClear { get; set; } = true;
        public bool AutoClearOnRecovery { get; set; } = true;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public string SuggestedAction { get; set; } = string.Empty;
        public IDictionary<string, object?> ExtraTemplate { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public AlarmDefinitionInfo CreateCopy()
        {
            return new AlarmDefinitionInfo
            {
                Id = Id,
                Code = Code,
                Name = Name,
                Category = Category,
                SourceType = SourceType,
                DefaultSource = DefaultSource,
                DefaultLocation = DefaultLocation,
                Severity = Severity,
                NeedAcknowledge = NeedAcknowledge,
                AllowManualClear = AllowManualClear,
                AutoClearOnRecovery = AutoClearOnRecovery,
                DebounceMilliseconds = DebounceMilliseconds,
                ThrottleSeconds = ThrottleSeconds,
                Enabled = Enabled,
                IsSystem = IsSystem,
                SuggestedAction = SuggestedAction,
                ExtraTemplate = new Dictionary<string, object?>(ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
```

- [ ] **Step 3: Add the query model**

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionQuery.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Models;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public sealed class AlarmDefinitionQuery
    {
        public string Keyword { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public AlarmSeverity? Severity { get; set; }
        public bool? Enabled { get; set; }
        public bool IncludeSystem { get; set; } = true;
        public int MaxCount { get; set; } = 500;
    }
}
```

- [ ] **Step 4: Add hardware constants and request**

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCodes.cs`:

```csharp
namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public static class HardwareAlarmCodes
    {
        public const string ConnectionFailed = "HW.COMM.CONNECTION_FAILED";
        public const string Disconnected = "HW.COMM.DISCONNECTED";
        public const string InitializationFailed = "HW.INITIALIZATION_FAILED";
        public const string OperationFailed = "HW.OPERATION_FAILED";
        public const string SafetyError = "HW.SAFETY_ERROR";
        public const string ConfigurationInvalid = "HW.CONFIG.INVALID";
        public const string PlcHeartbeatTimeout = "HW.PLC.HEARTBEAT_TIMEOUT";
        public const string PlcReadWriteFailed = "HW.PLC.READ_WRITE_FAILED";
        public const string PlcCommandTimeout = "HW.PLC.COMMAND_TIMEOUT";
        public const string MotionControllerError = "HW.MOTION.CONTROLLER_ERROR";
        public const string MotionServoAlarm = "HW.MOTION.SERVO_ALARM";
        public const string MotionLimitTriggered = "HW.MOTION.LIMIT_TRIGGERED";
        public const string MotionSafetyError = "HW.MOTION.SAFETY_ERROR";
        public const string SensorAcquireFailed = "HW.SENSOR.ACQUIRE_FAILED";
        public const string SensorReadResultFailed = "HW.SENSOR.READ_RESULT_FAILED";
        public const string SensorNoData = "HW.SENSOR.NO_DATA";
        public const string SensorZAxisFailed = "HW.SENSOR.Z_AXIS_FAILED";
        public const string CameraCaptureFailed = "HW.CAMERA.CAPTURE_FAILED";
        public const string LightControlFailed = "HW.LIGHT.CONTROL_FAILED";
    }
}
```

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmSources.cs`:

```csharp
namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public static class HardwareAlarmSources
    {
        public const string Hardware = "Hardware";
        public const string Plc = "PLC";
        public const string MotionCard = "MotionCard";
        public const string Sensor = "Sensor";
        public const string Camera = "Camera";
        public const string LightController = "LightController";
        public const string ControlCard = "ControlCard";
    }
}
```

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmCategories.cs`:

```csharp
namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public static class HardwareAlarmCategories
    {
        public const string Communication = "硬件通信";
        public const string Initialization = "硬件初始化";
        public const string Operation = "硬件操作";
        public const string MotionSafety = "运动安全";
        public const string Acquisition = "采集设备";
        public const string Configuration = "配置错误";
    }
}
```

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRequest.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public sealed class HardwareAlarmRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public AlarmSeverity? Severity { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public bool? NeedAcknowledge { get; set; }
        public bool? AllowManualClear { get; set; }
        public Exception? Exception { get; set; }
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 5: Add the service contract and resolver**

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\IAlarmDefinitionService.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public interface IAlarmDefinitionService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AlarmDefinitionInfo>> GetDefinitionsAsync(AlarmDefinitionQuery query, CancellationToken cancellationToken = default);

        Task<AlarmDefinitionInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

        Task SaveAsync(AlarmDefinitionInfo definition, string operatorName, CancellationToken cancellationToken = default);

        Task SetEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default);

        AlarmRaiseRequest BuildRaiseRequest(HardwareAlarmRequest request);
    }
}
```

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionResolver.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    public static class AlarmDefinitionResolver
    {
        public static bool TryBuildRaiseRequest(HardwareAlarmRequest request, AlarmDefinitionInfo? definition, out AlarmRaiseRequest raiseRequest)
        {
            raiseRequest = new AlarmRaiseRequest();
            if (definition != null && !definition.Enabled)
            {
                return false;
            }

            raiseRequest = BuildRaiseRequest(request, definition);
            return true;
        }

        public static AlarmRaiseRequest BuildRaiseRequest(HardwareAlarmRequest request, AlarmDefinitionInfo? definition)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            string code = FirstNonEmpty(request.Code, definition?.Code);
            string source = FirstNonEmpty(request.Source, definition?.DefaultSource, HardwareAlarmSources.Hardware);
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

            if (!string.IsNullOrWhiteSpace(definition?.SuggestedAction))
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
                AllowManualClear = request.AllowManualClear ?? definition?.AllowManualClear ?? true,
                ExtraData = extraData
            };
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
```

- [ ] **Step 6: Run the checks**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: FAIL at compile time because `HardwareAlarmReporter`, `HardwareAlarmStatePolicy`, and `AlarmDefinitionService` do not exist yet; resolver-related missing type errors should be gone.

## Task 3: Add Built-In Definition Defaults And Persisted Service

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRuleDefaults.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionService.cs`

- [ ] **Step 1: Add built-in definitions**

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmRuleDefaults.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Models;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    public static class HardwareAlarmRuleDefaults
    {
        public static IReadOnlyList<AlarmDefinitionInfo> CreateDefaults()
        {
            return new[]
            {
                Create(HardwareAlarmCodes.ConnectionFailed, "硬件连接失败", HardwareAlarmCategories.Communication, HardwareAlarmSources.Hardware, AlarmSeverity.Error, false, "检查网线、IP、端口和设备电源。"),
                Create(HardwareAlarmCodes.Disconnected, "硬件断线", HardwareAlarmCategories.Communication, HardwareAlarmSources.Hardware, AlarmSeverity.Error, false, "检查设备连接状态并尝试重新连接。"),
                Create(HardwareAlarmCodes.InitializationFailed, "硬件初始化失败", HardwareAlarmCategories.Initialization, HardwareAlarmSources.Hardware, AlarmSeverity.Error, true, "检查配置参数和驱动初始化日志。"),
                Create(HardwareAlarmCodes.OperationFailed, "硬件操作失败", HardwareAlarmCategories.Operation, HardwareAlarmSources.Hardware, AlarmSeverity.Error, true, "检查操作参数、设备状态和异常信息。"),
                Create(HardwareAlarmCodes.SafetyError, "硬件安全异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.Hardware, AlarmSeverity.Fatal, false, "停止自动流程，确认安全条件并执行硬件复位。"),
                Create(HardwareAlarmCodes.PlcReadWriteFailed, "PLC 读写失败", HardwareAlarmCategories.Communication, HardwareAlarmSources.Plc, AlarmSeverity.Error, true, "检查 PLC 地址、通讯连接和数据类型。"),
                Create(HardwareAlarmCodes.PlcCommandTimeout, "PLC 指令超时", HardwareAlarmCategories.Operation, HardwareAlarmSources.Plc, AlarmSeverity.Warning, true, "检查 PLC 目标值、等待时间和外部设备动作。"),
                Create(HardwareAlarmCodes.MotionControllerError, "运动控制卡异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Error, false, "检查控制卡状态、驱动器和运动轴报警。"),
                Create(HardwareAlarmCodes.MotionSafetyError, "运动安全异常", HardwareAlarmCategories.MotionSafety, HardwareAlarmSources.MotionCard, AlarmSeverity.Fatal, false, "停止运动流程，确认限位、急停和伺服状态。"),
                Create(HardwareAlarmCodes.SensorAcquireFailed, "传感器采集失败", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Error, true, "检查传感器连接、曝光、触发和采集参数。"),
                Create(HardwareAlarmCodes.SensorReadResultFailed, "传感器结果读取失败", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Error, true, "检查结果缓存、SDK 返回码和采集流程。"),
                Create(HardwareAlarmCodes.SensorNoData, "传感器无有效数据", HardwareAlarmCategories.Acquisition, HardwareAlarmSources.Sensor, AlarmSeverity.Warning, true, "检查被测物、触发时序和数据输出配置。")
            };
        }

        private static AlarmDefinitionInfo Create(string code, string name, string category, string sourceType, AlarmSeverity severity, bool allowManualClear, string suggestedAction)
        {
            DateTime now = DateTime.Now;
            return new AlarmDefinitionInfo
            {
                Id = code.Replace(".", "_"),
                Code = code,
                Name = name,
                Category = category,
                SourceType = sourceType,
                Severity = severity,
                NeedAcknowledge = true,
                AllowManualClear = allowManualClear,
                AutoClearOnRecovery = true,
                DebounceMilliseconds = severity >= AlarmSeverity.Fatal ? 0 : 500,
                ThrottleSeconds = 1,
                Enabled = true,
                IsSystem = true,
                SuggestedAction = suggestedAction,
                CreatedAt = now,
                UpdatedAt = now
            };
        }
    }
}
```

- [ ] **Step 2: Add the service**

Create `Core\ReeYin-V.Core\Services\Alarm\Definitions\AlarmDefinitionService.cs`:

```csharp
using Newtonsoft.Json;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Alarm.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.Definitions
{
    [ExposedService(Lifetime.Singleton, 5, typeof(IAlarmDefinitionService))]
    public sealed class AlarmDefinitionService : IAlarmDefinitionService
    {
        private readonly ISqlSugarClient _database;
        private readonly SemaphoreSlim _initializeLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<string, AlarmDefinitionInfo> _cache = new Dictionary<string, AlarmDefinitionInfo>(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        public AlarmDefinitionService(ISqlSugarClient database)
        {
            _database = database;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            await _initializeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_initialized)
                {
                    return;
                }

                await SeedDefaultsAsync().ConfigureAwait(false);
                List<AlarmDefinitionEntity> entities = await _database.Queryable<AlarmDefinitionEntity>().ToListAsync().ConfigureAwait(false);
                lock (_cache)
                {
                    _cache.Clear();
                    foreach (AlarmDefinitionEntity entity in entities)
                    {
                        AlarmDefinitionInfo info = MapToInfo(entity);
                        _cache[info.Code] = info;
                    }

                    _initialized = true;
                }
            }
            finally
            {
                _initializeLock.Release();
            }
        }

        public async Task<IReadOnlyList<AlarmDefinitionInfo>> GetDefinitionsAsync(AlarmDefinitionQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            query ??= new AlarmDefinitionQuery();
            string keyword = query.Keyword?.Trim() ?? string.Empty;
            string sourceType = query.SourceType?.Trim() ?? string.Empty;
            string category = query.Category?.Trim() ?? string.Empty;

            lock (_cache)
            {
                IEnumerable<AlarmDefinitionInfo> definitions = _cache.Values;
                if (!query.IncludeSystem)
                {
                    definitions = definitions.Where(item => !item.IsSystem);
                }

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    definitions = definitions.Where(item =>
                        Contains(item.Code, keyword) ||
                        Contains(item.Name, keyword) ||
                        Contains(item.Category, keyword) ||
                        Contains(item.SourceType, keyword));
                }

                if (!string.IsNullOrWhiteSpace(sourceType))
                {
                    definitions = definitions.Where(item => item.SourceType.Equals(sourceType, StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(category))
                {
                    definitions = definitions.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
                }

                if (query.Severity.HasValue)
                {
                    definitions = definitions.Where(item => item.Severity == query.Severity.Value);
                }

                if (query.Enabled.HasValue)
                {
                    definitions = definitions.Where(item => item.Enabled == query.Enabled.Value);
                }

                return definitions
                    .OrderBy(item => item.Code)
                    .Take(query.MaxCount <= 0 ? 500 : query.MaxCount)
                    .Select(item => item.CreateCopy())
                    .ToArray();
            }
        }

        public async Task<AlarmDefinitionInfo?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            lock (_cache)
            {
                return _cache.TryGetValue(code.Trim(), out AlarmDefinitionInfo? definition)
                    ? definition.CreateCopy()
                    : null;
            }
        }

        public async Task SaveAsync(AlarmDefinitionInfo definition, string operatorName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (string.IsNullOrWhiteSpace(definition.Code))
            {
                throw new ArgumentException("Alarm definition code is required.", nameof(definition));
            }

            await InitializeAsync(cancellationToken).ConfigureAwait(false);
            definition.UpdatedAt = DateTime.Now;
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                definition.Id = Guid.NewGuid().ToString("N");
            }

            AlarmDefinitionEntity entity = MapToEntity(definition);
            bool exists = await _database.Queryable<AlarmDefinitionEntity>().AnyAsync(item => item.Code == definition.Code).ConfigureAwait(false);
            if (exists)
            {
                await _database.Updateable(entity).Where(item => item.Code == entity.Code).ExecuteCommandAsync().ConfigureAwait(false);
            }
            else
            {
                await _database.Insertable(entity).ExecuteCommandAsync().ConfigureAwait(false);
            }

            lock (_cache)
            {
                _cache[definition.Code] = definition.CreateCopy();
            }
        }

        public async Task SetEnabledAsync(string code, bool enabled, string operatorName, CancellationToken cancellationToken = default)
        {
            AlarmDefinitionInfo? definition = await FindByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (definition == null)
            {
                return;
            }

            definition.Enabled = enabled;
            await SaveAsync(definition, operatorName, cancellationToken).ConfigureAwait(false);
        }

        public AlarmRaiseRequest BuildRaiseRequest(HardwareAlarmRequest request)
        {
            EnsureInitialized();
            AlarmDefinitionInfo? definition = null;
            if (!string.IsNullOrWhiteSpace(request?.Code))
            {
                lock (_cache)
                {
                    _cache.TryGetValue(request.Code.Trim(), out definition);
                }
            }

            if (!AlarmDefinitionResolver.TryBuildRaiseRequest(request, definition, out AlarmRaiseRequest raiseRequest))
            {
                throw new InvalidOperationException($"Alarm definition is disabled: {request.Code}");
            }

            return raiseRequest;
        }

        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                InitializeAsync().GetAwaiter().GetResult();
            }
        }

        private async Task SeedDefaultsAsync()
        {
            foreach (AlarmDefinitionInfo definition in HardwareAlarmRuleDefaults.CreateDefaults())
            {
                bool exists = await _database.Queryable<AlarmDefinitionEntity>().AnyAsync(item => item.Code == definition.Code).ConfigureAwait(false);
                if (!exists)
                {
                    await _database.Insertable(MapToEntity(definition)).ExecuteCommandAsync().ConfigureAwait(false);
                }
            }
        }

        private static AlarmDefinitionInfo MapToInfo(AlarmDefinitionEntity entity)
        {
            return new AlarmDefinitionInfo
            {
                Id = entity.Id,
                Code = entity.Code,
                Name = entity.Name,
                Category = entity.Category,
                SourceType = entity.SourceType,
                DefaultSource = entity.DefaultSource,
                DefaultLocation = entity.DefaultLocation,
                Severity = (AlarmSeverity)entity.SeverityValue,
                NeedAcknowledge = entity.NeedAcknowledge,
                AllowManualClear = entity.AllowManualClear,
                AutoClearOnRecovery = entity.AutoClearOnRecovery,
                DebounceMilliseconds = entity.DebounceMilliseconds,
                ThrottleSeconds = entity.ThrottleSeconds,
                Enabled = entity.Enabled,
                IsSystem = entity.IsSystem,
                SuggestedAction = entity.SuggestedAction,
                ExtraTemplate = string.IsNullOrWhiteSpace(entity.ExtraTemplateJson)
                    ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    : JsonConvert.DeserializeObject<Dictionary<string, object?>>(entity.ExtraTemplateJson) ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        private static AlarmDefinitionEntity MapToEntity(AlarmDefinitionInfo info)
        {
            return new AlarmDefinitionEntity
            {
                Id = info.Id,
                Code = info.Code.Trim(),
                Name = info.Name?.Trim() ?? info.Code.Trim(),
                Category = info.Category?.Trim() ?? string.Empty,
                SourceType = info.SourceType?.Trim() ?? string.Empty,
                DefaultSource = info.DefaultSource?.Trim() ?? string.Empty,
                DefaultLocation = info.DefaultLocation?.Trim() ?? string.Empty,
                SeverityValue = (int)info.Severity,
                NeedAcknowledge = info.NeedAcknowledge,
                AllowManualClear = info.AllowManualClear,
                AutoClearOnRecovery = info.AutoClearOnRecovery,
                DebounceMilliseconds = Math.Max(0, info.DebounceMilliseconds),
                ThrottleSeconds = Math.Max(0, info.ThrottleSeconds),
                Enabled = info.Enabled,
                IsSystem = info.IsSystem,
                SuggestedAction = info.SuggestedAction?.Trim() ?? string.Empty,
                ExtraTemplateJson = JsonConvert.SerializeObject(info.ExtraTemplate ?? new Dictionary<string, object?>()),
                CreatedAt = info.CreatedAt == default ? DateTime.Now : info.CreatedAt,
                UpdatedAt = info.UpdatedAt == default ? DateTime.Now : info.UpdatedAt
            };
        }

        private static bool Contains(string? value, string keyword)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
```

- [ ] **Step 3: Run the checks**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: FAIL at compile time because `HardwareAlarmReporter` and `HardwareAlarmStatePolicy` do not exist yet; definition service errors should be gone.

## Task 4: Add Hardware Alarm Reporter

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\IHardwareAlarmReporter.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmReporter.cs`

- [ ] **Step 1: Add reporter contract**

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\IHardwareAlarmReporter.cs`:

```csharp
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
        AlarmInfo Report(HardwareAlarmRequest request);
        bool Clear(string code, string source, string location, string? user = null, string? note = null);
    }
}
```

- [ ] **Step 2: Add reporter implementation**

Create `Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmReporter.cs`:

```csharp
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
            HardwareAlarmRequest request = CreateRequest(HardwareAlarmCodes.SafetyError, source, location, message, HardwareAlarmSources.Hardware, "Safety", exception, extraData);
            request.Severity = AlarmSeverity.Fatal;
            request.AllowManualClear = false;
            return Report(request);
        }

        public AlarmInfo ReportNoData(string source, string location, string message, IDictionary<string, object?>? extraData = null)
        {
            return Report(CreateRequest(HardwareAlarmCodes.SensorNoData, source, location, message, HardwareAlarmSources.Sensor, "ReadData", null, extraData));
        }

        public AlarmInfo Report(HardwareAlarmRequest request)
        {
            try
            {
                AlarmRaiseRequest raiseRequest = _definitionService.BuildRaiseRequest(NormalizeRequest(request));
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
                        AllowManualClear = raiseRequest.AllowManualClear,
                        ExtraData = raiseRequest.ExtraData
                    };
                }

                return _alarmService.AddAlarm(raiseRequest);
            }
            catch
            {
                return new AlarmInfo
                {
                    Code = request?.Code ?? string.Empty,
                    Message = request?.Message ?? "硬件报警上报失败",
                    Level = request?.Severity ?? AlarmSeverity.Warning,
                    Source = request?.Source ?? HardwareAlarmSources.Hardware,
                    Location = request?.Location ?? "Unknown"
                };
            }
        }

        public bool Clear(string code, string source, string location, string? user = null, string? note = null)
        {
            try
            {
                return _alarmService.ClearAlarm(code, source, user, note, location);
            }
            catch
            {
                return false;
            }
        }

        private static HardwareAlarmRequest CreateRequest(string code, string source, string location, string message, string sourceType, string operation, Exception? exception, IDictionary<string, object?>? extraData)
        {
            return new HardwareAlarmRequest
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

        private static HardwareAlarmRequest NormalizeRequest(HardwareAlarmRequest request)
        {
            request ??= new HardwareAlarmRequest();
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

            string key = $"{request.Source}|{request.Code}|{request.Location}";
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
    }
}
```

- [ ] **Step 3: Run the checks**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: FAIL at compile time because `HardwareAlarmStatePolicy` does not exist yet; reporter-related errors should be gone.

## Task 5: Add Hardware State Policy And Monitor

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStatePolicy.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStateSnapshot.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmMonitorService.cs`

- [ ] **Step 1: Add the state action policy**

Create `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStatePolicy.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.Hardware;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    public sealed class HardwareAlarmStateAction
    {
        public bool ShouldRaise { get; set; }
        public bool ShouldClear { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public static class HardwareAlarmStatePolicy
    {
        public static HardwareAlarmStateAction Resolve(HardwareState state, bool wasInAlarm)
        {
            return state switch
            {
                HardwareState.NotConnected => new HardwareAlarmStateAction
                {
                    ShouldRaise = true,
                    Code = HardwareAlarmCodes.Disconnected,
                    Message = "硬件未连接或已断开。"
                },
                HardwareState.Error => new HardwareAlarmStateAction
                {
                    ShouldRaise = true,
                    Code = HardwareAlarmCodes.OperationFailed,
                    Message = "硬件状态进入错误。"
                },
                HardwareState.Connected or HardwareState.Ready => new HardwareAlarmStateAction
                {
                    ShouldClear = wasInAlarm,
                    Code = HardwareAlarmCodes.Disconnected,
                    Message = "硬件连接恢复。"
                },
                HardwareState.Complete => new HardwareAlarmStateAction
                {
                    ShouldClear = wasInAlarm,
                    Code = HardwareAlarmCodes.OperationFailed,
                    Message = "硬件操作恢复完成。"
                },
                _ => new HardwareAlarmStateAction()
            };
        }
    }
}
```

- [ ] **Step 2: Add monitor state snapshot**

Create `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmStateSnapshot.cs`:

```csharp
using System;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    internal sealed class HardwareAlarmStateSnapshot
    {
        public string Name { get; set; } = string.Empty;
        public HardwareState State { get; set; }
        public bool IsInAlarm { get; set; }
        public string ActiveCode { get; set; } = string.Empty;
        public DateTime LastChangedAt { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 3: Add monitor service**

Create `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmMonitorService.cs`:

```csharp
using Prism.Events;
using ReeYin_V.Core.Events.Hardware;
using ReeYin_V.Core.IOC;
using ReeYin_V.Core.Services.Alarm.Hardware;
using ReeYin_V.Core.Services.Module;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Monitoring
{
    [ExposedService(Lifetime.Singleton, 7, AutoInitialize = true)]
    public sealed class HardwareAlarmMonitorService
    {
        private readonly IHardwareAlarmReporter _reporter;
        private readonly Dictionary<string, HardwareAlarmStateSnapshot> _snapshots = new Dictionary<string, HardwareAlarmStateSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly object _gate = new object();
        private SubscriptionToken? _subscriptionToken;

        public HardwareAlarmMonitorService(IEventAggregator eventAggregator, IHardwareAlarmReporter reporter)
        {
            _reporter = reporter;
            _subscriptionToken = eventAggregator.GetEvent<HardwareStatusChangedEvent>().Subscribe(OnHardwareStatusChanged, ThreadOption.PublisherThread, false);
        }

        private void OnHardwareStatusChanged(HardwareStatus status)
        {
            if (status == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(status.Name) ? "UnknownHardware" : status.Name.Trim();
            HardwareAlarmStateSnapshot snapshot;
            HardwareAlarmStateAction action;
            lock (_gate)
            {
                if (!_snapshots.TryGetValue(name, out snapshot))
                {
                    snapshot = new HardwareAlarmStateSnapshot { Name = name };
                    _snapshots[name] = snapshot;
                }

                action = HardwareAlarmStatePolicy.Resolve(status.Status, snapshot.IsInAlarm);
                snapshot.State = status.Status;
                snapshot.LastChangedAt = DateTime.Now;
            }

            string location = $"Hardware:{name}";
            if (action.ShouldRaise)
            {
                _reporter.Report(new HardwareAlarmRequest
                {
                    Code = action.Code,
                    Source = HardwareAlarmSources.Hardware,
                    SourceType = HardwareAlarmSources.Hardware,
                    Location = location,
                    Message = string.IsNullOrWhiteSpace(status.Describe) ? action.Message : status.Describe,
                    ExtraData =
                    {
                        ["HardwareName"] = name,
                        ["HardwareState"] = status.Status.ToString(),
                        ["IsConnect"] = status.IsConnect
                    }
                });

                lock (_gate)
                {
                    snapshot.IsInAlarm = true;
                    snapshot.ActiveCode = action.Code;
                }
            }

            if (action.ShouldClear)
            {
                string codeToClear;
                lock (_gate)
                {
                    codeToClear = string.IsNullOrWhiteSpace(snapshot.ActiveCode) ? action.Code : snapshot.ActiveCode;
                    snapshot.IsInAlarm = false;
                    snapshot.ActiveCode = string.Empty;
                }

                _reporter.Clear(codeToClear, HardwareAlarmSources.Hardware, location, "System", action.Message);
            }
        }
    }
}
```

- [ ] **Step 4: Run the checks**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: PASS and print `Alarm core checks passed.`

## Task 6: Wire Core Registration

**Files:**
- Modify: `Core\ReeYin-V.Core\CoreModule.cs`
- Modify: `Core\ReeYin-V.Core\IOC\PrismProvider.cs`

- [ ] **Step 1: Update CoreModule table registration**

Modify `Core\ReeYin-V.Core\CoreModule.cs`.

Add using:

```csharp
using ReeYin_V.Core.Services.Alarm.Definitions;
```

Change CodeFirst registration so the table list includes `AlarmDefinitionEntity`:

```csharp
sqlSugarClient.CodeFirst
    .SetStringDefaultLength(200)
    .InitTables(
        typeof(User),
        typeof(Dict),
        typeof(Menu),
        typeof(Role),
        typeof(Permission),
        typeof(PermMenuRelation),
        typeof(ModuleLoadConfig),
        typeof(AlarmRecordEntity),
        typeof(AlarmDefinitionEntity)
    );
```

- [ ] **Step 2: Update PrismProvider constructor and properties**

Modify `Core\ReeYin-V.Core\IOC\PrismProvider.cs`.

Add usings:

```csharp
using ReeYin_V.Core.Services.Alarm.Definitions;
using ReeYin_V.Core.Services.Alarm.Hardware;
```

Add constructor parameters after `IAlarmService alarmService`:

```csharp
IAlarmDefinitionService alarmDefinitionService,
IHardwareAlarmReporter hardwareAlarmReporter,
```

Set the static properties in the constructor:

```csharp
AlarmDefinitionService = alarmDefinitionService;
HardwareAlarmReporter = hardwareAlarmReporter;
```

Add static properties after `AlarmService`:

```csharp
/// <summary>
/// 报警定义服务
/// </summary>
public static IAlarmDefinitionService AlarmDefinitionService { get; private set; }

/// <summary>
/// 硬件报警上报器
/// </summary>
public static IHardwareAlarmReporter HardwareAlarmReporter { get; private set; }
```

- [ ] **Step 3: Run the checks**

Run:

```powershell
dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore
```

Expected: PASS and print `Alarm core checks passed.`

## Task 7: Build Verification

**Files:**
- Verify: `Core\ReeYin-V.Core\ReeYin_V.Core.csproj`
- Verify: `Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj`

- [ ] **Step 1: Build Core**

Run:

```powershell
dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore
```

Expected: build succeeds with 0 errors. Existing warnings may remain, but new files should not introduce errors.

- [ ] **Step 2: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: build succeeds with 0 errors. If this project exposes pre-existing reference issues unrelated to the new Core files, capture the first failing project and build `Core\ReeYin-V.Core\ReeYin_V.Core.csproj` again to isolate the new work.

- [ ] **Step 3: Keep the scratch check project as the regression harness**

Keep `Scratch\AlarmCoreChecks` so the next increments can verify resolver, reporter, and state policy behavior without adding external test packages.

Expected: `Scratch\AlarmCoreChecks\Program.cs` remains runnable with `dotnet run --project Scratch\AlarmCoreChecks\AlarmCoreChecks.csproj --no-restore`.

## Task 8: Commit Foundation Increment

**Files:**
- Commit all created Core files.
- Commit `Core\ReeYin-V.Core\CoreModule.cs`.
- Commit `Core\ReeYin-V.Core\IOC\PrismProvider.cs`.
- Commit scratch check project only if it is kept.

- [ ] **Step 1: Inspect changes**

Run:

```powershell
git diff -- Core\ReeYin-V.Core docs\superpowers Scratch\AlarmCoreChecks
```

Expected: diff contains only hardware alarm foundation, design/plan docs, and optional check project.

- [ ] **Step 2: Commit**

Run:

```powershell
git add Core\ReeYin-V.Core docs\superpowers Scratch\AlarmCoreChecks
git commit -m "feat: add hardware alarm core foundation"
```

Expected: commit succeeds. If the workspace is not a git repository, skip the commit and record the changed files in the final response.

## Self-Review

- Spec coverage: This plan covers the Core foundation, hardware reporter, automatic status monitor, built-in rules, and registration. AlarmCenter rule UI and pilot hardware edits are intentionally separate increments because they can be tested independently after the foundation exists.
- Placeholder scan: No placeholder tasks remain; every code step includes concrete files, commands, and expected results.
- Type consistency: `HardwareAlarmRequest`, `AlarmDefinitionInfo`, `AlarmDefinitionResolver`, `IAlarmDefinitionService`, `HardwareAlarmReporter`, and `HardwareAlarmStatePolicy` names are consistent across tasks.
