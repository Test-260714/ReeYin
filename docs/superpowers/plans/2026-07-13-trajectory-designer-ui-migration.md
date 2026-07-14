# Trajectory Designer UI Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the generic Skia trajectory designer control and its generic shape/geometry contract into `ReeYin_V.UI`, while preserving the wafer-flatness designer, preview, saved plan fields, and executable trajectory results.

**Architecture:** `ReeYin_V.UI` owns only generic coordinate mapping, editable shapes, geometry, hit testing, and WPF/Skia interaction. `Custom.WaferFlatnessMeasure` retains `DesignedTrajectoryPlan`, `DesignedTrajectoryBuilder`, editor windows, motion execution, and all wafer-specific behavior; it consumes the UI control and generic types directly.

**Tech Stack:** .NET 8 WPF, Prism, SkiaSharp.Views.WPF 2.88.9, existing console regression project.

---

### Task 1: Establish the shared UI contract

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Models/EditableTrajectoryShape.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Models/TrajectoryPoint.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Models/TrajectoryShapeKind.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Models/TrajectoryInnerPattern.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Models/TrajectoryShapeCategory.cs`
- Modify: `Core/ReeYin_V.UI/ReeYin_V.UI.csproj`
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Write a regression check that requires the UI project to expose the generic shape contract and that no longer permits the wafer control namespace in consumer XAML.
- [ ] Run the test project and record that it fails because the public UI contract is absent, independently of the two pre-existing `LineSegmentSurfaceAnalysisSelection` errors.
- [ ] Move the generic shape contract without changing property names, defaults, enum integer values, or validation behavior; add `SkiaSharp.Views.WPF` 2.88.9 to the UI project.
- [ ] Confirm only the existing two test-project compilation errors remain and build `ReeYin_V.UI`.

### Task 2: Move generic geometry and the Skia control

**Files:**
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Services/TrajectoryCoordinateMapper.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Services/TrajectoryShapeGeometry.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Services/TrajectoryGeometryService.cs`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Views/SkiaTrajectoryDesignerControl.xaml`
- Create: `Core/ReeYin_V.UI/UserControls/TrajectoryDesigner/Views/SkiaTrajectoryDesignerControl.xaml.cs`
- Delete after consumer migration: the corresponding eleven generic files under `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates`

- [ ] Write a regression check for the UI control namespace and shared geometry service.
- [ ] Compile the check before implementation to establish the missing-control failure.
- [ ] Move coordinate mapping, geometry, and control source to the UI project, changing only namespaces and intra-project imports.
- [ ] Keep dependency properties, command/event behavior, default values, preview-only behavior, selection behavior, and coordinate scaling unchanged.

### Task 3: Rewire wafer-flatness consumers

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Models/DesignedTrajectoryPlan.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Services/DesignedTrajectoryBuilder.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/ViewModels/TrajectoryDesignerEditViewModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/ViewModels/TrajectoryDesignerTestViewModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/ViewModels/WaferFlatnessConfigViewModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Views/TrajectoryDesignerEditWindow.xaml`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates/Views/TrajectoryDesignerTestView.xaml`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/Views/WaferFlatnessConfigView.xaml`

- [ ] Add a direct project reference from the wafer module to `ReeYin_V.UI`.
- [ ] Replace imports and XAML namespaces with `ReeYin_V.UI.UserControls.TrajectoryDesigner` types.
- [ ] Keep `DesignedTrajectoryPlan` member names and enum values unchanged so existing JSON plan payloads retain their shape semantics.
- [ ] Keep all builder and motion logic in the wafer module; it consumes shared geometry but the UI project receives no wafer reference.

### Task 4: Verify consumers, compatibility, and deployment inputs

**Files:**
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Add checks for UI ownership, consumer XAML usage, builder access to shared geometry, and unchanged serialized member names.
- [ ] Build `Core/ReeYin_V.UI/ReeYin_V.UI.csproj` and `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj` with `SkipModuleCopy=true`.
- [ ] Build the regression project; report the two pre-existing unresolved `LineSegmentSurfaceAnalysisSelection` errors separately if they remain.
- [ ] Inspect the wafer module build output for `SkiaSharp.Views.WPF.dll`, `SkiaSharp.dll`, and the Windows native `libSkiaSharp.dll` without launching or operating any hardware.

### Risk And Rollback

- The public UI package gains a SkiaSharp dependency. Verify the module output contains the managed and native dependencies before delivery.
- The generic type namespace changes, but JSON property names and enum values remain stable. Do not enable type-name serialization or modify existing plan fields.
- If a consumer fails to load the XAML control, restore the previous control namespace and source locations as one atomic revert; no device or persisted production data changes are part of this migration.
