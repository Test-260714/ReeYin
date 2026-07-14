# 数据源级点采集首尾去除数量 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [x]`) syntax for tracking.

**Goal:** Move point-collection edge trim count into each data-source settings dialog and apply it per data source during aggregation.

**Architecture:** Persist the trim count on `DataAnalysisDataSourceOption`. Pass all data-source options into `SensorPointDataProcessingOptions`, then aggregate `OriginalDatas` with a per-original-key trim map while preserving the old fallback trim for unmatched keys and non-original fields.

**Tech Stack:** C#/.NET 8 WPF, Prism MVVM, HandyControl/Wx controls, ReeYin-V module patterns.

---

### Task 1: Processing Behavior

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/DataAnalysisModels.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorDataCollectionModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorPointDataProcessor.cs`

- [x] Write a failing reflection-based test that expects `DataAnalysisDataSourceOption.PointCollectionTrimCountPerSide` and verifies two OriginalData keys aggregate with different trim counts.
- [x] Run the test and verify it fails before production changes.
- [x] Add the data-source property and pass all configured data sources into processing options.
- [x] Change `AggregateMeasureDataGroup` to average each OriginalData key from a source-specific trimmed sample list when a configured source matches the key.
- [x] Run the reflection-based test and verify it passes.

### Task 2: Popup UI And ViewModel

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/ViewModels/DataAnalysisSourceFilterSettingViewModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/DataAnalysisSourceFilterSettingView.xaml`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Views/SensorDataCollectionView.xaml`

- [x] Add dialog ViewModel property `PointCollectionTrimCountPerSide`, load it from the selected source, and apply it on OK.
- [x] Add numeric input to the dialog and replace fixed dialog width/height with adaptive width, max height, and a scrollable content region.
- [x] Remove the global trim input from the data-analysis tab and update helper text to mention popup settings.

### Task 3: Verification

**Files:**
- Build: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [x] Run `dotnet build CustomizedDemand\Custom.WaferFlatnessMeasure\Custom.WaferFlatnessMeasure.csproj --no-restore -v:minimal /p:SolutionDir="E:\Company\工作目录\ReeYin-V\ReeYin-V\"`.
- [x] Confirm exit code `0` and report any warnings separately.
- [x] Remove temporary test harness files.

