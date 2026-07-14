# Wafer Flatness Folder Reorganization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reorganize `Custom.WaferFlatnessMeasure` into feature-focused folders for acquisition, motion control, coordinates, and processing without changing runtime behavior.

**Architecture:** Move files into feature folders while preserving existing namespaces, class names, XAML `x:Class` values, Prism registrations, and model lifecycle contracts. The SDK-style WPF project will continue to include `.cs` and `.xaml` files by glob, so only stale `.csproj.user` path metadata needs an explicit content update.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, ReeYin-V `ModelParamBase`, SDK-style MSBuild globbing, PowerShell file operations.

---

## File Structure

- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Views`: point-spectrum acquisition page.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/ViewModels`: point-spectrum acquisition view model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Models`: point-spectrum acquisition model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Processing`: point-spectrum data analysis, preprocessing, and algorithm runner partials.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Calibration`: point-spectrum calibration partials.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Storage`: point temperature and sensor-data storage services.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/Views`: line-spectrum acquisition page.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/ViewModels`: line-spectrum acquisition view model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/Models`: line-spectrum acquisition model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/Processing`: line-spectrum TIFF export service.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl`: wafer flatness motion configuration and sensor motion model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/GripperClamp`: gripper clamp node page, view model, and model.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Coordinates`: coordinate models, trajectory events, trajectory services, and trajectory monitor page.
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing`: result display pages and flatness algorithms.

## Task 1: Preflight Inventory

**Files:**
- Read: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Read: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj.user`
- Read: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`

- [ ] **Step 1: Confirm the active project file exists**

Run:

```powershell
Test-Path -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj"
```

Expected output:

```text
True
```

- [ ] **Step 2: Confirm path-sensitive references before moving files**

Run:

```powershell
rg -n "Views\\|ViewModels\\|Trajectory\\|Acquisition\\SensorMotionControlModel|Processing\\Calibration|LineSpectrumDataCollectionView.xaml|SensorDataCollectionView.xaml|WaferFlatnessConfigView.xaml|ResultDisposeView.xaml|GripperClampControlView.xaml|WaferFlatnessResultChartView.xaml|WaferTrajectoryMonitorDemoView.xaml" "CustomizedDemand\Custom.WaferFlatnessMeasure" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!**/.codex_obj/**"
```

Expected output contains only `Custom.WaferFlatnessMeasure.csproj.user` path metadata plus code-behind XML doc comments that mention `.xaml` file names.

- [ ] **Step 3: Confirm local Git is unavailable in this workspace**

Run:

```powershell
git status --short
```

Expected output:

```text
fatal: not a git repository (or any of the parent directories): .git
```

Do not run commit steps in this workspace unless Git metadata is restored.

## Task 2: Move Point-Spectrum Acquisition Files

**Files:**
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/SensorDataCollectionView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/SensorDataCollectionView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/SensorDataCollectionViewModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/SensorDataCollectionModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/RunALGO.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/RunAlgoPointCloudImageExport.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/SensorPointDataProcessor.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/PreprocessDatasetModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/PreprocessDatasetFilter.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/DataAnalysisModels.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Processing/Calibration/CalibModel.cs`

- [ ] **Step 1: Create point-spectrum target folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\ViewModels","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Calibration"
```

Expected: all listed directories exist after the command.

- [ ] **Step 2: Move point-spectrum page and view model files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\SensorDataCollectionView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\SensorDataCollectionView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\SensorDataCollectionViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\ViewModels\SensorDataCollectionViewModel.cs"
```

Expected: the three source paths no longer exist and the three destination paths exist.

- [ ] **Step 3: Move point-spectrum model and processing files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\SensorDataCollectionModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Models\SensorDataCollectionModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\RunALGO.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\RunALGO.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\RunAlgoPointCloudImageExport.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\RunAlgoPointCloudImageExport.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\SensorPointDataProcessor.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\SensorPointDataProcessor.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\PreprocessDatasetModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\PreprocessDatasetModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\PreprocessDatasetFilter.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\PreprocessDatasetFilter.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\DataAnalysisModels.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Processing\DataAnalysisModels.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Calibration\CalibModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum\Calibration\CalibModel.cs"
```

Expected: `SensorDataCollectionModel` partial files remain in namespace `Custom.WaferFlatnessMeasure.Models` after the move.

- [ ] **Step 4: Verify point-spectrum namespaces stayed unchanged**

Run:

```powershell
rg -n "^namespace Custom\.WaferFlatnessMeasure\.(Models|ViewModels|Views)|public partial class SensorDataCollectionModel|public class SensorDataCollectionViewModel|public partial class SensorDataCollectionView" "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\PointSpectrum"
```

Expected: results show the moved point-spectrum files still use the original namespaces and class names.

## Task 3: Move Line-Spectrum Acquisition Files

**Files:**
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/LineSpectrumDataCollectionViewModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumTiffExportService.cs`

- [ ] **Step 1: Create line-spectrum target folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\ViewModels","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Models","CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Processing"
```

Expected: all listed directories exist after the command.

- [ ] **Step 2: Move line-spectrum files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\LineSpectrumDataCollectionView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Views\LineSpectrumDataCollectionView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\LineSpectrumDataCollectionView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Views\LineSpectrumDataCollectionView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\LineSpectrumDataCollectionViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\ViewModels\LineSpectrumDataCollectionViewModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\LineSpectrumDataCollectionModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Models\LineSpectrumDataCollectionModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\LineSpectrumTiffExportService.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum\Processing\LineSpectrumTiffExportService.cs"
```

Expected: all five destination paths exist.

- [ ] **Step 3: Verify line-spectrum namespaces stayed unchanged**

Run:

```powershell
rg -n "^namespace Custom\.WaferFlatnessMeasure\.(Models|ViewModels|Views)|public class LineSpectrumDataCollectionModel|public class LineSpectrumDataCollectionViewModel|public partial class LineSpectrumDataCollectionView" "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\LineSpectrum"
```

Expected: results show `Custom.WaferFlatnessMeasure.Models`, `Custom.WaferFlatnessMeasure.ViewModels`, and `Custom.WaferFlatnessMeasure.Views`.

## Task 4: Move Motion-Control And Gripper Files

**Files:**
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessConfigViewModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/SensorMotionControlModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/AcsLciFixedDistancePulseConfig.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/GripperClampControlView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/GripperClampControlView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/GripperClampControlViewModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/GripperClampControlModel.cs`

- [ ] **Step 1: Create motion-control target folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\ViewModels","CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models","CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\ViewModels","CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Models"
```

Expected: all listed directories exist after the command.

- [ ] **Step 2: Move wafer flatness motion configuration files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessConfigView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Views\WaferFlatnessConfigView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\WaferFlatnessConfigViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\ViewModels\WaferFlatnessConfigViewModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Acquisition\SensorMotionControlModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\SensorMotionControlModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Models\AcsLciFixedDistancePulseConfig.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\Models\AcsLciFixedDistancePulseConfig.cs"
```

Expected: all five destination paths exist.

- [ ] **Step 3: Move gripper clamp files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\GripperClampControlView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Views\GripperClampControlView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\GripperClampControlView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Views\GripperClampControlView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\GripperClampControlViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\ViewModels\GripperClampControlViewModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Models\GripperClampControlModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl\GripperClamp\Models\GripperClampControlModel.cs"
```

Expected: all four destination paths exist.

- [ ] **Step 4: Verify motion-control namespaces stayed unchanged**

Run:

```powershell
rg -n "^namespace Custom\.WaferFlatnessMeasure(\.Models|\.ViewModels|\.Views)?|public partial class SensorMotionControlModel|public class WaferFlatnessConfigViewModel|public partial class WaferFlatnessConfigView|public class GripperClampControlModel|public class GripperClampControlViewModel|public partial class GripperClampControlView" "CustomizedDemand\Custom.WaferFlatnessMeasure\MotionControl"
```

Expected: results show original namespaces and class names.

## Task 5: Move Coordinate And Trajectory Files

**Files:**
- Move: all non-build files currently under `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory`

- [ ] **Step 1: Create coordinate target folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models","CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Events","CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services","CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\ViewModels"
```

Expected: all listed directories exist after the command.

- [ ] **Step 2: Move coordinate model files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\CreateTrajectoryModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\CreateTrajectoryModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\LocusInfo.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\LocusInfo.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\CalibrationWaferPosition.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\CalibrationWaferPosition.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\CustomRingDefinition.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\CustomRingDefinition.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\PointCollectionStepInfo.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\PointCollectionStepInfo.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\LineSegmentCsvSessionInfo.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\LineSegmentCsvSessionInfo.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\LineSegmentStartPositionInfo.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Models\LineSegmentStartPositionInfo.cs"
```

Expected: all seven destination paths exist.

- [ ] **Step 3: Move coordinate event and service files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\SensorTrajectoryDataPackage.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Events\SensorTrajectoryDataPackage.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryTrackingPayload.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Events\WaferTrajectoryTrackingPayload.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryMotionHelper.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services\WaferTrajectoryMotionHelper.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryTrackingPublisher.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Services\WaferTrajectoryTrackingPublisher.cs"
```

Expected: all four destination paths exist.

- [ ] **Step 4: Move coordinate monitor view files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryMonitorDemoView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\WaferTrajectoryMonitorDemoView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryMonitorDemoView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\Views\WaferTrajectoryMonitorDemoView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Trajectory\WaferTrajectoryMonitorDemoViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates\ViewModels\WaferTrajectoryMonitorDemoViewModel.cs"
```

Expected: all three destination paths exist.

- [ ] **Step 5: Verify coordinate namespaces stayed unchanged**

Run:

```powershell
rg -n "^namespace Custom\.WaferFlatnessMeasure(\.ViewModels|\.Views)?|public class CreateTrajectoryModel|public class LocusInfo|public sealed class SensorTrajectoryDataPackage|public sealed class WaferTrajectoryMonitorDemoViewModel|public partial class WaferTrajectoryMonitorDemoView" "CustomizedDemand\Custom.WaferFlatnessMeasure\Coordinates"
```

Expected: results show original namespaces and class names.

## Task 6: Move Result Processing And Chart Files

**Files:**
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/ResultDisposeView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/ResultDisposeView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/ResultDisposeViewModel.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessResultChartView.xaml`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessResultChartView.xaml.cs`
- Move: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessResultChartViewModel.cs`

- [ ] **Step 1: Create result processing target folders**

Run:

```powershell
New-Item -ItemType Directory -Force -Path "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Views","CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\ViewModels"
```

Expected: both listed directories exist after the command.

- [ ] **Step 2: Move result processing files**

Run:

```powershell
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\ResultDisposeView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Views\ResultDisposeView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\ResultDisposeView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Views\ResultDisposeView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\ResultDisposeViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\ViewModels\ResultDisposeViewModel.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessResultChartView.xaml" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Views\WaferFlatnessResultChartView.xaml"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\Views\WaferFlatnessResultChartView.xaml.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\Views\WaferFlatnessResultChartView.xaml.cs"
Move-Item -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure\ViewModels\WaferFlatnessResultChartViewModel.cs" -Destination "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing\ViewModels\WaferFlatnessResultChartViewModel.cs"
```

Expected: all six destination paths exist.

- [ ] **Step 3: Verify result processing namespaces stayed unchanged**

Run:

```powershell
rg -n "^namespace Custom\.WaferFlatnessMeasure\.(ViewModels|Views)|public class ResultDisposeViewModel|public partial class ResultDisposeView|public sealed class WaferFlatnessResultChartViewModel|public partial class WaferFlatnessResultChartView" "CustomizedDemand\Custom.WaferFlatnessMeasure\Processing"
```

Expected: results show original namespaces and class names.

## Task 7: Update Visual Studio User Metadata

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj.user`

- [ ] **Step 1: Replace stale code-behind path metadata**

Replace the entire content of `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj.user` with:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup />
  <ItemGroup>
    <Compile Update="Processing\Views\ResultDisposeView.xaml.cs">
      <SubType>Code</SubType>
    </Compile>
    <Compile Update="Acquisition\PointSpectrum\Views\SensorDataCollectionView.xaml.cs">
      <SubType>Code</SubType>
    </Compile>
    <Compile Update="MotionControl\Views\WaferFlatnessConfigView.xaml.cs">
      <SubType>Code</SubType>
    </Compile>
  </ItemGroup>
</Project>
```

Expected: no `Compile Update="Views\...` entries remain in the file.

- [ ] **Step 2: Verify no stale `.csproj.user` paths remain**

Run:

```powershell
rg -n "Compile Update=\"Views\\" "CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj.user"
```

Expected: no output.

## Task 8: Verify Registration And Build

**Files:**
- Read: `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`
- Build: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`

- [ ] **Step 1: Verify module registrations still reference known type names**

Run:

```powershell
rg -n "RegisterForNavigation|RegisterDialogAndMenu|SensorDataCollectionView|LineSpectrumDataCollectionView|GripperClampControlView|WaferFlatnessConfigView|ResultDisposeView|WaferFlatnessResultChartView|WaferTrajectoryMonitorDemoView" "CustomizedDemand\Custom.WaferFlatnessMeasure\CustomWaferFlatnessMeasure.cs"
```

Expected: registrations for all existing views remain present.

- [ ] **Step 2: Verify no stale source paths remain outside build output**

Run:

```powershell
rg -n "Views\\|ViewModels\\|Trajectory\\|Acquisition\\SensorMotionControlModel|Processing\\Calibration" "CustomizedDemand\Custom.WaferFlatnessMeasure" --glob "!**/bin/**" --glob "!**/obj/**" --glob "!**/.codex_obj/**"
```

Expected: no stale file-path references remain except XML doc comments that mention `.xaml` file names.

- [ ] **Step 3: Build the touched project**

Run:

```powershell
dotnet build "CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj"
```

Expected: `Build succeeded.` appears in the output.

- [ ] **Step 4: Run the existing wafer flatness tests**

Run:

```powershell
dotnet run --project "CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj"
```

Expected: the test executable exits with code `0` and prints its normal success message.

- [ ] **Step 5: Capture final file structure**

Run:

```powershell
Get-ChildItem -LiteralPath "CustomizedDemand\Custom.WaferFlatnessMeasure" -Recurse -File | Where-Object { $_.FullName -notmatch "\\(bin|obj|\.codex_obj)\\" } | ForEach-Object { $_.FullName.Substring((Resolve-Path "CustomizedDemand\Custom.WaferFlatnessMeasure").Path.Length + 1) } | Sort-Object
```

Expected: the list shows acquisition files under `Acquisition\PointSpectrum` and `Acquisition\LineSpectrum`, motion files under `MotionControl`, coordinate files under `Coordinates`, and result display files under `Processing`.

## Self-Review

- Spec coverage: Tasks 2 and 3 cover acquisition grouping and point/line separation; Task 4 covers motion control; Task 5 covers coordinates; Task 6 covers result processing; Task 7 covers stale metadata; Task 8 covers verification.
- Placeholder scan: no unresolved implementation markers remain.
- Type consistency: all moved model, view, and view model type names match the existing source names; no namespace changes are required.
- Compatibility check: the plan preserves Prism registrations, `x:Class` values, partial class namespaces, `ModelParamBase` lifecycle code, and node serialization type identities.
