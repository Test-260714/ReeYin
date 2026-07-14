# ACS Axis Control Tab Layout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign the ACS config window axis-control tab into a three-column workspace that keeps selected-axis configuration, speed/home settings, and debug/status operations visible and non-overlapping.

**Architecture:** Keep the existing `AcsControlCardConfigViewModel` command/property surface and primarily refactor `AcsControlCardConfigView.xaml`. Add source tests first to lock the new layout markers, current-axis speed table, ACS-specific sections, and fixed right-side debug panel. Use named XAML regions so future source tests can verify the layout without brittle row/column counting.

**Tech Stack:** C#/.NET 8 WPF, Prism `DelegateCommand`, ReeYin_V shared XAML styles (`GeneralButtonStyle`, `DefaultDataGridStyle`, `ExpanderStyle`), existing console-style ACS source tests.

---

## File Structure

- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`
  - Add/adjust source tests for the new axis-tab layout.
  - Update the existing `TestAcsConfigViewCombinesAxisControls` expectation so `AxisMotionProfiles` is preserved instead of forbidden.
  - Replace the old "scrollable non-overlapping" broad-scroll test with a three-column workspace test.
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml`
  - Replace only the `TabItem Header="轴控制"` contents.
  - Preserve all existing binding names and commands.
  - Add named regions: `AxisContextBar`, `AxisWorkspaceGrid`, `AxisConfigColumn`, `AxisSettingsColumn`, `AxisDebugColumn`.
- Optional modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`
  - Only if implementation reveals a missing notification for the context bar. Do not change motion command behavior.

## Verification Commands

Run from `E:\Company\工作目录\ReeYin-V\ReeYin-V`:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Known caveat: the ACS test executable currently may stop later at the unrelated `Motion model exposes axis-aware target mapping helpers` source test. The new axis-layout tests are registered before that known failure, so they should pass before the test executable reaches the unrelated blocker.

## Task 1: Add Failing Layout Source Tests

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Add new registrations near existing axis config view tests**

Place these lines after:

```csharp
Run("ACS config view axis tab uses scrollable non-overlapping layout", TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout);
```

Add:

```csharp
Run("ACS config view axis tab uses three-column workspace", TestAcsConfigViewAxisTabUsesThreeColumnWorkspace);
Run("ACS config view axis tab keeps fixed debug panel", TestAcsConfigViewAxisTabKeepsFixedDebugPanel);
Run("ACS config view axis tab keeps ACS-specific settings", TestAcsConfigViewAxisTabKeepsAcsSpecificSettings);
```

- [ ] **Step 2: Update `TestAcsConfigViewCombinesAxisControls` to preserve motion profiles**

Find this existing assertion:

```csharp
AssertFalse(axisTab.Contains("AxisMotionProfiles", StringComparison.Ordinal), "axis tab should not keep a separate ACS motion profile table");
```

Replace it with:

```csharp
AssertContains(axisTab, "AxisMotionProfiles", "axis tab should keep ACS motion profile operations");
AssertContains(axisTab, "RefreshMotionProfilesCommand", "axis tab should keep motion profile refresh");
AssertContains(axisTab, "ApplySelectedMotionProfileCommand", "axis tab should allow applying selected motion profile");
AssertContains(axisTab, "ApplyAllMotionProfilesCommand", "axis tab should allow applying all motion profiles");
```

- [ ] **Step 3: Replace the old broad-scroll layout test**

Replace the body of `TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout()` with:

```csharp
void TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    AssertContains(axisTab, "x:Name=\"AxisWorkspaceGrid\"", "axis tab should expose the workspace grid");
    AssertContains(axisTab, "x:Name=\"AxisConfigColumn\"", "axis tab should expose the left configuration column");
    AssertContains(axisTab, "x:Name=\"AxisSettingsColumn\"", "axis tab should expose the middle settings column");
    AssertContains(axisTab, "x:Name=\"AxisDebugColumn\"", "axis tab should expose the right debug column");
    AssertFalse(axisTab.Contains("MinWidth=\"900\"", StringComparison.Ordinal),
        "axis tab should not rely on the old broad horizontal scroll layout");
    AssertFalse(axisTab.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\" HorizontalScrollBarVisibility=\"Auto\"", StringComparison.Ordinal),
        "normal axis layout should avoid the old top-level horizontal scroll viewer");
}
```

- [ ] **Step 4: Add three new source test methods after `TestAcsConfigViewAxisTabUsesScrollableNonOverlappingLayout`**

```csharp
void TestAcsConfigViewAxisTabUsesThreeColumnWorkspace()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);

    AssertTrue(axisTabStart >= 0, "axis control tab should exist");
    AssertTrue(ioTabStart > axisTabStart, "IO tab should follow axis control tab");

    var axisTab = xaml[axisTabStart..ioTabStart];
    AssertContains(axisTab, "x:Name=\"AxisContextBar\"", "axis tab should have a top selected-axis context bar");
    AssertContains(axisTab, "当前轴", "axis context bar should label the selected axis");
    AssertContains(axisTab, "SelectedItem=\"{Binding SelectedAxis, Mode=TwoWay}\"", "axis context bar should bind selected axis");
    AssertContains(axisTab, "当前轴基础配置", "left column should be titled as current-axis basic configuration");
    AssertContains(axisTab, "速度参数", "middle column should expose selected-axis speed parameters");
    AssertContains(axisTab, "单轴回零 Buffer", "middle column should expose selected-axis home Buffer settings");
    AssertContains(axisTab, "调试操作", "right column should expose debug operations");
    AssertContains(axisTab, "Grid.Column=\"0\"", "workspace should place content in the left column");
    AssertContains(axisTab, "Grid.Column=\"1\"", "workspace should place content in the middle column");
    AssertContains(axisTab, "Grid.Column=\"2\"", "workspace should place content in the right column");
}

void TestAcsConfigViewAxisTabKeepsFixedDebugPanel()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);
    var axisTab = xaml[axisTabStart..ioTabStart];
    var debugStart = axisTab.IndexOf("x:Name=\"AxisDebugColumn\"", StringComparison.Ordinal);

    AssertTrue(debugStart >= 0, "axis debug column should exist");
    var debugPanel = axisTab[debugStart..];

    AssertContains(debugPanel, "Command=\"{Binding EnableAxisCommand}\"", "debug panel should keep axis enable command");
    AssertContains(debugPanel, "Command=\"{Binding DisableAxisCommand}\"", "debug panel should keep axis disable command");
    AssertContains(debugPanel, "Command=\"{Binding StopAxisCommand}\"", "debug panel should keep stop command visible");
    AssertContains(debugPanel, "Command=\"{Binding MoveRelativeCommand}\"", "debug panel should keep relative move command");
    AssertContains(debugPanel, "Command=\"{Binding MoveAbsoluteCommand}\"", "debug panel should keep absolute move command");
    AssertContains(debugPanel, "Command=\"{Binding StartPositiveContinuousMoveCommand}\"", "debug panel should keep positive hold-to-move command");
    AssertContains(debugPanel, "Command=\"{Binding StartNegativeContinuousMoveCommand}\"", "debug panel should keep negative hold-to-move command");
    AssertContains(debugPanel, "Command=\"{Binding StopContinuousMoveCommand}\"", "debug panel should keep hold-to-move stop command");
    AssertContains(debugPanel, "Text=\"{Binding AxisStatusText, Mode=OneWay}\"", "debug panel should keep status output visible");
}

void TestAcsConfigViewAxisTabKeepsAcsSpecificSettings()
{
    var xaml = ReadRepoFile(@"Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml");
    var axisTabStart = xaml.IndexOf("Header=\"轴控制\"", StringComparison.Ordinal);
    var ioTabStart = xaml.IndexOf("Header=\"IO测试\"", StringComparison.Ordinal);
    var axisTab = xaml[axisTabStart..ioTabStart];

    AssertContains(axisTab, "ItemsSource=\"{Binding SelectedAxisSpeedRows}\"", "axis tab should keep selected-axis speed rows");
    AssertContains(axisTab, "SelectedAxisHomeBufferNo", "axis tab should keep per-axis home Buffer number");
    AssertContains(axisTab, "SelectedAxisHomeBufferTimeout", "axis tab should keep per-axis home Buffer timeout");
    AssertContains(axisTab, "Options.XyHomeBuffer", "axis tab should keep ACS XY combined home Buffer settings");
    AssertContains(axisTab, "Style=\"{DynamicResource ExpanderStyle}\"", "ACS-specific global settings should use the shared expander style");
    AssertContains(axisTab, "ItemsSource=\"{Binding AxisMotionProfiles}\"", "axis tab should keep ACS motion profile rows");
    AssertContains(axisTab, "SelectedItem=\"{Binding SelectedMotionProfile, Mode=TwoWay}\"", "axis tab should bind selected ACS motion profile");
}
```

- [ ] **Step 5: Run tests and verify RED**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected:

- Build succeeds.
- Test executable fails at one of the new axis-layout assertions, such as missing `AxisWorkspaceGrid` or `AxisContextBar`.

## Task 2: Replace Axis Tab With Three-Column Workspace

**Files:**
- Modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/Views/AcsControlCardConfigView.xaml`

- [ ] **Step 1: Locate the axis tab**

Find:

```xaml
<TabItem Header="轴控制" Width="110" Style="{DynamicResource SingleTabItemStyle}">
```

Replace only this `TabItem` content. Do not modify the connection, IO, LCI, or monitor column sections.

- [ ] **Step 2: Replace the opening axis tab layout shell**

Use this shell inside the `TabItem`:

```xaml
<Grid Margin="12">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>
        <RowDefinition Height="10"/>
        <RowDefinition Height="*"/>
    </Grid.RowDefinitions>

    <Border x:Name="AxisContextBar"
            Grid.Row="0"
            Style="{StaticResource AcsCardStyle}">
        <DockPanel LastChildFill="True">
            <StackPanel DockPanel.Dock="Left" Orientation="Horizontal" VerticalAlignment="Center">
                <TextBlock Text="当前轴" FontWeight="SemiBold" VerticalAlignment="Center" Margin="0,0,8,0"/>
                <ComboBox ItemsSource="{Binding AxisOptions}"
                          SelectedItem="{Binding SelectedAxis, Mode=TwoWay}"
                          Width="110"
                          Height="28"
                          Margin="0,0,14,0"/>
                <TextBlock Text="物理轴" VerticalAlignment="Center" Margin="0,0,4,0"/>
                <TextBlock Text="{Binding SelectedAxisConfig.AxisNo, Mode=OneWay}" VerticalAlignment="Center" Margin="0,0,14,0"/>
                <TextBlock Text="名称" VerticalAlignment="Center" Margin="0,0,4,0"/>
                <TextBlock Text="{Binding SelectedAxisConfig.NickName, Mode=OneWay}" VerticalAlignment="Center" Margin="0,0,14,0"/>
                <CheckBox Content="启用"
                          IsChecked="{Binding SelectedAxisConfig.IsUsing, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                          VerticalAlignment="Center"
                          Margin="0,0,14,0"/>
                <TextBlock Text="回零Buffer" VerticalAlignment="Center" Margin="0,0,4,0"/>
                <TextBlock Text="{Binding SelectedAxisHomeBufferNo, Mode=OneWay}" VerticalAlignment="Center"/>
            </StackPanel>
            <StackPanel DockPanel.Dock="Right" Orientation="Horizontal" HorizontalAlignment="Right">
                <Button Content="刷新轴状态"
                        Width="90"
                        Height="30"
                        Margin="0,0,8,0"
                        Style="{StaticResource GeneralButtonStyle}"
                        Command="{Binding RefreshAxisStatusCommand}"/>
                <Button Content="打开Buffer脚本"
                        Width="112"
                        Height="30"
                        Style="{StaticResource GeneralButtonStyle}"
                        Command="{Binding OpenBufferScriptCommand}"/>
            </StackPanel>
        </DockPanel>
    </Border>

    <Grid x:Name="AxisWorkspaceGrid" Grid.Row="2">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="350" MinWidth="320"/>
            <ColumnDefinition Width="*" MinWidth="430"/>
            <ColumnDefinition Width="330" MinWidth="300"/>
        </Grid.ColumnDefinitions>

        <!-- Insert AxisConfigColumn, AxisSettingsColumn, and AxisDebugColumn in the steps below. -->
    </Grid>
</Grid>
```

- [ ] **Step 3: Add the left current-axis configuration column**

Inside `AxisWorkspaceGrid`, insert this first column:

```xaml
<ScrollViewer x:Name="AxisConfigColumn"
              Grid.Column="0"
              Margin="0,0,10,0"
              VerticalScrollBarVisibility="Auto">
    <Border Style="{StaticResource AcsCardStyle}">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="10"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="92"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <TextBlock Grid.Row="0" Grid.ColumnSpan="2" Text="当前轴基础配置" FontWeight="SemiBold"/>

            <TextBlock Grid.Row="2" Grid.Column="0" Text="物理轴号" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox Grid.Row="2" Grid.Column="1" Text="{Binding SelectedAxisConfig.AxisNo, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>

            <TextBlock Grid.Row="4" Grid.Column="0" Text="轴昵称" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox Grid.Row="4" Grid.Column="1" Text="{Binding SelectedAxisConfig.NickName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>

            <TextBlock Grid.Row="6" Grid.Column="0" Text="脉冲当量/mm" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox Grid.Row="6" Grid.Column="1" Text="{Binding SelectedAxisConfig.PulseEquivalent, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>

            <TextBlock Grid.Row="8" Grid.Column="0" Text="原点偏移" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <TextBox Grid.Row="8" Grid.Column="1" Text="{Binding SelectedAxisConfig.OriginOffset, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>

            <TextBlock Grid.Row="10" Grid.Column="0" Text="正/负软限位" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <Grid Grid.Row="10" Grid.Column="1">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column="0" Text="{Binding SelectedAxisConfig.SoftLimitPositive, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                <TextBox Grid.Column="2" Text="{Binding SelectedAxisConfig.SoftLimitNegative, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
            </Grid>

            <TextBlock Grid.Row="12" Grid.Column="0" Text="安全/减速" VerticalAlignment="Center" Margin="0,0,8,0"/>
            <Grid Grid.Row="12" Grid.Column="1">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <TextBox Grid.Column="0" Text="{Binding SelectedAxisConfig.SafetyDis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                <TextBox Grid.Column="2" Text="{Binding SelectedAxisConfig.DecelerateDis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
            </Grid>
        </Grid>
    </Border>
</ScrollViewer>
```

- [ ] **Step 4: Add the middle speed/home/ACS settings column**

Place this after the left column:

```xaml
<ScrollViewer x:Name="AxisSettingsColumn"
              Grid.Column="1"
              Margin="0,0,10,0"
              VerticalScrollBarVisibility="Auto">
    <StackPanel>
        <Border Style="{StaticResource AcsCardStyle}" Margin="0,0,0,10">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <DockPanel Grid.Row="0" LastChildFill="True">
                    <TextBlock Text="速度参数" FontWeight="SemiBold" VerticalAlignment="Center"/>
                    <TextBlock DockPanel.Dock="Right"
                               Text="当前轴 SpeedDict1"
                               Foreground="#777777"
                               VerticalAlignment="Center"/>
                </DockPanel>
                <DataGrid Grid.Row="2"
                          MinHeight="200"
                          MaxHeight="260"
                          AutoGenerateColumns="False"
                          Style="{StaticResource DefaultDataGridStyle}"
                          ItemsSource="{Binding SelectedAxisSpeedRows}"
                          CanUserAddRows="False"
                          IsReadOnly="False"
                          RowHeaderWidth="0"
                          HeadersVisibility="Column"
                          GridLinesVisibility="All"
                          ScrollViewer.HorizontalScrollBarVisibility="Auto"
                          ScrollViewer.VerticalScrollBarVisibility="Auto">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="速度类型" Binding="{Binding SpeedType}" Width="90"/>
                        <DataGridTextColumn Header="描述" Binding="{Binding SpeedDescribe, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="*"/>
                        <DataGridTextColumn Header="起始速度" Binding="{Binding StartSpeed, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                        <DataGridTextColumn Header="最大速度" Binding="{Binding MaxSpeed, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                        <DataGridTextColumn Header="加减速" Binding="{Binding AccSpeed, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="90"/>
                    </DataGrid.Columns>
                </DataGrid>
            </Grid>
        </Border>

        <Border Style="{StaticResource AcsCardStyle}" Margin="0,0,0,10">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="单轴回零 Buffer" FontWeight="SemiBold"/>
                <Grid Grid.Row="2">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="80"/>
                        <ColumnDefinition Width="12"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="12"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="90"/>
                        <ColumnDefinition Width="12"/>
                        <ColumnDefinition Width="Auto"/>
                    </Grid.ColumnDefinitions>
                    <TextBlock Grid.Column="0" Text="Buffer" VerticalAlignment="Center" Margin="0,0,6,0"/>
                    <TextBox Grid.Column="1" Text="{Binding SelectedAxisHomeBufferNo, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <CheckBox Grid.Column="3" Content="启用" IsChecked="{Binding SelectedAxisHomeBufferEnabled, Mode=TwoWay}" VerticalAlignment="Center"/>
                    <TextBlock Grid.Column="5" Text="超时" VerticalAlignment="Center" Margin="0,0,6,0"/>
                    <TextBox Grid.Column="6" Text="{Binding SelectedAxisHomeBufferTimeout, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <Button Grid.Column="8" Content="回零" Width="64" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding GoHomeCommand}"/>
                </Grid>
                <WrapPanel Grid.Row="4" VerticalAlignment="Center">
                    <CheckBox Content="运行前停止轴" IsChecked="{Binding SelectedAxisStopBeforeHomeBuffer, Mode=TwoWay}" Margin="0,0,14,0"/>
                    <CheckBox Content="成功后清反馈" IsChecked="{Binding SelectedAxisResetFeedbackAfterHome, Mode=TwoWay}" Margin="0,0,14,0"/>
                    <TextBlock Text="清零位置" VerticalAlignment="Center" Margin="0,0,6,0"/>
                    <TextBox Text="{Binding SelectedAxisHomeResetPosition, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Width="76" Height="28"/>
                </WrapPanel>
            </Grid>
        </Border>

        <Border Style="{StaticResource AcsCardStyle}" Margin="0,0,0,10">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="8"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <TextBlock Grid.Row="0" Text="ACS运动参数" FontWeight="SemiBold"/>
                <DataGrid Grid.Row="2"
                          MinHeight="120"
                          MaxHeight="180"
                          AutoGenerateColumns="False"
                          Style="{StaticResource DefaultDataGridStyle}"
                          ItemsSource="{Binding AxisMotionProfiles}"
                          SelectedItem="{Binding SelectedMotionProfile, Mode=TwoWay}"
                          CanUserAddRows="False"
                          IsReadOnly="True"
                          RowHeaderWidth="0">
                    <DataGrid.Columns>
                        <DataGridTextColumn Header="轴" Binding="{Binding Axis}" Width="70"/>
                        <DataGridTextColumn Header="速度" Binding="{Binding Velocity}" Width="80"/>
                        <DataGridTextColumn Header="加速度" Binding="{Binding Acceleration}" Width="90"/>
                        <DataGridTextColumn Header="减速度" Binding="{Binding Deceleration}" Width="90"/>
                        <DataGridTextColumn Header="Jerk" Binding="{Binding Jerk}" Width="80"/>
                    </DataGrid.Columns>
                </DataGrid>
                <StackPanel Grid.Row="4" Orientation="Horizontal">
                    <Button Content="刷新参数" Width="82" Height="30" Margin="0,0,8,0" Style="{StaticResource GeneralButtonStyle}" Command="{Binding RefreshMotionProfilesCommand}"/>
                    <Button Content="下发当前轴" Width="92" Height="30" Margin="0,0,8,0" Style="{StaticResource GeneralButtonStyle}" Command="{Binding ApplySelectedMotionProfileCommand}"/>
                    <Button Content="下发全部轴" Width="92" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding ApplyAllMotionProfilesCommand}"/>
                </StackPanel>
            </Grid>
        </Border>

        <Expander Header="XY合并回零（ACS全局）"
                  IsExpanded="False"
                  Style="{DynamicResource ExpanderStyle}">
            <Border Style="{StaticResource AcsCardStyle}" Margin="0,8,0,0">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="8"/>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="8"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="14"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="70"/>
                        <ColumnDefinition Width="14"/>
                        <ColumnDefinition Width="Auto"/>
                        <ColumnDefinition Width="70"/>
                    </Grid.ColumnDefinitions>
                    <CheckBox Grid.Row="0" Grid.Column="0" Content="启用" IsChecked="{Binding Options.XyHomeBuffer.IsEnabled, Mode=TwoWay}" VerticalAlignment="Center"/>
                    <TextBlock Grid.Row="0" Grid.Column="2" Text="X Buffer" VerticalAlignment="Center" Margin="0,0,4,0"/>
                    <TextBox Grid.Row="0" Grid.Column="3" Text="{Binding Options.XyHomeBuffer.XBufferNo, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <TextBlock Grid.Row="0" Grid.Column="5" Text="Y Buffer" VerticalAlignment="Center" Margin="0,0,4,0"/>
                    <TextBox Grid.Row="0" Grid.Column="6" Text="{Binding Options.XyHomeBuffer.YBufferNo, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <CheckBox Grid.Row="2" Grid.Column="0" Content="运行前停止X/Y" IsChecked="{Binding Options.XyHomeBuffer.StopAxesBeforeRun, Mode=TwoWay}" VerticalAlignment="Center"/>
                    <TextBlock Grid.Row="2" Grid.Column="2" Text="X ACS轴" VerticalAlignment="Center" Margin="0,0,4,0"/>
                    <TextBox Grid.Row="2" Grid.Column="3" Text="{Binding Options.XyHomeBuffer.XPhysicalAxis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <TextBlock Grid.Row="2" Grid.Column="5" Text="Y ACS轴" VerticalAlignment="Center" Margin="0,0,4,0"/>
                    <TextBox Grid.Row="2" Grid.Column="6" Text="{Binding Options.XyHomeBuffer.YPhysicalAxis, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                    <TextBlock Grid.Row="4" Grid.Column="0" Text="超时(ms)" VerticalAlignment="Center" Margin="0,0,6,0"/>
                    <TextBox Grid.Row="4" Grid.Column="2" Grid.ColumnSpan="2" Text="{Binding Options.XyHomeBuffer.Timeout, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                </Grid>
            </Border>
        </Expander>
    </StackPanel>
</ScrollViewer>
```

- [ ] **Step 5: Add the fixed right debug/status column**

Place this after the middle column:

```xaml
<Border x:Name="AxisDebugColumn"
        Grid.Column="2"
        Style="{StaticResource AcsCardStyle}">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="10"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="调试操作" FontWeight="SemiBold"/>

        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="8"/>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="8"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <Button Grid.Column="0" Content="使能" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding EnableAxisCommand}"/>
            <Button Grid.Column="2" Content="失能" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding DisableAxisCommand}"/>
            <Button Grid.Column="4" Content="停止轴" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding StopAxisCommand}"/>
        </Grid>

        <Grid Grid.Row="4">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="Auto"/>
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="8"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            <StackPanel Grid.Row="0" Grid.Column="0">
                <TextBlock Text="相对距离"/>
                <TextBox Text="{Binding RelativeDistance, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28" Margin="0,4,0,0"/>
            </StackPanel>
            <StackPanel Grid.Row="0" Grid.Column="2">
                <TextBlock Text="绝对目标"/>
                <TextBox Text="{Binding AbsoluteTarget, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28" Margin="0,4,0,0"/>
            </StackPanel>
            <Button Grid.Row="2" Grid.Column="0" Content="相对移动" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding MoveRelativeCommand}"/>
            <Button Grid.Row="2" Grid.Column="2" Content="绝对移动" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding MoveAbsoluteCommand}"/>
        </Grid>

        <StackPanel Grid.Row="6">
            <TextBlock Text="点动 / 连续移动" FontWeight="SemiBold" Margin="0,0,0,6"/>
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="90"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="步长" VerticalAlignment="Center" Margin="0,0,6,0"/>
                <TextBox Grid.Column="1" Text="{Binding JogStep, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                <TextBlock Grid.Column="3" Text="速度" VerticalAlignment="Center" Margin="0,0,6,0"/>
                <ComboBox Grid.Column="4" ItemsSource="{Binding SpeedTypeOptions}" SelectedItem="{Binding ContinuousMoveSpeedType, Mode=TwoWay}" Height="28"/>
            </Grid>
            <Grid Margin="0,8,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Button Grid.Column="0" Content="正向点动" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding JogPositiveCommand}"/>
                <Button Grid.Column="2" Content="反向点动" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding JogNegativeCommand}"/>
            </Grid>
            <Grid Margin="0,8,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>
                <Button Grid.Column="0" Content="按住正向" Height="30" Style="{StaticResource GeneralButtonStyle}">
                    <i:Interaction.Triggers>
                        <i:EventTrigger EventName="PreviewMouseDown"><i:InvokeCommandAction Command="{Binding StartPositiveContinuousMoveCommand}"/></i:EventTrigger>
                        <i:EventTrigger EventName="PreviewMouseUp"><i:InvokeCommandAction Command="{Binding StopContinuousMoveCommand}"/></i:EventTrigger>
                        <i:EventTrigger EventName="MouseLeave"><i:InvokeCommandAction Command="{Binding StopContinuousMoveCommand}"/></i:EventTrigger>
                    </i:Interaction.Triggers>
                </Button>
                <Button Grid.Column="2" Content="按住反向" Height="30" Style="{StaticResource GeneralButtonStyle}">
                    <i:Interaction.Triggers>
                        <i:EventTrigger EventName="PreviewMouseDown"><i:InvokeCommandAction Command="{Binding StartNegativeContinuousMoveCommand}"/></i:EventTrigger>
                        <i:EventTrigger EventName="PreviewMouseUp"><i:InvokeCommandAction Command="{Binding StopContinuousMoveCommand}"/></i:EventTrigger>
                        <i:EventTrigger EventName="MouseLeave"><i:InvokeCommandAction Command="{Binding StopContinuousMoveCommand}"/></i:EventTrigger>
                    </i:Interaction.Triggers>
                </Button>
            </Grid>
            <Grid Margin="0,8,0,0">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto"/>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="8"/>
                    <ColumnDefinition Width="80"/>
                </Grid.ColumnDefinitions>
                <TextBlock Grid.Column="0" Text="反馈" VerticalAlignment="Center" Margin="0,0,6,0"/>
                <TextBox Grid.Column="1" Text="{Binding ResetPosition, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" Height="28"/>
                <Button Grid.Column="3" Content="反馈清零" Height="30" Style="{StaticResource GeneralButtonStyle}" Command="{Binding ResetFeedbackCommand}"/>
            </Grid>
        </StackPanel>

        <Grid Grid.Row="8">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="8"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>
            <DockPanel Grid.Row="0" LastChildFill="True">
                <TextBlock Text="轴状态" FontWeight="SemiBold" VerticalAlignment="Center"/>
                <Button DockPanel.Dock="Right" Content="刷新" Width="58" Height="28" Style="{StaticResource GeneralButtonStyle}" Command="{Binding RefreshAxisStatusCommand}"/>
            </DockPanel>
            <TextBox Grid.Row="2"
                     Text="{Binding AxisStatusText, Mode=OneWay}"
                     IsReadOnly="True"
                     BorderThickness="0"
                     Background="Transparent"
                     FontFamily="Consolas"
                     TextWrapping="Wrap"
                     VerticalScrollBarVisibility="Auto"/>
        </Grid>
    </Grid>
</Border>
```

- [ ] **Step 6: Build the ACS project**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

If build fails because `AcsAxisMotionProfile` does not expose one of the columns used in the snippet, inspect `AcsAxisMotionProfile` and change only those `DataGridTextColumn` bindings to existing properties. Keep `ItemsSource="{Binding AxisMotionProfiles}"` and `SelectedItem="{Binding SelectedMotionProfile, Mode=TwoWay}"`.

## Task 3: Tighten ViewModel Refresh Only If Needed

**Files:**
- Optional modify `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS/ViewModels/AcsControlCardConfigViewModel.cs`
- Test through existing source/reflection tests in `Hardware/ControlCard/ReeYin_V.Hardware.ControlCard.ACS.Tests/Program.cs`

- [ ] **Step 1: Check if context bar properties already refresh**

Inspect `SyncSelectedAxisContext()` and verify it raises:

```csharp
RaisePropertyChanged(nameof(SelectedAxisHomeBufferNo));
RaisePropertyChanged(nameof(SelectedAxisHomeBufferEnabled));
RaisePropertyChanged(nameof(SelectedAxisHomeBufferTimeout));
```

Expected: these calls already exist.

- [ ] **Step 2: Add missing notifications only if they are absent**

If the XAML context bar binds to `SelectedAxisConfig.AxisNo`, `SelectedAxisConfig.NickName`, and `SelectedAxisConfig.IsUsing`, verify `SelectedAxisConfig` setter raises `RaisePropertyChanged()`. If it does not, update the setter to:

```csharp
public SingleAxisParam? SelectedAxisConfig
{
    get => _selectedAxisConfig;
    private set
    {
        _selectedAxisConfig = value;
        RaisePropertyChanged();
    }
}
```

If the existing setter already matches this pattern, do not change the ViewModel.

- [ ] **Step 3: Build ACS and tests after any ViewModel change**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: both builds exit 0.

## Task 4: Run Green Verification

**Files:**
- No source edits unless verification exposes a concrete issue.

- [ ] **Step 1: Build the ACS test project**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\ReeYin_V.Hardware.ControlCard.ACS.Tests.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

- [ ] **Step 2: Run the ACS tests**

Run:

```powershell
dotnet "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS.Tests\bin\Debug\net8.0-windows\ReeYin_V.Hardware.ControlCard.ACS.Tests.dll"
```

Expected:

- All axis layout tests pass:
  - `ACS config view combines axis controls`
  - `ACS config view uses selected-axis speed table`
  - `ACS config view axis tab uses scrollable non-overlapping layout`
  - `ACS config view axis tab uses three-column workspace`
  - `ACS config view axis tab keeps fixed debug panel`
  - `ACS config view axis tab keeps ACS-specific settings`
- The executable may later stop at the known unrelated `Motion model exposes axis-aware target mapping helpers` failure. If so, report it as unrelated and include the fact that all new layout tests passed before the blocker.

- [ ] **Step 3: Build the ACS project**

Run:

```powershell
dotnet build "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\ReeYin_V.Hardware.ControlCard.ACS.csproj" --no-dependencies -m:1 -p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\" -v:minimal
```

Expected: exit code 0.

- [ ] **Step 4: Source-check preserved bindings**

Run:

```powershell
rg -n "SelectedAxisSpeedRows|SelectedAxisHomeBufferNo|Options.XyHomeBuffer|AxisMotionProfiles|StopAxisCommand|AxisStatusText|AxisWorkspaceGrid" "Hardware\ControlCard\ReeYin_V.Hardware.ControlCard.ACS\Views\AcsControlCardConfigView.xaml"
```

Expected: every token appears in `AcsControlCardConfigView.xaml`.

## Self-Review Notes

- Spec coverage: the plan implements the confirmed three-column workspace, selected-axis context bar, selected-axis speed table, ACS-specific settings, and fixed debug/status panel.
- TDD coverage: Task 1 adds source tests and requires observing RED before XAML changes.
- Type consistency: all bindings use existing ViewModel members except optional context display relying on existing `SelectedAxisConfig` and `SelectedAxisHomeBufferNo`.
- Scope control: the plan does not change command behavior, hardware calls, LCI tab, IO tab, monitor panel, or base control-card APIs.
- Git note: this workspace currently reports `fatal: not a git repository`, so commit steps are intentionally omitted.
