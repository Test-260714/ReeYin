# ACS LCI Persistent Config UI Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist ACS LCI test parameters and split the LCI configuration page into clear straight-line and circle test sections.

**Architecture:** Add serializable LCI configuration objects to `AcsControlCardOptions`. Keep `AcsControlCardConfigViewModel` as the command surface, but redirect existing LCI properties to the persistent option objects. Refactor only the `LCI脉冲` XAML tab into an inner TabControl while keeping the shared status/script preview.

**Tech Stack:** C#/.NET 8 WPF, Prism `DelegateCommand`, Newtonsoft.Json-compatible serializable option objects, existing console-style ACS tests.

---

### Task 1: Persistence Tests

**Files:**
- Modify: `E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs`

- [ ] **Step 1: Add failing option defaults test**

Add a test that creates `new AcsControlCardOptions()` and asserts `LciFixedDistancePulse` and `LciSegmentCircle` are non-null with the expected default values: fixed pulse buffer 10, axes 0/1, width 0.01, interval 1, timeout 60000, points text containing `0,0`; circle buffer 10, axes 0/1, velocity 50, center 10/5, radius 5, gate state 1, timeout 60000.

- [ ] **Step 2: Add failing ViewModel wrapper test**

Extend the existing LCI ViewModel test to set `LciFixedDistancePulseBufferNo`, `LciFixedDistancePulsePointsText`, `LciSegmentCircleRadius`, and `LciSegmentCircleCenterX`, then assert the values are stored in `card.Options.LciFixedDistancePulse` and `card.Options.LciSegmentCircle`.

- [ ] **Step 3: Run red verification**

Run:

```powershell
dotnet build .\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-restore -p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\
```

Expected: FAIL because `AcsControlCardOptions.LciFixedDistancePulse` and `AcsControlCardOptions.LciSegmentCircle` do not exist.

### Task 2: Persistent Option Models

**Files:**
- Modify: `E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\App\AcsControlCardOptions.cs`

- [ ] **Step 1: Add serializable config classes**

Add `AcsLciFixedDistancePulseConfig` and `AcsLciSegmentCircleConfig` classes that inherit `BindableBase` and expose the fields described in the design.

- [ ] **Step 2: Add option properties**

Add `LciFixedDistancePulse` and `LciSegmentCircle` properties to `AcsControlCardOptions`, with null-protecting setters.

- [ ] **Step 3: Run green verification for option compilation**

Run the same `dotnet build` command. Expected: build continues past missing option members and reveals ViewModel wrapper test failures.

### Task 3: ViewModel Wrappers

**Files:**
- Modify: `E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ViewModels\AcsControlCardConfigViewModel.cs`

- [ ] **Step 1: Replace LCI backing-field usage**

Keep public property names unchanged, but make each LCI getter/setter read/write `Options?.LciFixedDistancePulse` or `Options?.LciSegmentCircle`, falling back to private default config objects only before a card is assigned.

- [ ] **Step 2: Notify LCI properties after card assignment**

After `Card = acsControlCard` in `SetCard`, raise property changed for the LCI fixed pulse and circle properties so the view refreshes from persisted options.

- [ ] **Step 3: Run ViewModel verification**

Run the ACS test project with `--no-build` after building. Expected: persistence tests pass until XAML layout assertions are added or fail for the known unrelated WaferFlatness assertion after the new tests pass.

### Task 4: XAML Layout Tests And Refactor

**Files:**
- Modify: `E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs`
- Modify: `E:\Company\工作目录\ReeYin-V\ReeYin-V\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml`

- [ ] **Step 1: Add failing XAML layout assertions**

Extend `TestAcsConfigViewLciFixedDistancePulseSurface` to assert the LCI tab contains inner headers `直线脉冲` and `分段画圆`, fixed pulse inputs bind to `Options.LciFixedDistancePulse.*`, and circle inputs bind to `Options.LciSegmentCircle.*`.

- [ ] **Step 2: Run red verification**

Run the test project. Expected: FAIL because the XAML still binds to ViewModel-only property names and does not have inner LCI sub-tabs.

- [ ] **Step 3: Refactor LCI XAML**

Replace the left-side LCI `ScrollViewer` content with an inner `TabControl` containing two `TabItem`s. Bind fixed-distance fields to `Options.LciFixedDistancePulse.*` and circle fields to `Options.LciSegmentCircle.*`. Keep run buttons bound to ViewModel commands.

- [ ] **Step 4: Run green verification**

Run the ACS test project and confirm all new tests pass before any unrelated pre-existing failure.

### Task 5: Final Build Verification

**Files:**
- No source edits expected.

- [ ] **Step 1: Build ACS project**

Run:

```powershell
dotnet build .\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj --no-restore -p:SolutionDir=E:\Company\工作目录\ReeYin-V\ReeYin-V\
```

Expected: exit 0; existing `halcondotnet` warning may remain.

- [ ] **Step 2: Run ACS tests**

Run:

```powershell
dotnet run --project .\Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj --no-build
```

Expected: new ACS LCI persistence/layout tests pass. The existing WaferFlatness `TrrigerStartCollect` failure may still stop the suite and must be reported as unrelated if unchanged.
