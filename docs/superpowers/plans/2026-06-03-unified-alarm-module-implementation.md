# Unified Alarm Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a unified alarm module where software alarms and hardware alarms share the existing `AlarmService` lifecycle, and hardware alarms can be customized through persisted rules.

**Architecture:** Keep `IAlarmService` as the lifecycle engine. Add a software reporter, a hardware rule service, and a hardware rule engine in Core; extend AlarmCenter only for rule management UI.

**Tech Stack:** C#/.NET 8 WPF, Prism, SqlSugar, SQLite, Newtonsoft.Json, existing ReeYin-V Core IOC and AlarmCenter MVVM patterns.

---

## File Structure

- Create: `Core\ReeYin-V.Core\Services\Alarm\Software\ISoftwareAlarmReporter.cs` - software alarm entry contract.
- Create: `Core\ReeYin-V.Core\Services\Alarm\Software\SoftwareAlarmReporter.cs` - software alarm reporter implementation.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEnums.cs` - trigger, operator, and clear enums.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEntity.cs` - SqlSugar entity for `hardware_alarm_rule`.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleInfo.cs` - mutable rule DTO used by services and UI.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleQuery.cs` - filter model for rule lists.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleContext.cs` - normalized hardware status context.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleAction.cs` - rule evaluation output.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\IHardwareAlarmRuleService.cs` - rule persistence contract.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleService.cs` - rule persistence, defaults, and cache.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleDefaults.cs` - system hardware rules.
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEngine.cs` - rule matching, defense, and clear decision logic.
- Modify: `Core\ReeYin-V.Core\Services\HardwareModule\HardwareModel.cs` - extend `HardwareStatus` with optional rule context fields.
- Modify: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmMonitorService.cs` - delegate hardware status handling to the rule engine.
- Modify: `Core\ReeYin-V.Core\CoreModule.cs` - add `HardwareAlarmRuleEntity` to CodeFirst table initialization.
- Modify: `Core\ReeYin-V.Core\IOC\PrismProvider.cs` - expose `ISoftwareAlarmReporter` and `IHardwareAlarmRuleService`.
- Modify: `Application\ReeYin.AlarmCenter\Models\AlarmDefinitionManagementModels.cs` - add UI model for hardware rules.
- Modify: `Application\ReeYin.AlarmCenter\ViewModels\AlarmDefinitionsViewModel.cs` - load, edit, save, and toggle hardware rules.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml` - add a hardware rule tab/panel using existing command style.
- Create: `Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1` - local compile and source-shape check script.

## Scope Check

The spec includes three related subsystems: Core alarm entry/reporting, hardware custom rule engine, and AlarmCenter rule configuration. They are coupled through the same data model and must ship together for user-configurable hardware alarms, so this plan keeps them in one implementation sequence.

## Task 1: Add Core Rule Models

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEnums.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEntity.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleInfo.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleQuery.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleContext.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleAction.cs`

- [ ] **Step 1: Write the source-shape check**

Create `Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1` with this initial content:

```powershell
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$required = @(
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEnums.cs",
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEntity.cs",
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleInfo.cs",
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleQuery.cs",
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleContext.cs",
    "Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleAction.cs"
)
foreach ($path in $required) {
    $full = Join-Path $root $path
    if (-not (Test-Path $full)) {
        throw "Missing required file: $path"
    }
}
Write-Host "Unified alarm source-shape check passed."
```

- [ ] **Step 2: Run the check and verify it fails**

Run: `powershell -ExecutionPolicy Bypass -File Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1`

Expected: FAIL with `Missing required file`.

- [ ] **Step 3: Add the enums**

Create `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEnums.cs`:

```csharp
#nullable enable
namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public enum HardwareAlarmTriggerKind
    {
        State = 0,
        ErrorCode = 1,
        ExtraData = 2,
        Heartbeat = 3
    }

    public enum HardwareAlarmOperator
    {
        Equals = 0,
        NotEquals = 1,
        GreaterThan = 2,
        GreaterThanOrEqual = 3,
        LessThan = 4,
        LessThanOrEqual = 5,
        Contains = 6,
        BitHasFlag = 7
    }

    public enum HardwareAlarmClearKind
    {
        StateRecovery = 0,
        FieldRecovery = 1,
        ManualOnly = 2
    }
}
```

- [ ] **Step 4: Add the SqlSugar entity**

Create `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEntity.cs`:

```csharp
#nullable enable
using SqlSugar;
using System;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    [SugarTable("hardware_alarm_rule", TableDescription = "硬件报警触发和恢复规则")]
    public sealed class HardwareAlarmRuleEntity
    {
        [SugarColumn(IsPrimaryKey = true, Length = 64)]
        public string Id { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = false)]
        public string DefinitionCode { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = false)]
        public string Name { get; set; } = string.Empty;
        [SugarColumn(Length = 64, IsNullable = false)]
        public string SourceType { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = true)]
        public string SourcePattern { get; set; } = string.Empty;
        [SugarColumn(Length = 128, IsNullable = true)]
        public string LocationPattern { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string TriggerKind { get; set; } = HardwareAlarmTriggerKind.State.ToString();
        [SugarColumn(Length = 128, IsNullable = false)]
        public string TriggerField { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string Operator { get; set; } = HardwareAlarmOperator.Equals.ToString();
        [SugarColumn(Length = 256, IsNullable = true)]
        public string TriggerValue { get; set; } = string.Empty;
        [SugarColumn(Length = 32, IsNullable = false)]
        public string ClearKind { get; set; } = HardwareAlarmClearKind.StateRecovery.ToString();
        [SugarColumn(Length = 256, IsNullable = true)]
        public string ClearValue { get; set; } = string.Empty;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool LatchMode { get; set; }
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public int Priority { get; set; } = 100;
        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true)]
        public string ExtraTemplateJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 5: Add DTOs and action/context models**

Create `HardwareAlarmRuleInfo.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string DefinitionCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string SourcePattern { get; set; } = string.Empty;
        public string LocationPattern { get; set; } = string.Empty;
        public HardwareAlarmTriggerKind TriggerKind { get; set; } = HardwareAlarmTriggerKind.State;
        public string TriggerField { get; set; } = string.Empty;
        public HardwareAlarmOperator Operator { get; set; } = HardwareAlarmOperator.Equals;
        public string TriggerValue { get; set; } = string.Empty;
        public HardwareAlarmClearKind ClearKind { get; set; } = HardwareAlarmClearKind.StateRecovery;
        public string ClearValue { get; set; } = string.Empty;
        public int DebounceMilliseconds { get; set; }
        public int ThrottleSeconds { get; set; } = 1;
        public bool LatchMode { get; set; }
        public bool Enabled { get; set; } = true;
        public bool IsSystem { get; set; }
        public int Priority { get; set; } = 100;
        public IDictionary<string, object?> ExtraTemplate { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public HardwareAlarmRuleInfo CreateCopy()
        {
            return new HardwareAlarmRuleInfo
            {
                Id = Id,
                DefinitionCode = DefinitionCode,
                Name = Name,
                SourceType = SourceType,
                SourcePattern = SourcePattern,
                LocationPattern = LocationPattern,
                TriggerKind = TriggerKind,
                TriggerField = TriggerField,
                Operator = Operator,
                TriggerValue = TriggerValue,
                ClearKind = ClearKind,
                ClearValue = ClearValue,
                DebounceMilliseconds = DebounceMilliseconds,
                ThrottleSeconds = ThrottleSeconds,
                LatchMode = LatchMode,
                Enabled = Enabled,
                IsSystem = IsSystem,
                Priority = Priority,
                ExtraTemplate = new Dictionary<string, object?>(ExtraTemplate, StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }
}
```

Create `HardwareAlarmRuleQuery.cs`:

```csharp
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
```

Create `HardwareAlarmRuleContext.cs`:

```csharp
#nullable enable
using ReeYin_V.Core;
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleContext
    {
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public HardwareState Status { get; set; }
        public bool IsConnect { get; set; }
        public string Describe { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
```

Create `HardwareAlarmRuleAction.cs`:

```csharp
#nullable enable
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public sealed class HardwareAlarmRuleAction
    {
        public string DefinitionCode { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool ShouldRaise { get; set; }
        public bool ShouldClear { get; set; }
        public bool IsLatched { get; set; }
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        public DateTime OccurredAt { get; set; } = DateTime.Now;
    }
}
```

- [ ] **Step 6: Run the check and build Core**

Run: `powershell -ExecutionPolicy Bypass -File Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1`

Expected: PASS with `Unified alarm source-shape check passed.`

Run: `dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 2: Add Rule Service and Default Rules

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\IHardwareAlarmRuleService.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleService.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleDefaults.cs`
- Modify: `Core\ReeYin-V.Core\CoreModule.cs`
- Modify: `Core\ReeYin-V.Core\IOC\PrismProvider.cs`

- [ ] **Step 1: Extend the check script**

Append these required paths to `$required` in `Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1`:

```powershell
"Core\ReeYin-V.Core\Services\Alarm\HardwareRules\IHardwareAlarmRuleService.cs",
"Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleService.cs",
"Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleDefaults.cs"
```

- [ ] **Step 2: Add the service contract**

Create `IHardwareAlarmRuleService.cs`:

```csharp
#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ReeYin_V.Core.Services.Alarm.HardwareRules
{
    public interface IHardwareAlarmRuleService
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<HardwareAlarmRuleInfo>> GetRulesAsync(HardwareAlarmRuleQuery query, CancellationToken cancellationToken = default);
        Task<HardwareAlarmRuleInfo?> FindByIdAsync(string id, CancellationToken cancellationToken = default);
        Task SaveAsync(HardwareAlarmRuleInfo rule, string operatorName, CancellationToken cancellationToken = default);
        Task SetEnabledAsync(string id, bool enabled, string operatorName, CancellationToken cancellationToken = default);
        IReadOnlyList<HardwareAlarmRuleInfo> GetEnabledRulesSnapshot();
    }
}
```

- [ ] **Step 3: Add default system rules**

Create `HardwareAlarmRuleDefaults.cs` with rules for `HW.COMM.DISCONNECTED`, `HW.OPERATION_FAILED`, `HW.PLC.HEARTBEAT_TIMEOUT`, `HW.MOTION.LIMIT_TRIGGERED`, `HW.MOTION.SERVO_ALARM`, and `HW.SENSOR.NO_DATA`. Use `IsSystem = true`, `Enabled = true`, `Priority = 10` for safety rules and `Priority = 100` for general rules.

- [ ] **Step 4: Implement the service**

Create `HardwareAlarmRuleService.cs` using the same cache pattern as `AlarmDefinitionService`: singleton `[ExposedService]`, `SemaphoreSlim` initialization lock, `SeedDefaultsAsync()`, `MapToInfo`, `MapToEntity`, and `GetEnabledRulesSnapshot()`.

- [ ] **Step 5: Register the table**

Modify `Core\ReeYin-V.Core\CoreModule.cs`:

```csharp
using ReeYin_V.Core.Services.Alarm.HardwareRules;
```

Add this type to `InitTables(...)`:

```csharp
typeof(HardwareAlarmRuleEntity), // 硬件报警触发规则表
```

- [ ] **Step 6: Expose the service**

Modify `Core\ReeYin-V.Core\IOC\PrismProvider.cs` constructor to accept `IHardwareAlarmRuleService hardwareAlarmRuleService`, assign it to a new static property, and add:

```csharp
public static IHardwareAlarmRuleService HardwareAlarmRuleService { get; private set; }
```

- [ ] **Step 7: Verify**

Run: `powershell -ExecutionPolicy Bypass -File Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1`

Expected: PASS.

Run: `dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 3: Add Hardware Rule Engine and Monitor Integration

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\HardwareRules\HardwareAlarmRuleEngine.cs`
- Modify: `Core\ReeYin-V.Core\Services\HardwareModule\HardwareModel.cs`
- Modify: `Core\ReeYin-V.Core\Services\Alarm\Monitoring\HardwareAlarmMonitorService.cs`

- [ ] **Step 1: Extend `HardwareStatus`**

Modify `HardwareStatus` in `Core\ReeYin-V.Core\Services\HardwareModule\HardwareModel.cs` with optional fields:

```csharp
public string SourceType { get; set; } = string.Empty;
public string Location { get; set; } = string.Empty;
public string ErrorCode { get; set; } = string.Empty;
public string Operation { get; set; } = string.Empty;
public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
public DateTime Timestamp { get; set; } = DateTime.Now;
```

Add `using System.Collections.Generic;` if the file does not already have it.

- [ ] **Step 2: Add the rule engine**

Create `HardwareAlarmRuleEngine.cs` as `[ExposedService(Lifetime.Singleton, 7)]`. Constructor dependencies: `IHardwareAlarmRuleService`, `IAlarmDefinitionService`. Implement:

```csharp
public IReadOnlyList<HardwareAlarmRuleAction> Evaluate(HardwareAlarmRuleContext context)
```

The method must:

- Filter enabled rules by `SourceType`, `SourcePattern`, and `LocationPattern`.
- Compare `State`, `ErrorCode`, or `ExtraData` fields with the configured operator.
- Return a Raise action when trigger condition matches.
- Return a Clear action when `ClearKind` is `StateRecovery` or `FieldRecovery` and the configured recovery condition matches.
- Skip disabled definitions by relying on `IAlarmDefinitionService.BuildRaiseRequest(...)` in the caller path.

- [ ] **Step 3: Integrate monitor**

Modify `HardwareAlarmMonitorService` constructor to accept `HardwareAlarmRuleEngine engine`. In `OnHardwareStatusChanged`, normalize `HardwareStatus` into `HardwareAlarmRuleContext`, evaluate actions, call `_reporter.Report(...)` for Raise and `_reporter.Clear(...)` for Clear.

- [ ] **Step 4: Preserve existing fallback behavior**

If `Evaluate(...)` returns no actions, call existing `HardwareAlarmStatePolicy.Resolve(...)` so current NotConnected/Error behavior still works for old modules.

- [ ] **Step 5: Verify**

Run: `dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 4: Add Software Alarm Reporter

**Files:**
- Create: `Core\ReeYin-V.Core\Services\Alarm\Software\ISoftwareAlarmReporter.cs`
- Create: `Core\ReeYin-V.Core\Services\Alarm\Software\SoftwareAlarmReporter.cs`
- Modify: `Core\ReeYin-V.Core\IOC\PrismProvider.cs`

- [ ] **Step 1: Add interface**

Create `ISoftwareAlarmReporter.cs` using the interface from the spec.

- [ ] **Step 2: Add implementation**

Create `SoftwareAlarmReporter.cs` with `[ExposedService(Lifetime.Singleton, 6, typeof(ISoftwareAlarmReporter))]`. It should depend on `IAlarmService` and `IAlarmDefinitionService`, build a `HardwareAlarmRequest`-compatible request with `SourceType = "Software"`, and call `IAlarmService.AddAlarm(...)`.

- [ ] **Step 3: Add default software definitions**

Extend `HardwareAlarmRuleDefaults` only for hardware rules; add software definitions in `AlarmDefinitionService.SeedDefaultsAsync()` through a new private `CreateSoftwareDefaults()` method or a new `SoftwareAlarmRuleDefaults` class. Include `SW.MODULE.EXECUTE_FAILED`, `SW.RECIPE.INVALID_PARAM`, `SW.ALGORITHM.FAILED`, `SW.DATA.NO_RESULT`, and `SW.SYSTEM.UNHANDLED_EXCEPTION`.

- [ ] **Step 4: Expose on PrismProvider**

Modify `PrismProvider` constructor to accept `ISoftwareAlarmReporter softwareAlarmReporter` and add:

```csharp
public static ISoftwareAlarmReporter SoftwareAlarmReporter { get; private set; }
```

- [ ] **Step 5: Verify**

Run: `dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 5: Add AlarmCenter Hardware Rule Models and ViewModel Commands

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Models\AlarmDefinitionManagementModels.cs`
- Modify: `Application\ReeYin.AlarmCenter\ViewModels\AlarmDefinitionsViewModel.cs`

- [ ] **Step 1: Add UI model**

In `AlarmDefinitionManagementModels.cs`, add `AlarmHardwareRuleItem : BindableBase` mirroring `HardwareAlarmRuleInfo`. Include `FromInfo(...)`, `ToInfo()`, and display properties `StatusText`, `SystemText`, and `TriggerSummary`.

- [ ] **Step 2: Add service dependency**

Modify `AlarmDefinitionsViewModel` constructor to accept `IHardwareAlarmRuleService hardwareRuleService`. The default constructor should use `PrismProvider.HardwareAlarmRuleService`.

- [ ] **Step 3: Add collections and commands**

Add:

```csharp
public ObservableCollection<AlarmHardwareRuleItem> HardwareRules { get; }
public DelegateCommand NewHardwareRuleCommand { get; }
public DelegateCommand EditHardwareRuleCommand { get; }
public DelegateCommand SaveHardwareRuleCommand { get; }
public DelegateCommand CancelHardwareRuleEditCommand { get; }
public DelegateCommand ToggleHardwareRuleCommand { get; }
```

Add selected/editing properties and an `IsHardwareRuleEditorOpen` flag following the existing definition/suppression editor pattern.

- [ ] **Step 4: Load rules in RefreshAsync**

Call:

```csharp
IReadOnlyList<HardwareAlarmRuleInfo> hardwareRules = await _hardwareRuleService.GetRulesAsync(new HardwareAlarmRuleQuery
{
    Keyword = DefinitionKeyword.Trim(),
    IncludeSystem = true,
    MaxCount = 500
});
```

Replace `HardwareRules` on the UI thread.

- [ ] **Step 5: Save and toggle**

Implement save and toggle methods using:

```csharp
await _hardwareRuleService.SaveAsync(EditingHardwareRule.ToInfo(), ResolveCurrentUser());
await _hardwareRuleService.SetEnabledAsync(SelectedHardwareRule.Id, next, ResolveCurrentUser());
```

- [ ] **Step 6: Verify AlarmCenter build**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 6: Add AlarmCenter Hardware Rule UI

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml`

- [ ] **Step 1: Add hardware rule tab**

Add a tab or left navigation entry named `硬件规则`. Bind its table to `HardwareRules` and selected item to `SelectedHardwareRule`.

- [ ] **Step 2: Add columns**

Include columns for `Name`, `DefinitionCode`, `SourceType`, `SourcePattern`, `LocationPattern`, `TriggerSummary`, `Enabled`, `IsSystem`, and `UpdatedAt`.

- [ ] **Step 3: Add command bar**

Bind buttons to `NewHardwareRuleCommand`, `EditHardwareRuleCommand`, `ToggleHardwareRuleCommand`, and `RefreshCommand`. Use existing AlarmCenter button styles.

- [ ] **Step 4: Add editor panel**

Add fields for rule name, definition code, source type, source pattern, location pattern, trigger kind, trigger field, operator, trigger value, clear kind, clear value, debounce milliseconds, throttle seconds, latch mode, enabled, and priority.

- [ ] **Step 5: Verify navigation**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 7: Trial Hardware Integration

**Files:**
- Modify: `Hardware\PLC\ReeYin_V.Hardware.PLC\Models\PLCBase.cs`
- Modify: `Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ControlCardBase.cs`
- Modify: `Hardware\Sensor\ReeYin.Hardware.Sensor\Models\SensorBase.cs`
- Existing reference: `Hardware\Camera\ReeYin_V.Hardware.Camera.Basler\CameraBasler.cs`

- [ ] **Step 1: PLC context**

In `PLCBase.State`, publish `HardwareStatus` with `SourceType = HardwareAlarmSources.Plc`, `Location = "PLC"`, and `ExtraData["IsConnect"] = Config.IsConnected`.

- [ ] **Step 2: PLC heartbeat context**

When heartbeat read fails, publish `HardwareStatus` with `ExtraData["HeartbeatAlive"] = false`; when it succeeds, publish `ExtraData["HeartbeatAlive"] = true`.

- [ ] **Step 3: Motion context**

In `ControlCardBase.State`, publish `SourceType = HardwareAlarmSources.MotionCard` and `Location = string.IsNullOrWhiteSpace(NickName) ? "MotionCard" : NickName`.

- [ ] **Step 4: Motion safety report**

When `ValidateJogLimitCondition` or `ValidateLimitPosition` returns false for a safety condition, report `HW.MOTION.LIMIT_TRIGGERED` or `HW.MOTION.SAFETY_ERROR` through `PrismProvider.HardwareAlarmReporter` without changing the returned boolean behavior.

- [ ] **Step 5: Sensor context**

In `SensorBase.State`, publish `SourceType = HardwareAlarmSources.Sensor` and `Location = string.IsNullOrWhiteSpace(IP) ? NickName : IP`.

- [ ] **Step 6: Verify hardware project builds**

Run: `dotnet build Hardware\PLC\ReeYin_V.Hardware.PLC\ReeYin_V.Hardware.PLC.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

Run: `dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj --no-restore`

Expected: PASS or existing unrelated compile errors only.

## Task 8: Final Verification

**Files:**
- Verify: `Core\ReeYin-V.Core\ReeYin_V.Core.csproj`
- Verify: `Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj`
- Verify: `docs\superpowers\specs\2026-06-03-unified-alarm-module-design.md`

- [ ] **Step 1: Run source-shape check**

Run: `powershell -ExecutionPolicy Bypass -File Scratch\AlarmModuleChecks\Check-UnifiedAlarmDesign.ps1`

Expected: PASS.

- [ ] **Step 2: Build Core**

Run: `dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore`

Expected: PASS.

- [ ] **Step 3: Build AlarmCenter**

Run: `dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore`

Expected: PASS.

- [ ] **Step 4: Manual smoke test**

Open AlarmCenter, enter the alarm definition page, switch to `硬件规则`, create a disabled custom rule with code `CUSTOM.HW.TEST`, save it, enable it, disable it, and confirm the rule list refreshes without closing the page.

- [ ] **Step 5: Manual hardware event test**

Publish `HardwareStatusChangedEvent` with `Status = HardwareState.NotConnected`, `SourceType = "Hardware"`, `Name = "TestHardware"`, and `Location = "Hardware:TestHardware"`. Confirm a `HW.COMM.DISCONNECTED` active alarm appears. Publish a second event with `Status = HardwareState.Ready` and the same source/location. Confirm the active alarm clears and enters history.

## Self-Review

- Spec coverage: software reporter, hardware reporter, rule table, rule service, rule engine, monitor integration, AlarmCenter UI, and hardware trial integration each have a task.
- Placeholder scan: this plan contains concrete file paths, commands, and expected outcomes; it does not use placeholder implementation labels.
- Type consistency: service, entity, DTO, and UI model names match the design spec.
- Testing: the plan uses a source-shape check plus project builds because the repository does not currently expose a general Core unit test project.
