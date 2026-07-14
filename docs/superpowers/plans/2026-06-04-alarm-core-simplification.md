# Alarm Core Simplification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Simplify the alarm Core data model so software and hardware alarms share a common report request while hardware-specific rules remain configurable and compatible.

**Architecture:** Add `AlarmReportRequest` as the common external report model, keep `AlarmRaiseRequest` as the internal service request, and retain `HardwareAlarmRequest` as a compatibility wrapper. Keep existing hardware rule persistence in place, but rename confusing default-definition responsibilities and UI wording to reduce hardware-specific leakage.

**Tech Stack:** C# WPF, Prism, SqlSugar, ReeYin_V.Core alarm services, ReeYin.AlarmCenter MVVM pages.

---

### Task 1: Add Common Alarm Report Request

**Files:**
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmReportRequest.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Hardware/HardwareAlarmRequest.cs`

- [ ] **Step 1: Add the common request model**

Create `AlarmReportRequest` with the same report-facing fields currently carried by `HardwareAlarmRequest`:

```csharp
using System;
using System.Collections.Generic;

namespace ReeYin_V.Core.Services.Alarm.Models
{
    public class AlarmReportRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public AlarmSeverity? Severity { get; set; }
        public bool? NeedAcknowledge { get; set; }
        public bool? AllowManualClear { get; set; }
        public string? SuggestedAction { get; set; }
        public AlarmPopupMode? PopupMode { get; set; }
        public int? PopupThrottleSeconds { get; set; }
        public IDictionary<string, object?> ExtraData { get; set; } = new Dictionary<string, object?>();
    }
}
```

- [ ] **Step 2: Convert `HardwareAlarmRequest` to compatibility wrapper**

Change `HardwareAlarmRequest` to derive from the common model:

```csharp
using System;
using ReeYin_V.Core.Services.Alarm.Models;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    [Obsolete("Use AlarmReportRequest for software and hardware alarm reporting.")]
    public sealed class HardwareAlarmRequest : AlarmReportRequest
    {
    }
}
```

- [ ] **Step 3: Run static check**

Run:

```powershell
rg -n "class AlarmReportRequest|class HardwareAlarmRequest" Core\ReeYin-V.Core\Services\Alarm
```

Expected: one common model and one compatibility wrapper.

### Task 2: Switch Definition Resolution to Common Request

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/IAlarmDefinitionService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionResolver.cs`

- [ ] **Step 1: Change public definition service signature**

Replace `BuildRaiseRequest(HardwareAlarmRequest request)` with:

```csharp
AlarmRaiseRequest BuildRaiseRequest(AlarmReportRequest request);
```

and import `ReeYin_V.Core.Services.Alarm.Models`.

- [ ] **Step 2: Change resolver signatures**

Replace resolver parameters from `HardwareAlarmRequest` to `AlarmReportRequest`:

```csharp
public static bool TryBuildRaiseRequest(AlarmReportRequest request, AlarmDefinitionInfo? definition, out AlarmRaiseRequest raiseRequest)
public static AlarmRaiseRequest BuildRaiseRequest(AlarmReportRequest request, AlarmDefinitionInfo? definition)
```

- [ ] **Step 3: Change service implementation**

Normalize null requests using the new model:

```csharp
request ??= new AlarmReportRequest();
```

and call:

```csharp
AlarmDefinitionResolver.TryBuildRaiseRequest(request, definition, out AlarmRaiseRequest raiseRequest)
```

- [ ] **Step 4: Run static check**

Run:

```powershell
rg -n "BuildRaiseRequest\(HardwareAlarmRequest|TryBuildRaiseRequest\(HardwareAlarmRequest" Core\ReeYin-V.Core\Services\Alarm
```

Expected: no matches.

### Task 3: Remove Software Reporter Dependency on Hardware Request

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Software/SoftwareAlarmReporter.cs`

- [ ] **Step 1: Replace request type**

Change all local helper signatures and variables from `HardwareAlarmRequest` to `AlarmReportRequest`.

- [ ] **Step 2: Remove hardware namespace import**

Remove:

```csharp
using ReeYin_V.Core.Services.Alarm.Hardware;
```

and keep:

```csharp
using ReeYin_V.Core.Services.Alarm.Models;
```

- [ ] **Step 3: Run static check**

Run:

```powershell
rg -n "HardwareAlarmRequest|Services\.Alarm\.Hardware" Core\ReeYin-V.Core\Services\Alarm\Software
```

Expected: no matches.

### Task 4: Keep Hardware Reporter Compatible While Using Common Request Internally

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Hardware/IHardwareAlarmReporter.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Hardware/HardwareAlarmReporter.cs`

- [ ] **Step 1: Add common overload to the interface**

Expose the common request overload:

```csharp
AlarmInfo Report(AlarmReportRequest request);
```

Keep the old overload:

```csharp
AlarmInfo Report(HardwareAlarmRequest request);
```

The old overload remains for compatibility.

- [ ] **Step 2: Change reporter internals**

Use `AlarmReportRequest` for `CreateRequest`, `NormalizeRequest`, and the main `Report` implementation. Implement the old overload as:

```csharp
public AlarmInfo Report(HardwareAlarmRequest request)
{
    return Report(request as AlarmReportRequest);
}
```

- [ ] **Step 3: Run static check**

Run:

```powershell
rg -n "private static HardwareAlarmRequest|new HardwareAlarmRequest" Core\ReeYin-V.Core\Services\Alarm\Hardware\HardwareAlarmReporter.cs
```

Expected: no matches for private helper usage; the compatibility overload may still reference the old type.

### Task 5: Rename Default Definition Responsibility

**Files:**
- Create: `Core/ReeYin-V.Core/Services/Alarm/Definitions/DefaultAlarmDefinitions.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Hardware/HardwareAlarmRuleDefaults.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionService.cs`

- [ ] **Step 1: Move default definition factory**

Create `DefaultAlarmDefinitions.CreateDefaults()` containing the current default `AlarmDefinitionInfo` factory logic from `Hardware/HardwareAlarmRuleDefaults.cs`.

- [ ] **Step 2: Keep old class as wrapper**

Replace the old hardware default class body with:

```csharp
using System;
using System.Collections.Generic;
using ReeYin_V.Core.Services.Alarm.Definitions;

namespace ReeYin_V.Core.Services.Alarm.Hardware
{
    [Obsolete("Use DefaultAlarmDefinitions for default alarm definitions.")]
    public static class HardwareAlarmRuleDefaults
    {
        public static IReadOnlyList<AlarmDefinitionInfo> CreateDefaults()
        {
            return DefaultAlarmDefinitions.CreateDefaults();
        }
    }
}
```

- [ ] **Step 3: Update definition seeding**

Change `AlarmDefinitionService` seeding from:

```csharp
HardwareAlarmRuleDefaults.CreateDefaults()
```

to:

```csharp
DefaultAlarmDefinitions.CreateDefaults()
```

- [ ] **Step 4: Run static check**

Run:

```powershell
rg -n "HardwareAlarmRuleDefaults.CreateDefaults" Core\ReeYin-V.Core\Services\Alarm\Definitions
```

Expected: no matches inside `Definitions`.

### Task 6: Adjust AlarmCenter Wording

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/Views/AlarmDefinitionsView.xaml`
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmDefinitionsViewModel.cs`

- [ ] **Step 1: Change tab/page wording**

Replace user-facing “硬件规则” text with “自定义触发规则”.

- [ ] **Step 2: Change status wording**

Change status text from:

```csharp
$"Loaded {Definitions.Count} definitions, {HardwareRules.Count} hardware rules, ..."
```

to wording that describes trigger rules rather than hardware-only rules.

- [ ] **Step 3: Run static check**

Run:

```powershell
rg -n "硬件规则|hardware rules" Application\ReeYin.AlarmCenter
```

Expected: no user-facing matches except internal property names such as `HardwareRules`.

### Task 7: Build and Verify

**Files:**
- No source modifications expected.

- [ ] **Step 1: Run focused static checks**

Run:

```powershell
rg -n "HardwareAlarmRequest" Core\ReeYin-V.Core\Services\Alarm\Software Core\ReeYin-V.Core\Services\Alarm\Definitions
```

Expected: no matches.

- [ ] **Step 2: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore -m:1 -p:UseSharedCompilation=false -nr:false
```

Expected: exit code 0. Existing `halcondotnet` warning can remain.

