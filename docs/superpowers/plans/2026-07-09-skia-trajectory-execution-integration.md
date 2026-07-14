# Skia Trajectory Execution Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect the Skia trajectory designer to `SensorMotionControlModel` execution by storing a clear trajectory plan, converting shapes into executable `LocusInfo`, and replacing the old trajectory-generation UI with an edit-and-apply flow.

**Architecture:** Add serializable trajectory result models and a geometry conversion service under `Coordinates`. The designer edit window edits shape data, builds a `DesignedTrajectoryPlan`, and writes it back to `SensorMotionControlModel`, which keeps `AllLocusInfo` synchronized for existing motion execution paths.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, SkiaSharp designer control, existing ReeYin-V `ModelParamBase` and recipe serialization patterns.

---

### Task 1: Source Tests

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Add source-level tests for the new result classes, conversion service, model integration, config view button, and edit window.
- [ ] Run the tests and confirm they fail because the new files/properties do not exist yet.

### Task 2: Result Models And Conversion Service

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Models/DesignedTrajectoryPlan.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Services/DesignedTrajectoryBuilder.cs`

- [ ] Implement `DesignedTrajectoryPlan`, `DesignedTrajectoryShape`, `DesignedTrajectoryRunStep`, `DesignedTrajectoryExecutionMode`, and `DesignedTrajectoryRunStepKind`.
- [ ] Implement conversion between `EditableTrajectoryShape` and plan shapes.
- [ ] Implement geometry generation for point, line, circle, rectangle, arc, and internal point/line patterns.
- [ ] Implement conversion from plan run steps to `LocusInfo`.

### Task 3: Designer Edit Window

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/ViewModels/TrajectoryDesignerEditViewModel.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Views/TrajectoryDesignerEditWindow.xaml`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Views/TrajectoryDesignerEditWindow.xaml.cs`

- [ ] Add a full-plane editor window with `SkiaTrajectoryDesignerControl` as the main surface.
- [ ] Add a floating selected-shape parameter panel and a bottom shape table.
- [ ] Add a `返回` command that builds and exposes the edited `DesignedTrajectoryPlan`.

### Task 4: Motion Model And Config Page Integration

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/Models/SensorMotionControlModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/ViewModels/WaferFlatnessConfigViewModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/Views/WaferFlatnessConfigView.xaml`

- [ ] Add a recipe-managed `DesignedTrajectoryPlan` property and summary properties.
- [ ] Synchronize the designed plan into `AllLocusInfo` before execution and after editing.
- [ ] Add `EditDesignedTrajectoryCommand` to open the edit window and apply the returned plan.
- [ ] Replace the old trajectory-generation UI with a summary, edit button, and execution-step preview.

### Task 5: Verification

**Files:**
- Build project: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [ ] Run source tests again and confirm they pass.
- [ ] Build the module project with `dotnet build` using the existing no-dependency/no-restore command.
- [ ] Fix compile errors without broad refactors.
