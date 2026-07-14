# Gripper Jog And Temperature Address Configuration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the gripper clamp page for four editable jog/pressure channels and expose editable point-temperature PLC addresses on the sensor collection page.

**Architecture:** Keep the existing ReeYin-V MVVM module names and serialization-compatible model shape. Extend `GripperClampChannelModel` with jog/pressure fields while mapping existing clamp/release command properties to forward/reverse jog addresses. Add a configurable temperature address collection to `SensorDataCollectionModel` and pass it through events so `SensorMotionControlModel` reads the selected PLC addresses.

**Tech Stack:** WPF XAML, Prism MVVM, ReeYin-V `ModelParamBase`, PLC `PLCBase` read/write APIs, console-style regression tests in `Custom.WaferFlatnessMeasure.Tests`.

---

### Task 1: Regression Tests

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Add tests that assert gripper defaults include M3600/M3601 through M3630/M3631 and D220/D222/D224/D226.
- [ ] Add tests that assert `GripperClampControlView.xaml` exposes forward/reverse jog controls and editable pressure addresses.
- [ ] Add tests that assert `SensorDataCollectionView.xaml` exposes a temperature PLC address table bound to `ModelParam.PointTemperatureAddresses`.
- [ ] Add tests that assert `SensorMotionControlModel.cs` no longer directly loops over `PointTemperatureStorageService.DefaultTemperatureAddresses` for live capture.
- [ ] Build and run the test DLL; expected failure before implementation.

### Task 2: Gripper Model And ViewModel

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/GripperClamp/Models/GripperClampControlModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/GripperClamp/ViewModels/GripperClampControlViewModel.cs`

- [ ] Add serializable gripper fields: `PositionName`, `ForwardJogAddress`, `ReverseJogAddress`, `PressureAddress`, `PressureValue`.
- [ ] Keep `ClampCommandAddress` and `ReleaseCommandAddress` as compatibility aliases for forward/reverse jog addresses.
- [ ] Initialize four default grippers with top/bottom/left/right labels and requested PLC addresses.
- [ ] Add `StartJogAsync(index, direction)`, `StopJogAsync(index)`, `RefreshPressureAsync(index)`, and `RefreshPressuresAsync()`.
- [ ] Add `GeneralCommand` cases for `StartForwardJog`, `StartReverseJog`, `StopJog`, and `RefreshPressures`.

### Task 3: Gripper XAML Redesign

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/GripperClamp/Views/GripperClampControlView.xaml`

- [ ] Replace the old operation monitor with a four-gripper 2x2 control layout.
- [ ] Each card shows name, position, pressure value, forward/reverse jog buttons, and addresses.
- [ ] Use WPF interaction triggers for `PreviewMouseLeftButtonDown`, `PreviewMouseLeftButtonUp`, `MouseLeave`, and `LostMouseCapture` to implement hold-to-jog behavior.
- [ ] Update address configuration grid with position, forward jog, reverse jog, and pressure columns.

### Task 4: Temperature Address Configuration

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Storage/PointTemperatureStorageService.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Models/SensorDataCollectionModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Views/SensorDataCollectionView.xaml`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/MotionControl/Models/SensorMotionControlModel.cs`

- [ ] Add a serializable `PointTemperatureAddressModel` with editable `Name` and `Address`.
- [ ] Add `SensorDataCollectionModel.PointTemperatureAddresses`, defaulting to the existing D160-D176 addresses.
- [ ] Publish the active address list before point motion starts.
- [ ] Subscribe in `SensorMotionControlModel` and use the configured addresses for live PLC reads.
- [ ] Add a `SensorDataCollectionView` tab with an editable address grid and reset button.

### Task 5: Verification

**Files:**
- Build/test only.

- [ ] Run `dotnet build "CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\Custom.WaferFlatnessMeasure.Tests.csproj" /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" /p:UseAppHost=false`.
- [ ] Run `dotnet "CustomizedDemand\Custom.WaferFlatnessMeasure.Tests\bin\Debug\net8.0-windows\Custom.WaferFlatnessMeasure.Tests.dll"`.
- [ ] Run `dotnet build "CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj" /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"`.
