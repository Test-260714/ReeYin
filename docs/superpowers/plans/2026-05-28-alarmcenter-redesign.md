# AlarmCenter Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rework `Application\ReeYin.AlarmCenter` into the approved “稳态指挥舱” visual direction while preserving current ViewModel data flow and commands.

**Architecture:** Keep the existing Prism navigation and bindings intact: `AlarmWorkbenchShellViewModel` still owns shell, realtime, history, and statistics data; `AlarmDefinitionsViewModel` still owns rule management data. Implement the redesign in XAML by expanding `AlarmWorkbenchResources.xaml` first, then replacing each view layout with shared styles and structural markers that can be statically checked.

**Tech Stack:** .NET 8 WPF, Prism, XAML resource dictionaries, LightningChart WPF, PowerShell static checks, `dotnet build`.

---

## File Structure

- Create: `Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1` - static verification script for required resource keys, layout markers, and existing binding preservation.
- Modify: `Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml` - central AlarmCenter color, card, command bar, navigation, table, badge, and event card resources.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml` - command-center shell with deep rail navigation, KPI strip, status/export area, and Prism content region.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmRealtimeView.xaml` - realtime triage layout with activity table, detail/action panel, and realtime feed.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmHistoryView.xaml` - fixed filter/export bar, history table, and pager.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmStatisticsView.xaml` - analytics layout around existing LightningChart bindings and statistic collections.
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml` - governance-style rule management layout while preserving all current tabs, lists, editor overlays, and commands.

Version-control note: this workspace is not a Git repository and the `svn` CLI is not installed in PATH. Checkpoint steps list files and build outputs instead of running commit commands.

## Task 1: Add Redesign Static Check Harness

**Files:**
- Create: `Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1`

- [ ] **Step 1: Write the failing static check**

Create `Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1` with this content:

```powershell
$ErrorActionPreference = 'Stop'

param(
    [ValidateSet('Resources', 'Shell', 'Realtime', 'History', 'Statistics', 'Definitions', 'All')]
    [string]$Stage = 'All'
)

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$resourcePath = Join-Path $root 'Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml'
$shellPath = Join-Path $root 'Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml'
$realtimePath = Join-Path $root 'Application\ReeYin.AlarmCenter\Views\AlarmRealtimeView.xaml'
$historyPath = Join-Path $root 'Application\ReeYin.AlarmCenter\Views\AlarmHistoryView.xaml'
$statisticsPath = Join-Path $root 'Application\ReeYin.AlarmCenter\Views\AlarmStatisticsView.xaml'
$definitionsPath = Join-Path $root 'Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml'

function Read-Text {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Missing file: $Path"
    }
    return Get-Content -Raw -LiteralPath $Path
}

function Assert-Contains {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -notmatch $Pattern) {
        throw $Message
    }
}

function Assert-DoesNotContain {
    param(
        [string]$Content,
        [string]$Pattern,
        [string]$Message
    )

    if ($Content -match $Pattern) {
        throw $Message
    }
}

$resources = Read-Text $resourcePath

if ($Stage -in @('Resources', 'All')) {
    Assert-Contains $resources 'x:Key="AlarmShellBackgroundBrush"' 'Missing AlarmShellBackgroundBrush resource.'
    Assert-Contains $resources 'x:Key="AlarmRailBackgroundBrush"' 'Missing AlarmRailBackgroundBrush resource.'
    Assert-Contains $resources 'x:Key="AlarmCommandBarStyle"' 'Missing AlarmCommandBarStyle resource.'
    Assert-Contains $resources 'x:Key="AlarmRailStyle"' 'Missing AlarmRailStyle resource.'
    Assert-Contains $resources 'x:Key="AlarmContentCardStyle"' 'Missing AlarmContentCardStyle resource.'
    Assert-Contains $resources 'x:Key="AlarmDataGridStyle"' 'Missing AlarmDataGridStyle resource.'
    Assert-Contains $resources 'x:Key="AlarmMetricCardTemplate"' 'Missing AlarmMetricCardTemplate resource.'
    Assert-Contains $resources 'x:Key="AlarmEventCardTemplate"' 'Missing AlarmEventCardTemplate resource.'
    Assert-Contains $resources 'BasedOn="{StaticResource GeneralButtonStyle}"' 'Alarm buttons must stay based on ReeYin_V.UI GeneralButtonStyle.'
    Assert-Contains $resources 'BasedOn="{StaticResource DefaultDataGridStyle}"' 'Alarm grids must stay based on ReeYin_V.UI DefaultDataGridStyle.'
}

if ($Stage -in @('Shell', 'All')) {
    $shell = Read-Text $shellPath
    Assert-Contains $shell 'AlarmShellRoot' 'Shell root marker missing.'
    Assert-Contains $shell 'Style="{StaticResource AlarmRailStyle}"' 'Shell must use the deep rail style.'
    Assert-Contains $shell 'AlarmCommandBarStyle' 'Shell must use the command bar style.'
    Assert-Contains $shell 'AlarmMetricCardTemplate' 'Shell must use the redesigned metric card template.'
    Assert-Contains $shell 'AlarmWorkbenchContentRegion' 'Shell must preserve the Prism content region.'
    Assert-Contains $shell 'OpenLastExportLocationCommand' 'Shell must preserve the open export location command.'
    Assert-Contains $shell 'HasExportedFileLocation' 'Shell must preserve export location visibility binding.'
}

if ($Stage -in @('Realtime', 'All')) {
    $realtime = Read-Text $realtimePath
    Assert-Contains $realtime 'RealtimeTriageGrid' 'Realtime page must expose the triage grid marker.'
    Assert-Contains $realtime 'ItemsSource="{Binding ActiveAlarms}"' 'Realtime page must preserve ActiveAlarms binding.'
    Assert-Contains $realtime 'SelectedItem="{Binding SelectedActiveAlarm}"' 'Realtime page must preserve SelectedActiveAlarm binding.'
    Assert-Contains $realtime 'Command="{Binding ConfirmSelectedCommand}"' 'Realtime page must preserve confirm command.'
    Assert-Contains $realtime 'Command="{Binding ClearSelectedCommand}"' 'Realtime page must preserve clear command.'
    Assert-Contains $realtime 'ItemsSource="{Binding RealtimeFeed}"' 'Realtime page must preserve RealtimeFeed binding.'
    Assert-Contains $realtime 'AlarmEventCardTemplate' 'Realtime feed must use the redesigned event card template.'
}

if ($Stage -in @('History', 'All')) {
    $history = Read-Text $historyPath
    Assert-Contains $history 'HistoryJournalLayout' 'History page layout marker missing.'
    Assert-Contains $history 'SelectedDate="{Binding FilterStartDate}"' 'History page must preserve start date binding.'
    Assert-Contains $history 'SelectedDate="{Binding FilterEndDate}"' 'History page must preserve end date binding.'
    Assert-Contains $history 'Command="{Binding ApplyHistoryFilterCommand}"' 'History page must preserve query command.'
    Assert-Contains $history 'Command="{Binding ExportCsvCommand}"' 'History page must preserve CSV export command.'
    Assert-Contains $history 'Command="{Binding ExportExcelCommand}"' 'History page must preserve Excel export command.'
    Assert-Contains $history 'ItemsSource="{Binding HistoryAlarms}"' 'History page must preserve HistoryAlarms binding.'
}

if ($Stage -in @('Statistics', 'All')) {
    $statistics = Read-Text $statisticsPath
    Assert-Contains $statistics 'StatisticsAnalyticsLayout' 'Statistics page layout marker missing.'
    Assert-Contains $statistics 'ItemsSource="{Binding StatisticCards}"' 'Statistics page must preserve StatisticCards binding.'
    Assert-Contains $statistics 'ViewXY="{Binding TrendChartView}"' 'Statistics page must preserve trend chart binding.'
    Assert-Contains $statistics 'ViewXY="{Binding TypeChartView}"' 'Statistics page must preserve type chart binding.'
    Assert-Contains $statistics 'ViewXY="{Binding SourceChartView}"' 'Statistics page must preserve source chart binding.'
    Assert-Contains $statistics 'ItemsSource="{Binding TrendPoints}"' 'Statistics page must preserve TrendPoints binding.'
}

if ($Stage -in @('Definitions', 'All')) {
    $definitions = Read-Text $definitionsPath
    Assert-Contains $definitions 'DefinitionsGovernanceLayout' 'Definitions page layout marker missing.'
    Assert-Contains $definitions 'ItemsSource="{Binding Definitions}"' 'Definitions list binding missing.'
    Assert-Contains $definitions 'ItemsSource="{Binding SuppressionRules}"' 'SuppressionRules binding missing.'
    Assert-Contains $definitions 'ItemsSource="{Binding Shelves}"' 'Shelves binding missing.'
    Assert-Contains $definitions 'ItemsSource="{Binding NotificationRoutes}"' 'NotificationRoutes binding missing.'
    Assert-Contains $definitions 'ItemsSource="{Binding AuditItems}"' 'AuditItems binding missing.'
    Assert-Contains $definitions 'IsDefinitionEditorOpen' 'Definition editor overlay binding missing.'
    Assert-Contains $definitions 'SaveDefinitionCommand' 'Definition save command missing.'
    Assert-Contains $definitions 'RefreshAuditCommand' 'Audit refresh command missing.'
    Assert-DoesNotContain $definitions '<ColumnDefinition Width="3\*"' 'Definitions page must not return to the old cramped split editor layout.'
}

Write-Host "AlarmCenter redesign source checks passed for stage: $Stage"
```

- [ ] **Step 2: Run the check before implementation and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Resources
```

Expected: command fails with `Missing AlarmShellBackgroundBrush resource.`

- [ ] **Step 3: Capture current build status**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`. If the build fails before any redesign changes, stop and record the existing errors separately because later failures would not be attributable to this redesign.

- [ ] **Step 4: Checkpoint**

Record the changed file list:

```powershell
Get-ChildItem Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 | Select-Object FullName,Length,LastWriteTime
```

Expected: one row for `Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1`.

## Task 2: Expand Shared AlarmCenter Resources

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml`

- [ ] **Step 1: Run the resource-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Resources
```

Expected: command fails with `Missing AlarmShellBackgroundBrush resource.`

- [ ] **Step 2: Add the shared brush and style resources**

In `Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml`, keep the existing namespace declarations and `BooleanToVisibilityConverter`, then add these resources before the existing `LightChartStyle`. Existing templates can remain below this block until later tasks rename usages.

```xaml
    <SolidColorBrush x:Key="AlarmShellBackgroundBrush" Color="#EEF5FA" />
    <SolidColorBrush x:Key="AlarmRailBackgroundBrush" Color="#102640" />
    <SolidColorBrush x:Key="AlarmRailSelectedBrush" Color="#1F4B6F" />
    <SolidColorBrush x:Key="AlarmBorderBrush" Color="#DCE7F1" />
    <SolidColorBrush x:Key="AlarmTextBrush" Color="#162238" />
    <SolidColorBrush x:Key="AlarmMutedBrush" Color="#68768D" />
    <SolidColorBrush x:Key="AlarmPanelBrush" Color="#F8FBFD" />
    <SolidColorBrush x:Key="AlarmPrimaryBrush" Color="#1664D9" />
    <SolidColorBrush x:Key="AlarmCyanBrush" Color="#13A8C6" />
    <SolidColorBrush x:Key="AlarmDangerBrush" Color="#DC2626" />
    <SolidColorBrush x:Key="AlarmWarningBrush" Color="#F59E0B" />
    <SolidColorBrush x:Key="AlarmSuccessBrush" Color="#13A37B" />
    <SolidColorBrush x:Key="AlarmDangerSoftBrush" Color="#FFF0F0" />
    <SolidColorBrush x:Key="AlarmWarningSoftBrush" Color="#FFF7E4" />
    <SolidColorBrush x:Key="AlarmSuccessSoftBrush" Color="#EAFAF5" />

    <Style x:Key="AlarmShellRootStyle" TargetType="Grid">
        <Setter Property="Background" Value="{StaticResource AlarmShellBackgroundBrush}" />
        <Setter Property="SnapsToDevicePixels" Value="True" />
    </Style>

    <Style x:Key="AlarmRailStyle" TargetType="Border">
        <Setter Property="Width" Value="96" />
        <Setter Property="Padding" Value="12,20" />
        <Setter Property="Background" Value="{StaticResource AlarmRailBackgroundBrush}" />
        <Setter Property="CornerRadius" Value="24,0,0,24" />
    </Style>

    <Style x:Key="AlarmContentCardStyle" TargetType="Border">
        <Setter Property="Padding" Value="16" />
        <Setter Property="Background" Value="White" />
        <Setter Property="BorderBrush" Value="{StaticResource AlarmBorderBrush}" />
        <Setter Property="BorderThickness" Value="1" />
        <Setter Property="CornerRadius" Value="22" />
        <Setter Property="SnapsToDevicePixels" Value="True" />
    </Style>

    <Style x:Key="CardStyle" TargetType="Border" BasedOn="{StaticResource AlarmContentCardStyle}" />

    <Style x:Key="QueryBarStyle" TargetType="Border" BasedOn="{StaticResource AlarmContentCardStyle}">
        <Setter Property="Padding" Value="14" />
        <Setter Property="Background" Value="#FCFCFD" />
    </Style>

    <Style x:Key="AlarmCommandBarStyle" TargetType="Border" BasedOn="{StaticResource AlarmContentCardStyle}">
        <Setter Property="Padding" Value="20,18" />
        <Setter Property="Margin" Value="0,0,0,14" />
        <Setter Property="CornerRadius" Value="24" />
    </Style>

    <Style x:Key="AlarmWorkbenchNavigationPanel" TargetType="Border" BasedOn="{StaticResource AlarmContentCardStyle}">
        <Setter Property="Padding" Value="12" />
        <Setter Property="Background" Value="#F5F8FB" />
    </Style>

    <Style x:Key="AlarmPageNavButtonStyle"
           TargetType="Button"
           BasedOn="{StaticResource GeneralButtonStyle}">
        <Setter Property="Height" Value="64" />
        <Setter Property="Margin" Value="0,0,0,10" />
        <Setter Property="Padding" Value="8" />
        <Setter Property="HorizontalContentAlignment" Value="Center" />
        <Setter Property="VerticalContentAlignment" Value="Center" />
        <Setter Property="Background" Value="#183856" />
        <Setter Property="BorderBrush" Value="#284D6E" />
        <Setter Property="Foreground" Value="#D7E7F7" />
        <Setter Property="FontWeight" Value="SemiBold" />
        <Setter Property="TextBlock.TextAlignment" Value="Center" />
        <Style.Triggers>
            <DataTrigger Binding="{Binding IsSelected}" Value="True">
                <Setter Property="Background" Value="{StaticResource AlarmRailSelectedBrush}" />
                <Setter Property="BorderBrush" Value="{StaticResource AlarmCyanBrush}" />
                <Setter Property="Foreground" Value="White" />
            </DataTrigger>
        </Style.Triggers>
    </Style>

    <Style x:Key="AlarmPrimaryButtonStyle"
           TargetType="Button"
           BasedOn="{StaticResource GeneralButtonStyle}">
        <Setter Property="Height" Value="34" />
        <Setter Property="MinWidth" Value="92" />
        <Setter Property="Padding" Value="14,0" />
        <Setter Property="Background" Value="{StaticResource AlarmPrimaryBrush}" />
        <Setter Property="BorderBrush" Value="{StaticResource AlarmPrimaryBrush}" />
        <Setter Property="Foreground" Value="White" />
        <Setter Property="FontWeight" Value="SemiBold" />
    </Style>

    <Style x:Key="AlarmSecondaryButtonStyle"
           TargetType="Button"
           BasedOn="{StaticResource GeneralButtonStyle}">
        <Setter Property="Height" Value="34" />
        <Setter Property="MinWidth" Value="92" />
        <Setter Property="Padding" Value="14,0" />
        <Setter Property="Background" Value="White" />
        <Setter Property="BorderBrush" Value="{StaticResource AlarmBorderBrush}" />
        <Setter Property="Foreground" Value="#344054" />
        <Setter Property="FontWeight" Value="SemiBold" />
    </Style>

    <Style x:Key="AlarmDataGridStyle"
           TargetType="DataGrid"
           BasedOn="{StaticResource DefaultDataGridStyle}">
        <Setter Property="AutoGenerateColumns" Value="False" />
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="CanUserAddRows" Value="False" />
        <Setter Property="CanUserDeleteRows" Value="False" />
        <Setter Property="EnableColumnVirtualization" Value="True" />
        <Setter Property="EnableRowVirtualization" Value="True" />
        <Setter Property="GridLinesVisibility" Value="None" />
        <Setter Property="HeadersVisibility" Value="Column" />
        <Setter Property="IsReadOnly" Value="True" />
    </Style>
```

- [ ] **Step 3: Replace the metric template key**

Rename the existing KPI template from `MetricCardTemplate` to `AlarmMetricCardTemplate`, and update its body to this shape:

```xaml
    <DataTemplate x:Key="AlarmMetricCardTemplate" DataType="{x:Type models:AlarmSummaryCard}">
        <Border Margin="6,0,6,12"
                Background="{Binding BackgroundBrush}"
                Style="{StaticResource AlarmContentCardStyle}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <TextBlock Foreground="{StaticResource AlarmMutedBrush}" Text="{Binding Title}" />
                    <Ellipse Grid.Column="1" Width="10" Height="10" Fill="{Binding AccentBrush}" />
                </Grid>
                <TextBlock Grid.Row="1"
                           Margin="0,10,0,0"
                           FontSize="28"
                           FontWeight="Bold"
                           Foreground="{StaticResource AlarmTextBrush}"
                           Text="{Binding Value}" />
                <TextBlock Grid.Row="2"
                           Margin="0,8,0,0"
                           Foreground="{StaticResource AlarmMutedBrush}"
                           Text="{Binding Caption}"
                           TextWrapping="Wrap" />
            </Grid>
        </Border>
    </DataTemplate>
```

- [ ] **Step 4: Add the event card template**

Add this template after the statistic templates:

```xaml
    <DataTemplate x:Key="AlarmEventCardTemplate" DataType="{x:Type models:AlarmFeedItem}">
        <Border Margin="0,0,0,10"
                Padding="14"
                Background="{Binding RowBackground}"
                BorderBrush="{Binding RowBorderBrush}"
                BorderThickness="1"
                CornerRadius="18">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <Border Padding="8,4"
                        Background="{Binding LevelBadgeBrush}"
                        CornerRadius="12"
                        VerticalAlignment="Top">
                    <TextBlock Foreground="White" Text="{Binding LevelText}" />
                </Border>
                <StackPanel Grid.Column="1" Margin="12,0,12,0">
                    <TextBlock FontWeight="SemiBold"
                               Foreground="{StaticResource AlarmTextBrush}"
                               Text="{Binding Summary}"
                               TextTrimming="CharacterEllipsis" />
                    <TextBlock Margin="0,6,0,0"
                               Style="{StaticResource MutedTextStyle}"
                               Text="{Binding Message}" />
                </StackPanel>
                <StackPanel Grid.Column="2" HorizontalAlignment="Right">
                    <TextBlock HorizontalAlignment="Right" Foreground="#D97706" Text="{Binding ActionText}" />
                    <TextBlock Margin="0,6,0,0" Foreground="{StaticResource AlarmMutedBrush}" Text="{Binding TimestampText}" />
                </StackPanel>
            </Grid>
        </Border>
    </DataTemplate>
```

- [ ] **Step 5: Run resource verification**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Resources
```

Expected: `AlarmCenter redesign source checks passed for stage: Resources`

- [ ] **Step 6: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 3: Redesign Workbench Shell

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml`

- [ ] **Step 1: Run shell-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Shell
```

Expected: command fails with `Shell root marker missing.`

- [ ] **Step 2: Replace the shell layout**

In `AlarmWorkbenchShellView.xaml`, keep the `UserControl` declaration and resource dictionary, then replace the root `<Grid Margin="14">...</Grid>` with this layout:

```xaml
    <Grid x:Name="AlarmShellRoot" Style="{StaticResource AlarmShellRootStyle}" Margin="14">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="96" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <Border Grid.Column="0" Style="{StaticResource AlarmRailStyle}">
            <DockPanel LastChildFill="True">
                <Border DockPanel.Dock="Top"
                        Width="48"
                        Height="48"
                        Margin="0,0,0,24"
                        Background="{StaticResource AlarmCyanBrush}"
                        CornerRadius="18" />
                <ItemsControl ItemsSource="{Binding PageItems}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate DataType="{x:Type models:AlarmWorkbenchPageItem}">
                            <Button Command="{Binding DataContext.NavigatePageCommand, RelativeSource={RelativeSource AncestorType=ItemsControl}}"
                                    CommandParameter="{Binding}"
                                    Content="{Binding Header}"
                                    Style="{StaticResource AlarmPageNavButtonStyle}" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </DockPanel>
        </Border>

        <Grid Grid.Column="1" Margin="16,0,0,0">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <Border Grid.Row="0" Style="{StaticResource AlarmCommandBarStyle}">
                <Grid>
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>
                    <StackPanel>
                        <TextBlock Foreground="{StaticResource AlarmCyanBrush}"
                                   FontSize="12"
                                   FontWeight="Bold"
                                   Text="ALARM COMMAND CENTER" />
                        <TextBlock Margin="0,6,0,0"
                                   FontSize="30"
                                   FontWeight="Bold"
                                   Foreground="{StaticResource AlarmTextBrush}"
                                   Text="报警中心" />
                        <TextBlock Margin="0,6,0,0"
                                   Style="{StaticResource MutedTextStyle}"
                                   Text="监控活动报警、历史记录、趋势分布和硬件报警状态。" />
                    </StackPanel>
                    <StackPanel Grid.Column="1" MinWidth="320" VerticalAlignment="Center">
                        <ProgressBar Height="6"
                                     Margin="0,0,0,8"
                                     IsIndeterminate="True"
                                     Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}" />
                        <Grid>
                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="*" />
                                <ColumnDefinition Width="Auto" />
                            </Grid.ColumnDefinitions>
                            <TextBlock VerticalAlignment="Center"
                                       HorizontalAlignment="Right"
                                       Foreground="{StaticResource AlarmMutedBrush}"
                                       Text="{Binding StatusText}"
                                       TextWrapping="Wrap" />
                            <Button Grid.Column="1"
                                    Margin="8,0,0,0"
                                    Command="{Binding OpenLastExportLocationCommand}"
                                    Content="打开位置"
                                    Style="{StaticResource AlarmSecondaryButtonStyle}"
                                    Visibility="{Binding HasExportedFileLocation, Converter={StaticResource BooleanToVisibilityConverter}}" />
                        </Grid>
                    </StackPanel>
                </Grid>
            </Border>

            <ItemsControl Grid.Row="1"
                          Margin="0,0,0,14"
                          ItemTemplate="{StaticResource AlarmMetricCardTemplate}"
                          ItemsSource="{Binding SummaryCards}">
                <ItemsControl.ItemsPanel>
                    <ItemsPanelTemplate>
                        <WrapPanel ItemWidth="220" />
                    </ItemsPanelTemplate>
                </ItemsControl.ItemsPanel>
            </ItemsControl>

            <Border Grid.Row="2" Style="{StaticResource AlarmContentCardStyle}" Padding="0">
                <ContentControl prism:RegionManager.RegionName="AlarmWorkbenchContentRegion"
                                HorizontalAlignment="Stretch"
                                VerticalAlignment="Stretch" />
            </Border>
        </Grid>
    </Grid>
```

- [ ] **Step 3: Verify shell stage**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Shell
```

Expected: `AlarmCenter redesign source checks passed for stage: Shell`

- [ ] **Step 4: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 4: Redesign Realtime Alarm Triage Page

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmRealtimeView.xaml`

- [ ] **Step 1: Run realtime-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Realtime
```

Expected: command fails with `Realtime page must expose the triage grid marker.`

- [ ] **Step 2: Replace page-local row/cell styles with shared styles**

Remove `ActiveAlarmRowStyle`, `ActiveAlarmCellStyle`, and `ActiveAlarmTextElementStyle` from the page resources. The page must use:

```xaml
RowStyle="{StaticResource FlatRowStyle}"
CellStyle="{StaticResource FlatCellStyle}"
ColumnHeaderStyle="{StaticResource FlatColumnHeaderStyle}"
Style="{StaticResource AlarmDataGridStyle}"
```

- [ ] **Step 3: Replace the outer layout**

Replace the root page `<Grid Margin="16">...</Grid>` with a root named grid:

```xaml
    <Grid x:Name="RealtimeTriageGrid" Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Style="{StaticResource AlarmCommandBarStyle}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <StackPanel>
                    <TextBlock Foreground="{StaticResource AlarmCyanBrush}"
                               FontSize="12"
                               FontWeight="Bold"
                               Text="REALTIME TRIAGE" />
                    <TextBlock Margin="0,6,0,0"
                               Style="{StaticResource SectionTitleStyle}"
                               Text="实时报警监控" />
                    <TextBlock Margin="0,6,0,0"
                               Style="{StaticResource MutedTextStyle}"
                               Text="默认只刷新活动报警和实时事件，确认或清除后延迟同步，降低进入页面时的阻塞。" />
                </StackPanel>
                <StackPanel Grid.Column="1" VerticalAlignment="Center" Orientation="Horizontal">
                    <TextBlock Margin="0,0,16,0"
                               VerticalAlignment="Center"
                               Style="{StaticResource MutedTextStyle}"
                               Text="{Binding FeedStateText}" />
                    <Button Command="{Binding ToggleRealtimeCommand}"
                            Content="{Binding RealtimeToggleText}"
                            Style="{StaticResource AlarmPrimaryButtonStyle}" />
                </StackPanel>
            </Grid>
        </Border>

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="3*" />
                <ColumnDefinition Width="16" />
                <ColumnDefinition Width="2*" />
            </Grid.ColumnDefinitions>

            <Border Grid.Column="0" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <Border Padding="16,14" BorderBrush="{StaticResource AlarmBorderBrush}" BorderThickness="0,0,0,1">
                        <DockPanel>
                            <TextBlock DockPanel.Dock="Right" VerticalAlignment="Center" Style="{StaticResource MutedTextStyle}" Text="最多展示 200 条活动报警" />
                            <StackPanel>
                                <TextBlock Style="{StaticResource SectionTitleStyle}" Text="当前活动报警" />
                                <TextBlock Margin="0,4,0,0" Style="{StaticResource MutedTextStyle}" Text="按等级、来源、位置和持续时间快速定位未处理报警。" />
                            </StackPanel>
                        </DockPanel>
                    </Border>

                    <DataGrid Grid.Row="1"
                              Margin="12"
                              Style="{StaticResource AlarmDataGridStyle}"
                              RowStyle="{StaticResource FlatRowStyle}"
                              CellStyle="{StaticResource FlatCellStyle}"
                              ColumnHeaderStyle="{StaticResource FlatColumnHeaderStyle}"
                              ItemsSource="{Binding ActiveAlarms}"
                              SelectedItem="{Binding SelectedActiveAlarm}">
                        <!-- ActiveAlarms columns are the existing column block from this file. -->
                    </DataGrid>
                </Grid>
            </Border>

            <Grid Grid.Column="2">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="16" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>

                <Border Grid.Row="0" Style="{StaticResource AlarmContentCardStyle}">
                    <!-- Selected alarm detail is the existing detail block from this file with the same commands. -->
                </Border>

                <Border Grid.Row="2" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                    <Grid>
                        <Grid.RowDefinitions>
                            <RowDefinition Height="Auto" />
                            <RowDefinition Height="*" />
                        </Grid.RowDefinitions>
                        <Border Padding="16,14" BorderBrush="{StaticResource AlarmBorderBrush}" BorderThickness="0,0,0,1">
                            <DockPanel>
                                <Button DockPanel.Dock="Right"
                                        Command="{Binding ToggleRealtimeCommand}"
                                        Content="{Binding RealtimeToggleText}"
                                        Style="{StaticResource AlarmSecondaryButtonStyle}" />
                                <StackPanel>
                                    <TextBlock Style="{StaticResource SectionTitleStyle}" Text="实时事件流" />
                                    <TextBlock Margin="0,4,0,0" Style="{StaticResource MutedTextStyle}" Text="最新事件置顶，高频报警时保持列表虚拟化。" />
                                </StackPanel>
                            </DockPanel>
                        </Border>
                        <ScrollViewer Grid.Row="1" Margin="12" VerticalScrollBarVisibility="Auto">
                            <ItemsControl ItemTemplate="{StaticResource AlarmEventCardTemplate}"
                                          ItemsSource="{Binding RealtimeFeed}" />
                        </ScrollViewer>
                    </Grid>
                </Border>
            </Grid>
        </Grid>
    </Grid>
```

- [ ] **Step 4: Preserve and paste existing DataGrid columns and detail content**

In the `DataGrid.Columns` target section, paste the existing column block from `AlarmRealtimeView.xaml` that contains headers `等级`, `来源`, `位置`, `编码`, `报警项`, `报警信息`, `触发时间`, `持续时长`, `次数`, and `确认状态`. In the detail target section, paste the existing selected-alarm detail content that contains `SelectedActiveAlarm.Title`, `SelectedActiveAlarm.Subtitle`, `ConfirmSelectedCommand`, and `ClearSelectedCommand`.

- [ ] **Step 5: Verify realtime stage**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Realtime
```

Expected: `AlarmCenter redesign source checks passed for stage: Realtime`

- [ ] **Step 6: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 5: Redesign History Journal Page

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmHistoryView.xaml`

- [ ] **Step 1: Run history-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage History
```

Expected: command fails with `History page layout marker missing.`

- [ ] **Step 2: Replace the history root layout**

Replace the root grid with:

```xaml
    <Grid x:Name="HistoryJournalLayout" Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Margin="0,0,0,14" Style="{StaticResource AlarmCommandBarStyle}">
            <DockPanel>
                <TextBlock DockPanel.Dock="Right" VerticalAlignment="Center" Style="{StaticResource MutedTextStyle}" Text="{Binding HistoryResultText}" />
                <StackPanel>
                    <TextBlock Foreground="{StaticResource AlarmCyanBrush}" FontSize="12" FontWeight="Bold" Text="JOURNAL SEARCH" />
                    <TextBlock Margin="0,6,0,0" Style="{StaticResource SectionTitleStyle}" Text="历史报警查询" />
                    <TextBlock Margin="0,6,0,0" Style="{StaticResource MutedTextStyle}" Text="筛选条件与分页固定在表格外，长列表滚动时操作区保持稳定。" />
                </StackPanel>
            </DockPanel>
        </Border>

        <Grid Grid.Row="1">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <Border Grid.Row="0" Margin="0,0,0,14" Style="{StaticResource QueryBarStyle}">
                <!-- Keep the existing filter controls and commands here: FilterStartDate, FilterEndDate, LevelOptions, SourceOptions, HistoryKeyword, ApplyHistoryFilterCommand, ResetHistoryFilterCommand, ExportCsvCommand, ExportExcelCommand. -->
            </Border>

            <Border Grid.Row="1" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <Border Padding="16,14" BorderBrush="{StaticResource AlarmBorderBrush}" BorderThickness="0,0,0,1">
                        <DockPanel>
                            <TextBlock DockPanel.Dock="Right" VerticalAlignment="Center" Foreground="{StaticResource AlarmPrimaryBrush}" Text="{Binding HistoryPagerText}" />
                            <StackPanel>
                                <TextBlock Style="{StaticResource SectionTitleStyle}" Text="历史报警记录" />
                                <TextBlock Margin="0,4,0,0" Style="{StaticResource MutedTextStyle}" Text="按当前筛选条件分页加载，表格区域独立滚动。" />
                            </StackPanel>
                        </DockPanel>
                    </Border>

                    <DataGrid Grid.Row="1"
                              Margin="12"
                              Style="{StaticResource AlarmDataGridStyle}"
                              RowStyle="{StaticResource FlatRowStyle}"
                              CellStyle="{StaticResource FlatCellStyle}"
                              ColumnHeaderStyle="{StaticResource FlatColumnHeaderStyle}"
                              ItemsSource="{Binding HistoryAlarms}">
                        <!-- HistoryAlarms columns are the existing column block from this file. -->
                    </DataGrid>
                </Grid>
            </Border>

            <Border Grid.Row="2" Margin="0,14,0,0" Padding="12" Style="{StaticResource QueryBarStyle}">
                <DockPanel>
                    <TextBlock VerticalAlignment="Center" Style="{StaticResource MutedTextStyle}" Text="{Binding HistoryResultText}" />
                    <StackPanel DockPanel.Dock="Right" Orientation="Horizontal">
                        <Button Command="{Binding PreviousPageCommand}" Content="上一页" Style="{StaticResource AlarmSecondaryButtonStyle}" />
                        <TextBlock Margin="14,0" VerticalAlignment="Center" Foreground="{StaticResource AlarmTextBrush}" Text="{Binding HistoryPagerText}" />
                        <Button Command="{Binding NextPageCommand}" Content="下一页" Style="{StaticResource AlarmSecondaryButtonStyle}" />
                    </StackPanel>
                </DockPanel>
            </Border>
        </Grid>
    </Grid>
```

- [ ] **Step 3: Restore exact filter controls and table columns**

Paste the existing filter controls from the current history page into the filter target section, preserving every binding and command listed in the target section. Paste the existing history table columns into the DataGrid target section, preserving headers and bindings.

- [ ] **Step 4: Verify history stage**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage History
```

Expected: `AlarmCenter redesign source checks passed for stage: History`

- [ ] **Step 5: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 6: Redesign Statistics Analytics Page

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmStatisticsView.xaml`

- [ ] **Step 1: Run statistics-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Statistics
```

Expected: command fails with `Statistics page layout marker missing.`

- [ ] **Step 2: Replace statistic page root layout**

Replace the root grid with a named analytics layout:

```xaml
    <Grid x:Name="StatisticsAnalyticsLayout" Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <ItemsControl Grid.Row="0"
                      Margin="0,0,0,14"
                      ItemTemplate="{StaticResource AlarmMetricCardTemplate}"
                      ItemsSource="{Binding StatisticCards}">
            <ItemsControl.ItemsPanel>
                <ItemsPanelTemplate>
                    <WrapPanel ItemWidth="220" />
                </ItemsPanelTemplate>
            </ItemsControl.ItemsPanel>
        </ItemsControl>

        <Grid Grid.Row="1">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="3*" />
                <ColumnDefinition Width="16" />
                <ColumnDefinition Width="2*" />
            </Grid.ColumnDefinitions>
            <Grid.RowDefinitions>
                <RowDefinition Height="*" />
                <RowDefinition Height="16" />
                <RowDefinition Height="Auto" />
            </Grid.RowDefinitions>

            <Border Grid.Column="0" Grid.Row="0" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                <!-- Trend chart block uses the existing TrendChartView binding from this file. -->
            </Border>

            <Grid Grid.Column="2" Grid.Row="0">
                <Grid.RowDefinitions>
                    <RowDefinition Height="*" />
                    <RowDefinition Height="16" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>
                <Border Grid.Row="0" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                    <!-- Type chart block uses the existing TypeChartView and TypeStatistics bindings. -->
                </Border>
                <Border Grid.Row="2" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                    <!-- Source chart block uses the existing SourceChartView and SourceStatistics bindings. -->
                </Border>
            </Grid>

            <Border Grid.Column="0" Grid.ColumnSpan="3" Grid.Row="2" Padding="0" Style="{StaticResource AlarmContentCardStyle}">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="Auto" />
                    </Grid.RowDefinitions>
                    <Border Padding="16,14" BorderBrush="{StaticResource AlarmBorderBrush}" BorderThickness="0,0,0,1">
                        <StackPanel>
                            <TextBlock Style="{StaticResource SectionTitleStyle}" Text="趋势概览" />
                            <TextBlock Margin="0,4,0,0" Style="{StaticResource MutedTextStyle}" Text="以卡片形式保留趋势数据摘要，窗口缩放时自动换行。" />
                        </StackPanel>
                    </Border>
                    <ItemsControl Grid.Row="1" Margin="12" ItemTemplate="{StaticResource TrendTileTemplate}" ItemsSource="{Binding TrendPoints}">
                        <ItemsControl.ItemsPanel>
                            <ItemsPanelTemplate>
                                <WrapPanel />
                            </ItemsPanelTemplate>
                        </ItemsControl.ItemsPanel>
                    </ItemsControl>
                </Grid>
            </Border>
        </Grid>
    </Grid>
```

- [ ] **Step 3: Preserve the three chart bindings**

Move the existing chart sections into the named target sections and keep these exact bindings:

```xaml
ViewXY="{Binding TrendChartView}"
ViewXY="{Binding TypeChartView}"
ViewXY="{Binding SourceChartView}"
ItemsSource="{Binding TypeStatistics}"
ItemsSource="{Binding SourceStatistics}"
```

- [ ] **Step 4: Verify statistics stage**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Statistics
```

Expected: `AlarmCenter redesign source checks passed for stage: Statistics`

- [ ] **Step 5: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 7: Redesign Alarm Definitions Governance Page

**Files:**
- Modify: `Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml`

- [ ] **Step 1: Run definitions-stage check and confirm it fails**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Definitions
```

Expected: command fails with `Definitions page layout marker missing.`

- [ ] **Step 2: Keep editor overlays and replace the main page container**

In `AlarmDefinitionsView.xaml`, keep existing resource dictionary, all editor overlay bindings, and all command bindings. Replace the main visible layout root with:

```xaml
    <Grid x:Name="DefinitionsGovernanceLayout" Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Grid.Row="0" Margin="0,0,0,14" Style="{StaticResource AlarmCommandBarStyle}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>
                <StackPanel>
                    <TextBlock Foreground="{StaticResource AlarmCyanBrush}" FontSize="12" FontWeight="Bold" Text="GOVERNANCE" />
                    <TextBlock Margin="0,6,0,0" Style="{StaticResource SectionTitleStyle}" Text="报警定义 / 硬件规则管理" />
                    <TextBlock Margin="0,6,0,0" Style="{StaticResource MutedTextStyle}" Text="集中维护硬件报警定义、等级、启停、处理建议，并管理抑制、搁置、通知路由和事件审计。" />
                </StackPanel>
                <StackPanel Grid.Column="1" MinWidth="260" VerticalAlignment="Center">
                    <ProgressBar Height="6" Margin="0,0,0,8" IsIndeterminate="True" Visibility="{Binding IsBusy, Converter={StaticResource BooleanToVisibilityConverter}}" />
                    <TextBlock HorizontalAlignment="Right" Style="{StaticResource MutedTextStyle}" Text="{Binding StatusText}" />
                </StackPanel>
            </Grid>
        </Border>

        <Border Grid.Row="1" Style="{StaticResource AlarmContentCardStyle}" Padding="0">
            <TabControl>
                <!-- TabItems are the existing 报警定义, 报警抑制, 报警搁置, 通知路由, and 事件审计 tabs from this file. -->
            </TabControl>
        </Border>
    </Grid>
```

- [ ] **Step 3: Apply shared table and button styles to each tab**

For each DataGrid in the five tabs, set:

```xaml
Style="{StaticResource AlarmDataGridStyle}"
RowStyle="{StaticResource FlatRowStyle}"
CellStyle="{StaticResource FlatCellStyle}"
ColumnHeaderStyle="{StaticResource FlatColumnHeaderStyle}"
```

For each command button, keep the existing command and set either:

```xaml
Style="{StaticResource AlarmPrimaryButtonStyle}"
```

or:

```xaml
Style="{StaticResource AlarmSecondaryButtonStyle}"
```

- [ ] **Step 4: Preserve the current binding inventory**

Confirm the rewritten file still contains these bindings:

```xaml
ItemsSource="{Binding Definitions}"
SelectedItem="{Binding SelectedDefinition}"
ItemsSource="{Binding SuppressionRules}"
SelectedItem="{Binding SelectedSuppressionRule}"
ItemsSource="{Binding Shelves}"
SelectedItem="{Binding SelectedShelve}"
ItemsSource="{Binding NotificationRoutes}"
SelectedItem="{Binding SelectedNotificationRoute}"
ItemsSource="{Binding AuditItems}"
Text="{Binding DefinitionKeyword, UpdateSourceTrigger=PropertyChanged}"
Text="{Binding AuditKeyword, UpdateSourceTrigger=PropertyChanged}"
```

Confirm the rewritten file still contains these commands:

```xaml
Command="{Binding RefreshCommand}"
Command="{Binding NewDefinitionCommand}"
Command="{Binding EditDefinitionCommand}"
Command="{Binding ToggleDefinitionCommand}"
Command="{Binding NewSuppressionCommand}"
Command="{Binding EditSuppressionCommand}"
Command="{Binding ToggleSuppressionCommand}"
Command="{Binding NewShelveCommand}"
Command="{Binding EditShelveCommand}"
Command="{Binding ReleaseShelveCommand}"
Command="{Binding NewRouteCommand}"
Command="{Binding EditRouteCommand}"
Command="{Binding ToggleRouteCommand}"
Command="{Binding RefreshAuditCommand}"
```

- [ ] **Step 5: Verify definitions stage**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage Definitions
```

Expected: `AlarmCenter redesign source checks passed for stage: Definitions`

- [ ] **Step 6: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

## Task 8: Full Verification and Handoff

**Files:**
- Verify: all modified files from Tasks 1-7

- [ ] **Step 1: Run the full redesign source check**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 -Stage All
```

Expected: `AlarmCenter redesign source checks passed for stage: All`

- [ ] **Step 2: Run the existing AlarmCenter source check**

Run:

```powershell
.\Scratch\AlarmCenterChecks\Check-AlarmCenter.ps1
```

Expected: `AlarmCenter source checks passed.`

- [ ] **Step 3: Build AlarmCenter**

Run:

```powershell
dotnet build Application\ReeYin.AlarmCenter\ReeYin.AlarmCenter.csproj --no-restore
```

Expected: exit code `0`.

- [ ] **Step 4: Inspect changed file list**

Run:

```powershell
Get-ChildItem `
  Application\ReeYin.AlarmCenter\Styles\AlarmWorkbenchResources.xaml, `
  Application\ReeYin.AlarmCenter\Views\AlarmWorkbenchShellView.xaml, `
  Application\ReeYin.AlarmCenter\Views\AlarmRealtimeView.xaml, `
  Application\ReeYin.AlarmCenter\Views\AlarmHistoryView.xaml, `
  Application\ReeYin.AlarmCenter\Views\AlarmStatisticsView.xaml, `
  Application\ReeYin.AlarmCenter\Views\AlarmDefinitionsView.xaml, `
  Scratch\AlarmCenterChecks\Check-AlarmCenterRedesign.ps1 |
  Select-Object FullName,Length,LastWriteTime
```

Expected: seven rows, one for each redesigned or verification file.

- [ ] **Step 5: Manual visual smoke test**

Open the application path that loads `AlarmWorkbenchShellView`, then verify:

```text
1. Shell opens with the dark left rail.
2. The default page is realtime alarm monitoring.
3. Navigation switches realtime, history, statistics, and definitions.
4. KPI cards render SummaryCards values.
5. Realtime page shows ActiveAlarms, SelectedActiveAlarm details, ConfirmSelectedCommand, ClearSelectedCommand, and RealtimeFeed.
6. History page query, reset, CSV, Excel, previous page, and next page buttons are visible.
7. Statistics page renders TrendChartView, TypeChartView, SourceChartView, and TrendPoints.
8. Definitions page shows all five management areas and editor overlays still open from existing commands.
```

Expected: all eight visual checks pass without binding errors in the output window.
