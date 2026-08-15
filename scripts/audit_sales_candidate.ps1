<#
.SYNOPSIS
    HexCivの販売候補を、成果物・検証ログ・販売KPIの3層で監査する。

.DESCRIPTION
    ZIPやmanifestの存在だけで「販売可能」と誤判定しないための監査スクリプト。
    ローカル候補の再現可能性と、本番計測／公開の外部ゲートを分離して出力する。
    外部サービスへの書き込み、公開、価格変更、決済は行わない。

.PARAMETER ConfigPath
    候補定義JSON。既定はリポジトリ直下のsales_candidate.json。

.PARAMETER OutputDirectory
    JSON/Markdown監査結果の出力先。
#>
[CmdletBinding()]
param(
    [string]$ConfigPath,
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $ConfigPath) {
    $ConfigPath = Join-Path $repoRoot 'sales_candidate.json'
}
if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'reports\sales-candidate'
}

function Resolve-RepoPath([string]$relativePath) {
    return Join-Path $repoRoot ($relativePath -replace '/', '\')
}

function New-Check([string]$name, [bool]$passed, [string]$detail) {
    return [pscustomobject][ordered]@{
        name   = $name
        passed = $passed
        detail = $detail
    }
}

if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
    throw "候補定義がありません: $ConfigPath"
}

$config = Get-Content -LiteralPath $ConfigPath -Raw -Encoding UTF8 | ConvertFrom-Json
$candidateDir = Resolve-RepoPath $config.candidateDirectory
if (-not (Test-Path -LiteralPath $candidateDir -PathType Container)) {
    throw "候補ディレクトリがありません: $candidateDir"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$packageResults = @()
foreach ($package in $config.packages) {
    $checks = @()
    $zipPath = Join-Path $candidateDir $package.zip
    $manifestPath = Join-Path $candidateDir $package.manifest

    $checks += New-Check 'zip exists' (Test-Path -LiteralPath $zipPath -PathType Leaf) $package.zip
    $checks += New-Check 'manifest exists' (Test-Path -LiteralPath $manifestPath -PathType Leaf) $package.manifest

    if ((Test-Path -LiteralPath $zipPath -PathType Leaf) -and
        (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        $manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $zipItem = Get-Item -LiteralPath $zipPath
        $actualHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()

        $checks += New-Check 'manifest filename' ($manifest.fileName -eq $zipItem.Name) ("manifest={0}" -f $manifest.fileName)
        $checks += New-Check 'manifest size' ([int64]$manifest.sizeBytes -eq $zipItem.Length) ("manifest={0}; actual={1}" -f $manifest.sizeBytes, $zipItem.Length)
        $checks += New-Check 'manifest sha256' ($manifest.sha256 -eq $actualHash) $actualHash

        $archive = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
        try {
            $entries = @($archive.Entries | ForEach-Object { $_.FullName -replace '\\', '/' })
            $requiredExePattern = '/{0}$' -f [regex]::Escape($package.requiredExe)
            $requiredAssemblyPattern = '/{0}/Managed/Assembly-CSharp\.dll$' -f [regex]::Escape($package.requiredDataDirectory)
            $exeCount = @($entries | Where-Object { $_ -match $requiredExePattern }).Count
            $assemblyCount = @($entries | Where-Object { $_ -match $requiredAssemblyPattern }).Count
            $readmeCount = @($entries | Where-Object { $_ -match '/README\.md$' }).Count
            $licenseCount = @($entries | Where-Object { $_ -match '/LICENSE\.md$' }).Count
            $forbiddenCount = @($entries | Where-Object { $_ -match '(^|/)(UnityCrashHandler64\.exe|[^/]+\.pdb)$' }).Count

            $checks += New-Check 'required exe' ($exeCount -eq 1) ("count={0}" -f $exeCount)
            $checks += New-Check 'managed assembly' ($assemblyCount -eq 1) ("count={0}" -f $assemblyCount)
            $checks += New-Check 'README' ($readmeCount -eq 1) ("count={0}" -f $readmeCount)
            $checks += New-Check 'LICENSE' ($licenseCount -eq 1) ("count={0}" -f $licenseCount)
            $checks += New-Check 'forbidden debug/crash files' ($forbiddenCount -eq 0) ("count={0}" -f $forbiddenCount)
        }
        finally {
            $archive.Dispose()
        }
    }

    $packageResults += [pscustomobject][ordered]@{
        edition = $package.edition
        passed  = @($checks | Where-Object { -not $_.passed }).Count -eq 0
        checks  = $checks
    }
}

$logResults = @()
$fatalPattern = 'SMOKE FAIL|BUILD FAIL|Compilation failed|NullReferenceException|Unhandled exception|Crash!!!|Aborting batchmode due to failure'
foreach ($log in $config.validationLogs) {
    $path = Resolve-RepoPath $log.path
    $checks = @()
    $exists = Test-Path -LiteralPath $path -PathType Leaf
    $checks += New-Check 'log exists' $exists $log.path
    if ($exists) {
        $content = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        $checks += New-Check 'success marker' $content.Contains([string]$log.successMarker) ([string]$log.successMarker)
        foreach ($marker in @($log.additionalMarkers)) {
            $checks += New-Check 'additional marker' $content.Contains([string]$marker) ([string]$marker)
        }
        $checks += New-Check 'fatal marker absent' (-not ($content -match $fatalPattern)) $fatalPattern
    }
    $logResults += [pscustomobject][ordered]@{
        name   = $log.name
        passed = @($checks | Where-Object { -not $_.passed }).Count -eq 0
        checks = $checks
    }
}

$assetResults = @()
foreach ($assetPath in $config.salesAssets) {
    $path = Resolve-RepoPath $assetPath
    $exists = Test-Path -LiteralPath $path -PathType Leaf
    $assetResults += [pscustomobject][ordered]@{
        path   = $assetPath
        passed = $exists -and ((Get-Item -LiteralPath $path).Length -gt 0)
        bytes  = $(if ($exists) { (Get-Item -LiteralPath $path).Length } else { 0 })
        sha256 = $(if ($exists) { (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null })
    }
}

$candidateCommitExists = $false
& git -C $repoRoot cat-file -e ("{0}^{{commit}}" -f $config.candidateCommit) 2>$null
if ($LASTEXITCODE -eq 0) {
    $candidateCommitExists = $true
}

$artifactReady = (@($packageResults | Where-Object { -not $_.passed }).Count -eq 0) -and
    (@($logResults | Where-Object { -not $_.passed }).Count -eq 0) -and
    (@($assetResults | Where-Object { -not $_.passed }).Count -eq 0) -and
    $candidateCommitExists
$measurementContractDefined = @($config.measurementContract.stages).Count -eq 4 -and
    -not [string]::IsNullOrWhiteSpace([string]$config.measurementContract.primaryKpi)
$activationBlocked = @($config.activationGates | Where-Object { $_.status -ne 'passed' }).Count -gt 0

$report = [pscustomobject][ordered]@{
    schemaVersion              = 1
    generatedAtUtc             = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    candidateId                = $config.candidateId
    candidateCommit            = $config.candidateCommit
    candidateCommitExists      = $candidateCommitExists
    classification             = 'MEASURABLE_SALES_CANDIDATE'
    artifactCandidateDecision  = $(if ($artifactReady) { 'GO' } else { 'NO_GO' })
    measurementContractDecision = $(if ($measurementContractDefined) { 'GO' } else { 'NO_GO' })
    measurementActivation      = $(if ($activationBlocked) { 'BLOCKED' } else { 'GO' })
    publicSalesDecision        = $(if ($artifactReady -and $measurementContractDefined -and -not $activationBlocked) { 'GO' } else { 'NO_GO' })
    packages                   = $packageResults
    validationLogs             = $logResults
    salesAssets                = $assetResults
    measurementContract       = $config.measurementContract
    activationGates           = $config.activationGates
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$jsonPath = Join-Path $OutputDirectory 'latest.json'
$markdownPath = Join-Path $OutputDirectory 'latest.md'
$report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$gateRows = @($report.activationGates | ForEach-Object {
    '| {0} | {1} | {2} | {3} |' -f $_.id, $_.name, $_.status.ToUpperInvariant(), $_.evidence
})
$packageRows = @($report.packages | ForEach-Object {
    '| {0} | {1} |' -f $_.edition, $(if ($_.passed) { 'PASS' } else { 'FAIL' })
})
$logRows = @($report.validationLogs | ForEach-Object {
    '| {0} | {1} |' -f $_.name, $(if ($_.passed) { 'PASS' } else { 'FAIL' })
})

$markdown = @"
# HexCiv sales candidate audit

- Generated: $($report.generatedAtUtc)
- Candidate: ``$($report.candidateId)`` at ``$($report.candidateCommit)``
- Classification: **$($report.classification)**
- Artifact candidate: **$($report.artifactCandidateDecision)**
- Measurement contract: **$($report.measurementContractDecision)**
- Measurement activation: **$($report.measurementActivation)**
- Public sales: **$($report.publicSalesDecision)**

Organic purchases remain **unmeasured**. The one self-purchase is excluded from every organic sales KPI.

## Packages

| Edition | Result |
|---|---|
$($packageRows -join "`n")

## Validation evidence

| Evidence | Result |
|---|---|
$($logRows -join "`n")

## Activation gates

| Gate | Requirement | Status | Evidence |
|---|---|---|---|
$($gateRows -join "`n")

This audit never publishes a build, changes a price, performs a payment, or writes to an external service.
"@
$markdown | Set-Content -LiteralPath $markdownPath -Encoding utf8

Write-Output 'SALES CANDIDATE AUDIT OK'
Write-Output ("  artifact candidate : {0}" -f $report.artifactCandidateDecision)
Write-Output ("  measurement contract: {0}" -f $report.measurementContractDecision)
Write-Output ("  measurement activation: {0}" -f $report.measurementActivation)
Write-Output ("  public sales       : {0}" -f $report.publicSalesDecision)
Write-Output ("  json               : {0}" -f $jsonPath)
Write-Output ("  markdown           : {0}" -f $markdownPath)

if (-not $artifactReady -or -not $measurementContractDefined) {
    exit 1
}
