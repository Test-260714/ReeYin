param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath($RepositoryRoot)
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Result {
    param(
        [bool]$Passed,
        [string]$Category,
        [string]$Message
    )

    if ($Passed) {
        Write-Output "[PASS][$Category] $Message"
    }
    else {
        Write-Output "[FAIL][$Category] $Message"
        $script:failures.Add("[$Category] $Message")
    }
}

function Get-RelativePathCompat {
    param([string]$BasePath, [string]$TargetPath)

    $baseWithSeparator = $BasePath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
    $baseUri = [Uri]$baseWithSeparator
    $targetUri = [Uri]$TargetPath
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

$requiredFiles = @(
    'AGENTS.md',
    'CONTRIBUTING.md',
    'docs/development/README.md',
    'docs/development/change-impact-map.md',
    'docs/development/build-and-test-map.md',
    'docs/development/architecture.md',
    'docs/development/coding-standards.md',
    'docs/development/testing-and-verification.md',
    'docs/development/module-development.md',
    'docs/development/safety-and-security.md',
    'docs/development/review-and-delivery.md',
    'docs/development/ai-development.md',
    'Application/AGENTS.md',
    'Shell/AGENTS.md',
    'CustomizedDemand/AGENTS.md',
    'Semiconductor/AGENTS.md',
    'GemeralUI/AGENTS.md',
    'Core/AGENTS.md',
    'Hardware/AGENTS.md',
    'Algorithm/AGENTS.md',
    'Tools/AGENTS.md',
    'CustomUI/AGENTS.md',
    'thirdparty/AGENTS.md',
    'packages/AGENTS.md',
    'OutputExe/AGENTS.md',
    'Resource/AGENTS.md'
)

$governanceFiles = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $requiredFiles) {
    $fullPath = Join-Path $root $relativePath
    $exists = Test-Path -LiteralPath $fullPath -PathType Leaf
    Add-Result -Passed $exists -Category 'required-file' -Message $relativePath
    if ($exists) {
        $governanceFiles.Add($fullPath)
    }
}

foreach ($file in $governanceFiles) {
    $relativeFile = (Get-RelativePathCompat -BasePath $root -TargetPath $file).Replace('\', '/')
    $lines = Get-Content -LiteralPath $file -Encoding utf8
    $insideFence = $false

    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line -match '^\s*```') {
            $insideFence = -not $insideFence
            continue
        }
        if ($insideFence) {
            continue
        }

        if ($line -match '^(<<<<<<<|=======|>>>>>>>)') {
            Add-Result -Passed $false -Category 'merge-marker' -Message ("{0}:{1}" -f $relativeFile, ($index + 1))
        }

        if ($line -match '\b(TODO|TBD)\b') {
            Add-Result -Passed $false -Category 'placeholder' -Message ("{0}:{1}" -f $relativeFile, ($index + 1))
        }

        $matches = [regex]::Matches($line, '!?(?:\[[^\]]*\])\(([^)]+)\)')
        foreach ($match in $matches) {
            $target = $match.Groups[1].Value.Trim().Trim('<', '>')
            if ($target -match '^(https?://|mailto:|#)') {
                continue
            }

            $fileTarget = ($target -split '#', 2)[0]
            if ([string]::IsNullOrWhiteSpace($fileTarget)) {
                continue
            }

            $decodedTarget = [System.Uri]::UnescapeDataString($fileTarget)
            $resolvedTarget = [System.IO.Path]::GetFullPath((Join-Path (Split-Path -Parent $file) $decodedTarget))
            $withinRoot = $resolvedTarget.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)
            $targetExists = $withinRoot -and (Test-Path -LiteralPath $resolvedTarget)
            Add-Result -Passed $targetExists -Category 'relative-link' -Message ("{0}:{1} -> {2}" -f $relativeFile, ($index + 1), $target)
        }
    }
}

$impactMapPath = Join-Path $root 'docs/development/change-impact-map.md'
if (Test-Path -LiteralPath $impactMapPath) {
    $impactMap = Get-Content -Raw -LiteralPath $impactMapPath -Encoding utf8
    $ignoredDirectories = @('.git', '.vs', 'bin', 'obj')
    foreach ($directory in Get-ChildItem -LiteralPath $root -Directory) {
        if ($ignoredDirectories -contains $directory.Name) {
            continue
        }
        $covered = $impactMap.Contains("``$($directory.Name)/``")
        Add-Result -Passed $covered -Category 'directory-coverage' -Message $directory.Name
    }
}

$reviewPath = Join-Path $root 'docs/development/review-and-delivery.md'
if (Test-Path -LiteralPath $reviewPath) {
    $reviewContent = Get-Content -Raw -LiteralPath $reviewPath -Encoding utf8
    foreach ($field in @('risk', 'verification', 'compatibility', 'rollback', 'unverified', 'review', 'approval')) {
        Add-Result -Passed $reviewContent.Contains($field) -Category 'delivery-template' -Message $field
    }
}

if ($failures.Count -gt 0) {
    Write-Output ("Governance validation failed: {0} issue(s)." -f $failures.Count)
    exit 1
}

Write-Output ("Governance validation passed: {0} required file(s)." -f $requiredFiles.Count)
exit 0
