# Line Segment Raw And Filtered CSV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Export both raw and filtered PreDatas CSV files for line-scan segments and line-scan summaries.

**Architecture:** Split line-scan preprocessing into raw and filtered datasets in `SensorDataCollectionModel`; store both through overloads in `SensorDataStorageService` using `_Filtered` suffix for processed files. Keep `PreDatas` and analysis behavior on the filtered dataset.

**Tech Stack:** C#/.NET 8 WPF module, existing console-style test project, existing `PreprocessDatasetFilter` and CSV storage service.

---

### Task 1: Red Tests For File Naming And Wiring

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Add tests asserting storage service exposes `Line_001_PreDatas_Filtered.csv` and `LineSegments_All_PreDatas_Filtered.csv` paths.
- [ ] Add tests asserting model keeps raw and filtered line-segment summary caches and calls both raw and filtered save methods.
- [ ] Run `dotnet run --no-restore --project CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`; expected red until implementation exists.

### Task 2: Storage Service Support

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Storage/SensorDataStorageService.cs`

- [ ] Add `GetLineSegmentFilteredPreDatasCsvPath` returning `Line_001_PreDatas_Filtered.csv`.
- [ ] Add `SaveLineSegmentFilteredPreDatasCsv` using the same export path.
- [ ] Add `SaveLineSegmentFilteredSummaryCsv` returning `LineSegments_All_PreDatas_Filtered.csv`.

### Task 3: Model Raw/Filtered Flow

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Models/SensorDataCollectionModel.cs`

- [ ] Add `_lineSegmentSummaryRawPreDatas` beside existing filtered summary cache.
- [ ] Split preprocessing into `BuildRawPreprocessDatas` and `BuildFilteredPreprocessDatas`.
- [ ] In line-scan stop flow, build raw and filtered datasets, assign filtered to `PreDatas`, and save both.
- [ ] On summary completion, save both summary CSVs and run summary analysis against the filtered summary CSV.

### Task 4: Verification

**Files:**
- No new production files.

- [ ] Run source-level tests/checks.
- [ ] Try `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj --no-restore`; if existing NuGet assets block it, report exact error.
