# Custom.WaferFlatnessMeasure Folder Reorganization Design

Date: 2026-07-02

## Scope

`Custom.WaferFlatnessMeasure` needs a clearer physical folder layout. Sensor acquisition pages, view models, models, storage, and processing should be grouped under one acquisition area, then separated by concrete acquisition type. Line-scan or line-spectrum acquisition and point-spectrum acquisition should be split. Motion-control files should move to their own area. Coordinate and trajectory files should be managed together.

This change reorganizes files only. It does not redesign module behavior.

## Goals

- Group sensor acquisition-related pages and processing under `Acquisition`.
- Split point-spectrum acquisition from line-scan or line-spectrum acquisition.
- Move motion-control-related pages, view models, models, and configuration into `MotionControl`.
- Move coordinate, trajectory generation, trajectory events, and trajectory monitor files into `Coordinates`.
- Keep result processing and display under `Processing`.
- Keep existing class names and namespaces stable to reduce serialized node and Prism registration risk.
- Verify the WPF project still builds after moving XAML and code-behind files.

## Non-Goals

- Do not rename public classes such as `SensorDataCollectionModel`, `LineSpectrumDataCollectionModel`, or `SensorMotionControlModel`.
- Do not change existing namespaces such as `Custom.WaferFlatnessMeasure.Views`, `Custom.WaferFlatnessMeasure.ViewModels`, or `Custom.WaferFlatnessMeasure.Models`.
- Do not change node menu names, node descriptions, output parameter names, recipe parameter names, or execution behavior.
- Do not refactor the large point-spectrum model internals in this pass.
- Do not change algorithm implementation or acquisition data formats.

## Recommended Approach

Use a function-first physical folder layout while preserving code-level compatibility. Files move into feature folders, but existing namespaces and type names remain unchanged.

This approach gives the project a clearer structure without creating unnecessary migration risk for old saved projects, dynamic page registration, XAML `x:Class`, or Prism auto-wiring.

## Target Folder Layout

```text
Custom.WaferFlatnessMeasure
+-- Acquisition
|   +-- PointSpectrum
|   |   +-- Views
|   |   +-- ViewModels
|   |   +-- Models
|   |   +-- Processing
|   |   +-- Calibration
|   |   +-- Storage
|   +-- LineSpectrum
|       +-- Views
|       +-- ViewModels
|       +-- Models
|       +-- Processing
+-- MotionControl
|   +-- Views
|   +-- ViewModels
|   +-- Models
|   +-- GripperClamp
|       +-- Views
|       +-- ViewModels
|       +-- Models
+-- Coordinates
|   +-- Views
|   +-- ViewModels
|   +-- Models
|   +-- Events
|   +-- Services
+-- Processing
|   +-- Views
|   +-- ViewModels
|   +-- Algorithms
+-- CustomWaferFlatnessMeasure.cs
```

## File Migration Map

### Point-Spectrum Acquisition

Move these files under `Acquisition/PointSpectrum`:

- `Views/SensorDataCollectionView.xaml` -> `Acquisition/PointSpectrum/Views/SensorDataCollectionView.xaml`
- `Views/SensorDataCollectionView.xaml.cs` -> `Acquisition/PointSpectrum/Views/SensorDataCollectionView.xaml.cs`
- `ViewModels/SensorDataCollectionViewModel.cs` -> `Acquisition/PointSpectrum/ViewModels/SensorDataCollectionViewModel.cs`
- `Acquisition/PointSpectrum/SensorDataCollectionModel.cs` -> `Acquisition/PointSpectrum/Models/SensorDataCollectionModel.cs`
- `Acquisition/PointSpectrum/Storage/PointTemperatureStorageService.cs` stays under `Acquisition/PointSpectrum/Storage`
- `Acquisition/PointSpectrum/Storage/SensorDataStorageService.cs` stays under `Acquisition/PointSpectrum/Storage`
- `Processing/RunALGO.cs` -> `Acquisition/PointSpectrum/Processing/RunALGO.cs`
- `Processing/RunAlgoPointCloudImageExport.cs` -> `Acquisition/PointSpectrum/Processing/RunAlgoPointCloudImageExport.cs`
- `Processing/SensorPointDataProcessor.cs` -> `Acquisition/PointSpectrum/Processing/SensorPointDataProcessor.cs`
- `Processing/PreprocessDatasetModel.cs` -> `Acquisition/PointSpectrum/Processing/PreprocessDatasetModel.cs`
- `Processing/PreprocessDatasetFilter.cs` -> `Acquisition/PointSpectrum/Processing/PreprocessDatasetFilter.cs`
- `Processing/DataAnalysisModels.cs` -> `Acquisition/PointSpectrum/Processing/DataAnalysisModels.cs`
- `Processing/Calibration/CalibModel.cs` -> `Acquisition/PointSpectrum/Calibration/CalibModel.cs`

### Line-Spectrum Acquisition

Move these files under `Acquisition/LineSpectrum`:

- `Views/LineSpectrumDataCollectionView.xaml` -> `Acquisition/LineSpectrum/Views/LineSpectrumDataCollectionView.xaml`
- `Views/LineSpectrumDataCollectionView.xaml.cs` -> `Acquisition/LineSpectrum/Views/LineSpectrumDataCollectionView.xaml.cs`
- `ViewModels/LineSpectrumDataCollectionViewModel.cs` -> `Acquisition/LineSpectrum/ViewModels/LineSpectrumDataCollectionViewModel.cs`
- `Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs` -> `Acquisition/LineSpectrum/Models/LineSpectrumDataCollectionModel.cs`
- `Acquisition/LineSpectrum/LineSpectrumTiffExportService.cs` -> `Acquisition/LineSpectrum/Processing/LineSpectrumTiffExportService.cs`

### Motion Control

Move these files under `MotionControl`:

- `Views/WaferFlatnessConfigView.xaml` -> `MotionControl/Views/WaferFlatnessConfigView.xaml`
- `Views/WaferFlatnessConfigView.xaml.cs` -> `MotionControl/Views/WaferFlatnessConfigView.xaml.cs`
- `ViewModels/WaferFlatnessConfigViewModel.cs` -> `MotionControl/ViewModels/WaferFlatnessConfigViewModel.cs`
- `Acquisition/SensorMotionControlModel.cs` -> `MotionControl/Models/SensorMotionControlModel.cs`
- `Models/AcsLciFixedDistancePulseConfig.cs` -> `MotionControl/Models/AcsLciFixedDistancePulseConfig.cs`
- `Views/GripperClampControlView.xaml` -> `MotionControl/GripperClamp/Views/GripperClampControlView.xaml`
- `Views/GripperClampControlView.xaml.cs` -> `MotionControl/GripperClamp/Views/GripperClampControlView.xaml.cs`
- `ViewModels/GripperClampControlViewModel.cs` -> `MotionControl/GripperClamp/ViewModels/GripperClampControlViewModel.cs`
- `Models/GripperClampControlModel.cs` -> `MotionControl/GripperClamp/Models/GripperClampControlModel.cs`

### Coordinates And Trajectory

Move these files under `Coordinates`:

- `Trajectory/CreateTrajectoryModel.cs` -> `Coordinates/Models/CreateTrajectoryModel.cs`
- `Trajectory/LocusInfo.cs` -> `Coordinates/Models/LocusInfo.cs`
- `Trajectory/CalibrationWaferPosition.cs` -> `Coordinates/Models/CalibrationWaferPosition.cs`
- `Trajectory/CustomRingDefinition.cs` -> `Coordinates/Models/CustomRingDefinition.cs`
- `Trajectory/PointCollectionStepInfo.cs` -> `Coordinates/Models/PointCollectionStepInfo.cs`
- `Trajectory/LineSegmentCsvSessionInfo.cs` -> `Coordinates/Models/LineSegmentCsvSessionInfo.cs`
- `Trajectory/LineSegmentStartPositionInfo.cs` -> `Coordinates/Models/LineSegmentStartPositionInfo.cs`
- `Trajectory/SensorTrajectoryDataPackage.cs` -> `Coordinates/Events/SensorTrajectoryDataPackage.cs`
- `Trajectory/WaferTrajectoryTrackingPayload.cs` -> `Coordinates/Events/WaferTrajectoryTrackingPayload.cs`
- `Trajectory/WaferTrajectoryMotionHelper.cs` -> `Coordinates/Services/WaferTrajectoryMotionHelper.cs`
- `Trajectory/WaferTrajectoryTrackingPublisher.cs` -> `Coordinates/Services/WaferTrajectoryTrackingPublisher.cs`
- `Trajectory/WaferTrajectoryMonitorDemoView.xaml` -> `Coordinates/Views/WaferTrajectoryMonitorDemoView.xaml`
- `Trajectory/WaferTrajectoryMonitorDemoView.xaml.cs` -> `Coordinates/Views/WaferTrajectoryMonitorDemoView.xaml.cs`
- `Trajectory/WaferTrajectoryMonitorDemoViewModel.cs` -> `Coordinates/ViewModels/WaferTrajectoryMonitorDemoViewModel.cs`

### Result Processing And Display

Move these files under `Processing`:

- `Views/ResultDisposeView.xaml` -> `Processing/Views/ResultDisposeView.xaml`
- `Views/ResultDisposeView.xaml.cs` -> `Processing/Views/ResultDisposeView.xaml.cs`
- `ViewModels/ResultDisposeViewModel.cs` -> `Processing/ViewModels/ResultDisposeViewModel.cs`
- `Views/WaferFlatnessResultChartView.xaml` -> `Processing/Views/WaferFlatnessResultChartView.xaml`
- `Views/WaferFlatnessResultChartView.xaml.cs` -> `Processing/Views/WaferFlatnessResultChartView.xaml.cs`
- `ViewModels/WaferFlatnessResultChartViewModel.cs` -> `Processing/ViewModels/WaferFlatnessResultChartViewModel.cs`

Existing algorithm files stay under `Processing/Algorithms`.

## Compatibility Rules

- Preserve namespaces during this pass.
- Preserve XAML `x:Class` values.
- Preserve Prism registrations in `CustomWaferFlatnessMeasure.cs`; only update `using` directives if the compiler requires them.
- Preserve `[Serializable]`, `[RecipeParam]`, `[OutputParam]`, and `[JsonIgnore]` usage.
- Preserve `ModelParamBase`, `DialogViewModelBase`, `IViewModuleParam`, `INavigationAware`, and `TriggerModuleRun` lifecycle behavior.
- Keep partial class files in the same namespace so `SensorDataCollectionModel` partials continue to combine.

## Implementation Notes

- SDK-style C# projects include source and XAML files by glob, so moving files should not require explicit `.csproj` item updates.
- `Custom.WaferFlatnessMeasure.csproj.user` contains old `Compile Update="Views\*.xaml.cs"` entries and should be updated or cleaned to match new paths.
- Build output folders (`bin`, `obj`, `.codex_obj`) should not be moved manually.
- Because the workspace `.git` directory is empty, local commit commands are not available from this folder. The spec and implementation can still be written and verified.

## Verification

Run:

```powershell
dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj
```

Expected result:

- WPF XAML compiles after path changes.
- `CustomWaferFlatnessMeasure.cs` registration compiles.
- Partial classes such as `SensorDataCollectionModel` still combine.
- No runtime behavior is intentionally changed.

If available, also run the existing console-style test project:

```powershell
dotnet run --project CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj
```

## Risks

- WPF generated files may retain stale paths until a clean build refreshes `obj`.
- Any hard-coded relative paths inside XAML or code could break after file movement; search should confirm whether such paths exist.
- Prism ViewModel auto-wiring should continue because namespaces and type names are unchanged, but moved files must keep their class declarations unchanged.
- Existing serialized projects rely on type names and namespaces, so changing namespaces is intentionally deferred.

## Self-Review

- Placeholder scan: no unresolved markers or incomplete sections remain.
- Scope check: the design is limited to folder structure and verification; behavioral refactoring is excluded.
- Compatibility check: namespace, class name, recipe/output parameter, and lifecycle compatibility are explicit.
- Ambiguity check: each current file has a target location, and line-spectrum, point-spectrum, motion-control, coordinates, and processing boundaries are distinct.
