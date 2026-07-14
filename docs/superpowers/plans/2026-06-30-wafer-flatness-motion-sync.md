# Wafer Flatness Motion Sync Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Incrementally sync the motion-control behavior from the reference `Custom.WaferFlatnessMeasure` into the active project without removing the active point-temperature acquisition logic.

**Architecture:** Keep the active project layout (`Acquisition`, `Acquisition/Storage`, `Models`, `Trajectory`, `Views`, `ViewModels`) and merge only the reference motion-control additions. ACS LCI configuration is a serializable model bound by the config UI; motion execution branches to ACS LCI only when enabled and otherwise keeps the current trajectory/temperature logic.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM/EventAggregator, ReeYin control-card abstractions, XAML shared styles.

---

## File Structure

- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/AcsLciFixedDistancePulseConfig.cs` for persisted ACS LCI fixed-distance pulse settings.
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/LineSegmentStartPositionInfo.cs` for publishing actual segment-start coordinates.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/SensorMotionControlModel.cs` to add ACS LCI branch, helper methods, and line-start publishing while preserving point-temperature capture methods.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessConfigViewModel.cs` to expose ACS/Googol parameter-section visibility.
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml` to restore ACS LCI settings UI and vendor-specific visibility.

### Task 1: Add ACS LCI Configuration Model

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/AcsLciFixedDistancePulseConfig.cs`

- [ ] **Step 1: Add serializable config class**

```csharp
using Prism.Mvvm;
using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public class AcsLciFixedDistancePulseConfig : BindableBase
    {
        private bool _isEnabled;
        public bool IsEnabled { get => _isEnabled; set => SetProperty(ref _isEnabled, value); }

        private int _bufferNo = 10;
        public int BufferNo { get => _bufferNo; set => SetProperty(ref _bufferNo, Math.Clamp(value, 0, 64)); }

        private int _axisX;
        public int AxisX { get => _axisX; set => SetProperty(ref _axisX, value); }

        private int _axisY = 1;
        public int AxisY { get => _axisY; set => SetProperty(ref _axisY, value); }

        private double _pulseWidth = 0.01d;
        public double PulseWidth { get => _pulseWidth; set => SetProperty(ref _pulseWidth, Math.Max(double.Epsilon, value)); }

        private bool _useSensorInterval = true;
        public bool UseSensorInterval { get => _useSensorInterval; set => SetProperty(ref _useSensorInterval, value); }

        private double _interval = 1d;
        public double Interval { get => _interval; set => SetProperty(ref _interval, Math.Max(double.Epsilon, value)); }

        private double _startDistance;
        public double StartDistance { get => _startDistance; set => SetProperty(ref _startDistance, Math.Max(0d, value)); }

        private double _endDistance;
        public double EndDistance { get => _endDistance; set => SetProperty(ref _endDistance, Math.Max(0d, value)); }

        private bool _routeConfigOutput = true;
        public bool RouteConfigOutput { get => _routeConfigOutput; set => SetProperty(ref _routeConfigOutput, value); }

        private int _configOutputIndex;
        public int ConfigOutputIndex { get => _configOutputIndex; set => SetProperty(ref _configOutputIndex, value); }

        private int _configOutputCode = 7;
        public int ConfigOutputCode { get => _configOutputCode; set => SetProperty(ref _configOutputCode, value); }

        private int _timeout = 60000;
        public int Timeout { get => _timeout; set => SetProperty(ref _timeout, Math.Max(1000, value)); }
    }
}
```

### Task 2: Add Line Segment Start Event Model

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Trajectory/LineSegmentStartPositionInfo.cs`

- [ ] **Step 1: Add event payload class**

```csharp
using System;

namespace Custom.WaferFlatnessMeasure
{
    [Serializable]
    public sealed class LineSegmentStartPositionInfo
    {
        public const string EventName = "LineSegmentStartPosition";
        public int SegmentIndex { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public LineSegmentStartPositionInfo Clone() => new LineSegmentStartPositionInfo
        {
            SegmentIndex = SegmentIndex,
            StartX = StartX,
            StartY = StartY
        };
    }
}
```

### Task 3: Merge Motion Execution Behavior

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/SensorMotionControlModel.cs`

- [ ] **Step 1: Add missing usings**

```csharp
using System.Collections;
using System.Globalization;
using System.Reflection;
using LineSegment = ReeYin_V.Core.MovingRelated.LineSegment;
```

- [ ] **Step 2: Add ACS config and line-start fields**

```csharp
[JsonIgnore]
private readonly List<LineSegmentStartPositionInfo> _lineSegmentStartPositions = new List<LineSegmentStartPositionInfo>();

private AcsLciFixedDistancePulseConfig _acsLciPulseParam = new AcsLciFixedDistancePulseConfig();
public AcsLciFixedDistancePulseConfig AcsLciPulseParam
{
    get => _acsLciPulseParam;
    set
    {
        _acsLciPulseParam = value ?? new AcsLciFixedDistancePulseConfig();
        RaisePropertyChanged();
    }
}
```

- [ ] **Step 3: In `ExecuteMoving`, clear/record line-starts and branch to ACS LCI**

Replace direct start/stop publishes with `PublishStartCollectEvent` and `PublishStopCollectEvent`, call `RecordLineSegmentStartPosition` after moving to each segment start, and when `AcsLciPulseParam.IsEnabled` is true call `ExecuteAcsLciFixedDistancePulseSegment(...)` then continue to the next segment.

- [ ] **Step 4: Add helper methods from reference**

Add methods: `RecordLineSegmentStartPosition`, `TryReadCurrentAxisPosition`, `TryGetAxisPositionFromControlCard`, `PublishStartCollectEvent`, `PublishStopCollectEvent`, `FormatCollectModeName`, `ExecuteAcsLciFixedDistancePulseSegment`, `TryRunLciFixedDistancePulseXseg`, `BuildAcsLciFixedDistancePulseParam`, `TryGetAcsLciFixedDistancePulseMethod`, `SetAcsLciProperty`, `SetAcsLciPoints`, `CreateAcsPoint2D`, `GetLineSegmentLength`, and `PublishLineCollectPoints`.

### Task 4: Restore Vendor-Specific Config Visibility

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/WaferFlatnessConfigViewModel.cs`

- [ ] **Step 1: Add visibility properties and refresh call**

```csharp
private bool _isGoogolControlCardVisible = true;
public bool IsGoogolControlCardVisible
{
    get => _isGoogolControlCardVisible;
    private set => SetProperty(ref _isGoogolControlCardVisible, value);
}

private bool _isAcsControlCardVisible;
public bool IsAcsControlCardVisible
{
    get => _isAcsControlCardVisible;
    private set => SetProperty(ref _isAcsControlCardVisible, value);
}
```

Call `RefreshControlCardParameterVisibility();` in `InitParam()` after `ModelParam.LoadKeyParam();`.

- [ ] **Step 2: Add control-card detection helpers**

Add `RefreshControlCardParameterVisibility`, `ResolveCurrentControlCard`, `IsGoogolControlCard`, `IsAcsControlCard`, and `GetStringProperty` from the reference ViewModel.

### Task 5: Restore ACS LCI Config UI

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/WaferFlatnessConfigView.xaml`

- [ ] **Step 1: Apply vendor-specific visibility**

Set the position comparison group visibility to `IsGoogolControlCardVisible`.

- [ ] **Step 2: Add ACS LCI settings group**

Insert the ACS LCI fixed-distance pulse `WxGroupBox` from the reference file after the position comparison group and bind all fields to `ModelParam.AcsLciPulseParam.*`.

### Task 6: Verify

**Files:**
- Build: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [ ] **Step 1: Build touched project**

Run: `dotnet build "E:\Company\工作目录\ReeYin-V\ReeYin-V\CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj" --no-restore`

Expected: Build succeeds or reports only pre-existing restore/package issues unrelated to the touched files.

- [ ] **Step 2: If build fails on compile errors in touched files**

Fix compile errors and rerun the same build command until the touched project compiles.

## Self-Review

- Spec coverage: ACS LCI config model, execution branch, UI visibility/config, and line-start event payload are covered.
- Placeholder scan: no TBD/TODO placeholders.
- Type consistency: file paths and class names match the active project namespace and reference classes.
