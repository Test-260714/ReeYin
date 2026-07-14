# Wafer Point Temperature CSV Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Record eight mapped PLC float temperatures for each wafer point into one CSV row per point.

**Architecture:** Add a small point-temperature storage service for address mapping and CSV writing, then integrate it into SensorDataCollectionModel after point PreDatas are produced. Keep PLC access isolated in SensorDataCollectionModel so the storage service can be tested without hardware.

**Tech Stack:** C# net8.0-windows, existing ReeYin PLC models, existing console test project.

---

### Task 1: Test Point Temperature CSV Service

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Program.cs`
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/PointTemperatureStorageService.cs`

- [ ] Add a failing test that writes two point rows and checks columns PointIndex, X, Y, D160, D162, D164, D166, D170, D172, D174, D176.
- [ ] Add a failing test that verifies each point reads all eight addresses.
- [ ] Run the test project and verify it fails before implementation.

### Task 2: Implement Point Temperature CSV Service

**Files:**
- Create: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/PointTemperatureStorageService.cs`

- [ ] Add PointTemperatureRecord with a per-address temperature dictionary.
- [ ] Add ordered default address mapping D160/D162/D164/D166/D170/D172/D174/D176.
- [ ] Add BuildRecords and SavePointTemperaturesCsv methods.
- [ ] Run the test project and verify it passes.

### Task 3: Integrate PLC Reads Into Point Collection

**Files:**
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Models/SensorDataCollectionModel.cs`

- [ ] Add LastPointTemperatureCsvPath output property.
- [ ] Resolve the first PLC from ConfigKey.PLCConfig.
- [ ] For each point row, read each address as EnumParaInfoModelParaType.Float.
- [ ] Save point temperature CSV after point PreDatas are updated.
- [ ] Clear LastPointTemperatureCsvPath when starting/resetting collection or running line mode.

### Task 4: Verify

**Files:**
- Test: `CustomizedDemand/Custom.WaferFlatnessMeasure.Tests/Custom.WaferFlatnessMeasure.Tests.csproj`
- Build: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [ ] Run the test project.
- [ ] Build Custom.WaferFlatnessMeasure.
- [ ] Report any build warnings/errors that are unrelated or pre-existing.
