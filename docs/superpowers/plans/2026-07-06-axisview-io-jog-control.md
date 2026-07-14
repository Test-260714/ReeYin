# AxisView IO Jog Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configurable IO3/IO4/IO5/IO6 hold-to-move control for X/Y axes that is active only while `AxisView` is open.

**Architecture:** Persist the feature switch and four input ports in `ControlCardConfigModel`, expose them in `ControlCardConfigView`, and keep the IO-to-Jog state machine in a small `AxisViewIoJogController`. `AxisViewModel` owns the controller lifecycle and calls it from the existing view timer so closing the panel reliably stops IO-driven motion.

**Tech Stack:** C#/.NET 8 WPF, Prism `DelegateCommand`, HandyControl `NumericUpDown`, existing `ControlCardBase` and `IControlCard` motion/IO APIs.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Add structural and behavioral tests for config defaults, XAML bindings, controller mapping, multi-input stop, and `AxisViewModel` lifecycle integration.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardConfigModel.cs`
  - Persist the enable flag and IO port numbers under the existing control-card config model.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/ControlCardConfigView.xaml`
  - Add UI controls in the existing “其他” tab.
- Create `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisViewIoJogController.cs`
  - Encapsulate IO direction mapping and active Jog start/stop state.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`
  - Construct and call the controller, and stop it on close or unsafe states.

---

### Task 1: Add Failing Tests

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add test registrations**

In `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`, add these `Run(...)` calls immediately after the existing startup auto-reset view binding test registration:

```csharp
Run("ControlCardConfig model supports AxisView IO Jog settings", TestControlCardConfigModelSupportsAxisViewIoJogSettings);
Run("ControlCardConfig view binds AxisView IO Jog settings", TestControlCardConfigViewBindsAxisViewIoJogSettings);
Run("AxisView IO Jog controller maps input directions", TestAxisViewIoJogControllerMapsInputDirections);
Run("AxisView IO Jog controller stops on multi-direction input", TestAxisViewIoJogControllerStopsOnMultiDirectionInput);
Run("AxisView integrates IO Jog lifecycle", TestAxisViewIntegratesIoJogLifecycle);
```

- [ ] **Step 2: Add config, XAML, controller, and lifecycle tests**

Add these test methods after `TestControlCardConfigViewBindsStartupAutoReset()`:

```csharp
void TestControlCardConfigModelSupportsAxisViewIoJogSettings()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs");
    var model = new ControlCardConfigModel();

    AssertContains(source, "public bool IsAxisViewIoJogEnabled",
        "control-card config should persist the AxisView IO Jog enable switch");
    AssertContains(source, "public int AxisViewIoJogUpInputPort",
        "control-card config should persist the up input port");
    AssertContains(source, "public int AxisViewIoJogDownInputPort",
        "control-card config should persist the down input port");
    AssertContains(source, "public int AxisViewIoJogLeftInputPort",
        "control-card config should persist the left input port");
    AssertContains(source, "public int AxisViewIoJogRightInputPort",
        "control-card config should persist the right input port");
    AssertFalse(model.IsAxisViewIoJogEnabled, "AxisView IO Jog should default to disabled");
    AssertEqual(3, model.AxisViewIoJogUpInputPort, "default up input port");
    AssertEqual(4, model.AxisViewIoJogDownInputPort, "default down input port");
    AssertEqual(5, model.AxisViewIoJogLeftInputPort, "default left input port");
    AssertEqual(6, model.AxisViewIoJogRightInputPort, "default right input port");
}

void TestControlCardConfigViewBindsAxisViewIoJogSettings()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml");

    AssertContains(xaml, "AxisView IO方向控制",
        "Other tab should expose the AxisView IO Jog setting group");
    AssertContains(xaml, "IsChecked=\"{Binding ModelParam.IsAxisViewIoJogEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "AxisView IO Jog checkbox should bind to the persisted enable switch");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogUpInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "up input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogDownInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "down input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogLeftInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "left input port should bind to config");
    AssertContains(xaml, "Value=\"{Binding ModelParam.AxisViewIoJogRightInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"",
        "right input port should bind to config");
    AssertContains(xaml, "仅在运动轴操作面板打开时生效",
        "UI should explain the AxisView-only scope");
}

void TestAxisViewIoJogControllerMapsInputDirections()
{
    var card = CreateBaseTestCard();
    var config = new ControlCardConfigModel { IsAxisViewIoJogEnabled = true };
    var controller = new AxisViewIoJogController(card, config);

    controller.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(1, card.JogCommands.Count, "IO3 should start one Jog command");
    AssertEqual(En_AxisNum.Y, card.JogCommands[0].Axis, "IO3 should map to Y axis");
    AssertEqual(MoveDirection.正向, card.JogCommands[0].Direction, "IO3 should map to Y positive");
    AssertEqual(EN_SpeedType.High, card.JogCommands[0].SpeedType, "IO Jog should use the current AxisView speed type");
    AssertTrue(card.JogCommands[0].IsRunStop, "IO3 should start continuous Jog");

    controller.Update(CreateInputs(), EN_SpeedType.High, _ => true, (_, _) => true);
    controller.Update(CreateInputs(4), EN_SpeedType.Mid, _ => true, (_, _) => true);
    AssertEqual(3, card.JogCommands.Count, "IO4 should add stop/start commands after IO3 release");
    AssertEqual(En_AxisNum.Y, card.JogCommands[2].Axis, "IO4 should map to Y axis");
    AssertEqual(MoveDirection.反向, card.JogCommands[2].Direction, "IO4 should map to Y negative");
    AssertEqual(EN_SpeedType.Mid, card.JogCommands[2].SpeedType, "IO4 should use the refreshed speed type");
    AssertTrue(card.JogCommands[2].IsRunStop, "IO4 should start continuous Jog");

    controller.Update(CreateInputs(), EN_SpeedType.Mid, _ => true, (_, _) => true);
    controller.Update(CreateInputs(5), EN_SpeedType.Low, _ => true, (_, _) => true);
    AssertEqual(5, card.JogCommands.Count, "IO5 should add stop/start commands after IO4 release");
    AssertEqual(En_AxisNum.X, card.JogCommands[4].Axis, "IO5 should map to X axis");
    AssertEqual(MoveDirection.反向, card.JogCommands[4].Direction, "IO5 should map to X negative");

    controller.Update(CreateInputs(), EN_SpeedType.Low, _ => true, (_, _) => true);
    controller.Update(CreateInputs(6), EN_SpeedType.Work, _ => true, (_, _) => true);
    AssertEqual(7, card.JogCommands.Count, "IO6 should add stop/start commands after IO5 release");
    AssertEqual(En_AxisNum.X, card.JogCommands[6].Axis, "IO6 should map to X axis");
    AssertEqual(MoveDirection.正向, card.JogCommands[6].Direction, "IO6 should map to X positive");
}

void TestAxisViewIoJogControllerStopsOnMultiDirectionInput()
{
    var card = CreateBaseTestCard();
    var config = new ControlCardConfigModel { IsAxisViewIoJogEnabled = true };
    var controller = new AxisViewIoJogController(card, config);

    controller.Update(CreateInputs(3), EN_SpeedType.High, _ => true, (_, _) => true);
    controller.Update(CreateInputs(3, 6), EN_SpeedType.High, _ => true, (_, _) => true);

    AssertEqual(2, card.JogCommands.Count, "multi-direction input should stop the active Jog without starting another");
    AssertEqual(En_AxisNum.Y, card.JogCommands[1].Axis, "multi-direction stop should target the active axis");
    AssertEqual(MoveDirection.正向, card.JogCommands[1].Direction, "multi-direction stop should target the active direction");
    AssertFalse(card.JogCommands[1].IsRunStop, "multi-direction input should issue a Jog stop");

    controller.Update(CreateInputs(3, 6), EN_SpeedType.High, _ => true, (_, _) => true);
    AssertEqual(2, card.JogCommands.Count, "holding multiple inputs should not repeat stop commands");
}

void TestAxisViewIntegratesIoJogLifecycle()
{
    var source = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs");
    var closeBody = ReadSourceBetween(source, "case \"关闭\":", "default:");

    AssertContains(source, "private readonly ControlCardConfigModel _controlCardConfig",
        "AxisViewModel should keep the persisted control-card config for IO Jog settings");
    AssertContains(source, "private readonly AxisViewIoJogController _ioJogController",
        "AxisViewModel should own the IO Jog controller");
    AssertContains(source, "new AxisViewIoJogController(ControlCard, _controlCardConfig)",
        "AxisViewModel should create the IO Jog controller for the selected control card");
    AssertContains(source, "UpdateAxisViewIoJog()",
        "AxisView timer should process IO Jog each refresh");
    AssertContains(source, "ControlCard.GetAllInput(out var inputStatus)",
        "AxisViewModel should poll input IO through the common control-card API");
    AssertContains(source, "_ioJogController.Update(",
        "AxisViewModel should delegate IO direction state handling to the controller");
    AssertContains(closeBody, "_ioJogController.StopActiveJog();",
        "closing AxisView should stop any active IO Jog");
}
```

- [ ] **Step 3: Add test helper for input snapshots**

Add this helper function near `CreateBaseTestCard()`:

```csharp
bool[] CreateInputs(params int[] activePorts)
{
    var inputs = new bool[16];
    foreach (var port in activePorts)
    {
        inputs[port] = true;
    }

    return inputs;
}
```

- [ ] **Step 4: Extend the existing `TestControlCard` helper**

Inside the existing `sealed class TestControlCard : ControlCardBase`, add the `JogCommands` property after `RelativeMoveDistances`, and add the override before `DoGoHome(...)`:

```csharp
public List<(En_AxisNum Axis, MoveDirection Direction, EN_SpeedType SpeedType, bool IsRunStop)> JogCommands { get; } = new();
```

```csharp
public override bool JogAxis(En_AxisNum axisId, MoveDirection dir, EN_SpeedType spdType, bool isRunStop)
{
    JogCommands.Add((axisId, dir, spdType, isRunStop));
    return true;
}
```

- [ ] **Step 5: Run tests and verify they fail for missing implementation**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL. The failure should mention missing `AxisViewIoJogController` and missing `ControlCardConfigModel` IO Jog properties.

- [ ] **Step 6: Commit tests if git is usable**

Run:

```powershell
git status --short
```

Expected in a valid git workspace: status output lists the modified test file. If this workspace still reports `not a git repository`, record that in the task handoff and continue without a commit.

If git is usable, run:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs
git commit -m "test: cover AxisView IO jog control"
```

---

### Task 2: Add Persisted Config and UI Bindings

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/ControlCardConfigModel.cs`
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Views/ControlCardConfigView.xaml`

- [ ] **Step 1: Add persisted config properties**

In `ControlCardConfigModel.cs`, add these fields and properties after `StartupAutoResetTimeoutSeconds`:

```csharp
[JsonIgnore]
private bool _isAxisViewIoJogEnabled;
/// <summary>
/// AxisView 打开时是否允许输入 IO 控制 X/Y 方向连续点动。
/// </summary>
public bool IsAxisViewIoJogEnabled
{
    get { return _isAxisViewIoJogEnabled; }
    set { _isAxisViewIoJogEnabled = value; RaisePropertyChanged(); }
}

[JsonIgnore]
private int _axisViewIoJogUpInputPort = 3;
/// <summary>
/// AxisView IO 方向控制：向上输入端口，对应 Y 轴正向。
/// </summary>
public int AxisViewIoJogUpInputPort
{
    get { return _axisViewIoJogUpInputPort; }
    set { _axisViewIoJogUpInputPort = value; RaisePropertyChanged(); }
}

[JsonIgnore]
private int _axisViewIoJogDownInputPort = 4;
/// <summary>
/// AxisView IO 方向控制：向下输入端口，对应 Y 轴反向。
/// </summary>
public int AxisViewIoJogDownInputPort
{
    get { return _axisViewIoJogDownInputPort; }
    set { _axisViewIoJogDownInputPort = value; RaisePropertyChanged(); }
}

[JsonIgnore]
private int _axisViewIoJogLeftInputPort = 5;
/// <summary>
/// AxisView IO 方向控制：向左输入端口，对应 X 轴反向。
/// </summary>
public int AxisViewIoJogLeftInputPort
{
    get { return _axisViewIoJogLeftInputPort; }
    set { _axisViewIoJogLeftInputPort = value; RaisePropertyChanged(); }
}

[JsonIgnore]
private int _axisViewIoJogRightInputPort = 6;
/// <summary>
/// AxisView IO 方向控制：向右输入端口，对应 X 轴正向。
/// </summary>
public int AxisViewIoJogRightInputPort
{
    get { return _axisViewIoJogRightInputPort; }
    set { _axisViewIoJogRightInputPort = value; RaisePropertyChanged(); }
}
```

- [ ] **Step 2: Add the configuration UI**

In `ControlCardConfigView.xaml`, inside the “其他” tab `StackPanel Grid.Row="1" Margin="18" VerticalAlignment="Top"`, add this block after the existing startup reset explanatory `TextBlock` elements:

```xml
<Separator Margin="0,18,0,14"/>

<TextBlock Text="AxisView IO方向控制"
           FontSize="15"
           FontWeight="SemiBold"
           Foreground="#333333"/>
<CheckBox Content="启用 AxisView IO方向控制"
          IsChecked="{Binding ModelParam.IsAxisViewIoJogEnabled, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
          FontSize="15"
          Margin="0,10,0,0"
          VerticalAlignment="Center"/>

<Grid Margin="0,12,0,0">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="130"/>
        <ColumnDefinition Width="22"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="130"/>
    </Grid.ColumnDefinitions>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="8"/>
        <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>

    <TextBlock Grid.Row="0" Grid.Column="0" Text="向上输入 IO：" VerticalAlignment="Center" Foreground="#444444"/>
    <hc:NumericUpDown Grid.Row="0" Grid.Column="1"
                      Minimum="0"
                      Maximum="1024"
                      Value="{Binding ModelParam.AxisViewIoJogUpInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                      HorizontalContentAlignment="Center"
                      Style="{StaticResource NumericUpDownPlus}"
                      Margin="8,0"/>

    <TextBlock Grid.Row="0" Grid.Column="3" Text="向下输入 IO：" VerticalAlignment="Center" Foreground="#444444"/>
    <hc:NumericUpDown Grid.Row="0" Grid.Column="4"
                      Minimum="0"
                      Maximum="1024"
                      Value="{Binding ModelParam.AxisViewIoJogDownInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                      HorizontalContentAlignment="Center"
                      Style="{StaticResource NumericUpDownPlus}"
                      Margin="8,0"/>

    <TextBlock Grid.Row="2" Grid.Column="0" Text="向左输入 IO：" VerticalAlignment="Center" Foreground="#444444"/>
    <hc:NumericUpDown Grid.Row="2" Grid.Column="1"
                      Minimum="0"
                      Maximum="1024"
                      Value="{Binding ModelParam.AxisViewIoJogLeftInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                      HorizontalContentAlignment="Center"
                      Style="{StaticResource NumericUpDownPlus}"
                      Margin="8,0"/>

    <TextBlock Grid.Row="2" Grid.Column="3" Text="向右输入 IO：" VerticalAlignment="Center" Foreground="#444444"/>
    <hc:NumericUpDown Grid.Row="2" Grid.Column="4"
                      Minimum="0"
                      Maximum="1024"
                      Value="{Binding ModelParam.AxisViewIoJogRightInputPort, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                      HorizontalContentAlignment="Center"
                      Style="{StaticResource NumericUpDownPlus}"
                      Margin="8,0"/>
</Grid>

<TextBlock Text="仅在运动轴操作面板打开时生效；按住输入 IO 连续移动，松开后停止。默认 IO3/IO4/IO5/IO6 对应 Y+/Y-/X-/X+。"
           Margin="0,12,0,0"
           Foreground="#666666"
           TextWrapping="Wrap"/>
<TextBlock Text="多个方向同时触发时会停止当前运动，不执行斜向或随机方向移动。"
           Margin="0,4,0,0"
           Foreground="#999999"
           TextWrapping="Wrap"/>
```

- [ ] **Step 3: Run tests and verify config/UI tests pass while controller tests still fail**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL. Missing `AxisViewIoJogController` should remain; config property errors should be gone.

- [ ] **Step 4: Commit config and UI if git is usable**

Run:

```powershell
git status --short
```

Expected in a valid git workspace: status output lists `ControlCardConfigModel.cs` and `ControlCardConfigView.xaml`. If git is usable, run:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml
git commit -m "feat: add AxisView IO jog settings"
```

---

### Task 3: Add the IO Jog Controller

**Files:**
- Create: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/Models/AxisViewIoJogController.cs`

- [ ] **Step 1: Create the controller**

Create `AxisViewIoJogController.cs` with this full content:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace ReeYin_V.Hardware.ControlCard.Models
{
    public enum AxisViewIoJogDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public sealed class AxisViewIoJogController : IDisposable
    {
        private readonly ControlCardBase _controlCard;
        private readonly ControlCardConfigModel _config;
        private AxisViewIoJogDirection? _activeDirection;
        private EN_SpeedType _activeSpeedType;

        public AxisViewIoJogController(ControlCardBase controlCard, ControlCardConfigModel config)
        {
            _controlCard = controlCard ?? throw new ArgumentNullException(nameof(controlCard));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Update(
            bool[] inputs,
            EN_SpeedType speedType,
            Func<En_AxisNum, bool> isAxisConfigured,
            Func<En_AxisNum, MoveDirection, bool> canStartJog)
        {
            if (isAxisConfigured == null)
            {
                throw new ArgumentNullException(nameof(isAxisConfigured));
            }

            if (canStartJog == null)
            {
                throw new ArgumentNullException(nameof(canStartJog));
            }

            if (!_config.IsAxisViewIoJogEnabled || _controlCard.IsReseting || !_controlCard.IsReady || inputs == null)
            {
                StopActiveJog();
                return;
            }

            var triggeredDirections = GetTriggeredDirections(inputs).ToList();
            if (triggeredDirections.Count != 1)
            {
                StopActiveJog();
                return;
            }

            var nextDirection = triggeredDirections[0];
            var movement = ResolveMovement(nextDirection);

            if (!isAxisConfigured(movement.Axis))
            {
                StopActiveJog();
                return;
            }

            if (_activeDirection == nextDirection && _activeSpeedType == speedType)
            {
                return;
            }

            StopActiveJog();

            if (!canStartJog(movement.Axis, movement.Direction))
            {
                return;
            }

            if (_controlCard.JogAxis(movement.Axis, movement.Direction, speedType, true))
            {
                _activeDirection = nextDirection;
                _activeSpeedType = speedType;
            }
        }

        public void StopActiveJog()
        {
            if (!_activeDirection.HasValue)
            {
                return;
            }

            var movement = ResolveMovement(_activeDirection.Value);
            _controlCard.JogAxis(movement.Axis, movement.Direction, _activeSpeedType, false);
            _activeDirection = null;
        }

        public void Dispose()
        {
            StopActiveJog();
        }

        public static (En_AxisNum Axis, MoveDirection Direction) ResolveMovement(AxisViewIoJogDirection direction)
        {
            return direction switch
            {
                AxisViewIoJogDirection.Up => (En_AxisNum.Y, MoveDirection.正向),
                AxisViewIoJogDirection.Down => (En_AxisNum.Y, MoveDirection.反向),
                AxisViewIoJogDirection.Left => (En_AxisNum.X, MoveDirection.反向),
                AxisViewIoJogDirection.Right => (En_AxisNum.X, MoveDirection.正向),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        private IEnumerable<AxisViewIoJogDirection> GetTriggeredDirections(bool[] inputs)
        {
            foreach (var item in GetConfiguredPorts())
            {
                if (IsPortActive(inputs, item.Port))
                {
                    yield return item.Direction;
                }
            }
        }

        private IEnumerable<(AxisViewIoJogDirection Direction, int Port)> GetConfiguredPorts()
        {
            yield return (AxisViewIoJogDirection.Up, _config.AxisViewIoJogUpInputPort);
            yield return (AxisViewIoJogDirection.Down, _config.AxisViewIoJogDownInputPort);
            yield return (AxisViewIoJogDirection.Left, _config.AxisViewIoJogLeftInputPort);
            yield return (AxisViewIoJogDirection.Right, _config.AxisViewIoJogRightInputPort);
        }

        private static bool IsPortActive(bool[] inputs, int port)
        {
            return port >= 0 && port < inputs.Length && inputs[port];
        }
    }
}
```

- [ ] **Step 2: Run tests and verify controller tests pass while AxisView integration still fails**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: FAIL only on `TestAxisViewIntegratesIoJogLifecycle`, because `AxisViewModel` has not been wired yet.

- [ ] **Step 3: Commit controller if git is usable**

Run:

```powershell
git status --short
```

Expected in a valid git workspace: status output lists the new controller file. If git is usable, run:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewIoJogController.cs
git commit -m "feat: add AxisView IO jog controller"
```

---

### Task 4: Wire the Controller into AxisViewModel

**Files:**
- Modify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ViewModels/AxisViewModel.cs`

- [ ] **Step 1: Add fields for config and controller**

In `AxisViewModel.cs`, replace the current field area:

```csharp
private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];

private DelegateCommand<string> _generalCommand;
private DelegateCommand<object> _movingCommand;
```

with:

```csharp
private static readonly List<En_AxisNum> PlanarMoveAxes = [En_AxisNum.X, En_AxisNum.Y];

private readonly ControlCardConfigModel _controlCardConfig;
private readonly AxisViewIoJogController _ioJogController;

private DelegateCommand<string> _generalCommand;
private DelegateCommand<object> _movingCommand;
```

- [ ] **Step 2: Initialize the config and controller**

In the `AxisViewModel` constructor, replace:

```csharp
//先直接获取到第一个
this.ControlCard = (PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel).CardModels[0];
```

with:

```csharp
_controlCardConfig = PrismProvider.HardwareModuleManager.Modules[ConfigKey.ControlCard] as ControlCardConfigModel
    ?? new ControlCardConfigModel();

//先直接获取到第一个
this.ControlCard = _controlCardConfig.CardModels[0];
_ioJogController = new AxisViewIoJogController(ControlCard, _controlCardConfig);
```

- [ ] **Step 3: Update the timer to stop on position failure and process IO**

Replace the full `InitTimer()` method with:

```csharp
private void InitTimer()
{
    _timer = new DispatcherTimer();
    _timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
    _timer.Tick += (s, e) =>
    {
        if (!ControlCard.GetAllPosInfos())
        {
            _ioJogController.StopActiveJog();
            return;
        }

        ModelParam.CurPosInfos = AxisViewAxisMatcher.BuildPositionSnapshot(ControlCard.Config.AllAxis);
        RefreshDisplayedAxisStatus();
        RefreshCurrentSpeedSnapshotFromCard();
        UpdateAxisViewIoJog();

        if (!ControlCard.GetAllPosInfos(1))
        {
            Console.WriteLine($"获取核1位置数据失败!!!");
            return;
        }

        ModelParam.CurCore1PosInfos = AxisViewAxisMatcher.BuildPositionSnapshot(ControlCard.Config.AllAxis);
    };
}
```

- [ ] **Step 4: Add the IO Jog update method**

Add this method after `RefreshCurrentSpeedSnapshotFromCard()`:

```csharp
private void UpdateAxisViewIoJog()
{
    if (!_controlCardConfig.IsAxisViewIoJogEnabled)
    {
        _ioJogController.StopActiveJog();
        return;
    }

    if (ControlCard.IsReseting)
    {
        _ioJogController.StopActiveJog();
        return;
    }

    if (!ControlCard.GetAllInput(out var inputStatus))
    {
        Console.WriteLine("获取AxisView IO方向控制输入失败!!!");
        _ioJogController.StopActiveJog();
        return;
    }

    _ioJogController.Update(
        inputStatus,
        ModelParam.CurSpeedType,
        IsAxisConfigured,
        (axis, direction) =>
        {
            if (!ControlCard.ValidateJogLimitCondition(axis, direction, out var message))
            {
                Console.WriteLine($"AxisView IO方向控制限位：{message}");
                return false;
            }

            return true;
        });
}
```

- [ ] **Step 5: Stop IO Jog before manual global stop and pause**

In the `"停止"` and `"暂停"` cases, add `_ioJogController.StopActiveJog();` before `ControlCard.Stop(null);`:

```csharp
case "停止":
    {
        _ioJogController.StopActiveJog();
        ControlCard.Stop(null);
    }
    break;
case "暂停":
    {
        _ioJogController.StopActiveJog();
        ControlCard.Stop(null);
    }
    break;
```

- [ ] **Step 6: Stop IO Jog when closing AxisView**

In the `"关闭"` case, replace:

```csharp
//存一下参数
ConfigManager.Write(ConfigKey.AxisModel, ModelParam);

_timer.Stop();
```

with:

```csharp
//存一下参数
ConfigManager.Write(ConfigKey.AxisModel, ModelParam);

_ioJogController.StopActiveJog();
_timer.Stop();
```

- [ ] **Step 7: Run tests and verify all added tests pass**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: PASS for the added AxisView IO Jog tests. If unrelated existing tests fail, capture the failing test names and error text before changing more code.

- [ ] **Step 8: Commit AxisView integration if git is usable**

Run:

```powershell
git status --short
```

Expected in a valid git workspace: status output lists `AxisViewModel.cs`. If git is usable, run:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs
git commit -m "feat: enable IO jog while AxisView is open"
```

---

### Task 5: Final Verification

**Files:**
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard/ReeYin_V.Hardware.ControlCard.csproj`
- Verify: `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj`

- [ ] **Step 1: Build the base control-card project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ReeYin_V.Hardware.ControlCard.csproj
```

Expected: PASS with `0 Error(s)`. Existing warnings may remain if they are already present before this change.

- [ ] **Step 2: Build the structural test project**

Run:

```powershell
dotnet build Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: PASS with `0 Error(s)`.

- [ ] **Step 3: Run the test executable if build succeeds**

Run:

```powershell
dotnet run --project Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj
```

Expected: process exits with code `0` and prints `ACS PEG/DataCollection tests passed.` after all `Run(...)` calls complete.

- [ ] **Step 4: Review changed files**

Run:

```powershell
git diff -- Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewIoJogController.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs
```

Expected in a valid git workspace: diff shows only tests, config/UI additions, the new controller, and `AxisViewModel` wiring described in this plan. If git is unavailable in this workspace, use file review with the same file list.

- [ ] **Step 5: Final commit if git is usable and earlier commits were skipped**

Run:

```powershell
git status --short
```

Expected in a valid git workspace: only intended files are modified. If git is usable and task-level commits were skipped, run:

```powershell
git add Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\Program.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\ControlCardConfigModel.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Views\ControlCardConfigView.xaml Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\Models\AxisViewIoJogController.cs Hardware\ControlCard\ReeYin_V.Hardware.ControlCard\ViewModels\AxisViewModel.cs
git commit -m "feat: add AxisView IO jog control"
```

---

## Self-Review

- Spec coverage: Tasks cover persisted enable/port config, UI in `ControlCardConfigView`, IO3/4/5/6 mapping, hold-to-move behavior, single-panel lifecycle, multi-direction stop, IO failure stop, and build verification.
- Placeholder scan: The plan contains no placeholder implementation steps; every code-changing step includes exact code snippets or full file content.
- Type consistency: `AxisViewIoJogController`, `AxisViewIoJogDirection`, config property names, test method names, and `AxisViewModel` calls use matching signatures throughout the plan.
