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

function Get-SimpleAssemblyName([string]$identity) {
    if ([string]::IsNullOrWhiteSpace($identity)) {
        return ''
    }

    $separatorIndex = $identity.IndexOf(',')
    if ($separatorIndex -ge 0) {
        $identity = $identity.Substring(0, $separatorIndex)
    }
    return $identity.Trim()
}

function Get-ObjectProperty($instance, [string]$name) {
    if ($null -eq $instance) {
        return $null
    }
    return $instance.PSObject.Properties[$name]
}

function Get-HalconReferences($referenceItems, [string]$projectRelativePath) {
    $references = [System.Collections.Generic.List[object]]::new()
    foreach ($referenceItem in @($referenceItems)) {
        $identityProperty = Get-ObjectProperty $referenceItem 'Identity'
        if ($null -eq $identityProperty -or
            [string]::IsNullOrWhiteSpace([string]$identityProperty.Value)) {
            Add-Failure "$projectRelativePath evaluation contains a Reference item without Identity metadata."
            continue
        }
        if ((Get-SimpleAssemblyName ([string]$identityProperty.Value)) -in
            @('halcondotnet', 'halcondotnetxl')) {
            [void]$references.Add($referenceItem)
        }
    }
    return $references
}

[xml]$props = Get-Content -Raw -LiteralPath $propsPath
$centralReferences = @($props.SelectNodes("//*[local-name()='Reference']") | Where-Object {
    (Get-SimpleAssemblyName $_.GetAttribute('Include')) -in @('halcondotnet', 'halcondotnetxl')
})

if ($centralReferences.Count -ne 1) {
    Add-Failure "Directory.Build.props must define exactly one HALCON reference."
} else {
    $central = $centralReferences[0]
    if ($central.GetAttribute('Include') -cne 'halcondotnetxl') {
        Add-Failure "Central HALCON reference identity must be halcondotnetxl."
    }
    if ($central.ParentNode.GetAttribute('Condition') -ne "'`$(ReeYinUseHalconDotNet)' == 'true'") {
        Add-Failure "Central HALCON reference must be opt-in."
    }
    $privateNode = $central.SelectSingleNode("*[local-name()='Private']")
    if ($null -eq $privateNode -or $privateNode.InnerText -ne 'true') {
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
        (Get-SimpleAssemblyName $_.GetAttribute('Include')) -in @('halcondotnet', 'halcondotnetxl') -or
        (Get-SimpleAssemblyName $_.GetAttribute('Remove')) -in @('halcondotnet', 'halcondotnetxl') -or
        (Get-SimpleAssemblyName $_.GetAttribute('Update')) -in @('halcondotnet', 'halcondotnetxl')
    })
    if ($localReferences.Count -gt 0) {
        Add-Failure "$relativePath contains project-level HALCON Reference items."
    }

    $enabledNodes = @($projectXml.SelectNodes("//*[local-name()='ReeYinUseHalconDotNet']"))
    if ($enabledNodes.Count -gt 1) {
        Add-Failure "$relativePath declares ReeYinUseHalconDotNet more than once."
    }
    if ($enabledNodes.Count -eq 1) {
        $enabledNode = $enabledNodes[0]
        $propertyGroup = $enabledNode.ParentNode
        $isValidEnabledNode = $true
        $isDirectPropertyGroup =
            $propertyGroup -is [System.Xml.XmlElement] -and
            $propertyGroup.LocalName -ceq 'PropertyGroup' -and
            [object]::ReferenceEquals($propertyGroup.ParentNode, $projectXml.DocumentElement)

        if ($enabledNode.InnerText -cne 'true') {
            Add-Failure "$relativePath ReeYinUseHalconDotNet value must be exactly lowercase true."
            $isValidEnabledNode = $false
        }
        if (-not $isDirectPropertyGroup) {
            Add-Failure "$relativePath ReeYinUseHalconDotNet must be a direct child of a project PropertyGroup."
            $isValidEnabledNode = $false
        }
        if ($enabledNode.GetAttribute('Condition') -ne '') {
            Add-Failure "$relativePath ReeYinUseHalconDotNet must not have a Condition."
            $isValidEnabledNode = $false
        }
        if ($propertyGroup -is [System.Xml.XmlElement] -and
            $propertyGroup.LocalName -ceq 'PropertyGroup' -and
            $propertyGroup.GetAttribute('Condition') -ne '') {
            Add-Failure "$relativePath ReeYinUseHalconDotNet PropertyGroup must not have a Condition."
            $isValidEnabledNode = $false
        }
        if ($isValidEnabledNode) {
            [void]$enabledProjects.Add($relativePath)
        }
    }

    $imageToolReferences = @($projectXml.SelectNodes("//*[local-name()='ProjectReference']") | Where-Object {
        $_.GetAttribute('Include') -match 'ImageTool\.Halcon[\\/]ImageTool\.Halcon\.csproj$'
    })
    if ($imageToolReferences.Count -gt 0) {
        [void]$expectedConsumers.Add($relativePath)
    }
}

$usageFiles = @()
$rgCommand = Get-Command rg -CommandType Application -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($null -eq $rgCommand) {
    Add-Failure "Required tool is missing: rg."
} else {
    $usageFiles = @(& $rgCommand.Source -l --glob '*.cs' --glob '*.xaml' '\bHalconDotNet\b' $repositoryRoot)
    if ($LASTEXITCODE -notin @(0, 1)) {
        Add-Failure "Failed to scan HALCON source usage with rg."
        $usageFiles = @()
    }
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

$evaluationCases = [System.Collections.Generic.Dictionary[string, int]]::new(
    [System.StringComparer]::OrdinalIgnoreCase)
$evaluationCases.Add('Core\ReeYin_V.Logger\ReeYin_V.Logger.csproj', 0)
$evaluationCases.Add('Tools\PointCloud\PointCloud.Algorithms\PointCloud.Algorithms.csproj', 0)
$evaluationCases.Add('Core\ReeYin-V.Core\ReeYin_V.Core.csproj', 1)
$evaluationCases.Add('Tools\Image\ImageTool.Halcon\ImageTool.Halcon.csproj', 1)
$evaluationCases.Add('CustomizedDemand\Custom.XYHD\Custom.XYHD.csproj', 1)
$evaluationCases.Add(
    'Hardware\Camera\ReeYin_V.Hardware.Camera.Basler\ReeYin_V.Hardware.Cam.Basler.csproj', 1)

$enabledProjectPaths = [string[]]@($enabledProjects)
[Array]::Sort($enabledProjectPaths, [System.StringComparer]::Ordinal)
foreach ($enabled in $enabledProjectPaths) {
    if ($evaluationCases.ContainsKey($enabled)) {
        if ($evaluationCases[$enabled] -ne 1) {
            Add-Failure "$enabled is enabled but is a fixed non-consumer evaluation case."
        }
    } else {
        $evaluationCases.Add($enabled, 1)
    }
}

$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue |
    Select-Object -First 1
if ($null -eq $dotnetCommand) {
    Add-Failure "Required tool is missing: dotnet."
} else {
    $evaluationProjectPaths = [string[]]@($evaluationCases.Keys)
    [Array]::Sort($evaluationProjectPaths, [System.StringComparer]::Ordinal)
    foreach ($projectRelativePath in $evaluationProjectPaths) {
        $expectedReferenceCount = $evaluationCases[$projectRelativePath]
        $projectPath = Join-Path $repositoryRoot $projectRelativePath
        $output = & $dotnetCommand.Source msbuild $projectPath -nologo `
            -getProperty:ReeYinUseHalconDotNet -getItem:Reference 2>&1
        if ($LASTEXITCODE -ne 0) {
            Add-Failure "MSBuild evaluation failed for $projectRelativePath`: $($output -join ' ')"
            continue
        }

        $jsonText = $output -join [Environment]::NewLine
        if ([string]::IsNullOrWhiteSpace($jsonText)) {
            Add-Failure "MSBuild evaluation returned no JSON for $projectRelativePath."
            continue
        }
        try {
            $evaluation = $jsonText | ConvertFrom-Json -ErrorAction Stop
        } catch {
            Add-Failure "MSBuild evaluation returned invalid JSON for $projectRelativePath`: $($_.Exception.Message)"
            continue
        }
        if ($null -eq $evaluation) {
            Add-Failure "MSBuild evaluation returned null JSON for $projectRelativePath."
            continue
        }

        $propertiesProperty = Get-ObjectProperty $evaluation 'Properties'
        if ($null -eq $propertiesProperty) {
            Add-Failure "$projectRelativePath evaluation is missing Properties metadata."
        } else {
            $optInProperty = Get-ObjectProperty $propertiesProperty.Value 'ReeYinUseHalconDotNet'
            if ($null -eq $optInProperty) {
                Add-Failure "$projectRelativePath evaluation is missing ReeYinUseHalconDotNet metadata."
            } else {
                $evaluatedOptIn = [string]$optInProperty.Value
                if ($expectedReferenceCount -eq 1 -and $evaluatedOptIn -cne 'true') {
                    Add-Failure "$projectRelativePath evaluates ReeYinUseHalconDotNet as '$evaluatedOptIn'; expected exactly true."
                }
                if ($expectedReferenceCount -eq 0 -and $evaluatedOptIn -ieq 'true') {
                    Add-Failure "$projectRelativePath evaluates ReeYinUseHalconDotNet as '$evaluatedOptIn'; expected not true."
                }
            }
        }

        $referenceItems = $null
        $itemsProperty = Get-ObjectProperty $evaluation 'Items'
        if ($null -eq $itemsProperty) {
            Add-Failure "$projectRelativePath evaluation is missing Items metadata."
        } else {
            $referenceProperty = Get-ObjectProperty $itemsProperty.Value 'Reference'
            if ($null -eq $referenceProperty) {
                Add-Failure "$projectRelativePath evaluation is missing Reference item metadata."
            } else {
                $referenceItems = $referenceProperty.Value
            }
        }

        $references = @(Get-HalconReferences $referenceItems $projectRelativePath)
        if ($references.Count -ne $expectedReferenceCount) {
            Add-Failure "$projectRelativePath has $($references.Count) HALCON references; expected $expectedReferenceCount."
        }
        if ($references.Count -eq 1) {
            $identityProperty = Get-ObjectProperty $references[0] 'Identity'
            if ($null -eq $identityProperty) {
                Add-Failure "$projectRelativePath resolved HALCON reference is missing Identity metadata."
            } else {
                $simpleIdentity = Get-SimpleAssemblyName ([string]$identityProperty.Value)
                if ($simpleIdentity -cne 'halcondotnetxl') {
                    Add-Failure "$projectRelativePath resolves $simpleIdentity, expected halcondotnetxl."
                }
            }

            $hintPathProperty = Get-ObjectProperty $references[0] 'HintPath'
            if ($null -eq $hintPathProperty -or
                [string]::IsNullOrWhiteSpace([string]$hintPathProperty.Value)) {
                Add-Failure "$projectRelativePath resolved HALCON reference is missing HintPath metadata."
            } else {
                try {
                    $resolvedHintPath = [IO.Path]::GetFullPath([string]$hintPathProperty.Value)
                    $expectedHintPath = [IO.Path]::GetFullPath($halconPath)
                    if (-not [string]::Equals(
                        $resolvedHintPath,
                        $expectedHintPath,
                        [StringComparison]::OrdinalIgnoreCase)) {
                        Add-Failure "$projectRelativePath resolves an unexpected HALCON path."
                    }
                } catch {
                    Add-Failure "$projectRelativePath has an invalid HALCON HintPath: $($_.Exception.Message)"
                }
            }

            $privateProperty = Get-ObjectProperty $references[0] 'Private'
            if ($null -ne $privateProperty -and
                [string]$privateProperty.Value -cne 'true') {
                Add-Failure "$projectRelativePath resolves HALCON with Private=$($privateProperty.Value), expected true."
            }
        }
    }
}

if ($failures.Count -gt 0) {
    $orderedFailures = [string[]]@($failures)
    [Array]::Sort($orderedFailures, [System.StringComparer]::Ordinal)
    $orderedFailures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    throw "HALCON reference validation failed with $($failures.Count) issue(s)."
}

Write-Host "HALCON reference validation passed for $($projects.Count) projects and $enabledCount consumers."
