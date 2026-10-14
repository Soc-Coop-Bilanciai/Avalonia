#requires -Version 7.0
[CmdletBinding()]
param([string]$Source)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$distribution = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'distribution.json') -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($Source)) { $Source = $distribution.publishSource }
if ([string]::IsNullOrWhiteSpace($Source)) { throw 'Configure publishSource or pass -Source with the NuGet upload endpoint.' }
if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) { throw 'Set NUGET_API_KEY in the publishing environment.' }
$packageRoot = Join-Path $root 'artifacts/nuget'
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'bilanciai-release.json') -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($manifest.repositoryUrl)) { throw 'Configure repositoryUrl and rebuild before publishing.' }
$releaseCommit = git -C $root rev-parse --verify --end-of-options "bilanciai/$($manifest.version)^{commit}"
if ($LASTEXITCODE -ne 0 -or $releaseCommit -ne $manifest.forkCommit) { throw 'Create the immutable release tag at the manifest commit before publishing.' }
$packages = @()
foreach ($entry in $manifest.packages) {
    if ([IO.Path]::GetFileName($entry.file) -ne $entry.file) { throw 'Manifest package names must not contain directories.' }
    $package = Join-Path $packageRoot $entry.file
    if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $entry.sha256) { throw "Package hash mismatch: $($entry.file)" }
    $packages += $package
}
if ($packages.Count -eq 0) { throw 'No packages in the release manifest.' }
foreach ($package in $packages) {
    # A duplicate fails deliberately: a published version must never be silently replaced.
    & dotnet nuget push $package --source $Source --api-key $env:NUGET_API_KEY
    if ($LASTEXITCODE -ne 0) { throw "Publishing failed for $([IO.Path]::GetFileName($package)). Inspect the feed before retrying a partial upload." }
}
Write-Host "Published $($packages.Count) packages for Avalonia $($manifest.version). Archive bilanciai-release.json with this release."
