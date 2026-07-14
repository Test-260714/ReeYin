# Alarm P0 Safety and Consistency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enforce alarm manual-clear policy in Core, preserve alarm-definition fields across configuration editing, and make acknowledgement reset/idempotency behavior explicit and tested.

**Architecture:** Extend the existing alarm facade with structured operation results and explicit clear origins while preserving legacy wrappers. Carry acknowledgement policy and full definition metadata through all persisted/runtime/configuration models, then update Reporter and UI call sites to select the correct operation semantics.

**Tech Stack:** .NET 8, C#, WPF/Prism, SqlSugar, SQLite functional-test console runner.

---

## File Map

- Create `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmOperationModels.cs`: clear origins, operation statuses and result type.
- Modify `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmEnums.cs`: acknowledgement reset mode.
- Modify `Core/ReeYin-V.Core/Services/Alarm/IAlarmService.cs`: structured clear and acknowledge APIs plus compatible wrappers.
- Modify `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`: single policy-aware mutation path and idempotent acknowledgement.
- Modify alarm runtime/persistence/definition models and mappings: carry reset policy and complete definition metadata.
- Modify hardware/software Reporter implementations: call recovery-clear API explicitly.
- Modify `Application/ReeYin.AlarmCenter/Models/AlarmDefinitionManagementModels.cs`: preserve hidden advanced definition values.
- Modify `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`: consume structured manual-clear result.
- Modify `Scratch/AlarmCenterFunctionalTests/Program.cs`: regression tests and test registration.
- Create `docs/testing/alarm-p0-test-plan-2026-07-10.md`: executable manual/automated test specification.
- Create `docs/testing/alarm-p0-test-report-2026-07-10.md`: populated only after actual runs.

### Task 1: Establish RED tests for clear policy

**Files:**
- Test: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] Add tests that create active alarms with `AllowManualClear=false/true`, invoke the wished-for structured clear API with `Manual` and `Recovery`, and assert `ManualClearNotAllowed`, `Succeeded`, `NotFound`, active-state retention/removal and audit counts.
- [ ] Add `AlarmDefinitionEntity` to `CreateAlarmTestDatabase` so later definition migration tests use the same isolated SQLite database.
- [ ] Run `dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore`; expected result: compile failure because `AlarmClearOrigin`, `AlarmOperationResult` and the structured API do not exist.

### Task 2: Implement clear origins and structured results

**Files:**
- Create: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmOperationModels.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/IAlarmService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Hardware/HardwareAlarmReporter.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Software/SoftwareAlarmReporter.cs`

- [ ] Define `AlarmClearOrigin`, `AlarmOperationStatus` and immutable factory methods on `AlarmOperationResult`.
- [ ] Add `ClearByIdAsync(..., AlarmClearOrigin, ...)` and `ClearByKey(..., AlarmClearOrigin, ...)` to `IAlarmService`.
- [ ] Replace direct `ClearUnsafe` access with one locked policy-aware helper: reject manual clear when `AllowManualClear=false`; return `NotFound` without side effects; otherwise perform the existing state/event/persistence flow.
- [ ] Keep `ClearAsync` as the legacy manual wrapper and `ClearAlarm` as a compatibility wrapper; update both Reporter implementations to call `ClearByKey(..., Recovery, ...)` explicitly.
- [ ] Run the functional runner; expected: new clear-policy tests pass and existing tests remain green.

### Task 3: Establish RED tests for definition round trips

**Files:**
- Test: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] Add a definition containing every advanced field and a mutable `ExtraTemplate`, invoke `AlarmConfigService.ToModel` and `ToInfo` through reflection or existing internal access, then assert every value is preserved.
- [ ] Mutate the source dictionary after conversion and assert converted dictionaries do not change.
- [ ] Convert through `AlarmDefinitionItem.FromModel().ToModel()` after editing only `Name`; assert hidden fields and timestamps remain unchanged.
- [ ] Run the functional runner; expected result: compile/assertion failures because the public and AlarmCenter models omit advanced fields.

### Task 4: Implement lossless definition models and mappings

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmDefinition.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Config/AlarmConfigService.cs`
- Modify: `Application/ReeYin.AlarmCenter/Models/AlarmDefinitionManagementModels.cs`
- Modify: `Scratch/AlarmCenterFunctionalTests/Program.cs` fake cloning helper

- [ ] Add identity, source/location defaults, recovery/debounce/throttle policy, reset policy, extra template and timestamps to `AlarmDefinition`.
- [ ] Map all fields both ways in `AlarmConfigService`; create defensive dictionary copies and apply defaults only when values are genuinely absent for a new definition.
- [ ] Carry the same hidden fields through `AlarmDefinitionItem.NewCustom`, `FromModel` and `ToModel` without exposing new UI controls.
- [ ] Update functional-test fake cloning so tests exercise the complete contract.
- [ ] Run the functional runner; expected: round-trip tests and prior tests pass.

### Task 5: Establish RED tests for acknowledgement semantics

**Files:**
- Test: `Scratch/AlarmCenterFunctionalTests/Program.cs`

- [ ] Add tests for `Never`, `OnSeverityIncrease` (same severity and increased severity) and `OnEveryRepeat` after an initial acknowledgement.
- [ ] Subscribe to `DataChanged` and query audits to prove a second acknowledgement returns `AlreadyAcknowledged` without a new realtime event or audit.
- [ ] Run the functional runner; expected result: compile/assertion failures because reset mode and structured acknowledgement do not exist and old repeats always reset confirmation.

### Task 6: Implement reset policy persistence and idempotent acknowledgement

**Files:**
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmEnums.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmRaiseRequest.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Models/AlarmInfo.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/AlarmRecordEntity.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionInfo.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionEntity.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionResolver.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/AlarmDefinitionService.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/Definitions/DefaultAlarmDefinitions.cs`
- Modify: `Core/ReeYin-V.Core/Services/Alarm/AlarmService.cs`

- [ ] Define `AlarmAcknowledgeResetMode` with an explicit default of `OnSeverityIncrease` in new runtime/config models and persistence fields.
- [ ] Map the policy definition → request → active alarm → entity → restored active alarm.
- [ ] In repeated raise, compare the previous severity before overwriting and reset confirmation only when the selected policy says so.
- [ ] Add structured `AcknowledgeOperationAsync`; return `AlreadyAcknowledged` before creating realtime/audit side effects and keep old confirmation wrappers compatible.
- [ ] Run the functional runner; expected: all acknowledgement tests pass.

### Task 7: Integrate AlarmCenter result handling

**Files:**
- Modify: `Application/ReeYin.AlarmCenter/ViewModels/AlarmWorkbenchShellViewModel.cs`
- Modify: `Application/ReeYin.Status/ViewModels/AlarmCenterViewModel.cs` if its clear command can target non-manually-clearable alarms

- [ ] Change the workbench clear command to call the structured manual-clear API and set `StatusText` from rejection/success result.
- [ ] Preserve existing command enablement based on `AllowManualClear`; Core remains authoritative if UI state is stale.
- [ ] Build `Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj`; expected: exit 0.

### Task 8: Write and execute the formal test plan

**Files:**
- Create: `docs/testing/alarm-p0-test-plan-2026-07-10.md`
- Create: `docs/testing/alarm-p0-test-report-2026-07-10.md`

- [ ] Document test environment, assumptions, test data, automated cases AC-P0-001 through AC-P0-011, manual smoke checks, expected results and exact commands.
- [ ] Run `dotnet run --project Scratch/AlarmCenterFunctionalTests/AlarmCenterFunctionalTests.csproj --no-restore` and record exact totals/output.
- [ ] Run `dotnet build Core/ReeYin-V.Core/ReeYin_V.Core.csproj --no-restore` and record warnings/errors.
- [ ] Run `dotnet build Application/ReeYin.AlarmCenter/ReeYin.AlarmCenter.csproj --no-restore` and record warnings/errors.
- [ ] If the repository solution is identifiable, run the relevant solution build; otherwise record that limitation without marking it passed.
- [ ] Populate the report exclusively from actual command evidence, including failures, reruns and residual risks.

### Task 9: Final verification

**Files:**
- Review all modified files and both testing documents.

- [ ] Search for every clear call and verify each is classified `Manual`, `Recovery` or `System`.
- [ ] Search all mappings of `AlarmDefinition`, `AlarmDefinitionInfo`, `AlarmDefinitionEntity`, `AlarmInfo` and `AlarmRecordEntity` for reset policy and advanced-field coverage.
- [ ] Re-run the full functional runner and both project builds fresh.
- [ ] Compare the implementation line-by-line against the design acceptance criteria and update the report with the final evidence.

