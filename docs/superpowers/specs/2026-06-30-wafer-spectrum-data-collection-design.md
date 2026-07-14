# Wafer Spectrum Data Collection Design

Date: 2026-06-30

## Scope

`Custom.WaferFlatnessMeasure` currently keeps point-spectrum collection, point temperature CSV export, line-segment CSV export, and general sensor collection control in `Acquisition/SensorDataCollectionModel.cs`. This change separates point-spectrum responsibilities into a dedicated folder and adds a new line-spectrum collection node modeled after `Custom.MFDJC` sensor collection behavior.

## Goals

- Keep the existing point-spectrum node compatible with existing projects and serialized node parameters.
- Move point-spectrum-related `SensorDataCollectionModel` implementation into `Acquisition/PointSpectrum`.
- Add a separate line-spectrum model under `Acquisition/LineSpectrum`.
- Register a new line-spectrum node with its own View and ViewModel.
- Add focused tests for line-spectrum `MeasureData.AreaData` conversion.

## Non-Goals

- Rename the existing `SensorDataCollectionModel` class.
- Change existing point-spectrum node behavior, output parameter names, or menu registration.
- Add a line-spectrum algorithm beyond the reference behavior of collecting sensor rows and exposing height/gray data.
- Rework trajectory generation or motion-control behavior.

## Architecture

The existing point-spectrum node remains backed by `SensorDataCollectionModel`. Its source files will be reorganized without changing the class name or namespace. The main acquisition partial will be moved/split into `Acquisition/PointSpectrum`, keeping helper storage files near the point-spectrum code.

The new line-spectrum node uses a new `LineSpectrumDataCollectionModel : ModelParamBase`. It follows the ReeYin-V lifecycle pattern:

- `LoadKeyParam()` calls `base.LoadKeyParam()`.
- `OnceInit()` calls `base.OnceInit()`, loads configured sensor modules, subscribes event handlers, and sets `TriggerModuleRun`.
- `Dispose()` calls `base.Dispose()`.
- `ExecuteModule()` handles start/stop collection choices and refreshes output parameters.

The line-spectrum UI is intentionally minimal: sensor selection, trigger mode flags, start/stop event names, and latest output summary. It can be extended later without coupling to point-spectrum controls.

## Data Flow

### Point Spectrum

The existing data flow remains unchanged:

1. Motion publishes point and line-session events.
2. `SensorDataCollectionModel` starts/stops sensor collection.
3. Point mode builds `PreDatas`, saves point temperature CSV when enabled, and runs flatness data analysis.
4. Line-segment mode aligns collected data with positions and saves segment CSV files.

### Line Spectrum

The new line-spectrum node follows the `Custom.MFDJC` pattern:

1. Start command resolves the selected `SensorBase` and calls `StartCollect()`.
2. Stop command calls `StopCollect()` and `ReceiveSensorData()`.
3. `MeasureData.AreaData[0]` is converted to height rows, scaled by `HeightScale`.
4. `MeasureData.AreaData[1]` is converted to gray rows when present.
5. Latest height/gray row lists and row counts are exposed as output parameters.

Invalid rows are skipped. Missing gray rows do not block height output.

## Files

- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/SensorDataCollectionModel.cs`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Storage/*`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/LineSpectrum/LineSpectrumDataCollectionModel.cs`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/LineSpectrumDataCollectionViewModel.cs`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/LineSpectrumDataCollectionView.xaml.cs`
- `CustomizedDemand/Custom.WaferFlatnessMeasure/CustomWaferFlatnessMeasure.cs`
- `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

## Compatibility

The existing point-spectrum class name remains `SensorDataCollectionModel` and the namespace remains `Custom.WaferFlatnessMeasure.Models`, so old serialized node parameters continue to resolve. The old node title and existing View/ViewModel remain intact.

The new line-spectrum class has a distinct name and node registration, so it does not affect old node loading.

## Error Handling

- Missing selected sensors log a warning and return without throwing.
- Stop collection handles null or empty sensor data by publishing empty output lists and logging a warning.
- Output refresh uses `TryGetValue` against `OutputParamCollector.GetDataPointValues` to avoid missing-key failures.
- Line-spectrum conversion skips malformed `MeasureData` rows rather than failing the whole node.

## Testing

Add tests that prove:

- `AreaData[0]` becomes height rows and applies the configured scale.
- `AreaData[1]` becomes gray rows when present.
- Invalid rows are skipped.
- The test suite still passes after the point-spectrum file move.

Build verification will run `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj` and the existing test executable project.
