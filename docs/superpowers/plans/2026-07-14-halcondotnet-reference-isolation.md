# HALCON .NET Reference Isolation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the unconditional and inconsistent HALCON .NET reference with a single opt-in `halcondotnetxl` 23.11.0.0 reference used only by verified consumers.

**Architecture:** `Directory.Build.props` remains the sole owner of the HALCON assembly path and Copy Local metadata, guarded by `ReeYinUseHalconDotNet=true`. Each verified consumer declares only that property; project-level HALCON `Reference` and compensating `Remove` items are eliminated. A repository PowerShell check proves the configuration fails before migration and validates the final MSBuild evaluation.

**Tech Stack:** MSBuild XML, .NET 8/Framework project evaluation, PowerShell 5.1+, HALCON .NET XL 23.11.0.0.

**Authorization note:** Repository rules require separate authorization for commits. All commit steps are intentionally replaced with review checkpoints; do not create commits, push, merge, publish, or deploy without later explicit authorization.

---

### Task 1: Add the failing repository dependency check

**Files:**
- Create: `scripts/verification/Test-HalconReferences.ps1`
- Reference: `Directory.Build.props`
- Reference: all non-generated `*.csproj`

- [ ] **Step 1: Create the dependency check before changing production configuration**

Create `scripts/verification/Test-HalconReferences.ps1` with these checks:

```powershell
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$propsPath = Join-Path $repositoryRoot 'Directory.Build.props'
$halconPath = Join-Path $repositoryRoot 'packages\A_ThirdParty\Halcon\halcondotnetxl.dll'
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$message) {
    $failures.Add($message)
}

function Get-ProjectRelativePath([System.IO.FileInfo]$project) {
    return $project.FullName.Substring($repositoryRoot.Length + 1)
}

function Get-HalconReferences($evaluation) {
    return @($evaluation.Items.Reference | Where-Object {
        $_.Identity -in @('halcondotnet', 'halcondotnetxl')
    })
}

[xml]$props = Get-Content -Raw -LiteralPath $propsPath
$centralReferences = @($props.SelectNodes("//*[local-name()='Reference']") | Where-Object {
    $_.Include -in @('halcondotnet', 'halcondotnetxl')
})

if ($centralReferences.Count -ne 1) {
    Add-Failure "Directory.Build.props must define exactly one HALCON reference."
} else {
    $central = $centralReferences[0]
    if ($central.Include -ne 'halcondotnetxl') {
        Add-Failure "Central HALCON reference identity must be halcondotnetxl."
    }
    if ($central.ParentNode.Condition -ne "'`$(ReeYinUseHalconDotNet)' == 'true'") {
        Add-Failure "Central HALCON reference must be opt-in."
    }
    if ($central.Private -ne 'true') {
        Add-Failure "Central HALCON reference must set Private=true."
    }
}

if (-not (Test-Path -LiteralPath $halconPath -PathType Leaf)) {
    Add-Failure "HALCON XL assembly is missing: $halconPath"
} else {
    $assembly = [Reflection.AssemblyName]::GetAssemblyName($halconPath)
    if ($assembly.Name -ne 'halcondotnetxl') {
        Add-Failure "HALCON file identity is $($assembly.Name), expected halcondotnetxl."
    }
    if ($assembly.Version.ToString() -ne '23.11.0.0') {
        Add-Failure "HALCON file version is $($assembly.Version), expected 23.11.0.0."
    }
}

$projects = @(Get-ChildItem -LiteralPath $repositoryRoot -Recurse -Filter '*.csproj' -File | Where-Object {
    $_.Name -notmatch '_wpftmp\.csproj$' -and
    $_.FullName -notmatch '[\\/](bin|obj|obj-[^\\/]+)[\\/]'
})

$projectByDirectory = @{}
$expectedConsumers = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$enabledProjects = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)

foreach ($project in $projects) {
    [xml]$projectXml = Get-Content -Raw -LiteralPath $project.FullName
    $relativePath = Get-ProjectRelativePath $project
    $projectByDirectory[$project.DirectoryName] = $relativePath
    $localReferences = @($projectXml.SelectNodes("//*[local-name()='Reference']") | Where-Object {
        $_.Include -in @('halcondotnet', 'halcondotnetxl') -or
        $_.Remove -in @('halcondotnet', 'halcondotnetxl')
    })
    if ($localReferences.Count -gt 0) {
        Add-Failure "$relativePath contains project-level HALCON Reference items."
    }

    $enabledNodes = @($projectXml.SelectNodes("//*[local-name()='ReeYinUseHalconDotNet']"))
    if ($enabledNodes.Count -gt 1) {
        Add-Failure "$relativePath declares ReeYinUseHalconDotNet more than once."
    }
    if ($enabledNodes.Count -eq 1 -and $enabledNodes[0].InnerText -eq 'true') {
        [void]$enabledProjects.Add($relativePath)
    }

    $imageToolReferences = @($projectXml.SelectNodes("//*[local-name()='ProjectReference']") | Where-Object {
        $_.Include -match 'ImageTool\.Halcon[\\/]ImageTool\.Halcon\.csproj$'
    })
    if ($imageToolReferences.Count -gt 0) {
        [void]$expectedConsumers.Add($relativePath)
    }
}

$usageFiles = @(& rg -l --glob '*.cs' --glob '*.xaml' '\bHalconDotNet\b' $repositoryRoot)
if ($LASTEXITCODE -notin @(0, 1)) {
    Add-Failure "Failed to scan HALCON source usage with rg."
}
foreach ($usageFile in $usageFiles) {
    $directory = Split-Path -Parent $usageFile
    $ownerFound = $false
    while ($directory.StartsWith($repositoryRoot, [StringComparison]::OrdinalIgnoreCase)) {
        if ($projectByDirectory.ContainsKey($directory)) {
            [void]$expectedConsumers.Add($projectByDirectory[$directory])
            $ownerFound = $true
            break
        }
        $parent = Split-Path -Parent $directory
        if ($parent -eq $directory) {
            break
        }
        $directory = $parent
    }
    if (-not $ownerFound) {
        Add-Failure "HALCON usage has no owning project: $usageFile"
    }
}

# Basler has a legacy direct dependency without textual HalconDotNet usage.
[void]$expectedConsumers.Add(
    'Hardware\Camera\ReeYin_V.Hardware.Camera.Basler\ReeYin_V.Hardware.Cam.Basler.csproj')

foreach ($expected in $expectedConsumers) {
    if (-not $enabledProjects.Contains($expected)) {
        Add-Failure "$expected must enable ReeYinUseHalconDotNet."
    }
}
foreach ($enabled in $enabledProjects) {
    if (-not $expectedConsumers.Contains($enabled)) {
        Add-Failure "$enabled enables HALCON without consumer evidence."
    }
}

$enabledCount = $enabledProjects.Count
if ($expectedConsumers.Count -ne 67 -or $enabledCount -ne 67) {
    Add-Failure "Expected 67 HALCON consumers; evidence found $($expectedConsumers.Count), opt-in found $enabledCount."
}

$evaluationCases = [ordered]@{
    'Core\ReeYin_V.Logger\ReeYin_V.Logger.csproj' = 0
    'Tools\PointCloud\PointCloud.Algorithms\PointCloud.Algorithms.csproj' = 0
    'Core\ReeYin-V.Core\ReeYin_V.Core.csproj' = 1
    'Tools\Image\ImageTool.Halcon\ImageTool.Halcon.csproj' = 1
    'CustomizedDemand\Custom.XYHD\Custom.XYHD.csproj' = 1
    'Hardware\Camera\ReeYin_V.Hardware.Camera.Basler\ReeYin_V.Hardware.Cam.Basler.csproj' = 1
}

foreach ($entry in $evaluationCases.GetEnumerator()) {
    $projectPath = Join-Path $repositoryRoot $entry.Key
    $output = & dotnet msbuild $projectPath -nologo `
        -getProperty:ReeYinUseHalconDotNet -getItem:Reference 2>&1
    if ($LASTEXITCODE -ne 0) {
        Add-Failure "MSBuild evaluation failed for $($entry.Key): $($output -join ' ')"
        continue
    }
    $evaluation = ($output -join [Environment]::NewLine) | ConvertFrom-Json
    $references = @(Get-HalconReferences $evaluation)
    if ($references.Count -ne $entry.Value) {
        Add-Failure "$($entry.Key) has $($references.Count) HALCON references; expected $($entry.Value)."
    }
    if ($references.Count -eq 1) {
        if ($references[0].Identity -ne 'halcondotnetxl') {
            Add-Failure "$($entry.Key) resolves $($references[0].Identity), expected halcondotnetxl."
        }
        if ([IO.Path]::GetFullPath($references[0].HintPath) -ne [IO.Path]::GetFullPath($halconPath)) {
            Add-Failure "$($entry.Key) resolves an unexpected HALCON path."
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    throw "HALCON reference validation failed with $($failures.Count) issue(s)."
}

Write-Host "HALCON reference validation passed for $($projects.Count) projects and $enabledCount consumers."
```

- [ ] **Step 2: Run the check and verify RED**

Run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verification\Test-HalconReferences.ps1
```

Expected: exit code `1`. Failures must include the unconditional/misnamed central reference, project-level HALCON references, and `Expected 67 HALCON consumers, found 0`. A syntax or path error is not an acceptable RED result.

- [ ] **Step 3: Record the RED checkpoint without committing**

Record the exact command, time, Windows/.NET/PowerShell versions, exit code, issue count, and representative failure messages. Do not modify the test to accept current behavior.

### Task 2: Make the root HALCON reference opt-in and identity-correct

**Files:**
- Modify: `Directory.Build.props:14-18`

- [ ] **Step 1: Replace the unconditional reference**

Use exactly:

```xml
<ItemGroup Condition="'$(ReeYinUseHalconDotNet)' == 'true'">
  <Reference Include="halcondotnetxl">
    <HintPath>$(ReeYinRepositoryRoot)packages\A_ThirdParty\Halcon\halcondotnetxl.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

- [ ] **Step 2: Verify a non-consumer no longer inherits HALCON**

Run:

```powershell
dotnet msbuild Core\ReeYin_V.Logger\ReeYin_V.Logger.csproj -nologo -getProperty:ReeYinUseHalconDotNet -getItem:Reference
```

Expected: `ReeYinUseHalconDotNet` is empty and the `Reference` array contains no HALCON identity.

- [ ] **Step 3: Run the repository check and confirm it remains RED for migration-only reasons**

Expected: exit code `1`; the central-reference failures disappear, while consumer-count and project-level-reference failures remain.

### Task 3: Migrate Application, Core, Shell, and shared UI consumers

**Files:**
- Modify: `Application/NodifyFlow/Nodify.FlowApp/Nodify.FlowApp.csproj`
- Modify: `Application/ReeYin_V.Share/ReeYin_V.Share.csproj`
- Modify: `Core/ReeYin_V.UI/ReeYin_V.UI.csproj`
- Modify: `Core/ReeYin-V.Core/ReeYin_V.Core.csproj`
- Modify: `GemeralUI/ReeYin.ChartShow/ReeYin.ChartShow.csproj`
- Modify: `Shell/ReeYin_V.Shell.csproj`

- [ ] **Step 1: Add the opt-in property once to each file**

Add this member to the primary unconditional `PropertyGroup`:

```xml
<ReeYinUseHalconDotNet>true</ReeYinUseHalconDotNet>
```

- [ ] **Step 2: Remove legacy direct HALCON blocks**

Remove the full `Reference` element whose `Include` is `halcondotnet` or
`halcondotnetxl` from `Application/ReeYin_V.Share/ReeYin_V.Share.csproj`.
Do not remove `ProjectReference` entries or unrelated assembly references.

- [ ] **Step 3: Evaluate representative projects**

Run `dotnet msbuild ... -getProperty:ReeYinUseHalconDotNet -getItem:Reference`
for `ReeYin_V.Share.csproj`, `ReeYin_V.Core.csproj`, and `ReeYin_V.Shell.csproj`.
Expected: each property is `true` and each project has exactly one
`halcondotnetxl` reference from `Directory.Build.props`.

### Task 4: Migrate Tools consumers and remove PointCloud compensation

**Files:**
- Modify: `Tools/CSharpScript/ReeYin.CSharpScript/ReeYin.CSharpScript.csproj`
- Modify: `Tools/GeneralALGO/ALGO.BlobAnalysis/ALGO.BlobAnalysis.csproj`
- Modify: `Tools/GeneralALGO/ALGO.Calibration/ALGO.Calibration.csproj`
- Modify: `Tools/GeneralALGO/ALGO.CreatRegion/ALGO.CreatRegion.csproj`
- Modify: `Tools/GeneralALGO/ALGO.DeepLearning/ALGO.DeepLearning.csproj`
- Modify: `Tools/GeneralALGO/ALGO.DefectPostProcess/ALGO.DefectPostProcess.csproj`
- Modify: `Tools/GeneralALGO/ALGO.DistanceLL/ALGO.DistanceLL.csproj`
- Modify: `Tools/GeneralALGO/ALGO.DistancePL/ALGO.DistancePL.csproj`
- Modify: `Tools/GeneralALGO/ALGO.DistancePP/ALGO.DistancePP.csproj`
- Modify: `Tools/GeneralALGO/ALGO.FilterRegion/ALGO.FilterRegion.csproj`
- Modify: `Tools/GeneralALGO/ALGO.FindCode/ALGO.FindCode.csproj`
- Modify: `Tools/GeneralALGO/ALGO.ImageOperation/ALGO.ImageOperation.csproj`
- Modify: `Tools/GeneralALGO/ALGO.ImagePerProcessing/ALGO.ImagePerProcessing.csproj`
- Modify: `Tools/GeneralALGO/ALGO.LineScanSheetCounter/ALGO.LineScanSheetCounter.csproj`
- Modify: `Tools/GeneralALGO/ALGO.MeasureCircle/ALGO.MeasureCircle.csproj`
- Modify: `Tools/GeneralALGO/ALGO.MeasureLine/ALGO.MeasureLine.csproj`
- Modify: `Tools/GeneralALGO/ALGO.MeasureRect/ALGO.MeasureRect.csproj`
- Modify: `Tools/GeneralALGO/ALGO.MultiCameraCalibration/ALGO.MultiCameraCalibration.csproj`
- Modify: `Tools/GeneralALGO/ALGO.NinePointCalibration/ALGO.NinePointCalibration.csproj`
- Modify: `Tools/GeneralALGO/ALGO.NPointCalibration/ALGO.NPointCalibration.csproj`
- Modify: `Tools/GeneralALGO/ALGO.RegionProcessing/ALGO.RegionProcessing.csproj`
- Modify: `Tools/GeneralALGO/ALGO.RegionTrans/ALGO.RegionTrans.csproj`
- Modify: `Tools/GeneralALGO/ALGO.ShapeMatching/ALGO.ShapeMatching.csproj`
- Modify: `Tools/Image/ImageTool.GrabImage/ImageTool.GrabImage.csproj`
- Modify: `Tools/Image/ImageTool.Halcon/ImageTool.Halcon.csproj`
- Modify: `Tools/Image/ImageTool.HalconExtension/ImageTool.HalconExtension.csproj`
- Modify: `Tools/Image/ImageTool.SaveImage/ImageTool.SaveImage.csproj`
- Modify: `Tools/Image/ImageTool.VTKPCDisplay/ImageTool.VTKPCDisplay.csproj`
- Modify: `Tools/Logical/LogicalTool.ParamLink/LogicalTool.ParamLink.csproj`
- Modify: `Tools/PointCloud/PointCloud.Algorithms/PointCloud.Algorithms.csproj`
- Modify: `Tools/PointCloud/PointCloud.Interop/PointCloud.Interop.csproj`
- Modify: `Tools/PointCloud/PointCloud.ToolViewer/PointCloud.ToolViewer.csproj`
- Modify: `Tools/PointCloud/PointCloud.VTKWPF/PointCloud.VTKWPF.csproj`

- [ ] **Step 1: Opt in the 29 Tools consumers**

Add `<ReeYinUseHalconDotNet>true</ReeYinUseHalconDotNet>` once to every Tools
consumer listed above, excluding the four PointCloud projects.

- [ ] **Step 2: Remove all Tools project-level HALCON Include blocks**

For every listed Tools consumer, remove complete `Reference` elements with
`Include="halcondotnet"` or `Include="halcondotnetxl"`. Preserve all
`ProjectReference`, package, framework, native-copy, and post-build items.

- [ ] **Step 3: Remove PointCloud compensation**

From each of the four PointCloud files, remove only:

```xml
<Reference Remove="halcondotnet" />
```

Remove an `ItemGroup` as well only when it becomes empty.

- [ ] **Step 4: Evaluate enabled and disabled representatives**

Evaluate `ImageTool.Halcon.csproj`, `ALGO.ShapeMatching.csproj`, and
`PointCloud.Algorithms.csproj`. Expected HALCON reference counts: `1`, `1`, and
`0`, respectively.

### Task 5: Migrate Hardware consumers

**Files:**
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.Basler/ReeYin_V.Hardware.Cam.Basler.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.DaHeng/ReeYin_V.Hardware.Camera.DaHeng.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.Dalsa/ReeYin_V.Hardware.Camera.Dalsa.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.HIK/ReeYin_V.Hardware.Camera.HIK.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.IKap/ReeYin_V.Hardware.Camera.IKap.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera.Mind/ReeYin_V.Hardware.Camera.Mind.csproj`
- Modify: `Hardware/Camera/ReeYin_V.Hardware.Camera/ReeYin_V.Hardware.Camera.csproj`
- Modify: `Hardware/PLC/ReeYin_V.Hardware.PLC/ReeYin_V.Hardware.PLC.csproj`
- Modify: `Hardware/Sensor/ReeYin.Hardware.Sensor.ChroCodile/ReeYin.Hardware.Sensor.ChroCodile.csproj`
- Modify: `Hardware/Sensor/ReeYin.Hardware.Sensor.JingCe/ReeYin.Hardware.Sensor.JingCe.csproj`
- Modify: `Hardware/Sensor/ReeYin.Hardware.Sensor.JueXin_LineSpectrum/ReeYin.Hardware.Sensor.JueXin_LineSpectrum.csproj`
- Modify: `Hardware/Sensor/ReeYin.Hardware.Sensor.MEGAPHASE/ReeYin.Hardware.Sensor.MEGAPHASE.csproj`
- Modify: `Hardware/Sensor/ReeYin.Hardware.Sensor.SSZN/ReeYin.Hardware.Sensor.SSZN.csproj`

- [ ] **Step 1: Add the opt-in property to all 13 Hardware consumers**

Use the same single `ReeYinUseHalconDotNet` property in each primary
unconditional `PropertyGroup`.

- [ ] **Step 2: Remove direct HALCON Include blocks from camera projects**

Remove only complete `halcondotnet`/`halcondotnetxl` `Reference` elements. Do
not change vendor SDK references, target architecture, or copy rules.

- [ ] **Step 3: Evaluate the Basler and base Camera projects**

Expected: both opt in and resolve exactly one `halcondotnetxl` reference from
the root props file. No device connection or initialization is authorized.

### Task 6: Migrate CustomizedDemand consumers

**Files:**
- Modify: `CustomizedDemand/bac2/新建文件夹/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Modify: `CustomizedDemand/Custom.CalibrationPlateMeasure/Custom.CalibrationPlateMeasure.csproj`
- Modify: `CustomizedDemand/Custom.DefectOverview/Custom.DefectOverview.csproj`
- Modify: `CustomizedDemand/Custom.ElectroStaticChuck/Custom.ElectroStaticChuck.csproj`
- Modify: `CustomizedDemand/Custom.EVEMFDJC/Custom.EVEMFDJC.csproj`
- Modify: `CustomizedDemand/Custom.HeightDifference/Custom.HeightDifference.csproj`
- Modify: `CustomizedDemand/Custom.KBTBox/Custom.KBTBox.csproj`
- Modify: `CustomizedDemand/Custom.KBTScraper/Custom.KBTScraper.csproj`
- Modify: `CustomizedDemand/Custom.KCJC/Custom.KCJC.csproj`
- Modify: `CustomizedDemand/Custom.LineScan/Custom.LineScan.csproj`
- Modify: `CustomizedDemand/Custom.MFDJC/Custom.MFDJC.csproj`
- Modify: `CustomizedDemand/Custom.PhysicalScale/Custom.PhysicalScale.csproj`
- Modify: `CustomizedDemand/Custom.PlcImageCollect/Custom.PlcImageCollect.csproj`
- Modify: `CustomizedDemand/Custom.Semiconductor/Custom.Semiconductor.csproj`
- Modify: `CustomizedDemand/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Modify: `CustomizedDemand/Custom.WaferRoutePlan/Custom.WaferRoutePlan.csproj`
- Modify: `CustomizedDemand/Custom.XYHD/Custom.XYHD.csproj`
- Modify: `CustomizedDemand/新建文件夹 (2)/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`
- Modify: `CustomizedDemand/新建文件夹/Custom.WaferFlatnessMeasure/Custom.WaferFlatnessMeasure.csproj`

- [ ] **Step 1: Add the opt-in property to all 19 listed projects**

Use the same property and do not change target frameworks, business module
metadata, or output-copy behavior.

- [ ] **Step 2: Remove direct HALCON Include blocks**

Remove complete direct `halcondotnet`/`halcondotnetxl` references from the
listed projects. Do not touch other third-party references.

- [ ] **Step 3: Evaluate active and archival representatives**

Evaluate `Custom.XYHD.csproj`, the active `Custom.WaferFlatnessMeasure.csproj`,
and the `bac2` project. Expected: each resolves exactly one canonical XL
reference with an existing path.

### Task 7: Verify GREEN and inspect the dependency diff

**Files:**
- Verify: `Directory.Build.props`
- Verify: `scripts/verification/Test-HalconReferences.ps1`
- Verify: all modified `*.csproj`

- [ ] **Step 1: Run the repository dependency check**

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\verification\Test-HalconReferences.ps1
```

Expected: exit code `0` and a message reporting 67 consumers. Errors: `0`;
failures: `0`; skipped: not applicable.

- [ ] **Step 2: Search for forbidden legacy configuration**

```powershell
rg -n -i --glob '*.csproj' --glob 'Directory.Build.props' `
  'Reference (Include|Remove)="halcondotnet|packages[\\/]Halcon[\\/]halcondotnet|packages[\\/]halcondotnet' .
```

Expected: only the conditional canonical `halcondotnetxl` reference in
`Directory.Build.props`; no project-level match and no missing legacy path.

- [ ] **Step 3: Check exact diff scope**

Run:

```powershell
git diff --check
git status --short
git diff -- Directory.Build.props scripts/verification '*.csproj'
```

Expected: only the approved root config, validation script, design/plan docs,
and listed project files. No `bin`, `obj`, `_wpftmp`, package, generated, or
business-code changes.

### Task 8: Run layered builds and consumer verification

**Files:**
- Build: `Core/ReeYin_V.Logger/ReeYin_V.Logger.csproj`
- Build: `Tools/PointCloud/PointCloud.Algorithms/PointCloud.Algorithms.csproj`
- Build: `Tools/Image/ImageTool.Halcon/ImageTool.Halcon.csproj`
- Build: `Core/ReeYin-V.Core/ReeYin_V.Core.csproj`
- Build: `CustomizedDemand/Custom.XYHD/Custom.XYHD.csproj`
- Build: `Shell/ReeYin_V.Shell.csproj`
- Build: `ReeYin.sln`

- [ ] **Step 1: Restore only when assets are missing**

Run targeted `dotnet restore <project>` commands for the listed SDK projects.
If restore requires network access, obtain network approval; do not alter
package sources, disable TLS, or copy cached asset files between projects.

- [ ] **Step 2: Build the non-consumer representatives**

```powershell
dotnet build Core\ReeYin_V.Logger\ReeYin_V.Logger.csproj --no-restore --verbosity:minimal
dotnet build Tools\PointCloud\PointCloud.Algorithms\PointCloud.Algorithms.csproj --no-restore --verbosity:minimal -p:SkipCopyPointCloudRuntimeToOutput=true
```

Expected: exit code `0`, zero HALCON reference warnings, zero errors.

- [ ] **Step 3: Build HALCON and direct consumers**

```powershell
dotnet build Tools\Image\ImageTool.Halcon\ImageTool.Halcon.csproj --no-restore --verbosity:minimal
dotnet build Core\ReeYin-V.Core\ReeYin_V.Core.csproj --no-restore --verbosity:minimal
dotnet build CustomizedDemand\Custom.XYHD\Custom.XYHD.csproj --no-restore --verbosity:minimal
dotnet build Shell\ReeYin_V.Shell.csproj --no-restore --verbosity:minimal
```

Expected: exit code `0`, exactly one resolved `halcondotnetxl` dependency per
consumer, and no `halcondotnet` missing/conflict warning.

- [ ] **Step 4: Build the solution when the local VS/native toolchain permits**

```powershell
dotnet build ReeYin.sln --no-restore --verbosity:minimal
```

Record the exact outcome. If native VC++ projects require full Visual Studio
MSBuild or dependencies are unavailable, mark solution verification as
`未验证` with the precise blocker; do not represent partial project builds as a
solution pass.

- [ ] **Step 5: Record the human runtime verification requirement**

A human with the licensed HALCON 23.11 runtime must verify a clean output
application start and one HALCON module load. Record environment, steps,
expected/actual result, executor, time, and logs. No camera or other real device
operation is part of this check.

### Task 9: Final self-review and delivery evidence

**Files:**
- Review: all task files
- Reference: `docs/superpowers/specs/2026-07-14-halcondotnet-reference-isolation-design.md`

- [ ] **Step 1: Reconcile implementation with the approved design**

Confirm the exact 67-project count, canonical assembly identity/version,
default-off behavior, PointCloud behavior, runtime-copy metadata, non-goals,
and rollback instructions.

- [ ] **Step 2: Classify all verification results**

Separate fresh successes, current failures, historical failures, skipped work,
and unverified items. Include exact command, time zone, OS, SDK/MSBuild versions,
exit code, error/failure/skipped counts, and evidence scope.

- [ ] **Step 3: Prepare the R2 handoff**

Report modified files and rationale, consumer compatibility, build and runtime
evidence, unverified items, rollback by reverting the root/project config, and
residual risk. Request HALCON/module-loading domain review. Do not claim release
readiness and do not commit or push without separate authorization.
