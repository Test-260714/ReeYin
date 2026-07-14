param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $RepositoryRoot 'scripts/Test-Governance.ps1'
if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
    throw "Governance validator does not exist: $scriptPath"
}

function Invoke-Validator {
    param([string]$Root)

    & $scriptPath -RepositoryRoot $Root *> $null
    return $LASTEXITCODE
}

function Copy-GovernanceFixture {
    param([string]$Source, [string]$Destination)

    $files = @(
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

    foreach ($relativePath in $files) {
        $sourcePath = Join-Path $Source $relativePath
        $targetPath = Join-Path $Destination $relativePath
        $targetDirectory = Split-Path -Parent $targetPath
        New-Item -ItemType Directory -Path $targetDirectory -Force | Out-Null
        Copy-Item -LiteralPath $sourcePath -Destination $targetPath
    }
}

$failures = [System.Collections.Generic.List[string]]::new()

if ((Invoke-Validator -Root $RepositoryRoot) -ne 0) {
    $failures.Add('valid repository governance must return exit code 0')
}

$fixtureRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("reeyin-governance-{0}" -f [Guid]::NewGuid())
try {
    Copy-GovernanceFixture -Source $RepositoryRoot -Destination $fixtureRoot

    $brokenLinkRoot = Join-Path $fixtureRoot 'broken-link'
    Copy-GovernanceFixture -Source $RepositoryRoot -Destination $brokenLinkRoot
    Add-Content -LiteralPath (Join-Path $brokenLinkRoot 'AGENTS.md') -Value "`n[broken](missing-policy.md)"
    if ((Invoke-Validator -Root $brokenLinkRoot) -eq 0) {
        $failures.Add('broken relative link must return a nonzero exit code')
    }

    $missingFileRoot = Join-Path $fixtureRoot 'missing-file'
    Copy-GovernanceFixture -Source $RepositoryRoot -Destination $missingFileRoot
    Remove-Item -LiteralPath (Join-Path $missingFileRoot 'CONTRIBUTING.md')
    if ((Invoke-Validator -Root $missingFileRoot) -eq 0) {
        $failures.Add('missing required file must return a nonzero exit code')
    }

    $mergeMarkerRoot = Join-Path $fixtureRoot 'merge-marker'
    Copy-GovernanceFixture -Source $RepositoryRoot -Destination $mergeMarkerRoot
    Add-Content -LiteralPath (Join-Path $mergeMarkerRoot 'AGENTS.md') -Value "`n<<<<<<< local"
    if ((Invoke-Validator -Root $mergeMarkerRoot) -eq 0) {
        $failures.Add('merge marker must return a nonzero exit code')
    }
}
finally {
    if (Test-Path -LiteralPath $fixtureRoot) {
        Remove-Item -LiteralPath $fixtureRoot -Recurse -Force
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output '[PASS] Governance validator success and negative fixtures'
exit 0
