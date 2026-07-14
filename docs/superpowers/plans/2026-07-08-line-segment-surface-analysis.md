# Line Segment Surface Analysis Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Calculate selected upper/lower surface metrics from line-scan `LineSegments_All_PreDatas.csv`.

**Architecture:** Add a line-segment summary analysis path in `SensorDataCollectionModel` that reuses existing algorithm helpers with fixed upper/lower surface data sources. Keep point-mode `RunALGO()` behavior unchanged.

**Tech Stack:** C#/.NET 8 WPF module, existing console-style test project, existing `Flatness_Algorithm`.

---

### Task 1: Add Red Tests

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`

- [ ] Add tests that inspect source behavior for line-segment summary analysis trigger and fixed upper/lower surface usage.
- [ ] Run `dotnet run --project CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj` and confirm failure.

### Task 2: Implement Line Summary Analysis Entry

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Models/SensorDataCollectionModel.cs`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Acquisition/PointSpectrum/Processing/RunALGO.cs`

- [ ] Add `RunLineSegmentSummaryAnalysis(...)` after summary CSV save.
- [ ] Ensure the method sets `LastPreDatasCsvPath` to the summary CSV, updates `PreDatas`, runs selected algorithms on fixed upper/lower surfaces, appends results, and refreshes outputs.
- [ ] Keep `RunALGO()` point-mode gate intact for live point acquisition.

### Task 3: Verify

**Files:**
- No new files.

- [ ] Run the test project.
- [ ] Run `dotnet build CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj --no-restore`.
